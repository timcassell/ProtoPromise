#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class Lookup<TKey, TElement> : ILookup<TKey, TElement>
        {
            private readonly IEqualityComparer<TKey> _comparer;
            private Grouping<TKey, TElement>[] _groupings;
            private Grouping<TKey, TElement> _lastGrouping;

            private Lookup(IEqualityComparer<TKey> comparer)
            {
                _comparer = comparer ?? EqualityComparer<TKey>.Default;
                _groupings = new Grouping<TKey, TElement>[7];
            }

            public int Count { get; private set; }

            public IEnumerable<TElement> this[TKey key]
            {
                get
                {
                    var grouping = GetGrouping(key);
                    if (grouping != null)
                    {
                        return grouping;
                    }
                    return Array.Empty<TElement>();
                }
            }

            public bool Contains(TKey key)
            {
                return GetGrouping(key) != null;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
            {
                var g = _lastGrouping;
                if (g != null)
                {
                    do
                    {
                        g = g._next;
                        yield return g;
                    } while (g != _lastGrouping);
                }
            }

            internal static async Promise<ILookup<TKey, TElement>> CreateAsync<TSource, TKeySelector, TElementSelector>(
                AsyncEnumerable<TSource> source,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer,
                CancelationToken cancelationToken)
                where TKeySelector : IFunc<TSource, TKey>
                where TElementSelector : IFunc<TSource, TElement>
            {
                var asyncEnumerator = source.GetAsyncEnumerator(cancelationToken);
                var lookup = new Lookup<TKey, TElement>(comparer);

                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var item = asyncEnumerator.Current;
                        var key = keySelector.Invoke(item);
                        var group = lookup.GetOrCreateGrouping(key);

                        var element = elementSelector.Invoke(item);
                        group.Add(element);
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                return lookup;
            }

            internal static async Promise<ILookup<TKey, TElement>> CreateAsync<TKeySelector>(
                AsyncEnumerable<TElement> source,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer,
                CancelationToken cancelationToken)
                where TKeySelector : IFunc<TElement, TKey>
            {
                var asyncEnumerator = source.GetAsyncEnumerator(cancelationToken);
                var lookup = new Lookup<TKey, TElement>(comparer);

                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var item = asyncEnumerator.Current;
                        var key = keySelector.Invoke(item);
                        lookup.GetOrCreateGrouping(key).Add(item);
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                return lookup;
            }

            internal static async Promise<ILookup<TKey, TElement>> CreateAwaitAsync<TSource, TKeySelector, TElementSelector>(
                AsyncEnumerable<TSource> source,
                TKeySelector keySelector,
                TElementSelector elementSelector,
                IEqualityComparer<TKey> comparer,
                CancelationToken cancelationToken)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TElementSelector : IFunc<TSource, Promise<TElement>>
            {
                var asyncEnumerator = source.GetAsyncEnumerator(cancelationToken);
                var lookup = new Lookup<TKey, TElement>(comparer);

                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var item = asyncEnumerator.Current;
                        var key = await keySelector.Invoke(item);
                        var group = lookup.GetOrCreateGrouping(key);

                        var element = await elementSelector.Invoke(item);
                        group.Add(element);
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                return lookup;
            }

            internal static async Promise<ILookup<TKey, TElement>> CreateAwaitAsync<TKeySelector>(
                AsyncEnumerable<TElement> source,
                TKeySelector keySelector,
                IEqualityComparer<TKey> comparer,
                CancelationToken cancelationToken)
                where TKeySelector : IFunc<TElement, Promise<TKey>>
            {
                var asyncEnumerator = source.GetAsyncEnumerator(cancelationToken);
                var lookup = new Lookup<TKey, TElement>(comparer);

                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        var item = asyncEnumerator.Current;
                        var key = await keySelector.Invoke(item);
                        lookup.GetOrCreateGrouping(key).Add(item);
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }

                return lookup;
            }

            internal Grouping<TKey, TElement> GetGrouping(TKey key)
            {
                var hashCode = InternalGetHashCode(key);

                return GetGrouping(key, hashCode);
            }

            internal Grouping<TKey, TElement> GetGrouping(TKey key, int hashCode)
            {
                for (var g = _groupings[hashCode % _groupings.Length]; g != null; g = g._hashNext)
                {
                    if (g._hashCode == hashCode && _comparer.Equals(g._key, key))
                    {
                        return g;
                    }
                }

                return null;
            }

            internal Grouping<TKey, TElement> GetOrCreateGrouping(TKey key)
            {
                var hashCode = InternalGetHashCode(key);

                var grouping = GetGrouping(key, hashCode);
                if (grouping != null)
                {
                    return grouping;
                }

                if (Count == _groupings.Length)
                {
                    Resize();
                }

                var index = hashCode % _groupings.Length;
                var g = new Grouping<TKey, TElement>(key, hashCode, new TElement[1], _groupings[index]);
                _groupings[index] = g;
                if (_lastGrouping == null)
                {
                    g._next = g;
                }
                else
                {
                    g._next = _lastGrouping._next;
                    _lastGrouping._next = g;
                }

                _lastGrouping = g;
                Count++;
                return g;
            }

            internal int InternalGetHashCode(TKey key)
            {
                // Handle comparer implementations that throw when passed null
                return (key == null) ? 0 : _comparer.GetHashCode(key) & 0x7FFFFFFF;
            }

            private void Resize()
            {
                var newSize = checked((Count * 2) + 1);
                var newGroupings = new Grouping<TKey, TElement>[newSize];
                var g = _lastGrouping;
                do
                {
                    g = g._next;
                    var index = g._hashCode % newSize;
                    g._hashNext = newGroupings[index];
                    newGroupings[index] = g;
                } while (g != _lastGrouping);

                _groupings = newGroupings;
            }
        }
    } // class Internal
#endif
} // namespace Proto.Promises