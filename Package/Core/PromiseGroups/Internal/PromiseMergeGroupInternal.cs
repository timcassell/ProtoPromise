#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0028 // Collection initialization can be simplified
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0306 // Collection initialization can be simplified

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class HandleablePromiseBase
        {
            internal virtual void Handle(PromiseRefBase.PromisePassThroughForMergeGroup passthrough, PromiseRefBase handler, Promise.State state) => throw new System.InvalidOperationException();
        }

        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromisePassThroughForMergeGroup : PromisePassThrough, ILinked<PromisePassThroughForMergeGroup>
            {
                PromisePassThroughForMergeGroup ILinked<PromisePassThroughForMergeGroup>.Next
                {
                    get => _next.UnsafeAs<PromisePassThroughForMergeGroup>();
                    set => _next = value;
                }

                internal PromiseRefBase Owner
                {
                    [MethodImpl(InlineOption)]
                    get => _owner;
                }

                internal int Index
                {
                    [MethodImpl(InlineOption)]
                    get => _index;
                }

                private PromisePassThroughForMergeGroup() { }

                [MethodImpl(InlineOption)]
                private static PromisePassThroughForMergeGroup GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromisePassThroughForMergeGroup>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromisePassThroughForMergeGroup()
                        : obj.UnsafeAs<PromisePassThroughForMergeGroup>();
                }

                [MethodImpl(InlineOption)]
                new internal static PromisePassThroughForMergeGroup GetOrCreate(PromiseRefBase owner, PromiseRefBase target, int index)
                {
                    var passThrough = GetOrCreate();
                    passThrough._next = target;
                    passThrough._index = index;
                    passThrough._owner = owner;
                    return passThrough;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                    // Unlike regular PromisePassThrough, we don't dispose here. This gets stored in the merge group promise,
                    // so that the ultimate ValueTuple result will be filled with the proper type, and will be disposed when it's complete.
                    => _next.Handle(this, handler, state);

                internal void Dispose()
                {
                    ThrowIfInPool(this);
                    _owner.MaybeDispose();
                    _owner = null;
                    ObjectPool.MaybeRepool(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class MergePromiseGroupBase<TResult> : PromiseGroupBase<TResult>
            {
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class MergePromiseGroupVoid : MergePromiseGroupBase<VoidResult>
            {
                [MethodImpl(InlineOption)]
                private static MergePromiseGroupVoid GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseGroupVoid>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseGroupVoid()
                        : obj.UnsafeAs<MergePromiseGroupVoid>();
                }

                [MethodImpl(InlineOption)]
                internal static MergePromiseGroupVoid GetOrCreate(CancelationRef cancelationSource)
                {
                    var promise = GetOrCreate();
                    promise.Reset(cancelationSource);
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    // This is called from void promises. They don't need to update any value, so they have no index.
                    RemovePromiseAndSetCompletionState(handler, state);
                    if (state != Promise.State.Resolved)
                    {
                        CancelGroup();
                        if (state == Promise.State.Rejected)
                        {
                            RecordException(handler.RejectContainer.GetValueAsException());
                        }
                    }
                    handler.MaybeDispose();
                    if (TryComplete())
                    {
                        // All promises are complete.
                        HandleNextInternal(CompleteAndGetState());
                    }
                }

                private Promise.State CompleteAndGetState()
                {
                    if (_exceptions == null)
                    {
                        return _cancelationRef.IsCanceledUnsafe() ? Promise.State.Canceled : Promise.State.Resolved;
                    }

                    RejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                    _exceptions = null;
                    return Promise.State.Rejected;
                }

                internal override void Handle(PromisePassThroughForMergeGroup passthrough, PromiseRefBase handler, Promise.State state)
                {
                    // We store the passthrough until all promises are complete,
                    // so that the ultimate ValueTuple will be filled with the proper types.
                    // We don't handle the rejection here, it is handled in the attached promise.
                    RemovePromiseAndSetCompletionState(handler, state);
                    _completedPassThroughs.PushInterlocked(passthrough);
                    if (state != Promise.State.Resolved)
                    {
                        CancelGroup();
                    }
                    if (TryComplete())
                    {
                        // All promises are complete.
                        // We just pass the state here and don't do anything about the exceptions,
                        // because the attached promise will handle the actual completion logic.
                        HandleNextInternal(_cancelationRef.IsCanceledUnsafe() ? Promise.State.Canceled : Promise.State.Resolved);
                    }
                }

                internal void MarkReady(uint totalPromises)
                {
                    // This method is called after all promises have been hooked up to this.
                    if (MarkReadyAndGetIsComplete(unchecked((int) totalPromises)))
                    {
                        // All promises already completed.
                        _next = PromiseCompletionSentinel.s_instance;
                        SetCompletionState(CompleteAndGetState());
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class MergePromiseGroup<TResult> : SingleAwaitPromise<TResult>
            {
                private static GetResultDelegate<TResult> s_getResult;

                [MethodImpl(InlineOption)]
                private static MergePromiseGroup<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseGroup<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseGroup<TResult>()
                        : obj.UnsafeAs<MergePromiseGroup<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> New(MergePromiseGroupVoid group, in TResult value, GetResultDelegate<TResult> getResultFunc, bool isExtended, bool isFinal,
                    ValueLinkedStack<MergeCleanupCallback> cleanupCallbacks, int cleanupCount)
                {
                    s_getResult = getResultFunc;
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    promise._isExtended = isExtended;
                    promise._isFinal = isFinal;
                    promise._cleanupCallbacks = cleanupCallbacks;
                    promise._cleanupCount = cleanupCount;
                    promise._isCleaning = false;
                    promise.SetPrevious(group);
                    group.HookupNewWaiter(group.Id, promise);
                    return new Promise<TResult>(promise);
                }

                internal override void MaybeDispose()
                {
                    ValidateNoPending();
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    handler.SetCompletionState(state);

                    if (_isCleaning)
                    {
                        HandleCleanupPromise(handler, state);
                        return;
                    }
                    
                    // The cleanup stack is possibly shared between multiple extended merge groups.
                    // Only prepare the count of cleanups for this promise, not the entire stack.
                    var cleanupCallback = _cleanupCallbacks.Peek();
                    for (var i = _cleanupCount; i > 0; --i)
                    {
                        cleanupCallback.Prepare();
                        cleanupCallback = cleanupCallback._next.UnsafeAs<MergeCleanupCallback>();
                    }

                    var group = handler.UnsafeAs<MergePromiseGroupVoid>();
                    var passthroughs = group._completedPassThroughs.TakeAndClear();
                    while (passthroughs.IsNotEmpty)
                    {
                        var passthrough = passthroughs.Pop();
                        var owner = passthrough.Owner;
                        var index = passthrough.Index;
                        s_getResult.Invoke(owner, index, ref _result);
                        if (owner.State == Promise.State.Rejected)
                        {
                            RecordRejection(group, owner, index);
                        }
                        passthrough.Dispose();
                    }

                    if (group._exceptions != null)
                    {
                        state = Promise.State.Rejected;
                        RejectContainer = CreateRejectContainer(new AggregateException(group._exceptions), int.MinValue, null, this);
                        group._exceptions = null;
                    }
                    else
                    {
                        // The group may have been completed by a void promise, in which case it already converted its exceptions to a reject container.
                        RejectContainer = handler.RejectContainer;
                    }
                    group.MaybeDispose();

                    if (_isFinal & _cleanupCallbacks.IsNotEmpty)
                    {
                        Cleanup(state);
                    }
                    else
                    {
                        HandleNextInternal(state);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private void RecordRejection(MergePromiseGroupVoid group, PromiseRefBase owner, int index)
                {
                    var exception = owner.RejectContainer.GetValueAsException();
                    if (_isExtended & index == 0)
                    {
                        // If this is an extended merge group, we need to extract the inner exceptions of the first cluster to make a flattened AggregateException.
                        // We only do this for 1 layer instead of using the Flatten method of the AggregateException, because we only want to flatten the exceptions from the group,
                        // and not any nested AggregateExceptions from the actual async work.
                        foreach (var ex in exception.UnsafeAs<AggregateException>().InnerExceptions)
                        {
                            group.RecordException(ex);
                        }
                    }
                    else
                    {
                        group.RecordException(exception);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private void HandleCleanupPromise(PromiseRefBase handler, Promise.State state)
                {
                    RemoveForCircularAwaitDetection(handler);
                    if (state == Promise.State.Rejected)
                    {
                        RecordCleanupException(handler.RejectContainer.GetValueAsException());
                    }
                    handler.MaybeDispose();

                    MaybeHandleNextAfterCleanup();
                }

                private void RecordCleanupException(Exception exception)
                {
                    lock (this)
                    {
                        // Extract the previous exceptions to append the new exception if another promise hasn't already done so.
                        if (RejectContainer != null)
                        {
                            _exceptions = new List<Exception>(RejectContainer.GetValueAsException().UnsafeAs<AggregateException>().InnerExceptions);
                            RejectContainer = null;
                        }
                        RecordException(exception, ref _exceptions);
                    }
                }

                private void Cleanup(Promise.State state)
                {
                    var cleanupCallbacks = _cleanupCallbacks.TakeAndClear();
                    if (state == Promise.State.Resolved)
                    {
                        do
                        {
                            cleanupCallbacks.Pop().Dispose();
                        } while (cleanupCallbacks.IsNotEmpty);

                        HandleNextInternal(Promise.State.Resolved);
                        return;
                    }

                    // We reuse the _cleanupCount as an interlocked counter to know when all cleanup promises are complete.
                    _cleanupCount = 1;
                    _isCleaning = true;
                    do
                    {
                        var cleanupPromise = cleanupCallbacks.Pop().InvokeAndDispose();
                        if (cleanupPromise._ref == null)
                        {
                            continue;
                        }
                        try
                        {
                            Interlocked.Increment(ref _cleanupCount);
                            ValidateReturn(cleanupPromise);
                            AddForCircularAwaitDetection(cleanupPromise._ref);
                            cleanupPromise._ref.HookupExistingWaiter(cleanupPromise._id, this);
                        }
                        catch (Exception e)
                        {
                            Interlocked.Decrement(ref _cleanupCount);
                            RemoveForCircularAwaitDetection(cleanupPromise._ref);
                            RecordCleanupException(e is InvalidReturnException ? e : new InvalidReturnException("onCleanup returned an invalid promise.", string.Empty));
                        }
                    } while (cleanupCallbacks.IsNotEmpty);

                    MaybeHandleNextAfterCleanup();
                }

                private void MaybeHandleNextAfterCleanup()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _cleanupCount, -1) != 0)
                    {
                        return;
                    }

                    if (_exceptions != null)
                    {
                        RejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                        _exceptions = null;
                    }
                    HandleNextInternal(RejectContainer == null ? Promise.State.Canceled : Promise.State.Rejected);
                }

                partial void AddForCircularAwaitDetection(PromiseRefBase pendingPromise);
                partial void RemoveForCircularAwaitDetection(PromiseRefBase completePromise);
                partial void ValidateNoPending();
            } // class MergePromiseGroup<TResult>

#if PROMISE_DEBUG
            partial class MergePromiseGroup<TResult>
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
                            throw new System.InvalidOperationException("MergePromiseGroup disposed with pending promises.");
                        }
                    }
                }

                partial void AddForCircularAwaitDetection(PromiseRefBase pendingPromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Add(pendingPromise);
                    }
                }

                partial void RemoveForCircularAwaitDetection(PromiseRefBase completePromise)
                {
                    lock (_pendingPromises)
                    {
                        _pendingPromises.Remove(completePromise);
                    }
                }
            }
#endif
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.MergePromiseGroupVoid GetOrCreateMergePromiseGroupVoid(CancelationRef cancelationSource)
            => PromiseRefBase.MergePromiseGroupVoid.GetOrCreate(cancelationSource);

        [MethodImpl(InlineOption)]
        internal static Promise<TResult> NewMergePromiseGroup<TResult>(
            PromiseRefBase.MergePromiseGroupVoid group, in TResult value, GetResultDelegate<TResult> getResultFunc, bool isExtended, bool isFinal, ValueLinkedStack<MergeCleanupCallback> cleanupCallbacks, int cleanupCount)
            => PromiseRefBase.MergePromiseGroup<TResult>.New(group, value, getResultFunc, isExtended, isFinal, cleanupCallbacks, cleanupCount);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidMergeGroup(int skipFrames)
            => throw new InvalidOperationException("The promise merge group is invalid.", GetFormattedStacktrace(skipFrames + 1));
    } // class Internal
}