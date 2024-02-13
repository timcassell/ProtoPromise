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

            internal static OrderedAsyncEnumerable<TSource> Order<TComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TComparer comparer)
                where TComparer : IComparer<TSource>
            {
                var enumerable = OrderedAsyncEnumerableHead<TSource>.ConfiguredSourceComparer<TComparer>.GetOrCreate(configuredSource, comparer);
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
                where TKeySelector : IFunc<TSource, TKey>
                where TComparer : IComparer<TKey>
            {
                var enumerable = OrderedAsyncEnumerableHead<TSource>.SyncComparer<TKey, TKeySelector, TComparer>.GetOrCreate(source, keySelector, comparer);
                // _next points to the head so the enumerator will know where to start.
                enumerable._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal static OrderedAsyncEnumerable<TSource> OrderBy<TKeySelector, TComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TKeySelector keySelector, TComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TComparer : IComparer<TKey>
            {
                var enumerable = OrderedAsyncEnumerableHead<TSource>.ConfiguredSyncComparer<TKey, TKeySelector, TComparer>.GetOrCreate(configuredSource, keySelector, comparer);
                // _next points to the head so the enumerator will know where to start.
                enumerable._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal static OrderedAsyncEnumerable<TSource> OrderByAwait<TKeySelector, TComparer>(AsyncEnumerator<TSource> source, TKeySelector keySelector, TComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                var enumerable = OrderedAsyncEnumerableHead<TSource>.AsyncComparer<TKey, TKeySelector, TComparer>.GetOrCreate(source, keySelector, comparer);
                // _next points to the head so the enumerator will know where to start.
                enumerable._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal static OrderedAsyncEnumerable<TSource> OrderByAwait<TKeySelector, TComparer>(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TKeySelector keySelector, TComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                var enumerable = OrderedAsyncEnumerableHead<TSource>.ConfiguredAsyncComparer<TKey, TKeySelector, TComparer>.GetOrCreate(configuredSource, keySelector, comparer);
                // _next points to the head so the enumerator will know where to start.
                enumerable._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal static OrderedAsyncEnumerable<TSource> ThenBy<TKeySelector, TComparer>(OrderedAsyncEnumerable<TSource> source, TKeySelector keySelector, TComparer comparer)
                where TKeySelector : IFunc<TSource, TKey>
                where TComparer : IComparer<TKey>
            {
                source._target.IncrementId(source._id);

                var enumerable = OrderedAsyncEnumerableThenBy<TSource>.SyncComparer<TKey, TKeySelector, TComparer>.GetOrCreate(source._target._next, keySelector, comparer);
                // The new enumerable is used for the OrderedAsyncEnumerable, and source is invalidated, so we point _next to it for resolving ThenBy(Descending) comparisons.
                source._target._next = enumerable;
                return new OrderedAsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal static OrderedAsyncEnumerable<TSource> ThenByAwait<TKeySelector, TComparer>(OrderedAsyncEnumerable<TSource> source, TKeySelector keySelector, TComparer comparer)
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                source._target.IncrementId(source._id);

                var enumerable = OrderedAsyncEnumerableThenBy<TSource>.AsyncComparer<TKey, TKeySelector, TComparer>.GetOrCreate(source._target._next, keySelector, comparer);
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
                    _comparer = default;
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

                            var next = _next;
                            while (next != null)
                            {
                                await next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().ComputeKeys(elements);
                                next = next._next;
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
                private ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;

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
                internal static ConfiguredSourceComparer<TComparer> GetOrCreate(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    // This is the head, _next points to the head.
                    instance._next = instance;
                    instance._configuredSource = configuredSource;
                    instance._comparer = comparer;
                    return instance;
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _configuredSource = default;
                    ObjectPool.MaybeRepool(this);
                }

                public override Promise DisposeAsyncWithoutStart()
                {
                    var source = _configuredSource;
                    Dispose();
                    return source.DisposeAsync();
                }

                public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredSource._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        if (!await _configuredSource.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var elements = new TempCollectionBuilder<TSource>(1))
                        {
                            do
                            {
                                elements.Add(_configuredSource.Current);
                            } while (await _configuredSource.MoveNextAsync());

                            var next = _next;
                            if (next != null)
                            {
                                var switchToConfiguredContextAwaiter = _configuredSource.SwitchToContextReusable();
                                do
                                {
                                    await next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().ComputeKeys(elements, switchToConfiguredContextAwaiter);
                                    next = next._next;
                                } while (next != null);
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
                        joinedCancelationSource.TryDispose();
                        var source = _configuredSource;
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
                    _comparer = default;
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
            internal sealed class SyncComparer<TKey, TKeySelector, TComparer> : Comparer<TKey, TComparer>
                where TKeySelector : IFunc<TSource, TKey>
                where TComparer : IComparer<TKey>
            {
                private AsyncEnumerator<TSource> _source;
                private TKeySelector _keySelector;

                private SyncComparer() { }

                [MethodImpl(InlineOption)]
                private static SyncComparer<TKey, TKeySelector, TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<SyncComparer<TKey, TKeySelector, TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new SyncComparer<TKey, TKeySelector, TComparer>()
                        : obj.UnsafeAs<SyncComparer<TKey, TKeySelector, TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static SyncComparer<TKey, TKeySelector, TComparer> GetOrCreate(AsyncEnumerator<TSource> source, TKeySelector keySelector, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    // This is the head, _next points to the head.
                    instance._next = instance;
                    instance._source = source;
                    instance._keySelector = keySelector;
                    instance._comparer = comparer;
                    return instance;
                }

                private void ComputeKeys(ReadOnlySpan<TSource> elements, Span<TKey> keys)
                {
                    for (int i = 0; i < elements.Length; ++i)
                    {
                        keys[i] = _keySelector.Invoke(elements[i]);
                    }
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
                                    ComputeKeys(elements.ReadOnlySpan, keys.Span);
                                    var next = _next;
                                    while (next != null)
                                    {
                                        await next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().ComputeKeys(elements);
                                        next = next._next;
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
            } // class SyncComparer<TKey, TKeySelector, TComparer>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class ConfiguredSyncComparer<TKey, TKeySelector, TComparer> : Comparer<TKey, TComparer>
                where TKeySelector : IFunc<TSource, TKey>
                where TComparer : IComparer<TKey>
            {
                private ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;
                private TKeySelector _keySelector;

                private ConfiguredSyncComparer() { }

                [MethodImpl(InlineOption)]
                private static ConfiguredSyncComparer<TKey, TKeySelector, TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ConfiguredSyncComparer<TKey, TKeySelector, TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new ConfiguredSyncComparer<TKey, TKeySelector, TComparer>()
                        : obj.UnsafeAs<ConfiguredSyncComparer<TKey, TKeySelector, TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static ConfiguredSyncComparer<TKey, TKeySelector, TComparer> GetOrCreate(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TKeySelector keySelector, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    // This is the head, _next points to the head.
                    instance._next = instance;
                    instance._configuredSource = configuredSource;
                    instance._keySelector = keySelector;
                    instance._comparer = comparer;
                    return instance;
                }

                private void ComputeKeys(ReadOnlySpan<TSource> elements, TempCollectionBuilder<TKey> keys)
                {
                    var keysSpan = keys.Span;
                    for (int i = 0; i < elements.Length; ++i)
                    {
                        keysSpan[i] = _keySelector.Invoke(elements[i]);
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _configuredSource = default;
                    _keySelector = default;
                    ObjectPool.MaybeRepool(this);
                }

                public override Promise DisposeAsyncWithoutStart()
                {
                    var source = _configuredSource;
                    Dispose();
                    return source.DisposeAsync();
                }

                public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredSource._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        if (!await _configuredSource.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var elements = new TempCollectionBuilder<TSource>(1))
                        {
                            do
                            {
                                elements.Add(_configuredSource.Current);
                            } while (await _configuredSource.MoveNextAsync());

                            using (var indices = new TempCollectionBuilder<int>(elements._count, elements._count))
                            {
                                for (int i = 0; i < elements._count; ++i)
                                {
                                    indices._items[i] = i;
                                }
                                using (var keys = new TempCollectionBuilder<TKey>(elements._count, elements._count))
                                {
                                    ComputeKeys(elements.ReadOnlySpan, keys);
                                    var next = _next;
                                    if (next != null)
                                    {
                                        var switchToConfiguredContextAwaiter = _configuredSource.SwitchToContextReusable();
                                        do
                                        {
                                            await next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().ComputeKeys(elements, switchToConfiguredContextAwaiter);
                                            next = next._next;
                                        } while (next != null);
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
                        joinedCancelationSource.TryDispose();
                        var source = _configuredSource;
                        Dispose();
                        await source.DisposeAsync();
                    }
                }
            } // class ConfiguredSyncComparer<TKey, TKeySelector, TComparer>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class AsyncComparer<TKey, TKeySelector, TComparer> : Comparer<TKey, TComparer>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                private AsyncEnumerator<TSource> _source;
                private TKeySelector _keySelector;

                private AsyncComparer() { }

                [MethodImpl(InlineOption)]
                private static AsyncComparer<TKey, TKeySelector, TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AsyncComparer<TKey, TKeySelector, TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new AsyncComparer<TKey, TKeySelector, TComparer>()
                        : obj.UnsafeAs<AsyncComparer<TKey, TKeySelector, TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static AsyncComparer<TKey, TKeySelector, TComparer> GetOrCreate(AsyncEnumerator<TSource> source, TKeySelector keySelector, TComparer comparer)
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
                                        keys._items[i] = await _keySelector.Invoke(elements._items[i]);
                                    }
                                    var next = _next;
                                    while (next != null)
                                    {
                                        await next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().ComputeKeys(elements);
                                        next = next._next;
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
            } // class AsyncComparer<TKey, TKeySelector, TComparer>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class ConfiguredAsyncComparer<TKey, TKeySelector, TComparer> : Comparer<TKey, TComparer>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                private ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredSource;
                private TKeySelector _keySelector;

                private ConfiguredAsyncComparer() { }

                [MethodImpl(InlineOption)]
                private static ConfiguredAsyncComparer<TKey, TKeySelector, TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ConfiguredAsyncComparer<TKey, TKeySelector, TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new ConfiguredAsyncComparer<TKey, TKeySelector, TComparer>()
                        : obj.UnsafeAs<ConfiguredAsyncComparer<TKey, TKeySelector, TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static ConfiguredAsyncComparer<TKey, TKeySelector, TComparer> GetOrCreate(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredSource, TKeySelector keySelector, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    // This is the head, _next points to the head.
                    instance._next = instance;
                    instance._configuredSource = configuredSource;
                    instance._keySelector = keySelector;
                    instance._comparer = comparer;
                    return instance;
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _configuredSource = default;
                    _keySelector = default;
                    ObjectPool.MaybeRepool(this);
                }

                public override Promise DisposeAsyncWithoutStart()
                {
                    var source = _configuredSource;
                    Dispose();
                    return source.DisposeAsync();
                }

                public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                    var enumerableRef = _configuredSource._enumerator._target;
                    var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);

                    try
                    {
                        if (!await _configuredSource.MoveNextAsync())
                        {
                            // Empty source.
                            return;
                        }

                        using (var elements = new TempCollectionBuilder<TSource>(1))
                        {
                            do
                            {
                                elements.Add(_configuredSource.Current);
                            } while (await _configuredSource.MoveNextAsync());

                            using (var indices = new TempCollectionBuilder<int>(elements._count, elements._count))
                            {
                                for (int i = 0; i < elements._count; ++i)
                                {
                                    indices._items[i] = i;
                                }
                                using (var keys = new TempCollectionBuilder<TKey>(elements._count, elements._count))
                                {
                                    var switchToConfiguredContextAwaiter = _configuredSource.SwitchToContextReusable();
                                    for (int i = 0; i < keys._count; ++i)
                                    {
                                        await switchToConfiguredContextAwaiter;
                                        keys._items[i] = await _keySelector.Invoke(elements._items[i]);
                                    }
                                    var next = _next;
                                    while (next != null)
                                    {
                                        await next.UnsafeAs<OrderedAsyncEnumerableThenBy<TSource>>().ComputeKeys(elements, switchToConfiguredContextAwaiter);
                                        next = next._next;
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
                        joinedCancelationSource.TryDispose();
                        var source = _configuredSource;
                        Dispose();
                        await source.DisposeAsync();
                    }
                }
            } // class ConfiguredAsyncComparer<TKey, TKeySelector, TComparer>
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class OrderedAsyncEnumerableThenBy<TSource> : OrderedAsyncEnumerableBase<TSource>
        {
            internal abstract Promise ComputeKeys(TempCollectionBuilder<TSource> elements);
            internal abstract Promise ComputeKeys(TempCollectionBuilder<TSource> elements, SwitchToConfiguredContextReusableAwaiter switchToConfiguredContextAwaiter);
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
                    _comparer = default;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class SyncComparer<TKey, TKeySelector, TComparer> : Comparer<TKey, TComparer>
                where TKeySelector : IFunc<TSource, TKey>
                where TComparer : IComparer<TKey>
            {
                private TKeySelector _keySelector;

                private SyncComparer() { }

                [MethodImpl(InlineOption)]
                private static SyncComparer<TKey, TKeySelector, TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<SyncComparer<TKey, TKeySelector, TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new SyncComparer<TKey, TKeySelector, TComparer>()
                        : obj.UnsafeAs<SyncComparer<TKey, TKeySelector, TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static SyncComparer<TKey, TKeySelector, TComparer> GetOrCreate(HandleablePromiseBase head, TKeySelector keySelector, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    instance._next = head;
                    instance._keySelector = keySelector;
                    instance._comparer = comparer;
                    return instance;
                }

                private void ComputeKeysSync(TempCollectionBuilder<TSource> elements)
                {
                    var elementsSpan = elements.ReadOnlySpan;
                    _keys = new TempCollectionBuilder<TKey>(elementsSpan.Length, elementsSpan.Length);
                    var tempSpan = _keys.Span;
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

                internal override async Promise ComputeKeys(TempCollectionBuilder<TSource> elements, SwitchToConfiguredContextReusableAwaiter switchToConfiguredContextAwaiter)
                {
                    await switchToConfiguredContextAwaiter;
                    ComputeKeysSync(elements);
                }

                internal override void Dispose()
                {
                    base.Dispose();
                    ObjectPool.MaybeRepool(this);
                }
            } // SyncComparer<TKey, TKeySelector, TComparer>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class AsyncComparer<TKey, TKeySelector, TComparer> : Comparer<TKey, TComparer>
                where TKeySelector : IFunc<TSource, Promise<TKey>>
                where TComparer : IComparer<TKey>
            {
                private TKeySelector _keySelector;

                private AsyncComparer() { }

                [MethodImpl(InlineOption)]
                private static AsyncComparer<TKey, TKeySelector, TComparer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AsyncComparer<TKey, TKeySelector, TComparer>>();
                    return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                        ? new AsyncComparer<TKey, TKeySelector, TComparer>()
                        : obj.UnsafeAs<AsyncComparer<TKey, TKeySelector, TComparer>>();
                }

                [MethodImpl(InlineOption)]
                internal static AsyncComparer<TKey, TKeySelector, TComparer> GetOrCreate(HandleablePromiseBase head, TKeySelector keySelector, TComparer comparer)
                {
                    var instance = GetOrCreate();
                    instance._next = head;
                    instance._keySelector = keySelector;
                    instance._comparer = comparer;
                    return instance;
                }

                internal override async Promise ComputeKeys(TempCollectionBuilder<TSource> elements)
                {
                    _keys = new TempCollectionBuilder<TKey>(elements._count, elements._count);
                    for (int i = 0; i < _keys._count; ++i)
                    {
                        _keys._items[i] = await _keySelector.Invoke(elements._items[i]);
                    }
                }

                internal override async Promise ComputeKeys(TempCollectionBuilder<TSource> elements, SwitchToConfiguredContextReusableAwaiter switchToConfiguredContextAwaiter)
                {
                    _keys = new TempCollectionBuilder<TKey>(elements._count, elements._count);
                    for (int i = 0; i < _keys._count; ++i)
                    {
                        await switchToConfiguredContextAwaiter;
                        _keys._items[i] = await _keySelector.Invoke(elements._items[i]);
                    }
                }

                internal override void Dispose()
                {
                    base.Dispose();
                    ObjectPool.MaybeRepool(this);
                }
            } // AsyncComparer<TKey, TKeySelector, TComparer>
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