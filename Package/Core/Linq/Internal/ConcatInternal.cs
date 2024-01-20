#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using Proto.Promises.Linq;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        // Just implementing the Internal.IAsyncEnumerable<T> interface instead of the full AsyncEnumerableBase<T> class,
        // because we can build up .Concat(...).Concat(...). chains with arbitrary depth,
        // so we only create the iterator class when the AsyncEnumerable is actually iterated.
        internal abstract class ConcatAsyncEnumerableBase<TSource> : HandleablePromiseBase, IAsyncEnumerable<TSource>, IAsyncIterator<TSource>
        {
            internal int _id = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.

            public bool GetIsValid(int id) => id == _id;

            internal void IncrementId(int id)
            {
                if (Interlocked.CompareExchange(ref _id, id + 1, id) != id)
                {
                    throw new InvalidOperationException("AsyncEnumerable instance is not valid. AsyncEnumerable may only be used once.", GetFormattedStacktrace(3));
                }
            }

            public AsyncEnumerator<TSource> GetAsyncEnumerator(int id, CancelationToken cancelationToken)
            {
                IncrementId(id);

                // The head is stored in _next.
                var head = _next.UnsafeAs<ConcatAsyncEnumerableBase<TSource>>();
                _next = null;
                // We use `IAsyncIterator` instead of the specific type to reduce the number of generated generic types.
                var enumerable = AsyncEnumerableCreate<TSource, IAsyncIterator<TSource>>.GetOrCreate(head);
                return new AsyncEnumerable<TSource>(enumerable).GetAsyncEnumerator(cancelationToken);
            }

            public abstract AsyncEnumerableMethod Start(AsyncStreamWriter<TSource> streamWriter, CancelationToken cancelationToken);
            internal abstract AsyncEnumerator<TSource> GetNextEnumerator(ref ConcatAsyncEnumerableBase<TSource> nextRef, ref bool first);
            internal abstract AsyncEnumerator<TSource> GetNextEnumeratorAndDispose(ref ConcatAsyncEnumerableBase<TSource> nextRef, ref bool first);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class Concat2AsyncEnumerable<TSource> : ConcatAsyncEnumerableBase<TSource>
        {
            private AsyncEnumerator<TSource> _first;
            private AsyncEnumerator<TSource> _second;

            private Concat2AsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static Concat2AsyncEnumerable<TSource> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<Concat2AsyncEnumerable<TSource>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new Concat2AsyncEnumerable<TSource>()
                    : obj.UnsafeAs<Concat2AsyncEnumerable<TSource>>();
            }

            [MethodImpl(InlineOption)]
            internal static Concat2AsyncEnumerable<TSource> GetOrCreate(AsyncEnumerator<TSource> first, AsyncEnumerator<TSource> second)
            {
                var instance = GetOrCreate();
                // This is the head, _next points to the head.
                instance._next = instance;
                instance._first = first;
                instance._second = second;
                return instance;
            }

            private void Dispose()
            {
                _first = default;
                _second = default;
                ObjectPool.MaybeRepool(this);
            }

            // This will be called if 2 concatenated enumerables were concatenated (so the concatenations were flattened).
            internal override AsyncEnumerator<TSource> GetNextEnumerator(ref ConcatAsyncEnumerableBase<TSource> nextRef, ref bool first)
            {
                if (first)
                {
                    first = false;
                    return _first;
                }
                nextRef = _next.UnsafeAs<ConcatAsyncEnumerableBase<TSource>>();
                first = true;
                return _second;
            }

            internal override AsyncEnumerator<TSource> GetNextEnumeratorAndDispose(ref ConcatAsyncEnumerableBase<TSource> nextRef, ref bool first)
            {
                if (first)
                {
                    first = false;
                    return _first;
                }
                nextRef = _next.UnsafeAs<ConcatAsyncEnumerableBase<TSource>>();
                first = true;
                var second = _second;
                Dispose();
                return second;
            }

            public override async AsyncEnumerableMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
            {
                // The enumerators were retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _first._target._cancelationToken = cancelationToken;
                _second._target._cancelationToken = cancelationToken;
                try
                {
                    while (await _first.MoveNextAsync())
                    {
                        await writer.YieldAsync(_first.Current);
                    }
                    while (await _second.MoveNextAsync())
                    {
                        await writer.YieldAsync(_second.Current);
                    }

                    var next = _next.UnsafeAs<ConcatAsyncEnumerableBase<TSource>>();
                    bool first = true;
                    while (next != null)
                    {
                        var nextEnumerator = next.GetNextEnumerator(ref next, ref first);
                        // The enumerators were retrieved without a cancelation token when the original function was called.
                        // We need to propagate the token that was passed in, so we assign it before starting iteration.
                        nextEnumerator._target._cancelationToken = cancelationToken;
                        while (await nextEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(nextEnumerator.Current);
                        }
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    var firstEnumerator = _first;
                    var nextEnumerator = _second;
                    var next = _next.UnsafeAs<ConcatAsyncEnumerableBase<TSource>>();
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
                            // We can't do a try/finally loop to dispose all of the ConcatN enumerators,
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
        } // class Concat2AsyncEnumerable<TSource>

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class ConcatNAsyncEnumerable<TSource> : ConcatAsyncEnumerableBase<TSource>
        {
            internal AsyncEnumerator<TSource> _nextEnumerator;

            private ConcatNAsyncEnumerable() { }

            [MethodImpl(InlineOption)]
            private static ConcatNAsyncEnumerable<TSource> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ConcatNAsyncEnumerable<TSource>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new ConcatNAsyncEnumerable<TSource>()
                    : obj.UnsafeAs<ConcatNAsyncEnumerable<TSource>>();
            }

            [MethodImpl(InlineOption)]
            internal static ConcatNAsyncEnumerable<TSource> GetOrCreate(HandleablePromiseBase head, AsyncEnumerator<TSource> next)
            {
                var instance = GetOrCreate();
                instance._next = head;
                instance._nextEnumerator = next;
                return instance;
            }

            private void Dispose()
            {
                _nextEnumerator = default;
                ObjectPool.MaybeRepool(this);
            }

            internal override AsyncEnumerator<TSource> GetNextEnumerator(ref ConcatAsyncEnumerableBase<TSource> nextRef, ref bool first)
            {
                nextRef = _next.UnsafeAs<ConcatAsyncEnumerableBase<TSource>>();
                return _nextEnumerator;
            }

            internal override AsyncEnumerator<TSource> GetNextEnumeratorAndDispose(ref ConcatAsyncEnumerableBase<TSource> nextRef, ref bool first)
            {
                nextRef = _next.UnsafeAs<ConcatAsyncEnumerableBase<TSource>>();
                var next = _nextEnumerator;
                Dispose();
                return next;
            }

            public override async AsyncEnumerableMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
            {
                // The enumerators were retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                _nextEnumerator._target._cancelationToken = cancelationToken;
                try
                {
                    while (await _nextEnumerator.MoveNextAsync())
                    {
                        await writer.YieldAsync(_nextEnumerator.Current);
                    }

                    var next = _next.UnsafeAs<ConcatAsyncEnumerableBase<TSource>>();
                    bool first = true;
                    while (next != null)
                    {
                        var nextEnumerator = next.GetNextEnumerator(ref next, ref first);
                        // The enumerators were retrieved without a cancelation token when the original function was called.
                        // We need to propagate the token that was passed in, so we assign it before starting iteration.
                        nextEnumerator._target._cancelationToken = cancelationToken;
                        while (await nextEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(nextEnumerator.Current);
                        }
                    }

                    // We yield and wait for the enumerator to be disposed, but only if there were no exceptions.
                    await writer.YieldAsync(default).ForLinqExtension();
                }
                finally
                {
                    var nextEnumerator = _nextEnumerator;
                    var next = _next.UnsafeAs<ConcatAsyncEnumerableBase<TSource>>();
                    Dispose();
                    try
                    {
                        await nextEnumerator.DisposeAsync();
                    }
                    finally
                    {
                        // We can't do a try/finally loop to dispose all of the ConcatN enumerators,
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
        } // class ConcatNAsyncEnumerable<TSource>
    } // class Internal
#endif
} // namespace Proto.Promises