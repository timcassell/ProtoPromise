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

namespace Proto.Promises.Collections
{
    // Double-ended queue, backed by ArrayPool.
    internal struct PoolBackedDeque<T> : IDisposable
    {
        private T[] _array;     // Pooled array
        private int _head;      // First valid element in the queue
        private int _tail;      // First open slot in the dequeue, unless the dequeue is full
        private int _size;      // Number of elements.

        internal PoolBackedDeque(int capacity)
        {
            _array = ArrayPool<T>.Shared.Rent(capacity);
            _head = _tail = _size = 0;
        }

        internal int Count
        {
            [MethodImpl(Internal.InlineOption)]
            get => _size;
        }

        public bool IsEmpty
        {
            [MethodImpl(Internal.InlineOption)]
            get => _size == 0;
        }

        public bool IsNotEmpty
        {
            [MethodImpl(Internal.InlineOption)]
            get => _size != 0;
        }

        public void EnqueueTail(T item)
        {
            if (_size == _array.Length)
            {
                Grow();
            }

            _array[_tail] = item;
            if (++_tail == _array.Length)
            {
                _tail = 0;
            }
            _size++;
        }

        //// Uncomment if/when enqueueing at the head is needed
        //public void EnqueueHead(T item)
        //{
        //    if (_size == _array.Length)
        //    {
        //        Grow();
        //    }
        //
        //    _head = (_head == 0 ? _array.Length : _head) - 1;
        //    _array[_head] = item;
        //    _size++;
        //}

        public T DequeueHead()
        {
            Debug.Assert(!IsEmpty); // caller's responsibility to make sure there are elements remaining

            T item = _array[_head];
            _array[_head] = default;

            if (++_head == _array.Length)
            {
                _head = 0;
            }
            _size--;

            return item;
        }

        public T PeekHead()
        {
            Debug.Assert(!IsEmpty); // caller's responsibility to make sure there are elements remaining
            return _array[_head];
        }

        //// Uncomment if/when peeking at the tail is needed
        //public T PeekTail()
        //{
        //    Debug.Assert(!IsEmpty); // caller's responsibility to make sure there are elements remaining
        //    var index = _tail - 1;
        //    if (index == -1)
        //    {
        //        index = _array.Length - 1;
        //    }
        //    return _array[index];
        //}

        public T DequeueTail()
        {
            Debug.Assert(!IsEmpty); // caller's responsibility to make sure there are elements remaining

            if (--_tail == -1)
            {
                _tail = _array.Length - 1;
            }

            T item = _array[_tail];
            _array[_tail] = default;

            _size--;
            return item;
        }

        private void Grow()
        {
            Debug.Assert(_size == _array.Length);
            Debug.Assert(_head == _tail);

            const int MinimumGrow = 4;

            int capacity = (int) (_array.Length * 2L);
            if (capacity < _array.Length + MinimumGrow)
            {
                capacity = _array.Length + MinimumGrow;
            }

            T[] newArray = ArrayPool<T>.Shared.Rent(capacity);

            if (_head < _tail)
            {
                Array.Copy(_array, _head, newArray, 0, _size);
                Array.Clear(_array, _head, _size);
            }
            else
            {
                Array.Copy(_array, _head, newArray, 0, _array.Length - _head);
                Array.Copy(_array, 0, newArray, _array.Length - _head, _tail);
                Array.Clear(_array, _head, _array.Length - _head);
                Array.Clear(_array, 0, _tail);
            }

            ArrayPool<T>.Shared.Return(_array, false);
            _array = newArray;
            _head = 0;
            _tail = _size;
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
}