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
                channel._readerCount = 1;
                channel._writerCount = 1;
                return channel;
            }

            private void Dispose()
            {
                // TODO: how to handle items that are left in the queue that need to be disposed?
                // BCL Channels use an `Action<T> itemDropped` delegate, can we do better to be able to handle async dispose?
                // Perhaps APIs on ChannelReader and ChannelWriter like `public AsyncEnumerable<T> DisposeAndReadRemaining()` that returns a sequence yielding all remaining elements.
                // Or another option is to only allow writers to close the channel, and readers can always read all remaining elements until there are none left.
                // If readers continue to read past that, the read simply fails.
#if NETSTANDARD2_0 || (UNITY_2018_3_OR_NEWER && !UNITY_2021_2_OR_NEWER)
                while (_queue.TryDequeue(out _)) { }
#else
                _queue.Clear();
#endif
                ObjectPool.MaybeRepool(this);
            }

            internal override int GetCount(int id)
            {
                if (id != Id)
                {
                    throw new InvalidOperationException("The channel is not valid.", GetFormattedStacktrace(2));
                }
                // The channel could become invalid between the id check and the count retrieval, but it doesn't matter.
                // That would be a race condition that the user needs to handle anyway.
                return _queue.Count;
            }

            internal override Promise<ChannelPeekResult<T>> PeekAsync(int id, CancelationToken cancelationToken)
            {
                bool isValid = id == Id & _readerCount > 0;
                if (!isValid)
                {
                    throw new InvalidOperationException("Channel reader is invalid.", GetFormattedStacktrace(2));
                }

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
                    isValid = id == Id & _readerCount > 0;
                    var rejection = _rejection;
                    if (!isValid | rejection != null)
                    {
                        _smallFields._locker.Exit();
                        if (!isValid)
                        {
                            throw new InvalidOperationException("Channel reader is invalid.", GetFormattedStacktrace(2));
                        }
                        return Promise<ChannelPeekResult<T>>.Rejected(rejection);
                    }

                    // Try to peek again inside the lock.
                    if (_queue.TryPeek(out item))
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new ChannelPeekResult<T>(item, ChannelPeekResult.Success));
                    }

                    // There could be some items remaining in the queue after all writers were disposed,
                    // so we check the writer count after we try to peek.
                    if (_writerCount == 0)
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new ChannelPeekResult<T>(default, ChannelPeekResult.Closed));
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
                bool isValid = id == Id & _readerCount > 0;
                if (!isValid)
                {
                    throw new InvalidOperationException("Channel reader is invalid.", GetFormattedStacktrace(2));
                }

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
                    isValid = id == Id & _readerCount > 0;
                    var rejection = _rejection;
                    if (!isValid | rejection != null)
                    {
                        _smallFields._locker.Exit();
                        if (!isValid)
                        {
                            throw new InvalidOperationException("Channel reader is invalid.", GetFormattedStacktrace(2));
                        }
                        return Promise<ChannelReadResult<T>>.Rejected(rejection);
                    }

                    // Try to dequeue again inside the lock.
                    if (_queue.TryDequeue(out item))
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                    }

                    // There could be some items remaining in the queue after all writers were disposed,
                    // so we check the writer count after we try to dequeue.
                    if (_writerCount == 0)
                    {
                        _smallFields._locker.Exit();
                        return Promise.Resolved(new ChannelReadResult<T>(default, ChannelReadResult.Closed));
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
                bool isValid = id == Id & _writerCount > 0;
                if (!isValid)
                {
                    throw new InvalidOperationException("Channel writer is invalid.", GetFormattedStacktrace(2));
                }

                // Quick cancelation check before we perform the operation.
                if (cancelationToken.IsCancelationRequested)
                {
                    return Promise<ChannelWriteResult<T>>.Canceled();
                }

                _smallFields._locker.Enter();
                {
                    isValid = id == Id & _writerCount > 0;
                    var rejection = _rejection;
                    if (!isValid | rejection != null | _readerCount == 0)
                    {
                        _smallFields._locker.Exit();
                        if (!isValid)
                        {
                            throw new InvalidOperationException("Channel writer is invalid.", GetFormattedStacktrace(2));
                        }
                        if (rejection != null)
                        {
                            return Promise<ChannelWriteResult<T>>.Rejected(rejection);
                        }
                        return Promise.Resolved(new ChannelWriteResult<T>(default, ChannelWriteResult.Closed));
                    }

                    ChannelReadPromise<T> reader;
                    var peekers = _peekers.MoveElementsToStack();
                    // If there are no waiting readers, we just add the item to the queue. Otherwise, we grab a reader and complete it outside of the lock.
                    if (_readers.IsEmpty)
                    {
                        _queue.Enqueue(item);
                        _smallFields._locker.Exit();
                        goto SkipReader;
                    }

                    reader = _readers.Dequeue();
                    _smallFields._locker.Exit();
                    reader.Resolve(new ChannelReadResult<T>(item, ChannelReadResult.Success));
                
                SkipReader:
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
                    bool isValid = id == Id & _writerCount > 0;
                    if (!isValid | _rejection != null | _readerCount == 0)
                    {
                        _smallFields._locker.Exit();
                        if (isValid)
                        {
                            return false;
                        }
                        throw new InvalidOperationException("Channel writer is invalid.", GetFormattedStacktrace(2));
                    }

                    var rejection = CreateRejectContainer(reason, 1, null, this);
                    _rejection = rejection;
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

            internal override void RemoveReader(int id)
            {
                _smallFields._locker.Enter();
                {
                    if (id != Id | _readerCount == 0)
                    {
                        _smallFields._locker.Exit();
                        throw new InvalidOperationException("Channel reader is invalid.", GetFormattedStacktrace(2));
                    }

                    if (unchecked(--_readerCount) > 0)
                    {
                        _smallFields._locker.Exit();
                        return;
                    }

                    bool zeroWriters = _writerCount == 0;
                    // JIT should be able to make this branchless.
                    _smallFields._id += zeroWriters ? 1 : 0;
                    var peekers = _peekers.MoveElementsToStack();
                    var readers = _readers.MoveElementsToStack();
                    _smallFields._locker.Exit();

                    if (peekers.IsNotEmpty | readers.IsNotEmpty)
                    {
                        var rejection = CreateRejectContainer(new System.InvalidOperationException("All channel readers were disposed before the operation completed."), 2, null, this);
                        while (peekers.IsNotEmpty)
                        {
                            peekers.Pop().Reject(rejection);
                        }
                        while (readers.IsNotEmpty)
                        {
                            readers.Pop().Reject(rejection);
                        }
                    }

                    if (zeroWriters)
                    {
                        Dispose();
                    }
                }
            }

            internal override void RemoveWriter(int id)
            {
                _smallFields._locker.Enter();
                {
                    if (id != Id | _writerCount == 0)
                    {
                        _smallFields._locker.Exit();
                        throw new InvalidOperationException("Channel writer is invalid.", GetFormattedStacktrace(2));
                    }

                    if (unchecked(--_writerCount) > 0)
                    {
                        _smallFields._locker.Exit();
                        return;
                    }

                    bool zeroReaders = _readerCount == 0;
                    // JIT should be able to make this branchless.
                    _smallFields._id += zeroReaders ? 1 : 0;
                    var peekers = _peekers.MoveElementsToStack();
                    var readers = _readers.MoveElementsToStack();
                    _smallFields._locker.Exit();

                    if (zeroReaders)
                    {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        Debug.Assert(peekers.IsEmpty);
                        Debug.Assert(readers.IsEmpty);
#endif
                        Dispose();
                        return;
                    }

                    while (peekers.IsNotEmpty)
                    {
                        peekers.Pop().Resolve(new ChannelPeekResult<T>(default, ChannelPeekResult.Closed));
                    }
                    while (readers.IsNotEmpty)
                    {
                        readers.Pop().Resolve(new ChannelReadResult<T>(default, ChannelReadResult.Closed));
                    }
                }
            }
        }
    }
}