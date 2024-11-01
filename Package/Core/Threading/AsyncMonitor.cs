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
    // Old IL2CPP runtime crashes if this code exists, even if it is not used. So we only include them in newer build targets that old Unity versions cannot consume.
    // See https://github.com/timcassell/ProtoPromise/issues/169 for details.

    /// <summary>
    /// Async-compatible Monitor.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static class AsyncMonitor
    {
        private static readonly ConditionalWeakTable<AsyncLock, AsyncConditionVariable> s_condVarTable = new ConditionalWeakTable<AsyncLock, AsyncConditionVariable>();

        /// <summary>
        /// Asynchronously acquire the lock on the specified <see cref="AsyncLock"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Promise<AsyncLock.Key> EnterAsync(AsyncLock asyncLock)
            => asyncLock.LockAsync();

        /// <summary>
        /// Asynchronously acquire the lock on the specified <see cref="AsyncLock"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="continuationOptions">The options used to configure the continuation behavior of the returned <see cref="Promise{T}"/>.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Promise<AsyncLock.Key> EnterAsync(AsyncLock asyncLock, ContinuationOptions continuationOptions)
            => asyncLock.LockAsync(continuationOptions);

        /// <summary>
        /// Asynchronously acquire the lock on the specified <see cref="AsyncLock"/>, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Promise<AsyncLock.Key> EnterAsync(AsyncLock asyncLock, CancelationToken cancelationToken)
            => asyncLock.LockAsync(cancelationToken);

        /// <summary>
        /// Asynchronously acquire the lock on the specified <see cref="AsyncLock"/>, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
        /// <param name="continuationOptions">The options used to configure the continuation behavior of the returned <see cref="Promise{T}"/>.</param>
        [MethodImpl(Internal.InlineOption)]
        public static Promise<AsyncLock.Key> EnterAsync(AsyncLock asyncLock, CancelationToken cancelationToken, ContinuationOptions continuationOptions)
            => asyncLock.LockAsync(cancelationToken, continuationOptions);

        /// <summary>
        /// Synchronously acquire the lock on the specified <see cref="AsyncLock"/>.
        /// Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        [MethodImpl(Internal.InlineOption)]
        public static AsyncLock.Key Enter(AsyncLock asyncLock)
            => asyncLock.Lock();

        /// <summary>
        /// Synchronously acquire the lock on the specified <see cref="AsyncLock"/>, while observing a <see cref="CancelationToken"/>.
        /// Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, a <see cref="CanceledException"/> will be thrown.</param>
        [MethodImpl(Internal.InlineOption)]
        public static AsyncLock.Key Enter(AsyncLock asyncLock, CancelationToken cancelationToken)
            => asyncLock.Lock(cancelationToken);

        /// <summary>
        /// Synchronously try to acquire the lock on the specified <see cref="AsyncLock"/>. If successful, <paramref name="key"/> is the key that will release the lock when it is disposed.
        /// This function does not wait, and returns immediately.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="key">If successful, the key that will release the lock when it is disposed.</param>
        /// <returns><see langword="true"/> if the lock was acquired, <see langword="false"/> otherwise.</returns>
        [MethodImpl(Internal.InlineOption)]
        public static bool TryEnter(AsyncLock asyncLock, out AsyncLock.Key key)
            => asyncLock.TryEnterImpl(out key);

        /// <summary>
        /// Asynchronously try to acquire the lock on the specified <see cref="AsyncLock"/>, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired, or the <paramref name="cancelationToken"/> has been canceled, with the success state and key.
        /// If successful, the key will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="cancelationToken">
        /// The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the success state of the returned <see cref="Promise{T}"/> will be <see langword="false"/>.
        /// </param>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the lock is available, the result will be (<see langword="true"/>, key), even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public static Promise<(bool didEnter, AsyncLock.Key key)> TryEnterAsync(AsyncLock asyncLock, CancelationToken cancelationToken)
            => asyncLock.TryEnterAsyncImpl(cancelationToken, ContinuationOptions.CapturedContext);

        /// <summary>
        /// Asynchronously try to acquire the lock on the specified <see cref="AsyncLock"/>, while observing a <see cref="CancelationToken"/>.
        /// Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired, or the <paramref name="cancelationToken"/> has been canceled, with the success state and key.
        /// If successful, the key will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="cancelationToken">
        /// The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the success state of the returned <see cref="Promise{T}"/> will be <see langword="false"/>.
        /// </param>
        /// <param name="continuationOptions">The options used to configure the continuation behavior of the returned <see cref="Promise{T}"/>.</param>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the lock is available, the result will be (<see langword="true"/>, key), even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public static Promise<(bool didEnter, AsyncLock.Key key)> TryEnterAsync(AsyncLock asyncLock, CancelationToken cancelationToken, ContinuationOptions continuationOptions)
            => asyncLock.TryEnterAsyncImpl(cancelationToken, continuationOptions.GetValidated());

        /// <summary>
        /// Synchronously try to acquire the lock on the specified <see cref="AsyncLock"/>, while observing a <see cref="CancelationToken"/>.
        /// If successful, <paramref name="key"/> is the key that will release the lock when it is disposed.
        /// This function does not return until the lock has been acquired, or the <paramref name="cancelationToken"/> has been canceled.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="key">If successful, the key that will release the lock when it is disposed.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, this will return <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if the lock was acquired before the <paramref name="cancelationToken"/> was canceled, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// This first tries to take the lock before checking the <paramref name="cancelationToken"/>>.
        /// If the lock is available, this will return <see langword="true"/>, even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public static bool TryEnter(AsyncLock asyncLock, out AsyncLock.Key key, CancelationToken cancelationToken)
            => asyncLock.TryEnterImpl(out key, cancelationToken);

        /// <summary>
        /// Release the lock on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static void Exit(AsyncLock.Key asyncLockKey)
            => asyncLockKey.Dispose();

        /// <summary>
        /// Release the lock and asynchronously wait for a pulse signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// The lock will be re-acquired before the returned <see cref="Promise{T}"/> is resolved.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <returns>
        /// A <see cref="Promise"/> that will be resolved when the lock is re-acquired.
        /// </returns>
        public static Promise WaitAsync(AsyncLock.Key asyncLockKey)
            => GetConditionVariable(asyncLockKey).WaitAsync(asyncLockKey);

        /// <summary>
        /// Release the lock and asynchronously wait for a pulse signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// The lock will be re-acquired before the returned <see cref="Promise{T}"/> is resolved.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <param name="continuationOptions">The options used to configure the continuation behavior of the returned <see cref="Promise{T}"/>.</param>
        /// <returns>
        /// A <see cref="Promise"/> that will be resolved when the lock is re-acquired.
        /// </returns>
        public static Promise WaitAsync(AsyncLock.Key asyncLockKey, ContinuationOptions continuationOptions)
            => GetConditionVariable(asyncLockKey).WaitAsync(asyncLockKey, continuationOptions);

        /// <summary>
        /// Release the lock and asynchronously wait for a pulse signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>, while observing a <see cref="CancelationToken"/>.
        /// The lock will be re-acquired before the returned <see cref="Promise{T}"/> is resolved.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> that will be resolved when the lock is re-acquired.
        /// Its result will be <see langword="true"/> if the lock was re-acquired before the <paramref name="cancelationToken"/> was canceled,
        /// <see langword="false"/> if the lock was re-acquired after the <paramref name="cancelationToken"/> was canceled
        /// </returns>
        public static Promise<bool> TryWaitAsync(AsyncLock.Key asyncLockKey, CancelationToken cancelationToken)
            => GetConditionVariable(asyncLockKey).TryWaitAsync(asyncLockKey, cancelationToken);

        /// <summary>
        /// Release the lock and asynchronously wait for a pulse signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>, while observing a <see cref="CancelationToken"/>.
        /// The lock will be re-acquired before the returned <see cref="Promise{T}"/> is resolved.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <param name="continuationOptions">The options used to configure the continuation behavior of the returned <see cref="Promise{T}"/>.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> that will be resolved when the lock is re-acquired.
        /// Its result will be <see langword="true"/> if the lock was re-acquired before the <paramref name="cancelationToken"/> was canceled,
        /// <see langword="false"/> if the lock was re-acquired after the <paramref name="cancelationToken"/> was canceled
        /// </returns>
        public static Promise<bool> TryWaitAsync(AsyncLock.Key asyncLockKey, CancelationToken cancelationToken, ContinuationOptions continuationOptions)
            => GetConditionVariable(asyncLockKey).TryWaitAsync(asyncLockKey, cancelationToken, continuationOptions);

        /// <summary>
        /// Release the lock and synchronously wait for a pulse signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// The lock will be re-acquired before this method returns.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        public static void Wait(AsyncLock.Key asyncLockKey)
            => GetConditionVariable(asyncLockKey).Wait(asyncLockKey);

        /// <summary>
        /// Release the lock and synchronously wait for a pulse signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>, while observing a <see cref="CancelationToken"/>.
        /// The lock will be re-acquired before this method returns.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <returns>
        /// <see langword="true"/> if the lock was re-acquired before the <paramref name="cancelationToken"/> was canceled,
        /// <see langword="false"/> if the lock was re-acquired after the <paramref name="cancelationToken"/> was canceled
        /// </returns>
        public static bool TryWait(AsyncLock.Key asyncLockKey, CancelationToken cancelationToken)
            => GetConditionVariable(asyncLockKey).TryWait(asyncLockKey, cancelationToken);

        /// <summary>
        /// Send a signal to a single waiter on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        public static void Pulse(AsyncLock.Key asyncLockKey)
            => GetConditionVariable(asyncLockKey).Notify(asyncLockKey);

        /// <summary>
        /// Send a signal to all waiters on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        public static void PulseAll(AsyncLock.Key asyncLockKey)
            => GetConditionVariable(asyncLockKey).NotifyAll(asyncLockKey);

        private static AsyncConditionVariable GetConditionVariable(AsyncLock.Key asyncLockKey)
        {
            var impl = asyncLockKey._owner;
            if (impl == null)
            {
                AsyncLock.ThrowInvalidKey(2);
            }
            return s_condVarTable.GetOrCreateValue(impl);
        }
    } // class AsyncMonitor

#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
} // namespace Proto.Promises.Threading