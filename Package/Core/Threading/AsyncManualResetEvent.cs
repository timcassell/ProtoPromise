#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.ComponentModel;
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
    public sealed class AsyncManualResetEvent
    {
        // We wrap the impl with another class so that we can lock on it safely.
        private readonly Internal.AsyncManualResetEventInternal _impl;

        /// <summary>
        /// Creates an async-compatible manual-reset event.
        /// </summary>
        /// <param name="initialState">Whether the manual-reset event is initially set or unset.</param>
        public AsyncManualResetEvent(bool initialState)
        {
            _impl = new Internal.AsyncManualResetEventInternal(initialState);
        }

        /// <summary>
        /// Creates an async-compatible manual-reset event that is initially unset.
        /// </summary>
        public AsyncManualResetEvent() : this(false)
        {
        }

        /// <summary>
        /// Whether this event is currently set.
        /// </summary>
        public bool IsSet
        {
            [MethodImpl(Internal.InlineOption)]
            get { return _impl._isSet; }
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
        /// Sets this event, completing every wait.
        /// </summary>
        /// <remarks>
        /// If this event is already set, this does nothing.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public void Set()
        {
            _impl.Set();
        }

        /// <summary>
        /// Resets this event.
        /// </summary>
        /// <remarks>
        /// If this event is already reset, this does nothing.
        /// </remarks>
        [MethodImpl(Internal.InlineOption)]
        public void Reset()
        {
            _impl.Reset();
        }

        /// <summary>
        /// Asynchronous infrastructure support. This method permits instances of <see cref="AsyncManualResetEvent"/> to be awaited.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CompilerServices.PromiseAwaiterVoid GetAwaiter()
        {
            return WaitAsync().GetAwaiter();
        }
    }
}