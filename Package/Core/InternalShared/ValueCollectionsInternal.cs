#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0251 // Make member 'readonly'

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
#if PROTO_PROMISE_DEVELOPER_MODE
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

        // Make collections class instead of struct, and inherit TraceableCollection for debugging purposes.
        partial class ValueLinkedStack<T> : TraceableCollection
        {
            internal ValueLinkedStack() { }

            private void AssertNotInCollection(T item)
            {
                CollectionChecker<T>.AssertNotInCollection(item, this);
            }
        }

        partial class ValueLinkedStackSafe : TraceableCollection
        {
            internal ValueLinkedStackSafe() { }

            private void AssertNotInCollection(HandleablePromiseBase item)
            {
                CollectionChecker<HandleablePromiseBase>.AssertNotInCollection(item, this);
            }
        }

        partial class ValueLinkedQueue<T> : TraceableCollection
        {
            internal ValueLinkedQueue() { }

            private void AssertNotInCollection(T item)
            {
                CollectionChecker<T>.AssertNotInCollection(item, this);
            }
        }

        private static class CollectionChecker<T> where T : class, ILinked<T>
        {
            private static readonly Dictionary<T, TraceableCollection> s_itemsInACollection = new Dictionary<T, TraceableCollection>();

            internal static void AssertNotInCollection(T item, TraceableCollection newCollection)
            {
                if (newCollection == null)
                {
                    throw new System.ArgumentNullException();
                }
                TraceableCollection currentCollection;
                lock (s_itemsInACollection)
                {
                    if (!s_itemsInACollection.TryGetValue(item, out currentCollection))
                    {
                        s_itemsInACollection.Add(item, newCollection);
                        return;
                    }
                }
                Exception innerException = currentCollection.createdAt == null ? null : new InvalidOperationException("Inhabited collection createdAt stacktrace.", currentCollection.createdAt.ToString());
                throw new System.InvalidOperationException("Item is in a collection, cannot add to a different collection.", innerException);
            }

            internal static void Remove(T item)
            {
                lock (s_itemsInACollection)
                {
                    s_itemsInACollection.Remove(item);
                }
            }
        }
#else // PROTO_PROMISE_DEVELOPER_MODE
        static partial void AssertNotInCollection<T>(T item) where T : class, ILinked<T>;
#endif // PROTO_PROMISE_DEVELOPER_MODE

        [MethodImpl(InlineOption)]
        static private void MarkRemovedFromCollection<T>(T item) where T : class, ILinked<T>
        {
            item.Next = null;
#if PROTO_PROMISE_DEVELOPER_MODE
            CollectionChecker<T>.Remove(item);
#endif
        }

        /// <summary>
        /// This structure is unsuitable for general purpose.
        /// </summary>
#if PROTO_PROMISE_DEVELOPER_MODE
        internal partial class ValueLinkedStack<T> where T : class, ILinked<T>
#else
        [DebuggerNonUserCode, StackTraceHidden]
        internal struct ValueLinkedStack<T> where T : class, ILinked<T>
