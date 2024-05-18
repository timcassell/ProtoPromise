#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
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
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    passThrough._disposed = false;
#endif
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
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
                    ObjectPool.MaybeRepool(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class PromiseGroupBase<TResult> : PromiseSingleAwait<TResult>
            {
                partial void AddPending(PromiseRefBase pendingPromise);
                partial void RemoveComplete(PromiseRefBase completePromise);
                partial void ValidateNoPending();

                internal override void Handle(PromiseRefBase handler, Promise.State state) { throw new System.InvalidOperationException(); }

                [MethodImpl(InlineOption)]
                protected void Reset(CancelationRef cancelationSource)
                {
                    _cancelationRef = cancelationSource;
                    _cancelationId = cancelationSource.SourceId;
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
                internal void AddPromiseWithIndex(PromiseRefBase promise, short id, int index)
                {
                    AddPending(promise);
                    var passthrough = PromisePassThrough.GetOrCreate(promise, this, index);
                    promise.HookupNewWaiter(id, passthrough);
                }

                [MethodImpl(InlineOption)]
                internal void AddPromiseForMerge(PromiseRefBase promise, short id, int index)
                {
                    AddPending(promise);
                    var passthrough = PromisePassThroughForMergeGroup.GetOrCreate(promise, this, index);
                    promise.HookupNewWaiter(id, passthrough);
                }

                [MethodImpl(InlineOption)]
                internal void AddPromise(PromiseRefBase promise, short id)
                {
                    AddPending(promise);
                    promise.HookupNewWaiter(id, this);
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

                [MethodImpl(InlineOption)]
                internal void MarkReady(uint totalPromises)
                    => MarkReady(unchecked((int) totalPromises));

                internal void MarkReady(int totalPromises)
                {
                    // This method is called after all promises have been hooked up to this.
                    if (MarkReadyAndGetIsComplete(totalPromises))
                    {
                        // All promises already completed.
                        _next = PromiseCompletionSentinel.s_instance;
                        if (_exceptions != null)
                        {
                            _rejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                            _exceptions = null;
                            SetCompletionState(Promise.State.Rejected);
                        }
                        else
                        {
                            SetCompletionState(_completeState);
                        }
                    }
                }

                protected void CancelGroup()
                {
                    // This may be called multiple times. It's fine because it checks internally if it's already canceled.
                    try
                    {
                        _cancelationRef.Cancel();
                    }
                    catch (Exception e)
                    {
                        RecordException(e);
                    }
                }

                internal void RecordException(Exception e)
                    => Internal.RecordException(e, ref _exceptions);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class MergePromiseGroupBase<TResult> : PromiseGroupBase<TResult>
            {
                [MethodImpl(InlineOption)]
                new protected void CancelGroup()
                {
                    _completeState = Promise.State.Canceled;
                    base.CancelGroup();
                }
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
                    promise._completeState = Promise.State.Resolved; // Default to Resolved state. If the promise is actually canceled or rejected, the state will be overwritten.
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
                            RecordException(handler._rejectContainer.GetValueAsException());
                        }
                    }
                    handler.MaybeDispose();
                    if (TryComplete())
                    {
                        // All promises are complete.
                        HandleCompletion();
                    }
                }

                private void HandleCompletion()
                {
                    if (_exceptions != null)
                    {
                        _rejectContainer = CreateRejectContainer(new AggregateException(_exceptions), int.MinValue, null, this);
                        _exceptions = null;
                        HandleNextInternal(Promise.State.Rejected);
                    }
                    else
                    {
                        HandleNextInternal(_completeState);
                    }
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
                        HandleNextInternal(_completeState);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class MergePromiseGroup<TResult> : PromiseSingleAwait<TResult>
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
                internal static MergePromiseGroup<TResult> GetOrCreate(in TResult value, GetResultDelegate<TResult> getResultFunc, bool isExtended)
                {
                    s_getResult = getResultFunc;
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    promise._isExtended = isExtended;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    handler.SetCompletionState(state);

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
                            var exception = owner._rejectContainer.GetValueAsException();
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
                        passthrough.Dispose();
                    }

                    if (group._exceptions != null)
                    {
                        state = Promise.State.Rejected;
                        _rejectContainer = CreateRejectContainer(new AggregateException(group._exceptions), int.MinValue, null, this);
                        group._exceptions = null;
                    }
                    else
                    {
                        // The group may have been completed by a void promise, in which case it already converted its exceptions to a reject container.
                        _rejectContainer = handler._rejectContainer;
                    }
                    group.MaybeDispose();

                    HandleNextInternal(state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class MergePromiseResultsGroup<TResult> : PromiseSingleAwait<TResult>
            {
                private static GetResultContainerDelegate<TResult> s_getResult;

                [MethodImpl(InlineOption)]
                private static MergePromiseResultsGroup<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseResultsGroup<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseResultsGroup<TResult>()
                        : obj.UnsafeAs<MergePromiseResultsGroup<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static MergePromiseResultsGroup<TResult> GetOrCreate(in TResult value, GetResultContainerDelegate<TResult> getResultFunc, bool isExtended)
                {
                    s_getResult = getResultFunc;
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    promise._isExtended = isExtended;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    handler.SetCompletionState(state);

                    // If any of the promises in the group completed unsuccessfully, the group state was set to canceled.
                    // We ignore that and set it to always resolved, because we're yielding a ValueTuple of ResultContainers.
                    state = Promise.State.Resolved;
                    var group = handler.UnsafeAs<MergePromiseGroupVoid>();
                    var passthroughs = group._completedPassThroughs.TakeAndClear();
                    while (passthroughs.IsNotEmpty)
                    {
                        var passthrough = passthroughs.Pop();
                        var owner = passthrough.Owner;
                        var index = passthrough.Index;
                        var ownerState = owner.State;
                        s_getResult.Invoke(owner, owner._rejectContainer, ownerState, index, ref _result);
                        if (ownerState == Promise.State.Rejected & _isExtended & index == 0)
                        {
                            // If this is an extended merge group, we need to propagate the exceptions from cancelation token callbacks.
                            state = Promise.State.Rejected;
                            _rejectContainer = owner._rejectContainer;
                        }
                        passthrough.Dispose();
                    }

                    if (group._exceptions != null)
                    {
                        // In case any cancelation token callbacks threw, we propagate them out of this promise instead of resolving this and ignoring the exceptions.
                        state = Promise.State.Rejected;
                        _rejectContainer = CreateRejectContainer(new AggregateException(group._exceptions), int.MinValue, null, this);
                        group._exceptions = null;
                    }
                    else if (handler._rejectContainer != null)
                    {
                        // The group may have been already completed, in which case it already converted its exceptions to a reject container.
                        state = Promise.State.Rejected;
                        _rejectContainer = handler._rejectContainer;
                    }
                    group.MaybeDispose();

                    HandleNextInternal(state);
                }
            }
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.MergePromiseGroupVoid GetOrCreateMergePromiseGroupVoid(CancelationRef cancelationSource)
            => PromiseRefBase.MergePromiseGroupVoid.GetOrCreate(cancelationSource);

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.MergePromiseGroup<TResult> GetOrCreateMergePromiseGroup<TResult>(in TResult value, GetResultDelegate<TResult> getResultFunc, bool isExtended)
            => PromiseRefBase.MergePromiseGroup<TResult>.GetOrCreate(value, getResultFunc, isExtended);

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.MergePromiseResultsGroup<TResult> GetOrCreateMergePromiseResultsGroup<TResult>(in TResult value, GetResultContainerDelegate<TResult> getResultFunc, bool isExtended)
            => PromiseRefBase.MergePromiseResultsGroup<TResult>.GetOrCreate(value, getResultFunc, isExtended);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidMergeGroup(int skipFrames)
            => throw new InvalidOperationException("The promise merge group is invalid.", GetFormattedStacktrace(skipFrames + 1));
    } // class Internal
}