#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

#pragma warning disable IDE0180 // Use tuple to swap values

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Just implementing the Internal.IAsyncEnumerable<T> interface instead of the full AsyncEnumerableBase<T> class,
        // because we can build up .Union(...).Union(...). chains with arbitrary depth,
        // so we only create the iterator class when the AsyncEnumerable is actually iterated.
        internal abstract class UnionAsyncEnumerableBase<TSource, TEqualityComparer> : HandleablePromiseBase, IAsyncEnumerable<TSource>, IAsyncIterator<TSource>
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            internal TEqualityComparer _comparer;
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
                var head = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                _next = null;
                // We use `IAsyncIterator` instead of the specific type to reduce the number of generated generic types.
                var enumerable = AsyncEnumerableCreate<TSource, IAsyncIterator<TSource>>.GetOrCreate(head);
                return new AsyncEnumerable<TSource>(enumerable).GetAsyncEnumerator(cancelationToken);
            }

            public abstract AsyncIteratorMethod Start(AsyncStreamWriter<TSource> streamWriter, CancelationToken cancelationToken);
            public abstract Promise DisposeAsyncWithoutStart();

            internal abstract AsyncEnumerator<TSource> GetNextEnumerator(ref UnionAsyncEnumerableBase<TSource, TEqualityComparer> nextRef, ref bool first);
            internal abstract AsyncEnumerator<TSource> GetNextEnumeratorAndDispose(ref UnionAsyncEnumerableBase<TSource, TEqualityComparer> nextRef, ref bool first);

            internal virtual AsyncEnumerable<TSource> Union(int enumerableId, AsyncEnumerable<TSource> first, TEqualityComparer comparer)
            {
                if (first._target is UnionAsyncEnumerableBase<TSource, TEqualityComparer> unionEnumerable1)
                {
                    if (_next is ConfiguredUnion2AsyncEnumerable<TSource, TEqualityComparer>)
                    {
                        // We can't flatten configured unions as second, so we just use the naive union.
                        var naiveEnumerable = Union2AsyncEnumerable<TSource, TEqualityComparer>.GetOrCreate(first.GetAsyncEnumerator(), GetAsyncEnumerator(enumerableId, default), _comparer);
                        return new AsyncEnumerable<TSource>(naiveEnumerable, naiveEnumerable._id);
                    }

                    if (EqualityComparer<TEqualityComparer>.Default.Equals(comparer, unionEnumerable1._comparer))
                    {
                        IncrementId(enumerableId);
                        unionEnumerable1.IncrementId(first._id);
                        // Both were previously unioned with the same comparer, just link them together and return.
                        var secondHead = _next;
                        _next = unionEnumerable1._next;
                        unionEnumerable1._next = secondHead;
                        return new AsyncEnumerable<TSource>(this, _id);
                    }
                }

                IncrementId(enumerableId);
                var enumerable = UnionNAsyncEnumerable<TSource, TEqualityComparer>.GetOrCreate(_next, first.GetAsyncEnumerator(), comparer);
                _next = enumerable;
                return new AsyncEnumerable<TSource>(this, _id);
            }

            internal virtual AsyncEnumerable<TSource> Union(int enumerableId, AsyncEnumerable<TSource> second)
            {
                if (second._target is UnionAsyncEnumerableBase<TSource, TEqualityComparer> unionEnumerable2)
                {
                    if (unionEnumerable2._next is ConfiguredUnion2AsyncEnumerable<TSource, TEqualityComparer>)
                    {
                        // We can't flatten configured unions as second, so we just use the naive union.
                        var naiveEnumerable = Union2AsyncEnumerable<TSource, TEqualityComparer>.GetOrCreate(GetAsyncEnumerator(enumerableId, default), second.GetAsyncEnumerator(), _comparer);
                        return new AsyncEnumerable<TSource>(naiveEnumerable, naiveEnumerable._id);
                    }

                    if (EqualityComparer<TEqualityComparer>.Default.Equals(_comparer, unionEnumerable2._comparer))
                    {
                        IncrementId(enumerableId);
                        unionEnumerable2.IncrementId(second._id);
                        // Both were previously unioned with the same comparer, just link them together and return.
                        var secondHead = _next;
                        _next = unionEnumerable2._next;
                        unionEnumerable2._next = secondHead;
                        return new AsyncEnumerable<TSource>(unionEnumerable2, unionEnumerable2._id);
                    }
                }

                IncrementId(enumerableId);
                var enumerable = UnionNAsyncEnumerable<TSource, TEqualityComparer>.GetOrCreate(_next, second.GetAsyncEnumerator(), _comparer);
                _next = enumerable;
                return new AsyncEnumerable<TSource>(enumerable, enumerable._id);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class Union2AsyncEnumerable<TSource, TEqualityComparer> : UnionAsyncEnumerableBase<TSource, TEqualityComparer>
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            private AsyncEnumerator<TSource> _first;
            private AsyncEnumerator<TSource> _second;

            private Union2AsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static Union2AsyncEnumerable<TSource, TEqualityComparer> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<Union2AsyncEnumerable<TSource, TEqualityComparer>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new Union2AsyncEnumerable<TSource, TEqualityComparer>()
                    : obj.UnsafeAs<Union2AsyncEnumerable<TSource, TEqualityComparer>>();
            }

            [MethodImpl(InlineOption)]
            internal static Union2AsyncEnumerable<TSource, TEqualityComparer> GetOrCreate(AsyncEnumerator<TSource> first, AsyncEnumerator<TSource> second, TEqualityComparer comparer)
            {
                var instance = GetOrCreate();
                // This is the head, _next points to the head.
                instance._next = instance;
                instance._comparer = comparer;
                instance._first = first;
                instance._second = second;
                return instance;
            }

            private void Dispose()
            {
                ClearReferences(ref _comparer);
                _first = default;
                _second = default;
                ObjectPool.MaybeRepool(this);
            }

            public override async Promise DisposeAsyncWithoutStart()
            {
                var firstEnumerator = _first;
                var nextEnumerator = _second;
                var next = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                Dispose();
                try
                {
                    await firstEnumerator.DisposeAsync();
                }
                finally
                {
                    try
                    {
                        await nextEnumerator.DisposeAsync();
                    }
                    finally
                    {
                        // We can't do a try/finally loop to dispose all of the UnionN enumerators,
                        // and we don't want to dispose recursively,
                        // so instead we simulate the behavior by only capturing the last exception.
                        Exception ex = null;
                        bool first = true;
                        while (next != null)
                        {
                            nextEnumerator = next.GetNextEnumeratorAndDispose(ref next, ref first);
                            try
                            {
                                await nextEnumerator.DisposeAsync();
                            }
                            catch (Exception e)
                            {
                                ex = e;
                            }
                        }
                        if (ex != null)
                        {
                            ExceptionDispatchInfo.Capture(ex).Throw();
                        }
                    }
                }
            }

            // This will be called if 2 unioned enumerables were unioned (so the unions were flattened).
            internal override AsyncEnumerator<TSource> GetNextEnumerator(ref UnionAsyncEnumerableBase<TSource, TEqualityComparer> nextRef, ref bool first)
            {
                if (first)
                {
                    first = false;
                    return _first;
                }
                nextRef = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                first = true;
                return _second;
            }

            internal override AsyncEnumerator<TSource> GetNextEnumeratorAndDispose(ref UnionAsyncEnumerableBase<TSource, TEqualityComparer> nextRef, ref bool first)
            {
                if (first)
                {
                    first = false;
                    return _first;
                }
                nextRef = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                first = true;
                var second = _second;
                Dispose();
                return second;
            }

            public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
            {
                // The enumerators were retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _first._target._cancelationToken = cancelationToken;
                _second._target._cancelationToken = cancelationToken;
                try
                {
                    using (var set = new PoolBackedSet<TSource, TEqualityComparer>(_comparer))
                    {
                        while (await _first.MoveNextAsync())
                        {
                            var element = _first.Current;
                            if (set.Add(element))
                            {
                                await writer.YieldAsync(element);
                            }
                        }
                        while (await _second.MoveNextAsync())
                        {
                            var element = _second.Current;
                            if (set.Add(element))
                            {
                                await writer.YieldAsync(element);
                            }
                        }

                        var next = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                        bool first = true;
                        while (next != null)
                        {
                            var nextEnumerator = next.GetNextEnumerator(ref next, ref first);
                            // The enumerators were retrieved without a cancelation token when the original function was called.
                            // We need to propagate the token that was passed in, so we assign it before starting iteration.
                            nextEnumerator._target._cancelationToken = cancelationToken;
                            while (await nextEnumerator.MoveNextAsync())
                            {
                                var element = nextEnumerator.Current;
                                if (set.Add(element))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    // Disposal logic is exactly the same as DisposeAsyncWithoutStart, we copy it here instead of calling the method so that we only have 1 async state machine.
                    var firstEnumerator = _first;
                    var nextEnumerator = _second;
                    var next = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                    Dispose();
                    try
                    {
                        await firstEnumerator.DisposeAsync();
                    }
                    finally
                    {
                        try
                        {
                            await nextEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            // We can't do a try/finally loop to dispose all of the UnionN enumerators,
                            // and we don't want to dispose recursively,
                            // so instead we simulate the behavior by only capturing the last exception.
                            Exception ex = null;
                            bool first = true;
                            while (next != null)
                            {
                                nextEnumerator = next.GetNextEnumeratorAndDispose(ref next, ref first);
                                try
                                {
                                    await nextEnumerator.DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    ex = e;
                                }
                            }
                            if (ex != null)
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }
                        }
                    }
                }
            } // Start
        } // class Union2AsyncEnumerable<TSource>

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class UnionNAsyncEnumerable<TSource, TEqualityComparer> : UnionAsyncEnumerableBase<TSource, TEqualityComparer>
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            private AsyncEnumerator<TSource> _nextEnumerator;

            private UnionNAsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static UnionNAsyncEnumerable<TSource, TEqualityComparer> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<UnionNAsyncEnumerable<TSource, TEqualityComparer>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new UnionNAsyncEnumerable<TSource, TEqualityComparer>()
                    : obj.UnsafeAs<UnionNAsyncEnumerable<TSource, TEqualityComparer>>();
            }

            [MethodImpl(InlineOption)]
            internal static UnionNAsyncEnumerable<TSource, TEqualityComparer> GetOrCreate(HandleablePromiseBase head, AsyncEnumerator<TSource> next, TEqualityComparer comparer)
            {
                var instance = GetOrCreate();
                instance._next = head;
                instance._comparer = comparer;
                instance._nextEnumerator = next;
                return instance;
            }

            private void Dispose()
            {
                ClearReferences(ref _comparer);
                _nextEnumerator = default;
                ObjectPool.MaybeRepool(this);
            }

            public override async Promise DisposeAsyncWithoutStart()
            {
                var nextEnumerator = _nextEnumerator;
                var next = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                Dispose();
                try
                {
                    await nextEnumerator.DisposeAsync();
                }
                finally
                {
                    // We can't do a try/finally loop to dispose all of the UnionN enumerators,
                    // and we don't want to dispose recursively,
                    // so instead we simulate the behavior by only capturing the last exception.
                    Exception ex = null;
                    bool first = true;
                    while (next != null)
                    {
                        nextEnumerator = next.GetNextEnumeratorAndDispose(ref next, ref first);
                        try
                        {
                            await nextEnumerator.DisposeAsync();
                        }
                        catch (Exception e)
                        {
                            ex = e;
                        }
                    }
                    if (ex != null)
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                    }
                }
            }

            internal override AsyncEnumerator<TSource> GetNextEnumerator(ref UnionAsyncEnumerableBase<TSource, TEqualityComparer> nextRef, ref bool first)
            {
                nextRef = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                return _nextEnumerator;
            }

            internal override AsyncEnumerator<TSource> GetNextEnumeratorAndDispose(ref UnionAsyncEnumerableBase<TSource, TEqualityComparer> nextRef, ref bool first)
            {
                nextRef = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                var next = _nextEnumerator;
                Dispose();
                return next;
            }

            public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
            {
                // The enumerators were retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _nextEnumerator._target._cancelationToken = cancelationToken;
                try
                {
                    using (var set = new PoolBackedSet<TSource, TEqualityComparer>(_comparer))
                    {
                        while (await _nextEnumerator.MoveNextAsync())
                        {
                            var element = _nextEnumerator.Current;
                            if (set.Add(element))
                            {
                                await writer.YieldAsync(element);
                            }
                        }

                        var next = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                        bool first = true;
                        while (next != null)
                        {
                            var nextEnumerator = next.GetNextEnumerator(ref next, ref first);
                            // The enumerators were retrieved without a cancelation token when the original function was called.
                            // We need to propagate the token that was passed in, so we assign it before starting iteration.
                            nextEnumerator._target._cancelationToken = cancelationToken;
                            while (await nextEnumerator.MoveNextAsync())
                            {
                                var element = nextEnumerator.Current;
                                if (set.Add(element))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    // Disposal logic is exactly the same as DisposeAsyncWithoutStart, we copy it here instead of calling the method so that we only have 1 async state machine.
                    var nextEnumerator = _nextEnumerator;
                    var next = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                    Dispose();
                    try
                    {
                        await nextEnumerator.DisposeAsync();
                    }
                    finally
                    {
                        // We can't do a try/finally loop to dispose all of the UnionN enumerators,
                        // and we don't want to dispose recursively,
                        // so instead we simulate the behavior by only capturing the last exception.
                        Exception ex = null;
                        bool first = true;
                        while (next != null)
                        {
                            nextEnumerator = next.GetNextEnumeratorAndDispose(ref next, ref first);
                            try
                            {
                                await nextEnumerator.DisposeAsync();
                            }
                            catch (Exception e)
                            {
                                ex = e;
                            }
                        }
                        if (ex != null)
                        {
                            ExceptionDispatchInfo.Capture(ex).Throw();
                        }
                    }
                }
            } // Start
        } // class UnionNAsyncEnumerable<TSource>

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class ConfiguredUnion2AsyncEnumerable<TSource, TEqualityComparer> : UnionAsyncEnumerableBase<TSource, TEqualityComparer>
            where TEqualityComparer : IEqualityComparer<TSource>
        {
            private ConfiguredAsyncEnumerable<TSource>.Enumerator _configuredFirst;
            private AsyncEnumerator<TSource> _second;

            private ConfiguredUnion2AsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static ConfiguredUnion2AsyncEnumerable<TSource, TEqualityComparer> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ConfiguredUnion2AsyncEnumerable<TSource, TEqualityComparer>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new ConfiguredUnion2AsyncEnumerable<TSource, TEqualityComparer>()
                    : obj.UnsafeAs<ConfiguredUnion2AsyncEnumerable<TSource, TEqualityComparer>>();
            }

            [MethodImpl(InlineOption)]
            internal static ConfiguredUnion2AsyncEnumerable<TSource, TEqualityComparer> GetOrCreate(ConfiguredAsyncEnumerable<TSource>.Enumerator configuredFirst, AsyncEnumerator<TSource> second, TEqualityComparer comparer)
            {
                var instance = GetOrCreate();
                // This is the head, _next points to the head.
                instance._next = instance;
                instance._comparer = comparer;
                instance._configuredFirst = configuredFirst;
                instance._second = second;
                return instance;
            }

            private void Dispose()
            {
                ClearReferences(ref _comparer);
                _configuredFirst = default;
                _second = default;
                ObjectPool.MaybeRepool(this);
            }

            public override async Promise DisposeAsyncWithoutStart()
            {
                var firstEnumerator = _configuredFirst;
                var nextEnumerator = _second;
                var next = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                Dispose();
                try
                {
                    await firstEnumerator.DisposeAsync();
                }
                finally
                {
                    try
                    {
                        await nextEnumerator.DisposeAsync();
                    }
                    finally
                    {
                        // We can't do a try/finally loop to dispose all of the UnionN enumerators,
                        // and we don't want to dispose recursively,
                        // so instead we simulate the behavior by only capturing the last exception.
                        Exception ex = null;
                        bool first = true;
                        while (next != null)
                        {
                            nextEnumerator = next.GetNextEnumeratorAndDispose(ref next, ref first);
                            try
                            {
                                await nextEnumerator.DisposeAsync();
                            }
                            catch (Exception e)
                            {
                                ex = e;
                            }
                        }
                        if (ex != null)
                        {
                            ExceptionDispatchInfo.Capture(ex).Throw();
                        }
                    }
                }
            }

            public override async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
            {
                // The enumerator may have been configured with a cancelation token. We need to join the passed in token before starting iteration.
                var enumerableRef = _configuredFirst._enumerator._target;
                var joinedCancelationSource = MaybeJoinCancelationTokens(enumerableRef._cancelationToken, cancelationToken, out enumerableRef._cancelationToken);
                // Use the same cancelation token for both enumerators.
                _second._target._cancelationToken = enumerableRef._cancelationToken;

                try
                {
                    using (var set = new PoolBackedSet<TSource, TEqualityComparer>(_comparer))
                    {
                        while (await _configuredFirst.MoveNextAsync())
                        {
                            var element = _configuredFirst.Current;
                            if (set.Add(element))
                            {
                                await writer.YieldAsync(element);
                            }
                        }
                        // We need to make sure we're on the configured context before invoking the comparer.
                        while (await _second.MoveNextAsync().ConfigureAwait(_configuredFirst.ContinuationOptions))
                        {
                            var element = _second.Current;
                            if (set.Add(element))
                            {
                                await writer.YieldAsync(element);
                            }
                        }

                        var next = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                        bool first = true;
                        while (next != null)
                        {
                            var nextEnumerator = next.GetNextEnumerator(ref next, ref first);
                            // The enumerators were retrieved without a cancelation token when the original function was called.
                            // We need to propagate the token that was passed in, so we assign it before starting iteration.
                            nextEnumerator._target._cancelationToken = cancelationToken;
                            // We need to make sure we're on the configured context before invoking the comparer.
                            while (await nextEnumerator.MoveNextAsync().ConfigureAwait(_configuredFirst.ContinuationOptions))
                            {
                                var element = nextEnumerator.Current;
                                if (set.Add(element))
                                {
                                    await writer.YieldAsync(element);
                                }
                            }
                        }
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    joinedCancelationSource.TryDispose();
                    // Disposal logic is exactly the same as DisposeAsyncWithoutStart, we copy it here instead of calling the method so that we only have 1 async state machine.
                    var firstEnumerator = _configuredFirst;
                    var nextEnumerator = _second;
                    var next = _next.UnsafeAs<UnionAsyncEnumerableBase<TSource, TEqualityComparer>>();
                    Dispose();
                    try
                    {
                        await firstEnumerator.DisposeAsync();
                    }
                    finally
                    {
                        try
                        {
                            await nextEnumerator.DisposeAsync();
                        }
                        finally
                        {
                            // We can't do a try/finally loop to dispose all of the UnionN enumerators,
                            // and we don't want to dispose recursively,
                            // so instead we simulate the behavior by only capturing the last exception.
                            Exception ex = null;
                            bool first = true;
                            while (next != null)
                            {
                                nextEnumerator = next.GetNextEnumeratorAndDispose(ref next, ref first);
                                try
                                {
                                    await nextEnumerator.DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    ex = e;
                                }
                            }
                            if (ex != null)
                            {
                                ExceptionDispatchInfo.Capture(ex).Throw();
                            }
                        }
                    }
                }
            } // Start

            internal override AsyncEnumerable<TSource> Union(int enumerableId, AsyncEnumerable<TSource> first, TEqualityComparer comparer)
            {
                // We can't flatten configured unions as second, so we just use the naive union.
                var enumerable = Union2AsyncEnumerable<TSource, TEqualityComparer>.GetOrCreate(first.GetAsyncEnumerator(), GetAsyncEnumerator(enumerableId, default), comparer);
                return new AsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal override AsyncEnumerable<TSource> Union(int enumerableId, AsyncEnumerable<TSource> second)
            {
                if (second._target is UnionAsyncEnumerableBase<TSource, TEqualityComparer> unionEnumerable2
                    && unionEnumerable2._next is ConfiguredUnion2AsyncEnumerable<TSource, TEqualityComparer>)
                {
                    // We can't flatten configured unions as second, so we just use the naive union.
                    var naiveEnumerable = Union2AsyncEnumerable<TSource, TEqualityComparer>.GetOrCreate(GetAsyncEnumerator(enumerableId, default), second.GetAsyncEnumerator(), _comparer);
                    return new AsyncEnumerable<TSource>(naiveEnumerable, naiveEnumerable._id);
                }

                IncrementId(enumerableId);
                var enumerable = UnionNAsyncEnumerable<TSource, TEqualityComparer>.GetOrCreate(_next, second.GetAsyncEnumerator(), _comparer);
                _next = enumerable;
                return new AsyncEnumerable<TSource>(enumerable, enumerable._id);
            }

            internal override AsyncEnumerator<TSource> GetNextEnumerator(ref UnionAsyncEnumerableBase<TSource, TEqualityComparer> nextRef, ref bool first) => throw new System.InvalidOperationException();
            internal override AsyncEnumerator<TSource> GetNextEnumeratorAndDispose(ref UnionAsyncEnumerableBase<TSource, TEqualityComparer> nextRef, ref bool first) => throw new System.InvalidOperationException();
        } // class ConfiguredUnion2AsyncEnumerable<TSource>
    } // class Internal
} // namespace Proto.Promises