#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0028 // Simplify collection initialization
#pragma warning disable IDE0090 // Use 'new(...)'

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
            partial class SingleAwaitPromise<TResult>
            {
                internal void RecordException(Exception e)
                {
                    lock (this)
                    {
                        var rejectContainer = RejectContainer;
                        if (rejectContainer == null)
                        {
                            RejectContainer = rejectContainer = new AggregateRejectionContainer(this);
                        }
                        rejectContainer.UnsafeAs<AggregateRejectionContainer>().Add(e);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class PromiseGroupBase<TResult> : SingleAwaitPromise<TResult>
            {
                partial void AddPending(PromiseRefBase pendingPromise);
                partial void RemoveComplete(PromiseRefBase completePromise);
                partial void ValidateNoPending();

                internal override void Handle(PromiseRefBase handler, Promise.State state) => throw new System.InvalidOperationException();

                [MethodImpl(InlineOption)]
                protected void Reset(CancelationRef cancelationRef)
                {
                    _cancelationRef = cancelationRef;
                    _cancelationId = cancelationRef.SourceId;
                    Reset();
                }

                new protected void Dispose()
                {
                    ValidateNoPending();
                    base.Dispose();
                    _cancelationRef.TryDispose(_cancelationId);
                    _cancelationRef = null;
                }

                [MethodImpl(InlineOption)]
                protected bool TryComplete()
                    // We don't do an overflow check here, because it starts at zero
                    // and a promise may complete and decrement it before MarkReady() is called.
                    => Interlocked.Decrement(ref _waitCount) == 0;

                [MethodImpl(InlineOption)]
                protected void RemovePromiseAndSetCompletionState(PromiseRefBase completePromise, Promise.State state)
                {
                    RemoveComplete(completePromise);
                    completePromise.SetCompletionState(state);
                }

                [MethodImpl(InlineOption)]
                internal void AddPromiseForMerge(Promise promise, int index)
                {
                    AddPending(promise._ref);
                    var passthrough = PromisePassThroughForMergeGroup.GetOrCreate(promise._ref, this, index);
                    promise._ref.HookupNewWaiter(promise._id, passthrough);
                }

                [MethodImpl(InlineOption)]
                internal void AddPromise(Promise promise)
                {
                    AddPending(promise._ref);
                    promise._ref.HookupNewWaiter(promise._id, this);
                }

                [MethodImpl(InlineOption)]
                internal bool TryIncrementId(short id)
                {
                    // Unfortunately Interlocked doesn't contain APIs for short prior to .Net 9, so this is not thread-safe.
                    // But it's just a light check which users shouldn't be mis-using anyway, so it's not a big deal.
                    // It's probably not worth adding conditional compilation to use Interlocked in .Net 9.
                    if (id != _promiseId)
                    {
                        return false;
                    }
                    IncrementPromiseId();
                    return true;
                }

                [MethodImpl(InlineOption)]
                protected bool MarkReadyAndGetIsComplete(int totalPromises)
                    // _waitCount starts at 0 and is decremented every time an added promise completes.
                    // We add back the number of promises that were added, and when the count goes back to 0, all promises are complete.
                    => Interlocked.Add(ref _waitCount, totalPromises) == 0;

                protected void CancelGroup()
                {
                    // This may be called multiple times. It's fine because it checks internally if it's already canceled.
                    try
                    {
                        _cancelationRef.CancelUnsafe();
                    }
                    catch (Exception e)
                    {
                        RecordException(e);
                    }
                }
            }

#if PROMISE_DEBUG
            partial class PromiseGroupBase<TResult>
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
                            throw new System.InvalidOperationException("PromiseGroupBase disposed with pending promises.");
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

                protected void AddForCircularAwaitDetection(PromiseRefBase pendingPromise)
                    => AddPending(pendingPromise);

                protected void RemoveForCircularAwaitDetection(PromiseRefBase pendingPromise)
                    => RemoveComplete(pendingPromise);
            }
#endif
        } // class PromiseRefBase
    } // class Internal
}