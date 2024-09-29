#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Channels;
using System.Collections.Concurrent;
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
            // TODO: implement custom ConcurrentQueue that avoids allocations.
            private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

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
                return channel;
            }

            internal override int GetCount(int id)
            {
                Validate(id);
                // The channel could become invalid between the validation and the count retrieval, but it doesn't matter.
                // That would be a race condition that the user needs to handle anyway.
                return _queue.Count;
            }

            internal override Promise<ChannelPeekResult<T>> PeekAsync(int id, CancelationToken cancelationToken)
            {
                Validate(id);

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

            internal override Promise<ChannelReadResult<T>> ReadAsync(int id, CancelationToken cancelationToken)
            {
                Validate(id);

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

            internal override Promise<ChannelWriteResult<T>> WriteAsync(in T item, int id, CancelationToken cancelationToken)
            {
                Validate(id);

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<ChannelWriteResult<T>>.Canceled();
                }

                _smallFields._locker.Enter();
                {
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
                }
                return Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Success));
            }

            internal override bool TryReject(object reason, int id)
            {
                Validate(id);

                _smallFields._locker.Enter();
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
                }
                return true;
            }

            internal override bool TryClose(int id)
            {
                Validate(id);

                _smallFields._locker.Enter();
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
                }
                return true;
            }

            protected override void Dispose()
            {
                base.Dispose();
                _smallFields._locker.Enter();
                {
                    var peekers = _peekers.MoveElementsToStack();
                    var readers = _readers.MoveElementsToStack();
                    _smallFields._locker.Exit();

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
#if NETSTANDARD2_0 || (UNITY_2018_3_OR_NEWER && !UNITY_2021_2_OR_NEWER)
                while (_queue.TryDequeue(out _)) { }
#else
                _queue.Clear();
#endif
                ObjectPool.MaybeRepool(this);
            }
        } // class UnboundedChannel<T>
    } // class Internal
} // namespace Proto.Promises