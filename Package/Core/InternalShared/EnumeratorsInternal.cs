#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0090 // Use 'new(...)'
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
                get => _list[_index];
            }

            object IEnumerator.Current { get { return Current; } }

            void IDisposable.Dispose() { }

            void IEnumerator.Reset() => throw new NotImplementedException();
        }

        internal readonly struct TwoItems<T> : IReadonlyIndexableCollection<T>
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
                get => index == 0 ? _item1 : _item2;
            }

            int IReadonlyIndexableCollection<T>.Count
            {
                [MethodImpl(InlineOption)]
                get => 2;
            }
        }

        internal readonly struct ThreeItems<T> : IReadonlyIndexableCollection<T>
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
                get => 3;
            }
        }

        internal readonly struct FourItems<T> : IReadonlyIndexableCollection<T>
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
                get => 4;
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
                => ++_index < _collection.Length;

            public T Current
            {
                [MethodImpl(InlineOption)]
                get => _collection[_index];
            }

            object IEnumerator.Current => Current;

            void IDisposable.Dispose() { }

            void IEnumerator.Reset() => throw new NotImplementedException();
        }

        [MethodImpl(InlineOption)]
        internal static ArrayEnumerator<T> GetGenericEnumerator<T>(this T[] array)
            => new ArrayEnumerator<T>(array);

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal struct PersistedSpanEnumerator<T> : IEnumerator<T>
        {
            private TempCollectionBuilder<T> _tempCollection;
            private int _index;

            [MethodImpl(InlineOption)]
            internal PersistedSpanEnumerator(ReadOnlySpan<T> span)
            {
                _index = -1;
                // Span can only live on the stack, so we copy the elements to a temp collection,
                // then dispose it when this enumerator is disposed.
                _tempCollection = new TempCollectionBuilder<T>(span.Length, span.Length);
                for (int i = 0; i < span.Length; ++i)
                {
                    _tempCollection[i] = span[i];
                }
            }

            [MethodImpl(InlineOption)]
            public bool MoveNext()
                => ++_index < _tempCollection._count;

            public T Current
            {
                [MethodImpl(InlineOption)]
                get => _tempCollection[_index];
            }

            object IEnumerator.Current => Current;

            [MethodImpl(InlineOption)]
            void IDisposable.Dispose() => _tempCollection.Dispose();

            void IEnumerator.Reset() => throw new NotImplementedException();
        }

        // This is used to pass the span to methods accepting a generic enumerator. (C# doesn't support passing ref structs into generics currently.)
        [MethodImpl(InlineOption)]
        internal static PersistedSpanEnumerator<T> GetPersistedEnumerator<T>(this ReadOnlySpan<T> span)
            => new PersistedSpanEnumerator<T>(span);
    }
} // namespace Proto.Promises