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
        partial class HandleablePromiseBase
        {
            internal virtual void Handle(PromiseRefBase.PromisePassThroughForMergeGroup passthrough, PromiseRefBase handler, Promise.State state) { throw new System.InvalidOperationException(); }
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
                        return _completeState;
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
                        HandleNextInternal(_completeState);
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
                internal static Promise<TResult> New(MergePromiseGroupVoid group, in TResult value, GetResultDelegate<TResult> getResultFunc, bool isExtended)
                {
                    s_getResult = getResultFunc;
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    promise._isExtended = isExtended;
                    promise.SetPrevious(group);
                    group.HookupNewWaiter(group.Id, promise);
                    return new Promise<TResult>(promise);
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

                    HandleNextInternal(state);
                }
            }
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.MergePromiseGroupVoid GetOrCreateMergePromiseGroupVoid(CancelationRef cancelationSource)
            => PromiseRefBase.MergePromiseGroupVoid.GetOrCreate(cancelationSource);

        [MethodImpl(InlineOption)]
        internal static Promise<TResult> NewMergePromiseGroup<TResult>(
            PromiseRefBase.MergePromiseGroupVoid group, in TResult value, GetResultDelegate<TResult> getResultFunc, bool isExtended)
            => PromiseRefBase.MergePromiseGroup<TResult>.New(group, value, getResultFunc, isExtended);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidMergeGroup(int skipFrames)
            => throw new InvalidOperationException("The promise merge group is invalid.", GetFormattedStacktrace(skipFrames + 1));
    } // class Internal
}