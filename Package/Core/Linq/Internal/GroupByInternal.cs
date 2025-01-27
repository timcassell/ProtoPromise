#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
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
        internal static class GroupByHelper<TKey, TElement>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct GroupByKeyElementSyncIterator<TSource, TKeySelector, TElementSelector, TEqualityComparer> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TElementSelector _elementSelector;
                private readonly TEqualityComparer _comparer;

                internal GroupByKeyElementSyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TElementSelector elementSelector, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _elementSelector = elementSelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        using (var lookup = new LookupImpl<TKey, TElement, TEqualityComparer>(_comparer, true))
                        {
                            do
                            {
                                var item = _asyncEnumerator.Current;
                                var key = _keySelector.Invoke(item);
                                var group = lookup.GetOrCreateGrouping(key, true);

                                var element = _elementSelector.Invoke(item);
                                group.Add(element);
                            } while (await _asyncEnumerator.MoveNextAsync());
                            // We don't dispose the source enumerator until the owner is disposed.
                            // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                            // We don't need to check if g is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var g = lookup._lastGrouping;
                            do
                            {
                                g = g._nextGrouping;
                                await writer.YieldAsync(new Linq.Grouping<TKey, TElement>(g));
                            } while (g != lookup._lastGrouping);
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
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

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupBy<TSource, TKeySelector, TElementSelector, TEqualityComparer>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<Linq.Grouping<TKey, TElement>>.Create(
                    new GroupByKeyElementSyncIterator<TSource, TKeySelector, TElementSelector, TEqualityComparer>(asyncEnumerator, keySelector, elementSelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct GroupByKeySyncIterator<TKeySelector, TEqualityComparer> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TElement, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TElement> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal GroupByKeySyncIterator(AsyncEnumerator<TElement> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        using (var lookup = new LookupImpl<TKey, TElement, TEqualityComparer>(_comparer, true))
                        {
                            do
                            {
                                var item = _asyncEnumerator.Current;
                                var key = _keySelector.Invoke(item);
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            } while (await _asyncEnumerator.MoveNextAsync());
                            // We don't dispose the source enumerator until the owner is disposed.
                            // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                            // We don't need to check if g is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var g = lookup._lastGrouping;
                            do
                            {
                                g = g._nextGrouping;
                                await writer.YieldAsync(new Linq.Grouping<TKey, TElement>(g));
                            } while (g != lookup._lastGrouping);
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
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

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupBy<TKeySelector, TEqualityComparer>(
                AsyncEnumerator<TElement> asyncEnumerator,
                TKeySelector keySelector,
                TEqualityComparer comparer)
                where TKeySelector : IFunc<TElement, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<Linq.Grouping<TKey, TElement>>.Create(
                    new GroupByKeySyncIterator<TKeySelector, TEqualityComparer>(asyncEnumerator, keySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct GroupByKeyElementAsyncIterator<TSource, TKeySelector, TElementSelector, TEqualityComparer> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TElementSelector _elementSelector;
                private readonly TEqualityComparer _comparer;

                internal GroupByKeyElementAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TElementSelector elementSelector, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _elementSelector = elementSelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        using (var lookup = new LookupImpl<TKey, TElement, TEqualityComparer>(_comparer, true))
                        {
                            do
                            {
                                var item = _asyncEnumerator.Current;
                                var key = await _keySelector.Invoke(item);
                                var group = lookup.GetOrCreateGrouping(key, true);

                                var element = await _elementSelector.Invoke(item);
                                group.Add(element);
                            } while (await _asyncEnumerator.MoveNextAsync());
                            // We don't dispose the source enumerator until the owner is disposed.
                            // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                            // We don't need to check if g is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var g = lookup._lastGrouping;
                            do
                            {
                                g = g._nextGrouping;
                                await writer.YieldAsync(new Linq.Grouping<TKey, TElement>(g));
                            } while (g != lookup._lastGrouping);
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
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

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupByAwait<TSource, TKeySelector, TElementSelector, TEqualityComparer>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<Linq.Grouping<TKey, TElement>>.Create(
                    new GroupByKeyElementAsyncIterator<TSource, TKeySelector, TElementSelector, TEqualityComparer>(asyncEnumerator, keySelector, elementSelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct GroupByKeyAsyncIterator<TKeySelector, TEqualityComparer> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TElement, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TElement> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal GroupByKeyAsyncIterator(AsyncEnumerator<TElement> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        using (var lookup = new LookupImpl<TKey, TElement, TEqualityComparer>(_comparer, true))
                        {
                            do
                            {
                                var item = _asyncEnumerator.Current;
                                var key = await _keySelector.Invoke(item);
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            } while (await _asyncEnumerator.MoveNextAsync());
                            // We don't dispose the source enumerator until the owner is disposed.
                            // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                            // We don't need to check if g is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var g = lookup._lastGrouping;
                            do
                            {
                                g = g._nextGrouping;
                                await writer.YieldAsync(new Linq.Grouping<TKey, TElement>(g));
                            } while (g != lookup._lastGrouping);
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
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

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupByAwait<TKeySelector, TEqualityComparer>(
                AsyncEnumerator<TElement> asyncEnumerator,
                TKeySelector keySelector,
                TEqualityComparer comparer)
                where TKeySelector : IFunc<TElement, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<Linq.Grouping<TKey, TElement>>.Create(
                    new GroupByKeyAsyncIterator<TKeySelector, TEqualityComparer>(asyncEnumerator, keySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredGroupByKeyElementSyncIterator<TSource, TKeySelector, TElementSelector, TEqualityComparer> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TElementSelector _elementSelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredGroupByKeyElementSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TElementSelector elementSelector, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _elementSelector = elementSelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        using (var lookup = new LookupImpl<TKey, TElement, TEqualityComparer>(_comparer, true))
                        {
                            do
                            {
                                var item = _configuredAsyncEnumerator.Current;
                                var key = _keySelector.Invoke(item);
                                var group = lookup.GetOrCreateGrouping(key, true);

                                var element = _elementSelector.Invoke(item);
                                group.Add(element);
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());
                            // We don't dispose the source enumerator until the owner is disposed.
                            // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                            // We don't need to check if g is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var g = lookup._lastGrouping;
                            do
                            {
                                g = g._nextGrouping;
                                await writer.YieldAsync(new Linq.Grouping<TKey, TElement>(g));
                            } while (g != lookup._lastGrouping);
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupBy<TSource, TKeySelector, TElementSelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<Linq.Grouping<TKey, TElement>>.Create(
                    new ConfiguredGroupByKeyElementSyncIterator<TSource, TKeySelector, TElementSelector, TEqualityComparer>(configuredAsyncEnumerator, keySelector, elementSelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredGroupByKeySyncIterator<TKeySelector, TEqualityComparer> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TElement, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TElement>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredGroupByKeySyncIterator(ConfiguredAsyncEnumerable<TElement>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        using (var lookup = new LookupImpl<TKey, TElement, TEqualityComparer>(_comparer, true))
                        {
                            do
                            {
                                var item = _configuredAsyncEnumerator.Current;
                                var key = _keySelector.Invoke(item);
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());
                            // We don't dispose the source enumerator until the owner is disposed.
                            // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                            // We don't need to check if g is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var g = lookup._lastGrouping;
                            do
                            {
                                g = g._nextGrouping;
                                await writer.YieldAsync(new Linq.Grouping<TKey, TElement>(g));
                            } while (g != lookup._lastGrouping);
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupBy<TKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TElement>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                TEqualityComparer comparer)
                where TKeySelector : IFunc<TElement, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<Linq.Grouping<TKey, TElement>>.Create(
                    new ConfiguredGroupByKeySyncIterator<TKeySelector, TEqualityComparer>(configuredAsyncEnumerator, keySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredGroupByKeyElementAsyncIterator<TSource, TKeySelector, TElementSelector, TEqualityComparer> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TElementSelector _elementSelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredGroupByKeyElementAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TElementSelector elementSelector, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _elementSelector = elementSelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        using (var lookup = new LookupImpl<TKey, TElement, TEqualityComparer>(_comparer, true))
                        {
                            do
                            {
                                var item = _configuredAsyncEnumerator.Current;
                                // In case the key selector changed context, we need to make sure we're on the configured context before invoking the comparer and elementSelector.
                                var key = await _keySelector.Invoke(item).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                                var group = lookup.GetOrCreateGrouping(key, true);

                                var element = await _elementSelector.Invoke(item);
                                group.Add(element);
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());
                            // We don't dispose the source enumerator until the owner is disposed.
                            // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                            // We don't need to check if g is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var g = lookup._lastGrouping;
                            do
                            {
                                g = g._nextGrouping;
                                await writer.YieldAsync(new Linq.Grouping<TKey, TElement>(g));
                            } while (g != lookup._lastGrouping);
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupByAwait<TSource, TKeySelector, TElementSelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<Linq.Grouping<TKey, TElement>>.Create(
                    new ConfiguredGroupByKeyElementAsyncIterator<TSource, TKeySelector, TElementSelector, TEqualityComparer>(configuredAsyncEnumerator, keySelector, elementSelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredGroupByKeyAsyncIterator<TKeySelector, TEqualityComparer> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TElement, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TElement>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredGroupByKeyAsyncIterator(ConfiguredAsyncEnumerable<TElement>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        using (var lookup = new LookupImpl<TKey, TElement, TEqualityComparer>(_comparer, true))
                        {
                            do
                            {
                                var item = _configuredAsyncEnumerator.Current;
                                // In case the key selector changed context, we need to make sure we're on the configured context before invoking the comparer.
                                var key = await _keySelector.Invoke(item).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());
                            // We don't dispose the source enumerator until the owner is disposed.
                            // This is in case the source enumerator contains TempCollection that they will still be valid until the owner is disposed.

                            // We don't need to check if g is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var g = lookup._lastGrouping;
                            do
                            {
                                g = g._nextGrouping;
                                await writer.YieldAsync(new Linq.Grouping<TKey, TElement>(g));
                            } while (g != lookup._lastGrouping);
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

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupByAwait<TKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TElement>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                TEqualityComparer comparer)
                where TKeySelector : IFunc<TElement, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<Linq.Grouping<TKey, TElement>>.Create(
                    new ConfiguredGroupByKeyAsyncIterator<TKeySelector, TEqualityComparer>(configuredAsyncEnumerator, keySelector, comparer));
            }
        } // class Lookup<TKey, TElement>
    } // class Internal
} // namespace Proto.Promises