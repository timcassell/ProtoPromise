#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

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
            internal abstract partial class MultiHandleablePromiseBase<TResult> : PromiseSingleAwait<TResult>
            {
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

                protected void ReleaseAndHandleNext(PromiseRefBase handler)
                {
                    // Decrement retain counter instead of calling MaybeDispose, since we know the next handler will call MaybeDispose.
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1);
                    HandleNextFromHandler(handler);
                }

                protected void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ushort depth)
                {
                    _waitCount = pendingAwaits;
                    unchecked
                    {
                        _retainCounter = pendingAwaits + 1;
                    }
                    Reset(depth);

                    _passThroughs = promisePassThroughs;
                    foreach (var passThrough in promisePassThroughs)
                    {
#if PROMISE_DEBUG
                        lock (_previousPromises)
                        {
                            _previousPromises.Push(passThrough.Owner);
                        }
#endif
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
#if PROMISE_DEBUG
                    lock (_previousPromises)
                    {
                        _previousPromises.Clear();
                    }
#endif
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial class MergePromise<TResult> : MultiHandleablePromiseBase<TResult>
            {
                private MergePromise() { }

                internal override void MaybeDispose()
                {
                    MaybeDisposeNonVirt();
                }

                private void MaybeDisposeNonVirt()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal static MergePromise<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ulong completedProgress, ushort depth)
                {
                    var promise = ObjectPool.TryTake<MergePromise<TResult>>()
                        ?? new MergePromise<TResult>();
                    promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                    return promise;
                }

                internal static MergePromise<TResult> GetOrCreate(
                    ValueLinkedStack<PromisePassThrough> promisePassThroughs,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult value,
                    PromiseResolvedDelegate<TResult> onPromiseResolved,
                    int pendingAwaits, ulong completedProgress, ushort depth)
                {
                    var promise = MergePromiseT.GetOrCreate(value, onPromiseResolved);
                    promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, depth);
                    return promise;
                }

                private void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ulong completedProgress, ushort depth)
                {
#if PROMISE_PROGRESS
                    _completeProgress = completedProgress;
#endif
                    Setup(promisePassThroughs, pendingAwaits, depth);
                }

                internal override void Handle(PromiseRefBase handler, int index)
                {
                    bool isComplete = handler.State == Promise.State.Resolved
                        ? RemoveWaiterAndGetIsComplete()
                        : TrySetComplete();
                    if (isComplete)
                    {
                        ReleaseAndHandleNext(handler);
                        return;
                    }
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    MaybeDisposeNonVirt();
                }

                private sealed class MergePromiseT : MergePromise<TResult>
                {
                    private static PromiseResolvedDelegate<TResult> _onPromiseResolved;

                    private MergePromiseT() { }

                    internal override void MaybeDispose()
                    {
                        if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                        {
                            Dispose();
                            ObjectPool.MaybeRepool(this);
                        }
                    }

                    internal static MergePromiseT GetOrCreate(
#if CSHARP_7_3_OR_NEWER
                        in
#endif
                        TResult value, PromiseResolvedDelegate<TResult> onPromiseResolved)
                    {
#if NETCOREAPP && (PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE)
                        var oldDelegete = Interlocked.CompareExchange(ref _onPromiseResolved, onPromiseResolved, null);
                        if (oldDelegete != null && oldDelegete != onPromiseResolved)
                        {
                            throw new System.InvalidOperationException("_onPromiseResolved delegate not the same.");
                        }
#else
                        _onPromiseResolved = onPromiseResolved;
#endif
                        var promise = ObjectPool.TryTake<MergePromiseT>()
                            ?? new MergePromiseT();
                        promise._result = value;
                        return promise;
                    }

                    internal override void Handle(PromiseRefBase handler, int index)
                    {
                        bool isComplete;
                        if (handler.State == Promise.State.Resolved)
                        {
                            _onPromiseResolved.Invoke(handler, ref _result, index);
                            isComplete = RemoveWaiterAndGetIsComplete();
                        }
                        else
                        {
                            isComplete = TrySetComplete();
                        }
                        if (isComplete)
                        {
                            ReleaseAndHandleNext(handler);
                            return;
                        }
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
                        MaybeDisposeNonVirt();
                    }
                }
            }
        }
    }
}