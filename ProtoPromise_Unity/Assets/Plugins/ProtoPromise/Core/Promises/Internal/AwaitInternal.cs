#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if CSHARP_7_3_OR_NEWER // await not available in old runtime.

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Proto.Promises.Async.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            partial struct SmallFields
            {
                [MethodImpl(InlineOption)]
                internal bool InterlockedIncrementPromiseIdAndSetFlagsAndRetain(short promiseId, PromiseFlags flags)
                {
                    unchecked // We want the id to wrap around.
                    {
                        Thread.MemoryBarrier();
                        short idPlusOne = (short) (promiseId + 1);
                        SmallFields initialValue = default(SmallFields), newValue;
                        do
                        {
                            initialValue._longValue = Interlocked.Read(ref _longValue);
                            // Make sure id matches. Also check for id + 1 since this might be called after OnCompleted is hooked up.
                            if (initialValue._promiseId != promiseId & initialValue._promiseId != idPlusOne)
                            {
                                throw new InvalidOperationException("Attempted to GetResult on an invalid PromiseAwaiter.", GetFormattedStacktrace(3));
                            }
                            newValue = initialValue;
                            // If HadCallback is false, we should not retain. Convert the flag to 0 or 1 without branching.
                            ushort retainAdd = (ushort) (1 - ((byte) (~initialValue._flags & PromiseFlags.HadCallback) >> 7));
                            newValue._retains += retainAdd;
                            ++newValue._promiseId;
                            newValue._flags |= flags;
                        } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                        return newValue._retains == 0;
                    }
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedSetFlagsAndRetain(short promiseId, PromiseFlags flags)
                {
                    Thread.MemoryBarrier();
                    short idPlusOne = (short) (promiseId + 1);
                    SmallFields initialValue = default(SmallFields), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches. Also check for id + 1 since this might be called after the preserved promise is forgotten.
                        if (initialValue._promiseId != promiseId & initialValue._promiseId != idPlusOne)
                        {
                            throw new InvalidOperationException("Attempted to GetResult on an invalid PromiseAwaiter.", GetFormattedStacktrace(3));
                        }
                        newValue = initialValue;
                        ++newValue._retains;
                        newValue._flags |= flags;
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                }
            }

            internal abstract void IncrementIdAndSetFlags(short promiseId);

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal System.Runtime.ExceptionServices.ExceptionDispatchInfo GetExceptionDispatchInfo(Promise.State state)
            {
                if (state == Promise.State.Canceled)
                {
                    MaybeDispose();
                    throw CanceledExceptionInternal.GetOrCreate();
                }
                if (state == Promise.State.Rejected)
                {
                    var exceptionDispatchInfo = ((IRejectValueContainer) _valueOrPrevious).GetExceptionDispatchInfo();
                    MaybeDispose();
                    return exceptionDispatchInfo;
                }
                throw new InvalidOperationException("PromiseAwaiter.GetResult() is only valid when the promise is completed.", GetFormattedStacktrace(2));
            }

            partial class PromiseSingleAwait
            {
                internal override void IncrementIdAndSetFlags(short promiseId)
                {
                    _smallFields.InterlockedIncrementPromiseIdAndSetFlagsAndRetain(promiseId, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);
                    ThrowIfInPool(this);
                }
            }

            partial class PromiseMultiAwait
            {
                internal override void IncrementIdAndSetFlags(short promiseId)
                {
                    _smallFields.InterlockedSetFlagsAndRetain(promiseId, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);
                    ThrowIfInPool(this);
                }
            }

            [MethodImpl(InlineOption)]
            internal void OnCompleted(Action continuation, short promiseId)
            {
                MarkAwaited(promiseId, PromiseFlags.None);
                HookupNewWaiter(AwaiterRef.GetOrCreate(continuation));
            }

            [MethodImpl(InlineOption)]
            internal void AwaitOnCompletedInternal(AsyncPromiseRef asyncPromiseRef, short promiseId)
            {
                MarkAwaited(promiseId, PromiseFlags.None);
                HookupNewWaiter(asyncPromiseRef);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            private sealed class AwaiterRef : HandleablePromiseBase, ITraceable
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif

                private Action _continuation;

                private AwaiterRef() { }

                [MethodImpl(InlineOption)]
                internal static AwaiterRef GetOrCreate(Action continuation)
                {
                    var awaiter = ObjectPool<HandleablePromiseBase>.TryTake<AwaiterRef>()
                        ?? new AwaiterRef();
                    awaiter._continuation = continuation;
                    SetCreatedStacktrace(awaiter, 3);
                    return awaiter;
                }

                [MethodImpl(InlineOption)]
                private void Dispose()
                {
                    _continuation = null;
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    var callback = _continuation;
#if PROMISE_DEBUG
                    SetCurrentInvoker(this);
#else
                    Dispose();
#endif
                    try
                    {
                        callback.Invoke();
                    }
                    catch (Exception e)
                    {
                        // This should never hit if the `await` keyword is used, but a user manually subscribing to OnCompleted could throw.
                        AddRejectionToUnhandledStack(e, this);
                    }
#if PROMISE_DEBUG
                    ClearCurrentInvoker();
                    Dispose();
#endif
                }

                internal override void Handle(ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
            }
        }

        internal interface IPromiseAwaiter
        {
            void AwaitOnCompletedInternal(PromiseRef.AsyncPromiseRef asyncPromiseRef);
        }

        // TODO: Remove AwaitOverrider in .Net 5+ for a direct call instead.
        // Override AwaitOnCompleted implementation to prevent boxing in Unity.
#if UNITY_2021_2_OR_NEWER || !UNITY_5_5_OR_NEWER // C# 9 added in 2021.2. We can also use this in non-Unity library since CIL has supported function pointers forever.
        internal unsafe abstract class AwaitOverrider<T> where T : INotifyCompletion
        {
            private static delegate*<ref T, PromiseRef.AsyncPromiseRef, void> _awaitOverrider;

            [MethodImpl(InlineOption)]
            internal static bool IsOverridden()
            {
                return _awaitOverrider != null;
            }

            [MethodImpl(InlineOption)]
            internal static void Create<TAwaiter>() where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                AwaitOverriderImpl<TAwaiter>.Create();
            }

            [MethodImpl(InlineOption)]
            internal static void AwaitOnCompletedInternal(ref T awaiter, PromiseRef.AsyncPromiseRef asyncPromiseRef)
            {
                _awaitOverrider(ref awaiter, asyncPromiseRef);
            }

            private sealed class AwaitOverriderImpl<TAwaiter> : AwaitOverrider<TAwaiter> where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                [MethodImpl(InlineOption)]
                internal static void Create()
                {
                    _awaitOverrider = &AwaitOnCompletedVirt;
                }

                [MethodImpl(InlineOption)]
                private static void AwaitOnCompletedVirt(ref TAwaiter awaiter, PromiseRef.AsyncPromiseRef asyncPromiseRef)
                {
                    awaiter.AwaitOnCompletedInternal(asyncPromiseRef);
                }
            }
        }
