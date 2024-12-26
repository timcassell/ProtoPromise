#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using Proto.Promises.Collections;
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

                // These must not be readonly.
                private CancelationHelper _cancelationHelper;
                private PoolBackedDeque<TResult> _queue;
                private int _remaining;
                private bool _isMoveNextAsyncPending;

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
                    _cancelationHelper.Reset();
                    _queue = new PoolBackedDeque<TResult>(0);
                }

                [MethodImpl(InlineOption)]
                internal void AddResult(in TResult result)
                {
                    ++_remaining;
                    lock (this)
                    {
                        _queue.EnqueueTail(result);
                    }
                }

                internal void AddPromise(PromiseRefBase promise, short id)
                {
                    AddPending(promise);

                    ++_remaining;
                    _cancelationHelper.Retain();
                    promise.HookupNewWaiter(id, this);
                }

                new private void Dispose()
                {
                    ValidateNoPending();

                    // MoveNextAsync/DisposeAsync may have completed synchronously, or not called at all.
                    PrepareEarlyDispose();
                    base.Dispose();
                    _disposed = true;
                    ClearReferences(ref _current);
                    _cancelationHelper = default;
                    _queue.Dispose();
                    _queue = default;
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
                    int releaseCount = _cancelationHelper.TrySetCompleted() ? -2 : -1;
                    _cancelationHelper.UnregisterAndWait();
                    if (_cancelationHelper.TryRelease(releaseCount))
                    {
                        Dispose();
                    }
                    return Promise.Resolved();
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
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
                        _cancelationHelper.RegisterWithoutImmediateInvoke(_cancelationToken, this, out bool alreadyCanceled);
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
                    else if (_cancelationHelper.IsCompleted)
                    {
                        _enumerableId = id;
                        return Promise<bool>.Canceled();
                    }
                    --_remaining;
                    
                    // Reset this before entering the lock so that we're not spending extra time inside the lock.
                    ResetForNextAwait();
                    lock (this)
                    {
                        if (_isMoveNextAsyncPending = _queue.IsEmpty)
                        {
                            return new Promise<bool>(this, Id);
                        }
                        _current = _queue.DequeueHead();
                    }
                    _enumerableId = id;
                    return Promise.Resolved(true);
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
                        if (_isMoveNextAsyncPending)
                        {
                            // MoveNextAsync is pending, so we can skip the queue and just set the current directly.
                            _isMoveNextAsyncPending = false;
                            goto HandleNext;
                        }
                        _queue.EnqueueTail(result);
                    }
                    MaybeDispose();
                    return;

                HandleNext:
                    --_enumerableId;
                    _result = true;
                    _current = result;
                    HandleNextInternal(Promise.State.Resolved);
                }

                void ICancelable.Cancel()
                {
                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        return;
                    }

                    lock (this)
                    {
                        if (_isMoveNextAsyncPending)
                        {
                            _isMoveNextAsyncPending = false;
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