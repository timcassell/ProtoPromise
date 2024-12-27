#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using System;
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
            internal sealed partial class EachPromiseGroup<TResult> : AsyncEnumerableBase<TResult>, ICancelable
                where TResult : IResultContainer
            {
                private static GetResultDelegate<TResult> s_getResult;

                private CancelationRef _cancelationRef; // Store the reference directly instead of CancelationSource struct to reduce memory.
                private Exception _cancelationException; // In case a cancelation token callback throws, we have to store it to rethrow it from DisposeAsync.
                // These must not be readonly.
                private CancelationHelper _cancelationHelper;
                private PoolBackedDeque<TResult> _queue;
                private int _remaining;
                private bool _isMoveNextAsyncPending;
                private bool _suppressUnobservedRejections;

                private EachPromiseGroup() { }

                [MethodImpl(InlineOption)]
                private static EachPromiseGroup<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<EachPromiseGroup<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new EachPromiseGroup<TResult>()
                        : obj.UnsafeAs<EachPromiseGroup<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static EachPromiseGroup<TResult> GetOrCreate(CancelationRef cancelationRef, GetResultDelegate<TResult> getResultDelegate)
                {
                    s_getResult = getResultDelegate;

                    var enumerable = GetOrCreate();
                    enumerable.Reset();
                    enumerable._queue = new PoolBackedDeque<TResult>(0);
                    enumerable._cancelationRef = cancelationRef;
                    enumerable._cancelationHelper.Reset(0);
                    return enumerable;
                }

                [MethodImpl(InlineOption)]
                internal bool TryIncrementId(int id)
                    => Interlocked.CompareExchange(ref _enumerableId, unchecked(id + 1), id) == id;

                [MethodImpl(InlineOption)]
                internal void AddResult(in TResult result)
                {
                    lock (this)
                    {
                        _queue.EnqueueTail(result);
                    }
                }

                internal void AddPromise(PromiseRefBase promise, short id)
                {
                    AddPending(promise);
                    promise.HookupNewWaiter(id, this);
                }

                internal void MarkReady(bool suppressUnobservedRejections, int pendingCount, int totalCount)
                {
                    // This method is called after all promises have been hooked up to this.
                    _suppressUnobservedRejections = suppressUnobservedRejections;
                    _remaining = totalCount;
                    // The retain counter starts at 0 and is decremented every time an added promise completes.
                    // We add back the number of pending promises that were added, plus 1 extra retain for DisposeAsync,
                    // and when the count goes back to 0, this is complete.
                    _cancelationHelper.RetainUnchecked(unchecked(pendingCount + 1));
                }

                private void CancelGroup()
                {
                    try
                    {
                        _cancelationRef.CancelUnsafe();
                    }
                    catch (Exception e)
                    {
                        _cancelationException = e;
                    }
                }

                new private void Dispose()
                {
                    ValidateNoPending();

                    base.Dispose();
                    ClearReferences(ref _current);
                    _cancelationException = null;
                    _cancelationRef.DisposeUnsafe();
                    _cancelationRef = null;
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

                    _disposed = true;
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        CancelGroup();
                    }
                    _cancelationHelper.UnregisterAndWait();

                    // We have to reset this before decrementing _retainCount, or else MaybeHandleDisposeAsync could be called on another thread,
                    // causing the async state machine to be moved forward too early.
                    ResetForNextAwait();

                    if (!_cancelationHelper.TryRelease())
                    {
                        return new Promise(this, Id);
                    }

                    var exception = GetAggregateException();

                    // MoveNextAsync may have completed synchronously, or not called at all.
                    PrepareEarlyDispose();
                    Dispose();
                    return exception == null
                        ? Promise.Resolved()
                        : Promise.Rejected(exception);
                }

                private AggregateException GetAggregateException()
                {
                    // If a cancelation token callback threw, we always propagate it, regardless of the _suppressUnobservedRejections flag.
                    List<Exception> exceptions = _cancelationException == null
                        ? null
                        : new List<Exception>() { _cancelationException };
                    if (!_suppressUnobservedRejections)
                    {
                        while (_queue.IsNotEmpty)
                        {
                            var rejectContainer = _queue.DequeueHead().RejectContainer;
                            if (rejectContainer != null)
                            {
                                RecordException(rejectContainer.GetValueAsException(), ref exceptions);
                            }
                        }
                    }
                    return exceptions == null
                        ? null
                        : new AggregateException(exceptions);
                }

                internal override void MaybeDispose()
                {
                    // This is called on every MoveNextAsync, we only fully dispose and return to pool after DisposeAsync is called.
                    if (_disposed)
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
                    MaybeHandleDisposeAsync();
                    return;

                HandleNext:
                    --_enumerableId;
                    _result = true;
                    _current = result;
                    _cancelationHelper.TryReleaseUnchecked();
                    HandleNextInternal(Promise.State.Resolved);
                }

                private void MaybeHandleDisposeAsync()
                {
                    if (!_cancelationHelper.TryReleaseUnchecked())
                    {
                        return;
                    }

                    var exception = GetAggregateException();
                    if (exception == null)
                    {
                        HandleNextInternal(Promise.State.Resolved);
                        return;
                    }

                    RejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                    HandleNextInternal(Promise.State.Rejected);
                }

                void ICancelable.Cancel()
                {
                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        return;
                    }

                    CancelGroup();

                    lock (this)
                    {
                        if (!_isMoveNextAsyncPending)
                        {
                            return;
                        }
                        _isMoveNextAsyncPending = false;
                    }
                    --_enumerableId;
                    _result = false;
                    HandleNextInternal(Promise.State.Canceled);
                }

                partial void AddPending(PromiseRefBase pendingPromise);
                partial void RemoveComplete(PromiseRefBase completePromise);
                partial void ValidateNoPending();
            }

#if PROMISE_DEBUG
            partial class EachPromiseGroup<TResult>
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
        internal static PromiseRefBase.EachPromiseGroup<Promise<T>.ResultContainer> GetOrCreateEachPromiseGroup<T>(CancelationRef cancelationRef)
            => PromiseRefBase.EachPromiseGroup<Promise<T>.ResultContainer>.GetOrCreate(cancelationRef, Promise.MergeResultFuncs.GetMergeResult<T>());

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.EachPromiseGroup<Promise.ResultContainer> GetOrCreateEachPromiseGroup(CancelationRef cancelationRef)
            => PromiseRefBase.EachPromiseGroup<Promise.ResultContainer>.GetOrCreate(cancelationRef, Promise.MergeResultFuncs.GetMergeResultVoid());

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidEachGroup(int skipFrames)
            => throw new InvalidOperationException("The promise each group is invalid.", GetFormattedStacktrace(skipFrames + 1));
    } // class Internal
}