#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Collections;
using Proto.Promises.Linq;
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
        internal static class GroupJoinHelper<TKey>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct GroupJoinIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TempCollection<TInner> InnerElements)>
                where TOuterKeySelector : IFunc<TOuter, CancelationToken, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TOuter> _outerAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal GroupJoinIterator(AsyncEnumerator<TOuter> outerAsyncEnumerator,
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

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TempCollection<TInner> InnerElements)> writer, CancelationToken cancelationToken)
                {
                    // The enumerators were retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _outerAsyncEnumerator._target._cancelationToken = cancelationToken;
                    _innerAsyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TInner, TEqualityComparer> lookup = new LookupImpl<TKey, TInner, TEqualityComparer>(_comparer, true);
                    var emptyInnerElements = new TempCollectionBuilder<TInner>(0);
                    try
                    {
                        while (await _innerAsyncEnumerator.MoveNextAsync())
                        {
                            var item = _innerAsyncEnumerator.Current;
                            var key = await _innerKeySelector.Invoke(item, cancelationToken);
                            // Unlike GroupBy, GroupJoin ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        // We don't dispose the enumerators until the owner is disposed.
                        // This is in case either enumerator contains TempCollection that they will still be valid until the owner is disposed.

                        while (await _outerAsyncEnumerator.MoveNextAsync())
                        {
                            var outer = _outerAsyncEnumerator.Current;
                            var g = lookup.GetGrouping(await _outerKeySelector.Invoke(outer, cancelationToken));
                            if (g == null)
                            {
                                await writer.YieldAsync((outer, emptyInnerElements.View));
                            }
                            else
                            {
                                await writer.YieldAsync((outer, g._elements.View));
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        emptyInnerElements.Dispose();
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
                    // We consume less memory by using .Finally instead of async/await.
                    => _innerAsyncEnumerator.DisposeAsync()
                        .Finally(_outerAsyncEnumerator, e => e.DisposeAsync());
            }

            internal static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                AsyncEnumerator<TOuter> outerAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, CancelationToken, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)>.Create(
                    new GroupJoinIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(outerAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredGroupJoinIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer> : IAsyncIterator<(TOuter Outer, TempCollection<TInner> InnerElements)>
                where TOuterKeySelector : IFunc<TOuter, CancelationToken, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TOuter>.Enumerator _configuredOuterAsyncEnumerator;
                private readonly AsyncEnumerator<TInner> _innerAsyncEnumerator;
                private readonly TOuterKeySelector _outerKeySelector;
                private readonly TInnerKeySelector _innerKeySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredGroupJoinIterator(ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
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

                public async AsyncIteratorMethod Start(AsyncStreamWriter<(TOuter Outer, TempCollection<TInner> InnerElements)> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredOuterAsyncEnumerator._enumerator._target._cancelationToken);
                    // Use the same cancelation token for both enumerators.
                    _innerAsyncEnumerator._target._cancelationToken = cancelationToken;

                    // We could do await Lookup<TKey, TInner>.GetOrCreateAsync(...), but it's more efficient to do it manually so we won't allocate the Lookup class and a separate async state machine.
                    LookupImpl<TKey, TInner, TEqualityComparer> lookup = new LookupImpl<TKey, TInner, TEqualityComparer>(_comparer, true);
                    var emptyInnerElements = new TempCollectionBuilder<TInner>(0);
                    try
                    {
                        // The inner enumerator is not configured, so we have to configure the await manually so that the inner key selector will be invoked on the configured context.
                        while (await _innerAsyncEnumerator.MoveNextAsync().ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions))
                        {
                            var item = _innerAsyncEnumerator.Current;
                            // The key selector function could have switched context, make sure we're on the configured context before invoking the comparer.
                            var key = await _innerKeySelector.Invoke(item, cancelationToken).ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                            // Unlike GroupBy, GroupJoin ignores null keys.
                            if (key != null)
                            {
                                lookup.GetOrCreateGrouping(key, true).Add(item);
                            }
                        }
                        // We don't dispose the enumerators until the owner is disposed.
                        // This is in case either enumerator contains TempCollection that they will still be valid until the owner is disposed.

                        while (await _configuredOuterAsyncEnumerator.MoveNextAsync())
                        {
                            var outer = _configuredOuterAsyncEnumerator.Current;
                            // The key selector function could have switched context, make sure we're on the configured context before invoking the comparer.
                            var key = await _outerKeySelector.Invoke(outer, cancelationToken).ConfigureAwait(_configuredOuterAsyncEnumerator.ContinuationOptions);
                            var g = lookup.GetGrouping(key);
                            if (g == null)
                            {
                                await writer.YieldAsync((outer, emptyInnerElements.View));
                            }
                            else
                            {
                                await writer.YieldAsync((outer, g._elements.View));
                            }
                        }

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        emptyInnerElements.Dispose();
                        lookup.Dispose();
                        try
                        {
                            await _innerAsyncEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            await _configuredOuterAsyncEnumerator.DisposeAsync();
                        }
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                    // We consume less memory by using .Finally instead of async/await.
                    => _innerAsyncEnumerator.DisposeAsync()
                        .Finally(_configuredOuterAsyncEnumerator, e => e.DisposeAsync());
            }

            internal static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(
                ConfiguredAsyncEnumerable<TOuter>.Enumerator configuredOuterAsyncEnumerator,
                AsyncEnumerator<TInner> innerAsyncEnumerator,
                TOuterKeySelector outerKeySelector,
                TInnerKeySelector innerKeySelector,
                TEqualityComparer comparer)
                where TOuterKeySelector : IFunc<TOuter, CancelationToken, Promise<TKey>>
                where TInnerKeySelector : IFunc<TInner, CancelationToken, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)>.Create(
                    new ConfiguredGroupJoinIterator<TOuter, TInner, TOuterKeySelector, TInnerKeySelector, TEqualityComparer>(configuredOuterAsyncEnumerator, innerAsyncEnumerator, outerKeySelector, innerKeySelector, comparer));
        } // class Lookup<TKey, TElement>
    } // class Internal
} // namespace Proto.Promises