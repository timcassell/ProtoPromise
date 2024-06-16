#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseEachAsyncEnumerable<TResult> : AsyncEnumerableBase<TResult>, ICancelable
            {
                private static GetResultDelegate<TResult> s_getResult;

                private CancelationRegistration _cancelationRegistration;
                // This must not be readonly.
                private PoolBackedQueue<TResult> _queue;
                private int _remaining;
                private int _retainCount;
                private bool _isMoveNextAsyncWaiting;

                private PromiseEachAsyncEnumerable() { }

                [MethodImpl(InlineOption)]
                private static PromiseEachAsyncEnumerable<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseEachAsyncEnumerable<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseEachAsyncEnumerable<TResult>()
                        : obj.UnsafeAs<PromiseEachAsyncEnumerable<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseEachAsyncEnumerable<TResult> GetOrCreate(GetResultDelegate<TResult> getResultDelegate)
                {
                    s_getResult = getResultDelegate;

                    var enumerable = GetOrCreate();
                    enumerable.Reset();
                    return enumerable;
                }

                new private void Reset()
                {
                    base.Reset();
                    _remaining = 0;
                    // 1 retain for DisposeAsync, and 1 retain for cancelation registration.
                    _retainCount = 2;
                    _queue = new PoolBackedQueue<TResult>(0);
                }

                [MethodImpl(InlineOption)]
                internal void AddResult(in TResult result)
                {
                    ++_remaining;
                    lock (this)
                    {
                        _queue.Enqueue(result);
                    }
                }

                internal void AddPromise(PromiseRefBase promise, short id)
                {
                    AddPending(promise);

                    ++_remaining;
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCount, 1);
                    promise.HookupNewWaiter(id, this);
                }

                new private void Dispose()
                {
                    ValidateNoPending();

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    SetCompletionState(Promise.State.Resolved);
#endif
                    base.Dispose();
                    _disposed = true;
                    _current = default;
                    _queue.Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override Promise DisposeAsync(int id)
                {
                    int newId = id + 2;
                    int oldId = Interlocked.CompareExchange(ref _enumerableId, newId, id);
                    if (oldId != id)
                    {
                        if (oldId == id + 1)
                        {
                            throw new InvalidOperationException("AsyncEnumerator.DisposeAsync: the previous MoveNextAsync operation is still pending.", GetFormattedStacktrace(2));
                        }
                        // IAsyncDisposable.DisposeAsync must not throw if it's called multiple times, according to MSDN documentation.
                        return Promise.Resolved();
                    }

                    // Same as calling MaybeDispose twice, but without the extra interlocked and branch.
                    int releaseCount = TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & State != Promise.State.Canceled ? -2 : -1;
                    _cancelationRegistration = default;
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCount, releaseCount) == 0)
                    {
                        Dispose();
                    }
                    return Promise.Resolved();
                }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCount, -1) == 0)
                    {
                        Dispose();
                    }
                }

                internal override Promise<bool> MoveNextAsync(int id)
                {
                    if (Interlocked.CompareExchange(ref _enumerableId, id + 1, id) != id)
                    {
                        throw new InvalidOperationException("AsyncEnumerable.MoveNextAsync: instance is not valid, or the previous MoveNextAsync operation is still pending.", GetFormattedStacktrace(2));
                    }

                    if (!_isStarted)
                    {
                        _isStarted = true;
                        _cancelationToken.TryRegisterWithoutImmediateInvoke<ICancelable>(this, out _cancelationRegistration, out bool alreadyCanceled);
                        if (alreadyCanceled)
                        {
                            _enumerableId = id;
                            return Promise<bool>.Canceled();
                        }
                    }
                    else if (_remaining == 0)
                    {
                        _enumerableId = id;
                        return Promise.Resolved(false);
                    }
                    else if (_cancelationToken.IsCancelationRequested | State == Promise.State.Canceled)
                    {
                        _enumerableId = id;
                        return Promise<bool>.Canceled();
                    }
                    --_remaining;

                    lock (this)
                    {
                        _isMoveNextAsyncWaiting = !_queue.TryDequeue(out _current);
                    }

                    if (_isMoveNextAsyncWaiting)
                    {
                        return new Promise<bool>(this, Id);
                    }
                    else
                    {
                        _enumerableId = id;
                        return Promise.Resolved(true);
                    }
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    RemoveComplete(handler);

                    handler.SetCompletionState(state);
                    TResult result = default;
                    s_getResult.Invoke(handler, 0, ref result);
                    handler.MaybeDispose();

                    lock (this)
                    {
                        _queue.Enqueue(result);
                        if (_isMoveNextAsyncWaiting)
                        {
                            _isMoveNextAsyncWaiting = false;
                            goto HandleNext;
                        }
                    }
                    MaybeDispose();
                    return;

                HandleNext:
                    --_enumerableId;
                    _result = true;
                    HandleNextInternal(Promise.State.Resolved);
                }

                void ICancelable.Cancel()
                {
                    SetCompletionState(Promise.State.Canceled);

                    lock (this)
                    {
                        if (_isMoveNextAsyncWaiting)
                        {
                            _isMoveNextAsyncWaiting = false;
                            goto HandleNext;
                        }
                    }
                    MaybeDispose();
                    return;

                HandleNext:
                    --_enumerableId;
                    _result = false;
                    HandleNextInternal(Promise.State.Canceled);
                }

                partial void AddPending(PromiseRefBase pendingPromise);
                partial void RemoveComplete(PromiseRefBase completePromise);
                partial void ValidateNoPending();
            }

#if PROMISE_DEBUG
            partial class PromiseEachAsyncEnumerable<TResult>
            {
                private readonly HashSet<PromiseRefBase> _pendingPromises = new HashSet<PromiseRefBase>();

                protected override void BorrowPreviousPromises(Stack<PromiseRefBase> borrower)
                {
                    lock (_pendingPromises)
                    {
                        foreach (var promiseRef in _pendingPromises)
                        {
                            borrower.Push(promiseRef);
                        }
                    }
                }

                partial void ValidateNoPending()
                {
                    lock (_pendingPromises)
                    {
                        if (_pendingPromises.Count != 0)
                        {
                            throw new System.InvalidOperationException("PromiseEachAsyncEnumerable disposed with pending promises.");
                        }
                    }
                }

                partial void AddPending(PromiseRefBase pendingPromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Add(pendingPromise);
                    }
                }

                partial void RemoveComplete(PromiseRefBase completePromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Remove(completePromise);
                    }
                }
            }
#endif // PROMISE_DEBUG
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.PromiseEachAsyncEnumerable<Promise<T>.ResultContainer> GetOrCreatePromiseEachAsyncEnumerable<T>()
            => PromiseRefBase.PromiseEachAsyncEnumerable<Promise<T>.ResultContainer>.GetOrCreate(Promise.MergeResultFuncs.GetMergeResult<T>());

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.PromiseEachAsyncEnumerable<Promise.ResultContainer> GetOrCreatePromiseEachAsyncEnumerableVoid()
            => PromiseRefBase.PromiseEachAsyncEnumerable<Promise.ResultContainer>.GetOrCreate(Promise.MergeResultFuncs.GetMergeResultVoid());
    } // class Internal
}