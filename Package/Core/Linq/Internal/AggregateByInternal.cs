#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using Proto.Promises.Linq.Sources;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class AggregateByHelper<TKey>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct AggregateBySyncIterator<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator> : IAsyncIterator<KeyValuePair<TKey, TAccumulate>>
                where TKeySelector : IFunc<TSource, TKey>
                where TAccumulator : IFunc<TAccumulate, TSource, TAccumulate>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TAccumulate _seed;
                private readonly TAccumulator _accumulator;
                private readonly TEqualityComparer _comparer;

                internal AggregateBySyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TAccumulate seed, TAccumulator accumulator, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _seed = seed;
                    _keySelector = keySelector;
                    _accumulator = accumulator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, TAccumulate>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, TAccumulate, TEqualityComparer>(_comparer))
                        {
                            do
                            {
                                var element = _asyncEnumerator.Current;
                                var accNode = dict.GetOrCreateNode(_keySelector.Invoke(element), out bool exists);
                                accNode._value = _accumulator.Invoke(exists ? accNode._value : _seed, element);
                            } while (await _asyncEnumerator.MoveNextAsync());

                            // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var node = dict._lastNode;
                            do
                            {
                                node = node._nextNode;
                                await writer.YieldAsync(new KeyValuePair<TKey, TAccumulate>(node._key, node._value));
                            } while (node != dict._lastNode);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator>(AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector, TAccumulate seed, TAccumulator accumulator, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TAccumulator : IFunc<TAccumulate, TSource, TAccumulate>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, TAccumulate>>.Create(
                    new AggregateBySyncIterator<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator>(asyncEnumerator, keySelector, seed, accumulator, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct AggregateByAsyncIterator<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator> : IAsyncIterator<KeyValuePair<TKey, TAccumulate>>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TAccumulator : IFunc<TAccumulate, TSource, Promise<TAccumulate>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TAccumulate _seed;
                private readonly TAccumulator _accumulator;
                private readonly TEqualityComparer _comparer;

                internal AggregateByAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TAccumulate seed, TAccumulator accumulator, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _seed = seed;
                    _keySelector = keySelector;
                    _accumulator = accumulator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, TAccumulate>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, TAccumulate, TEqualityComparer>(_comparer))
                        {
                            do
                            {
                                var element = _asyncEnumerator.Current;
                                var accNode = dict.GetOrCreateNode(await _keySelector.Invoke(element), out bool exists);
                                accNode._value = await _accumulator.Invoke(exists ? accNode._value : _seed, element);
                            } while (await _asyncEnumerator.MoveNextAsync());

                            // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var node = dict._lastNode;
                            do
                            {
                                node = node._nextNode;
                                await writer.YieldAsync(new KeyValuePair<TKey, TAccumulate>(node._key, node._value));
                            } while (node != dict._lastNode);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateByAwait<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator>(AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector, TAccumulate seed, TAccumulator accumulator, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TAccumulator : IFunc<TAccumulate, TSource, Promise<TAccumulate>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, TAccumulate>>.Create(
                    new AggregateByAsyncIterator<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator>(asyncEnumerator, keySelector, seed, accumulator, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredAggregateBySyncIterator<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator> : IAsyncIterator<KeyValuePair<TKey, TAccumulate>>
                where TKeySelector : IFunc<TSource, TKey>
                where TAccumulator : IFunc<TAccumulate, TSource, TAccumulate>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TAccumulate _seed;
                private readonly TAccumulator _accumulator;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredAggregateBySyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TAccumulate seed, TAccumulator accumulator, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _seed = seed;
                    _keySelector = keySelector;
                    _accumulator = accumulator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, TAccumulate>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, TAccumulate, TEqualityComparer>(_comparer))
                        {
                            do
                            {
                                var element = _configuredAsyncEnumerator.Current;
                                var accNode = dict.GetOrCreateNode(_keySelector.Invoke(element), out bool exists);
                                accNode._value = _accumulator.Invoke(exists ? accNode._value : _seed, element);
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());

                            // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var node = dict._lastNode;
                            do
                            {
                                node = node._nextNode;
                                await writer.YieldAsync(new KeyValuePair<TKey, TAccumulate>(node._key, node._value));
                            } while (node != dict._lastNode);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector, TAccumulate seed, TAccumulator accumulator, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TAccumulator : IFunc<TAccumulate, TSource, TAccumulate>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, TAccumulate>>.Create(
                    new ConfiguredAggregateBySyncIterator<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator>(configuredAsyncEnumerator, keySelector, seed, accumulator, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredAggregateByAsyncIterator<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator> : IAsyncIterator<KeyValuePair<TKey, TAccumulate>>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TAccumulator : IFunc<TAccumulate, TSource, Promise<TAccumulate>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TAccumulate _seed;
                private readonly TAccumulator _accumulator;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredAggregateByAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TAccumulate seed, TAccumulator accumulator, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _seed = seed;
                    _keySelector = keySelector;
                    _accumulator = accumulator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, TAccumulate>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, TAccumulate, TEqualityComparer>(_comparer))
                        {
                            do
                            {
                                var element = _configuredAsyncEnumerator.Current;
                                // The key selector function could have switched context, make sure we're on the configured context before invoking the comparer and accumulator.
                                var key = await _keySelector.Invoke(element).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                                var accNode = dict.GetOrCreateNode(key, out bool exists);
                                accNode._value = await _accumulator.Invoke(exists ? accNode._value : _seed, element).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());

                            // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var node = dict._lastNode;
                            do
                            {
                                node = node._nextNode;
                                await writer.YieldAsync(new KeyValuePair<TKey, TAccumulate>(node._key, node._value));
                            } while (node != dict._lastNode);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateByAwait<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector, TAccumulate seed, TAccumulator accumulator, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TAccumulator : IFunc<TAccumulate, TSource, Promise<TAccumulate>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, TAccumulate>>.Create(
                    new ConfiguredAggregateByAsyncIterator<TSource, TAccumulate, TEqualityComparer, TKeySelector, TAccumulator>(configuredAsyncEnumerator, keySelector, seed, accumulator, comparer));
        } // class AggregateByHelper<TKey>

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class AggregateByHelper<TKey, TAccumulate>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct AggregateBySyncIterator<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator> : IAsyncIterator<KeyValuePair<TKey, TAccumulate>>
                where TKeySelector : IFunc<TSource, TKey>
                where TSeedSelector : IFunc<TKey, TAccumulate>
                where TAccumulator : IFunc<TAccumulate, TSource, TAccumulate>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TSeedSelector _seedSelector;
                private readonly TAccumulator _accumulator;
                private readonly TEqualityComparer _comparer;

                internal AggregateBySyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TSeedSelector seedSelector, TAccumulator accumulator, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _seedSelector = seedSelector;
                    _keySelector = keySelector;
                    _accumulator = accumulator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, TAccumulate>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, TAccumulate, TEqualityComparer>(_comparer))
                        {
                            do
                            {
                                var element = _asyncEnumerator.Current;
                                var key = _keySelector.Invoke(element);
                                var accNode = dict.GetOrCreateNode(key, out bool exists);
                                accNode._value = _accumulator.Invoke(exists ? accNode._value : _seedSelector.Invoke(key), element);
                            } while (await _asyncEnumerator.MoveNextAsync());

                            // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var node = dict._lastNode;
                            do
                            {
                                node = node._nextNode;
                                await writer.YieldAsync(new KeyValuePair<TKey, TAccumulate>(node._key, node._value));
                            } while (node != dict._lastNode);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector, TSeedSelector seedSelector, TAccumulator accumulator, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TSeedSelector : IFunc<TKey, TAccumulate>
                where TAccumulator : IFunc<TAccumulate, TSource, TAccumulate>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, TAccumulate>>.Create(
                    new AggregateBySyncIterator<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator>(asyncEnumerator, keySelector, seedSelector, accumulator, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct AggregateByAsyncIterator<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator> : IAsyncIterator<KeyValuePair<TKey, TAccumulate>>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TSeedSelector : IFunc<TKey, Promise<TAccumulate>>
                where TAccumulator : IFunc<TAccumulate, TSource, Promise<TAccumulate>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TSeedSelector _seedSelector;
                private readonly TAccumulator _accumulator;
                private readonly TEqualityComparer _comparer;

                internal AggregateByAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TSeedSelector seedSelector, TAccumulator accumulator, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _seedSelector = seedSelector;
                    _keySelector = keySelector;
                    _accumulator = accumulator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, TAccumulate>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, TAccumulate, TEqualityComparer>(_comparer))
                        {
                            do
                            {
                                var element = _asyncEnumerator.Current;
                                var key = await _keySelector.Invoke(element);
                                var accNode = dict.GetOrCreateNode(key, out bool exists);
                                accNode._value = await _accumulator.Invoke(exists ? accNode._value : await _seedSelector.Invoke(key), element);
                            } while (await _asyncEnumerator.MoveNextAsync());

                            // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var node = dict._lastNode;
                            do
                            {
                                node = node._nextNode;
                                await writer.YieldAsync(new KeyValuePair<TKey, TAccumulate>(node._key, node._value));
                            } while (node != dict._lastNode);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateByAwait<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector, TSeedSelector seedSelector, TAccumulator accumulator, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TSeedSelector : IFunc<TKey, Promise<TAccumulate>>
                where TAccumulator : IFunc<TAccumulate, TSource, Promise<TAccumulate>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, TAccumulate>>.Create(
                    new AggregateByAsyncIterator<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator>(asyncEnumerator, keySelector, seedSelector, accumulator, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredAggregateBySyncIterator<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator> : IAsyncIterator<KeyValuePair<TKey, TAccumulate>>
                where TKeySelector : IFunc<TSource, TKey>
                where TSeedSelector : IFunc<TKey, TAccumulate>
                where TAccumulator : IFunc<TAccumulate, TSource, TAccumulate>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TSeedSelector _seedSelector;
                private readonly TAccumulator _accumulator;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredAggregateBySyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TSeedSelector seedSelector, TAccumulator accumulator, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _seedSelector = seedSelector;
                    _keySelector = keySelector;
                    _accumulator = accumulator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, TAccumulate>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, TAccumulate, TEqualityComparer>(_comparer))
                        {
                            do
                            {
                                var element = _configuredAsyncEnumerator.Current;
                                var key = _keySelector.Invoke(element);
                                var accNode = dict.GetOrCreateNode(key, out bool exists);
                                accNode._value = _accumulator.Invoke(exists ? accNode._value : _seedSelector.Invoke(key), element);
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());

                            // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var node = dict._lastNode;
                            do
                            {
                                node = node._nextNode;
                                await writer.YieldAsync(new KeyValuePair<TKey, TAccumulate>(node._key, node._value));
                            } while (node != dict._lastNode);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector, TSeedSelector seedSelector, TAccumulator accumulator, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TSeedSelector : IFunc<TKey, TAccumulate>
                where TAccumulator : IFunc<TAccumulate, TSource, TAccumulate>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, TAccumulate>>.Create(
                    new ConfiguredAggregateBySyncIterator<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator>(configuredAsyncEnumerator, keySelector, seedSelector, accumulator, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredAggregateByAsyncIterator<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator> : IAsyncIterator<KeyValuePair<TKey, TAccumulate>>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TSeedSelector : IFunc<TKey, Promise<TAccumulate>>
                where TAccumulator : IFunc<TAccumulate, TSource, Promise<TAccumulate>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TSeedSelector _seedSelector;
                private readonly TAccumulator _accumulator;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredAggregateByAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TSeedSelector seedSelector, TAccumulator accumulator, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _seedSelector = seedSelector;
                    _keySelector = keySelector;
                    _accumulator = accumulator;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, TAccumulate>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, TAccumulate, TEqualityComparer>(_comparer))
                        {
                            do
                            {
                                var element = _configuredAsyncEnumerator.Current;
                                // The key selector function could have switched context, make sure we're on the configured context before invoking the comparer and seed selector.
                                var key = await _keySelector.Invoke(element).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                                var accNode = dict.GetOrCreateNode(key, out bool exists);
                                TAccumulate acc;
                                if (exists)
                                {
                                    acc = accNode._value;
                                }
                                else
                                {
                                    // The seed selector function could have switched context, make sure we're on the configured context before invoking the accumulator.
                                    acc = await _seedSelector.Invoke(key).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                                }
                                accNode._value = await _accumulator.Invoke(acc, element).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());

                            // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var node = dict._lastNode;
                            do
                            {
                                node = node._nextNode;
                                await writer.YieldAsync(new KeyValuePair<TKey, TAccumulate>(node._key, node._value));
                            } while (node != dict._lastNode);
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateByAwait<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector, TSeedSelector seedSelector, TAccumulator accumulator, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TSeedSelector : IFunc<TKey, Promise<TAccumulate>>
                where TAccumulator : IFunc<TAccumulate, TSource, Promise<TAccumulate>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, TAccumulate>>.Create(
                    new ConfiguredAggregateByAsyncIterator<TSource, TEqualityComparer, TKeySelector, TSeedSelector, TAccumulator>(configuredAsyncEnumerator, keySelector, seedSelector, accumulator, comparer));
        } // class AggregateByHelper<TKey, TAccumulate>
    } // class Internal
} // namespace Proto.Promises