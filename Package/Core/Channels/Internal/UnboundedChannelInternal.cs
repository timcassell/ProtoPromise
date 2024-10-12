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
                var peekers = _peekers.MoveElementsToStack();
                var readers = _readers.MoveElementsToStack();
                _smallFields._locker.Exit();

                _queue.Dispose();
                ObjectPool.MaybeRepool(this);

                if (peekers.IsNotEmpty | readers.IsNotEmpty)
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
                }
            }

            internal override int GetCount(int id)
            {
                ValidateAndRetain(id);
                var count = _queue.Count;
                _queue.Release();
                return count;
            }

            internal override Promise<ChannelPeekResult<T>> PeekAsync(int id, CancelationToken cancelationToken)
            {
                ValidateAndRetain(id);

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    _queue.Release();
                    return Promise<ChannelPeekResult<T>>.Canceled();
                }

                if (_queue.TryPeek(out T item))
                {
                    _queue.Release();
                    return Promise.Resolved(new ChannelPeekResult<T>(item, ChannelPeekResult.Success));
                }

                _smallFields._locker.Enter();
                {
                    // Try to peek again inside the lock.
                    if (_queue.TryPeek(out item))
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        return Promise.Resolved(new ChannelPeekResult<T>(item, ChannelPeekResult.Success));
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        _queue.Release();
                        return closedReason == ChannelSmallFields.ClosedResolvedReason ? Promise.Resolved(new ChannelPeekResult<T>(default, ChannelPeekResult.Closed))
                            : closedReason == ChannelSmallFields.ClosedCanceledReason ? Promise<ChannelPeekResult<T>>.Canceled()
                            : closedReason == ChannelSmallFields.DisposedReason ? Promise<ChannelPeekResult<T>>.Rejected(new System.ObjectDisposedException(nameof(Channel<T>)))
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
                    _queue.Release();
                    return new Promise<ChannelPeekResult<T>>(promise, promise.Id);
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

                    var promise = ChannelReadPromise<T>.GetOrCreate(this, CaptureContext());
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

                    ChannelReadPromise<T> reader;
                    var peekers = _peekers.MoveElementsToStack();
                    // If there is at least 1 waiting reader, we grab one and complete it outside of the lock.
                    if (_readers.IsNotEmpty)
                    {
                        reader = _readers.Dequeue();
                        _smallFields._locker.Exit();
                        _queue.Release();
                        reader.Resolve(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                    }
                    // Otherwise, we just add the item to the queue.
                    else
                    {
                        _queue.Enqueue(item);
                        _smallFields._locker.Exit();
                        _queue.Release();
                    }

                    // All waiting peekers receive the item, even if there was a waiting reader preventing it from entering the queue.
                    while (peekers.IsNotEmpty)
                    {
                        peekers.Pop().Resolve(new ChannelPeekResult<T>(item, ChannelPeekResult.Success));
                    }
                    return Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Success));
                }
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
                var peekers = _peekers.MoveElementsToStack();
                var readers = _readers.MoveElementsToStack();
                _smallFields._locker.Exit();

                while (peekers.IsNotEmpty)
                {
                    peekers.Pop().Reject(rejection);
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
                var peekers = _peekers.MoveElementsToStack();
                var readers = _readers.MoveElementsToStack();
                _smallFields._locker.Exit();

                while (peekers.IsNotEmpty)
                {
                    peekers.Pop().CancelDirect();
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
                var peekers = _peekers.MoveElementsToStack();
                var readers = _readers.MoveElementsToStack();
                _smallFields._locker.Exit();

                while (peekers.IsNotEmpty)
                {
                    peekers.Pop().Resolve(new ChannelPeekResult<T>(default, ChannelPeekResult.Closed));
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