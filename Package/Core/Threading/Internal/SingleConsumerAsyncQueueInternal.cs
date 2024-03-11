using System.Diagnostics;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
        // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
        private struct SingleConsumerAsyncQueueSmallFields
        {
            internal int _producerCount;
            internal SpinLocker _locker;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct SingleConsumerAsyncQueueInternal<T>
        {
            private PromiseRefBase.DeferredPromise<(bool, T)> _waiter;
            // These must not be readonly.
            private PoolBackedQueue<T> _queue;
            private SingleConsumerAsyncQueueSmallFields _smallValues;

            internal SingleConsumerAsyncQueueInternal(int capacity)
            {
                _queue = new PoolBackedQueue<T>(capacity);
                _waiter = null;
                _smallValues = default;
            }

            internal Promise<(bool hasValue, T value)> TryDequeueAsync()
            {
                PromiseRefBase.DeferredPromise<(bool, T)> promise;

                _smallValues._locker.Enter();
                {
                    if (_smallValues._producerCount == 0)
                    {
                        _smallValues._locker.Exit();
                        return Promise.Resolved((false, default(T)));
                    }
                    if (_queue.Count > 0)
                    {
                        var value = _queue.Dequeue();
                        _smallValues._locker.Exit();
                        return Promise.Resolved((true, value));
                    }
                    _waiter = promise = PromiseRefBase.DeferredPromise<(bool, T)>.GetOrCreate();
                }
                _smallValues._locker.Exit();

                return new Promise<(bool, T)>(promise, promise.Id);
            }

            internal void Enqueue(T value)
            {
                PromiseRefBase.DeferredPromise<(bool, T)> promise;

                _smallValues._locker.Enter();
                {
                    promise = _waiter;
                    if (promise == null)
                    {
                        _queue.Enqueue(value);
                        _smallValues._locker.Exit();
                        return;
                    }
                    _waiter = null;
                }
                _smallValues._locker.Exit();

                promise.ResolveDirect((true, value));
            }

            internal void AddProducer()
            {
                _smallValues._locker.Enter();
                ++_smallValues._producerCount;
                _smallValues._locker.Exit();
            }

            internal void RemoveProducer()
            {
                PromiseRefBase.DeferredPromise<(bool, T)> promise;

                _smallValues._locker.Enter();
                {
                    if ((--_smallValues._producerCount > 0) | (_queue.Count > 0))
                    {
                        _smallValues._locker.Exit();
                        return;
                    }
                    promise = _waiter;
                    _waiter = null;
                }
                _smallValues._locker.Exit();
                
                promise?.ResolveDirect((false, default));
            }
        } // class AsyncQueueInternal<T>
    } // class Internal
} // namespace Proto.Promises