#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0074 // Use compound assignment

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
#else
        internal delegate void GetResultDelegate<TResult>(PromiseRefBase handler, int index, ref TResult result);
#endif

        [MethodImpl(InlineOption)]
        internal static void PrepareForMerge<TResult>(Promise promise, in TResult result, ref uint pendingCount,
            ref PromiseRefBase.MergePromise<TResult> mergePromise, GetResultDelegate<TResult> getResultDelegate)
        {
            if (promise._ref != null)
            {
                checked { ++pendingCount; }
                if (mergePromise == null)
                {
                    mergePromise = PromiseRefBase.GetOrCreateMergePromise(result, getResultDelegate);
                }
                mergePromise.AddWaiter(promise._ref, promise._id);
            }
        }

        [MethodImpl(InlineOption)]
        internal static void PrepareForMerge<T, TResult>(Promise<T> promise, ref T value, in TResult result, ref uint pendingCount, int index,
            ref PromiseRefBase.MergePromise<TResult> mergePromise, GetResultDelegate<TResult> getResultDelegate)
        {
            if (promise._ref == null)
            {
                value = promise._result;
            }
            else
            {
                checked { ++pendingCount; }
                if (mergePromise == null)
                {
                    mergePromise = PromiseRefBase.GetOrCreateMergePromise(result, getResultDelegate);
                }
                mergePromise.AddWaiterWithIndex(promise._ref, promise._id, index);
            }
        }

        [MethodImpl(InlineOption)]
        internal static void PrepareForMergeSettled<TResult>(Promise promise, in TResult result, ref uint pendingCount, int index,
            ref PromiseRefBase.MergeSettledPromise<TResult> mergePromise, GetResultDelegate<TResult> getResultDelegate)
        {
            if (promise._ref != null)
            {
                checked { ++pendingCount; }
                if (mergePromise == null)
                {
                    mergePromise = PromiseRefBase.GetOrCreateMergeSettledPromise(result, getResultDelegate);
                }
                mergePromise.AddWaiterWithIndex(promise._ref, promise._id, index);
            }
        }

        [MethodImpl(InlineOption)]
        internal static void PrepareForMergeSettled<T, TResult>(Promise<T> promise, ref Promise<T>.ResultContainer value, in TResult result, ref uint pendingCount, int index,
            ref PromiseRefBase.MergeSettledPromise<TResult> mergePromise, GetResultDelegate<TResult> getResultDelegate)
        {
            if (promise._ref == null)
            {
                value = promise._result;
            }
            else
            {
                checked { ++pendingCount; }
                if (mergePromise == null)
                {
                    mergePromise = PromiseRefBase.GetOrCreateMergeSettledPromise(result, getResultDelegate);
                }
                mergePromise.AddWaiterWithIndex(promise._ref, promise._id, index);
            }
        }

        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromisePassThroughForAll : PromisePassThrough, ILinked<PromisePassThroughForAll>
            {
                PromisePassThroughForAll ILinked<PromisePassThroughForAll>.Next
                {
                    get => _next.UnsafeAs<PromisePassThroughForAll>();
                    set => _next = value;
                }

                internal PromiseRefBase Owner
                {
                    [MethodImpl(InlineOption)]
                    get => _owner;
                }

                private PromisePassThroughForAll() { }

                [MethodImpl(InlineOption)]
                private static PromisePassThroughForAll GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromisePassThroughForAll>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromisePassThroughForAll()
                        : obj.UnsafeAs<PromisePassThroughForAll>();
                }

                [MethodImpl(InlineOption)]
                internal static PromisePassThroughForAll GetOrCreate(PromiseRefBase owner, short id, int index)
                {
                    var passThrough = GetOrCreate();
                    passThrough._next = null;
                    passThrough._index = index;
                    passThrough._owner = owner;
                    passThrough._id = id;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    passThrough._disposed = false;
#endif
                    return passThrough;
                }

                internal void Hookup(PromiseRefBase target)
                {
                    ThrowIfInPool(this);
                    _next = target;
                    _owner.HookupNewWaiter(_id, this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    var target = _next;
                    var index = _index;
                    Dispose();
                    target.Handle(handler, state, index);
                }

                private void Dispose()
                {
                    ThrowIfInPool(this);
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
            internal abstract partial class MultiHandleablePromiseBase<TResult> : PromiseSingleAwait<TResult>
            {
                partial void AddPending(PromiseRefBase pendingPromise);
                partial void RemoveComplete(PromiseRefBase completePromise);

                internal override void Handle(PromiseRefBase handler, Promise.State state) { throw new System.InvalidOperationException(); }

                [MethodImpl(InlineOption)]
                new protected void Reset()
                {
                    _isComplete = 0; // false
                    _retainCounter = 1; // Start with 1 so this won't be disposed while promises are still being hooked up.
                    base.Reset();
                }

                [MethodImpl(InlineOption)]
                protected bool TrySetComplete(PromiseRefBase completePromise)
                {
                    RemoveComplete(completePromise);
                    return Interlocked.Exchange(ref _isComplete, 1) == 0;
                }

                [MethodImpl(InlineOption)]
                protected bool RemoveWaiterAndGetIsComplete(PromiseRefBase completePromise, ref int waitCount)
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref waitCount, -1) == 0)
                    {
                        return TrySetComplete(completePromise);
                    }
                    RemoveComplete(completePromise);
                    return false;
                }

                [MethodImpl(InlineOption)]
                protected void AddWaiterForAll(PromisePassThroughForAll passthrough)
                {
                    AddPending(passthrough.Owner);
                    passthrough.Hookup(this);
                }

                internal void AddWaiterWithIndex(PromiseRefBase promise, short id, int index)
                {
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);
                    AddPending(promise);
                    var passthrough = PromisePassThrough.GetOrCreate(promise, this, index);
                    promise.HookupNewWaiter(id, passthrough);
                }

                internal void AddWaiter(PromiseRefBase promise, short id)
                {
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);
                    AddPending(promise);
                    promise.HookupNewWaiter(id, this);
                }

                protected void MarkReady(uint totalWaiters, ref int waitCount, Promise.State stateIfComplete)
                {
                    // This method is called after all promises have been hooked up to this,
                    // so that we only need to do this Interlocked loop once, instead of for each promise.
                    // _waitCount is set to -1 when this is created, so we need to subtract how many promises have already completed.
                    // Promises can complete concurrently on other threads, which is why we need to do this with an Interlocked loop.
                    unchecked
                    {
                        int previousWaitCount = Volatile.Read(ref waitCount);
                        while (true)
                        {
                            uint completedCount = uint.MaxValue - (uint) previousWaitCount;
                            uint newWaitCount = totalWaiters - completedCount;
                            if (newWaitCount == 0)
                            {
                                // All promises already completed.
                                if (_isComplete == 0)
                                {
                                    _next = PromiseCompletionSentinel.s_instance;
                                    SetCompletionState(_rejectContainer == null ? stateIfComplete : Promise.State.Rejected);
                                }
                                break;
                            }
                            int oldCount = Interlocked.CompareExchange(ref waitCount, (int) newWaitCount, previousWaitCount);
                            if (oldCount == previousWaitCount)
                            {
                                break;
                            }
                            previousWaitCount = oldCount;
                        }
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class MergePromiseBase<TResult> : MultiHandleablePromiseBase<TResult>
            {
                [MethodImpl(InlineOption)]
                new protected void Reset()
                {
                    _waitCount = -1; // uint.MaxValue
                    base.Reset();
                }

                [MethodImpl(InlineOption)]
                protected bool RemoveWaiterAndGetIsComplete(PromiseRefBase completePromise)
                    => RemoveWaiterAndGetIsComplete(completePromise, ref _waitCount);

                [MethodImpl(InlineOption)]
                internal void MarkReady(uint totalWaiters)
                    => MarkReady(totalWaiters, ref _waitCount, Promise.State.Resolved);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class MergePromise<TResult> : MergePromiseBase<TResult>
            {
                private static GetResultDelegate<TResult> s_getResult;

                [MethodImpl(InlineOption)]
                private static MergePromise<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<MergePromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergePromise<TResult>()
                        : obj.UnsafeAs<MergePromise<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static MergePromise<TResult> GetOrCreate(in TResult value, GetResultDelegate<TResult> getResultFunc)
                {
                    s_getResult = getResultFunc;
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal static MergePromise<TResult> GetOrCreateVoid()
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal static MergePromise<TResult> GetOrCreateAll(
                    TResult value,
                    GetResultDelegate<TResult> getResultFunc,
                    ValueLinkedStack<PromisePassThroughForAll> passthroughs,
                    int waitCount)
                {
                    s_getResult = getResultFunc;
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    promise._waitCount = waitCount;
                    unchecked { promise._retainCounter = waitCount + 1; }
                    do
                    {
                        promise.AddWaiterForAll(passthroughs.Pop());
                    } while (passthroughs.IsNotEmpty);
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

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    handler.SetCompletionState(state);
                    bool isComplete;
                    if (state == Promise.State.Resolved)
                    {
                        s_getResult.Invoke(handler, index, ref _result);
                        isComplete = RemoveWaiterAndGetIsComplete(handler);
                    }
                    else
                    {
                        isComplete = TrySetComplete(handler);
                    }
                    if (isComplete)
                    {
                        _rejectContainer = handler._rejectContainer;
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
                        InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1);
                        HandleNextInternal(state);
                        return;
                    }
                    handler.MaybeReportUnhandledAndDispose(state);
                    MaybeDispose();
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    // This is called from void promises. They don't need to update any value, so they have no index.
                    handler.SetCompletionState(state);
                    bool isComplete = state == Promise.State.Resolved
                        ? RemoveWaiterAndGetIsComplete(handler)
                        : TrySetComplete(handler);
                    if (isComplete)
                    {
                        _rejectContainer = handler._rejectContainer;
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
                        InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1);
                        HandleNextInternal(state);
                        return;
                    }
                    handler.MaybeReportUnhandledAndDispose(state);
                    MaybeDispose();
                }
            }

            [MethodImpl(InlineOption)]
            internal static MergePromise<VoidResult> GetOrCreateAllPromiseVoid()
                => MergePromise<VoidResult>.GetOrCreateVoid();

            [MethodImpl(InlineOption)]
            internal static MergePromise<TResult> GetOrCreateMergePromise<TResult>(in TResult value, GetResultDelegate<TResult> getResultFunc)
                => MergePromise<TResult>.GetOrCreate(value, getResultFunc);

            [MethodImpl(InlineOption)]
            internal static MergePromise<IList<TResult>> GetOrCreateAllPromise<TResult>(
                IList<TResult> value,
                GetResultDelegate<IList<TResult>> getResultFunc,
                ValueLinkedStack<PromisePassThroughForAll> passthroughs,
                uint waitCount)
                => MergePromise<IList<TResult>>.GetOrCreateAll(value, getResultFunc, passthroughs, unchecked((int) waitCount));

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class MergeSettledPromise<TResult> : MergePromiseBase<TResult>
            {
                private static GetResultDelegate<TResult> s_getResult;

                [MethodImpl(InlineOption)]
                private static MergeSettledPromise<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<MergeSettledPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergeSettledPromise<TResult>()
                        : obj.UnsafeAs<MergeSettledPromise<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static MergeSettledPromise<TResult> GetOrCreate(in TResult value, GetResultDelegate<TResult> getResultFunc)
                {
                    s_getResult = getResultFunc;
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal static MergeSettledPromise<TResult> GetOrCreateAll(
                    TResult value,
                    GetResultDelegate<TResult> getResultFunc,
                    ValueLinkedStack<PromisePassThroughForAll> passthroughs,
                    int waitCount)
                {
                    s_getResult = getResultFunc;
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = value;
                    promise._waitCount = waitCount;
                    unchecked { promise._retainCounter = waitCount + 1; }
                    do
                    {
                        promise.AddWaiterForAll(passthroughs.Pop());
                    } while (passthroughs.IsNotEmpty);
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

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    handler.SetCompletionState(state);
                    s_getResult.Invoke(handler, index, ref _result);
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    if (RemoveWaiterAndGetIsComplete(handler))
                    {
                        InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1);
                        HandleNextInternal(Promise.State.Resolved);
                        return;
                    }
                    MaybeDispose();
                }
            }

            [MethodImpl(InlineOption)]
            internal static MergeSettledPromise<TResult> GetOrCreateMergeSettledPromise<TResult>(in TResult value, GetResultDelegate<TResult> getResultFunc)
                => MergeSettledPromise<TResult>.GetOrCreate(value, getResultFunc);

            [MethodImpl(InlineOption)]
            internal static MergeSettledPromise<IList<TResultContainer>> GetOrCreateAllSettledPromise<TResultContainer>(
                IList<TResultContainer> value,
                GetResultDelegate<IList<TResultContainer>> getResultFunc,
                ValueLinkedStack<PromisePassThroughForAll> passthroughs,
                uint waitCount)
                => MergeSettledPromise<IList<TResultContainer>>.GetOrCreateAll(value, getResultFunc, passthroughs, unchecked((int) waitCount));
        } // class PromiseRefBase
    } // class Internal
}