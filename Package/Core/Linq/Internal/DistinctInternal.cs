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
#if CSHARP_7_3_OR_NEWER
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
            private readonly struct DistinctSyncIterator<TSource, TEqualityComparer> : IAsyncIterator<TSource>
                where TEqualityComparer : IEqualityComparer<TSource>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TEqualityComparer _comparer;

                internal DistinctSyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TEqualityComparer comparer)
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
            {
                return AsyncEnumerable<TSource>.Create(new DistinctSyncIterator<TSource, TEqualityComparer>(asyncEnumerator, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredDistinctSyncIterator<TSource, TEqualityComparer> : IAsyncIterator<TSource>
                where TEqualityComparer : IEqualityComparer<TSource>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredDistinctSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

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
                        joinedCancelationSource.TryDispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> Distinct<TSource, TEqualityComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TEqualityComparer comparer)
                where TEqualityComparer : IEqualityComparer<TSource>
            {
                return AsyncEnumerable<TSource>.Create(new ConfiguredDistinctSyncIterator<TSource, TEqualityComparer>(configuredAsyncEnumerator, comparer));
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class DistinctByHelper<TKey>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct DistinctBySyncIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal DistinctBySyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
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
                            set.Add(_keySelector.Invoke(current));
                            await writer.YieldAsync(current);

                            while (await _asyncEnumerator.MoveNextAsync())
                            {
                                current = _asyncEnumerator.Current;
                                if (set.Add(_keySelector.Invoke(current)))
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
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<TSource>.Create(new DistinctBySyncIterator<TSource, TKeySelector, TEqualityComparer>(asyncEnumerator, keySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct DistinctByAsyncIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal DistinctByAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
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
                            set.Add(await _keySelector.Invoke(current));
                            await writer.YieldAsync(current);

                            while (await _asyncEnumerator.MoveNextAsync())
                            {
                                current = _asyncEnumerator.Current;
                                if (set.Add(await _keySelector.Invoke(current)))
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

            internal static AsyncEnumerable<TSource> DistinctByAwait<TSource, TKeySelector, TEqualityComparer>(
                AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<TSource>.Create(new DistinctByAsyncIterator<TSource, TKeySelector, TEqualityComparer>(asyncEnumerator, keySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredDistinctBySyncIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredDistinctBySyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

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
                            set.Add(_keySelector.Invoke(current));
                            await writer.YieldAsync(current);

                            while (await _configuredAsyncEnumerator.MoveNextAsync())
                            {
                                current = _configuredAsyncEnumerator.Current;
                                if (set.Add(_keySelector.Invoke(current)))
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
                        joinedCancelationSource.TryDispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> DistinctBy<TSource, TKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<TSource>.Create(new ConfiguredDistinctBySyncIterator<TSource, TKeySelector, TEqualityComparer>(asyncEnumerator, keySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredDistinctByAsyncIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredDistinctByAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

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
                            set.Add(await _keySelector.Invoke(current));
                            await writer.YieldAsync(current);

                            while (await _configuredAsyncEnumerator.MoveNextAsync())
                            {
                                current = _configuredAsyncEnumerator.Current;
                                if (set.Add(await _keySelector.Invoke(current)))
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
                        joinedCancelationSource.TryDispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> DistinctByAwait<TSource, TKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<TSource>.Create(new ConfiguredDistinctByAsyncIterator<TSource, TKeySelector, TEqualityComparer>(asyncEnumerator, keySelector, comparer));
            }
        } // class Lookup<TKey, TElement>
    } // class Internal
#endif
} // namespace Proto.Promises