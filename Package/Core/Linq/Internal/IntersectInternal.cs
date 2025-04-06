#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class IntersectHelper
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct IntersectIterator<TSource, TEqualityComparer> : IAsyncIterator<TSource>
                where TEqualityComparer : IEqualityComparer<TSource>
            {
                private readonly AsyncEnumerator<TSource> _firstAsyncEnumerator;
                private readonly AsyncEnumerator<TSource> _secondAsyncEnumerator;
                private readonly TEqualityComparer _comparer;

                internal IntersectIterator(AsyncEnumerator<TSource> firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TEqualityComparer comparer)
                {
                    _firstAsyncEnumerator = firstAsyncEnumerator;
                    _secondAsyncEnumerator = secondAsyncEnumerator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _firstAsyncEnumerator._target._cancelationToken = cancelationToken;
                    _secondAsyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        using (var set = new PoolBackedSet<TSource, TEqualityComparer>(_comparer))
                        {
                            while (await _secondAsyncEnumerator.MoveNextAsync())
                            {
                                set.Add(_secondAsyncEnumerator.Current);
                            }

                            while (await _firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = _firstAsyncEnumerator.Current;
                                if (set.Remove(element))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        try
                        {
                            await _secondAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _firstAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                {
                    // We consume less memory by using .Finally instead of async/await.
                    return _secondAsyncEnumerator.DisposeAsync()
                        .Finally(_firstAsyncEnumerator, e => e.DisposeAsync());
                }
            }

            internal static AsyncEnumerable<TSource> Intersect<TSource, TEqualityComparer>(AsyncEnumerator<TSource> firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TEqualityComparer comparer)
                where TEqualityComparer : IEqualityComparer<TSource>
                => AsyncEnumerable<TSource>.Create(new IntersectIterator<TSource, TEqualityComparer>(firstAsyncEnumerator, secondAsyncEnumerator, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredIntersectIterator<TSource, TEqualityComparer> : IAsyncIterator<TSource>
                where TEqualityComparer : IEqualityComparer<TSource>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _firstAsyncEnumerator;
                private readonly AsyncEnumerator<TSource> _secondAsyncEnumerator;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredIntersectIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TEqualityComparer comparer)
                {
                    _firstAsyncEnumerator = firstAsyncEnumerator;
                    _secondAsyncEnumerator = secondAsyncEnumerator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _firstAsyncEnumerator._enumerator._target._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _secondAsyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        using (var set = new PoolBackedSet<TSource, TEqualityComparer>(_comparer))
                        {
                            // We need to make sure we're on the configured context before invoking the comparer.
                            while (await _secondAsyncEnumerator.MoveNextAsync().ConfigureAwait(_firstAsyncEnumerator.ContinuationOptions))
                            {
                                set.Add(_secondAsyncEnumerator.Current);
                            }

                            while (await _firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = _firstAsyncEnumerator.Current;
                                if (set.Remove(element))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        try
                        {
                            await _secondAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _firstAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                {
                    // We consume less memory by using .Finally instead of async/await.
                    return _secondAsyncEnumerator.DisposeAsync()
                        .Finally(_firstAsyncEnumerator, e => e.DisposeAsync());
                }
            }

            internal static AsyncEnumerable<TSource> Intersect<TSource, TEqualityComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TEqualityComparer comparer)
                where TEqualityComparer : IEqualityComparer<TSource>
                => AsyncEnumerable<TSource>.Create(new ConfiguredIntersectIterator<TSource, TEqualityComparer>(firstAsyncEnumerator, secondAsyncEnumerator, comparer));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class IntersectByHelper<TKey>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct IntersectByIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _firstAsyncEnumerator;
                private readonly AsyncEnumerator<TSource> _secondAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal IntersectByIterator(AsyncEnumerator<TSource> firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _firstAsyncEnumerator = firstAsyncEnumerator;
                    _secondAsyncEnumerator = secondAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _firstAsyncEnumerator._target._cancelationToken = cancelationToken;
                    _secondAsyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        using (var set = new PoolBackedSet<TKey, TEqualityComparer>(_comparer))
                        {
                            while (await _secondAsyncEnumerator.MoveNextAsync())
                            {
                                set.Add(await _keySelector.Invoke(_secondAsyncEnumerator.Current, cancelationToken));
                            }

                            while (await _firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = _firstAsyncEnumerator.Current;
                                if (set.Remove(await _keySelector.Invoke(element, cancelationToken)))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        try
                        {
                            await _secondAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _firstAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                {
                    // We consume less memory by using .Finally instead of async/await.
                    return _secondAsyncEnumerator.DisposeAsync()
                        .Finally(_firstAsyncEnumerator, e => e.DisposeAsync());
                }
            }

            internal static AsyncEnumerable<TSource> IntersectBy<TSource, TKeySelector, TEqualityComparer>(
                AsyncEnumerator<TSource> firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create(new IntersectByIterator<TSource, TKeySelector, TEqualityComparer>(firstAsyncEnumerator, secondAsyncEnumerator, keySelector, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredIntersectByIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _firstAsyncEnumerator;
                private readonly AsyncEnumerator<TSource> _secondAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredIntersectByIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator firstAsyncEnumerator,
                    AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _firstAsyncEnumerator = firstAsyncEnumerator;
                    _secondAsyncEnumerator = secondAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _firstAsyncEnumerator._enumerator._target._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _secondAsyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        using (var set = new PoolBackedSet<TKey, TEqualityComparer>(_comparer))
                        {
                            // We need to make sure we're on the configured context before invoking the key selector.
                            while (await _secondAsyncEnumerator.MoveNextAsync().ConfigureAwait(_firstAsyncEnumerator.ContinuationOptions))
                            {
                                // In case the key selector changed context, we need to make sure we're on the configured context before invoking the comparer.
                                var key = await _keySelector.Invoke(_secondAsyncEnumerator.Current, cancelationToken).ConfigureAwait(_firstAsyncEnumerator.ContinuationOptions);
                                set.Add(key);
                            }

                            while (await _firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = _firstAsyncEnumerator.Current;
                                // In case the key selector changed context, we need to make sure we're on the configured context before invoking the comparer.
                                var key = await _keySelector.Invoke(element, cancelationToken).ConfigureAwait(_firstAsyncEnumerator.ContinuationOptions);
                                if (set.Remove(key))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        try
                        {
                            await _secondAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _firstAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                {
                    // We consume less memory by using .Finally instead of async/await.
                    return _secondAsyncEnumerator.DisposeAsync()
                        .Finally(_firstAsyncEnumerator, e => e.DisposeAsync());
                }
            }

            internal static AsyncEnumerable<TSource> IntersectBy<TSource, TKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create(new ConfiguredIntersectByIterator<TSource, TKeySelector, TEqualityComparer>(firstAsyncEnumerator, secondAsyncEnumerator, keySelector, comparer));
        } // class Lookup<TKey, TElement>
    } // class Internal
} // namespace Proto.Promises