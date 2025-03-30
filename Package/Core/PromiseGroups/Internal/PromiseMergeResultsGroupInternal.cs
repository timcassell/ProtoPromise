#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class MergePromiseResultsGroup<TResult> : SingleAwaitPromise<TResult>
            {
                private static GetResultDelegate<TResult> s_getResult;

                [MethodImpl(InlineOption)]
                private static MergePromiseResultsGroup<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseResultsGroup<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseResultsGroup<TResult>()
                        : obj.UnsafeAs<MergePromiseResultsGroup<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static MergePromiseResultsGroup<TResult> GetOrCreate(in TResult value, GetResultDelegate<TResult> getResultFunc, bool isExtended)
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
                        s_getResult.Invoke(owner, index, ref _result);
                        if (owner.State == Promise.State.Rejected & _isExtended & index == 0)
                        {
                            // If this is an extended merge group, we need to propagate the exceptions from cancelation token callbacks.
                            state = Promise.State.Rejected;
                            RejectContainer = owner.RejectContainer;
                        }
                        passthrough.Dispose();
                    }

                    if (group._exceptions != null)
                    {
                        // In case any cancelation token callbacks threw, we propagate them out of this promise instead of resolving this and ignoring the exceptions.
                        state = Promise.State.Rejected;
                        RejectContainer = CreateRejectContainer(new AggregateException(group._exceptions), int.MinValue, null, this);
                        group._exceptions = null;
                    }
                    else if (handler.RejectContainer != null)
                    {
                        // The group may have been already completed, in which case it already converted its exceptions to a reject container.
                        state = Promise.State.Rejected;
                        RejectContainer = handler.RejectContainer;
                    }
                    group.MaybeDispose();

                    HandleNextInternal(state);
                }
            }
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static PromiseRefBase.MergePromiseResultsGroup<TResult> GetOrCreateMergePromiseResultsGroup<TResult>(in TResult value, GetResultDelegate<TResult> getResultFunc, bool isExtended)
            => PromiseRefBase.MergePromiseResultsGroup<TResult>.GetOrCreate(value, getResultFunc, isExtended);
    } // class Internal
}