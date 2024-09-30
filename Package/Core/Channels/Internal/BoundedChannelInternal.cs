#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Channels;
using Proto.Promises.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class BoundedChannel<T> : ChannelBase<T>
        {
            // These must not be readonly.
            private ValueLinkedQueue<ChannelWritePromise<T>> _writers = new ValueLinkedQueue<ChannelWritePromise<T>>();
            private PoolBackedDeque<T> _queue;
            private int _capacity;
            private BoundedChannelFullMode _fullMode;

            [MethodImpl(InlineOption)]
            private static BoundedChannel<T> GetFromPoolOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<BoundedChannel<T>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new BoundedChannel<T>()
                    : obj.UnsafeAs<BoundedChannel<T>>();
            }

            [MethodImpl(InlineOption)]
            internal static BoundedChannel<T> GetOrCreate(int capacity, BoundedChannelFullMode fullMode)
            {
                var channel = GetFromPoolOrCreate();
                channel.Reset();
                channel._queue = new PoolBackedDeque<T>(0);
                channel._capacity = capacity;
                channel._fullMode = fullMode;
                return channel;
            }

            internal bool TryRemoveWaiter(ChannelWritePromise<T> promise)
            {
                _smallFields._locker.Enter();
                bool success = _writers.TryRemove(promise);
                _smallFields._locker.Exit();
                return success;
            }

            internal override int GetCount(int id)
            {
                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);
                    var count = _queue.Count;
                    _smallFields._locker.Exit();
                    return count;
                }
            }

            internal override Promise<ChannelPeekResult<T>> PeekAsync(int id, CancelationToken cancelationToken)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                Validate(id);
#endif

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<ChannelPeekResult<T>>.Canceled();
                }

                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    if (_queue.IsNotEmpty)
                    {
                        var item = _queue.PeekHead();
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new ChannelPeekResult<T>(item, ChannelPeekResult.Success));
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        return closedReason == ChannelSmallFields.ClosedResolvedReason
                            ? Promise.Resolved(new ChannelPeekResult<T>(default, ChannelPeekResult.Closed))
                            : Promise<ChannelPeekResult<T>>.Rejected(closedReason);
                    }

                    var promise = ChannelPeekPromise<T>.GetOrCreate(this, CaptureContext());
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<ChannelPeekResult<T>>.Canceled();
                    }

                    _peekers.Enqueue(promise);
                    _smallFields._locker.Exit();
                    return new Promise<ChannelPeekResult<T>>(promise, promise.Id);
                }
            }

            internal override Promise<ChannelReadResult<T>> ReadAsync(int id, CancelationToken cancelationToken)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                Validate(id);
#endif

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<ChannelReadResult<T>>.Canceled();
                }

                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    if (_queue.IsNotEmpty)
                    {
                        var item = _queue.DequeueHead();
                        if (_writers.IsEmpty)
                        {
                            _smallFields._locker.Exit();
                            goto ReturnSuccess;
                        }

                        // There was at least 1 writer waiting for capacity. Grab one, add its item to the queue, and resolve it outside of the lock.
                        var writer = _writers.Dequeue();
                        _queue.EnqueueTail(writer.GetItem());
                        _smallFields._locker.Exit();
                        writer.Resolve(new ChannelWriteResult<T>(default, ChannelWriteResult.Success));

                    ReturnSuccess:
                        return Promise.Resolved(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        return closedReason == ChannelSmallFields.ClosedResolvedReason
                            ? Promise.Resolved(new ChannelReadResult<T>(default, ChannelReadResult.Closed))
                            : Promise<ChannelReadResult<T>>.Rejected(closedReason);
                    }

                    var promise = ChannelReadPromise<T>.GetOrCreate(this, CaptureContext());
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<ChannelReadResult<T>>.Canceled();
                    }

                    _readers.Enqueue(promise);
                    _smallFields._locker.Exit();
                    return new Promise<ChannelReadResult<T>>(promise, promise.Id);
                }
            }

            internal override Promise<ChannelWriteResult<T>> WriteAsync(in T item, int id, CancelationToken cancelationToken)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                Validate(id);
