#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#pragma warning disable CA1507 // Use nameof to express symbol names

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Threading
{
    /// <summary>
    /// An async-compatible manual-reset event.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed class AsyncSemaphore
    {
        // We wrap the impl with another class so that we can lock on it safely.
        private readonly Internal.AsyncSemaphoreInternal _impl;

        /// <summary>
        /// Creates an async-compatible Semaphore, specifying
        /// the initial number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted
        /// concurrently.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="initialCount"/> is less than 0.</exception>
        public AsyncSemaphore(int initialCount) : this(initialCount, int.MaxValue)
        {
        }

        /// <summary>
        /// Creates an async-compatible Semaphore, specifying
        /// the initial and maximum number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted
        /// concurrently.</param>
        /// <param name="maxCount">The maximum number of requests for the semaphore that can be granted
        /// concurrently.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="initialCount"/> is less than 0. -or-
        /// <paramref name="initialCount"/> is greater than <paramref name="maxCount"/>. -or-
        /// <paramref name="maxCount"/> is equal to or less than 0.</exception>
        public AsyncSemaphore(int initialCount, int maxCount)
        {
            if (maxCount <= 0 | initialCount < 0 | initialCount > maxCount)
            {
                throw maxCount <= 0
                    ? new ArgumentOutOfRangeException("maxCount", "maxCount must be greater than 0", Internal.GetFormattedStacktrace(1))
                    : new ArgumentOutOfRangeException("initialCount", "initialCount must be >= 0 and <= maxCount", Internal.GetFormattedStacktrace(1));
            }

            _impl = new Internal.AsyncSemaphoreInternal(initialCount, maxCount);
        }

        /// <summary>
        /// Get the number of times remaining that this <see cref="AsyncSemaphore"/> can be entered concurrently.
        /// </summary>
        /// <remarks>
        /// The initial value of the <see cref="CurrentCount"/> property is set by the call to the <see cref="AsyncSemaphore"/> class constructor.
        /// It is decremented by each call to the <see cref="Wait()"/> or <see cref="WaitAsync()"/> methods, and incremented by each call to the <see cref="Release()"/> method.
        /// </remarks>
        public int CurrentCount
        {
            [MethodImpl(Internal.InlineOption)]
            get { return _impl._currentCount; }
        }

        /// <summary>
        /// Asynchronously wait to enter this <see cref="AsyncSemaphore"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise WaitAsync()
        {
            return _impl.WaitAsync();
        }

        /// <summary>
        /// Asynchronously wait to enter this <see cref="AsyncSemaphore"/>, or for the <paramref name="cancelationToken"/> to be canceled.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <remarks>
        /// The result of the returned <see cref="Promise{T}"/> will be <see langword="true"/> if this is entered before the <paramref name="cancelationToken"/> was canceled, otherwise it will be <see langword="false"/>.
        /// If this is available to be entered, the result will be <see langword="true"/>, even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public Promise<bool> TryWaitAsync(CancelationToken cancelationToken)
        {
            return _impl.TryWaitAsync(cancelationToken);
        }

        /// <summary>
        /// Synchronously wait to enter this <see cref="AsyncSemaphore"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public void Wait()
        {
            _impl.WaitSync();
        }

        /// <summary>
        /// Synchronously wait to enter this <see cref="AsyncSemaphore"/>, or for the <paramref name="cancelationToken"/> to be canceled.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <remarks>
        /// The returned value will be <see langword="true"/> if this is entered before the <paramref name="cancelationToken"/> was canceled, otherwise it will be <see langword="false"/>.
        /// If this is available to be entered, the result will be <see langword="true"/>, even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public bool TryWait(CancelationToken cancelationToken)
        {
            return _impl.TryWait(cancelationToken);
        }

        /// <summary>
        /// Exit this <see cref="AsyncSemaphore"/> once.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public void Release()
        {
            _impl.Release();
        }

        /// <summary>
        /// Exit this <see cref="AsyncSemaphore"/> a specified number of times.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public void Release(int releaseCount)
        {
            if (releaseCount < 1)
            {
                throw new ArgumentOutOfRangeException("releaseCount", "releaseCount cannot be less than 1", Internal.GetFormattedStacktrace(1));
            }

            _impl.Release(releaseCount);
        }

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        // Old IL2CPP runtime crashes if this code exists, even if it is not used. So we only include them in newer build targets that old Unity versions cannot consume.
        // See https://github.com/timcassell/ProtoPromise/issues/169 for details.

        /// <summary>
        /// Asynchronously wait to enter this <see cref="AsyncSemaphore"/>.
        /// The result of the returned <see cref="Promise{T}"/> is a disposable that releases this when disposed, thus treating this <see cref="AsyncSemaphore"/> as a "multi-lock".
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise<Scope> EnterScopeAsync()
        {
            return WaitAsync()
                .Then(this, _this => new Scope(_this));
        }

        /// <summary>
        /// Blocks the current thread until it can enter this <see cref="AsyncSemaphore"/>, and returns a disposable that releases this when disposed, thus treating this <see cref="AsyncSemaphore"/> as a "multi-lock".
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Scope EnterScope()
        {
            Wait();
            return new Scope(this);
        }

        /// <summary>
        /// Simple disposable wrapper around <see cref="Release()"/>. This is used to facilitate the <see langword="using"></see> keyword to treat the <see cref="AsyncSemaphore"/> as a "multi-lock".
        /// </summary>
        public readonly struct Scope : IDisposable
        {
            private readonly AsyncSemaphore _target;

            /// <summary>
            /// Creates a new <see cref="Scope"/> that will release the <paramref name="target"/> when it is disposed.
            /// </summary>
            /// <param name="target">The <see cref="AsyncSemaphore"/> that will be released when this <see cref="Scope"/> is disposed.</param>
            [MethodImpl(Internal.InlineOption)]
            public Scope(AsyncSemaphore target)
            {
                _target = target;
            }

            /// <summary>
            /// Releases the associated <see cref="AsyncSemaphore"/> once.
            /// </summary>
            /// <remarks>
            /// <see cref="Dispose"/> does not do any validation, it is simply a passthrough to <see cref="Release()"/>.
            /// </remarks>
            [MethodImpl(Internal.InlineOption)]
            public void Dispose()
            {
                _target.Release();
            }
        }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    }
}