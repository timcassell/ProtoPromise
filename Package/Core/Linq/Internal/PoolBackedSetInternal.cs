#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace Proto.Promises
{
    partial class Internal
    {
        // Adapted from System.Linq's Set.

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct PoolBackedSet<TElement, TEqualityComparer> : IDisposable
            where TEqualityComparer : IEqualityComparer<TElement>
        {
            // Pooled arrays
            private int[] _buckets;
            private Slot[] _slots;
            private readonly TEqualityComparer _comparer;
            // Usable length of the pooled arrays. They are both the same length.
            private int _arraysLength;
            internal int _count;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            private bool _haveRemoved;
#endif

            internal PoolBackedSet(TEqualityComparer comparer)
            {
                _comparer = comparer;
                // The smallest array returned from ArrayPool by default is 16, so we use 15 count to start instead of 7 that System.Linq uses.
                // The actual array length could be larger than the requested size, so we make sure the count is what we expect.
                _buckets = ArrayPool<int>.Shared.Rent(15);
                _slots = ArrayPool<Slot>.Shared.Rent(15);
                _arraysLength = 15;
                _count = 0;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _haveRemoved = false;
#endif
            }

            // If value is not in set, add it and return true; otherwise return false
            public bool Add(TElement value)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                Debug.Assert(!_haveRemoved, "This class is optimized for never calling Add after Remove. If your changes need to do so, undo that optimization.");
#endif
                var hashCode = InternalGetHashCode(value);
                for (var i = _buckets[hashCode % _arraysLength] - 1; i >= 0; i = _slots[i]._next)
                {
                    if (_slots[i]._hashCode == hashCode && _comparer.Equals(_slots[i]._value, value))
                    {
                        return false;
                    }
                }

                if (_count == _arraysLength)
                {
                    Resize();
                }

                var index = _count;
                ++_count;
                var bucket = hashCode % _arraysLength;
                _slots[index]._hashCode = hashCode;
                _slots[index]._value = value;
                _slots[index]._next = _buckets[bucket] - 1;
                _buckets[bucket] = index + 1;
                return true;
            }

            // If value is in set, remove it and return true; otherwise return false
            public bool Remove(TElement value)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _haveRemoved = true;
#endif
                var hashCode = InternalGetHashCode(value);
                var bucket = hashCode % _arraysLength;
                var last = -1;
                for (var i = _buckets[bucket] - 1; i >= 0; last = i, i = _slots[i]._next)
                {
                    if (_slots[i]._hashCode == hashCode && _comparer.Equals(_slots[i]._value, value))
                    {
                        if (last < 0)
                        {
                            _buckets[bucket] = _slots[i]._next + 1;
                        }
                        else
                        {
                            _slots[last]._next = _slots[i]._next;
                        }

                        _slots[i]._hashCode = -1;
                        ClearReferences(ref _slots[i]._value);
                        _slots[i]._next = -1;
                        return true;
                    }
                }

                return false;
            }

            private int InternalGetHashCode(TElement value)
            {
                // Handle comparer implementations that throw when passed null
                return (value == null) ? 0 : _comparer.GetHashCode(value) & 0x7FFFFFFF;
            }

            private void Resize()
            {
                var newSize = checked((_count * 2) + 1);
                ArrayPool<int>.Shared.Return(_buckets, clearArray: true);
                ArrayPool<Slot>.Shared.Return(_slots, clearArray: true);
                _buckets = ArrayPool<int>.Shared.Rent(newSize);
                _slots = ArrayPool<Slot>.Shared.Rent(newSize);
                _arraysLength = newSize;
                for (var i = 0; i < _count; i++)
                {
                    var bucket = _slots[i]._hashCode % newSize;
                    _slots[i]._next = _buckets[bucket] - 1;
                    _buckets[bucket] = i + 1;
                }
            }

            public void Dispose()
            {
                ArrayPool<int>.Shared.Return(_buckets, clearArray: true);
                ArrayPool<Slot>.Shared.Return(_slots, clearArray: true);
            }

            private struct Slot
            {
                internal int _hashCode;
                internal int _next;
                internal TElement _value;
            }
        }
    } // class Internal
} // namespace Proto.Promises