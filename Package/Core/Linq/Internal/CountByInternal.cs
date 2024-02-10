#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using System;
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
        // Similar to LookupImpl, but we care about key-value instead of key-group.
        internal struct LookupSingleValue<TKey, TValue, TEqualityComparer> : IDisposable
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            private readonly TEqualityComparer _comparer;
            internal Node _lastNode;
            // We use a TempCollectionBuilder to handle renting from ArrayPool.
            private TempCollectionBuilder<Node> _nodes;
            private int _count;

            internal LookupSingleValue(TEqualityComparer comparer)
            {
                _comparer = comparer;
                _lastNode = null;
                // The smallest array returned from ArrayPool by default is 16, so we use 15 count to start instead of 7 that System.Linq uses.
                // The actual array length could be larger than the requested size, so we make sure the count is what we expect.
                _nodes = new TempCollectionBuilder<Node>(15, 15);
                _count = 0;
            }

            private Node GetNode(TKey key, int hashCode)
            {
                for (var node = _nodes[hashCode % _nodes._count]; node != null; node = node._hashNext)
                {
                    if (node._hashCode == hashCode && _comparer.Equals(node._key, key))
                    {
                        return node;
                    }
                }
                return null;
            }

            internal Node GetOrCreateNode(TKey key, out bool exists)
            {
                var hashCode = InternalGetHashCode(key);

                var node = GetNode(key, hashCode);
                if (node != null)
                {
                    exists = true;
                    return node;
                }

                if (_count == _nodes._count)
                {
                    Resize();
                }

                var index = hashCode % _nodes._count;
                node = Node.GetOrCreate(key, hashCode, _nodes[index]);
                _nodes[index] = node;
                if (_lastNode == null)
                {
                    node._nextNode = node;
                }
                else
                {
                    node._nextNode = _lastNode._nextNode;
                    _lastNode._nextNode = node;
                }

                _lastNode = node;
                ++_count;
                exists = false;
                return node;
            }

            [MethodImpl(InlineOption)]
            private int InternalGetHashCode(TKey key)
            {
                // Handle comparer implementations that throw when passed null
                return (key == null) ? 0 : _comparer.GetHashCode(key) & 0x7FFFFFFF;
            }

            private void Resize()
            {
                var newSize = checked((_count * 2) + 1);
                _nodes.SetCapacityNoCopy(newSize);
                _nodes._count = newSize;
                var node = _lastNode;
                do
                {
                    node = node._nextNode;
                    var index = node._hashCode % newSize;
                    node._hashNext = _nodes[index];
                    _nodes[index] = node;
                } while (node != _lastNode);
            }

            public void Dispose()
            {
                if (_nodes._items == null)
                {
                    return;
                }
                // Dispose each node.
                if (_lastNode != null)
                {
                    var current = _lastNode._nextNode;
                    while (current != _lastNode)
                    {
                        var temp = current;
                        current = current._nextNode;
                        temp.Dispose();
                    }
                    _lastNode.Dispose();
                }
                _nodes.Dispose();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class Node : HandleablePromiseBase, IDisposable
            {
                internal Node _hashNext;
                internal Node _nextNode;
                internal TKey _key;
                internal TValue _value;
                internal int _hashCode;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private bool _disposed;

                ~Node()
                {
                    if (!_disposed)
                    {
                        // For debugging. This should never happen.
                        ReportRejection(new UnreleasedObjectException("A PreservedEnumerationDictionary<,,>.Node was garbage collected without it being disposed."), null);
                    }
                }
#endif

                private Node() { }

                [MethodImpl(InlineOption)]
                private static Node GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<Node>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new Node()
                        : obj.UnsafeAs<Node>();
                }

                internal static Node GetOrCreate(TKey key, int hashCode, Node hashNext)
                {
                    var node = GetOrCreate();
                    node._key = key;
                    node._hashCode = hashCode;
                    node._hashNext = hashNext;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    node._disposed = false;
#endif
                    return node;
                }

                public void Dispose()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
                    _hashNext = null;
                    _nextNode = null;
                    _key = default;
                    _value = default;
                    ObjectPool.MaybeRepool(this);
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class CountByHelper<TKey>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct CountBySyncIterator<TSource, TEqualityComparer, TKeySelector> : IAsyncIterator<KeyValuePair<TKey, int>>
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal CountBySyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, int>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    LookupSingleValue<TKey, int, TEqualityComparer> dict = default;
                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        dict = new LookupSingleValue<TKey, int, TEqualityComparer>(_comparer);
                        do
                        {
                            var key = _keySelector.Invoke(_asyncEnumerator.Current);
                            ++dict.GetOrCreateNode(key, out _)._value;
                        } while (await _asyncEnumerator.MoveNextAsync());

                        // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                        var node = dict._lastNode;
                        do
                        {
                            node = node._nextNode;
                            await writer.YieldAsync(new KeyValuePair<TKey, int>(node._key, node._value));
                        } while (node != dict._lastNode);

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        dict.Dispose();
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TEqualityComparer, TKeySelector>(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, int>>.Create(new CountBySyncIterator<TSource, TEqualityComparer, TKeySelector>(asyncEnumerator, keySelector, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct CountByAsyncIterator<TSource, TEqualityComparer, TKeySelector> : IAsyncIterator<KeyValuePair<TKey, int>>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal CountByAsyncIterator(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, int>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;

                    LookupSingleValue<TKey, int, TEqualityComparer> dict = default;
                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        dict = new LookupSingleValue<TKey, int, TEqualityComparer>(_comparer);
                        do
                        {
                            var key = await _keySelector.Invoke(_asyncEnumerator.Current);
                            ++dict.GetOrCreateNode(key, out _)._value;
                        } while (await _asyncEnumerator.MoveNextAsync());

                        // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                        var node = dict._lastNode;
                        do
                        {
                            node = node._nextNode;
                            await writer.YieldAsync(new KeyValuePair<TKey, int>(node._key, node._value));
                        } while (node != dict._lastNode);

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        dict.Dispose();
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, int>> CountByAwait<TSource, TEqualityComparer, TKeySelector>(AsyncEnumerator<TSource> asyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, int>>.Create(new CountByAsyncIterator<TSource, TEqualityComparer, TKeySelector>(asyncEnumerator, keySelector, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredCountBySyncIterator<TSource, TEqualityComparer, TKeySelector> : IAsyncIterator<KeyValuePair<TKey, int>>
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredCountBySyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, int>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    LookupSingleValue<TKey, int, TEqualityComparer> dict = default;
                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        dict = new LookupSingleValue<TKey, int, TEqualityComparer>(_comparer);
                        do
                        {
                            var key = _keySelector.Invoke(_configuredAsyncEnumerator.Current);
                            ++dict.GetOrCreateNode(key, out _)._value;
                        } while (await _configuredAsyncEnumerator.MoveNextAsync());

                        // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                        var node = dict._lastNode;
                        do
                        {
                            node = node._nextNode;
                            await writer.YieldAsync(new KeyValuePair<TKey, int>(node._key, node._value));
                        } while (node != dict._lastNode);

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        dict.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TEqualityComparer, TKeySelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, int>>.Create(new ConfiguredCountBySyncIterator<TSource, TEqualityComparer, TKeySelector>(configuredAsyncEnumerator, keySelector, comparer));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct ConfiguredCountByAsyncIterator<TSource, TEqualityComparer, TKeySelector> : IAsyncIterator<KeyValuePair<TKey, int>>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
            {
                private readonly ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private readonly TKeySelector _keySelector;
                private readonly TEqualityComparer _comparer;

                internal ConfiguredCountByAsyncIterator(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TEqualityComparer comparer)
                {
                    _configuredAsyncEnumerator = configuredAsyncEnumerator;
                    _keySelector = keySelector;
                    _comparer = comparer;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<KeyValuePair<TKey, int>> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredAsyncEnumerator._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    LookupSingleValue<TKey, int, TEqualityComparer> dict = default;
                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        dict = new LookupSingleValue<TKey, int, TEqualityComparer>(_comparer);
                        do
                        {
                            var key = await _keySelector.Invoke(_configuredAsyncEnumerator.Current);
                            // The async selector function could have switched context, make sure we're on the configured context before invoking the comparer.
                            await _configuredAsyncEnumerator.SwitchToContext();
                            ++dict.GetOrCreateNode(key, out _)._value;
                        } while (await _configuredAsyncEnumerator.MoveNextAsync());

                        // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                        var node = dict._lastNode;
                        do
                        {
                            node = node._nextNode;
                            await writer.YieldAsync(new KeyValuePair<TKey, int>(node._key, node._value));
                        } while (node != dict._lastNode);

                        // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        joinedCancelationSource.TryDispose();
                        dict.Dispose();
                        await _configuredAsyncEnumerator.DisposeAsync();
                    }
                }

                [MethodImpl(InlineOption)]
                public Promise DisposeAsyncWithoutStart()
                    => _configuredAsyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<KeyValuePair<TKey, int>> CountByAwait<TSource, TEqualityComparer, TKeySelector>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator,
                TKeySelector keySelector, TEqualityComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TEqualityComparer : IEqualityComparer<TKey>
                => AsyncEnumerable<KeyValuePair<TKey, int>>.Create(new ConfiguredCountByAsyncIterator<TSource, TEqualityComparer, TKeySelector>(configuredAsyncEnumerator, keySelector, comparer));
        }
    } // class Internal
#endif
} // namespace Proto.Promises