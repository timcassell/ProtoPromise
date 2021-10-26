using System.Diagnostics;
using System.Threading;

namespace Proto.Promises.Threading
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public sealed class PromiseSynchronizationContext : SynchronizationContext
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        private sealed class SyncCallback : Internal.ILinked<SyncCallback>
        {
            SyncCallback Internal.ILinked<SyncCallback>.Next { get; set; }

            private SendOrPostCallback _callback;
            private object _state;
            private bool _needsPulse;

            private SyncCallback() { }

            internal static SyncCallback GetOrCreate(SendOrPostCallback callback, object state, bool needsPulse)
            {
                var sc = Internal.ObjectPool<SyncCallback>.TryTake<SyncCallback>()
                    ?? new SyncCallback();
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
                    InvokeWithoutDispose();
                    Monitor.Pulse(this);
                }
                Dispose(); // Dispose after pulse in case this object is re-used.
            }

            private void InvokeWithoutDispose()
            {
                _callback.Invoke(_state);
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
                _callback = null;
                _state = null;
                Internal.ObjectPool<SyncCallback>.MaybeRepool(this);
            }
        }

        private readonly Thread _thread;
        // These must not be readonly.
        private Internal.ValueLinkedQueue<SyncCallback> _syncQueue = new Internal.ValueLinkedQueue<SyncCallback>();
        private Internal.SpinLocker _syncLocker;

        public PromiseSynchronizationContext()
        {
            _thread = Thread.CurrentThread;
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

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

            SyncCallback syncCallback = SyncCallback.GetOrCreate(d, state, true);
            lock (syncCallback)
            {
                _syncLocker.Enter();
                _syncQueue.Enqueue(syncCallback);
                _syncLocker.Exit();

                Monitor.Wait(syncCallback);
            }
        }

        public void Execute()
        {
            if (Thread.CurrentThread != _thread)
            {
                throw new System.InvalidOperationException("Execute may only be called from the thread on which the PromiseSynchronizationContext was created.");
            }

            while (true)
            {
                _syncLocker.Enter();
                var syncStack = _syncQueue.MoveElementsToStack();
                _syncLocker.Exit();

                if (syncStack.IsEmpty)
                {
                    break;
                }
                do
                {
                    syncStack.Pop().Invoke();
                } while (syncStack.IsNotEmpty);
            }
        }
    }
}