#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if CSHARP_7_3_OR_NEWER

using System;
#if (NET47 || NETCOREAPP || NETSTANDARD2_0_OR_GREATER || UNITY_2021_2_OR_NEWER)
using System.Buffers;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0251 // Make member 'readonly'

#endif // CSHARP_7_3_OR_NEWER

namespace Proto.Promises.Collections
{
#if CSHARP_7_3_OR_NEWER // This is only used for async Linq, so we don't need to expose it in older runtimes.
    /// <summary>
    /// Exposes a readonly collection of items using temporary storage.
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct TempCollection<T> : IReadOnlyList<T>
    {
        // We use this type to expose items from ArrayPool arrays, so we can return to the pool safely.
        // This way we don't allocate garbage as a library.
        private readonly TempCollectionBuilder<T> _target;

        [MethodImpl(Internal.InlineOption)]
        internal TempCollection(TempCollectionBuilder<T> target)
            => _target = target;

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">The index is less than 0 or greater than <see cref="Count"/>.</exception>
        /// <exception cref="InvalidOperationException">This instance is no longer valid.</exception>
        public T this[int index]
        {
            [MethodImpl(Internal.InlineOption)]
            get
            {
                ValidateIndex(index);
                return _target._items[index];
            }
        }

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">This instance is no longer valid.</exception>
        public int Count
        {
            [MethodImpl(Internal.InlineOption)]
            get
            {
                ValidateAccess();
                return _target._count;
            }
        }

        /// <summary>
        /// Gets an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        /// <exception cref="InvalidOperationException">This instance is no longer valid.</exception>
        public Enumerator GetEnumerator()
        {
            ValidateAccess();
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Copies the items to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from this. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The <see cref="Count"/> of this is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        /// <exception cref="InvalidOperationException">This instance is no longer valid.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ValidateAccess();
            _target.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies the elements to a new <see cref="Array"/>.
        /// </summary>
        /// <returns>A new <see cref="Array"/> containing the copied elements.</returns>
        /// <exception cref="InvalidOperationException">This instance is no longer valid.</exception>
        public T[] ToArray()
        {
            ValidateAccess();
            var arr = new T[_target._count];
            CopyTo(arr, 0);
            return arr;
        }

        /// <summary>
        /// Copies the elements to a new <see cref="List{T}"/>.
        /// </summary>
        /// <returns>A new <see cref="List{T}"/> containing the copied elements.</returns>
        /// <exception cref="InvalidOperationException">This instance is no longer valid.</exception>
        public List<T> ToList()
        {
            ValidateAccess();
            int count = _target._count;
            var list = new List<T>(count);
            for (int i = 0; i < count; ++i)
            {
                list.Add(_target._items[i]);
            }
            return list;
        }

#if NETCOREAPP || NETSTANDARD2_0_OR_GREATER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Gets a readonly view of the elements.
        /// </summary>
        public ReadOnlySpan<T> Span
        {
            [MethodImpl(Internal.InlineOption)]
            get
            {
                ValidateAccess();
                return _target.Span;
            }
        }
#endif // NETCOREAPP || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        private void ValidateIndex(int index)
        {
            ValidateAccess();
            _target.ValidateIndex(index);
        }

        private void ValidateAccess()
        {
            _target.ValidateAccess();
        }
#else
        partial void ValidateIndex(int index);
        partial void ValidateAccess();
#endif

        /// <summary>
        /// Provides an enumerator for iterating over the items in the <see cref="TempCollection{T}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly TempCollection<T> _target;
            private int _current;

            /// <summary>
            /// Gets the element at the current index.
            /// </summary>
            /// <exception cref="InvalidOperationException">The collection is no longer valid.</exception>
            public T Current
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _target[_current]; }
            }

            object IEnumerator.Current => Current;

            internal Enumerator(TempCollection<T> target)
            {
                _target = target;
                _current = -1;
            }

