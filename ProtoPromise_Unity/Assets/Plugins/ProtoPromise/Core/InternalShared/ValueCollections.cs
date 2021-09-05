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
        static partial void AssertNotInCollection<T>(T item) where T : class, ILinked<T>
        {
            if (item.Next != null)
            {
                throw new System.InvalidOperationException("Item is in a collection, cannot add to a different collection.");
            }
        }
#endif

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
        internal struct ValueLinkedStack<T> : IEnumerable<T> where T : class, ILinked<T>
        {
            private T _first;

            internal bool IsEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _first == null; }
            }
            internal bool IsNotEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _first != null; }
            }

            [MethodImpl(InlineOption)]
            internal ValueLinkedStack(T item)
            {
                // No need to check item, this is only used to copy a collection to a new ValueLinkedStack.
                _first = item;
            }

            [MethodImpl(InlineOption)]
            internal void Clear()
            {
                _first = null;
            }

            [MethodImpl(InlineOption)]
            internal void Push(T item)
            {
                AssertNotInCollection(item);

                item.Next = _first;
                _first = item;
            }

            [MethodImpl(InlineOption)]
            internal T Pop()
            {
                T temp = _first;
                _first = _first.Next;
                temp.Next = null;
                return temp;
            }

            internal bool TryRemove(T item)
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

            [MethodImpl(InlineOption)]
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
            volatile private T _first;

            [MethodImpl(InlineOption)]
            internal void ClearUnsafe()
            {
                // Worst case scenario, ClearUnsafe() is called concurrently with Push() and/or TryPop() and the objects are re-pooled.
                // Very low probability, probably not a big deal, not worth adding an extra lock.
                _first = null;
            }

            [MethodImpl(InlineOption)]
            internal void Push(T item, ref SpinLocker locker)
            {
                AssertNotInCollection(item);

                locker.Enter();
                item.Next = _first;
                _first = item;
                locker.Exit();
            }

            [MethodImpl(InlineOption)]
            internal T TryPop(ref SpinLocker locker)
            {
                locker.Enter();
                T obj = _first;
                if (obj == null)
                {
                    locker.Exit();
                    return null;
                }
                _first = obj.Next;
                locker.Exit();
                obj.Next = null;
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
            private T _first;
            private T _last;

            internal bool IsEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _first == null; }
            }
            internal bool IsNotEmpty
            {
                [MethodImpl(InlineOption)]
                get { return _first != null; }
            }

            [MethodImpl(InlineOption)]
            internal ValueLinkedQueue(T item)
            {
                AssertNotInCollection(item);

                _first = _last = item;
            }

            [MethodImpl(InlineOption)]
            internal void Clear()
            {
                _first = null;
                _last = null;
            }

            [MethodImpl(InlineOption)]
            internal void ClearLast()
            {
                _last = null;
            }

            internal void Enqueue(T item)
            {
                AssertNotInCollection(item);

                if (_first == null)
                {
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
            [MethodImpl(InlineOption)]
            internal void EnqueueRisky(T item)
            {
                AssertNotInCollection(item);

                _last.Next = item;
                _last = item;
            }

            internal void Push(T item)
            {
                AssertNotInCollection(item);

                if (_first == null)
                {
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
            [MethodImpl(InlineOption)]
            internal void PushRisky(T item)
            {
                AssertNotInCollection(item);

                item.Next = _first;
                _first = item;
            }

            internal void PushAndClear(ref ValueLinkedQueue<T> other)
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

            internal T Dequeue()
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
            [MethodImpl(InlineOption)]
            internal T DequeueRisky()
            {
                T temp = _first;
                _first = _first.Next;
                temp.Next = null;
                return temp;
            }

            internal bool TryRemove(T item)
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

            internal bool Contains(T item)
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

            [MethodImpl(InlineOption)]
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
                    var node = Promises.Internal.ObjectPool<Node>.TryTake<Node>()
                        ?? new Node();
                    node._value = value;
                    return node;
                }

                [MethodImpl(InlineOption)]
                internal void Dispose()
                {
                    _value = default(T);
                    Promises.Internal.ObjectPool<Node>.MaybeRepool(this);
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
                    get
                    {
                        return _enumerator.Current._value;
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
            private ValueLinkedStackZeroGC(ValueLinkedStack<Node> stack)
            {
                _stack = stack;
            }

            [MethodImpl(InlineOption)]
            internal void ClearWithoutRepoolUnsafe()
            {
                _stack.Clear();
            }

            [MethodImpl(InlineOption)]
            internal ValueLinkedStackZeroGC<T> ClearWithoutRepoolAndGetCopy(ref SpinLocker locker)
            {
                locker.Enter();
                ValueLinkedStack<Node> newStack = _stack;
                _stack.Clear();
                locker.Exit();
                return new ValueLinkedStackZeroGC<T>(newStack);
            }

            [MethodImpl(InlineOption)]
            internal void PushUnsafe(T item)
            {
                _stack.Push(Node.GetOrCreate(item));
            }

            [MethodImpl(InlineOption)]
            internal T PopUnsafe()
            {
                var node = _stack.Pop();
                T item = node._value;
                node.Dispose();
                return item;
            }

            [MethodImpl(InlineOption)]
            internal void Push(T item, ref SpinLocker locker)
            {
                Node node = Node.GetOrCreate(item);
                locker.Enter();
                _stack.Push(node);
                locker.Exit();
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