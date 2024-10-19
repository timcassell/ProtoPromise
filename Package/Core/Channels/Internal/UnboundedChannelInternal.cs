#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Channels;
using Proto.Promises.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class UnboundedChannel<T> : ChannelBase<T>
        {
            // Must not be readonly.
            private PoolBackedConcurrentQueue<T> _queue = new PoolBackedConcurrentQueue<T>();

            private UnboundedChannel() { }

            [MethodImpl(InlineOption)]
            private static UnboundedChannel<T> GetFromPoolOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<UnboundedChannel<T>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new UnboundedChannel<T>()
                    : obj.UnsafeAs<UnboundedChannel<T>>();
            }

            [MethodImpl(InlineOption)]
            internal static UnboundedChannel<T> GetOrCreate()
            {
                var channel = GetFromPoolOrCreate();
                channel.Reset();
                channel._queue.Reset();
                return channel;
            }

            private void ValidateAndRetain(int id)
            {
                // Retain before validating to ensure the concurrent queue is still valid until all threads are done with it.
                // We use retain/release instead of lock for improved concurrency.
                _queue.Retain();
                if (id != Id | _closedReason == ChannelSmallFields.DisposedReason)
                {
                    _queue.Release();
                    throw new System.ObjectDisposedException(nameof(Channel<T>));
                }
                ThrowIfInPool(this);
            }

            internal override void Dispose(int id)
            {
                if (Interlocked.CompareExchange(ref _smallFields._id, id + 1, id) != id)
                {
                    // Do nothing if the id doesn't match, it was already disposed.
                    // We don't throw here because MSDN says IDisposable.Dispose should be a no-op if it's called more than once.
                    return;
                }
                ThrowIfInPool(this);

                _queue.PreDispose();
                
                _smallFields._locker.Enter();
                _closedReason = ChannelSmallFields.DisposedReason;
                var waitToReaders = _waitToReaders.MoveElementsToStack();
                var readers = _readers.MoveElementsToStack();
                _smallFields._locker.Exit();

                _queue.Dispose();
                ObjectPool.MaybeRepool(this);

                if (waitToReaders.IsNotEmpty | readers.IsNotEmpty)
                {
                    var rejection = CreateRejectContainer(new System.ObjectDisposedException(nameof(Channel<T>)), 3, null, this);
                    while (waitToReaders.IsNotEmpty)
                    {
                        waitToReaders.Pop().Reject(rejection);
                    }
                    while (readers.IsNotEmpty)
                    {
                        readers.Pop().Reject(rejection);
                    }
                }
            }

            internal override int GetCount(int id)
            {
                ValidateAndRetain(id);
                var count = _queue.Count;
                _queue.Release();
                return count;
            }

            internal override ChannelPeekResult<T> TryPeek(int id)
            {
                ValidateAndRetain(id);

                if (_queue.TryPeek(out T item))
                {
                    _queue.Release();
                    return new ChannelPeekResult<T>(item, ChannelPeekResult.Success);
                }

                _smallFields._locker.Enter();
                {
                    // Try to peek again inside the lock.
                    if (_queue.TryPeek(out item))
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        return new ChannelPeekResult<T>(item, ChannelPeekResult.Success);
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        if (closedReason == ChannelSmallFields.ClosedResolvedReason)
                        {
                            return new ChannelPeekResult<T>(default, ChannelPeekResult.Closed);
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
                }
                _smallFields._locker.Exit();
                _queue.Release();
                return new ChannelPeekResult<T>(default, ChannelPeekResult.Empty);
            }

            internal override ChannelReadResult<T> TryRead(int id)
            {
                ValidateAndRetain(id);

                if (_queue.TryDequeue(out T item))
                {
                    _queue.Release();
                    return new ChannelReadResult<T>(item, ChannelReadResult.Success);
                }

                _smallFields._locker.Enter();
                {
                    // Try to dequeue again inside the lock.
                    if (_queue.TryDequeue(out item))
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        return new ChannelReadResult<T>(item, ChannelReadResult.Success);
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        if (closedReason == ChannelSmallFields.ClosedResolvedReason)
                        {
                            return new ChannelReadResult<T>(default, ChannelReadResult.Closed);
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
                }
                _smallFields._locker.Exit();
                _queue.Release();
                return new ChannelReadResult<T>(default, ChannelReadResult.Empty);
            }

            internal override ChannelWriteResult<T> TryWrite(in T item, int id)
            {
                ValidateAndRetain(id);

                _smallFields._locker.Enter();
                {
                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
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
                        _queue.Release();
                        reader.Resolve(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                        return new ChannelWriteResult<T>(default, ChannelWriteResult.Success);
                    }

                    // Otherwise, we just add the item to the queue, and notify waiting readers.
                    _queue.Enqueue(item);
                    var waitToReaders = _waitToReaders.MoveElementsToStack();
                    _smallFields._locker.Exit();
                    _queue.Release();

                    while (waitToReaders.IsNotEmpty)
                    {
                        waitToReaders.Pop().Resolve(true);
                    }
                    return new ChannelWriteResult<T>(default, ChannelWriteResult.Success);
                }
            }

            internal override Promise<ChannelReadResult<T>> ReadAsync(int id, CancelationToken cancelationToken)
            {
                ValidateAndRetain(id);

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    _queue.Release();
                    return Promise<ChannelReadResult<T>>.Canceled();
                }

                if (_queue.TryDequeue(out T item))
                {
                    _queue.Release();
                    return Promise.Resolved(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                }

                _smallFields._locker.Enter();
                {
                    // Try to dequeue again inside the lock.
                    if (_queue.TryDequeue(out item))
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        return Promise.Resolved(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        return closedReason == ChannelSmallFields.ClosedResolvedReason ? Promise.Resolved(new ChannelReadResult<T>(default, ChannelReadResult.Closed))
                            : closedReason == ChannelSmallFields.ClosedCanceledReason ? Promise<ChannelReadResult<T>>.Canceled()
                            : closedReason == ChannelSmallFields.DisposedReason ? Promise<ChannelReadResult<T>>.Rejected(new System.ObjectDisposedException(nameof(Channel<T>)))
                            : Promise<ChannelReadResult<T>>.Rejected(closedReason);
                    }

                    var promise = ChannelReadPromise<T>.GetOrCreate(this, ContinuationOptions.CaptureContext());
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<ChannelReadResult<T>>.Canceled();
                    }

                    _readers.Enqueue(promise);
                    _smallFields._locker.Exit();
                    _queue.Release();
                    return new Promise<ChannelReadResult<T>>(promise, promise.Id);
                }
            }

            internal override Promise<ChannelWriteResult<T>> WriteAsync(in T item, int id, CancelationToken cancelationToken)
            {
                ValidateAndRetain(id);

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    _smallFields._locker.Exit();
                    _queue.Release();
                    return Promise<ChannelWriteResult<T>>.Canceled();
                }

                _smallFields._locker.Enter();
                {
                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        return closedReason == ChannelSmallFields.ClosedResolvedReason ? Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Closed))
                            : closedReason == ChannelSmallFields.ClosedCanceledReason ? Promise<ChannelWriteResult<T>>.Canceled()
                            : closedReason == ChannelSmallFields.DisposedReason ? Promise<ChannelWriteResult<T>>.Rejected(new System.ObjectDisposedException(nameof(Channel<T>)))
                            : Promise<ChannelWriteResult<T>>.Rejected(closedReason);
                    }

                    // If there is at least 1 reader, we grab one and complete it outside of the lock.
                    if (_readers.IsNotEmpty)
                    {
                        var reader = _readers.Dequeue();
                        _smallFields._locker.Exit();
                        _queue.Release();
                        reader.Resolve(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                        return Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Success));
                    }

                    // Otherwise, we just add the item to the queue, and notify waiting readers.
                    _queue.Enqueue(item);
                    var waitToReaders = _waitToReaders.MoveElementsToStack();
                    _smallFields._locker.Exit();
                    _queue.Release();

                    while (waitToReaders.IsNotEmpty)
                    {
                        waitToReaders.Pop().Resolve(true);
                    }
                    return Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Success));
                }
            }

            internal override Promise<bool> WaitToReadAsync(int id, CancelationToken cancelationToken)
            {
                ValidateAndRetain(id);

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    _queue.Release();
                    return Promise<bool>.Canceled();
                }

                if (!_queue.IsEmpty)
                {
                    _queue.Release();
                    return Promise.Resolved(true);
                }

                _smallFields._locker.Enter();
                {
                    // Query IsEmpty again inside the lock.
                    if (!_queue.IsEmpty)
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        return Promise.Resolved(true);
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        return closedReason == ChannelSmallFields.ClosedResolvedReason ? Promise.Resolved(false)
                            : closedReason == ChannelSmallFields.ClosedCanceledReason ? Promise<bool>.Canceled()
                            : closedReason == ChannelSmallFields.DisposedReason ? Promise<bool>.Rejected(new System.ObjectDisposedException(nameof(Channel<T>)))
                            : Promise<bool>.Rejected(closedReason);
                    }

                    var promise = ChannelWaitToReadPromise.GetOrCreate(this, ContinuationOptions.CaptureContext());
                    if (promise.HookupAndGetIsCanceled(cancelationToken))
                    {
                        _smallFields._locker.Exit();
                        promise.DisposeImmediate();
                        return Promise<bool>.Canceled();
                    }

                    _waitToReaders.Enqueue(promise);
                    _smallFields._locker.Exit();
                    _queue.Release();
                    return new Promise<bool>(promise, promise.Id);
                }
            }

            internal override Promise<bool> WaitToWriteAsync(int id, CancelationToken cancelationToken)
            {
                var closedReason = _closedReason;
                if (id != Id | closedReason == ChannelSmallFields.DisposedReason)
                {
                    throw new System.ObjectDisposedException(nameof(Channel<T>));
                }
                return cancelationToken.IsCancelationRequested | closedReason == ChannelSmallFields.ClosedCanceledReason ? Promise<bool>.Canceled()
                    : closedReason == null | closedReason == ChannelSmallFields.ClosedResolvedReason ? Promise.Resolved(closedReason == null)
                    : Promise<bool>.Rejected(closedReason);
            }

            internal override bool TryReject(object reason, int id)
            {
                _smallFields._locker.Enter();
                ValidateInsideLock(id);

                if (_closedReason != null)
                {
                    _smallFields._locker.Exit();
                    return false;
                }

                var rejection = CreateRejectContainer(reason, 1, null, this);
                _closedReason = rejection;
                var waitToReaders = _waitToReaders.MoveElementsToStack();
                var readers = _readers.MoveElementsToStack();
                _smallFields._locker.Exit();

                while (waitToReaders.IsNotEmpty)
                {
                    waitToReaders.Pop().Reject(rejection);
                }
                while (readers.IsNotEmpty)
                {
                    readers.Pop().Reject(rejection);
                }
                return true;
            }

            internal override bool TryCancel(int id)
            {
                _smallFields._locker.Enter();
                ValidateInsideLock(id);

                if (_closedReason != null)
                {
                    _smallFields._locker.Exit();
                    return false;
                }

                _closedReason = ChannelSmallFields.ClosedCanceledReason;
                var waitToReaders = _waitToReaders.MoveElementsToStack();
                var readers = _readers.MoveElementsToStack();
                _smallFields._locker.Exit();

                while (waitToReaders.IsNotEmpty)
                {
                    waitToReaders.Pop().CancelDirect();
                }
                while (readers.IsNotEmpty)
                {
                    readers.Pop().CancelDirect();
                }
                return true;
            }

            internal override bool TryClose(int id)
            {
                _smallFields._locker.Enter();
                ValidateInsideLock(id);

                if (_closedReason != null)
                {
                    _smallFields._locker.Exit();
                    return false;
                }

                _closedReason = ChannelSmallFields.ClosedResolvedReason;
                var waitToReaders = _waitToReaders.MoveElementsToStack();
                var readers = _readers.MoveElementsToStack();
                _smallFields._locker.Exit();

                while (waitToReaders.IsNotEmpty)
                {
                    waitToReaders.Pop().Resolve(false);
                }
                while (readers.IsNotEmpty)
                {
                    readers.Pop().Resolve(new ChannelReadResult<T>(default, ChannelReadResult.Closed));
                }
                return true;
            }
        } // class UnboundedChannel<T>
    } // class Internal
} // namespace Proto.Promises