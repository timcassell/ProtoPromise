using System.Collections.Generic;
using System.Diagnostics;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct SingleConsumerAsyncQueueInternal<T>
        {
            // TODO: optimize this queue.
            private readonly Queue<T> _queue;
            private PromiseRefBase.DeferredPromise<(bool, T)> _waiter;
            private int _producerCount;

            internal SingleConsumerAsyncQueueInternal(int capacity)
            {
                _queue = new Queue<T>(capacity);
                _waiter = null;
                _producerCount = 0;
            }

            internal Promise<(bool hasValue, T value)> TryDequeueAsync()
            {
                bool hasValue;
                T value = default;
                PromiseRefBase.DeferredPromise<(bool, T)> promise;
                lock (_queue)
                {
                    if (_producerCount == 0)
                    {
                        hasValue = false;
                        goto ReturnImmediate;
                    }
                    if (_queue.Count > 0)
                    {
                        hasValue = true;
                        value = _queue.Dequeue();
                        goto ReturnImmediate;
                    }
                    _waiter = promise = PromiseRefBase.DeferredPromise<(bool, T)>.GetOrCreate();
                }
                return new Promise<(bool, T)>(promise, promise.Id, 0);

            ReturnImmediate:
                return Promise.Resolved((hasValue, value));
            }

            internal void Enqueue(T value)
            {
                PromiseRefBase.DeferredPromise<(bool, T)> promise;
                lock (_queue)
                {
                    promise = _waiter;
                    if (promise == null)
                    {
                        _queue.Enqueue(value);
                        return;
                    }
                    _waiter = null;
                }
                promise.ResolveDirect((true, value));
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
                PromiseRefBase.DeferredPromise<(bool, T)> promise;
                lock (_queue)
                {
                    if ((--_producerCount > 0) | (_queue.Count > 0))
                    {
                        return;
                    }
                    promise = _waiter;
                    _waiter = null;
                }
                promise?.ResolveDirect((false, default));
            }
        } // class AsyncQueueInternal<T>
    } // class Internal
#endif // CSHARP_7_3_OR_NEWER
} // namespace Proto.Promises