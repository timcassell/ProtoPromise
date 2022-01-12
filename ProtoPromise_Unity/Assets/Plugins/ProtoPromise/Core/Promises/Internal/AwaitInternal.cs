#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if CSHARP_7_3_OR_NEWER // await not available in old runtime.

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
            [MethodImpl(InlineOption)]
            internal T GetResult<T>(short promiseId)
            {
                IncrementIdAndSetFlags(promiseId, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);
                if (State == Promise.State.Resolved)
                {
                    T result = ((ValueContainer) _valueOrPrevious).GetValue<T>();
                    MaybeDispose();
                    return result;
                }
                if (State == Promise.State.Pending)
                {
                    throw new InvalidOperationException("PromiseAwaiter.GetResult() is only valid when the promise is completed.", GetFormattedStacktrace(2));
                }
                // Throw unhandled exception or canceled exception.
                Exception exception = ((IThrowable) _valueOrPrevious).GetException();
                MaybeDispose();
                throw exception;
            }

            [MethodImpl(InlineOption)]
            internal void OnCompleted(Action continuation, short promiseId)
            {
                InterlockedRetainAndSetFlagsInternal(promiseId, PromiseFlags.None);
                HookupNewWaiter(AwaiterRef.GetOrCreate(continuation));
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

                internal override void Handle(ref ExecutionScheduler executionScheduler)
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

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    executionScheduler.ScheduleSynchronous(this);
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal readonly partial struct PromiseAwaiterInternal<T>
        {
            private readonly Promise<T> _promise;
#if PROMISE_DEBUG
            // To make sure this isn't reused.
            private class IdContainer
            {
                volatile internal int _id;
            }
            private readonly IdContainer _idContainer;
#endif

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(InlineOption)]
            internal PromiseAwaiterInternal(Promise<T> promise)
            {
                // Duplicate force gets a single use promise if it's a multi use promise.
                // It also prevents the promise from being used again improperly if it's a single use promise.
                // And it internally validates the promise.
                _promise = promise.Duplicate();
#if PROMISE_DEBUG
                _idContainer = new IdContainer() { _id = _promise.Id };
#endif
            }

            internal bool IsCompleted
            {
                [MethodImpl(InlineOption)]
                get
                {
                    ValidateOperation(1);
                    var _ref = _promise._ref;
                    return _ref == null || _ref.State != Promise.State.Pending;
                }
            }

            [MethodImpl(InlineOption)]
            internal T GetResult()
            {
                ValidateGetResult(1);
                var promise = _promise;
                return promise._ref == null
                    ? promise.Result
                    : promise._ref.GetResult<T>(promise.Id);
            }

            [MethodImpl(InlineOption)]
            internal void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
                ValidateOperation(1);
                var _ref = _promise._ref;
                if (_ref == null)
                {
                    continuation();
                    return;
                }
                _ref.OnCompleted(continuation, _promise.Id);
            }

            [MethodImpl(InlineOption)]
            internal void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            partial void ValidateOperation(int skipFrames);
            partial void ValidateGetResult(int skipFrames);
            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
            partial void ValidateOperation(int skipFrames)
            {
                bool isValid = _idContainer != null && _idContainer._id == _promise.Id;
                MaybeThrow(isValid, skipFrames + 1);
            }

            partial void ValidateGetResult(int skipFrames)
            {
                // GetResult may only be called once, set id to invalid for any future use.
                bool isValid = _idContainer != null
                    && Interlocked.CompareExchange(ref _idContainer._id, int.MaxValue, _promise.Id) == _promise.Id;
                MaybeThrow(isValid, skipFrames + 1);
            }

            private static void MaybeThrow(bool isValid, int skipFrames)
            {
                if (!isValid)
                {
                    throw new InvalidOperationException("Invalid use of PromiseAwaiter.", GetFormattedStacktrace(skipFrames + 1));
                }
            }

            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            {
                Internal.ValidateArgument(arg, argName, skipFrames + 1);
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