﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
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
                internal bool InterlockedIncrementPromiseIdAndSetFlagsAndMaybeReleaseComplete(short promiseId, PromiseFlags flags)
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
                            // If HadCallback is false, we must release. Convert the flag to 0 or 1 without branching.
                            ushort retainSubtract = (ushort) ((byte) (~initialValue._flags & PromiseFlags.HadCallback) >> 7);
                            newValue._retains -= retainSubtract;
                            ++newValue._promiseId;
                            newValue._flags |= flags;
                        } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                        return newValue._retains == 0;
                    }
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedSetFlags(short promiseId, PromiseFlags flags)
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
                        newValue._flags |= flags;
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                }
            }

            protected abstract bool TryIncrementIdAndSetFlagsAndRelease(short promiseId);

            partial class PromiseSingleAwait
            {
                protected override bool TryIncrementIdAndSetFlagsAndRelease(short promiseId)
                {
                    bool released = _smallFields.InterlockedIncrementPromiseIdAndSetFlagsAndMaybeReleaseComplete(promiseId, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);
                    ThrowIfInPool(this);
                    return released;
                }
            }

            partial class PromiseMultiAwait
            {
                protected override bool TryIncrementIdAndSetFlagsAndRelease(short promiseId)
                {
                    _smallFields.InterlockedSetFlags(promiseId, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);
                    ThrowIfInPool(this);
                    return false;
                }
            }

            [MethodImpl(InlineOption)]
            internal T GetResult<T>(short promiseId)
            {
                bool released = TryIncrementIdAndSetFlagsAndRelease(promiseId);
                if (State == Promise.State.Resolved)
                {
                    T result = ((ValueContainer) _valueOrPrevious).GetValue<T>();
                    if (released)
                    {
                        Dispose();
                    }
                    return result;
                }
                if (State == Promise.State.Pending)
                {
                    throw new InvalidOperationException("PromiseAwaiter.GetResult() is only valid when the promise is completed.", GetFormattedStacktrace(2));
                }
                // Throw unhandled exception or canceled exception.
                Exception exception = ((IThrowable) _valueOrPrevious).GetException();
                if (released)
                {
                    Dispose();
                }
                throw exception;
            }

            [MethodImpl(InlineOption)]
            internal void OnCompleted(Action continuation, short promiseId)
            {
                HookupNewWaiter(AwaiterRef.GetOrCreate(continuation), promiseId, PromiseFlags.None);
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

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal readonly partial struct PromiseAwaiterInternal<T>
        {
            private readonly Promise<T> _promise;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(InlineOption)]
            internal PromiseAwaiterInternal(Promise<T> promise)
            {
                _promise = promise;
            }

            internal bool IsCompleted
            {
                [MethodImpl(InlineOption)]
                get
                {
                    var promise = _promise;
                    ValidateOperation(promise, 1);
                    return promise._ref == null || promise._ref.State != Promise.State.Pending;
                }
            }

            [MethodImpl(InlineOption)]
            internal T GetResult()
            {
                var promise = _promise;
                ValidateGetResult(promise, 1);
                return promise._ref == null
                    ? promise.Result
                    : promise._ref.GetResult<T>(promise.Id);
            }

            [MethodImpl(InlineOption)]
            internal void OnCompleted(Action continuation)
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

            [MethodImpl(InlineOption)]
            internal void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
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
                    throw new InvalidOperationException("Attempted to use PromiseAwaiter incorrectly. You must call IsCompleted, then maybe OnCompleted, then GetResult when it is complete.", GetFormattedStacktrace(skipFrames + 1));
                }
            }
#endif
        }
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
            partial struct PromiseAwaiterVoid : ICriticalNotifyCompletion
        {
            private readonly Internal.PromiseAwaiterInternal<Internal.VoidResult> _awaiter;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseAwaiterVoid(Promise promise)
            {
                _awaiter = new Internal.PromiseAwaiterInternal<Internal.VoidResult>(promise._target);
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
                _awaiter.GetResult();
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
        }

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
            struct PromiseAwaiter<T> : ICriticalNotifyCompletion
        {
            private readonly Internal.PromiseAwaiterInternal<T> _awaiter;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseAwaiter(Promise<T> promise)
            {
                _awaiter = new Internal.PromiseAwaiterInternal<T>(promise);
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
            public T GetResult()
            {
                return _awaiter.GetResult();
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
        }
    }

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