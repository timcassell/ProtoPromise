#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0090 // Use 'new(...)'

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
            private int _minIndexInclusive;
            private int _maxIndexInclusive;

            private bool HasLowerLimit
            {
                [MethodImpl(InlineOption)]
                get => _minIndexInclusive != -1;
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
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _source._target._cancelationToken = _cancelationToken;
                var iteratorPromise = Iterate(enumerableId)._promise;
                if (iteratorPromise._ref == null)
                {
                    // Already complete.
                    HandleFromSynchronouslyCompletedIterator();
                    return;
                }

                // We only set _previous to support circular await detection.
                // We don't set _rejectContainerOrPreviousOrLink to prevent progress subscriptions from going down the chain, because progress is meaningless for AsyncEnumerable.
#if PROMISE_DEBUG
                _previous = iteratorPromise._ref;
#endif
                // We hook this up directly to the returned promise so we can know when the iteration is complete, and use this for the DisposeAsync promise.
                iteratorPromise._ref.HookupExistingWaiter(iteratorPromise._id, this);
            }

            private async AsyncIteratorMethod Iterate(int streamWriterId)
            {
                try
                {
                    int index = 0;
                    while (index < _minIndexInclusive)
                    {
                        if (!await _source.MoveNextAsync())
                        {
                            // Reached the end before we finished skipping.
                            return;
                        }
                        ++index;
                    }

                    if (HasUpperLimit)
                    {
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

            public AsyncEnumerable<TSource> Skip(int enumerableId, int count)
            {
                IncrementEnumerableId(enumerableId);

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
                        // If newMinIndex is greater than max, no elements will be yielded, so we can simply return an empty AsyncEnumerable.
                        // But we have to dispose the source, so we need to do it with a special empty, instead of AsyncEnumerable<TSource>.Empty().
                        SetStateForDisposeWithoutStart();
                        var source = _source;
                        DisposeAndReturnToPool();
                        return EmptyHelper.EmptyWithDispose(source);
                    }

                    _minIndexInclusive = newMinIndex;
                    return new AsyncEnumerable<TSource>(this);
                }
            }

            public AsyncEnumerable<TSource> Take(int enumerableId, int count)
            {
                IncrementEnumerableId(enumerableId);

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
        }
    } // class Internal
#endif
} // namespace Proto.Promises