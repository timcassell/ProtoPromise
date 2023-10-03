#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
#if NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
    partial class Internal
    {
        internal interface IAsyncIterator<T>
        {
            AsyncEnumerableMethod Start(AsyncStreamWriter<T> streamWriter, CancelationToken cancelationToken);
            bool IsNull { get; }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncIterator<T> : IAsyncIterator<T>
        {
            private readonly Func<AsyncStreamWriter<T>, CancelationToken, AsyncEnumerableMethod> _func;

            public bool IsNull
            {
                [MethodImpl(InlineOption)]
                get { return _func == null; }
            }

            [MethodImpl(InlineOption)]
            internal AsyncIterator(Func<AsyncStreamWriter<T>, CancelationToken, AsyncEnumerableMethod> func)
                => _func = func;

            [MethodImpl(InlineOption)]
            public AsyncEnumerableMethod Start(AsyncStreamWriter<T> streamWriter, CancelationToken cancelationToken)
                => _func.Invoke(streamWriter, cancelationToken);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncIterator<T, TCapture> : IAsyncIterator<T>
        {
            private readonly TCapture _capturedValue;
            private readonly Func<TCapture, AsyncStreamWriter<T>, CancelationToken, AsyncEnumerableMethod> _func;

            public bool IsNull
            {
                [MethodImpl(InlineOption)]
                get { return _func == null; }
            }

            [MethodImpl(InlineOption)]
            internal AsyncIterator(TCapture captureValue, Func<TCapture, AsyncStreamWriter<T>, CancelationToken, AsyncEnumerableMethod> func)
            {
                _capturedValue = captureValue;
                _func = func;
            }

            [MethodImpl(InlineOption)]
            public AsyncEnumerableMethod Start(AsyncStreamWriter<T> streamWriter, CancelationToken cancelationToken)
                => _func.Invoke(_capturedValue, streamWriter, cancelationToken);
        }

        partial class PromiseRefBase
        {
            internal virtual void GetResultForAsyncStreamYielder(short promiseId, int enumerableId) => throw new System.InvalidOperationException();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class AsyncEnumerableBase<T> : PromiseRefBase.PromiseSingleAwait<bool>
        {
            // This is used as the backing reference to 3 different awaiters. MoveNextAsync (Promise<bool>), DisposeAsync (Promise), and YieldAsync (AsyncStreamYielder).
            // We use `Interlocked.CompareExchange(ref _enumerableId` to enforce only 1 awaiter uses it at a time, in the correct order.

            private T _current;
            private int _iteratorCompleteExpectedId;
            private int _iteratorCompleteId;
            protected int _enumerableId = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.
            protected CancelationToken _cancelationToken;

            internal int EnumerableId
            {
                [MethodImpl(InlineOption)]
                get { return _enumerableId; }
            }

            internal Linq.AsyncEnumerator<T> GetAsyncEnumerator(int id, CancelationToken cancelationToken)
            {
                int newId = id + 1;
                if (Interlocked.CompareExchange(ref _enumerableId, newId, id) != id)
                {
                    throw new InvalidOperationException("AsyncEnumerable.GetAsyncEnumerator: instance is not valid. AsyncEnumerable may only be used once.", GetFormattedStacktrace(2));
                }
                _cancelationToken = cancelationToken;
                return new Linq.AsyncEnumerator<T>(this, newId);
            }

            [MethodImpl(InlineOption)]
            internal T GetCurrent(int id)
            {
                if (_enumerableId != id)
                {
                    throw new InvalidOperationException("AsyncEnumerable.GetCurrent: instance is not valid, or the MoveNextAsync operation is still pending.", GetFormattedStacktrace(2));
                }
                return _current;
            }

            [MethodImpl(InlineOption)]
            internal Promise<bool> MoveNextAsync(int id)
            {
                // We increment by 1 when MoveNextAsync, then decrement by 1 when YieldAsync.
                int newId = id + 1;
                if (Interlocked.CompareExchange(ref _enumerableId, newId, id) != id)
                {
                    throw new InvalidOperationException("AsyncEnumerable.MoveNextAsync: instance is not valid, or the previous MoveNextAsync operation is still pending.", GetFormattedStacktrace(2));
                }
                ThrowIfInPool(this);
                _current = default;
                _iteratorCompleteExpectedId = newId;
                _iteratorCompleteId = newId - 1;
                _result = false;
                StartOrMoveNext(newId);
                return new Promise<bool>(this, Id, 0);
            }

            [MethodImpl(InlineOption)]
            internal AsyncStreamYielder YieldAsync(in T value, int id)
            {
                int newId = id - 1;
                if (Interlocked.CompareExchange(ref _enumerableId, newId, id) != id)
                {
                    throw new InvalidOperationException("AsyncStreamWriter.YieldAsync: instance is not valid. This must only be called from the iterator method.", GetFormattedStacktrace(2));
                }
                ThrowIfInPool(this);
                _current = value;
                // Complete the MoveNextAsync promise.
                _result = true;
                HandleNextInternal(null, Promise.State.Resolved);
                return new AsyncStreamYielder(this, newId);
            }

            [MethodImpl(InlineOption)]
            internal Promise DisposeAsync(int id)
            {
                int newId = id + 2;
                int oldId = Interlocked.CompareExchange(ref _enumerableId, newId, id);
                if (oldId != id)
                {
                    if (oldId == id + 1)
                    {
                        throw new InvalidOperationException("AsyncEnumerable.DisposeAsync: the previous MoveNextAsync operation is still pending.", GetFormattedStacktrace(2));
                    }
                    // IAsyncEnumerable.DisposeAsync must not throw if it's called multiple times, according to MSDN documentation.
                    return Promise.Resolved();
                }
                ThrowIfInPool(this);
                _current = default;
                if (State != Promise.State.Pending)
                {
                    // If the async iterator function is not already complete, we move the async state machine forward.
                    // Once that happens, GetResultForAsyncStreamYielder will be called which throws the special exception.
                    // If DisposeAsync was called before MoveNextAsync (the async iterator function never started), this just sets the promise to complete.
                    _iteratorCompleteExpectedId = newId;
                    _iteratorCompleteId = newId;
                    MoveNext();
                }
                return new Promise(this, Id, 0);
            }

            internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state)
            {
                // This is called when the async iterator function completes.
                ThrowIfInPool(this);
                handler.SetCompletionState(rejectContainer, state);
                if (Interlocked.CompareExchange(ref _enumerableId, _iteratorCompleteId, _iteratorCompleteExpectedId) != _iteratorCompleteExpectedId)
                {
                    handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
                    rejectContainer = CreateRejectContainer(new InvalidOperationException("AsyncEnumerable.Create iterator function completed invalidly. Did you YieldAsync without await?"), int.MinValue, null, this);
                    state = Promise.State.Rejected;
                }
                else
                {
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                }
                HandleNextInternal(rejectContainer, state);
            }

            internal override void GetResultForAsyncStreamYielder(short promiseId, int enumerableId)
            {
                int pId = Id;
                int enumId = _enumerableId;
                if (promiseId != pId | enumerableId != enumId)
                {
                    if (promiseId == pId & enumId == enumerableId + 2)
                    {
                        // DisposeAsync was called early (before the async iterator function completed).
                        // Throw this special exception so that the async iterator function will run any finally blocks and complete.
                        throw AsyncEnumerableDisposedException.s_instance;
                    }
                    throw new InvalidOperationException("AsyncStreamYielder.GetResult: instance is not valid. This should only be called from the iterator method, and it may only be called once.", GetFormattedStacktrace(2));
                }
            }

            protected void MoveNext()
            {
                // Reset so that the next yield will work.
                Dispose();
                ResetWithoutStacktrace();
                // Handle next to move the async state machine forward.
                HandleNextInternal(null, Promise.State.Resolved);
            }

            protected abstract void StartOrMoveNext(int enumerableId);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncEnumerableImpl<TValue, TIterator> : AsyncEnumerableBase<TValue>
            where TIterator : IAsyncIterator<TValue>
        {
            private TIterator _iterator;

            private AsyncEnumerableImpl() { }

            [MethodImpl(InlineOption)]
            private static AsyncEnumerableImpl<TValue, TIterator> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncEnumerableImpl<TValue, TIterator>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncEnumerableImpl<TValue, TIterator>()
                    : obj.UnsafeAs<AsyncEnumerableImpl<TValue, TIterator>>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncEnumerableImpl<TValue, TIterator> GetOrCreate(in TIterator iterator)
            {
                var enumerable = GetOrCreate();
                enumerable.Reset();
                enumerable._iterator = iterator;
                return enumerable;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                _cancelationToken = default;
                ObjectPool.MaybeRepool(this);
            }

            protected override void StartOrMoveNext(int enumerableId)
            {
                if (_iterator.IsNull)
                {
                    MoveNext();
                    return;
                }

                var iterator = _iterator;
                _iterator = default;
                var iteratorPromise = iterator.Start(new AsyncStreamWriter<TValue>(this, enumerableId), _cancelationToken)._promise;
                if (iteratorPromise._ref == null)
                {
                    // Already complete.
                    HandleNextInternal(null, Promise.State.Resolved);
                }
                else
                {
                    // We only set _previous to support circular await detection.
                    // We don't set _rejectContainerOrPreviousOrLink to prevent progress subscriptions from going down the chain, because progress is meaningless for AsyncEnumerable.
#if PROMISE_DEBUG
                    _previous = iteratorPromise._ref;
#endif
                    // We hook this up directly to the returned promise so we can know when the iteration is complete, and use this for the DisposeAsync promise.
                    iteratorPromise._ref.HookupExistingWaiter(iteratorPromise._id, this);
                }
            }
        }
    }
#endif
}