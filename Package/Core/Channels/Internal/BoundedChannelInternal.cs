#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Channels;
using Proto.Promises.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class BoundedChannel<T> : ChannelBase<T>
        {
            // These must not be readonly.
            private ValueLinkedQueue<ChannelWritePromise<T>> _writers = new ValueLinkedQueue<ChannelWritePromise<T>>();
            private ValueLinkedQueue<ChannelWaitToWritePromise> _waitToWriters = new ValueLinkedQueue<ChannelWaitToWritePromise>();
            private PoolBackedDeque<T> _queue;
            private int _capacity;
            private BoundedChannelFullMode _fullMode;

            private BoundedChannel() { }

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

            internal override bool TryRemoveWaiter(ChannelWaitToWritePromise promise)
            {
                _smallFields._locker.Enter();
                bool success = _waitToWriters.TryRemove(promise);
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

            internal override ChannelPeekResult<T> TryPeek(int id)
            {
                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    if (_queue.IsNotEmpty)
                    {
                        var item = _queue.PeekHead();
                        _smallFields._locker.Exit();
                        return new ChannelPeekResult<T>(item, ChannelPeekResult.Success);
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        if (closedReason == ChannelSmallFields.ClosedResolvedReason)
                        {
                            return new ChannelPeekResult<T>(default, ChannelPeekResult.Closed);
                        }
                        if (closedReason == ChannelSmallFields.ClosedCanceledReason)
                        {
                            throw Promise.CancelException();
                        }
                        closedReason.UnsafeAs<IRejectContainer>().GetExceptionDispatchInfo().Throw();
                    }
                }
                _smallFields._locker.Exit();
                return new ChannelPeekResult<T>(default, ChannelPeekResult.Empty);
            }

            internal override ChannelReadResult<T> TryRead(int id)
            {
                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    if (_queue.IsNotEmpty)
                    {
                        var item = _queue.DequeueHead();
                        // If there is at least 1 writer, grab one, add its item to the queue, and resolve it outside of the lock.
                        if (_writers.IsNotEmpty)
                        {
                            var writer = _writers.Dequeue();
                            _queue.EnqueueTail(writer.GetItem());
                            _smallFields._locker.Exit();

                            writer.Resolve(new ChannelWriteResult<T>(default, ChannelWriteResult.Success));
                        }
                        else
                        {
                            // Otherwise, notify waiting writers.
                            var waitToWriters = _waitToWriters.TakeElements();
                            _smallFields._locker.Exit();

                            if (waitToWriters.IsNotEmpty)
                            {
                                var stack = waitToWriters.MoveElementsToStackUnsafe();
                                do
                                {
                                    stack.Pop().Resolve(true);
                                } while (stack.IsNotEmpty);
                            }
                        }
                        return new ChannelReadResult<T>(item, ChannelReadResult.Success);
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        if (closedReason == ChannelSmallFields.ClosedResolvedReason)
                        {
                            return new ChannelReadResult<T>(default, ChannelReadResult.Closed);
                        }
                        if (closedReason == ChannelSmallFields.ClosedCanceledReason)
                        {
                            throw Promise.CancelException();
                        }
                        closedReason.UnsafeAs<IRejectContainer>().GetExceptionDispatchInfo().Throw();
                    }
                }
                _smallFields._locker.Exit();
                return new ChannelReadResult<T>(default, ChannelReadResult.Empty);
            }

            internal override ChannelWriteResult<T> TryWrite(in T item, int id)
            {
                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        if (closedReason == ChannelSmallFields.ClosedResolvedReason)
                        {
                            return new ChannelWriteResult<T>(default, ChannelWriteResult.Closed);
                        }
                        if (closedReason == ChannelSmallFields.ClosedCanceledReason)
                        {
                            throw Promise.CancelException();
                        }
                        if (closedReason == ChannelSmallFields.DisposedReason)
                        {
                            throw new System.ObjectDisposedException(nameof(Channel<T>));
                        }
                        closedReason.UnsafeAs<IRejectContainer>().GetExceptionDispatchInfo().Throw();
                    }

                    // If there is at least 1 reader, we grab one and complete it outside of the lock.
                    if (_readers.IsNotEmpty)
                    {
                        var reader = _readers.Dequeue();
                        _smallFields._locker.Exit();
                        reader.Resolve(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                        return new ChannelWriteResult<T>(default, ChannelWriteResult.Success);
                    }

                    // Otherwise, we attempt to add the item to the queue.
                    if (_queue.Count < _capacity)
                    {
                        // Notify waiting readers.
                        var waitToReaders = _waitToReaders.TakeElements();
                        _queue.EnqueueTail(item);
                        _smallFields._locker.Exit();

                        if (waitToReaders.IsNotEmpty)
                        {
                            var stack = waitToReaders.MoveElementsToStackUnsafe();
                            do
                            {
                                stack.Pop().Resolve(true);
                            } while (stack.IsNotEmpty);
                        }
                        return new ChannelWriteResult<T>(default, ChannelWriteResult.Success);
                    }

                    // The queue is at max capacity. Drop an item, depending on the full mode.
                    if (_fullMode == BoundedChannelFullMode.Wait | _fullMode == BoundedChannelFullMode.DropWrite)
                    {
                        _smallFields._locker.Exit();
                        return new ChannelWriteResult<T>(item, ChannelWriteResult.DroppedItem);
                    }

                    T droppedItem = _fullMode == BoundedChannelFullMode.DropNewest
                        ? _queue.DequeueTail()
                        : _queue.DequeueHead();
                    _queue.EnqueueTail(item);
                    _smallFields._locker.Exit();
                    return new ChannelWriteResult<T>(droppedItem, ChannelWriteResult.DroppedItem);
                }
            }

            internal override Promise<ChannelReadResult<T>> ReadAsync(int id, CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                Validate(id);

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
                        return closedReason == ChannelSmallFields.ClosedResolvedReason ? Promise.Resolved(new ChannelReadResult<T>(default, ChannelReadResult.Closed))
                            : closedReason == ChannelSmallFields.ClosedCanceledReason ? Promise<ChannelReadResult<T>>.Canceled()
                            : Promise<ChannelReadResult<T>>.Rejected(closedReason);
                    }

                    var promise = ChannelReadPromise<T>.GetOrCreate(this, continueOnCapturedContext);
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

            internal override Promise<ChannelWriteResult<T>> WriteAsync(in T item, int id, CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                Validate(id);

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
                        return closedReason == ChannelSmallFields.ClosedResolvedReason ? Promise.Resolved(new ChannelWriteResult<T>(item, ChannelWriteResult.Closed))
                            : closedReason == ChannelSmallFields.ClosedCanceledReason ? Promise<ChannelWriteResult<T>>.Canceled()
                            : Promise<ChannelWriteResult<T>>.Rejected(closedReason);
                    }

                    // If there is at least 1 reader, we grab one and complete it outside of the lock.
                    if (_readers.IsNotEmpty)
                    {
                        var reader = _readers.Dequeue();
                        _smallFields._locker.Exit();
                        reader.Resolve(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                        return Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Success));
                    }

                    // Otherwise, we attempt to add the item to the queue.
                    if (_queue.Count < _capacity)
                    {
                        // Notify waiting readers.
                        var waitToReaders = _waitToReaders.TakeElements();
                        _queue.EnqueueTail(item);
                        _smallFields._locker.Exit();

                        if (waitToReaders.IsNotEmpty)
                        {
                            var stack = waitToReaders.MoveElementsToStackUnsafe();
                            do
                            {
                                stack.Pop().Resolve(true);
                            } while (stack.IsNotEmpty);
                        }
                        return Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Success));
                    }

                    // The queue is at max capacity. Either drop an item, or wait for an item to be read, depending on the full mode.
                    if (_fullMode == BoundedChannelFullMode.Wait)
                    {
                        var promise = ChannelWritePromise<T>.GetOrCreate(item, this, continueOnCapturedContext);
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

                    if (_fullMode == BoundedChannelFullMode.DropWrite)
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new ChannelWriteResult<T>(item, ChannelWriteResult.DroppedItem));
                    }

                    T droppedItem = _fullMode == BoundedChannelFullMode.DropNewest
                        ? _queue.DequeueTail()
                        : _queue.DequeueHead();
                    _queue.EnqueueTail(item);
                    _smallFields._locker.Exit();
                    return Promise.Resolved(new ChannelWriteResult<T>(droppedItem, ChannelWriteResult.DroppedItem));
                }
            }

            internal override Promise<bool> WaitToReadAsync(int id, CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                Validate(id);

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<bool>.Canceled();
                }

                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    if (_queue.IsNotEmpty)
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved(true);
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        return closedReason == ChannelSmallFields.ClosedResolvedReason ? Promise.Resolved(false)
                            : closedReason == ChannelSmallFields.ClosedCanceledReason ? Promise<bool>.Canceled()
                            : Promise<bool>.Rejected(closedReason);
                    }

                    var promise = ChannelWaitToReadPromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<bool>.Canceled();
                    }

                    _waitToReaders.Enqueue(promise);
                    _smallFields._locker.Exit();
                    return new Promise<bool>(promise, promise.Id);
                }
            }

            internal override Promise<bool> WaitToWriteAsync(int id, CancelationToken cancelationToken, bool continueOnCapturedContext)
            {
                Validate(id);

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<bool>.Canceled();
                }

                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        return closedReason == ChannelSmallFields.ClosedResolvedReason ? Promise.Resolved(false)
                            : closedReason == ChannelSmallFields.ClosedCanceledReason ? Promise<bool>.Canceled()
                            : Promise<bool>.Rejected(closedReason);
                    }

                    if (_queue.Count < _capacity)
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved(true);
                    }

                    var promise = ChannelWaitToWritePromise.GetOrCreate(this, continueOnCapturedContext);
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<bool>.Canceled();
                    }

                    _waitToWriters.Enqueue(promise);
                    _smallFields._locker.Exit();
                    return new Promise<bool>(promise, promise.Id);
                }
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
                    var readers = _readers.TakeElements();
                    var writers = _writers.TakeElements();
                    var waitToReaders = _waitToReaders.TakeElements();
                    var waitToWriters = _waitToWriters.TakeElements();
                    _smallFields._locker.Exit();

                    if (readers.IsNotEmpty)
                    {
                        var stack = readers.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Reject(rejection);
                        } while (stack.IsNotEmpty);
                    }
                    if (writers.IsNotEmpty)
                    {
                        var stack = writers.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Reject(rejection);
                        } while (stack.IsNotEmpty);
                    }
                    if (waitToReaders.IsNotEmpty)
                    {
                        var stack = waitToReaders.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Reject(rejection);
                        } while (stack.IsNotEmpty);
                    }
                    if (waitToWriters.IsNotEmpty)
                    {
                        var stack = waitToWriters.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Reject(rejection);
                        } while (stack.IsNotEmpty);
                    }
                }
                return true;
            }

            internal override bool TryCancel(int id)
            {
                _smallFields._locker.Enter();
                {
                    ValidateInsideLock(id);

                    if (_closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        return false;
                    }

                    _closedReason = ChannelSmallFields.ClosedCanceledReason;
                    var readers = _readers.TakeElements();
                    var writers = _writers.TakeElements();
                    var waitToReaders = _waitToReaders.TakeElements();
                    var waitToWriters = _waitToWriters.TakeElements();
                    _smallFields._locker.Exit();

                    if (readers.IsNotEmpty)
                    {
                        var stack = readers.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().CancelDirect();
                        } while (stack.IsNotEmpty);
                    }
                    if (writers.IsNotEmpty)
                    {
                        var stack = writers.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().CancelDirect();
                        } while (stack.IsNotEmpty);
                    }
                    if (waitToReaders.IsNotEmpty)
                    {
                        var stack = waitToReaders.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().CancelDirect();
                        } while (stack.IsNotEmpty);
                    }
                    if (waitToWriters.IsNotEmpty)
                    {
                        var stack = waitToWriters.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().CancelDirect();
                        } while (stack.IsNotEmpty);
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
                    var readers = _readers.TakeElements();
                    var writers = _writers.TakeElements();
                    var waitToReaders = _waitToReaders.TakeElements();
                    var waitToWriters = _waitToWriters.TakeElements();
                    _smallFields._locker.Exit();

                    if (readers.IsNotEmpty)
                    {
                        var stack = readers.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Resolve(new ChannelReadResult<T>(default, ChannelReadResult.Closed));
                        } while (stack.IsNotEmpty);
                    }
                    if (writers.IsNotEmpty)
                    {
                        var stack = writers.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Resolve(new ChannelWriteResult<T>(default, ChannelWriteResult.Closed));
                        } while (stack.IsNotEmpty);
                    }
                    if (waitToReaders.IsNotEmpty)
                    {
                        var stack = waitToReaders.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Resolve(false);
                        } while (stack.IsNotEmpty);
                    }
                    if (waitToWriters.IsNotEmpty)
                    {
                        var stack = waitToWriters.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Resolve(false);
                        } while (stack.IsNotEmpty);
                    }
                }
                return true;
            }

            internal override void Dispose(int id)
            {
                _smallFields._locker.Enter();
                if (id != _smallFields._id)
                {
                    // Do nothing if the id doesn't match, it was already disposed.
                    _smallFields._locker.Exit();
                    return;
                }

                ThrowIfInPool(this);
                unchecked { _smallFields._id = id + 1; }
                _closedReason = ChannelSmallFields.DisposedReason;
                _queue.Dispose();
                var readers = _readers.TakeElements();
                var writers = _writers.TakeElements();
                var waitToReaders = _waitToReaders.TakeElements();
                var waitToWriters = _waitToWriters.TakeElements();
                _smallFields._locker.Exit();

                ObjectPool.MaybeRepool(this);

                if (readers.IsNotEmpty | writers.IsNotEmpty | waitToReaders.IsNotEmpty | waitToWriters.IsNotEmpty)
                {
                    var rejection = CreateRejectContainer(new System.ObjectDisposedException(nameof(Channel<T>)), 3, null, this);
                    if (readers.IsNotEmpty)
                    {
                        var stack = readers.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Reject(rejection);
                        } while (stack.IsNotEmpty);
                    }
                    if (writers.IsNotEmpty)
                    {
                        var stack = writers.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Reject(rejection);
                        } while (stack.IsNotEmpty);
                    }
                    if (waitToReaders.IsNotEmpty)
                    {
                        var stack = waitToReaders.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Reject(rejection);
                        } while (stack.IsNotEmpty);
                    }
                    if (waitToWriters.IsNotEmpty)
                    {
                        var stack = waitToWriters.MoveElementsToStackUnsafe();
                        do
                        {
                            stack.Pop().Reject(rejection);
                        } while (stack.IsNotEmpty);
                    }
                }
            }

            partial void Validate(int id);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            partial void Validate(int id)
            {
                if (id != Id | _closedReason == ChannelSmallFields.DisposedReason)
                {
                    throw new System.ObjectDisposedException(nameof(Channel<T>));
                }
            }
#endif
        } // class BoundedChannel<T>
    } // class Internal
} // namespace Proto.Promises