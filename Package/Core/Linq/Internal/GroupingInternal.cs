#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Adapted from System.Linq.Grouping from .NET Framework
        // Source: https://github.com/dotnet/corefx/blob/b90532bc97b07234a7d18073819d019645285f1c/src/System.Linq/src/System/Linq/Grouping.cs#L64
        internal class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IList<TElement>
        {
            internal Grouping<TKey, TElement> _hashNext;
            internal Grouping<TKey, TElement> _next;
            internal TElement[] _elements;
            internal TKey _key;
            internal int _count;
            internal int _hashCode;

            public Grouping(TKey key, int hashCode, TElement[] elements, Grouping<TKey, TElement> hashNext)
            {
                _key = key;
                _hashCode = hashCode;
                _elements = elements;
                _hashNext = hashNext;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IEnumerator<TElement> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    yield return _elements[i];
                }
            }

            public TKey Key => _key;

            int ICollection<TElement>.Count => _count;

            bool ICollection<TElement>.IsReadOnly => true;

            void ICollection<TElement>.Add(TElement item) => throw new NotSupportedException();

            void ICollection<TElement>.Clear() => throw new NotSupportedException();

            bool ICollection<TElement>.Contains(TElement item) => Array.IndexOf(_elements, item, 0, _count) >= 0;

            void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex) => Array.Copy(_elements, 0, array, arrayIndex, _count);

            bool ICollection<TElement>.Remove(TElement item) => throw new NotSupportedException();

            int IList<TElement>.IndexOf(TElement item) => Array.IndexOf(_elements, item, 0, _count);

            void IList<TElement>.Insert(int index, TElement item) => throw new NotSupportedException();

            void IList<TElement>.RemoveAt(int index) => throw new NotSupportedException();

            TElement IList<TElement>.this[int index]
            {
                get
                {
                    if (index < 0 || index >= _count)
                    {
                        throw new System.ArgumentOutOfRangeException(nameof(index));
                    }

                    return _elements[index];
                }

                set => throw new NotSupportedException();
            }

            internal void Add(TElement element)
            {
                if (_elements.Length == _count)
                {
                    Array.Resize(ref _elements, checked(_count * 2));
                }

                _elements[_count] = element;
                _count++;
            }

            internal void Trim()
            {
                if (_elements.Length != _count)
                {
                    Array.Resize(ref _elements, _count);
                }
            }
        }
    } // class Internal
#endif
} // namespace Proto.Promises