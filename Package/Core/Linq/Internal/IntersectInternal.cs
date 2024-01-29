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
        internal static class IntersectHelper
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct IntersectSyncIterator<TSource, TEqualityComparer> : IAsyncIterator<TSource>
                where TEqualityComparer : IEqualityComparer<TSource>
            {
                private readonly AsyncEnumerator<TSource> _firstAsyncEnumerator;
                private readonly AsyncEnumerator<TSource> _secondAsyncEnumerator;
                private readonly TEqualityComparer _comparer;

                internal IntersectSyncIterator(AsyncEnumerator<TSource> firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TEqualityComparer comparer)
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
            {
                return AsyncEnumerable<TSource>.Create(new IntersectSyncIterator<TSource, TEqualityComparer>(firstAsyncEnumerator, secondAsyncEnumerator, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredIntersectSyncIterator<TSource, TEqualityComparer> : IAsyncIterator<TSource>
                where TEqualityComparer : IEqualityComparer<TSource>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _firstAsyncEnumerator;
                private readonly AsyncEnumerator<TSource> _secondAsyncEnumerator;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredIntersectSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TEqualityComparer comparer)
                {
                    _firstAsyncEnumerator = firstAsyncEnumerator;
                    _secondAsyncEnumerator = secondAsyncEnumerator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _firstAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _secondAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    try
                    {
                        using (var set = new PoolBackedSet<TSource, TEqualityComparer>(_comparer))
                        {
                            while (await _secondAsyncEnumerator.MoveNextAsync())
                            {
                                // We need to make sure we're on the configured context before invoking the comparer.
                                await _firstAsyncEnumerator.SwitchToContext();
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
                        joinedCancelationSource.TryDispose();
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
            {
                return AsyncEnumerable<TSource>.Create(new ConfiguredIntersectSyncIterator<TSource, TEqualityComparer>(firstAsyncEnumerator, secondAsyncEnumerator, comparer));
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class IntersectByHelper<TKey>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct IntersectBySyncIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _firstAsyncEnumerator;
                private readonly AsyncEnumerator<TSource> _secondAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal IntersectBySyncIterator(AsyncEnumerator<TSource> firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
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
                                set.Add(_keySelector.Invoke(_secondAsyncEnumerator.Current));
                            }

                            while (await _firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = _firstAsyncEnumerator.Current;
                                if (set.Remove(_keySelector.Invoke(element)))
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
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<TSource>.Create(new IntersectBySyncIterator<TSource, TKeySelector, TEqualityComparer>(firstAsyncEnumerator, secondAsyncEnumerator, keySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct IntersectByAsyncIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _firstAsyncEnumerator;
                private readonly AsyncEnumerator<TSource> _secondAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal IntersectByAsyncIterator(AsyncEnumerator<TSource> firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
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
                                set.Add(await _keySelector.Invoke(_secondAsyncEnumerator.Current));
                            }

                            while (await _firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = _firstAsyncEnumerator.Current;
                                if (set.Remove(await _keySelector.Invoke(element)))
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

            internal static AsyncEnumerable<TSource> IntersectByAwait<TSource, TKeySelector, TEqualityComparer>(
                AsyncEnumerator<TSource> firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<TSource>.Create(new IntersectByAsyncIterator<TSource, TKeySelector, TEqualityComparer>(firstAsyncEnumerator, secondAsyncEnumerator, keySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredIntersectBySyncIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _firstAsyncEnumerator;
                private readonly AsyncEnumerator<TSource> _secondAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredIntersectBySyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator firstAsyncEnumerator,
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
                    var enumerableRef = _firstAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _secondAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    try
                    {
                        using (var set = new PoolBackedSet<TKey, TEqualityComparer>(_comparer))
                        {
                            while (await _secondAsyncEnumerator.MoveNextAsync())
                            {
                                // We need to make sure we're on the configured context before invoking the key selector.
                                await _firstAsyncEnumerator.SwitchToContext();
                                set.Add(_keySelector.Invoke(_secondAsyncEnumerator.Current));
                            }

                            while (await _firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = _firstAsyncEnumerator.Current;
                                if (set.Remove(_keySelector.Invoke(element)))
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
                        joinedCancelationSource.TryDispose();
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
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<TSource>.Create(new ConfiguredIntersectBySyncIterator<TSource, TKeySelector, TEqualityComparer>(firstAsyncEnumerator, secondAsyncEnumerator, keySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredIntersectByAsyncIterator<TSource, TKeySelector, TEqualityComparer> : IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _firstAsyncEnumerator;
                private readonly AsyncEnumerator<TSource> _secondAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredIntersectByAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator firstAsyncEnumerator,
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
                    var enumerableRef = _firstAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _secondAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    try
                    {
                        using (var set = new PoolBackedSet<TKey, TEqualityComparer>(_comparer))
                        {
                            while (await _secondAsyncEnumerator.MoveNextAsync())
                            {
                                // We need to make sure we're on the configured context before invoking the key selector.
                                await _firstAsyncEnumerator.SwitchToContext();
                                var key = await _keySelector.Invoke(_secondAsyncEnumerator.Current);
                                // In case the key selector changed context, we need to make sure we're on the configured context before invoking the comparer.
                                await _firstAsyncEnumerator.SwitchToContext();
                                set.Add(key);
                            }

                            while (await _firstAsyncEnumerator.MoveNextAsync())
                            {
                                var element = _firstAsyncEnumerator.Current;
                                var key = await _keySelector.Invoke(element);
                                // In case the key selector changed context, we need to make sure we're on the configured context before invoking the comparer.
                                await _firstAsyncEnumerator.SwitchToContext();
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
                        joinedCancelationSource.TryDispose();
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

            internal static AsyncEnumerable<TSource> IntersectByAwait<TSource, TKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator firstAsyncEnumerator, AsyncEnumerator<TSource> secondAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<TSource>.Create(new ConfiguredIntersectByAsyncIterator<TSource, TKeySelector, TEqualityComparer>(firstAsyncEnumerator, secondAsyncEnumerator, keySelector, comparer));
            }
        } // class Lookup<TKey, TElement>
    } // class Internal
#endif
} // namespace Proto.Promises