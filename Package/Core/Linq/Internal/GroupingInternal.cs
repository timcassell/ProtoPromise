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
using System.Linq;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class Grouping<TKey, TElement> : HandleablePromiseBase, IGrouping<TKey, TElement>, IList<TElement>, IDisposable
        {
            internal Grouping<TKey, TElement> _hashNext;
            internal Grouping<TKey, TElement> _nextGrouping;
            internal TempCollectionBuilder<TElement> _elements;
            internal TKey _key;
            internal int _hashCode;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            private bool _disposed;

            ~Grouping()
            {
                if (!_disposed)
                {
                    // For debugging. This should never happen.
                    ReportRejection(new UnreleasedObjectException("A Grouping was garbage collected without it being disposed."), null);
                }
            }
#endif

            private Grouping() { }

            [MethodImpl(InlineOption)]
            private static Grouping<TKey, TElement> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<Grouping<TKey, TElement>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new Grouping<TKey, TElement>()
                    : obj.UnsafeAs<Grouping<TKey, TElement>>();
            }

            internal static Grouping<TKey, TElement> GetOrCreate(TKey key, int hashCode, Grouping<TKey, TElement> hashNext, bool willBeDisposed)
            {
                var grouping = GetOrCreate();
                grouping._key = key;
                grouping._hashCode = hashCode;
                grouping._elements = new TempCollectionBuilder<TElement>(1);
                grouping._hashNext = hashNext;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                // ToLookupAsync does not dispose. GroupByAsync does.
                grouping._disposed = !willBeDisposed;
                if (!willBeDisposed)
                {
                    Discard(grouping._elements._disposedChecker);
                    Discard(grouping);
                }
#endif
                return grouping;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IEnumerator<TElement> GetEnumerator() => _elements.View.GetEnumerator();

            public TKey Key => _key;

            int ICollection<TElement>.Count => _elements._count;

            bool ICollection<TElement>.IsReadOnly => true;

            void ICollection<TElement>.Add(TElement item) => throw new NotSupportedException();

            void ICollection<TElement>.Clear() => throw new NotSupportedException();

            bool ICollection<TElement>.Contains(TElement item) => Array.IndexOf(_elements._items, item, 0, _elements._count) >= 0;

            void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex) => _elements.View.CopyTo(array, arrayIndex);

            bool ICollection<TElement>.Remove(TElement item) => throw new NotSupportedException();

            int IList<TElement>.IndexOf(TElement item) => Array.IndexOf(_elements._items, item, 0, _elements._count);

            void IList<TElement>.Insert(int index, TElement item) => throw new NotSupportedException();

            void IList<TElement>.RemoveAt(int index) => throw new NotSupportedException();

            TElement IList<TElement>.this[int index]
            {
                get => _elements.View[index];
                set => throw new NotSupportedException();
            }

            [MethodImpl(InlineOption)]
            internal void Add(TElement element) => _elements.Add(element);

            public void Dispose()
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _disposed = true;
#endif
                _hashNext = null;
                _nextGrouping = null;
                _elements.Dispose();
                _elements = default;
                _key = default;
                ObjectPool.MaybeRepool(this);
            }
        }
    } // class Internal
#endif
} // namespace Proto.Promises