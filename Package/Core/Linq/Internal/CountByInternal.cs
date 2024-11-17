#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0251 // Make member 'readonly'

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Similar to LookupImpl, but we care about key-value instead of key-group.
        internal struct LookupSingleValue<TKey, TValue, TEqualityComparer> : IDisposable
            where TEqualityComparer : IEqualityComparer<TKey>
        {
            internal Node _lastNode;
            // Pooled array.
            private Node[] _nodes;
            private readonly TEqualityComparer _comparer;
            // Usable length of the pooled array.
            private int _nodesLength;
            private int _count;

            internal LookupSingleValue(TEqualityComparer comparer)
            {
                _comparer = comparer;
                _lastNode = null;
                // The smallest array returned from ArrayPool by default is 16, so we use 15 count to start instead of 7 that System.Linq uses.
                // The actual array length could be larger than the requested size, so we make sure the count is what we expect.
                _nodes = ArrayPool<Node>.Shared.Rent(15);
                _nodesLength = 15;
                _count = 0;
            }

            private Node GetNode(TKey key, int hashCode)
            {
                for (var node = _nodes[hashCode % _nodesLength]; node != null; node = node._hashNext)
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

                if (_count == _nodesLength)
                {
                    Resize();
                }

                var index = hashCode % _nodesLength;
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
                ArrayPool<Node>.Shared.Return(_nodes, clearArray: true);
                _nodes = ArrayPool<Node>.Shared.Rent(newSize);
                _nodesLength = newSize;
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

                ArrayPool<Node>.Shared.Return(_nodes, clearArray: true);
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
                    return node;
                }

                public void Dispose()
                {
                    _hashNext = null;
                    _nextNode = null;
                    ClearReferences(ref _key);
                    ClearReferences(ref _value);
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

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, int, TEqualityComparer>(_comparer))
                        {
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

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, int, TEqualityComparer>(_comparer))
                        {
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

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, int, TEqualityComparer>(_comparer))
                        {
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

                    try
                    {
                        // Make sure at least 1 element exists before creating the dictionary.
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            return;
                        }

                        using (var dict = new LookupSingleValue<TKey, int, TEqualityComparer>(_comparer))
                        {
                            do
                            {
                                // The async selector function could have switched context, make sure we're on the configured context before invoking the comparer.
                                var key = await _keySelector.Invoke(_configuredAsyncEnumerator.Current).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                                ++dict.GetOrCreateNode(key, out _)._value;
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());

                            // We don't need to check if node is null, it's guaranteed to be not null since we checked that the source enumerable had at least 1 element.
                            var node = dict._lastNode;
                            do
                            {
                                node = node._nextNode;
                                await writer.YieldAsync(new KeyValuePair<TKey, int>(node._key, node._value));
                            } while (node != dict._lastNode);
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
} // namespace Proto.Promises