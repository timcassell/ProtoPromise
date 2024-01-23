#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
        internal interface IAsyncEnumerable<T>
        {
            AsyncEnumerator<T> GetAsyncEnumerator(int id, CancelationToken cancelationToken);
            bool GetIsValid(int id);
        }

        internal interface IAsyncIterator<T>
        {
            AsyncEnumerableMethod Start(AsyncStreamWriter<T> streamWriter, CancelationToken cancelationToken);
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
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract class AsyncEnumerableBase<T> : PromiseSingleAwait<bool>, IAsyncEnumerable<T>
            {
                internal CancelationToken _cancelationToken;
                protected T _current;
                protected int _enumerableId = 1; // Start with Id 1 instead of 0 to reduce risk of false positives.
                protected bool _disposed;
                protected bool _isStarted;

                internal int EnumerableId
                {
                    [MethodImpl(InlineOption)]
                    get { return _enumerableId; }
                }

                [MethodImpl(InlineOption)]
                new protected void Reset()
                {
                    base.Reset();
                    _disposed = false;
                    _isStarted = false;
                }

                [MethodImpl(InlineOption)]
                new protected void Dispose()
                {
                    base.Dispose();
                    _cancelationToken = default;
                }

                ~AsyncEnumerableBase()
                {
                    try
                    {
                        if (!_disposed)
                        {
                            string message = "An AsyncEnumerable's resources were garbage collected without it being disposed. You must call DisposeAsync on the AsyncEnumerator.";
                            ReportRejection(new UnreleasedObjectException(message), this);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ReportRejection(e, this);
                    }
                }

                public virtual AsyncEnumerator<T> GetAsyncEnumerator(int id, CancelationToken cancelationToken)
                {
                    int newId = id + 1;
                    if (Interlocked.CompareExchange(ref _enumerableId, newId, id) != id)
                    {
                        throw new InvalidOperationException("AsyncEnumerable.GetAsyncEnumerator: instance is not valid. AsyncEnumerable may only be used once.", GetFormattedStacktrace(2));
                    }
                    _cancelationToken = cancelationToken;
                    return new AsyncEnumerator<T>(this, newId);
                }

                public bool GetIsValid(int id) => id == _enumerableId;

                [MethodImpl(InlineOption)]
                internal T GetCurrent(int id)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (_enumerableId != id)
                    {
                        throw new InvalidOperationException("AsyncEnumerable.GetCurrent: instance is not valid, or the MoveNextAsync operation is still pending.", GetFormattedStacktrace(2));
                    }
#endif
                    return _current;
                }

                internal abstract Promise<bool> MoveNextAsync(int id);
                internal abstract Promise DisposeAsync(int id);
            } // class AsyncEnumerableBase<T>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract class AsyncEnumerableWithIterator<T> : AsyncEnumerableBase<T>
            {
                // This is used as the backing reference to 3 different awaiters. MoveNextAsync (Promise<bool>), DisposeAsync (Promise), and YieldAsync (AsyncStreamYielder<T>).
                // We use `Interlocked.CompareExchange(ref _enumerableId` to enforce only 1 awaiter uses it at a time, in the correct order.
                // We use a separate field for AsyncStreamYielder continuation, because using _next for 2 separate async functions (the iterator and the consumer) proves problematic.
                protected PromiseRefBase _iteratorPromiseRef;
                private int _iteratorCompleteExpectedId;
                private int _iteratorCompleteId;

                internal override Promise<bool> MoveNextAsync(int id)
                {
                    // We increment by 1 when MoveNextAsync, then decrement by 1 when YieldAsync.
                    int newId = id + 1;
                    // When the async iterator function completes, we set it to the id + 2 so we can detect that case.
                    int iteratorCompleteId = id + 2;
                    int oldId = Interlocked.CompareExchange(ref _enumerableId, newId, id);
                    if (oldId != id)
                    {
                        if (oldId == iteratorCompleteId)
                        {
                            // The async iterator function is already complete. Return a resolved promise with `false` result.
                            return Promise.Resolved(false);
                        }
                        throw new InvalidOperationException("AsyncEnumerable.MoveNextAsync: instance is not valid, or the previous MoveNextAsync operation is still pending.", GetFormattedStacktrace(2));
                    }
                    ThrowIfInPool(this);
                    _current = default;
                    _iteratorCompleteExpectedId = newId;
                    _iteratorCompleteId = iteratorCompleteId;
                    _result = false;
                    if (_isStarted)
                    {
                        MoveNext();
                    }
                    else
                    {
                        _isStarted = true;
                        Start(newId);
                    }
                    return new Promise<bool>(this, Id, 0);
                }

                private void MoveNext()
                {
                    // Invalidate the previous awaiter.
                    IncrementPromiseIdAndClearPrevious();
                    // Reset for the next awaiter.
                    ResetWithoutStacktrace();
                    // Handle iterator promise to move the async state machine forward.
                    InterlockedExchange(ref _iteratorPromiseRef, null).Handle(this, null, Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                internal AsyncStreamYielder<T> YieldAsync(in T value, int id)
                {
                    int newId = id - 1;
                    if (Interlocked.CompareExchange(ref _enumerableId, newId, id) != id)
                    {
                        throw new InvalidOperationException("AsyncStreamWriter.YieldAsync: instance is not valid. This must only be called from the iterator method, and not within any catch or finally blocks.", GetFormattedStacktrace(2));
                    }
                    ThrowIfInPool(this);
                    _current = value;
                    _iteratorCompleteExpectedId = newId;
                    // When the async iterator function completes, we set it to the original id + 2 so we can detect that case.
                    _iteratorCompleteId = newId + 2;
                    return new AsyncStreamYielder<T>(this, newId);
                }

                internal override Promise DisposeAsync(int id)
                {
                    int newId = id + 3;
                    // When the async iterator function completes before DisposeAsync is called, it's set to id + 2.
                    int iteratorCompleteId = id + 2;
                    // Common case is DisposeAsync is called after the async iterator function is complete.
                    int oldId = Interlocked.CompareExchange(ref _enumerableId, newId, iteratorCompleteId);
                    if (oldId == iteratorCompleteId)
                    {
                        // The async iterator function is already complete, dispose this and return a resolved promise.
                        _disposed = true;
                        DisposeAndReturnToPool();
                        return Promise.Resolved();
                    }

                    // Otherwise, DisposeAsync was likely called to stop iterating early (`break` keyword in `await foreach` loop).
                    // We do another CompareExchange in case it was an invalid call.
                    oldId = Interlocked.CompareExchange(ref _enumerableId, newId, id);
                    if (oldId != id)
                    {
                        if (oldId == id + 1)
                        {
                            throw new InvalidOperationException("AsyncEnumerable.DisposeAsync: the previous MoveNextAsync operation is still pending.", GetFormattedStacktrace(2));
                        }
                        // IAsyncDisposable.DisposeAsync must not throw if it's called multiple times, according to MSDN documentation.
                        return Promise.Resolved();
                    }

                    ThrowIfInPool(this);
                    _disposed = true;
                    var iteratorPromise = InterlockedExchange(ref _iteratorPromiseRef, null);
                    if (iteratorPromise == null)
                    {
                        // DisposeAsync was called before MoveNextAsync, the async iterator function never started.
                        // Dispose this and return a resolved promise.
                        State = Promise.State.Resolved;
                        // This is never used as a backing reference for Promises, so we need to suppress the UnobservedPromiseException from the base finalizer.
                        WasAwaitedOrForgotten = true;
                        DisposeAndReturnToPool();
                        return Promise.Resolved();
                    }

                    // The async iterator function is not already complete, we move the async state machine forward.
                    // Once that happens, GetResultForAsyncStreamYielder will be called which throws the special exception.
                    _current = default;
                    _iteratorCompleteExpectedId = newId;
                    _iteratorCompleteId = newId;
                    // Invalidate the previous awaiter.
                    IncrementPromiseIdAndClearPrevious();
                    // Reset for the next awaiter.
                    ResetWithoutStacktrace();
                    iteratorPromise.Handle(this, null, Promise.State.Resolved);
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

                protected void HandleFromSynchronouslyCompletedIterator()
                {
                    ThrowIfInPool(this);
                    object rejectContainer = null;
                    Promise.State state = Promise.State.Resolved;
                    if (Interlocked.CompareExchange(ref _enumerableId, _iteratorCompleteId, _iteratorCompleteExpectedId) != _iteratorCompleteExpectedId)
                    {
                        rejectContainer = CreateRejectContainer(new InvalidOperationException("AsyncEnumerable.Create iterator function completed invalidly. Did you YieldAsync without await?"), int.MinValue, null, this);
                        state = Promise.State.Rejected;
                    }
                    HandleNextInternal(rejectContainer, state);
                }

                internal void GetResultForAsyncStreamYielder(int enumerableId)
                {
                    int enumId = _enumerableId;
                    // We add 1 because MoveNextAsync is expected to be called before this.
                    if (enumId != enumerableId + 1)
                    {
                        // If it wasn't MoveNextAsync, then it should've been DisposeAsync.
                        if (enumId == enumerableId + 3)
                        {
                            // DisposeAsync was called early (before the async iterator function completed).
                            // Reset in case the async iterator function completes synchronously from Start.
                            ResetWithoutStacktrace();
                            // Throw this special exception so that the async iterator function will run any finally blocks and complete.
                            throw AsyncEnumerableDisposedException.s_instance;
                        }
                        throw new InvalidOperationException("AsyncStreamYielder.GetResult: instance is not valid. This should only be called from the iterator method, and it may only be called once.", GetFormattedStacktrace(2));
                    }
                    // Reset in case the async iterator function completes synchronously from Start.
                    ResetWithoutStacktrace();
                }

                [MethodImpl(InlineOption)]
                internal void AwaitOnCompletedForAsyncStreamYielder(PromiseRefBase asyncPromiseRef, int enumerableId, bool hasValue = true)
                {
                    if (_enumerableId != enumerableId || Interlocked.CompareExchange(ref _iteratorPromiseRef, asyncPromiseRef, null) != null)
                    {
                        throw new InvalidOperationException("AsyncStreamYielder: invalid await. Only one await is allowed.", GetFormattedStacktrace(2));
                    }
                    // Complete the MoveNextAsync promise.
                    _result = hasValue;
                    HandleNextInternal(null, Promise.State.Resolved);
                }

                protected abstract void Start(int enumerableId);
                protected abstract void DisposeAndReturnToPool();
            } // class AsyncEnumerableWithIterator<TValue>
        } // class PromiseRefBase

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncEnumerableCreate<TValue, TIterator> : PromiseRefBase.AsyncEnumerableWithIterator<TValue>
            where TIterator : IAsyncIterator<TValue>
        {
            private TIterator _iterator;

            private AsyncEnumerableCreate() { }

            [MethodImpl(InlineOption)]
            private static AsyncEnumerableCreate<TValue, TIterator> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncEnumerableCreate<TValue, TIterator>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncEnumerableCreate<TValue, TIterator>()
                    : obj.UnsafeAs<AsyncEnumerableCreate<TValue, TIterator>>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncEnumerableCreate<TValue, TIterator> GetOrCreate(in TIterator iterator)
            {
                var enumerable = GetOrCreate();
                enumerable.Reset();
                enumerable._iterator = iterator;
                return enumerable;
            }

            protected override void DisposeAndReturnToPool()
            {
                Dispose();
                _iterator = default;
                ObjectPool.MaybeRepool(this);
            }

            internal override void MaybeDispose()
            {
                // This is called on every MoveNextAsync, we only fully dispose and return to pool after DisposeAsync is called.
                if (_disposed)
                {
                    DisposeAndReturnToPool();
                }
            }

            protected override void Start(int enumerableId)
            {
                var iteratorPromise = _iterator.Start(new AsyncStreamWriter<TValue>(this, enumerableId), _cancelationToken)._promise;
                _iterator = default;
                if (iteratorPromise._ref == null)
                {
                    // Already complete. This can happen if no awaits occurred in the async iterator function.
                    HandleFromSynchronouslyCompletedIterator();
                    return;
                }

                // We only set _previous to support circular await detection.
                // We don't set _rejectContainerOrPreviousOrLink to prevent progress subscriptions from going down the chain, because progress is meaningless for AsyncEnumerable.
#if PROMISE_DEBUG
                _previous = iteratorPromise._ref;
#endif
                // We hook this up directly to the returned promise so we can know when the iteration is complete, and use this for the DisposeAsync promise.
                iteratorPromise._ref.HookupExistingWaiter(iteratorPromise._id, this);
            }
        } // class AsyncEnumerableCreate<TValue, TIterator>
    } // class Internal
#endif
} // namespace Proto.Promises