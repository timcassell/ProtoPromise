#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using Proto.Promises.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class GroupByHelper<TKey, TElement>
        {
            private readonly struct GroupByKeyElementSyncIterator<TSource, TKeySelector, TElementSelector> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TElementSelector _elementSelector;
                private readonly IEqualityComparer<TKey> _comparer;

                internal GroupByKeyElementSyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TElementSelector elementSelector, IEqualityComparer<TKey> comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _elementSelector = elementSelector;
                    _comparer = comparer;
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncFromNeverStarted()
                {
                    return _asyncEnumerator.DisposeAsync();
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TElement> lookup = default;
                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        lookup = new LookupImpl<TKey, TElement>(_comparer, true);
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

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        lookup.Dispose();
                        await _asyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupBy<TSource, TKeySelector, TElementSelector>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
            {
                var enumerable = AsyncEnumerableCreate<Linq.Grouping<TKey, TElement>, GroupByKeyElementSyncIterator<TSource, TKeySelector, TElementSelector>>.GetOrCreate(
                    new GroupByKeyElementSyncIterator<TSource, TKeySelector, TElementSelector>(asyncEnumerator, keySelector, elementSelector, comparer));
                return new AsyncEnumerable<Linq.Grouping<TKey, TElement>>(enumerable);
            }

            private readonly struct GroupByKeySyncIterator<TKeySelector> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TElement, TKey>
            {
                private readonly AsyncEnumerator<TElement> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly IEqualityComparer<TKey> _comparer;

                internal GroupByKeySyncIterator(AsyncEnumerator<TElement> asyncEnumerator, TKeySelector keySelector, IEqualityComparer<TKey> comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncFromNeverStarted()
                {
                    return _asyncEnumerator.DisposeAsync();
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TElement> lookup = default;
                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        lookup = new LookupImpl<TKey, TElement>(_comparer, true);
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

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        lookup.Dispose();
                        await _asyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupBy<TKeySelector>(
                AsyncEnumerator<TElement> asyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TElement, TKey>
            {
                var enumerable = AsyncEnumerableCreate<Linq.Grouping<TKey, TElement>, GroupByKeySyncIterator<TKeySelector>>.GetOrCreate(
                    new GroupByKeySyncIterator<TKeySelector>(asyncEnumerator, keySelector, comparer));
                return new AsyncEnumerable<Linq.Grouping<TKey, TElement>>(enumerable);
            }

            private readonly struct GroupByKeyElementAsyncIterator<TSource, TKeySelector, TElementSelector> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TElementSelector _elementSelector;
                private readonly IEqualityComparer<TKey> _comparer;

                internal GroupByKeyElementAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TElementSelector elementSelector, IEqualityComparer<TKey> comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _elementSelector = elementSelector;
                    _comparer = comparer;
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncFromNeverStarted()
                {
                    return _asyncEnumerator.DisposeAsync();
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TElement> lookup = default;
                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        lookup = new LookupImpl<TKey, TElement>(_comparer, true);
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

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        lookup.Dispose();
                        await _asyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupByAwait<TSource, TKeySelector, TElementSelector>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
            {
                var enumerable = AsyncEnumerableCreate<Linq.Grouping<TKey, TElement>, GroupByKeyElementAsyncIterator<TSource, TKeySelector, TElementSelector>>.GetOrCreate(
                    new GroupByKeyElementAsyncIterator<TSource, TKeySelector, TElementSelector>(asyncEnumerator, keySelector, elementSelector, comparer));
                return new AsyncEnumerable<Linq.Grouping<TKey, TElement>>(enumerable);
            }

            private readonly struct GroupByKeyAsyncIterator<TKeySelector> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TElement, Promise<TKey>>
            {
                private readonly AsyncEnumerator<TElement> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly IEqualityComparer<TKey> _comparer;

                internal GroupByKeyAsyncIterator(AsyncEnumerator<TElement> asyncEnumerator, TKeySelector keySelector, IEqualityComparer<TKey> comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncFromNeverStarted()
                {
                    return _asyncEnumerator.DisposeAsync();
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TElement> lookup = default;
                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        lookup = new LookupImpl<TKey, TElement>(_comparer, true);
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

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        lookup.Dispose();
                        await _asyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupByAwait<TKeySelector>(
                AsyncEnumerator<TElement> asyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TElement, Promise<TKey>>
            {
                var enumerable = AsyncEnumerableCreate<Linq.Grouping<TKey, TElement>, GroupByKeyAsyncIterator<TKeySelector>>.GetOrCreate(
                    new GroupByKeyAsyncIterator<TKeySelector>(asyncEnumerator, keySelector, comparer));
                return new AsyncEnumerable<Linq.Grouping<TKey, TElement>>(enumerable);
            }

            private readonly struct ConfiguredGroupByKeyElementSyncIterator<TSource, TKeySelector, TElementSelector> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TElementSelector _elementSelector;
                private readonly IEqualityComparer<TKey> _comparer;

                internal ConfiguredGroupByKeyElementSyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TElementSelector elementSelector, IEqualityComparer<TKey> comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _elementSelector = elementSelector;
                    _comparer = comparer;
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncFromNeverStarted()
                {
                    return _configuredAsyncEnumerator.DisposeAsync();
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TElement> lookup = default;
                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        lookup = new LookupImpl<TKey, TElement>(_comparer, true);
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

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        lookup.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupBy<TSource, TKeySelector, TElementSelector>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
            {
                var enumerable = AsyncEnumerableCreate<Linq.Grouping<TKey, TElement>, ConfiguredGroupByKeyElementSyncIterator<TSource, TKeySelector, TElementSelector>>.GetOrCreate(
                    new ConfiguredGroupByKeyElementSyncIterator<TSource, TKeySelector, TElementSelector>(configuredAsyncEnumerator, keySelector, elementSelector, comparer));
                return new AsyncEnumerable<Linq.Grouping<TKey, TElement>>(enumerable);
            }

            private readonly struct ConfiguredGroupByKeySyncIterator<TKeySelector> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TElement, TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TElement>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly IEqualityComparer<TKey> _comparer;

                internal ConfiguredGroupByKeySyncIterator(ConfiguredAsyncEnumerable<TElement>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, IEqualityComparer<TKey> comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncFromNeverStarted()
                {
                    return _configuredAsyncEnumerator.DisposeAsync();
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TElement> lookup = default;
                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        lookup = new LookupImpl<TKey, TElement>(_comparer, true);
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

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        lookup.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupBy<TKeySelector>(
                ConfiguredAsyncEnumerable<TElement>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TElement, TKey>
            {
                var enumerable = AsyncEnumerableCreate<Linq.Grouping<TKey, TElement>, ConfiguredGroupByKeySyncIterator<TKeySelector>>.GetOrCreate(
                    new ConfiguredGroupByKeySyncIterator<TKeySelector>(configuredAsyncEnumerator, keySelector, comparer));
                return new AsyncEnumerable<Linq.Grouping<TKey, TElement>>(enumerable);
            }

            private readonly struct ConfiguredGroupByKeyElementAsyncIterator<TSource, TKeySelector, TElementSelector> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TElementSelector _elementSelector;
                private readonly IEqualityComparer<TKey> _comparer;

                internal ConfiguredGroupByKeyElementAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TElementSelector elementSelector, IEqualityComparer<TKey> comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _elementSelector = elementSelector;
                    _comparer = comparer;
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncFromNeverStarted()
                {
                    return _configuredAsyncEnumerator.DisposeAsync();
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TElement> lookup = default;
                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        lookup = new LookupImpl<TKey, TElement>(_comparer, true);
                        do
                        {
                            var item = _configuredAsyncEnumerator.Current;
                            var key = await _keySelector.Invoke(item);
                            var group = lookup.GetOrCreateGrouping(key, true);

                            // The keySelector could have switched contexts.
                            // We switch back to the configured context before invoking the elementSelector.
                            await _configuredAsyncEnumerator.SwitchToContext();
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

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        lookup.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupByAwait<TSource, TKeySelector, TElementSelector>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
            {
                var enumerable = AsyncEnumerableCreate<Linq.Grouping<TKey, TElement>, ConfiguredGroupByKeyElementAsyncIterator<TSource, TKeySelector, TElementSelector>>.GetOrCreate(
                    new ConfiguredGroupByKeyElementAsyncIterator<TSource, TKeySelector, TElementSelector>(configuredAsyncEnumerator, keySelector, elementSelector, comparer));
                return new AsyncEnumerable<Linq.Grouping<TKey, TElement>>(enumerable);
            }

            private readonly struct ConfiguredGroupByKeyAsyncIterator<TKeySelector> : IAsyncIterator<Linq.Grouping<TKey, TElement>>
                where TKeySelector : IFunc<TElement, Promise<TKey>>
            {
                private readonly ConfiguredAsyncEnumerable<TElement>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly IEqualityComparer<TKey> _comparer;

                internal ConfiguredGroupByKeyAsyncIterator(ConfiguredAsyncEnumerable<TElement>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, IEqualityComparer<TKey> comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncFromNeverStarted()
                {
                    return _configuredAsyncEnumerator.DisposeAsync();
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<Linq.Grouping<TKey, TElement>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    // We could do await Lookup<TKey, TElement>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TElement> lookup = default;
                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // No need to create the lookup if the enumerable is empty.
                            return;
                        }

                        lookup = new LookupImpl<TKey, TElement>(_comparer, true);
                        do
                        {
                            var item = _configuredAsyncEnumerator.Current;
                            var key = await _keySelector.Invoke(item);
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

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        lookup.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<Linq.Grouping<TKey, TElement>> GroupByAwait<TKeySelector>(
                ConfiguredAsyncEnumerable<TElement>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TElement, Promise<TKey>>
            {
                var enumerable = AsyncEnumerableCreate<Linq.Grouping<TKey, TElement>, ConfiguredGroupByKeyAsyncIterator<TKeySelector>>.GetOrCreate(
                    new ConfiguredGroupByKeyAsyncIterator<TKeySelector>(configuredAsyncEnumerator, keySelector, comparer));
                return new AsyncEnumerable<Linq.Grouping<TKey, TElement>>(enumerable);
            }
        } // class Lookup<TKey, TElement>
    } // class Internal
#endif
} // namespace Proto.Promises