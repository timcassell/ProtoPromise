#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable CA1507 // Use nameof to express symbol names

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Threading
{
    /// <summary>
    /// An async-compatible countdown event.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed class AsyncCountdownEvent
    {
        // We wrap the impl with another class so that we can lock on it safely.
        private readonly Internal.AsyncCountdownEventInternal _impl;

        /// <summary>
        /// Creates an async-compatible countdown event.
        /// </summary>
        /// <param name="initialCount">The number of signals initially required to set the <see cref="AsyncCountdownEvent"/>.</param>
        public AsyncCountdownEvent(int initialCount)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount", "initialCount must be greater than or equal to 0.", Internal.GetFormattedStacktrace(1));
            }

            _impl = new Internal.AsyncCountdownEventInternal(initialCount);
        }

        /// <summary>
        /// Gets the number of remaining signals required to set the event.
        /// </summary>
        public int CurrentCount
        {
            [MethodImpl(Internal.InlineOption)]
            get { return _impl._currentCount; }
        }

        /// <summary>
        /// Gets the numbers of signals initially required to set the event.
        /// </summary>
        public int InitialCount
        {
            [MethodImpl(Internal.InlineOption)]
            get { return _impl._initialCount; }
        }

        /// <summary>
        /// Increments <see cref="CurrentCount"/> by one.
        /// </summary>
        /// <exception cref="InvalidOperationException">The current instance is already set, or the <see cref="CurrentCount"/> is equal to or greater than <see cref="int.MaxValue"/>.</exception>
        [MethodImpl(Internal.InlineOption)]
        public void AddCount()
        {
            if (!TryAddCount())
            {
                throw new InvalidOperationException("AsyncCountdownEvent.AddCount: The AsyncCountdownEvent is already set.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Increments <see cref="CurrentCount"/> by a specified value.
        /// </summary>
        /// <param name="signalCount">The value by which to increase <see cref="CurrentCount"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="signalCount"/> is less than or equal to 0.</exception>
        /// <exception cref="InvalidOperationException">
        /// The current instance is already set, or <see cref="CurrentCount"/> + <paramref name="signalCount"/> is equal to or greater than <see cref="int.MaxValue"/>.
        /// </exception>
        [MethodImpl(Internal.InlineOption)]
        public void AddCount(int signalCount)
        {
            if (!TryAddCount(signalCount))
            {
                throw new InvalidOperationException("AsyncCountdownEvent.AddCount: The AsyncCountdownEvent is already set.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Attempts to increment <see cref="CurrentCount"/> by one.
        /// </summary>
        /// <returns><see langword="true"/> if the increment succeeded; otherwise, <see langword="false"/>. If <see cref="CurrentCount"/> is already 0, this will return <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException"><see cref="CurrentCount"/> is equal to or greater than <see cref="int.MaxValue"/>.</exception>
        [MethodImpl(Internal.InlineOption)]
        public bool TryAddCount()
        {
            return _impl.TryAddCount(1);
        }

        /// <summary>
        /// Attempts to increment <see cref="CurrentCount"/> by a specified value.
        /// </summary>
        /// <param name="signalCount">The value by which to increase <see cref="CurrentCount"/>.</param>
        /// <returns><see langword="true"/> if the increment succeeded; otherwise, <see langword="false"/>. If <see cref="CurrentCount"/> is already 0, this will return <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="signalCount"/> is less than or equal to 0.</exception>
        /// <exception cref="InvalidOperationException"><see cref="CurrentCount"/> + <paramref name="signalCount"/> is equal to or greater than <see cref="int.MaxValue"/>.</exception>
        [MethodImpl(Internal.InlineOption)]
        public bool TryAddCount(int signalCount)
        {
            return _impl.TryAddCount(signalCount);
        }

        /// <summary>
        /// Resets the <see cref="CurrentCount"/> to the value of <see cref="InitialCount"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public void Reset()
        {
            _impl.Reset();
        }

        /// <summary>
        /// Resets the <see cref="InitialCount"/> and <see cref="CurrentCount"/> to a specified value.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public void Reset(int count)
        {
            _impl.Reset(count);
        }

        /// <summary>
        /// Registers a signal with the <see cref="AsyncCountdownEvent"/>, decrementing the value of <see cref="CurrentCount"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the signal caused the count to reach zero and the event was set; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">The current instance is already set.</exception>
        [MethodImpl(Internal.InlineOption)]
        public bool Signal()
        {
            return _impl.Signal(1);
        }

        /// <summary>
        /// Registers multiple signals with the <see cref="AsyncCountdownEvent"/>, decrementing the value of <see cref="CurrentCount"/> by the specified amount.
        /// </summary>
        /// <param name="signalCount">The number of signals to register.</param>
        /// <returns><see langword="true"/> if the signals caused the count to reach zero and the event was set; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="signalCount"/> is less than 1.</exception>
        /// <exception cref="InvalidOperationException">The current instance is already set, or <paramref name="signalCount"/> is greater than <see cref="CurrentCount"/>.</exception>
        [MethodImpl(Internal.InlineOption)]
        public bool Signal(int signalCount)
        {
            return _impl.Signal(signalCount);
        }

        /// <summary>
        /// Asynchronously wait for this event to be set.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Promise WaitAsync()
        {
            return _impl.WaitAsync();
        }

        /// <summary>
        /// Asynchronously wait for this event to be set, or for the <paramref name="cancelationToken"/> to be canceled.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <remarks>
        /// The result of the returned <see cref="Promise{T}"/> will be <see langword="true"/> if this is set before the <paramref name="cancelationToken"/> was canceled, otherwise it will be <see langword="false"/>.
        /// If this is already set, the result will be <see langword="true"/>, even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public Promise<bool> TryWaitAsync(CancelationToken cancelationToken)
        {
            return _impl.TryWaitAsync(cancelationToken);
        }

        /// <summary>
        /// Synchronously wait for this event to be set.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public void Wait()
        {
            _impl.Wait();
        }

        /// <summary>
        /// Synchronously wait for this event to be set, or for the <paramref name="cancelationToken"/> to be canceled.
        /// </summary>
        /// <param name="cancelationToken">The <see cref="CancelationToken"/> used to cancel the wait.</param>
        /// <remarks>
        /// The returned value will be <see langword="true"/> if this is set before the <paramref name="cancelationToken"/> was canceled, otherwise it will be <see langword="false"/>.
        /// If this is already set, the result will be <see langword="true"/>, even if the <paramref name="cancelationToken"/> is already canceled.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public bool TryWait(CancelationToken cancelationToken)
        {
            return _impl.TryWait(cancelationToken);
        }

        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="AsyncManualResetEvent"/> to be awaited.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Async.CompilerServices.PromiseAwaiterVoid GetAwaiter()
        {
            return WaitAsync().GetAwaiter();
        }
    }
}