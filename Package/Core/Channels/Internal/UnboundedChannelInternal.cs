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
        internal sealed class UnboundedChannel<T> : ChannelBase<T>
        {
            // Must not be readonly.
            private PoolBackedConcurrentQueue<T> _queue;

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
                channel._queue = new PoolBackedConcurrentQueue<T>(true);
                return channel;
            }

            private void ValidateAndRetainInsideLock(int id)
            {
                ValidateInsideLock(id);
                ThrowIfInPool(this);
                // If it's still valid, we increment the retain counter to ensure the concurrent queue is still valid until all threads are done with it.
                _queue.IncrementRetainCounter();
            }

            private void ValidateAndRetain(int id)
            {
                // We have to enter the lock and check the id to make sure the concurrent queue isn't accessed after this is disposed.
                _smallFields._locker.Enter();
                ValidateAndRetainInsideLock(id);
                // Once we have validated and retained this,we exit the lock so the concurrent queue can do its job, and we aren't bottlenecking all threads unnecessarily.
                _smallFields._locker.Exit();
            }

            private void Release()
            {
                ThrowIfInPool(this);
                if (_queue.TryReleaseComplete())
                {
                    ObjectPool.MaybeRepool(this);
                }
            }

            internal override void Dispose(int id)
            {
                _smallFields._locker.Enter();
                if (id != _smallFields._id)
                {
                    // Do nothing if the id doesn't match, it was already disposed.
                    // We don't throw here because MSDN says IDisposable.Dispose should be a no-op if it's called more than once.
                    _smallFields._locker.Exit();
                    return;
                }

                ThrowIfInPool(this);
                unchecked { _smallFields._id = id + 1; }
                _closedReason = ChannelSmallFields.DisposedReason;

                var peekers = _peekers.MoveElementsToStack();
                var readers = _readers.MoveElementsToStack();
                _smallFields._locker.Exit();

                if (_queue.DisposeAndGetIsComplete())
                {
                    ObjectPool.MaybeRepool(this);
                }

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
                Release();
                return count;
            }

            internal override Promise<ChannelPeekResult<T>> PeekAsync(int id, CancelationToken cancelationToken)
            {
                ValidateAndRetain(id);

                try
                {
                    // Quick cancelation check before we perform the operation.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Promise<ChannelPeekResult<T>>.Canceled();
                    }

                    if (_queue.TryPeek(out T item))
                    {
                        return Promise.Resolved(new ChannelPeekResult<T>(item, ChannelPeekResult.Success));
                    }

                    _smallFields._locker.Enter();
                    {
                        // Try to peek again inside the lock.
                        if (_queue.TryPeek(out item))
                        {
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
                finally
                {
                    Release();
                }
            }

            internal override Promise<ChannelReadResult<T>> ReadAsync(int id, CancelationToken cancelationToken)
            {
                ValidateAndRetain(id);

                try
                {
                    // Quick cancelation check before we perform the operation.
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Promise<ChannelReadResult<T>>.Canceled();
                    }

                    if (_queue.TryDequeue(out T item))
                    {
                        return Promise.Resolved(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                    }

                    _smallFields._locker.Enter();
                    {
                        // Try to dequeue again inside the lock.
                        if (_queue.TryDequeue(out item))
                        {
                            _smallFields._locker.Exit();
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
                finally
                {
                    Release();
                }
            }

            internal override Promise<ChannelWriteResult<T>> WriteAsync(in T item, int id, CancelationToken cancelationToken)
            {
                // Query the cancelation token before entering the lock.
                bool isCanceled = cancelationToken.IsCancelationRequested;

                _smallFields._locker.Enter();
                ValidateAndRetainInsideLock(id);

                try
                {
                    // Quick cancelation check before we perform the operation.
                    if (isCanceled)
                    {
                        _smallFields._locker.Exit();
                        return Promise<ChannelWriteResult<T>>.Canceled();
                    }

                    var closedReason = _closedReason;
                    if (closedReason != null)
                    {
                        _smallFields._locker.Exit();
                        return closedReason == ChannelSmallFields.ClosedResolvedReason
                            ? Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Closed))
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
                    // Otherwise, we just add the item to the queue.
                    else
                    {
                        _queue.Enqueue(item);
                        _smallFields._locker.Exit();
                    }

                    // All waiting peekers receive the item, even if there was a waiting reader preventing it from entering the queue.
                    while (peekers.IsNotEmpty)
                    {
                        peekers.Pop().Resolve(new ChannelPeekResult<T>(item, ChannelPeekResult.Success));
                    }
                    return Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Success));
                }
                finally
                {
                    Release();
                }
            }

            internal override bool TryReject(object reason, int id)
            {
                _smallFields._locker.Enter();
                ValidateAndRetainInsideLock(id);

                try
                {
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
                finally
                {
                    Release();
                }
            }

            internal override bool TryClose(int id)
            {
                _smallFields._locker.Enter();
                ValidateAndRetainInsideLock(id);

                try
                {
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
                finally
                {
                    Release();
                }
            }
        } // class UnboundedChannel<T>
    } // class Internal
} // namespace Proto.Promises