#else // UNITY_2021_2_OR_NEWER || !UNITY_5_5_OR_NEWER
        internal abstract class AwaitOverrider<T> where T : INotifyCompletion
        {
            private static AwaitOverrider<T> _awaitOverrider;

            [MethodImpl(Internal.InlineOption)]
            internal static bool IsOverridden()
            {
                return _awaitOverrider != null;
            }

            [MethodImpl(Internal.InlineOption)]
            internal static void Create<TAwaiter>() where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                AwaitOverriderImpl<TAwaiter>.Create();
            }

            [MethodImpl(Internal.InlineOption)]
            internal static void AwaitOnCompletedInternal(ref T awaiter, PromiseRef.AsyncPromiseRef asyncPromiseRef)
            {
                _awaitOverrider.AwaitOnCompletedVirt(ref awaiter, asyncPromiseRef);
            }

            protected abstract void AwaitOnCompletedVirt(ref T awaiter, PromiseRef.AsyncPromiseRef asyncPromiseRef);

            private sealed class AwaitOverriderImpl<TAwaiter> : AwaitOverrider<TAwaiter> where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                [MethodImpl(Internal.InlineOption)]
                internal static void Create()
                {
                    _awaitOverrider = new AwaitOverriderImpl<TAwaiter>();
                }

                [MethodImpl(Internal.InlineOption)]
                protected override void AwaitOnCompletedVirt(ref TAwaiter awaiter, PromiseRef.AsyncPromiseRef asyncPromiseRef)
                {
                    awaiter.AwaitOnCompletedInternal(asyncPromiseRef);
                }
            }
        }
#endif // UNITY_2021_2_OR_NEWER || !UNITY_5_5_OR_NEWER
    }

    namespace Async.CompilerServices
    {
        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseAwaiterVoid : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly PromiseAwaiter<Internal.VoidResult> _awaiter;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseAwaiterVoid(Promise promise)
            {
                _awaiter = new PromiseAwaiter<Internal.VoidResult>(promise._target);
            }

            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return _awaiter.IsCompleted;
                }
            }

            [MethodImpl(Internal.InlineOption)]
            public void GetResult()
            {
                var promise = _awaiter._promise;
                ValidateGetResult(promise, 1);
                var _ref = promise._ref;
                if (_ref == null)
                {
                    return;
                }
                _ref.IncrementIdAndSetFlags(promise.Id);
                var state = _ref.State;
                if (state == Promise.State.Resolved)
                {
                    _ref.MaybeDispose();
                    return;
                }
                _ref.GetExceptionDispatchInfo(state).Throw();
            }

            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                _awaiter.OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                _awaiter.UnsafeOnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRef.AsyncPromiseRef asyncPromiseRef)
            {
                _awaiter._promise._ref.AwaitOnCompletedInternal(asyncPromiseRef, _awaiter._promise.Id);
            }

            static partial void ValidateGetResult(Promise<Internal.VoidResult> promise, int skipFrames);
            static partial void ValidateOperation(Promise<Internal.VoidResult> promise, int skipFrames);
