#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Threading;

#pragma warning disable IDE0016 // Use 'throw' expression
#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises.Threading
{
    /// <summary>
    /// A <see cref="SynchronizationContext"/> used to schedule callbacks to the thread with which it is associated.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed class PromiseSynchronizationContext : SynchronizationContext
    {
        // ManualSynchronizationContextCore is not associated with a thread, we do it in this type.
        private readonly Thread _thread;
        private ManualSynchronizationContextCore _core;

        /// <summary>
        /// Create a new <see cref="PromiseSynchronizationContext"/> affiliated with the current thread.
        /// </summary>
        public PromiseSynchronizationContext() : this(Thread.CurrentThread) { }

        /// <summary>
        /// Create a new <see cref="PromiseSynchronizationContext"/> affiliated with the <paramref name="runThread"/>.
        /// </summary>
        public PromiseSynchronizationContext(Thread runThread)
        {
            if (runThread == null)
            {
                throw new ArgumentNullException(nameof(runThread), "runThread may not be null", Internal.GetFormattedStacktrace(1));
            }
            _thread = runThread;
            // Start with a modest initial capacity. This will grow in Post() if necessary.
            _core = new ManualSynchronizationContextCore(8);
        }

        /// <summary>
        /// Create copy.
        /// </summary>
        /// <returns>this</returns>
        public override SynchronizationContext CreateCopy() => this;

        /// <summary>
        /// Schedule the delegate to execute on this context with the given state asynchronously, without waiting for it to complete.
        /// </summary>
        public override void Post(SendOrPostCallback d, object state)
            => _core.Post(d, state);

        /// <summary>
        /// Schedule the delegate to execute on this context with the given state, and wait for it to complete.
        /// </summary>
        public override void Send(SendOrPostCallback d, object state)
        {
            if (Thread.CurrentThread != _thread)
            {
                // The callback is checked for null in core, so we don't need to do it here.
                _core.Send(d, state);
                return;
            }

            if (d == null)
            {
                throw new System.ArgumentNullException(nameof(d), "SendOrPostCallback may not be null.");
            }
            d.Invoke(state);
        }

        /// <summary>
        /// Invoke all callbacks that were scheduled to run on this context,
        /// and all callbacks that are scheduled to run on this context while this is executing,
        /// exhaustively, until no more callbacks remain.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">If this is called on a different thread than this context is affiliated, or if this is called recursively.</exception>
        /// <exception cref="AggregateException">One or more callbacks threw an exception.</exception>
        public void Execute()
            => Execute(true);

        /// <summary>
        /// Invoke all callbacks that were scheduled to run on this context, exhaustively or non-exhaustively according to <paramref name="exhaustive"/>.
        /// </summary>
        /// <param name="exhaustive">
        /// If <see langword="true"/>, all callbacks that are scheduled to run on this context while this is executing will be invoked exhaustively, until no more callbacks remain;
        /// otherwise, any callbacks that are scheduled to run on this context while this is executing will not be invoked until the next time this is called.
        /// </param>
        /// <exception cref="System.InvalidOperationException">If this is called on a different thread than this context is affiliated, or if this is called recursively.</exception>
        /// <exception cref="AggregateException">One or more callbacks threw an exception.</exception>
        public void Execute(bool exhaustive)
        {
            if (Thread.CurrentThread != _thread)
            {
                throw new System.InvalidOperationException($"{nameof(Execute)} may only be called from the thread on which the {nameof(PromiseSynchronizationContext)} is affiliated.");
            }

            if (exhaustive)
            {
                _core.ExecuteExhaustive();
            }
            else
            {
                _core.ExecuteNonExhaustive();
            }
        }
    }
}