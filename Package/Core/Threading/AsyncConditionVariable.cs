#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Threading
{

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    // This type only works with AsyncLock, so we don't include it if AsyncLock is not included.

    /// <summary>
    /// An async-compatible condition variable.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed class AsyncConditionVariable : Internal.ITraceable
    {
        // These must not be readonly.
        internal Internal.ValueLinkedQueue<Internal.IAsyncLockPromise> _queue = new Internal.ValueLinkedQueue<Internal.IAsyncLockPromise>();
        volatile internal AsyncLock _lock;

        /// <summary>
        /// Creates a new async-compatible condition variable.
        /// </summary>
#if PROMISE_DEBUG
        public AsyncConditionVariable()
        {
            Internal.SetCreatedStacktraceInternal(this, 1);
        }

        Internal.CausalityTrace Internal.ITraceable.Trace { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        ~AsyncConditionVariable()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var locker = _lock;
            if (locker == null)
            {
                return;
            }
            // If the associated lock was abandoned, report it. Otherwise report this abandoned.
            var rejectContainer = locker._abandonedRejection;
            bool lockWasAbandoned = rejectContainer != null;
            if (!lockWasAbandoned)
            {
                rejectContainer = Internal.CreateRejectContainer(new AbandonedConditionVariableException("An AsyncConditionVariable was collected with waiters still pending."), int.MinValue, null, this);
            }
            Internal.ValueLinkedQueue<Internal.IAsyncLockPromise> queue;
            lock (locker)
            {
                queue = _queue.TakeElements();
            }
            if (queue.IsNotEmpty)
            {
                var stack = queue.MoveElementsToStackUnsafe();
                do
                {
                    stack.Pop().Reject(rejectContainer);
                } while (stack.IsNotEmpty);
            }
            if (!lockWasAbandoned)
            {
                rejectContainer.ReportUnhandled();
            }
        }
#else // PROMISE_DEBUG
        public AsyncConditionVariable() { }
#endif

        /// <summary>
        /// Release the lock and asynchronously wait for a notify signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// The lock will be re-acquired before the returned <see cref="Promise{T}"/> is resolved.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <returns>
        /// A <see cref="Promise"/> that will be resolved when the lock is re-acquired.
        /// </returns>
        [MethodImpl(Internal.InlineOption)]
        public Promise WaitAsync(AsyncLock.Key asyncLockKey)
            => WaitAsync(asyncLockKey, true);

        /// <summary>
        /// Release the lock and asynchronously wait for a notify signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// The lock will be re-acquired before the returned <see cref="Promise{T}"/> is resolved.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <param name="continueOnCapturedContext">If <see langword="true"/>, the async continuation will be executed on the captured context.</param>
        /// <returns>
        /// A <see cref="Promise"/> that will be resolved when the lock is re-acquired.
        /// </returns>
        [MethodImpl(Internal.InlineOption)]
        public Promise WaitAsync(AsyncLock.Key asyncLockKey, bool continueOnCapturedContext)
            => asyncLockKey.WaitAsync(this, continueOnCapturedContext);

        /// <summary>
        /// Release the lock and asynchronously wait for a notify signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>, while observing a <see cref="CancelationToken"/>.
        /// The lock will be re-acquired before the returned <see cref="Promise{T}"/> is resolved.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> that will be resolved when the lock is re-acquired.
        /// Its result will be <see langword="true"/> if the lock was re-acquired before the <paramref name="cancelationToken"/> was canceled,
        /// <see langword="false"/> if the lock was re-acquired after the <paramref name="cancelationToken"/> was canceled
        /// </returns>
        [MethodImpl(Internal.InlineOption)]
        public Promise<bool> TryWaitAsync(AsyncLock.Key asyncLockKey, CancelationToken cancelationToken)
            => TryWaitAsync(asyncLockKey, cancelationToken, true);

        /// <summary>
        /// Release the lock and asynchronously wait for a notify signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>, while observing a <see cref="CancelationToken"/>.
        /// The lock will be re-acquired before the returned <see cref="Promise{T}"/> is resolved.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <param name="continueOnCapturedContext">If <see langword="true"/>, the async continuation will be executed on the captured context.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> that will be resolved when the lock is re-acquired.
        /// Its result will be <see langword="true"/> if the lock was re-acquired before the <paramref name="cancelationToken"/> was canceled,
        /// <see langword="false"/> if the lock was re-acquired after the <paramref name="cancelationToken"/> was canceled
        /// </returns>
        [MethodImpl(Internal.InlineOption)]
        public Promise<bool> TryWaitAsync(AsyncLock.Key asyncLockKey, CancelationToken cancelationToken, bool continueOnCapturedContext)
            => asyncLockKey.TryWaitAsync(this, cancelationToken, continueOnCapturedContext);

        /// <summary>
        /// Release the lock and synchronously wait for a notify signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// The lock will be re-acquired before this method returns.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        [MethodImpl(Internal.InlineOption)]
        public void Wait(AsyncLock.Key asyncLockKey)
            => asyncLockKey.Wait(this);

        /// <summary>
        /// Release the lock and synchronously wait for a notify signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>, while observing a <see cref="CancelationToken"/>.
        /// The lock will be re-acquired before this method returns.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <returns>
        /// <see langword="true"/> if the lock was re-acquired before the <paramref name="cancelationToken"/> was canceled,
        /// <see langword="false"/> if the lock was re-acquired after the <paramref name="cancelationToken"/> was canceled
        /// </returns>
        [MethodImpl(Internal.InlineOption)]
        public bool TryWait(AsyncLock.Key asyncLockKey, CancelationToken cancelationToken)
            => asyncLockKey.TryWait(this, cancelationToken);

        /// <summary>
        /// Send a signal to a single waiter on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        [MethodImpl(Internal.InlineOption)]
        public void Notify(AsyncLock.Key asyncLockKey)
            => asyncLockKey.Pulse(this);

        /// <summary>
        /// Send a signal to all waiters on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        [MethodImpl(Internal.InlineOption)]
        public void NotifyAll(AsyncLock.Key asyncLockKey)
            => asyncLockKey.PulseAll(this);
    } // class AsyncConditionVariable

#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
} // namespace Proto.Promises.Threading