#if PROMISE_DEBUG
            static partial void ValidateGetResult(Promise<Internal.VoidResult> promise, int skipFrames)
            {
                if (promise._ref == null)
                {
                    ValidateOperation(promise, skipFrames + 1);
                }
            }

            static partial void ValidateOperation(Promise<Internal.VoidResult> promise, int skipFrames)
            {
                if (!promise.IsValid)
                {
                    throw new InvalidOperationException("Attempted to use PromiseAwaiter incorrectly. You must call IsCompleted, then maybe OnCompleted, then GetResult when it is complete.", Internal.GetFormattedStacktrace(skipFrames + 1));
                }
            }
#endif
        } // struct PromiseAwaiterVoid

    /// <summary>
    /// Used to support the await keyword.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseAwaiter<T> : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            internal readonly Promise<T> _promise;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseAwaiter(Promise<T> promise)
            {
                _promise = promise;
            }

            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var promise = _promise;
                    ValidateOperation(promise, 1);
                    return promise._ref == null || promise._ref.State != Promise.State.Pending;
                }
            }

            [MethodImpl(Internal.InlineOption)]
            public T GetResult()
            {
                var promise = _promise;
                ValidateGetResult(promise, 1);
                var _ref = promise._ref;
                if (_ref == null)
                {
                    return promise.Result;
                }
                _ref.IncrementIdAndSetFlags(promise.Id);
                var state = _ref.State;
                if (state == Promise.State.Resolved)
                {
                    T result = ((Internal.ValueContainer) _ref._valueOrPrevious).GetValue<T>();
                    _ref.MaybeDispose();
                    return result;
                }
                _ref.GetExceptionDispatchInfo(state).Throw();
                throw new Exception(); // This will never be reached, but the compiler needs help understanding that.
            }

            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
                var promise = _promise;
                ValidateOperation(promise, 1);
                if (promise._ref == null)
                {
                    continuation();
                    return;
                }
                promise._ref.OnCompleted(continuation, promise.Id);
            }

            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRef.AsyncPromiseRef asyncPromiseRef)
            {
                _promise._ref.AwaitOnCompletedInternal(asyncPromiseRef, _promise.Id);
            }

            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
            static partial void ValidateGetResult(Promise<T> promise, int skipFrames);
            static partial void ValidateOperation(Promise<T> promise, int skipFrames);
#if PROMISE_DEBUG
            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            {
                Internal.ValidateArgument(arg, argName, skipFrames + 1);
            }

            static partial void ValidateGetResult(Promise<T> promise, int skipFrames)
            {
                if (promise._ref == null)
                {
                    ValidateOperation(promise, skipFrames + 1);
                }
            }

            static partial void ValidateOperation(Promise<T> promise, int skipFrames)
            {
                if (!promise.IsValid)
                {
                    throw new InvalidOperationException("Attempted to use PromiseAwaiter incorrectly. You must call IsCompleted, then maybe OnCompleted, then GetResult when it is complete.", Internal.GetFormattedStacktrace(skipFrames + 1));
                }
            }
#endif
        } // struct PromiseAwaiter<T>

        partial struct PromiseAwaiterVoid
        {
            static PromiseAwaiterVoid()
            {
                Internal.AwaitOverrider<PromiseAwaiterVoid>.Create<PromiseAwaiterVoid>();
            }
        }

        partial struct PromiseAwaiter<T>
        {
            static PromiseAwaiter()
            {
                Internal.AwaitOverrider<PromiseAwaiter<T>>.Create<PromiseAwaiter<T>>();
            }
        }
    } // namespace Async.CompilerServices

    partial struct Promise
    {
        // TODO: ConfigureAwait for reporting progress to an async Promise.

        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public PromiseAwaiterVoid GetAwaiter()
        {
            ValidateOperation(1);
            return new PromiseAwaiterVoid(this);
        }
    }

    partial struct Promise<T>
    {
        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public PromiseAwaiter<T> GetAwaiter()
        {
            ValidateOperation(1);
            return new PromiseAwaiter<T>(this);
        }
    }
}
#endif // C#7