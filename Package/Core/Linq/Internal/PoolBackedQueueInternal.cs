#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
// ArrayPool for old runtime is in Proto.Promises.Collections namespace.
#if (NETCOREAPP || NETSTANDARD2_0_OR_GREATER || UNITY_2021_2_OR_NEWER)
using System.Buffers;
#else
using Proto.Promises.Collections;
#endif
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
        internal struct PoolBackedQueue<T> : IDisposable
        {
            private T[] _array;     // Pooled array
            private int _head;      // The index from which to dequeue if the queue isn't empty.
            private int _tail;      // The index at which to enqueue if the queue isn't full.
            private int _size;      // Number of elements.

            internal PoolBackedQueue(int capacity)
            {
                _array = ArrayPool<T>.Shared.Rent(capacity);
                _head = _tail = _size = 0;
            }

            internal int Count
            {
                [MethodImpl(InlineOption)]
                get => _size;
            }

            internal void Clear()
            {
                if (_size != 0)
                {
                    if (_head < _tail)
                    {
                        Array.Clear(_array, _head, _size);
                    }
                    else
                    {
                        Array.Clear(_array, _head, _array.Length - _head);
                        Array.Clear(_array, 0, _tail);
                    }

                    _size = 0;
                }

                _head = 0;
                _tail = 0;
            }

            internal void Enqueue(T item)
            {
                EnsureCapacity(_size + 1);

                _array[_tail] = item;
                MoveNext(ref _tail);
                _size++;
            }

            [MethodImpl(InlineOption)]
            internal T Dequeue()
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (_size == 0)
                {
                    throw new System.InvalidOperationException();
                }
#endif
                int head = _head;
                T removed = _array[head];
                _array[head] = default;
                MoveNext(ref _head);
                _size--;
                return removed;
            }

            [MethodImpl(InlineOption)]
            internal bool TryDequeue(out T result)
            {
                if (_size == 0)
                {
                    result = default;
                    return false;
                }
                result = Dequeue();
                return true;
            }

            [MethodImpl(InlineOption)]
            internal T Peek()
            {
                return _array[_head];
            }

            [MethodImpl(InlineOption)]
            internal bool TryPeek(out T result)
            {
                if (_size == 0)
                {
                    result = default;
                    return false;
                }

                result = Peek();
                return true;
            }

            private void SetCapacity(int capacity)
            {
                Debug.Assert(capacity >= _size);
                // Rent directly so that we can copy the elements.
                T[] newarray = ArrayPool<T>.Shared.Rent(capacity);
                if (_size > 0)
                {
                    if (_head < _tail)
                    {
                        Array.Copy(_array, _head, newarray, 0, _size);
                        Array.Clear(_array, _head, _size);
                    }
                    else
                    {
                        Array.Copy(_array, _head, newarray, 0, _array.Length - _head);
                        Array.Copy(_array, 0, newarray, _array.Length - _head, _tail);
                        Array.Clear(_array, _head, _array.Length - _head);
                        Array.Clear(_array, 0, _tail);
                    }
                }

                ArrayPool<T>.Shared.Return(_array, false);
                _array = newarray;
                _head = 0;
                _tail = (_size == capacity) ? 0 : _size;
            }

            private void MoveNext(ref int index)
            {
                // It is tempting to use the remainder operator here but it is actually much slower
                // than a simple comparison and a rarely taken branch.
                // JIT produces better code than with ternary operator ?:
                int tmp = index + 1;
                if (tmp == _array.Length)
                {
                    tmp = 0;
                }
                index = tmp;
            }

            internal void EnsureCapacity(int capacity)
            {
                if (_array.Length < capacity)
                {
                    SetCapacity(capacity);
                }
            }

            public void Dispose()
            {
                if (_size > 0)
                {
                    if (_head < _tail)
                    {
                        Array.Clear(_array, _head, _size);
                    }
                    else
                    {
                        Array.Clear(_array, _head, _array.Length - _head);
                        Array.Clear(_array, 0, _tail);
                    }
                }

                ArrayPool<T>.Shared.Return(_array, false);
            }
        }
    } // class Internal
} // namespace Proto.Promises