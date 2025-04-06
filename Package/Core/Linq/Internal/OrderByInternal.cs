#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
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
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class OrderHelper<TSource>
        {
            internal static OrderedAsyncEnumerable<TSource> Order<TComparer>(AsyncEnumerator<TSource> source, TComparer comparer)
                where TComparer : IComparer<TSource>
            {
                var enumerable = OrderedAsyncEnumerableHead<TSource>.SourceComparer<TComparer>.GetOrCreate(source, comparer);
                // _next points to the head so the enumerator will know where to start.
                enumerable._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal static OrderedAsyncEnumerable<TSource> Order<TComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TComparer comparer)
                where TComparer : IComparer<TSource>
            {
                var enumerable = OrderedAsyncEnumerableHead<TSource>.ConfiguredSourceComparer<TComparer>.GetOrCreate(configuredAsyncEnumerator, comparer);
                // _next points to the head so the enumerator will know where to start.
                enumerable._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class OrderHelper<TSource, TKey>
        {
            internal static OrderedAsyncEnumerable<TSource> OrderBy<TKeySelector, TComparer>(AsyncEnumerator<TSource> source, TKeySelector keySelector, TComparer comparer)
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                var enumerable = OrderedAsyncEnumerableHead<TSource>.ByComparer<TKey, TKeySelector, TComparer>.GetOrCreate(source, keySelector, comparer);
                // _next points to the head so the enumerator will know where to start.
                enumerable._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal static OrderedAsyncEnumerable<TSource> OrderBy<TKeySelector, TComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TComparer comparer)
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                var enumerable = OrderedAsyncEnumerableHead<TSource>.ConfiguredByComparer<TKey, TKeySelector, TComparer>.GetOrCreate(configuredAsyncEnumerator, keySelector, comparer);
                // _next points to the head so the enumerator will know where to start.
                enumerable._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal static OrderedAsyncEnumerable<TSource> ThenBy<TKeySelector, TComparer>(OrderedAsyncEnumerable<TSource> source, TKeySelector keySelector, TComparer comparer)
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                source._target.IncrementId(source._id);

                var enumerable = OrderedAsyncEnumerableThenBy<TSource>.ByComparer<TKey, TKeySelector, TComparer>.GetOrCreate(source._target._next, keySelector, comparer);
                // The new enumerable is used for the OrderedAsyncEnumerable, and source is invalidated, so we point _next to it for resolving ThenBy(Descending) comparisons.
                source._target._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Just implementing the Internal.IAsyncEnumerable<T> interface instead of the full AsyncEnumerableBase<T> class,
        // because we can build up OrderBy(...).ThenBy(...).ThenByDescending(...) etc chains with arbitrary depth,
        // so we only create the iterator class when the OrderedAsyncEnumerable is actually iterated.
        internal abstract class OrderedAsyncEnumerableBase<TSource> : HandleablePromiseBase, IAsyncEnumerable<TSource>
        {
            internal int _id = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.

            public bool GetCanBeEnumerated(int id) => id == _id;

            internal void IncrementId(int id)
            {
                if (Interlocked.CompareExchange(ref _id, id + 1, id) != id)
                {
                    ThrowInvalidAsyncEnumerable(3);
                }
            }

            public AsyncEnumerable<TSource> GetSelfWithIncrementedId(int id)
            {
                int newId = id + 1;
                if (Interlocked.CompareExchange(ref id, newId, id) != id)
                {
                    ThrowInvalidAsyncEnumerable(2);
                }
                return new AsyncEnumerable<TSource>(this, newId);
            }

            public AsyncEnumerator<TSource> GetAsyncEnumerator(int id, CancelationToken cancelationToken)
            {
                IncrementId(id);

                // The head is stored in _next.
                var head = _next.UnsafeAs<OrderedAsyncEnumerableHead<TSource>>();
                _next = null;
                // We use `IAsyncIterator` instead of the specific type to reduce the number of generated generic types.
                var enumerable = AsyncEnumerableCreate<TSource, IAsyncIterator<TSource>>.GetOrCreate(head);
                return new AsyncEnumerable<TSource>(enumerable).GetAsyncEnumerator(cancelationToken);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class OrderedAsyncEnumerableHead<TSource> : OrderedAsyncEnumerableBase<TSource>, IAsyncIterator<TSource>
        {
            public abstract AsyncIteratorMethod Start(AsyncStreamWriter<TSource> streamWriter, CancelationToken cancelationToken);
            public abstract Promise DisposeAsyncWithoutStart();

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            protected readonly struct IndexComparer<TElement, TComparer> : IComparer<int>
                where TComparer : IComparer<TElement>
            {
                private readonly OrderedAsyncEnumerableHead<TSource> _head;
                private readonly TElement[] _elements;
                private readonly TComparer _comparer;

                internal IndexComparer(OrderedAsyncEnumerableHead<TSource> head, TElement[] elements, TComparer comparer)
                {
                    _head = head;
                    _elements = elements;
                    _comparer = comparer;
                }

                [MethodImpl(InlineOption)]
                public int Compare(int index1, int index2)
                {
                    int result = _comparer.Compare(_elements[index1], _elements[index2]);
                    if (result != 0)
                    {
                        return result;
                    }
                    // We iterate over all the nexts instead of recursively, to avoid StackOverflowException in the event of a very long chain.
                    for (var next = _head._next; next != null; next = next._next)
                    {
                        result = next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().Compare(index1, index2);
                        if (result != 0)
                        {
                            return result;
                        }
                    }
                    // Make sure order is stable.
                    return index1 - index2;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract class Comparer<TComparer> : OrderedAsyncEnumerableHead<TSource>
                where TComparer : IComparer<TSource>
            {
                protected TComparer _comparer;

                protected void Dispose()
                {
                    ClearReferences(ref _comparer);
                    // Dispose ThenBys
                    var next = _next;
                    _next = null;
                    while (next != null)
                    {
                        var temp = next;
                        next = next._next;
                        temp.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().Dispose();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class SourceComparer<TComparer> : Comparer<TComparer>
                where TComparer : IComparer<TSource>
            {
                private AsyncEnumerator<TSource> _source;

                private SourceComparer() { }

                [MethodImpl(InlineOption)]
                private static SourceComparer<TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<SourceComparer<TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new SourceComparer<TComparer>()
                        : obj.UnsafeAs<SourceComparer<TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static SourceComparer<TComparer> GetOrCreate(AsyncEnumerator<TSource> source, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    // This is the head, _next points to the head.
                    instance._next = instance;
                    instance._source = source;
                    instance._comparer = comparer;
                    return instance;
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _source = default;
                    ObjectPool.MaybeRepool(this);
                }

                public override Promise DisposeAsyncWithoutStart()
                {
                    var source = _source;
                    Dispose();
                    return source.DisposeAsync();
                }

                public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _source._target._cancelationToken = cancelationToken;
                    try
                    {
                        if (!await _source.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var elements = new TempCollectionBuilder<TSource>(1))
                        {
                            do
                            {
                                elements.Add(_source.Current);
                            } while (await _source.MoveNextAsync());

                            for (var next = _next; next != null; next = next._next)
                            {
                                await next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().ComputeKeys(elements, cancelationToken);
                            }

                            using (var indices = new TempCollectionBuilder<int>(elements._count, elements._count))
                            {
                                for (int i = 0; i < elements._count; ++i)
                                {
                                    indices._items[i] = i;
                                }
                                indices.Span.Sort(new IndexComparer<TSource, TComparer>(this, elements._items, _comparer));

                                // Dispose all the keys before yielding back the ordered results.
                                base.Dispose();

                                for (int i = 0; i < indices._count; ++i)
                                {
                                    int index = indices._items[i];
                                    await writer.YieldAsync(elements._items[index]);
                                }
                            }
                        }

                        // We wait for this enumerator to be disposed in case the source contains temp collections.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        var source = _source;
                        Dispose();
                        await source.DisposeAsync();
                    }
                }
            } // class SourceComparer<TComparer>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class ConfiguredSourceComparer<TComparer> : Comparer<TComparer>
                where TComparer : IComparer<TSource>
            {
                private ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;

                private ConfiguredSourceComparer() { }

                [MethodImpl(InlineOption)]
                private static ConfiguredSourceComparer<TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ConfiguredSourceComparer<TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new ConfiguredSourceComparer<TComparer>()
                        : obj.UnsafeAs<ConfiguredSourceComparer<TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static ConfiguredSourceComparer<TComparer> GetOrCreate(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    // This is the head, _next points to the head.
                    instance._next = instance;
                    instance._configuredAsyncEnumerator = configuredAsyncEnumerator;
                    instance._comparer = comparer;
                    return instance;
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _configuredAsyncEnumerator = default;
                    ObjectPool.MaybeRepool(this);
                }

                public override Promise DisposeAsyncWithoutStart()
                {
                    var source = _configuredAsyncEnumerator;
                    Dispose();
                    return source.DisposeAsync();
                }

                public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var elements = new TempCollectionBuilder<TSource>(1))
                        {
                            do
                            {
                                elements.Add(_configuredAsyncEnumerator.Current);
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());

                            
                            for (var next = _next; next != null; next = next._next)
                            {
                                await next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().ComputeKeys(elements, _configuredAsyncEnumerator.ContinuationOptions, cancelationToken);
                            }

                            using (var indices = new TempCollectionBuilder<int>(elements._count, elements._count))
                            {
                                for (int i = 0; i < elements._count; ++i)
                                {
                                    indices._items[i] = i;
                                }
                                indices.Span.Sort(new IndexComparer<TSource, TComparer>(this, elements._items, _comparer));

                                // Dispose all the keys before yielding back the ordered results.
                                base.Dispose();

                                for (int i = 0; i < indices._count; ++i)
                                {
                                    int index = indices._items[i];
                                    await writer.YieldAsync(elements._items[index]);
                                }
                            }
                        }

                        // We wait for this enumerator to be disposed in case the source contains temp collections.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        var source = _configuredAsyncEnumerator;
                        Dispose();
                        await source.DisposeAsync();
                    }
                }
            } // class ConfiguredSourceComparer<TComparer>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract class Comparer<TKey, TComparer> : OrderedAsyncEnumerableHead<TSource>
                where TComparer : IComparer<TKey>
            {
                protected TComparer _comparer;

                protected void Dispose()
                {
                    ClearReferences(ref _comparer);
                    // Dispose ThenBys
                    var next = _next;
                    _next = null;
                    while (next != null)
                    {
                        var temp = next;
                        next = next._next;
                        temp.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().Dispose();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class ByComparer<TKey, TKeySelector, TComparer> : Comparer<TKey, TComparer>
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                private AsyncEnumerator<TSource> _source;
                private TKeySelector _keySelector;

                private ByComparer() { }

                [MethodImpl(InlineOption)]
                private static ByComparer<TKey, TKeySelector, TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ByComparer<TKey, TKeySelector, TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new ByComparer<TKey, TKeySelector, TComparer>()
                        : obj.UnsafeAs<ByComparer<TKey, TKeySelector, TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static ByComparer<TKey, TKeySelector, TComparer> GetOrCreate(AsyncEnumerator<TSource> source, TKeySelector keySelector, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    // This is the head, _next points to the head.
                    instance._next = instance;
                    instance._source = source;
                    instance._keySelector = keySelector;
                    instance._comparer = comparer;
                    return instance;
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _source = default;
                    _keySelector = default;
                    ObjectPool.MaybeRepool(this);
                }

                public override Promise DisposeAsyncWithoutStart()
                {
                    var source = _source;
                    Dispose();
                    return source.DisposeAsync();
                }

                public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _source._target._cancelationToken = cancelationToken;
                    try
                    {
                        if (!await _source.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var elements = new TempCollectionBuilder<TSource>(1))
                        {
                            do
                            {
                                elements.Add(_source.Current);
                            } while (await _source.MoveNextAsync());

                            using (var indices = new TempCollectionBuilder<int>(elements._count, elements._count))
                            {
                                for (int i = 0; i < elements._count; ++i)
                                {
                                    indices._items[i] = i;
                                }
                                using (var keys = new TempCollectionBuilder<TKey>(elements._count, elements._count))
                                {
                                    for (int i = 0; i < keys._count; ++i)
                                    {
                                        keys._items[i] = await _keySelector.Invoke(elements._items[i], cancelationToken);
                                    }
                                    for (var next = _next; next != null; next = next._next)
                                    {
                                        await next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().ComputeKeys(elements, cancelationToken);
                                    }
                                    indices.Span.Sort(new IndexComparer<TKey, TComparer>(this, keys._items, _comparer));
                                }

                                // Dispose all the keys before yielding back the ordered results.
                                base.Dispose();

                                for (int i = 0; i < indices._count; ++i)
                                {
                                    int index = indices._items[i];
                                    await writer.YieldAsync(elements._items[index]);
                                }
                            }
                        }

                        // We wait for this enumerator to be disposed in case the source contains temp collections.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        var source = _source;
                        Dispose();
                        await source.DisposeAsync();
                    }
                }
            } // class ByComparer<TKey, TKeySelector, TComparer>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class ConfiguredByComparer<TKey, TKeySelector, TComparer> : Comparer<TKey, TComparer>
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                private ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredAsyncEnumerator;
                private TKeySelector _keySelector;

                private ConfiguredByComparer() { }

                [MethodImpl(InlineOption)]
                private static ConfiguredByComparer<TKey, TKeySelector, TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ConfiguredByComparer<TKey, TKeySelector, TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new ConfiguredByComparer<TKey, TKeySelector, TComparer>()
                        : obj.UnsafeAs<ConfiguredByComparer<TKey, TKeySelector, TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static ConfiguredByComparer<TKey, TKeySelector, TComparer> GetOrCreate(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredAsyncEnumerator, TKeySelector keySelector, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    // This is the head, _next points to the head.
                    instance._next = instance;
                    instance._configuredAsyncEnumerator = configuredAsyncEnumerator;
                    instance._keySelector = keySelector;
                    instance._comparer = comparer;
                    return instance;
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _configuredAsyncEnumerator = default;
                    _keySelector = default;
                    ObjectPool.MaybeRepool(this);
                }

                public override Promise DisposeAsyncWithoutStart()
                {
                    var source = _configuredAsyncEnumerator;
                    Dispose();
                    return source.DisposeAsync();
                }

                public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var maybeJoinedCancelationSource = MaybeJoinCancelationTokens(ref cancelationToken, ref _configuredAsyncEnumerator._enumerator._target._cancelationToken);

                    try
                    {
                        if (!await _configuredAsyncEnumerator.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var elements = new TempCollectionBuilder<TSource>(1))
                        {
                            do
                            {
                                elements.Add(_configuredAsyncEnumerator.Current);
                            } while (await _configuredAsyncEnumerator.MoveNextAsync());

                            using (var indices = new TempCollectionBuilder<int>(elements._count, elements._count))
                            {
                                for (int i = 0; i < elements._count; ++i)
                                {
                                    indices._items[i] = i;
                                }
                                using (var keys = new TempCollectionBuilder<TKey>(elements._count, elements._count))
                                {
                                    for (int i = 0; i < keys._count; ++i)
                                    {
                                        keys._items[i] = await _keySelector.Invoke(elements._items[i], cancelationToken).ConfigureAwait(_configuredAsyncEnumerator.ContinuationOptions);
                                    }
                                    for (var next = _next; next != null; next = next._next)
                                    {
                                        await next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().ComputeKeys(elements, _configuredAsyncEnumerator.ContinuationOptions, cancelationToken);
                                    }
                                    indices.Span.Sort(new IndexComparer<TKey, TComparer>(this, keys._items, _comparer));
                                }

                                // Dispose all the keys before yielding back the ordered results.
                                base.Dispose();

                                for (int i = 0; i < indices._count; ++i)
                                {
                                    int index = indices._items[i];
                                    await writer.YieldAsync(elements._items[index]);
                                }
                            }
                        }

                        // We wait for this enumerator to be disposed in case the source contains temp collections.
                        await writer.YieldAsync(default).ForLinqExtension();
                    }
                    finally
                    {
                        maybeJoinedCancelationSource.Dispose();
                        var source = _configuredAsyncEnumerator;
                        Dispose();
                        await source.DisposeAsync();
                    }
                }
            } // class ConfiguredByComparer<TKey, TKeySelector, TComparer>
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class OrderedAsyncEnumerableThenBy<TSource> : OrderedAsyncEnumerableBase<TSource>
        {
            internal abstract Promise ComputeKeys(TempCollectionBuilder<TSource> elements, CancelationToken cancelationToken);
            internal abstract Promise ComputeKeys(TempCollectionBuilder<TSource> elements, ContinuationOptions continuationOptions, CancelationToken cancelationToken);
            internal abstract int Compare(int index1, int index2);
            internal abstract void Dispose();

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract class Comparer<TKey, TComparer> : OrderedAsyncEnumerableThenBy<TSource>
                where TComparer : IComparer<TKey>
            {
                protected TempCollectionBuilder<TKey> _keys;
                protected TComparer _comparer;

                // The head ensures stability, we don't do it here.
                internal override sealed int Compare(int index1, int index2)
                    => _comparer.Compare(_keys._items[index1], _keys._items[index2]);

                internal override void Dispose()
                {
                    if (_keys._items != null)
                    {
                        _keys.Dispose();
                    }
                    ClearReferences(ref _comparer);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class ByComparer<TKey, TKeySelector, TComparer> : Comparer<TKey, TComparer>
                where TKeySelector : IFunc<TSource, CancelationToken, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                private TKeySelector _keySelector;

                private ByComparer() { }

                [MethodImpl(InlineOption)]
                private static ByComparer<TKey, TKeySelector, TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ByComparer<TKey, TKeySelector, TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new ByComparer<TKey, TKeySelector, TComparer>()
                        : obj.UnsafeAs<ByComparer<TKey, TKeySelector, TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static ByComparer<TKey, TKeySelector, TComparer> GetOrCreate(HandleablePromiseBase head, TKeySelector keySelector, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    instance._next = head;
                    instance._keySelector = keySelector;
                    instance._comparer = comparer;
                    return instance;
                }

                internal override async Promise ComputeKeys(TempCollectionBuilder<TSource> elements, CancelationToken cancelationToken)
                {
                    _keys = new TempCollectionBuilder<TKey>(elements._count, elements._count);
                    for (int i = 0; i < _keys._count; ++i)
                    {
                        _keys._items[i] = await _keySelector.Invoke(elements._items[i], cancelationToken);
                    }
                }

                internal override async Promise ComputeKeys(TempCollectionBuilder<TSource> elements, ContinuationOptions continuationOptions, CancelationToken cancelationToken)
                {
                    _keys = new TempCollectionBuilder<TKey>(elements._count, elements._count);
                    for (int i = 0; i < _keys._count; ++i)
                    {
                        _keys._items[i] = await _keySelector.Invoke(elements._items[i], cancelationToken).ConfigureAwait(continuationOptions);
                    }
                }

                internal override void Dispose()
                {
                    base.Dispose();
                    ObjectPool.MaybeRepool(this);
                }
            } // ByComparer<TKey, TKeySelector, TComparer>
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // We use a reverse comparer and generics so that we don't need an extra branch on every comparison.
        internal readonly struct ReverseComparer<TKey, TComparer> : IComparer<TKey>
            where TComparer : IComparer<TKey>
        {
            private readonly TComparer _comparer;

            internal ReverseComparer(TComparer comparer)
                => _comparer = comparer;

            [MethodImpl(InlineOption)]
            public int Compare(TKey x, TKey y)
                => _comparer.Compare(y, x);
        }
    } // class Internal
} // namespace Proto.Promises