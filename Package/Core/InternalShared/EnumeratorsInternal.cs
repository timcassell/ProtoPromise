#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0066 // Convert switch statement to expression
#pragma warning disable IDE0250 // Make struct 'readonly'
#pragma warning disable IDE0251 // Make member 'readonly'

namespace Proto.Promises
{
    partial class Internal
    {
        internal interface IReadonlyIndexableCollection<T>
        {
            T this[int index] { get; }
            int Count { get; }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct Enumerator<T, TCollection> : IEnumerator<T>
            where TCollection : IReadonlyIndexableCollection<T>
        {
            private readonly TCollection _list;
            private int _index;

            [MethodImpl(InlineOption)]
            internal Enumerator(TCollection list)
            {
                _index = -1;
                _list = list;
            }

            [MethodImpl(InlineOption)]
            public bool MoveNext()
            {
                return ++_index < _list.Count;
            }

            public T Current
            {
                [MethodImpl(InlineOption)]
                get { return _list[_index]; }
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

        internal struct TwoItems<T> : IReadonlyIndexableCollection<T>
        {
            private readonly T _item1;
            private readonly T _item2;

            [MethodImpl(InlineOption)]
            internal TwoItems(T item1, T item2)
            {
                _item1 = item1;
                _item2 = item2;
            }

            T IReadonlyIndexableCollection<T>.this[int index]
            {
                [MethodImpl(InlineOption)]
                get { return index == 0 ? _item1 : _item2; }
            }

            int IReadonlyIndexableCollection<T>.Count
            {
                [MethodImpl(InlineOption)]
                get { return 2; }
            }
        }

        internal struct ThreeItems<T> : IReadonlyIndexableCollection<T>
        {
            private readonly T _item1;
            private readonly T _item2;
            private readonly T _item3;

            [MethodImpl(InlineOption)]
            internal ThreeItems(T item1, T item2, T item3)
            {
                _item1 = item1;
                _item2 = item2;
                _item3 = item3;
            }

            T IReadonlyIndexableCollection<T>.this[int index]
            {
                [MethodImpl(InlineOption)]
                get
                {
                    switch (index)
                    {
                        case 0: return _item1;
                        case 1: return _item2;
                        default: return _item3;
                    }
                }
            }

            int IReadonlyIndexableCollection<T>.Count
            {
                [MethodImpl(InlineOption)]
                get { return 3; }
            }
        }

        internal struct FourItems<T> : IReadonlyIndexableCollection<T>
        {
            private readonly T _item1;
            private readonly T _item2;
            private readonly T _item3;
            private readonly T _item4;

            [MethodImpl(InlineOption)]
            internal FourItems(T item1, T item2, T item3, T item4)
            {
                _item1 = item1;
                _item2 = item2;
                _item3 = item3;
                _item4 = item4;
            }

            T IReadonlyIndexableCollection<T>.this[int index]
            {
                [MethodImpl(InlineOption)]
                get
                {
                    switch (index)
                    {
                        case 0: return _item1;
                        case 1: return _item2;
                        case 2: return _item3;
                        default: return _item4;
                    }
                }
            }

            int IReadonlyIndexableCollection<T>.Count
            {
                [MethodImpl(InlineOption)]
                get { return 4; }
            }
        }

        [MethodImpl(InlineOption)]
        internal static Enumerator<T, TwoItems<T>> GetEnumerator<T>(T item1, T item2)
        {
            return new Enumerator<T, TwoItems<T>>(new TwoItems<T>(item1, item2));
        }

        [MethodImpl(InlineOption)]
        internal static Enumerator<T, ThreeItems<T>> GetEnumerator<T>(T item1, T item2, T item3)
        {
            return new Enumerator<T, ThreeItems<T>>(new ThreeItems<T>(item1, item2, item3));
        }

        [MethodImpl(InlineOption)]
        internal static Enumerator<T, FourItems<T>> GetEnumerator<T>(T item1, T item2, T item3, T item4)
        {
            return new Enumerator<T, FourItems<T>>(new FourItems<T>(item1, item2, item3, item4));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
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

        [MethodImpl(InlineOption)]
        internal static ArrayEnumerator<T> GetGenericEnumerator<T>(this T[] array)
        {
            return new ArrayEnumerator<T>(array);
        }
    }
} // namespace Proto.Promises