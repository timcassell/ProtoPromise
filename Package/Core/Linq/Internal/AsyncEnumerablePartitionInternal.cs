#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncEnumerablePartition<TSource> : PromiseRefBase.AsyncEnumerableWithIterator<TSource>
        {
            private AsyncEnumerator<TSource> _source;
            // We set this to -1 to indicate no elements will be yielded, for example e.Take(5).Skip(5).
            private int _minIndexInclusive;
            private int _maxIndexInclusive;

            private bool WillYieldNothing
            {
                [MethodImpl(InlineOption)]
                get => _minIndexInclusive == -1;
            }

            private bool HasUpperLimit
            {
                [MethodImpl(InlineOption)]
                get => _maxIndexInclusive != -1;
            }

            private AsyncEnumerablePartition() { }

            [MethodImpl(InlineOption)]
            private static AsyncEnumerablePartition<TSource> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncEnumerablePartition<TSource>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncEnumerablePartition<TSource>()
                    : obj.UnsafeAs<AsyncEnumerablePartition<TSource>>();
            }

            internal static AsyncEnumerablePartition<TSource> GetOrCreate(AsyncEnumerator<TSource> source, int minIndexInclusive, int maxIndexInclusive)
            {
                var instance = GetOrCreate();
                instance.Reset();
                instance._source = source;
                instance._minIndexInclusive = minIndexInclusive;
                instance._maxIndexInclusive = maxIndexInclusive;
                return instance;
            }

            protected override void DisposeAndReturnToPool()
            {
                Dispose();
                _source = default;
                ObjectPool.MaybeRepool(this);
            }

            internal override void MaybeDispose()
            {
                // This is called on every MoveNextAsync, we only fully dispose and return to pool after DisposeAsync is called.
                if (_disposed)
                {
                    DisposeAndReturnToPool();
                }
            }

            protected override Promise DisposeAsyncWithoutStart()
            {
                SetStateForDisposeWithoutStart();
                var source = _source;
                DisposeAndReturnToPool();
                return source.DisposeAsync();
            }

            protected override void Start(int enumerableId)
            {
                var iteratorPromise = WillYieldNothing
                    // If we're not going to yield any elements, we just dispose the source without doing extra work.
                    ? _source.DisposeAsync()
                    : Iterate(enumerableId)._promise;
                if (iteratorPromise._ref == null)
                {
                    // Already complete.
                    HandleFromSynchronouslyCompletedIterator();
                    return;
                }

                // We only set _previous to support circular await detection.
#if PROMISE_DEBUG
                _previous = iteratorPromise._ref;
#endif
                // We hook this up directly to the returned promise so we can know when the iteration is complete, and use this for the DisposeAsync promise.
                iteratorPromise._ref.HookupExistingWaiter(iteratorPromise._id, this);
            }

            private async AsyncIteratorMethod Iterate(int streamWriterId)
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _source._target._cancelationToken = _cancelationToken;

                try
                {
                    int index = 0;
                    while (index < _minIndexInclusive)
                    {
                        // Skip
                        if (!await _source.MoveNextAsync())
                        {
                            // Reached the end before we finished skipping.
                            return;
                        }
                        ++index;
                    }

                    if (HasUpperLimit)
                    {
                        // Take
                        while (index <= _maxIndexInclusive)
                        {
                            if (!await _source.MoveNextAsync())
                            {
                                // No more elements
                                break;
                            }
                            ++index;
                            await YieldAsync(_source.Current, streamWriterId);
                        }
                    }
                    else
                    {
                        while (await _source.MoveNextAsync())
                        {
                            await YieldAsync(_source.Current, streamWriterId);
                        }
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await YieldAsync(default, streamWriterId).ForLinqExtension();
                }
                finally
                {
                    await _source.DisposeAsync();
                }
            }

            private void IncrementEnumerableId(int enumerableId)
            {
                if (Interlocked.CompareExchange(ref _enumerableId, enumerableId + 1, enumerableId) != enumerableId)
                {
                    ThrowInvalidAsyncEnumerable(3);
                }
            }

            internal AsyncEnumerable<TSource> Skip(int enumerableId, int count)
            {
                IncrementEnumerableId(enumerableId);

                if (WillYieldNothing)
                {
                    // We're already yielding zero elements due to previous operations, just return this.
                    return new AsyncEnumerable<TSource>(this);
                }

                unchecked
                {
                    var newMinIndex = _minIndexInclusive + count;
                    if (!HasUpperLimit)
                    {
                        if (newMinIndex < 0)
                        {
                            // If we don't know our max count and newMinIndex can no longer fit in a positive int,
                            // then we will need to wrap ourselves in another iterator.
                            // This can happen, for example, during e.Skip(int.MaxValue).Skip(int.MaxValue).
                            var enumerable = GetOrCreate(new AsyncEnumerator<TSource>(this, enumerableId + 1), count, -1);
                            return new AsyncEnumerable<TSource>(enumerable);
                        }
                    }
                    else if ((uint) newMinIndex > (uint) _maxIndexInclusive)
                    {
                        // If newMinIndex is greater than max, no elements will be yielded.
                        _minIndexInclusive = -1;
                        return new AsyncEnumerable<TSource>(this);
                    }

                    _minIndexInclusive = newMinIndex;
                    return new AsyncEnumerable<TSource>(this);
                }
            }

            internal AsyncEnumerable<TSource> Take(int enumerableId, int count)
            {
                IncrementEnumerableId(enumerableId);

                if (WillYieldNothing)
                {
                    // We're already yielding zero elements due to previous operations, just return this.
                    return new AsyncEnumerable<TSource>(this);
                }

                unchecked
                {
                    var newMaxIndex = _minIndexInclusive + count - 1;
                    if (!HasUpperLimit)
                    {
                        if (newMaxIndex < 0)
                        {
                            // If we don't know our max count and newMaxIndex can no longer fit in a positive int,
                            // then we will need to wrap ourselves in another iterator.
                            // Note that although newMaxIndex may be too large, the difference between it and
                            // _minIndexInclusive (which is count - 1) must fit in an int.
                            // Example: e.Skip(50).Take(int.MaxValue).
                            var enumerable = GetOrCreate(new AsyncEnumerator<TSource>(this, enumerableId + 1), 0, count - 1);
                            return new AsyncEnumerable<TSource>(enumerable);
                        }
                    }
                    else if ((uint) newMaxIndex >= (uint) _maxIndexInclusive)
                    {
                        // We make no modifications if attempting to take more items than we're already going to yield.
                        return new AsyncEnumerable<TSource>(this);
                    }

                    _maxIndexInclusive = newMaxIndex;
                    return new AsyncEnumerable<TSource>(this);
                }
            }
        } // class AsyncEnumerablePartition<TSource>

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncEnumerablePartitionFromLast<TSource> : PromiseRefBase.AsyncEnumerableWithIterator<TSource>
        {
            private AsyncEnumerator<TSource> _source;
            // We set this to -1 to indicate no elements will be yielded, for example e.TakeLast(5).SkipLast(5).
            private int _minIndexInclusive;
            private int _maxIndexInclusive;

            private bool WillYieldNothing
            {
                [MethodImpl(InlineOption)]
                get => _minIndexInclusive == -1;
            }

            private bool HasUpperLimit
            {
                [MethodImpl(InlineOption)]
                get => _maxIndexInclusive != -1;
            }

            private AsyncEnumerablePartitionFromLast() { }

            [MethodImpl(InlineOption)]
            private static AsyncEnumerablePartitionFromLast<TSource> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncEnumerablePartitionFromLast<TSource>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncEnumerablePartitionFromLast<TSource>()
                    : obj.UnsafeAs<AsyncEnumerablePartitionFromLast<TSource>>();
            }

            internal static AsyncEnumerablePartitionFromLast<TSource> GetOrCreate(AsyncEnumerator<TSource> source, int minIndexInclusive, int maxIndexInclusive)
            {
                var instance = GetOrCreate();
                instance.Reset();
                instance._source = source;
                instance._minIndexInclusive = minIndexInclusive;
                instance._maxIndexInclusive = maxIndexInclusive;
                return instance;
            }

            protected override void DisposeAndReturnToPool()
            {
                Dispose();
                _source = default;
                ObjectPool.MaybeRepool(this);
            }

            internal override void MaybeDispose()
            {
                // This is called on every MoveNextAsync, we only fully dispose and return to pool after DisposeAsync is called.
                if (_disposed)
                {
                    DisposeAndReturnToPool();
                }
            }

            protected override Promise DisposeAsyncWithoutStart()
            {
                SetStateForDisposeWithoutStart();
                var source = _source;
                DisposeAndReturnToPool();
                return source.DisposeAsync();
            }

            protected override void Start(int enumerableId)
            {
                var iteratorPromise = WillYieldNothing
                    // If we're not going to yield any elements, we just dispose the source without doing extra work.
                    ? _source.DisposeAsync()
                    : Iterate(enumerableId)._promise;
                if (iteratorPromise._ref == null)
                {
                    // Already complete.
                    HandleFromSynchronouslyCompletedIterator();
                    return;
                }

                // We only set _previous to support circular await detection.
#if PROMISE_DEBUG
                _previous = iteratorPromise._ref;
#endif
                // We hook this up directly to the returned promise so we can know when the iteration is complete, and use this for the DisposeAsync promise.
                iteratorPromise._ref.HookupExistingWaiter(iteratorPromise._id, this);
            }

            private async AsyncIteratorMethod Iterate(int streamWriterId)
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _source._target._cancelationToken = _cancelationToken;

                try
                {
                    // Make sure at least 1 element exists before creating the queue.
                    if (!await _source.MoveNextAsync())
                    {
                        return;
                    }

                    using (var queue = new PoolBackedQueue<TSource>(1))
                    {
                        if (HasUpperLimit)
                        {
                            // TakeLast
                            queue.Enqueue(_source.Current);
                            int count = 1;

                            while (await _source.MoveNextAsync())
                            {
                                if (count <= _maxIndexInclusive)
                                {
                                    queue.Enqueue(_source.Current);
                                    ++count;
                                }
                                else
                                {
                                    do
                                    {
                                        queue.Dequeue();
                                        queue.Enqueue(_source.Current);
                                    } while (await _source.MoveNextAsync());
                                    break;
                                }
                            }

                            // SkipLast
                            while (count > _minIndexInclusive)
                            {
                                await YieldAsync(queue.Dequeue(), streamWriterId);
                                --count;
                            }
                        }
                        else
                        {
                            // SkipLast
                            do
                            {
                                if (queue.Count == _minIndexInclusive)
                                {
                                    do
                                    {
                                        await YieldAsync(queue.Dequeue(), streamWriterId);
                                        queue.Enqueue(_source.Current);
                                    }
                                    while (await _source.MoveNextAsync());
                                    break;
                                }
                                else
                                {
                                    queue.Enqueue(_source.Current);
                                }
                            } while (await _source.MoveNextAsync());
                        }
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await YieldAsync(default, streamWriterId).ForLinqExtension();
                }
                finally
                {
                    await _source.DisposeAsync();
                }
            }

            private void IncrementEnumerableId(int enumerableId)
            {
                if (Interlocked.CompareExchange(ref _enumerableId, enumerableId + 1, enumerableId) != enumerableId)
                {
                    ThrowInvalidAsyncEnumerable(3);
                }
            }

            internal AsyncEnumerable<TSource> SkipLast(int enumerableId, int count)
            {
                IncrementEnumerableId(enumerableId);

                if (WillYieldNothing)
                {
                    // We're already yielding zero elements due to previous operations, just return this.
                    return new AsyncEnumerable<TSource>(this);
                }

                unchecked
                {
                    var newMinIndex = _minIndexInclusive + count;
                    if (!HasUpperLimit)
                    {
                        if (newMinIndex < 0)
                        {
                            // If we don't know our max count and newMinIndex can no longer fit in a positive int,
                            // then we will need to wrap ourselves in another iterator.
                            // This can happen, for example, during e.Skip(int.MaxValue).Skip(int.MaxValue).
                            var enumerable = GetOrCreate(new AsyncEnumerator<TSource>(this, enumerableId + 1), count, -1);
                            return new AsyncEnumerable<TSource>(enumerable);
                        }
                    }
                    else if ((uint) newMinIndex > (uint) _maxIndexInclusive)
                    {
                        // If newMinIndex is greater than max, no elements will be yielded.
                        _minIndexInclusive = -1;
                        return new AsyncEnumerable<TSource>(this);
                    }

                    _minIndexInclusive = newMinIndex;
                    return new AsyncEnumerable<TSource>(this);
                }
            }

            internal AsyncEnumerable<TSource> TakeLast(int enumerableId, int count)
            {
                IncrementEnumerableId(enumerableId);

                if (WillYieldNothing)
                {
                    // We're already yielding zero elements due to previous operations, just return this.
                    return new AsyncEnumerable<TSource>(this);
                }

                unchecked
                {
                    var newMaxIndex = _minIndexInclusive + count - 1;
                    if (!HasUpperLimit)
                    {
                        if (newMaxIndex < 0)
                        {
                            // If we don't know our max count and newMaxIndex can no longer fit in a positive int,
                            // then we will need to wrap ourselves in another iterator.
                            // Note that although newMaxIndex may be too large, the difference between it and
                            // _minIndexInclusive (which is count - 1) must fit in an int.
                            // Example: e.Skip(50).Take(int.MaxValue).
                            var enumerable = GetOrCreate(new AsyncEnumerator<TSource>(this, enumerableId + 1), 0, count - 1);
                            return new AsyncEnumerable<TSource>(enumerable);
                        }
                    }
                    else if ((uint) newMaxIndex >= (uint) _maxIndexInclusive)
                    {
                        // We make no modifications if attempting to take more items than we're already going to yield.
                        return new AsyncEnumerable<TSource>(this);
                    }

                    _maxIndexInclusive = newMaxIndex;
                    return new AsyncEnumerable<TSource>(this);
                }
            }
        } // class AsyncEnumerablePartitionFromLast<TSource>


