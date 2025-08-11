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
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    RejectContainer = handler.RejectContainer;
                    var group = handler.UnsafeAs<MergePromiseGroupVoid>();
                    var passthroughs = group._completedPassThroughs.TakeAndClear();
                    group.MaybeDispose();
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

                    // If any of the promises in the group completed unsuccessfully, the group was canceled.
                    // We ignore that and set it to always resolved, because we're yielding a ValueTuple of ResultContainers, unless a cancelation token callback threw.
                    HandleNextInternal(RejectContainer == null ? Promise.State.Resolved : state);
                }
            }
        } // class PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static Promise<TResult> NewMergePromiseResultsGroup<TResult>(PromiseRefBase.MergePromiseGroupVoid group, in TResult value, GetResultDelegate<TResult> getResultFunc, bool isExtended)
            => PromiseRefBase.MergePromiseResultsGroup<TResult>.New(group, value, getResultFunc, isExtended);
    } // class Internal
}