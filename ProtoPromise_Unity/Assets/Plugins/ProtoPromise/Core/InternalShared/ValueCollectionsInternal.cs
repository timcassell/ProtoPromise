﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
#if PROMISE_DEBUG
        internal class TraceableCollection
        {
            internal readonly StackTrace createdAt;

            internal TraceableCollection()
            {
#if false // Set to true to capture the stacktrace. Slows down execution a lot, so only do this when absolutely necessary for debugging internal promise code.
                createdAt = new StackTrace(2, false);
#else
                createdAt = null;
#endif
            }
        }

#if PROTO_PROMISE_DEVELOPER_MODE
        // Make collections class instead of struct, and inherit TraceableCollection for debugging purposes.
        partial class ValueLinkedStack<T> : TraceableCollection
        {
            internal ValueLinkedStack() : base() { }
            
            private void AssertNotInCollection(T item)
            {
                CollectionChecker<T>.AssertNotInCollection(item, this);
            }
        }

        partial class ValueLinkedStackSafe<T> : TraceableCollection
        {
            internal ValueLinkedStackSafe() : base() { }

            private void AssertNotInCollection(T item)
            {
                CollectionChecker<T>.AssertNotInCollection(item, this);
            }
        }

        partial class ValueLinkedQueue<T> : TraceableCollection
        {
            internal ValueLinkedQueue() : base() { }

            private void AssertNotInCollection(T item)
            {
                CollectionChecker<T>.AssertNotInCollection(item, this);
            }
        }

        partial class ValueWriteOnlyLinkedQueue<T> : TraceableCollection
        {
            internal ValueWriteOnlyLinkedQueue() : base() { }

            private void AssertNotInCollection(T item)
            {
                CollectionChecker<T>.AssertNotInCollection(item, this);
            }
        }
#else // PROTO_PROMISE_DEVELOPER_MODE
        private static void AssertNotInCollection<T>(T item) where T : class, ILinked<T>
        {
            CollectionChecker<T>.AssertNotInCollection(item, null);
        }
#endif // PROTO_PROMISE_DEVELOPER_MODE

        private static class CollectionChecker<T> where T : class, ILinked<T>
        {
            private static readonly Dictionary<T, TraceableCollection> _itemsInACollection = new Dictionary<T, TraceableCollection>();

            internal static void AssertNotInCollection(T item, TraceableCollection newCollection)
            {
                TraceableCollection currentCollection;
                lock (_itemsInACollection)
                {
                    if (!_itemsInACollection.TryGetValue(item, out currentCollection) && item.Next == null)
                    {
                        _itemsInACollection.Add(item, newCollection);
                        return;
                    }
                }
#if PROTO_PROMISE_DEVELOPER_MODE
                Exception innerException = currentCollection.createdAt == null ? null : new InvalidOperationException("Inhabited collection createdAt stacktrace.", currentCollection.createdAt.ToString());
#else
                Exception innerException = null;
#endif
                throw new System.InvalidOperationException("Item is in a collection, cannot add to a different collection.", innerException);
            }

            internal static void Remove(T item)
            {
                lock (_itemsInACollection)
                {
                    _itemsInACollection.Remove(item);
                }
            }
        }
#else // PROMISE_DEBUG
        static partial void AssertNotInCollection<T>(T item) where T : class, ILinked<T>;

#endif // PROMISE_DEBUG

        [MethodImpl(InlineOption)]
        static private void MarkRemovedFromCollection<T>(T item) where T : class, ILinked<T>
        {
            item.Next = null;
#if PROMISE_DEBUG
            CollectionChecker<T>.Remove(item);
#endif
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
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
        [DebuggerNonUserCode]
#endif
#if PROMISE_DEBUG && PROTO_PROMISE_DEVELOPER_MODE
        internal partial class ValueLinkedStack<T> : IEnumerable<T> where T : class, ILinked<T>
#else
        internal struct ValueLinkedStack<T> : IEnumerable<T> where T : class, ILinked<T>
#endif
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
        [DebuggerNonUserCode]
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
        [DebuggerNonUserCode]
#endif
#if PROMISE_DEBUG && PROTO_PROMISE_DEVELOPER_MODE
        internal partial class ValueLinkedStackSafe<T> where T : class, ILinked<T>
#else
        internal struct ValueLinkedStackSafe<T> where T : class, ILinked<T>
#endif
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
        [DebuggerNonUserCode]
#endif
#if PROMISE_DEBUG && PROTO_PROMISE_DEVELOPER_MODE
        internal partial class ValueLinkedQueue<T> : IEnumerable<T> where T : class, ILinked<T>
#else
        internal struct ValueLinkedQueue<T> : IEnumerable<T> where T : class, ILinked<T>
#endif
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

            internal ValueLinkedQueue(T head, T tail)
            {
                _head = head;
                _tail = tail;
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

        /// <summary>
        /// This structure is unsuitable for general purpose.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
#if PROMISE_DEBUG && PROTO_PROMISE_DEVELOPER_MODE
        internal partial class ValueWriteOnlyLinkedQueue<T> where T : class, ILinked<T>
#else
        internal struct ValueWriteOnlyLinkedQueue<T> where T : class, ILinked<T>
#endif
        {
            private readonly ILinked<T> _sentinel;
            private ILinked<T> _tail;

            internal bool IsEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _sentinel.Next == null; }
            }

            [MethodImpl(InlineOption)]
            internal ValueWriteOnlyLinkedQueue(ILinked<T> sentinel)
            {
                _tail = _sentinel = sentinel;
            }

            internal void Enqueue(T item)
            {
                AssertNotInCollection(item);

                _tail.Next = item;
                _tail = item;
            }

            internal ValueLinkedStack<T> MoveElementsToStack()
            {
                ValueLinkedStack<T> newStack = new ValueLinkedStack<T>(_sentinel.Next);
                _sentinel.Next = null;
                _tail = _sentinel;
                return newStack;
            }

            internal ValueLinkedQueue<T> MoveElementsToQueue()
            {
                ValueLinkedQueue<T> newQueue = new ValueLinkedQueue<T>(_sentinel.Next, _tail as T);
                _sentinel.Next = null;
                _tail = _sentinel;
                return newQueue;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal struct ValueLinkedStackZeroGC<T> : IEnumerable<T>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
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
            [DebuggerNonUserCode]
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

            [MethodImpl(InlineOption)]
            private ValueLinkedStackZeroGC(ValueLinkedStack<Node> stack)
            {
                _stack = stack;
            }

            [MethodImpl(InlineOption)]
            internal static ValueLinkedStackZeroGC<T> Create()
            {
                return new ValueLinkedStackZeroGC<T>(new ValueLinkedStack<Node>());
            }

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
        [DebuggerNonUserCode]
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
    [DebuggerNonUserCode]
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