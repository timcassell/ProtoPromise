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
        internal static class DistinctHelper
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct DistinctIterator<TSource, TEqualityComparer> : IAsyncIterator<TSource>
                where TEqualityComparer : IEqualityComparer<TSource>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TEqualityComparer _comparer;

                internal DistinctIterator(AsyncEnumerator<TSource> asyncEnumerator, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var set = new PoolBackedSet<TSource, TEqualityComparer>(_comparer))
                        {
                            var current = _asyncEnumerator.Current;
                            set.Add(current);
                            await writer.YieldAsync(current);

                            while (await _asyncEnumerator.MoveNextAsync())
                            {
                                current = _asyncEnumerator.Current;
                                if (set.Add(current))
                                {
                                    await writer.YieldAsync(current);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> Distinct<TSource, TEqualityComparer>(AsyncEnumerator<TSource> asyncEnumerator, TEqualityComparer comparer)
                where TEqualityComparer : IEqualityComparer<TSource>
                => AsyncEnumerable<TSource>.Create(new DistinctIterator<TSource, TEqualityComparer>(asyncEnumerator, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredDistinctIterator<TSource, TEqualityComparer> : IAsyncIterator<TSource>
                where TEqualityComparer : IEqualityComparer<TSource>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredDistinctIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var set = new PoolBackedSet<TSource, TEqualityComparer>(_comparer))
                        {
                            var current = _configuredAsyncEnumerator.Current;
                            set.Add(current);
                            await writer.YieldAsync(current);

                            while (await _configuredAsyncEnumerator.MoveNextAsync())
                            {
                                current = _configuredAsyncEnumerator.Current;
                                if (set.Add(current))
                                {
                                    await writer.YieldAsync(current);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> Distinct<TSource, TEqualityComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TEqualityComparer comparer)
                where TEqualityComparer : IEqualityComparer<TSource>
                => AsyncEnumerable<TSource>.Create(new ConfiguredDistinctIterator<TSource, TEqualityComparer>(configuredAsyncEnumerator, comparer));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class DistinctByHelper<TKey>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct DistinctByIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal DistinctByIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var set = new PoolBackedSet<TKey, TEqualityComparer>(_comparer))
                        {
                            var current = _asyncEnumerator.Current;
                            set.Add(await _keySelector.Invoke(current, cancelationToken));
                            await writer.YieldAsync(current);

                            while (await _asyncEnumerator.MoveNextAsync())
                            {
                                current = _asyncEnumerator.Current;
                                if (set.Add(await _keySelector.Invoke(current, cancelationToken)))
                                {
                                    await writer.YieldAsync(current);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> DistinctBy<TSource, TKeySelector, TEqualityComparer>(
                AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create(new DistinctByIterator<TSource, TKeySelector, TEqualityComparer>(asyncEnumerator, keySelector, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredDistinctByIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredDistinctByIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var set = new PoolBackedSet<TKey, TEqualityComparer>(_comparer))
                        {
                            var current = _configuredAsyncEnumerator.Current;
                            // In case the key selector changed context, we need to make sure we're on the configured context before invoking the comparer.
                            var key = await _keySelector.Invoke(current, cancelationToken).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                            set.Add(key);
                            await writer.YieldAsync(current);

                            while (await _configuredAsyncEnumerator.MoveNextAsync())
                            {
                                current = _configuredAsyncEnumerator.Current;
                                // In case the key selector changed context, we need to make sure we're on the configured context before invoking the comparer.
                                key = await _keySelector.Invoke(current, cancelationToken).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                                if (set.Add(key))
                                {
                                    await writer.YieldAsync(current);
                                }
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> DistinctBy<TSource, TKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<TSource>.Create(new ConfiguredDistinctByIterator<TSource, TKeySelector, TEqualityComparer>(asyncEnumerator, keySelector, comparer));
        } // class Lookup<TKey, TElement>
    } // class Internal
} // namespace Proto.Promises