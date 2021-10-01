#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        static partial void AssertNotInCollection<T>(T item) where T : class, ILinked<T>;
#if PROMISE_DEBUG
        private static class CollectionChecker<T> where T : class, ILinked<T>
        {
            private static readonly HashSet<T> _itemsInACollection = new HashSet<T>();

            internal static void AssertNotInCollection(T item)
            {
                bool isInCollection;
                lock (_itemsInACollection)
                {
                    isInCollection = !_itemsInACollection.Add(item);
                }
                if (isInCollection || item.Next != null)
                {
                    throw new System.InvalidOperationException("Item is in a collection, cannot add to a different collection.");
                }
            }

            internal static void Remove(T item)
            {
                lock (_itemsInACollection)
                {
                    _itemsInACollection.Remove(item);
                }
            }
        }

        static partial void AssertNotInCollection<T>(T item) where T : class, ILinked<T>
        {
            CollectionChecker<T>.AssertNotInCollection(item);
        }
#endif
        
        [MethodImpl(InlineOption)]
        static private void MarkRemovedFromCollection<T>(T item) where T : class, ILinked<T>
        {
            item.Next = null;
#if PROMISE_DEBUG
            CollectionChecker<T>.Remove(item);
#endif
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct Enumerator<T> : IEnumerator<T> where T : class, ILinked<T>
        {
            private T _current;

            [MethodImpl(InlineOption)]
            internal Enumerator(T first)
            {
                _current = first;
            }

            /// <summary>
            /// Doesn't actually move next, just returns if Current is valid.
            /// This allows the function to be branch-less. Useful for foreach loops.
            /// </summary>
            [MethodImpl(InlineOption)]
            public bool MoveNext()
            {
                return _current != null;
            }

            /// <summary>
            /// Actually moves next and returns current.
            /// </summary>
            public T Current
            {
                [MethodImpl(InlineOption)]
                get
                {
                    T temp = _current;
                    _current = _current.Next;
                    return temp;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
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
        internal struct ValueLinkedStack<T> : IEnumerable<T> where T : class, ILinked<T>
        {
            private T _head;

            internal bool IsEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _head == null; }
            }
            internal bool IsNotEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _head != null; }
            }

            [MethodImpl(InlineOption)]
            internal ValueLinkedStack(T head)
            {
                _head = head;
            }

            [MethodImpl(InlineOption)]
            internal void Push(T item)
            {
                AssertNotInCollection(item);

                item.Next = _head;
                _head = item;
            }

            [MethodImpl(InlineOption)]
            internal T Pop()
            {
                T temp = _head;
                _head = _head.Next;
                MarkRemovedFromCollection(temp);
                return temp;
            }

            internal bool TryRemove(T item)
            {
                if (IsEmpty)
                {
                    return false;
                }
                if (item == _head)
                {
                    _head = _head.Next;
                    MarkRemovedFromCollection(item);
                    return true;
                }
                T node = _head;
                T next = node.Next;
                while (next != null)
                {
                    if (next == item)
                    {
                        node.Next = next.Next;
                        MarkRemovedFromCollection(item);
                        return true;
                    }
                    node = next;
                    next = node.Next;
                }
                return false;
            }

            [MethodImpl(InlineOption)]
            public Enumerator<T> GetEnumerator()
            {
                return new Enumerator<T>(_head);
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
        /// Use instead of Monitor.Enter(object).
        /// Must not be readonly.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct SpinLocker
        {
            volatile private int _locker;

            internal void Enter()
            {
                // Spin until we successfully get lock.
                SpinWait spinner = new SpinWait();
                while (Interlocked.Exchange(ref _locker, 1) == 1)
                {
                    spinner.SpinOnce();
                }
            }

            [MethodImpl(InlineOption)]
            internal void Exit()
            {
                _locker = 0; // Release lock.
            }
        }

        /// <summary>
        /// This structure is unsuitable for general purpose.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct ValueLinkedStackSafe<T> where T : class, ILinked<T>
        {
            volatile private T _head;

            [MethodImpl(InlineOption)]
            internal void ClearUnsafe()
            {
                // Worst case scenario, ClearUnsafe() is called concurrently with Push() and/or TryPop() and the objects are re-pooled.
                // Very low probability, probably not a big deal, not worth adding an extra lock.
                _head = null;
            }

            [MethodImpl(InlineOption)]
            internal void Push(T item, ref SpinLocker locker)
            {
                AssertNotInCollection(item);

                locker.Enter();
                item.Next = _head;
                _head = item;
                locker.Exit();
            }

            [MethodImpl(InlineOption)]
            internal T TryPop(ref SpinLocker locker)
            {
                locker.Enter();
                T obj = _head;
                if (obj == null)
                {
                    locker.Exit();
                    return null;
                }
                _head = obj.Next;
                locker.Exit();
                MarkRemovedFromCollection(obj);
                return obj;
            }
        }

        /// <summary>
        /// This structure is unsuitable for general purpose.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct ValueLinkedQueue<T> : IEnumerable<T> where T : class, ILinked<T>
        {
            private T _head;
            private T _tail;

            internal bool IsEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _head == null; }
            }
            internal bool IsNotEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _head != null; }
            }

            internal void Enqueue(T item)
            {
                AssertNotInCollection(item);

                if (_head == null)
                {
                    _head = _tail = item;
                }
                else
                {
                    _tail.Next = item;
                    _tail = item;
                }
            }

            internal void Push(T item)
            {
                AssertNotInCollection(item);

                if (_head == null)
                {
                    _head = _tail = item;
                }
                else
                {
                    item.Next = _head;
                    _head = item;
                }
            }

            /// <summary>
            /// This doesn't clear _tail when the last item is taken.
            /// </summary>
            [MethodImpl(InlineOption)]
            internal T DequeueRisky()
            {
                T temp = _head;
                _head = _head.Next;
                MarkRemovedFromCollection(temp);
                return temp;
            }

            internal bool TryRemove(T item)
            {
                if (IsEmpty)
                {
                    return false;
                }
                if (item == _head)
                {
                    _head = _head.Next;
                    if (item == _tail)
                    {
                        _tail = null;
                    }
                    MarkRemovedFromCollection(item);
                    return true;
                }
                T node = _head;
                T next = node.Next;
                while (next != null)
                {
                    if (next == item)
                    {
                        node.Next = next.Next;
                        if (item == _tail)
                        {
                            _tail = node;
                        }
                        MarkRemovedFromCollection(item);
                        return true;
                    }
                    node = next;
                    next = node.Next;
                }
                return false;
            }

            internal bool Contains(T item)
            {
                if (item == _head)
                {
                    return true;
                }
                if (IsEmpty)
                {
                    return false;
                }
                T node = _head;
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

            internal ValueLinkedStack<T> MoveElementsToStack()
            {
                ValueLinkedStack<T> newStack = new ValueLinkedStack<T>(_head);
                _head = null;
                _tail = null;
                return newStack;
            }

            [MethodImpl(InlineOption)]
            public Enumerator<T> GetEnumerator()
            {
                return new Enumerator<T>(_head);
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

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal struct ValueLinkedStackZeroGC<T> : IEnumerable<T>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class Node : ILinked<Node>
            {
                Node ILinked<Node>.Next { get; set; }

                internal T _value;

                [MethodImpl(InlineOption)]
                internal static Node GetOrCreate(T value)
                {
                    var node = ObjectPool<Node>.TryTake<Node>()
                        ?? new Node();
                    node._value = value;
                    return node;
                }

                [MethodImpl(InlineOption)]
                internal void Dispose()
                {
                    _value = default(T);
                    ObjectPool<Node>.MaybeRepool(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct Enumerator : IEnumerator<T>
            {
                private Enumerator<Node> _enumerator;

                [MethodImpl(InlineOption)]
                internal Enumerator(ValueLinkedStackZeroGC<T> stack)
                {
                    _enumerator = stack._stack.GetEnumerator();
                }

                [MethodImpl(InlineOption)]
                public bool MoveNext()
                {
                    return _enumerator.MoveNext();
                }

                public T Current
                {
                    [MethodImpl(InlineOption)]
                    get { return _enumerator.Current._value; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                void IEnumerator.Reset() { }
                void IDisposable.Dispose() { }
            }

            private ValueLinkedStack<Node> _stack;

            internal bool IsEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _stack.IsEmpty; }
            }
            internal bool IsNotEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _stack.IsNotEmpty; }
            }

            [MethodImpl(InlineOption)]
            internal void ClearWithoutRepoolUnsafe()
            {
                _stack = new ValueLinkedStack<Node>();
            }

            [MethodImpl(InlineOption)]
            internal void Push(T item)
            {
                _stack.Push(Node.GetOrCreate(item));
            }

            [MethodImpl(InlineOption)]
            internal T Pop()
            {
                var node = _stack.Pop();
                T item = node._value;
                node.Dispose();
                return item;
            }

            [MethodImpl(InlineOption)]
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
        internal struct ArrayEnumerator<T> : IEnumerator<T>
        {
            private readonly T[] _collection;
            private int _index;

            [MethodImpl(InlineOption)]
            internal ArrayEnumerator(T[] array)
            {
                _index = -1;
                _collection = array;
            }

            [MethodImpl(InlineOption)]
            public bool MoveNext()
            {
                return ++_index < _collection.Length;
            }

            public T Current
            {
                [MethodImpl(InlineOption)]
                get { return _collection[_index]; }
            }

            object IEnumerator.Current { get { return Current; } }

            void IDisposable.Dispose() { }

            void IEnumerator.Reset()
            {
#pragma warning disable RECS0083 // Shows NotImplementedException throws in the quick task bar
                throw new NotImplementedException();
#pragma warning restore RECS0083 // Shows NotImplementedException throws in the quick task bar
            }
        }
    } // class Internal

#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    internal static class ArrayExtensions
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.ArrayEnumerator<T> GetGenericEnumerator<T>(this T[] array)
        {
            return new Internal.ArrayEnumerator<T>(array);
        }
    }
} // namespace Proto.Promises