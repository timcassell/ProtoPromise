using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable IDE0251 // Make member 'readonly'

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct SingleConsumerAsyncQueueInternal<T>
        {
            // TODO: optimize this queue.
            private readonly Queue<T> _queue;
            private PromiseRefBase.DeferredPromise<bool> _waiter;
            private int _producerCount;

            internal SingleConsumerAsyncQueueInternal(int capacity)
            {
                _queue = new Queue<T>(capacity);
                _waiter = null;
                _producerCount = 0;
            }

            // Promise<(bool, T)> TryGetValueAsync would be more efficient, but old IL2CPP compiler crashes.
            internal Promise<bool> GetHasValueAsync()
            {
                bool value;
                PromiseRefBase.DeferredPromise<bool> promise;
                lock (_queue)
                {
                    if (_producerCount == 0)
                    {
                        value = false;
                        goto ReturnImmediate;
                    }
                    if (_queue.Count > 0)
                    {
                        value = true;
                        goto ReturnImmediate;
                    }
                    _waiter = promise = PromiseRefBase.DeferredPromise<bool>.GetOrCreate();
                }
                return new Promise<bool>(promise, promise.Id, 0);

            ReturnImmediate:
                return Promise.Resolved(value);
            }

            internal T Dequeue()
            {
                lock (_queue)
                {
                    return _queue.Dequeue();
                }
            }

            internal void Enqueue(T value)
            {
                PromiseRefBase.DeferredPromise<bool> promise;
                lock (_queue)
                {
                    _queue.Enqueue(value);
                    promise = _waiter;
                    _waiter = null;
                }
                if (promise != null)
                {
                    promise.TryIncrementDeferredIdAndUnregisterCancelation(promise.DeferredId);
                    promise.ResolveDirect(true);
                }
            }

            internal void AddProducer()
            {
                lock (_queue)
                {
                    ++_producerCount;
                }
            }

            internal void RemoveProducer()
            {
                PromiseRefBase.DeferredPromise<bool> promise;
                lock (_queue)
                {
                    if ((--_producerCount > 0) | (_queue.Count > 0))
                    {
                        return;
                    }
                    promise = _waiter;
                    _waiter = null;
                }
                if (promise != null)
                {
                    promise.TryIncrementDeferredIdAndUnregisterCancelation(promise.DeferredId);
                    promise.ResolveDirect(false);
                }
            }
        } // class AsyncQueueInternal<T>
    } // class Internal
} // namespace Proto.Promises