#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        // If C#9 is available, we use function pointers instead of delegates.
#if NETCOREAPP || UNITY_2021_2_OR_NEWER
        internal readonly unsafe struct GetResultDelegate<TResult>
        {
            private readonly delegate*<PromiseRefBase, int, ref TResult, void> _ptr;

            [MethodImpl(InlineOption)]
            internal GetResultDelegate(delegate*<PromiseRefBase, int, ref TResult, void> ptr) => _ptr = ptr;

            [MethodImpl(InlineOption)]
            internal void Invoke(PromiseRefBase handler, int index, ref TResult result) => _ptr(handler, index, ref result);
        }

        internal readonly unsafe struct GetResultContainerDelegate<TResult>
        {
            private readonly delegate*<PromiseRefBase, object, Promise.State, int, ref TResult, void> _ptr;

            [MethodImpl(InlineOption)]
            internal GetResultContainerDelegate(delegate*<PromiseRefBase, object, Promise.State, int, ref TResult, void> ptr) => _ptr = ptr;

            [MethodImpl(InlineOption)]
            internal void Invoke(PromiseRefBase handler, object rejectContainer, Promise.State state, int index, ref TResult result) => _ptr(handler, rejectContainer, state, index, ref result);
        }
#else
        internal delegate void GetResultDelegate<TResult>(PromiseRefBase handler, int index, ref TResult result);

        internal delegate void GetResultContainerDelegate<TResult>(PromiseRefBase handler, object rejectContainer, Promise.State state, int index, ref TResult result);
#endif

        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class MultiHandleablePromiseBase<TResult> : PromiseSingleAwait<TResult>
            {
                partial void AddPending(PromiseRefBase pendingPromise);
                partial void ClearPending();

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state) { throw new System.InvalidOperationException(); }

                // When each promise is completed, we decrement the wait count until it reaches zero before we handle the next waiter.
                // If a promise completes with a state that should complete this promise before all the other promises were complete,
                // we instead swap the count to 0 so that it will be marked complete early, and future completions will decrement into negative and never read 0 again.
                // (Merge reject/cancel, or First resolve, Race always tries to set complete).
                [MethodImpl(InlineOption)]
                protected bool TrySetComplete()
                {
                    return InterlockedExchange(ref _waitCount, 0) > 0;
                }

                [MethodImpl(InlineOption)]
                protected bool RemoveWaiterAndGetIsComplete()
                {
                    // No overflow check as we expect the count to be able to go negative.
                    return Interlocked.Add(ref _waitCount, -1) == 0;
                }

                internal void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits)
                {
                    _waitCount = pendingAwaits;
                    _retainCounter = pendingAwaits;
                    Reset();

                    _passThroughs = promisePassThroughs;
                    foreach (var passThrough in promisePassThroughs)
                    {
                        AddPending(passThrough.Owner);
                        passThrough.SetTargetAndAddToOwner(this);
                    }
                }

                new protected void Dispose()
                {
                    base.Dispose();
                    while (_passThroughs.IsNotEmpty)
                    {
                        _passThroughs.Pop().Dispose();
                    }
                    ClearPending();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class MergePromiseVoid : MultiHandleablePromiseBase<VoidResult>
            {
                [MethodImpl(InlineOption)]
                private static MergePromiseVoid GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseVoid>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseVoid()
                        : obj.UnsafeAs<MergePromiseVoid>();
                }

                [MethodImpl(InlineOption)]
                internal static MergePromiseVoid GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits)
                {
                    var promise = GetOrCreate();
                    promise.Setup(promisePassThroughs, pendingAwaits);
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state, int index)
                {
                    bool isComplete = state == Promise.State.Resolved
                        ? RemoveWaiterAndGetIsComplete()
                        : TrySetComplete();
                    if (isComplete)
                    {
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
                        HandleNextInternal(rejectContainer, state);
                        return;
                    }
                    handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
                    MaybeDispose();
                }
            }

            internal static MultiHandleablePromiseBase<VoidResult> GetOrCreateAllPromiseVoid(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits)
            {
                return MergePromiseVoid.GetOrCreate(promisePassThroughs, pendingAwaits);
            }

            internal sealed class MergePromiseT<TResult> : MultiHandleablePromiseBase<TResult>
            {
                private static GetResultDelegate<TResult> s_getResult;

                [MethodImpl(InlineOption)]
                private static MergePromiseT<TResult> GetOrCreate()
                {
                    // We take the base type instead of the concrete type because the base type re-pools with its type.
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromiseT<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromiseT<TResult>()
                        : obj.UnsafeAs<MergePromiseT<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static MergePromiseT<TResult> GetOrCreate(
                    ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult value,
                    int pendingAwaits,
                    GetResultDelegate<TResult> getResultFunc)
                {
                    s_getResult = getResultFunc;
                    var promise = GetOrCreate();
                    promise._result = value;
                    promise.Setup(promisePassThroughs, pendingAwaits);
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal override sealed void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state, int index)
                {
                    bool isComplete;
                    if (state == Promise.State.Resolved)
                    {
                        s_getResult.Invoke(handler, index, ref _result);
                        isComplete = RemoveWaiterAndGetIsComplete();
                    }
                    else
                    {
                        isComplete = TrySetComplete();
                    }
                    if (isComplete)
                    {
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
                        HandleNextInternal(rejectContainer, state);
                        return;
                    }
                    handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
                    MaybeDispose();
                }
            }

            [MethodImpl(InlineOption)]
            internal static MultiHandleablePromiseBase<TResult> GetOrCreateMergePromise<TResult>(
                ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TResult value,
                int pendingAwaits,
                GetResultDelegate<TResult> getResultFunc)
            {
                return MergePromiseT<TResult>.GetOrCreate(promisePassThroughs, value, pendingAwaits, getResultFunc);
            }

            internal sealed partial class MergeSettledPromise<TResult> : MultiHandleablePromiseBase<TResult>
            {
                private static GetResultContainerDelegate<TResult> s_getResult;

                [MethodImpl(InlineOption)]
                private static MergeSettledPromise<TResult> GetOrCreate()
                {
                    // We take the base type instead of the concrete type because the base type re-pools with its type.
                    var obj = ObjectPool.TryTakeOrInvalid<MergeSettledPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergeSettledPromise<TResult>()
                        : obj.UnsafeAs<MergeSettledPromise<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static MergeSettledPromise<TResult> GetOrCreate(
                    ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult value,
                    int pendingAwaits,
                    GetResultContainerDelegate<TResult> getResultFunc)
                {
                    s_getResult = getResultFunc;
                    var promise = GetOrCreate();
                    promise._result = value;
                    promise.Setup(promisePassThroughs, pendingAwaits);
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal override sealed void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state, int index)
                {
                    s_getResult.Invoke(handler, rejectContainer, state, index, ref _result);
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    if (RemoveWaiterAndGetIsComplete())
                    {
                        HandleNextInternal(null, Promise.State.Resolved);
                        return;
                    }
                    MaybeDispose();
                }
            }

            [MethodImpl(InlineOption)]
            internal static MergeSettledPromise<TResult> GetOrCreateMergeSettledPromise<TResult>(
                ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TResult value,
                int pendingAwaits,
                GetResultContainerDelegate<TResult> getResultFunc)
            {
                return MergeSettledPromise<TResult>.GetOrCreate(promisePassThroughs, value, pendingAwaits, getResultFunc);
            }
        } // class PromiseRefBase
    } // class Internal
}