#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if CSHARP_7_OR_LATER // await not available in old runtime.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using Proto.Promises.Async.CompilerServices;
using Proto.Utils;

namespace Proto.Promises
{
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
            partial struct PromiseAwaiter : ICriticalNotifyCompletion
        {
            private readonly Promise _promise;

            /// <summary>
            /// Internal use.
            /// </summary>
            internal PromiseAwaiter(Promise promise)
            {
                if (promise._ref != null)
                {
                    promise._ref.IncrementIdFromAwaiter(promise._id);
                    _promise = new Promise(promise._ref, promise._ref.Id);
                }
                else
                {
                    _promise = promise;
                }
            }

            public bool IsCompleted
            {
                get
                {
                    ValidateOperation(1);

                    return _promise._ref == null || _promise._ref.State != Promise.State.Pending;
                }
            }

            public void GetResult()
            {
                ValidateOperation(1);

                if (_promise._ref != null)
                {
                    _promise._ref.GetResultForAwaiter(_promise._id);
                }
            }

            public void OnCompleted(Action continuation)
            {
                ValidateOperation(1);

                // If this is called only from the `await` keyword, the check is unnecessary.
                // The check is added for safety in case users call `promise.GetAwaiter()` and use the awaiter directly.
                if (_promise._ref != null)
                {
                    _promise._ref.OnCompletedForAwaiter(continuation);
                }
                else
                {
                    _promise.Finally(continuation);
                }
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            partial void ValidateOperation(int skipFrames);
#if PROMISE_DEBUG
            partial void ValidateOperation(int skipFrames)
            {
                Internal.ValidateOperation(_promise, skipFrames + 1);
            }
#endif
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
            partial struct PromiseAwaiter<T> : ICriticalNotifyCompletion
        {
            private readonly Promise<T> _promise;

            /// <summary>
            /// Internal use.
            /// </summary>
            internal PromiseAwaiter(Promise<T> promise)
            {
                if (promise._ref != null)
                {
                    promise._ref.IncrementIdFromAwaiter(promise._id);
                    _promise = new Promise<T>(promise._ref, promise._ref.Id);
                }
                else
                {
                    _promise = promise;
                }
            }

            public bool IsCompleted
            {
                get
                {
                    ValidateOperation(1);

                    return _promise._ref == null || _promise._ref.State != Promise.State.Pending;
                }
            }

            public T GetResult()
            {
                ValidateOperation(1);

                if (_promise._ref != null)
                {
                    return _promise._ref.GetResultForAwaiter<T>(_promise._id);
                }
                return _promise._result;
            }

            public void OnCompleted(Action continuation)
            {
                ValidateOperation(1);

                // If this is called only from the `await` keyword, the check is unnecessary.
                // The check is added for safety in case users call `promise.GetAwaiter()` and use the awaiter directly.
                if (_promise._ref != null)
                {
                    _promise._ref.OnCompletedForAwaiter(continuation);
                }
                else
                {
                    _promise.Finally(continuation);
                }
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            partial void ValidateOperation(int skipFrames);
#if PROMISE_DEBUG
            partial void ValidateOperation(int skipFrames)
            {
                Internal.ValidateOperation(_promise, skipFrames + 1);
            }
#endif
        }
    }

    partial struct Promise
    {
        // TODO: ConfigureAwait taking CancelationToken, ExecutionOptions, and/or progress normalization.

        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
        public PromiseAwaiter GetAwaiter()
        {
            ValidateOperation(1);

            return new PromiseAwaiter(this);
        }
    }

    partial struct Promise<T>
    {
        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
        public PromiseAwaiter<T> GetAwaiter()
        {
            ValidateOperation(1);

            return new PromiseAwaiter<T>(this);
        }
    }
}
#endif // C#7