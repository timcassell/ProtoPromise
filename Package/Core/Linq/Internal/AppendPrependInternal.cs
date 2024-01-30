#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Collections;
using Proto.Promises.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class AppendPrependHelper<TSource>
        {
            internal static AsyncEnumerable<TSource> Prepend(AsyncEnumerable<TSource> source, TSource element)
            {
                var enumerable = source._target is AppendPrependAsyncEnumerableBase<TSource> appendable
                    ? appendable.Prepend(source._id, element)
                    : Prepend1AsyncEnumerable<TSource>.GetOrCreate(source.GetAsyncEnumerator(), element);
                return new AsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal static AsyncEnumerable<TSource> Append(AsyncEnumerable<TSource> source, TSource element)
            {
                var enumerable = source._target is AppendPrependAsyncEnumerableBase<TSource> appendable
                    ? appendable.Append(source._id, element)
                    : Append1AsyncEnumerable<TSource>.GetOrCreate(source.GetAsyncEnumerator(), element);
                return new AsyncEnumerable<TSource>(enumerable, enumerable._id);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Just implementing the Internal.IAsyncEnumerable<T> interface instead of the full AsyncEnumerableBase<T> class,
        // because we can build up Append(item).Append(item) etc chains with arbitrary depth,
        // so we only create the iterator class when the AsyncEnumerable is actually iterated.
        internal abstract class AppendPrependAsyncEnumerableBase<TSource> : HandleablePromiseBase, IAsyncEnumerable<TSource>, IAsyncIterator<TSource>
        {
            protected AsyncEnumerator<TSource> _source;
            internal int _id = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.

            public bool GetCanBeEnumerated(int id) => id == _id;

            protected void IncrementId(int id)
            {
                if (Interlocked.CompareExchange(ref _id, id + 1, id) != id)
                {
                    throw new InvalidOperationException("AsyncEnumerable instance is not valid. AsyncEnumerable may only be used once.", GetFormattedStacktrace(3));
                }
            }
            
            public AsyncEnumerator<TSource> GetAsyncEnumerator(int id, CancelationToken cancelationToken)
            {
                IncrementId(id);

                // We use `IAsyncIterator` instead of the specific type to reduce the number of generated generic types.
                var enumerable = AsyncEnumerableCreate<TSource, IAsyncIterator<TSource>>.GetOrCreate(this);
                return new AsyncEnumerable<TSource>(enumerable).GetAsyncEnumerator(cancelationToken);
            }

            public abstract AsyncIteratorMethod Start(AsyncStreamWriter<TSource> streamWriter, CancelationToken cancelationToken);
            public abstract Promise DisposeAsyncWithoutStart();

            internal abstract AppendPrependAsyncEnumerableBase<TSource> Prepend(int enumerableId, TSource element);
            internal abstract AppendPrependAsyncEnumerableBase<TSource> Append(int enumerableId, TSource element);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class Prepend1AsyncEnumerable<TSource> : AppendPrependAsyncEnumerableBase<TSource>
        {
            private TSource _prepended;

            private Prepend1AsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static Prepend1AsyncEnumerable<TSource> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<Prepend1AsyncEnumerable<TSource>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new Prepend1AsyncEnumerable<TSource>()
                    : obj.UnsafeAs<Prepend1AsyncEnumerable<TSource>>();
            }

            [MethodImpl(InlineOption)]
            internal static Prepend1AsyncEnumerable<TSource> GetOrCreate(AsyncEnumerator<TSource> source, TSource prepended)
            {
                var instance = GetOrCreate();
                instance._next = null;
                instance._source = source;
                instance._prepended = prepended;
                return instance;
            }

            private void Dispose()
            {
                _source = default;
                _prepended = default;
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
                    await writer.YieldAsync(_prepended);
                    while (await _source.MoveNextAsync())
                    {
                        await writer.YieldAsync(_source.Current);
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

            internal override AppendPrependAsyncEnumerableBase<TSource> Prepend(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                var prepended = new TempCollectionBuilder<TSource>(2, 2);
                prepended[0] = _prepended;
                prepended[1] = element;
                var enumerable = PrependNAsyncEnumerable<TSource>.GetOrCreate(_source, prepended);
                Dispose();
                return enumerable;
            }

            internal override AppendPrependAsyncEnumerableBase<TSource> Append(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                var enumerable = AppendPrepend1AsyncEnumerable<TSource>.GetOrCreate(_source, _prepended, element);
                Dispose();
                return enumerable;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class Append1AsyncEnumerable<TSource> : AppendPrependAsyncEnumerableBase<TSource>
        {
            private TSource _appended;

            private Append1AsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static Append1AsyncEnumerable<TSource> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<Append1AsyncEnumerable<TSource>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new Append1AsyncEnumerable<TSource>()
                    : obj.UnsafeAs<Append1AsyncEnumerable<TSource>>();
            }

            [MethodImpl(InlineOption)]
            internal static Append1AsyncEnumerable<TSource> GetOrCreate(AsyncEnumerator<TSource> source, TSource appended)
            {
                var instance = GetOrCreate();
                instance._next = null;
                instance._source = source;
                instance._appended = appended;
                return instance;
            }

            private void Dispose()
            {
                _source = default;
                _appended = default;
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
                    while (await _source.MoveNextAsync())
                    {
                        await writer.YieldAsync(_source.Current);
                    }
                    await writer.YieldAsync(_appended);

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

            internal override AppendPrependAsyncEnumerableBase<TSource> Prepend(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                var enumerable = AppendPrepend1AsyncEnumerable<TSource>.GetOrCreate(_source, element, _appended);
                Dispose();
                return enumerable;
            }

            internal override AppendPrependAsyncEnumerableBase<TSource> Append(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                var appended = new TempCollectionBuilder<TSource>(2, 2);
                appended[0] = _appended;
                appended[1] = element;
                var enumerable = AppendNAsyncEnumerable<TSource>.GetOrCreate(_source, appended);
                Dispose();
                return enumerable;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AppendPrepend1AsyncEnumerable<TSource> : AppendPrependAsyncEnumerableBase<TSource>
        {
            private TSource _prepended;
            private TSource _appended;

            private AppendPrepend1AsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static AppendPrepend1AsyncEnumerable<TSource> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AppendPrepend1AsyncEnumerable<TSource>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new AppendPrepend1AsyncEnumerable<TSource>()
                    : obj.UnsafeAs<AppendPrepend1AsyncEnumerable<TSource>>();
            }

            [MethodImpl(InlineOption)]
            internal static AppendPrepend1AsyncEnumerable<TSource> GetOrCreate(AsyncEnumerator<TSource> source, TSource prepended, TSource appended)
            {
                var instance = GetOrCreate();
                instance._next = null;
                instance._source = source;
                instance._prepended = prepended;
                instance._appended = appended;
                return instance;
            }

            private void Dispose()
            {
                _source = default;
                _prepended = default;
                _appended = default;
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
                    await writer.YieldAsync(_prepended);
                    while (await _source.MoveNextAsync())
                    {
                        await writer.YieldAsync(_source.Current);
                    }
                    await writer.YieldAsync(_appended);

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

            internal override AppendPrependAsyncEnumerableBase<TSource> Prepend(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                var prepended = new TempCollectionBuilder<TSource>(2, 2);
                prepended[0] = _prepended;
                prepended[1] = element;
                var appended = new TempCollectionBuilder<TSource>(1, 1);
                appended[0] = _appended;
                var enumerable = AppendPrependNAsyncEnumerable<TSource>.GetOrCreate(_source, prepended, appended);
                Dispose();
                return enumerable;
            }

            internal override AppendPrependAsyncEnumerableBase<TSource> Append(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                var prepended = new TempCollectionBuilder<TSource>(1, 1);
                prepended[0] = _prepended;
                var appended = new TempCollectionBuilder<TSource>(2, 2);
                appended[0] = _appended;
                appended[1] = element;
                var enumerable = AppendPrependNAsyncEnumerable<TSource>.GetOrCreate(_source, prepended, appended);
                Dispose();
                return enumerable;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class PrependNAsyncEnumerable<TSource> : AppendPrependAsyncEnumerableBase<TSource>
        {
            private TempCollectionBuilder<TSource> _prepended;

            private PrependNAsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static PrependNAsyncEnumerable<TSource> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<PrependNAsyncEnumerable<TSource>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new PrependNAsyncEnumerable<TSource>()
                    : obj.UnsafeAs<PrependNAsyncEnumerable<TSource>>();
            }

            [MethodImpl(InlineOption)]
            internal static PrependNAsyncEnumerable<TSource> GetOrCreate(AsyncEnumerator<TSource> source, TempCollectionBuilder<TSource> prepended)
            {
                var instance = GetOrCreate();
                instance._next = null;
                instance._source = source;
                instance._prepended = prepended;
                return instance;
            }

            private void Dispose()
            {
                _source = default;
                _prepended.Dispose();
                ObjectPool.MaybeRepool(this);
            }

            public override Promise DisposeAsyncWithoutStart()
            {
                var source = _source;
                Dispose();
                return source.DisposeAsync();
            }

            private void RepoolWithoutDispose()
            {
                _source = default;
                _prepended = default;
                ObjectPool.MaybeRepool(this);
            }

            public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _source._target._cancelationToken = cancelationToken;
                try
                {
                    for (int i = _prepended._count - 1; i >= 0; --i)
                    {
                        await writer.YieldAsync(_prepended[i]);
                    }
                    while (await _source.MoveNextAsync())
                    {
                        await writer.YieldAsync(_source.Current);
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

            internal override AppendPrependAsyncEnumerableBase<TSource> Prepend(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                _prepended.Add(element);
                return this;
            }

            internal override AppendPrependAsyncEnumerableBase<TSource> Append(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                var appended = new TempCollectionBuilder<TSource>(1, 1);
                appended[0] = element;
                var enumerable = AppendPrependNAsyncEnumerable<TSource>.GetOrCreate(_source, _prepended, appended);
                RepoolWithoutDispose();
                return enumerable;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AppendNAsyncEnumerable<TSource> : AppendPrependAsyncEnumerableBase<TSource>
        {
            private TempCollectionBuilder<TSource> _appended;

            private AppendNAsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static AppendNAsyncEnumerable<TSource> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AppendNAsyncEnumerable<TSource>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new AppendNAsyncEnumerable<TSource>()
                    : obj.UnsafeAs<AppendNAsyncEnumerable<TSource>>();
            }

            [MethodImpl(InlineOption)]
            internal static AppendNAsyncEnumerable<TSource> GetOrCreate(AsyncEnumerator<TSource> source, TempCollectionBuilder<TSource> appended)
            {
                var instance = GetOrCreate();
                instance._next = null;
                instance._source = source;
                instance._appended = appended;
                return instance;
            }

            private void Dispose()
            {
                _source = default;
                _appended.Dispose();
                ObjectPool.MaybeRepool(this);
            }

            public override Promise DisposeAsyncWithoutStart()
            {
                var source = _source;
                Dispose();
                return source.DisposeAsync();
            }

            private void RepoolWithoutDispose()
            {
                _source = default;
                _appended = default;
                ObjectPool.MaybeRepool(this);
            }

            public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _source._target._cancelationToken = cancelationToken;
                try
                {
                    while (await _source.MoveNextAsync())
                    {
                        await writer.YieldAsync(_source.Current);
                    }
                    for (int i = 0; i < _appended._count; ++i)
                    {
                        await writer.YieldAsync(_appended[i]);
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

            internal override AppendPrependAsyncEnumerableBase<TSource> Prepend(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                var prepended = new TempCollectionBuilder<TSource>(1, 1);
                prepended[0] = element;
                var enumerable = AppendPrependNAsyncEnumerable<TSource>.GetOrCreate(_source, prepended, _appended);
                RepoolWithoutDispose();
                return enumerable;
            }

            internal override AppendPrependAsyncEnumerableBase<TSource> Append(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                _appended.Add(element);
                return this;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AppendPrependNAsyncEnumerable<TSource> : AppendPrependAsyncEnumerableBase<TSource>
        {
            private TempCollectionBuilder<TSource> _prepended;
            private TempCollectionBuilder<TSource> _appended;

            private AppendPrependNAsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static AppendPrependNAsyncEnumerable<TSource> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AppendPrependNAsyncEnumerable<TSource>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new AppendPrependNAsyncEnumerable<TSource>()
                    : obj.UnsafeAs<AppendPrependNAsyncEnumerable<TSource>>();
            }

            [MethodImpl(InlineOption)]
            internal static AppendPrependNAsyncEnumerable<TSource> GetOrCreate(AsyncEnumerator<TSource> source, TempCollectionBuilder<TSource> prepended, TempCollectionBuilder<TSource> appended)
            {
                var instance = GetOrCreate();
                instance._next = null;
                instance._source = source;
                instance._prepended = prepended;
                instance._appended = appended;
                return instance;
            }

            private void Dispose()
            {
                _source = default;
                _prepended.Dispose();
                _appended.Dispose();
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
                    for (int i = _prepended._count - 1; i >= 0; --i)
                    {
                        await writer.YieldAsync(_prepended[i]);
                    }
                    while (await _source.MoveNextAsync())
                    {
                        await writer.YieldAsync(_source.Current);
                    }
                    for (int i = 0; i < _appended._count; ++i)
                    {
                        await writer.YieldAsync(_appended[i]);
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

            internal override AppendPrependAsyncEnumerableBase<TSource> Prepend(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                _prepended.Add(element);
                return this;
            }

            internal override AppendPrependAsyncEnumerableBase<TSource> Append(int enumerableId, TSource element)
            {
                IncrementId(enumerableId);

                _appended.Add(element);
                return this;
            }
        }
    } // class Internal
#endif
} // namespace Proto.Promises