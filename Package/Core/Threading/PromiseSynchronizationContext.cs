#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
        // Send callbacks are implemented as a linked-list, because we have to make sure the ExceptionDispatchInfo is still valid by the time the original thread reads it.
        // Post callbacks are implemented as a contiguous list for fast add and invoke, with minimal memory.
        // Post is also expected to be used more frequently than Send, so we want it to be as efficient as possible, rather than sharing an implementation.

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private readonly struct PostCallback
        {
            internal readonly SendOrPostCallback _callback;
            internal readonly object _state;

            [MethodImpl(Internal.InlineOption)]
            internal PostCallback(SendOrPostCallback callback, object state)
            {
                _callback = callback;
                _state = state;
            }

            [MethodImpl(Internal.InlineOption)]
            internal void Invoke()
                => _callback.Invoke(_state);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class SendCallback : Internal.HandleablePromiseBase, Internal.ILinked<SendCallback>
        {
            SendCallback Internal.ILinked<SendCallback>.Next
            {
                get => _next.UnsafeAs<SendCallback>();
                set => _next = value;
            }

            private PostCallback _callback;
            private ExceptionDispatchInfo _capturedInfo;
            private readonly AutoResetEvent _callbackCompletedEvent = new AutoResetEvent(false);

            private SendCallback() { }

            [MethodImpl(Internal.InlineOption)]
            private static SendCallback GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<SendCallback>();
                return obj == Internal.PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new SendCallback()
                    : obj.UnsafeAs<SendCallback>();
            }

            internal static SendCallback GetOrCreate(SendOrPostCallback callback, object state)
            {
                var sc = GetOrCreate();
                sc._next = null;
                sc._callback = new PostCallback(callback, state);
                return sc;
            }

            internal void Invoke()
            {
                try
                {
                    _callback.Invoke();
                }
                catch (Exception e)
                {
                    _capturedInfo = ExceptionDispatchInfo.Capture(e);
                }
                finally
                {
                    _callbackCompletedEvent.Set();
                }
            }

            internal void Send(PromiseSynchronizationContext parent)
            {
                parent._syncLocker.Enter();
                parent._sendQueue.Enqueue(this);
                parent._syncLocker.Exit();

                _callbackCompletedEvent.WaitOne();
                var capturedInfo = _capturedInfo;

                Dispose(); // Dispose after invoke.
                capturedInfo?.Throw();
            }

            private void Dispose()
            {
                _callback = default;
                _capturedInfo = default;
                Internal.ObjectPool.MaybeRepool(this);
            }
        }

        private readonly Thread _thread;
        // We swap these when Execute() is called.
        private PostCallback[] _postQueue;
        private PostCallback[] _executing;
        // These must not be readonly.
        private Internal.ValueLinkedQueue<SendCallback> _sendQueue = new Internal.ValueLinkedQueue<SendCallback>();
        private Internal.SpinLocker _syncLocker;
        private int _postCount;
        private bool _isInvoking;

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
            _postQueue = new PostCallback[8];
            _executing = new PostCallback[8];
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
        {
            if (d == null)
            {
                throw new System.ArgumentNullException(nameof(d), "SendOrPostCallback may not be null.");
            }

            _syncLocker.Enter();
            int count = _postCount;
            if (count >= _postQueue.Length)
            {
                Array.Resize(ref _postQueue, count * 2);
            }
            _postQueue[count] = new PostCallback(d, state);
            _postCount = count + 1;
            _syncLocker.Exit();
        }

        /// <summary>
        /// Schedule the delegate to execute on this context with the given state, and wait for it to complete.
        /// </summary>
        public override void Send(SendOrPostCallback d, object state)
        {
            if (d == null)
            {
                throw new System.ArgumentNullException(nameof(d), "SendOrPostCallback may not be null.");
            }

            if (Thread.CurrentThread == _thread)
            {
                d.Invoke(state);
                return;
            }

            SendCallback.GetOrCreate(d, state).Send(this);
        }

        /// <summary>
        /// Execute all callbacks that have been scheduled to run on this context.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">If this is called on a different thread than this was created on, or if this is called recursively.</exception>
        /// <exception cref="AggregateException">If one or more callbacks throw an exception, they will be wrapped and rethrown as <see cref="AggregateException"/>.</exception>
        public void Execute()
        {
            if (Thread.CurrentThread != _thread | _isInvoking)
            {
                throw new System.InvalidOperationException(Thread.CurrentThread != _thread
                    ? "Execute may only be called from the thread on which the PromiseSynchronizationContext is affiliated."
                    : "Execute invoked recursively. This is not supported.");
            }

            try
            {
                _isInvoking = true;

                while (true)
                {
                    _syncLocker.Enter();
                    var sendStack = _sendQueue.MoveElementsToStack();
                    var postQueue = _postQueue;
                    int postCount = _postCount;
                    _postCount = 0;
                    // Swap the executing and pending arrays.
                    _postQueue = _executing;
                    _executing = postQueue;
                    _syncLocker.Exit();

                    if (sendStack.IsEmpty & postCount == 0)
                    {
                        break;
                    }

                    // Execute Send callbacks first so that their waiting threads may continue sooner.
                    // We don't need to catch exceptions here because it's already handled in the SendCallback.Invoke().
                    while (sendStack.IsNotEmpty)
                    {
                        sendStack.Pop().Invoke();
                    }

                    // Catch all exceptions and continue executing callbacks until all are exhausted, then if there are any, throw all exceptions wrapped in AggregateException.
                    List<Exception> exceptions = null;
                    for (int i = 0; i < postCount; ++i)
                    {
                        ref var callback = ref postQueue[i];
                        try
                        {
                            callback.Invoke();
                        }
                        catch (Exception e)
                        {
                            Internal.RecordException(e, ref exceptions);
                        }
                        callback = default;
                    }

                    if (exceptions != null)
                    {
                        throw new AggregateException(exceptions);
                    }
                }
            }
            finally
            {
                _isInvoking = false;
            }
        }
    }
}