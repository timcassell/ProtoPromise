﻿#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

using System.Diagnostics;

namespace Proto.Promises.Threading
{
    /// <summary>
    /// Async-compatible Monitor.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static class AsyncMonitor
    {
        /// <summary>
        /// Asynchronously acquire the lock on the specified <see cref="AsyncLock"/>. Returns a <see cref="Promise{T}"/> that will be resolved when the lock has been acquired.
        /// The result of the promise is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, the returned <see cref="Promise{T}"/> will be canceled.</param>
        public static Promise<AsyncLock.Key> EnterAsync(AsyncLock asyncLock, CancelationToken cancelationToken = default(CancelationToken))
        {
            return asyncLock.LockAsync(cancelationToken);
        }

        /// <summary>
        /// Synchronously acquire the lock on the specified <see cref="AsyncLock"/>. Returns the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the lock. If the token is canceled before the lock has been acquired, <see cref="CanceledException"/> will be thrown.</param>
        public static AsyncLock.Key Enter(AsyncLock asyncLock, CancelationToken cancelationToken = default(CancelationToken))
        {
            return asyncLock.Lock(cancelationToken);
        }

        /// <summary>
        /// Synchronously try to acquire the lock on the specified <see cref="AsyncLock"/>. If successful, <paramref name="key"/> is the key that will release the lock when it is disposed.
        /// </summary>
        /// <param name="asyncLock">The async lock instance that is being entered.</param>
        /// <param name="key">If successful, the key that will release the lock when it is disposed.</param>
        /// <returns>True if the lock was acquired, false otherwise.</returns>
        public static bool TryEnter(AsyncLock asyncLock, out AsyncLock.Key key)
        {
            return asyncLock.TryEnter(out key);
        }

        /// <summary>
        /// Release the lock on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// </summary>
        public static void Exit(AsyncLock.Key asyncLockKey)
        {
            asyncLockKey.Dispose();
        }

        /// <summary>
        /// Release the lock and asynchronously wait for a pulse signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// The lock will be re-acquired before the returned <see cref="Promise{T}"/> is resolved.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> that will be resolved when the lock is re-acquired.
        /// Its result will be true if the lock was re-acquired before the <paramref name="cancelationToken"/> was canceled,
        /// false if the lock was re-acquired after the <paramref name="cancelationToken"/> was canceled
        /// </returns>
        public static Promise<bool> WaitAsync(AsyncLock.Key asyncLockKey, CancelationToken cancelationToken = default(CancelationToken))
        {
            return asyncLockKey.WaitAsync(cancelationToken);
        }

        /// <summary>
        /// Release the lock and synchronously wait for a pulse signal on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// The lock will be re-acquired before this method returns.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <returns>
        /// true if the lock was re-acquired before the <paramref name="cancelationToken"/> was canceled,
        /// false if the lock was re-acquired after the <paramref name="cancelationToken"/> was canceled
        /// </returns>
        public static bool Wait(AsyncLock.Key asyncLockKey, CancelationToken cancelationToken = default(CancelationToken))
        {
            return asyncLockKey.Wait(cancelationToken);
        }

        /// <summary>
        /// Send a signal to a single waiter on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        public static void Pulse(AsyncLock.Key asyncLockKey)
        {
            asyncLockKey.Pulse();
        }

        /// <summary>
        /// Send a signal to all waiters on the <see cref="AsyncLock"/> associated with the <paramref name="asyncLockKey"/>.
        /// </summary>
        /// <param name="asyncLockKey">The key to the <see cref="AsyncLock"/> that is currently acquired.</param>
        public static void PulseAll(AsyncLock.Key asyncLockKey)
        {
            asyncLockKey.PulseAll();
        }
    } // class AsyncMonitor
} // namespace Proto.Promises