#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections;
using System.Collections.Generic;

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

    [System.Diagnostics.DebuggerNonUserCode]
    public struct Enumerator<T> : IEnumerator<T> where T : class, ILinked<T>
    {
        private T _current;

        public Enumerator(T first)
        {
            _current = first;
        }

        /// <summary>
        /// Doesn't actually move next, just returns if Current is valid.
        /// This allows the function to be branch-less. Useful for foreach loops.
        /// </summary>
        public bool MoveNext()
        {
            return _current != null;
        }

        /// <summary>
        /// Actually moves next and returns current.
        /// </summary>
        public T Current
        {
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
    [System.Diagnostics.DebuggerNonUserCode]
    public struct ValueLinkedStack<T> : IEnumerable<T> where T : class, ILinked<T>
    {
        T _first;

        public bool IsEmpty { get { return _first == null; } }
        public bool IsNotEmpty { get { return _first != null; } }

        public ValueLinkedStack(T item)
        {
            item.Next = null;
            _first = item;
        }

        public void PushAndClear(ref ValueLinkedQueue<T> queue)
        {
            if (queue.IsNotEmpty)
            {
                queue.PeekLast().Next = _first;
                _first = queue.Peek();
                queue.Clear();
            }
        }

        public void Clear()
        {
            _first = null;
        }

        public void Push(T item)
        {
            item.Next = _first;
            _first = item;
        }

        public T Pop()
        {
            T temp = _first;
            _first = _first.Next;
            temp.Next = null;
            return temp;
        }

        public T Peek()
        {
            return _first;
        }

        public void Remove(T item)
        {
            if (item == _first)
            {
                _first = _first.Next;
                item.Next = null;
                return;
            }
            T node = _first;
            T next = node.Next;
            while (next != null)
            {
                if (next == item)
                {
                    node.Next = next.Next;
                    item.Next = null;
                    return;
                }
                node = next;
                next = node.Next;
            }
        }

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
    [System.Diagnostics.DebuggerNonUserCode]
    public struct ValueLinkedQueue<T> : IEnumerable<T> where T : class, ILinked<T>
    {
        T _first;
        T _last;

        public bool IsEmpty { get { return _first == null; } }
        public bool IsNotEmpty { get { return _first != null; } }

        public ValueLinkedQueue(T item)
        {
            item.Next = null;
            _first = _last = item;
        }

        public void Clear()
        {
            _first = null;
            _last = null;
        }

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

        public void EnqueueAndClear(ref ValueLinkedQueue<T> other)
        {
            if (IsEmpty)
            {
                this = other;
                other.Clear();
            }
            else if (other.IsNotEmpty)
            {
                _last.Next = other._first;
                _last = other._last;
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
        public T DequeueRisky()
        {
            T temp = _first;
            _first = _first.Next;
            temp.Next = null;
            return temp;
        }

        public T Peek()
        {
            return _first;
        }

        public T PeekLast()
        {
            return _last;
        }

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
    [System.Diagnostics.DebuggerNonUserCode]
    public sealed class ReusableValueContainer<T> : IValueContainer<T>, IDisposable, ILinked<ReusableValueContainer<T>>
    {
#pragma warning disable RECS0108 // Warns about static fields in generic types
        private static ValueLinkedStack<ReusableValueContainer<T>> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

        public static void ClearPool()
        {
            _pool.Clear();
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
            ReusableValueContainer<T> node = _pool.IsNotEmpty ? _pool.Pop() : new ReusableValueContainer<T>();
            node.Value = value;
            return node;
        }

        /// <summary>
        /// Adds this object back to the pool.
        /// Don't try to access it after disposing! Results are undefined.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the
        /// <see cref="T:ProtoPromise.ReusableValueContainer`1"/>. The <see cref="Dispose"/> method leaves the
        /// <see cref="T:ProtoPromise.ReusableValueContainer`1"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the
        /// <see cref="T:ProtoPromise.ReusableValueContainer`1"/> so the garbage collector can reclaim the memory that
        /// the <see cref="T:ProtoPromise.ReusableValueContainer`1"/> was occupying.</remarks>
        public void Dispose()
        {
            Value = default(T);
            _pool.Push(this);
        }
    }

    [System.Diagnostics.DebuggerNonUserCode]
    public struct ValueLinkedStackZeroGC<T> : IEnumerable<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            Enumerator<ReusableValueContainer<T>> enumerator;

            public Enumerator(ValueLinkedStackZeroGC<T> stack)
            {
                enumerator = stack._stack.GetEnumerator();
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public T Current
            {
                get
                {
                    return enumerator.Current.Value;
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

        public static void ClearPooledNodes()
        {
            ReusableValueContainer<T>.ClearPool();
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

        public T Peek()
        {
            return _stack.Peek().Value;
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

    [System.Diagnostics.DebuggerNonUserCode]
    public struct ValueLinkedQueueZeroGC<T> : IEnumerable<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            Enumerator<ReusableValueContainer<T>> enumerator;

            public Enumerator(ValueLinkedQueueZeroGC<T> queue)
            {
                enumerator = queue._queue.GetEnumerator();
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public T Current
            {
                get
                {
                    return enumerator.Current.Value;
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

        public static void ClearPooledNodes()
        {
            ReusableValueContainer<T>.ClearPool();
        }

        private ValueLinkedQueue<ReusableValueContainer<T>> _queue;

        public bool IsEmpty { get { return _queue.IsEmpty; } }
        public bool IsNotEmpty { get { return _queue.IsNotEmpty; } }

        public void Clear()
        {
            while (_queue.IsNotEmpty)
            {
                _queue.DequeueRisky().Dispose();
            }
            _queue.ClearLast();
        }

        public void ClearLast()
        {
            _queue.ClearLast();
        }

        public void ClearAndDontRepool()
        {
            _queue.Clear();
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(ReusableValueContainer<T>.New(item));
        }

        /// <summary>
        /// Only use this if you know the queue is not empty.
        /// </summary>
        public void EnqueueRisky(T item)
        {
            _queue.EnqueueRisky(ReusableValueContainer<T>.New(item));
        }

        public void Push(T item)
        {
            _queue.Push(ReusableValueContainer<T>.New(item));
        }

        /// <summary>
        /// Only use this if you know the queue is not empty.
        /// </summary>
        public void PushRisky(T item)
        {
            _queue.PushRisky(ReusableValueContainer<T>.New(item));
        }

        public T Dequeue()
        {
            var node = _queue.Dequeue();
            T item = node.Value;
            node.Dispose();
            return item;
        }

        /// <summary>
        /// This doesn't clear _last when the last item is taken.
        /// Only use this if you know this has 2 or more items, or if you will call ClearLast after a loop that takes all the items.
        /// </summary>
        public T DequeueRisky()
        {
            var node = _queue.DequeueRisky();
            T item = node.Value;
            node.Dispose();
            return item;
        }

        public T Peek()
        {
            return _queue.Peek().Value;
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
    [System.Diagnostics.DebuggerNonUserCode]
    public struct ArrayEnumerator<T> : IEnumerator<T>
    {
        private T[] collection;
        private int index;

        public ArrayEnumerator(T[] array)
        {
            index = -1;
            collection = array;
        }

        public T Current
        {
            get
            {
                return collection[index];
            }
        }

        object IEnumerator.Current { get { return Current; } }

        void IDisposable.Dispose() { }

        bool IEnumerator.MoveNext()
        {
            return ++index < collection.Length;
        }

        void IEnumerator.Reset()
        {
#pragma warning disable RECS0083 // Shows NotImplementedException throws in the quick task bar
            throw new NotImplementedException();
#pragma warning restore RECS0083 // Shows NotImplementedException throws in the quick task bar
        }
    }

    [System.Diagnostics.DebuggerNonUserCode]
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