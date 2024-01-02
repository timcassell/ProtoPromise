#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using Proto.Promises.Collections;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0063 // Use simple 'using' statement

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Just implementing the Internal.IAsyncEnumerable<T> interface instead of the full AsyncEnumerableBase<T> class,
        // because we can build up OrderBy(...).ThenBy(...).ThenByDescending(...) etc chains with arbitrary depth,
        // so we only create the iterator class when the OrderedAsyncEnumerable is actually iterated.
        internal abstract class OrderedAsyncEnumerableBase<TSource> : HandleablePromiseBase, IAsyncEnumerable<TSource>
        {
            internal int _id = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.

            public bool GetIsValid(int id) => id == _id;

            private void IncrementId(int id)
            {
                if (Interlocked.CompareExchange(ref _id, id + 1, id) != id)
                {
                    throw new InvalidOperationException("OrderedAsyncEnumerable instance is not valid. OrderedAsyncEnumerable may only be used once.", GetFormattedStacktrace(3));
                }
            }
            
            public AsyncEnumerator<TSource> GetAsyncEnumerator(int id, CancelationToken cancelationToken)
            {
                IncrementId(id);

                // The head is stored in _next.
                var head = _next.UnsafeAs<OrderedAsyncEnumerableHead<TSource>>();
                _next = null;
                return head.GetAsyncEnumerator(cancelationToken);
            }

            internal abstract Promise ComputeKeys(TempCollectionBuilder<TSource> elements);
            internal abstract int Compare(int index1, int index2);
            internal abstract void Dispose();

            internal OrderedAsyncEnumerable<TSource> GetOrCreateComposite<TKey, TKeySelector, TComparer>(int id, TKeySelector keySelector, TComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TComparer : IComparer<TKey>
            {
                IncrementId(id);

                var enumerable = OrderedAsyncEnumerableThen<TSource, TKey, TComparer>.SyncComparer<TKeySelector>.GetOrCreate(_next, keySelector, comparer);
                // The new enumerable is used for the OrderedAsyncEnumerable, and this one is invalidated, so we point _next to it for resolving ThenBy(Descending) comparisons.
                _next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            // TODO

            //internal OrderedAsyncEnumerable<TSource> GetOrCreateCompositeAwait<TKey, TKeySelector>(int id, TKeySelector keySelector, IComparer<TKey> comparer, bool descending)
            //    where TKeySelector : IFunc<TSource, Promise<TKey>>
            //{
            //    IncrementId(id);

            //    if (comparer == null)
            //    {
            //        comparer = Comparer<TKey>.Default;
            //    }
            //}
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class OrderedAsyncEnumerableHead<TSource> : OrderedAsyncEnumerableBase<TSource>
        {
            public abstract AsyncEnumerator<TSource> GetAsyncEnumerator(CancelationToken cancelationToken);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class OrderedAsyncEnumerableHead<TSource, TKey> : OrderedAsyncEnumerableHead<TSource>
        {
            internal static OrderedAsyncEnumerable<TSource> GetOrCreate<TKeySelector, TComparer>(AsyncEnumerator<TSource> source, TKeySelector keySelector, TComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TComparer : IComparer<TKey>
            {
                var enumerable = SyncComparer<TKeySelector, TComparer>.GetOrCreate(source, keySelector, comparer);
                // _next points to the head so the enumerator will know where to start.
                enumerable._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private abstract class Comparer<TComparer> : OrderedAsyncEnumerableHead<TSource>
                where TComparer : IComparer<TKey>
            {
                protected TempCollectionBuilder<TKey> _tempKeys;
                protected TComparer _comparer;

                internal override sealed int Compare(int index1, int index2)
                {
                    int result = _comparer.Compare(_tempKeys._items[index1], _tempKeys._items[index2]);
                    if (result != 0)
                    {
                        return result;
                    }
                    // We iterate over all the nexts instead of recursively, to avoid StackOverflowException in the event of a very long chain.
                    for (var next = _next; next != null; next = next._next)
                    {
                        result = _next.UnsafeAs<OrderedAsyncEnumerableBase<TSource>>().Compare(index1, index2);
                        if (result != 0)
                        {
                            return result;
                        }
                    }
                    // Make sure order is stable.
                    return index1 - index2;
                }

                internal override void Dispose()
                {
                    _tempKeys.Dispose();
                    _comparer = default;
                }

                protected void DisposeThens()
                {
                    var next = _next;
                    while (next != null)
                    {
                        var temp = next;
                        next = next._next;
                        temp.UnsafeAs<OrderedAsyncEnumerableBase<TSource>>().Dispose();
                    }
                }

                protected readonly struct IndexComparer : IComparer<int>
                {
                    private readonly Comparer<TComparer> _target;

                    internal IndexComparer(Comparer<TComparer> target)
                        => _target = target;

                    [MethodImpl(InlineOption)]
                    public int Compare(int x, int y)
                        => _target.Compare(x, y);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class SyncComparer<TKeySelector, TComparer> : Comparer<TComparer>, IAsyncIterator<TSource>
                where TKeySelector : IFunc<TSource, TKey>
                where TComparer : IComparer<TKey>
            {
                private AsyncEnumerator<TSource> _source;
                private TKeySelector _keySelector;

                private SyncComparer() { }

                [MethodImpl(InlineOption)]
                private static SyncComparer<TKeySelector, TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<SyncComparer<TKeySelector, TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new SyncComparer<TKeySelector, TComparer>()
                        : obj.UnsafeAs<SyncComparer<TKeySelector, TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static SyncComparer<TKeySelector, TComparer> GetOrCreate(AsyncEnumerator<TSource> source, TKeySelector keySelector, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    // This is the head, _next points to the head.
                    instance._next = instance;
                    instance._source = source;
                    instance._keySelector = keySelector;
                    instance._comparer = comparer;
                    return instance;
                }

                private void ComputeKeysSync(TempCollectionBuilder<TSource> elements)
                {
                    var elementsSpan = elements.ReadOnlySpan;
                    _tempKeys = new TempCollectionBuilder<TKey>(elementsSpan.Length, elementsSpan.Length);
                    var tempSpan = _tempKeys.Span;
                    for (int i = 0; i < elementsSpan.Length; ++i)
                    {
                        tempSpan[i] = _keySelector.Invoke(elementsSpan[i]);
                    }
                }

                internal override Promise ComputeKeys(TempCollectionBuilder<TSource> elements)
                {
                    ComputeKeysSync(elements);
                    return Promise.Resolved();
                }

                internal override void Dispose()
                {
                    base.Dispose();
                    _keySelector = default;
                    ObjectPool.MaybeRepool(this);
                }

                public override AsyncEnumerator<TSource> GetAsyncEnumerator(CancelationToken cancelationToken)
                {
                    var enumerable = AsyncEnumerableCreate<TSource, SyncComparer<TKeySelector, TComparer>>.GetOrCreate(this);
                    return new AsyncEnumerable<TSource>(enumerable).GetAsyncEnumerator(cancelationToken);
                }

                public async AsyncEnumerableMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    var source = _source;
                    _source = default;
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    source._target._cancelationToken = cancelationToken;
                    try
                    {
                        if (!await source.MoveNextAsync())
                        {
                            // Empty source.
                            Dispose();
                            DisposeThens();
                            return;
                        }

                        using (var elements = new TempCollectionBuilder<TSource>(1))
                        {
                            do
                            {
                                elements.Add(source.Current);
                            } while (await source.MoveNextAsync());

                            ComputeKeysSync(elements);
                            var next = _next;
                            while (next != null)
                            {
                                await next.UnsafeAs<OrderedAsyncEnumerableBase<TSource>>().ComputeKeys(elements);
                                next = next._next;
                            }

                            using (var indices = new TempCollectionBuilder<int>(elements._count, elements._count))
                            {
                                for (int i = 0; i < elements._count; ++i)
                                {
                                    indices._items[i] = i;
                                }
                                indices.Span.Sort(new IndexComparer(this));

                                Dispose();
                                DisposeThens();

                                for (int i = 0; i < indices._count; ++i)
                                {
                                    int index = indices._items[i];
                                    await writer.YieldAsync(elements._items[index]);
                                }
                            }
                        }

                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        await source.DisposeAsync();
                    }
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class OrderedAsyncEnumerableThen<TSource, TKey, TComparer> : OrderedAsyncEnumerableBase<TSource>
            where TComparer : IComparer<TKey>
        {
            private TempCollectionBuilder<TKey> _tempKeys;
            private TComparer _comparer;

            // The head ensures stability, we don't do it here.
            internal override int Compare(int index1, int index2)
                => _comparer.Compare(_tempKeys._items[index1], _tempKeys._items[index2]);

            internal override void Dispose()
            {
                _tempKeys.Dispose();
                _comparer = default;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class SyncComparer<TKeySelector> : OrderedAsyncEnumerableThen<TSource, TKey, TComparer>
                where TKeySelector : IFunc<TSource, TKey>
            {
                private TKeySelector _keySelector;

                private SyncComparer() { }

                [MethodImpl(InlineOption)]
                private static SyncComparer<TKeySelector> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<SyncComparer<TKeySelector>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new SyncComparer<TKeySelector>()
                        : obj.UnsafeAs<SyncComparer<TKeySelector>>();
                }

                [MethodImpl(InlineOption)]
                internal static SyncComparer<TKeySelector> GetOrCreate(HandleablePromiseBase head, TKeySelector keySelector, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    instance._next = head;
                    instance._keySelector = keySelector;
                    instance._comparer = comparer;
                    return instance;
                }

                internal override Promise ComputeKeys(TempCollectionBuilder<TSource> elements)
                {
                    var elementsSpan = elements.ReadOnlySpan;
                    _tempKeys = new TempCollectionBuilder<TKey>(elementsSpan.Length, elementsSpan.Length);
                    var tempSpan = _tempKeys.Span;
                    for (int i = 0; i < elementsSpan.Length; ++i)
                    {
                        tempSpan[i] = _keySelector.Invoke(elementsSpan[i]);
                    }
                    return Promise.Resolved();
                }

                internal override void Dispose()
                {
                    base.Dispose();
                    ObjectPool.MaybeRepool(this);
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // We use a reverse comparer and generics so that we don't need an extra branch on every comparison.
        internal readonly struct ReverseComparer<TKey> : IComparer<TKey>
        {
            private readonly IComparer<TKey> _comparer;

            internal ReverseComparer(IComparer<TKey> comparer)
                => _comparer = comparer;

            [MethodImpl(InlineOption)]
            public int Compare(TKey x, TKey y)
                => _comparer.Compare(y, x);
        }
    } // class Internal
#endif
} // namespace Proto.Promises