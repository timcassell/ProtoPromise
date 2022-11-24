#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CA1507 // Use nameof to express symbol names
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
#if !NET_LEGACY
            System.Runtime.ExceptionServices.ExceptionDispatchInfo _capturedInfo;
#endif
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
#if !NET_LEGACY
                    catch (Exception e)
                    {
                        _capturedInfo = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
                    }
#endif
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
#if !NET_LEGACY
                System.Runtime.ExceptionServices.ExceptionDispatchInfo capturedInfo;
#endif
                lock (this)
                {
                    parent._syncLocker.Enter();
                    parent._syncQueue.Enqueue(this);
                    parent._syncLocker.Exit();

                    Monitor.Wait(this);
#if !NET_LEGACY
                    capturedInfo = _capturedInfo;
#endif
                }

                Dispose(); // Dispose after invoke.
#if !NET_LEGACY
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
#if !NET_LEGACY
                _capturedInfo = null;
#endif
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
        public PromiseSynchronizationContext()
        {
            _thread = Thread.CurrentThread;
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
        /// Execute all callbacks that have been scheduled to run on this thread.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">If this is called on a different thread than this was created on, or if this is called recursively.</exception>
        /// <exception cref="AggregateException">If one or more callbacks throw an exception, they will be wrapped and rethrown as <see cref="AggregateException"/>.</exception>
        public void Execute()
        {
            if (Thread.CurrentThread != _thread | _isInvoking)
            {
                throw new System.InvalidOperationException(_isInvoking
                    ? "Execute invoked recursively. This is not supported."
                    : "Execute may only be called from the thread on which the PromiseSynchronizationContext was created.");
            }

            var currentContextInternal = Internal.ts_currentContext;
            var currentContextGlobal = Current;
            try
            {
                Internal.ts_currentContext = this;
                SetSynchronizationContext(this);
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
                Internal.ts_currentContext = currentContextInternal;
                SetSynchronizationContext(currentContextGlobal);
            }
        }
    }
}