            /// <summary>
            /// Moves the enumerator to the next index.
            /// </summary>
            /// <returns>True if there is an element at the next index, otherwise false.</returns>
            /// <exception cref="InvalidOperationException">The collection is no longer valid.</exception>
            public bool MoveNext()
            {
                int newIndex = _current + 1;
                if (newIndex >= _target.Count)
                {
                    return false;
                }
                _current = newIndex;
                return true;
            }

            /// <summary>
            /// Implementation of <see cref="IDisposable.Dispose"/>. Does nothing.
            /// </summary>
            public void Dispose() { }

            void IEnumerator.Reset() => throw new NotImplementedException();
        }
    }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
    internal sealed class TempCollectionDisposedChecker : Internal.ITraceable
    {
#if PROMISE_DEBUG
        Internal.CausalityTrace Internal.ITraceable.Trace { get; set; }
#endif

        internal bool _isDisposed;

        internal TempCollectionDisposedChecker()
        {
            Internal.SetCreatedStacktraceInternal(this, 2);
            Internal.MarkNotInPool(this);
        }

        ~TempCollectionDisposedChecker()
        {
            if (!_isDisposed)
            {
                Internal.ReportRejection(new UnreleasedObjectException("A TempCollectionDisposedChecker was garbage collected without being disposed."), this);
            }
        }
    }
#endif // PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE

    internal struct TempCollectionBuilder<T> : IDisposable
    {
        internal T[] _items;
        internal int _count;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        internal readonly TempCollectionDisposedChecker _disposedChecker;

        internal void ValidateIndex(int index)
        {
            ValidateAccess();
            if (index < 0 | index >= _count)
            {
                throw new IndexOutOfRangeException();
            }
        }

        internal void ValidateAccess()
        {
            if (_disposedChecker == null || _disposedChecker._isDisposed)
            {
                throw new InvalidOperationException("TempCollection is invalid.", Internal.GetFormattedStacktrace(2));
            }
        }
#endif // PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE

        internal TempCollection<T> View
        {
            [MethodImpl(Internal.InlineOption)]
            get { return new TempCollection<T>(this); }
        }

        internal TempCollectionBuilder(int capacity, int count = 0)
        {
            _items = ArrayPool<T>.Shared.Rent(capacity);
            _count = count;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            _disposedChecker = new TempCollectionDisposedChecker();
#endif
        }

        internal void SetCapacityNoCopy(int capacity)
        {
            Array.Clear(_items, 0, _count);
            ArrayPool<T>.Shared.Return(_items, false);
            _items = ArrayPool<T>.Shared.Rent(capacity);
        }

        internal void EnsureCapacity(int capacity)
        {
            if (capacity <= _items.Length)
            {
                return;
            }
            var newStorage = ArrayPool<T>.Shared.Rent(capacity);
            _items.CopyTo(newStorage, 0);
            Array.Clear(_items, 0, _count);
            ArrayPool<T>.Shared.Return(_items, false);
            _items = newStorage;
        }

        internal void Add(T item)
        {
            var count = _count;
            EnsureCapacity(count + 1);

            _items[count] = item;
            _count = count + 1;
        }

        public void Dispose()
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            ValidateAccess();
            _disposedChecker._isDisposed = true;
            Internal.Discard(_disposedChecker);
#endif
            Array.Clear(_items, 0, _count);
            ArrayPool<T>.Shared.Return(_items, false);
            _items = null;
            _count = -1;
        }

#if NETCOREAPP || NETSTANDARD2_0_OR_GREATER || UNITY_2021_2_OR_NEWER
        [MethodImpl(Internal.InlineOption)]
        internal void CopyTo(T[] array, int arrayIndex)
            => Span.CopyTo(array.AsSpan(arrayIndex));

        internal ReadOnlySpan<T> Span
        {
            [MethodImpl(Internal.InlineOption)]
            get { return new ReadOnlySpan<T>(_items, 0, _count); }
        }
#else
        [MethodImpl(Internal.InlineOption)]
        internal void CopyTo(T[] array, int arrayIndex)
            => Array.Copy(_items, 0, array, arrayIndex, _count);
#endif // NETCOREAPP || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER
    }
#endif // CSHARP_7_3_OR_NEWER
} // namespace Proto.Promises.Collections