#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct TakeRangeIterator<TSource> : IAsyncIterator<TSource>
        {
            private readonly AsyncEnumerator<TSource> _source;
            private readonly int _startFromEndIndex;
            private readonly int _endFromStartIndex;

            internal TakeRangeIterator(AsyncEnumerator<TSource> source, int startFromEndIndex, int endFromStartIndex)
            {
                _source = source;
                _startFromEndIndex = startFromEndIndex;
                _endFromStartIndex = endFromStartIndex;
            }

            public Promise DisposeAsyncWithoutStart()
                => _source.DisposeAsync();

            public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _source._target._cancelationToken = cancelationToken;

                try
                {
                    // Make sure at least 1 element exists before creating the queue.
                    if (!await _source.MoveNextAsync())
                    {
                        return;
                    }

                    var queue = new PoolBackedQueue<TSource>(1);
                    queue.Enqueue(_source.Current);
                    int count = 1;

                    while (await _source.MoveNextAsync())
                    {
                        if (count < _startFromEndIndex)
                        {
                            queue.Enqueue(_source.Current);
                            ++count;
                        }
                        else
                        {
                            do
                            {
                                queue.Dequeue();
                                queue.Enqueue(_source.Current);
                                checked { ++count; }
                            } while (await _source.MoveNextAsync());
                            break;
                        }
                    }

                    int startIndex = System.Math.Max(0, count - _startFromEndIndex);
                    int endIndex = System.Math.Min(count, _endFromStartIndex);

                    for (; startIndex < endIndex; ++startIndex)
                    {
                        await writer.YieldAsync(queue.Dequeue());
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    await _source.DisposeAsync();
                }
            }
        }
    } // class Internal
#endif
} // namespace Proto.Promises