#endif
        {
            private T _head;

            internal bool IsEmpty
            {
                [MethodImpl(InlineOption)]
                get => _head == null;
            }
            internal bool IsNotEmpty
            {
                [MethodImpl(InlineOption)]
                get => _head != null;
            }

            [MethodImpl(InlineOption)]
            internal ValueLinkedStack(T head)
            {
                _head = head;
            }

            [MethodImpl(InlineOption)]
            internal ValueLinkedStack<T> TakeAndClear()
            {
                T temp = _head;
                _head = null;
                return new ValueLinkedStack<T>(temp);
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
        }

        /// <summary>
        /// This structure is unsuitable for general purpose.
        /// </summary>
#if PROTO_PROMISE_DEVELOPER_MODE
        internal partial class ValueLinkedStackSafe
#else
        [DebuggerNonUserCode, StackTraceHidden]
        internal struct ValueLinkedStackSafe
#endif
        {
            private HandleablePromiseBase _head;
            // This must not be readonly.
            private SpinLocker _locker;

            [MethodImpl(InlineOption)]
            internal ValueLinkedStackSafe(HandleablePromiseBase tailSentinel)
            {
                // Sentinel is PromiseRefBase.InvalidAwaitSentinel.s_instance, it references itself so we don't need any null checks.
                _head = tailSentinel;
                _locker = new SpinLocker();
            }

            [MethodImpl(InlineOption)]
            internal void ClearUnsafe()
            {
                // Worst case scenario, ClearUnsafe() is called concurrently with Push() and/or TryPop() and the objects are re-pooled.
                // Very low probability, probably not a big deal, not worth adding an extra lock.
                _head = PromiseRefBase.InvalidAwaitSentinel.s_instance;
            }

            [MethodImpl(InlineOption)]
            internal void Push(HandleablePromiseBase item)
            {
                AssertNotInCollection(item);

                _locker.Enter();
                item._next = _head;
                _head = item;
                _locker.Exit();
            }

            [MethodImpl(InlineOption)]
            internal HandleablePromiseBase PopOrInvalid()
            {
                // We use InvalidAwaitSentinel as the sentinel, so we don't need a branch here to check the bottom of the stack, because it references itself.
                _locker.Enter();
                var head = _head;
                _head = head._next;
                _locker.Exit();

#if PROTO_PROMISE_DEVELOPER_MODE
                CollectionChecker<HandleablePromiseBase>.Remove(head);
#endif
                return head;
            }
        }

        /// <summary>
        /// This structure is unsuitable for general purpose.
        /// </summary>
#if PROTO_PROMISE_DEVELOPER_MODE
        internal partial class ValueLinkedQueue<T> where T : class, ILinked<T>
#else
        [DebuggerNonUserCode, StackTraceHidden]
        internal struct ValueLinkedQueue<T> where T : class, ILinked<T>
#endif
        {
            private T _head;
            private T _tail;

            internal bool IsEmpty
            {
                [MethodImpl(InlineOption)]
                get => _head == null;
            }

            internal bool IsNotEmpty
            {
                [MethodImpl(InlineOption)]
                get => _head != null;
            }

            [MethodImpl(InlineOption)]
            internal ValueLinkedQueue(T head)
            {
                _head = head;
                _tail = head;
            }

            // Only use if this is known to be not empty.
            internal void EnqueueUnsafe(T item)
            {
                AssertNotInCollection(item);

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (_head == null)
                {
                    throw new System.InvalidOperationException("EnqueueUnsafe must only be used on a non-empty queue.");
                }
#endif

                _tail.Next = item;
                _tail = item;
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

            internal T Dequeue()
            {
                T head = _head;
                if (_tail == head)
                {
                    _head = null;
                    _tail = null;
                }
                else
                {
                    _head = head.Next;
                }

                MarkRemovedFromCollection(head);
                return head;
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

            internal ValueLinkedStack<T> MoveElementsToStack()
            {
                var newStack = new ValueLinkedStack<T>(_head);
                _head = null;
                _tail = null;
                return newStack;
            }

            internal ValueLinkedStack<T> MoveElementsToStack(int maxCount, out int actualCount)
            {
                var current = _head;
                if (maxCount <= 0 | current == null)
                {
                    actualCount = 0;
                    return new ValueLinkedStack<T>();
                }

                var newStack = new ValueLinkedStack<T>(current);
                var next = current.Next;
                int count = 1;
                while (true)
                {
                    if (next == null)
                    {
                        _head = null;
                        _tail = null;
                        actualCount = count;
                        return newStack;
                    }
                    if (count == maxCount)
                    {
                        break;
                    }
                    ++count;
                    current = next;
                    next = current.Next;
                }

                current.Next = null;
                _head = next;
                actualCount = count;
                return newStack;
            }

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
            internal void TakeAndEnqueueElements(ref ValueLinkedQueue<T> other)
            {
                if (other.IsEmpty)
                {
                    return;
                }
                if (IsEmpty)
                {
                    _head = other._head;
                }
                else
                {
                    _tail.Next = other._head;
                }
                _tail = other._tail;
                other._head = null;
                other._tail = null;
            }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        }

        /// <summary>
        /// This structure is unsuitable for general purpose.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct ValueList<T>
        {
            // This structure is a specialized version of List<T> without the extra object overhead and unused methods.

            private T[] _storage;
            private int _count;

            internal int Count
            {
                [MethodImpl(InlineOption)]
                get => _count;
            }

            internal T this[int index]
            {
                [MethodImpl(InlineOption)]
                get => _storage[index];
                [MethodImpl(InlineOption)]
                set => _storage[index] = value;
            }

            [MethodImpl(InlineOption)]
            internal ValueList(int capacity)
            {
                _storage = new T[Math.Max(capacity, 8)];
                _count = 0;
            }

            [MethodImpl(InlineOption)]
            internal void Add(T item)
            {
                var count = _count;
                if (_storage.Length <= count)
                {
                    Array.Resize(ref _storage, count * 2);
                }
                _storage[count] = item;
                _count = count + 1;
            }

            [MethodImpl(InlineOption)]
            internal void Clear()
            {
                _count = 0;
                Array.Clear(_storage, 0, _storage.Length);
            }
        }
    } // class Internal
} // namespace Proto.Promises