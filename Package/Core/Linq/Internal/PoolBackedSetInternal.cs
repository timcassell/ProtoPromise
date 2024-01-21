#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
        // Adapted from System.Linq's Set.

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct PoolBackedSet<TElement, TEqualityComparer> : IDisposable
            where TEqualityComparer : IEqualityComparer<TElement>
        {
            private readonly TEqualityComparer _comparer;
            // We use TempCollectionBuilder to handle renting from ArrayPool.
            private TempCollectionBuilder<int> _buckets;
            private TempCollectionBuilder<Slot> _slots;
            internal int _count;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            private bool _haveRemoved;
#endif

            internal PoolBackedSet(TEqualityComparer comparer)
            {
                _comparer = comparer;
                // The smallest array returned from ArrayPool by default is 16, so we use 15 count to start instead of 7 that System.Linq uses.
                // The actual array length could be larger than the requested size, so we make sure the count is what we expect.
                _buckets = new TempCollectionBuilder<int>(15, 15);
                _slots = new TempCollectionBuilder<Slot>(15, 15);
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
                for (var i = _buckets._items[hashCode % _buckets._count] - 1; i >= 0; i = _slots._items[i]._next)
                {
                    if (_slots._items[i]._hashCode == hashCode && _comparer.Equals(_slots._items[i]._value, value))
                    {
                        return false;
                    }
                }

                if (_count == _slots._count)
                {
                    Resize();
                }

                var index = _count;
                ++_count;
                var bucket = hashCode % _buckets._count;
                _slots._items[index]._hashCode = hashCode;
                _slots._items[index]._value = value;
                _slots._items[index]._next = _buckets._items[bucket] - 1;
                _buckets._items[bucket] = index + 1;
                return true;
            }

            // If value is in set, remove it and return true; otherwise return false
            public bool Remove(TElement value)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _haveRemoved = true;
#endif
                var hashCode = InternalGetHashCode(value);
                var bucket = hashCode % _buckets._count;
                var last = -1;
                for (var i = _buckets._items[bucket] - 1; i >= 0; last = i, i = _slots._items[i]._next)
                {
                    if (_slots._items[i]._hashCode == hashCode && _comparer.Equals(_slots._items[i]._value, value))
                    {
                        if (last < 0)
                        {
                            _buckets._items[bucket] = _slots._items[i]._next + 1;
                        }
                        else
                        {
                            _slots._items[last]._next = _slots._items[i]._next;
                        }

                        _slots._items[i]._hashCode = -1;
                        _slots._items[i]._value = default;
                        _slots._items[i]._next = -1;
                        return true;
                    }
                }

                return false;
            }

            internal int InternalGetHashCode(TElement value)
            {
                // Handle comparer implementations that throw when passed null
                return (value == null) ? 0 : _comparer.GetHashCode(value) & 0x7FFFFFFF;
            }

            private void Resize()
            {
                var newSize = checked((_count * 2) + 1);
                _buckets.SetCapacityNoCopy(newSize);
                _slots.SetCapacityAndCopy(newSize);
                for (var i = 0; i < _count; i++)
                {
                    var bucket = _slots._items[i]._hashCode % newSize;
                    _slots._items[i]._next = _buckets._items[bucket] - 1;
                    _buckets._items[bucket] = i + 1;
                }
            }

            public void Dispose()
            {
                _buckets.Dispose();
                _slots.Dispose();
            }

            private struct Slot
            {
                internal int _hashCode;
                internal int _next;
                internal TElement _value;
            }
        }
    } // class Internal
#endif
} // namespace Proto.Promises