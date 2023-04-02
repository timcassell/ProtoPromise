#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

#pragma warning disable CA1507 // Use nameof to express symbol names
#pragma warning disable IDE0016 // Use 'throw' expression
#pragma warning disable IDE0031 // Use null propagation
#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises.Threading
{
    /// <summary>
    /// A <see cref="SynchronizationContext"/> used to schedule callbacks to the thread that it was created on.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed class PromiseSynchronizationContext : SynchronizationContext
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class SyncCallback : Internal.HandleablePromiseBase
        {
            ExceptionDispatchInfo _capturedInfo;
            private SendOrPostCallback _callback;
            private object _state;
            private bool _needsPulse;

            private SyncCallback() { }

            [MethodImpl(Internal.InlineOption)]
            private static SyncCallback GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<SyncCallback>();
                return obj == Internal.PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new SyncCallback()
                    : obj.UnsafeAs<SyncCallback>();
            }

            internal static SyncCallback GetOrCreate(SendOrPostCallback callback, object state, bool needsPulse)
            {
                var sc = GetOrCreate();
                sc._next = null;
                sc._callback = callback;
                sc._state = state;
                sc._needsPulse = needsPulse;
                return sc;
            }

            internal void Invoke()
            {
                if (!_needsPulse)
                {
                    InvokeAndDispose();
                    return;
                }

                lock (this) // Normally not safe to lock on `this`, but it's safe here because the class is private and a reference will never be used elsewhere.
                {
                    try
                    {
                        InvokeWithoutDispose();
                    }
                    catch (Exception e)
                    {
                        _capturedInfo = ExceptionDispatchInfo.Capture(e);
                    }
                    finally
                    {
                        Monitor.Pulse(this);
                    }
                }
            }

            private void InvokeWithoutDispose()
            {
                _callback.Invoke(_state);
            }

            internal void Send(PromiseSynchronizationContext parent)
            {
                ExceptionDispatchInfo capturedInfo;
                lock (this)
                {
                    parent._syncLocker.Enter();
                    parent._syncQueue.Enqueue(this);
                    parent._syncLocker.Exit();

                    Monitor.Wait(this);
                    capturedInfo = _capturedInfo;
                }

                Dispose(); // Dispose after invoke.
#if NET_LEGACY
                // Old runtime does not support ExceptionDispatchInfo, so we have to wrap the exception to preserve its stacktrace.
                if (capturedInfo.SourceException != null)
                {
                    throw new Exception("An exception was thrown from the invoked delegate.", capturedInfo.SourceException);
                }
#else
                if (capturedInfo != null)
                {
                    capturedInfo.Throw();
                }
#endif
            }

            private void InvokeAndDispose()
            {
                SendOrPostCallback cb = _callback;
                object state = _state;
                Dispose();
                cb.Invoke(state);
            }

            private void Dispose()
            {
                _capturedInfo = default(ExceptionDispatchInfo);
                _callback = null;
                _state = null;
                Internal.ObjectPool.MaybeRepool(this);
            }
        }

        private readonly Thread _thread;
        // These must not be readonly.
        private Internal.ValueLinkedQueue<Internal.HandleablePromiseBase> _syncQueue = new Internal.ValueLinkedQueue<Internal.HandleablePromiseBase>();
        private Internal.SpinLocker _syncLocker;
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
                throw new ArgumentNullException("runThread", "runThread may not be null", Internal.GetFormattedStacktrace(1));
            }
            _thread = runThread;
        }

        /// <summary>
        /// Create copy.
        /// </summary>
        /// <returns>this</returns>
        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        /// <summary>
        /// Schedule the delegate to execute on this context with the given state asynchronously, without waiting for it to complete.
        /// </summary>
        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null)
            {
                throw new System.ArgumentNullException("d", "SendOrPostCallback may not be null.");
            }

            SyncCallback syncCallback = SyncCallback.GetOrCreate(d, state, false);
            _syncLocker.Enter();
            _syncQueue.Enqueue(syncCallback);
            _syncLocker.Exit();
        }

        /// <summary>
        /// Schedule the delegate to execute on this context with the given state, and wait for it to complete.
        /// </summary>
        public override void Send(SendOrPostCallback d, object state)
        {
            if (d == null)
            {
                throw new System.ArgumentNullException("d", "SendOrPostCallback may not be null.");
            }

            if (Thread.CurrentThread == _thread)
            {
                d.Invoke(state);
                return;
            }

            SyncCallback.GetOrCreate(d, state, true).Send(this);
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
                    var syncStack = _syncQueue.MoveElementsToStack();
                    _syncLocker.Exit();

                    if (syncStack.IsEmpty)
                    {
                        break;
                    }

                    // Catch all exceptions and continue executing callbacks until all are exhausted, then if there are any, throw all exceptions wrapped in AggregateException.
                    List<Exception> exceptions = null;
                    do
                    {
                        try
                        {
                            syncStack.Pop().UnsafeAs<SyncCallback>().Invoke();
                        }
                        catch (Exception e)
                        {
                            if (exceptions == null)
                            {
                                exceptions = new List<Exception>();
                            }
                            exceptions.Add(e);
                        }
                    } while (syncStack.IsNotEmpty);

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