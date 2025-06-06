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

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class JoinHelper<TKey>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct JoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TOuter> _outerAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal JoinSyncIterator(AsyncEnumerator<TOuter> outerAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _outerAsyncEnumerator = outerAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _outerAsyncEnumerator._target._cancelationToken = cancelationToken;
                    _innerAsyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TInner, TEqualityComparer> lookup = new LookupImpl<TKey, TInner, TEqualityComparer>(_comparer);
                    try
                    {
                        while (await _innerAsyncEnumerator.MoveNextAsync())
                        {
                            var item = _innerAsyncEnumerator.Current;
                            var key = _innerKeySelector.Invoke(item);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _outerAsyncEnumerator.MoveNextAsync())
                        {
                            var outer = _outerAsyncEnumerator.Current;
                            var g = lookup.GetGrouping(_outerKeySelector.Invoke(outer));
                            if (g == null)
                            {
                                continue;
                            }
                            foreach (var inner in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _outerAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                {
                    // We consume less memory by using .Finally instead of async/await.
                    return _innerAsyncEnumerator.DisposeAsync()
                        .Finally(_outerAsyncEnumerator, e => e.DisposeAsync());
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> Join<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                AsyncEnumerator<TOuter> outerAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new JoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(outerAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct JoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TOuter> _outerAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal JoinAsyncIterator(AsyncEnumerator<TOuter> outerAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _outerAsyncEnumerator = outerAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _outerAsyncEnumerator._target._cancelationToken = cancelationToken;
                    _innerAsyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TInner, TEqualityComparer> lookup = new LookupImpl<TKey, TInner, TEqualityComparer>(_comparer);
                    try
                    {
                        while (await _innerAsyncEnumerator.MoveNextAsync())
                        {
                            var item = _innerAsyncEnumerator.Current;
                            var key = await _innerKeySelector.Invoke(item);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _outerAsyncEnumerator.MoveNextAsync())
                        {
                            var outer = _outerAsyncEnumerator.Current;
                            var g = lookup.GetGrouping(await _outerKeySelector.Invoke(outer));
                            if (g == null)
                            {
                                continue;
                            }
                            foreach (var inner in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _outerAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                {
                    // We consume less memory by using .Finally instead of async/await.
                    return _innerAsyncEnumerator.DisposeAsync()
                        .Finally(_outerAsyncEnumerator, e => e.DisposeAsync());
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> JoinAwait<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                AsyncEnumerator<TOuter> outerAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new JoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(outerAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredJoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TOuter>.Enumerator _configuredOuterAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredJoinSyncIterator(ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _configuredOuterAsyncEnumerator = configuredOuterAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredOuterAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _innerAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TInner, TEqualityComparer> lookup = new LookupImpl<TKey, TInner, TEqualityComparer>(_comparer);
                    try
                    {
                        // The inner enumerator is not configured, so we have to configure the await manually so that the inner key selector and comparer will be invoked on the configured context.
                        while (await _innerAsyncEnumerator.MoveNextAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions))
                        {
                            var item = _innerAsyncEnumerator.Current;
                            var key = _innerKeySelector.Invoke(item);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _configuredOuterAsyncEnumerator.MoveNextAsync())
                        {
                            var outer = _configuredOuterAsyncEnumerator.Current;
                            var g = lookup.GetGrouping(_outerKeySelector.Invoke(outer));
                            if (g == null)
                            {
                                continue;
                            }
                            foreach (var inner in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                        }
                        finally
                        {
                            await _configuredOuterAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public async Promise DisposeAsyncWithoutStart()
                {
                    try
                    {
                        await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                    }
                    finally
                    {
                        await _configuredOuterAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> Join<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new ConfiguredJoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(configuredOuterAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredJoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TOuter>.Enumerator _configuredOuterAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredJoinAsyncIterator(ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _configuredOuterAsyncEnumerator = configuredOuterAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredOuterAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _innerAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TInner, TEqualityComparer> lookup = new LookupImpl<TKey, TInner, TEqualityComparer>(_comparer);
                    try
                    {
                        // The inner enumerator is not configured, so we have to configure the await manually so that the inner key selector will be invoked on the configured context.
                        while (await _innerAsyncEnumerator.MoveNextAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions))
                        {
                            var item = _innerAsyncEnumerator.Current;
                            // The key selector function could have switched context, make sure we're on the configured context before invoking the comparer.
                            var key = await _innerKeySelector.Invoke(item).ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _configuredOuterAsyncEnumerator.MoveNextAsync())
                        {
                            var outer = _configuredOuterAsyncEnumerator.Current;
                            // The key selector function could have switched context, make sure we're on the configured context before invoking the comparer.
                            var key = await _outerKeySelector.Invoke(outer).ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                            var g = lookup.GetGrouping(key);
                            if (g == null)
                            {
                                continue;
                            }
                            foreach (var inner in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                        }
                        finally
                        {
                            await _configuredOuterAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public async Promise DisposeAsyncWithoutStart()
                {
                    try
                    {
                        await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                    }
                    finally
                    {
                        await _configuredOuterAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> JoinAwait<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new ConfiguredJoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(configuredOuterAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct LeftJoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TOuter> _outerAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal LeftJoinSyncIterator(AsyncEnumerator<TOuter> outerAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _outerAsyncEnumerator = outerAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _outerAsyncEnumerator._target._cancelationToken = cancelationToken;
                    _innerAsyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TInner, TEqualityComparer> lookup = new LookupImpl<TKey, TInner, TEqualityComparer>(_comparer);
                    try
                    {
                        while (await _innerAsyncEnumerator.MoveNextAsync())
                        {
                            var item = _innerAsyncEnumerator.Current;
                            var key = _innerKeySelector.Invoke(item);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _outerAsyncEnumerator.MoveNextAsync())
                        {
                            var outer = _outerAsyncEnumerator.Current;
                            var g = lookup.GetGrouping(_outerKeySelector.Invoke(outer));
                            if (g == null)
                            {
                                await writer.YieldAsync((outer, default));
                                continue;
                            }
                            foreach (var inner in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _outerAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                {
                    // We consume less memory by using .Finally instead of async/await.
                    return _innerAsyncEnumerator.DisposeAsync()
                        .Finally(_outerAsyncEnumerator, e => e.DisposeAsync());
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> LeftJoin<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                AsyncEnumerator<TOuter> outerAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new LeftJoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(outerAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct LeftJoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TOuter> _outerAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal LeftJoinAsyncIterator(AsyncEnumerator<TOuter> outerAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _outerAsyncEnumerator = outerAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _outerAsyncEnumerator._target._cancelationToken = cancelationToken;
                    _innerAsyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TInner, TEqualityComparer> lookup = new LookupImpl<TKey, TInner, TEqualityComparer>(_comparer);
                    try
                    {
                        while (await _innerAsyncEnumerator.MoveNextAsync())
                        {
                            var item = _innerAsyncEnumerator.Current;
                            var key = await _innerKeySelector.Invoke(item);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _outerAsyncEnumerator.MoveNextAsync())
                        {
                            var outer = _outerAsyncEnumerator.Current;
                            var g = lookup.GetGrouping(await _outerKeySelector.Invoke(outer));
                            if (g == null)
                            {
                                await writer.YieldAsync((outer, default));
                                continue;
                            }
                            foreach (var inner in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _outerAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                {
                    // We consume less memory by using .Finally instead of async/await.
                    return _innerAsyncEnumerator.DisposeAsync()
                        .Finally(_outerAsyncEnumerator, e => e.DisposeAsync());
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> LeftJoinAwait<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                AsyncEnumerator<TOuter> outerAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new LeftJoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(outerAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredLeftJoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TOuter>.Enumerator _configuredOuterAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredLeftJoinSyncIterator(ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _configuredOuterAsyncEnumerator = configuredOuterAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredOuterAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _innerAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TInner, TEqualityComparer> lookup = new LookupImpl<TKey, TInner, TEqualityComparer>(_comparer);
                    try
                    {
                        // The inner enumerator is not configured, so we have to configure the await manually so that the inner key selector and comparer will be invoked on the configured context.
                        while (await _innerAsyncEnumerator.MoveNextAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions))
                        {
                            var item = _innerAsyncEnumerator.Current;
                            var key = _innerKeySelector.Invoke(item);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _configuredOuterAsyncEnumerator.MoveNextAsync())
                        {
                            var outer = _configuredOuterAsyncEnumerator.Current;
                            var g = lookup.GetGrouping(_outerKeySelector.Invoke(outer));
                            if (g == null)
                            {
                                await writer.YieldAsync((outer, default));
                                continue;
                            }
                            foreach (var inner in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                        }
                        finally
                        {
                            await _configuredOuterAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public async Promise DisposeAsyncWithoutStart()
                {
                    try
                    {
                        await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                    }
                    finally
                    {
                        await _configuredOuterAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> LeftJoin<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new ConfiguredLeftJoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(configuredOuterAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredLeftJoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TOuter>.Enumerator _configuredOuterAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredLeftJoinAsyncIterator(ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _configuredOuterAsyncEnumerator = configuredOuterAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredOuterAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _innerAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TInner, TEqualityComparer> lookup = new LookupImpl<TKey, TInner, TEqualityComparer>(_comparer);
                    try
                    {
                        // The inner enumerator is not configured, so we have to configure the await manually so that the inner key selector will be invoked on the configured context.
                        while (await _innerAsyncEnumerator.MoveNextAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions))
                        {
                            var item = _innerAsyncEnumerator.Current;
                            // The key selector function could have switched context, make sure we're on the configured context before invoking the comparer.
                            var key = await _innerKeySelector.Invoke(item).ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _configuredOuterAsyncEnumerator.MoveNextAsync())
                        {
                            var outer = _configuredOuterAsyncEnumerator.Current;
                            // The key selector function could have switched context, make sure we're on the configured context before invoking the comparer.
                            var key = await _outerKeySelector.Invoke(outer).ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                            var g = lookup.GetGrouping(key);
                            if (g == null)
                            {
                                await writer.YieldAsync((outer, default));
                                continue;
                            }
                            foreach (var inner in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                        }
                        finally
                        {
                            await _configuredOuterAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public async Promise DisposeAsyncWithoutStart()
                {
                    try
                    {
                        await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                    }
                    finally
                    {
                        await _configuredOuterAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> LeftJoinAwait<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new ConfiguredLeftJoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(configuredOuterAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct RightJoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TOuter> _outerAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal RightJoinSyncIterator(AsyncEnumerator<TOuter> outerAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _outerAsyncEnumerator = outerAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _outerAsyncEnumerator._target._cancelationToken = cancelationToken;
                    _innerAsyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TOuter, TEqualityComparer> lookup = new LookupImpl<TKey, TOuter, TEqualityComparer>(_comparer);
                    try
                    {
                        while (await _outerAsyncEnumerator.MoveNextAsync())
                        {
                            var item = _outerAsyncEnumerator.Current;
                            var key = _outerKeySelector.Invoke(item);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _innerAsyncEnumerator.MoveNextAsync())
                        {
                            var inner = _innerAsyncEnumerator.Current;
                            var g = lookup.GetGrouping(_innerKeySelector.Invoke(inner));
                            if (g == null)
                            {
                                await writer.YieldAsync((default, inner));
                                continue;
                            }
                            foreach (var outer in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _outerAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                {
                    // We consume less memory by using .Finally instead of async/await.
                    return _innerAsyncEnumerator.DisposeAsync()
                        .Finally(_outerAsyncEnumerator, e => e.DisposeAsync());
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> RightJoin<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                AsyncEnumerator<TOuter> outerAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new RightJoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(outerAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct RightJoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TOuter> _outerAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal RightJoinAsyncIterator(AsyncEnumerator<TOuter> outerAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _outerAsyncEnumerator = outerAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _outerAsyncEnumerator._target._cancelationToken = cancelationToken;
                    _innerAsyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TOuter, TEqualityComparer> lookup = new LookupImpl<TKey, TOuter, TEqualityComparer>(_comparer);
                    try
                    {
                        while (await _outerAsyncEnumerator.MoveNextAsync())
                        {
                            var item = _outerAsyncEnumerator.Current;
                            var key = await _outerKeySelector.Invoke(item);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _innerAsyncEnumerator.MoveNextAsync())
                        {
                            var inner = _innerAsyncEnumerator.Current;
                            var g = lookup.GetGrouping(await _innerKeySelector.Invoke(inner));
                            if (g == null)
                            {
                                await writer.YieldAsync((default, inner));
                                continue;
                            }
                            foreach (var outer in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _outerAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                {
                    // We consume less memory by using .Finally instead of async/await.
                    return _innerAsyncEnumerator.DisposeAsync()
                        .Finally(_outerAsyncEnumerator, e => e.DisposeAsync());
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> RightJoinAwait<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                AsyncEnumerator<TOuter> outerAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new RightJoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(outerAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredRightJoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TOuter>.Enumerator _configuredOuterAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredRightJoinSyncIterator(ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _configuredOuterAsyncEnumerator = configuredOuterAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredOuterAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _innerAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TOuter, TEqualityComparer> lookup = new LookupImpl<TKey, TOuter, TEqualityComparer>(_comparer);
                    try
                    {
                        // The inner enumerator is not configured, so we have to configure the await manually so that the inner key selector and comparer will be invoked on the configured context.
                        while (await _configuredOuterAsyncEnumerator.MoveNextAsync())
                        {
                            var item = _configuredOuterAsyncEnumerator.Current;
                            var key = _outerKeySelector.Invoke(item);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _innerAsyncEnumerator.MoveNextAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions))
                        {
                            var inner = _innerAsyncEnumerator.Current;
                            var g = lookup.GetGrouping(_innerKeySelector.Invoke(inner));
                            if (g == null)
                            {
                                await writer.YieldAsync((default, inner));
                                continue;
                            }
                            foreach (var outer in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                        }
                        finally
                        {
                            await _configuredOuterAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public async Promise DisposeAsyncWithoutStart()
                {
                    try
                    {
                        await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                    }
                    finally
                    {
                        await _configuredOuterAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> RightJoin<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, TKey>
                where TInnerKeySelector : IFunc<TInner, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new ConfiguredRightJoinSyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(configuredOuterAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredRightJoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TInner Inner)>
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TOuter>.Enumerator _configuredOuterAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredRightJoinAsyncIterator(ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                    AsyncEnumerator<TInner> innerAsyncEnumerator,
                    TOuterKeySelector outerKeySelector,
                    TInnerKeySelector innerKeySelector,
                    TEqualityComparer comparer)
                {
                    _configuredOuterAsyncEnumerator = configuredOuterAsyncEnumerator;
                    _innerAsyncEnumerator = innerAsyncEnumerator;
                    _outerKeySelector = outerKeySelector;
                    _innerKeySelector = innerKeySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TInner Inner)> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredOuterAsyncEnumerator._enumerator._target;
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _innerAsyncEnumerator._target._cancelationToken = enumerableRef._cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TOuter, TEqualityComparer> lookup = new LookupImpl<TKey, TOuter, TEqualityComparer>(_comparer);
                    try
                    {
                        // The inner enumerator is not configured, so we have to configure the await manually so that the inner key selector will be invoked on the configured context.
                        while (await _configuredOuterAsyncEnumerator.MoveNextAsync())
                        {
                            var item = _configuredOuterAsyncEnumerator.Current;
                            // The key selector function could have switched context, make sure we're on the configured context before invoking the comparer.
                            var key = await _outerKeySelector.Invoke(item).ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                            // Unlike GroupBy, Join ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        while (await _innerAsyncEnumerator.MoveNextAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions))
                        {
                            var inner = _innerAsyncEnumerator.Current;
                            // The key selector function could have switched context, make sure we're on the configured context before invoking the comparer.
                            var key = await _innerKeySelector.Invoke(inner).ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                            var g = lookup.GetGrouping(key);
                            if (g == null)
                            {
                                await writer.YieldAsync((default, inner));
                                continue;
                            }
                            foreach (var outer in g)
                            {
                                await writer.YieldAsync((outer, inner));
                            }
                        }

                        await AsyncEnumerableSourceHelpers.WaitForDisposeAsync(writer);
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                        }
                        finally
                        {
                            await _configuredOuterAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public async Promise DisposeAsyncWithoutStart()
                {
                    try
                    {
                        await _innerAsyncEnumerator.DisposeAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                    }
                    finally
                    {
                        await _configuredOuterAsyncEnumerator.DisposeAsync();
                    }
                }
            }

            internal static AsyncEnumerable<(TOuter Outer, TInner Inner)> RightJoinAwait<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                return AsyncEnumerable<(TOuter Outer, TInner Inner)>.Create(
                    new ConfiguredRightJoinAsyncIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(configuredOuterAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
            }
        } // class JoinHelper<TKey>
    } // class Internal
} // namespace Proto.Promises