#endif

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<ChannelWriteResult<T>>.Canceled();
                }

                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        return closedReason == ChannelSmallFields.ClosedResolvedReason
                            ? Promise.Resolved(new ChannelWriteResult<T>(item, ChannelWriteResult.Closed))
                            : Promise<ChannelWriteResult<T>>.Rejected(closedReason);
                    }

                    ChannelReadPromise<T> reader;
                    var peekers = _peekers.MoveElementsToStack();
                    // If there is at least 1 waiting reader, we grab one and complete it outside of the lock.
                    if (_readers.IsNotEmpty)
                    {
                        reader = _readers.Dequeue();
                        _smallFields._locker.Exit();
                        reader.Resolve(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                    }
                    // Otherwise, we attempt to add the item to the queue.
                    else if (_queue.Count < _capacity)
                    {
                        _queue.EnqueueTail(item);
                        _smallFields._locker.Exit();
                    }
                    // If the queue is at max capacity, we either drop an item, or wait for an item to be read, depending on the full mode.
                    else if (_fullMode == BoundedChannelFullMode.Wait)
                    {
                        var promise = ChannelWritePromise<T>.GetOrCreate(item, this, CaptureContext());
                        if (promise.HookupAndGetIsCanceled(cancelationToken))
                        {
                            _smallFields._locker.Exit();
                            promise.DisposeImmediate();
                            return Promise<ChannelWriteResult<T>>.Canceled();
                        }

                        _writers.Enqueue(promise);
                        _smallFields._locker.Exit();
                        return new Promise<ChannelWriteResult<T>>(promise, promise.Id);
                    }
                    else if (_fullMode == BoundedChannelFullMode.DropWrite)
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new ChannelWriteResult<T>(item, ChannelWriteResult.DroppedItem));
                    }
                    else
                    {
                        T droppedItem = _fullMode == BoundedChannelFullMode.DropNewest
                            ? _queue.DequeueTail()
                            : _queue.DequeueHead();
                        _queue.EnqueueTail(item);
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new ChannelWriteResult<T>(droppedItem, ChannelWriteResult.DroppedItem));
                    }

                    // All waiting peekers receive the item, even if there was a waiting reader preventing it from entering the queue.
                    while (peekers.IsNotEmpty)
                    {
                        peekers.Pop().Resolve(new ChannelPeekResult<T>(item, ChannelPeekResult.Success));
                    }
                }
                return Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Success));
            }

            internal override bool TryReject(object reason, int id)
            {
                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    if (_closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        return false;
                    }

                    var rejection = CreateRejectContainer(reason, 1, null, this);
                    _closedReason = rejection;
                    var peekers = _peekers.MoveElementsToStack();
                    var readers = _readers.MoveElementsToStack();
                    var writers = _writers.MoveElementsToStack();
                    _smallFields._locker.Exit();

                    while (peekers.IsNotEmpty)
                    {
                        peekers.Pop().Reject(rejection);
                    }
                    while (readers.IsNotEmpty)
                    {
                        readers.Pop().Reject(rejection);
                    }
                    while (writers.IsNotEmpty)
                    {
                        writers.Pop().Reject(rejection);
                    }
                }
                return true;
            }

            internal override bool TryClose(int id)
            {
                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    if (_closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        return false;
                    }

                    _closedReason = ChannelSmallFields.ClosedResolvedReason;
                    var peekers = _peekers.MoveElementsToStack();
                    var readers = _readers.MoveElementsToStack();
                    var writers = _writers.MoveElementsToStack();
                    _smallFields._locker.Exit();

                    while (peekers.IsNotEmpty)
                    {
                        peekers.Pop().Resolve(new ChannelPeekResult<T>(default, ChannelPeekResult.Closed));
                    }
                    while (readers.IsNotEmpty)
                    {
                        readers.Pop().Resolve(new ChannelReadResult<T>(default, ChannelReadResult.Closed));
                    }
                    while (writers.IsNotEmpty)
                    {
                        writers.Pop().Resolve(new ChannelWriteResult<T>(default, ChannelWriteResult.Closed));
                    }
                }
                return true;
            }

            protected override void Dispose()
            {
                base.Dispose();
                _smallFields._locker.Enter();
                {
                    _queue.Dispose();
                    var peekers = _peekers.MoveElementsToStack();
                    var readers = _readers.MoveElementsToStack();
                    var writers = _writers.MoveElementsToStack();
                    _smallFields._locker.Exit();

                    if (peekers.IsNotEmpty | readers.IsNotEmpty | writers.IsNotEmpty)
                    {
                        var rejection = CreateRejectContainer(new System.ObjectDisposedException(nameof(Channel<T>)), 3, null, this);
                        while (peekers.IsNotEmpty)
                        {
                            peekers.Pop().Reject(rejection);
                        }
                        while (readers.IsNotEmpty)
                        {
                            readers.Pop().Reject(rejection);
                        }
                        while (writers.IsNotEmpty)
                        {
                            writers.Pop().Reject(rejection);
                        }
                    }
                }
                ObjectPool.MaybeRepool(this);
            }
        }
    }
}