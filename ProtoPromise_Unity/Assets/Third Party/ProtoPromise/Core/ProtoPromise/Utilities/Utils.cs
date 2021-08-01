#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Proto.Utils
{
    public interface IValueContainer<T>
    {
        T Value { get; }
    }

    public interface ILinked<T> where T : class, ILinked<T>
    {
        T Next { get; set; }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public struct Enumerator<T> : IEnumerator<T> where T : class, ILinked<T>
    {
        private T _current;

        [MethodImpl(Promises.Internal.InlineOption)]
        public Enumerator(T first)
        {
            _current = first;
        }

        /// <summary>
        /// Doesn't actually move next, just returns if Current is valid.
        /// This allows the function to be branch-less. Useful for foreach loops.
        /// </summary>
        [MethodImpl(Promises.Internal.InlineOption)]
        public bool MoveNext()
        {
            return _current != null;
        }

        /// <summary>
        /// Actually moves next and returns current.
        /// </summary>
        public T Current
        {
            [MethodImpl(Promises.Internal.InlineOption)]
            get
            {
                T temp = _current;
                _current = _current.Next;
                return temp;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        void IEnumerator.Reset() { }

        void IDisposable.Dispose() { }
    }

    /// <summary>
    /// This structure is unsuitable for general purpose.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public struct ValueLinkedStack<T> : IEnumerable<T> where T : class, ILinked<T>
    {
        private T _first;

        public bool IsEmpty
        {
            [MethodImpl(Promises.Internal.InlineOption)]
            get { return _first == null; }
        }
        public bool IsNotEmpty
        {
            [MethodImpl(Promises.Internal.InlineOption)]
            get { return _first != null; }
        }

        [MethodImpl(Promises.Internal.InlineOption)]
        public ValueLinkedStack(T item)
        {
            item.Next = null;
            _first = item;
        }

        [MethodImpl(Promises.Internal.InlineOption)]
        public void Clear()
        {
            _first = null;
        }

        [MethodImpl(Promises.Internal.InlineOption)]
        public void Push(T item)
        {
            item.Next = _first;
            _first = item;
        }

        [MethodImpl(Promises.Internal.InlineOption)]
        public T Pop()
        {
            T temp = _first;
            _first = _first.Next;
            temp.Next = null;
            return temp;
        }

        public bool TryRemove(T item)
        {
            if (IsEmpty)
            {
                return false;
            }
            if (item == _first)
            {
                _first = _first.Next;
                item.Next = null;
                return true;
            }
            T node = _first;
            T next = node.Next;
            while (next != null)
            {
                if (next == item)
                {
                    node.Next = next.Next;
                    item.Next = null;
                    return true;
                }
                node = next;
                next = node.Next;
            }
            return false;
        }

        [MethodImpl(Promises.Internal.InlineOption)]
        public Enumerator<T> GetEnumerator()
        {
            return new Enumerator<T>(_first);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// This structure is unsuitable for general purpose.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public struct ValueLinkedQueue<T> : IEnumerable<T> where T : class, ILinked<T>
    {
        // TODO: Use a sentinel object with a circular reference to make insertions/removals branchless.
        private T _first;
        private T _last;

        public bool IsEmpty
        {
            [MethodImpl(Promises.Internal.InlineOption)]
            get { return _first == null; }
        }
        public bool IsNotEmpty
        {
            [MethodImpl(Promises.Internal.InlineOption)]
            get { return _first != null; }
        }

        [MethodImpl(Promises.Internal.InlineOption)]
        public ValueLinkedQueue(T item)
        {
            item.Next = null;
            _first = _last = item;
        }

        [MethodImpl(Promises.Internal.InlineOption)]
        public void Clear()
        {
            _first = null;
            _last = null;
        }

        [MethodImpl(Promises.Internal.InlineOption)]
        public void ClearLast()
        {
            _last = null;
        }

        public void Enqueue(T item)
        {
            if (_first == null)
            {
                item.Next = null;
                _first = _last = item;
            }
            else
            {
                _last.Next = item;
                _last = item;
            }
        }

        /// <summary>
        /// Only use this if you know the queue is not empty.
        /// </summary>
        [MethodImpl(Promises.Internal.InlineOption)]
        public void EnqueueRisky(T item)
        {
            item.Next = null;
            _last.Next = item;
            _last = item;
        }

        public void Push(T item)
        {
            if (_first == null)
            {
                item.Next = null;
                _first = _last = item;
            }
            else
            {
                item.Next = _first;
                _first = item;
            }
        }

        /// <summary>
        /// Only use this if you know the queue is not empty.
        /// </summary>
        [MethodImpl(Promises.Internal.InlineOption)]
        public void PushRisky(T item)
        {
            item.Next = null;
            item.Next = _first;
            _first = item;
        }

        public void PushAndClear(ref ValueLinkedQueue<T> other)
        {
            if (IsEmpty)
            {
                this = other;
                other.Clear();
            }
            else if (other.IsNotEmpty)
            {
                other._last.Next = _first;
                _first = other._first;
                other.Clear();
            }
        }

        public T Dequeue()
        {
            T temp = _first;
            _first = _first.Next;
            temp.Next = null;
            if (_first == null)
            {
                _last = null;
            }
            return temp;
        }

        /// <summary>
        /// This doesn't clear _last when the last item is taken.
        /// Only use this if you know this has 2 or more items, or if you will call ClearLast when you know this is empty.
        /// </summary>
        [MethodImpl(Promises.Internal.InlineOption)]
        public T DequeueRisky()
        {
            T temp = _first;
            _first = _first.Next;
            temp.Next = null;
            return temp;
        }

        public bool TryRemove(T item)
        {
            if (IsEmpty)
            {
                return false;
            }
            if (item == _first)
            {
                _first = _first.Next;
                item.Next = null;
                if (item == _last)
                {
                    _last = null;
                }
                return true;
            }
            T node = _first;
            T next = node.Next;
            while (next != null)
            {
                if (next == item)
                {
                    node.Next = next.Next;
                    item.Next = null;
                    if (item == _last)
                    {
                        _last = node;
                    }
                    return true;
                }
                node = next;
                next = node.Next;
            }
            return false;
        }

        public bool Contains(T item)
        {
            if (item == _first)
            {
                return true;
            }
            if (IsEmpty)
            {
                return false;
            }
            T node = _first;
            T next = node.Next;
            while (next != null)
            {
                if (next == item)
                {
                    return true;
                }
                node = next;
                next = node.Next;
            }
            return false;
        }

        [MethodImpl(Promises.Internal.InlineOption)]
        public Enumerator<T> GetEnumerator()
        {
            return new Enumerator<T>(_first);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Reusable value container. Use <see cref="New(T)"/> to reuse a pooled instance or create a new instance, use <see cref="Dispose"/> to add the instance back to the pool.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public sealed class ReusableValueContainer<T> : IValueContainer<T>, IDisposable, ILinked<ReusableValueContainer<T>>
    {
        private struct Creator : Promises.Internal.ICreator<ReusableValueContainer<T>>
        {
            [MethodImpl(Promises.Internal.InlineOption)]
            public ReusableValueContainer<T> Create()
            {
                return new ReusableValueContainer<T>();
            }
        }

        ReusableValueContainer<T> ILinked<ReusableValueContainer<T>>.Next { get; set; }

        public T Value { get; set; }

        /// <summary>
        /// Returns a new reusable value container containing <paramref name="value"/>.
        /// It will try to take from the pool, otherwise it will create a new object.
        /// Call <see cref="Dispose"/> when you are finished with this to add it back to the pool.
        /// </summary>
        public static ReusableValueContainer<T> New(T value)
        {
            var node = Promises.Internal.ObjectPool<ReusableValueContainer<T>>.GetOrCreate<ReusableValueContainer<T>, Creator>();
            node.Value = value;
            return node;
        }

        /// <summary>
        /// Adds this object back to the pool.
        /// Don't try to access it after disposing! Results are undefined.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the
        /// <see cref="ReusableValueContainer{T}"/>. The <see cref="Dispose"/> method leaves the
        /// <see cref="ReusableValueContainer{T}"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the
        /// <see cref="ReusableValueContainer{T}"/> so the garbage collector can reclaim the memory that
        /// the <see cref="ReusableValueContainer{T}"/> was occupying.</remarks>
        public void Dispose()
        {
            Value = default(T);
            Promises.Internal.ObjectPool<ReusableValueContainer<T>>.MaybeRepool(this);
        }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public struct ValueLinkedStackZeroGC<T> : IEnumerable<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            private Enumerator<ReusableValueContainer<T>> _enumerator;

            public Enumerator(ValueLinkedStackZeroGC<T> stack)
            {
                _enumerator = stack._stack.GetEnumerator();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public T Current
            {
                get
                {
                    return _enumerator.Current.Value;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset() { }

            void IDisposable.Dispose() { }
        }

        private ValueLinkedStack<ReusableValueContainer<T>> _stack;

        public bool IsEmpty { get { return _stack.IsEmpty; } }
        public bool IsNotEmpty { get { return _stack.IsNotEmpty; } }

        public void Clear()
        {
            while (_stack.IsNotEmpty)
            {
                _stack.Pop().Dispose();
            }
        }

        public void ClearAndDontRepool()
        {
            _stack.Clear();
        }

        public void Push(T item)
        {
            _stack.Push(ReusableValueContainer<T>.New(item));
        }

        public T Pop()
        {
            var node = _stack.Pop();
            T item = node.Value;
            node.Dispose();
            return item;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Generic Array enumerator. Use this instead of the default <see cref="Array.GetEnumerator"/> for passing it around as an <see cref="IEnumerator{T}"/>.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public struct ArrayEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] _collection;
        private int _index;

        public ArrayEnumerator(T[] array)
        {
            _index = -1;
            _collection = array;
        }

        public T Current
        {
            get
            {
                return _collection[_index];
            }
        }

        object IEnumerator.Current { get { return Current; } }

        void IDisposable.Dispose() { }

        bool IEnumerator.MoveNext()
        {
            return ++_index < _collection.Length;
        }

        void IEnumerator.Reset()
        {
#pragma warning disable RECS0083 // Shows NotImplementedException throws in the quick task bar
            throw new NotImplementedException();
#pragma warning restore RECS0083 // Shows NotImplementedException throws in the quick task bar
        }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public static class ArrayExtensions
    {
        /// <summary>
        /// Get a generic array enumerator. Use this instead of the default <see cref="Array.GetEnumerator"/> for passing it around as an <see cref="IEnumerator{T}"/>.
        /// </summary>
        public static ArrayEnumerator<T> GetGenericEnumerator<T>(this T[] array)
        {
            return new ArrayEnumerator<T>(array);
        }
    }
}