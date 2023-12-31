#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using Proto.Promises.Collections;
using Proto.Promises.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#pragma warning disable IDE0251 // Make member 'readonly'

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Implemented in a struct so that GroupBy doesn't need to allocate the Lookup class.
        internal struct LookupImpl<TKey, TElement> : IDisposable
        {
            private readonly IEqualityComparer<TKey> _comparer;
            internal Grouping<TKey, TElement> _lastGrouping;
            // We use a TempCollectionBuilder to handle renting from ArrayPool.
            internal TempCollectionBuilder<Grouping<TKey, TElement>> _groupings;
            internal int _count;

            internal LookupImpl(IEqualityComparer<TKey> comparer, bool willBeDisposed)
            {
                _comparer = comparer ?? EqualityComparer<TKey>.Default;
                _lastGrouping = null;
                // The smallest array returned from ArrayPool by default is 16, so we use 15 count to start instead of 7 that System.Linq uses.
                // The actual array length could be larger than the requested size, so we make sure the count is what we expect.
                _groupings = new TempCollectionBuilder<Grouping<TKey, TElement>>(15, 15);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                // ToLookupAsync does not dispose. GroupBy does.
                if (!willBeDisposed)
                {
                    Discard(_groupings._disposedChecker);
                }
#endif
                _count = 0;
            }

            internal Grouping<TKey, TElement> GetGrouping(TKey key)
            {
                var hashCode = InternalGetHashCode(key);

                return GetGrouping(key, hashCode);
            }

            private Grouping<TKey, TElement> GetGrouping(TKey key, int hashCode)
            {
                for (var g = _groupings._items[hashCode % _groupings._count]; g != null; g = g._hashNext)
                {
                    if (g._hashCode == hashCode && _comparer.Equals(g._key, key))
                    {
                        return g;
                    }
                }

                return null;
            }

            internal Grouping<TKey, TElement> GetOrCreateGrouping(TKey key, bool willBeDisposed)
            {
                var hashCode = InternalGetHashCode(key);

                var grouping = GetGrouping(key, hashCode);
                if (grouping != null)
                {
                    return grouping;
                }

                if (_count == _groupings._count)
                {
                    Resize();
                }

                var index = hashCode % _groupings._count;
                var g = Grouping<TKey, TElement>.GetOrCreate(key, hashCode, _groupings._items[index], willBeDisposed);
                _groupings._items[index] = g;
                if (_lastGrouping == null)
                {
                    g._nextGrouping = g;
                }
                else
                {
                    g._nextGrouping = _lastGrouping._nextGrouping;
                    _lastGrouping._nextGrouping = g;
                }

                _lastGrouping = g;
                _count++;
                return g;
            }

            private int InternalGetHashCode(TKey key)
            {
                // Handle comparer implementations that throw when passed null
                return (key == null) ? 0 : _comparer.GetHashCode(key) & 0x7FFFFFFF;
            }

            private void Resize()
            {
                var newSize = checked((_count * 2) + 1);
                _groupings.SetCapacityNoCopy(newSize);
                _groupings._count = newSize;
                var g = _lastGrouping;
                do
                {
                    g = g._nextGrouping;
                    var index = g._hashCode % newSize;
                    g._hashNext = _groupings._items[index];
                    _groupings._items[index] = g;
                } while (g != _lastGrouping);
            }

            public void Dispose()
            {
                if (_groupings._items == null)
                {
                    return;
                }
                // Dispose each grouping.
                if (_lastGrouping != null)
                {
                    var current = _lastGrouping._nextGrouping;
                    while (current != _lastGrouping)
                    {
                        var temp = current;
                        current = current._nextGrouping;
                        temp.Dispose();
                    }
                    _lastGrouping.Dispose();
                }
                _groupings.Dispose();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class Lookup<TKey, TElement> : ILookup<TKey, TElement>
        {
            private readonly LookupImpl<TKey, TElement> _impl;

            private Lookup(LookupImpl<TKey, TElement> impl)
            {
                _impl = impl;
            }

            public int Count => _impl._count;

            public IEnumerable<TElement> this[TKey key]
            {
                get
                {
                    var grouping = _impl.GetGrouping(key);
                    if (grouping != null)
                    {
                        return grouping;
                    }
                    return Array.Empty<TElement>();
                }
            }

            public bool Contains(TKey key) => _impl.GetGrouping(key) != null;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
            {
                var g = _impl._lastGrouping;
                if (g != null)
                {
                    do
                    {
                        g = g._nextGrouping;
                        yield return g;
                    } while (g != _impl._lastGrouping);
                }
            }

            internal static async Promise<ILookup<TKey, TElement>> GetOrCreateAsync<TSource, TKeySelector, TElementSelector>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
            {
                var lookup = new LookupImpl<TKey, TElement>(comparer, false);

                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var item = asyncEnumerator.Current;
                        var key = keySelector.Invoke(item);
                        var group = lookup.GetOrCreateGrouping(key, false);

                        var element = elementSelector.Invoke(item);
                        group.Add(element);
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                return new Lookup<TKey, TElement>(lookup);
            }

            internal static async Promise<ILookup<TKey, TElement>> GetOrCreateAsync<TKeySelector>(
                AsyncEnumerator<TElement> asyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TElement, TKey>
            {
                var lookup = new LookupImpl<TKey, TElement>(comparer, false);

                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var item = asyncEnumerator.Current;
                        var key = keySelector.Invoke(item);
                        lookup.GetOrCreateGrouping(key, false).Add(item);
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                return new Lookup<TKey, TElement>(lookup);
            }

            internal static async Promise<ILookup<TKey, TElement>> GetOrCreateAwaitAsync<TSource, TKeySelector, TElementSelector>(
                AsyncEnumerator<TSource> asyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
            {
                var lookup = new LookupImpl<TKey, TElement>(comparer, false);

                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var item = asyncEnumerator.Current;
                        var key = await keySelector.Invoke(item);
                        var group = lookup.GetOrCreateGrouping(key, false);

                        var element = await elementSelector.Invoke(item);
                        group.Add(element);
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                return new Lookup<TKey, TElement>(lookup);
            }

            internal static async Promise<ILookup<TKey, TElement>> GetOrCreateAwaitAsync<TKeySelector>(
                AsyncEnumerator<TElement> asyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TElement, Promise<TKey>>
            {
                var lookup = new LookupImpl<TKey, TElement>(comparer, false);

                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var item = asyncEnumerator.Current;
                        var key = await keySelector.Invoke(item);
                        lookup.GetOrCreateGrouping(key, false).Add(item);
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                return new Lookup<TKey, TElement>(lookup);
            }

            internal static async Promise<ILookup<TKey, TElement>> GetOrCreateAsync<TSource, TKeySelector, TElementSelector>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
            {
                var lookup = new LookupImpl<TKey, TElement>(comparer, false);

                try
                {
                    while (await configuredAsyncEnumerator.MoveNextAsync())
                    {
                        var item = configuredAsyncEnumerator.Current;
                        var key = keySelector.Invoke(item);
                        var group = lookup.GetOrCreateGrouping(key, false);

                        var element = elementSelector.Invoke(item);
                        group.Add(element);
                    }
                }
                finally
                {
                    await configuredAsyncEnumerator.DisposeAsync();
                }

                return new Lookup<TKey, TElement>(lookup);
            }

            internal static async Promise<ILookup<TKey, TElement>> GetOrCreateAsync<TKeySelector>(
                ConfiguredAsyncEnumerable<TElement>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TElement, TKey>
            {
                var lookup = new LookupImpl<TKey, TElement>(comparer, false);

                try
                {
                    while (await configuredAsyncEnumerator.MoveNextAsync())
                    {
                        var item = configuredAsyncEnumerator.Current;
                        var key = keySelector.Invoke(item);
                        lookup.GetOrCreateGrouping(key, false).Add(item);
                    }
                }
                finally
                {
                    await configuredAsyncEnumerator.DisposeAsync();
                }

                return new Lookup<TKey, TElement>(lookup);
            }

            internal static async Promise<ILookup<TKey, TElement>> GetOrCreateAwaitAsync<TSource, TKeySelector, TElementSelector>(
                ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
            {
                var lookup = new LookupImpl<TKey, TElement>(comparer, false);

                try
                {
                    while (await configuredAsyncEnumerator.MoveNextAsync())
                    {
                        var item = configuredAsyncEnumerator.Current;
                        var key = await keySelector.Invoke(item);
                        var group = lookup.GetOrCreateGrouping(key, false);

                        // The keySelector could have switched contexts.
                        // We switch back to the configured context before invoking the elementSelector.
                        await configuredAsyncEnumerator.SwitchToContext();
                        var element = await elementSelector.Invoke(item);
                        group.Add(element);
                    }
                }
                finally
                {
                    await configuredAsyncEnumerator.DisposeAsync();
                }

                return new Lookup<TKey, TElement>(lookup);
            }

            internal static async Promise<ILookup<TKey, TElement>> GetOrCreateAwaitAsync<TKeySelector>(
                ConfiguredAsyncEnumerable<TElement>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer)
                where TKeySelector : IFunc<TElement, Promise<TKey>>
            {
                var lookup = new LookupImpl<TKey, TElement>(comparer, false);

                try
                {
                    while (await configuredAsyncEnumerator.MoveNextAsync())
                    {
                        var item = configuredAsyncEnumerator.Current;
                        var key = await keySelector.Invoke(item);
                        lookup.GetOrCreateGrouping(key, false).Add(item);
                    }
                }
                finally
                {
                    await configuredAsyncEnumerator.DisposeAsync();
                }

                return new Lookup<TKey, TElement>(lookup);
            }
        } // class Lookup<TKey, TElement>
    } // class Internal
#endif
} // namespace Proto.Promises