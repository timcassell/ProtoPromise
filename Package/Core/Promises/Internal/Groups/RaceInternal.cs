#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
            internal partial class RacePromise<TResult> : MultiHandleablePromiseBase<TResult>
            {
                protected RacePromise() { }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static RacePromise<TResult> GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RacePromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RacePromise<TResult>()
                        : obj.UnsafeAs<RacePromise<TResult>>();
                }

                internal static RacePromise<TResult> GetOrCreate()
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    handler.SetCompletionState(state);
                    if (TrySetComplete(handler))
                    {
                        _result = handler.GetResult<TResult>();
                        RejectContainer = handler.RejectContainer;
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

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RacePromiseWithIndexVoid : RacePromise<int>
            {
                private RacePromiseWithIndexVoid() { }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static RacePromiseWithIndexVoid GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RacePromiseWithIndexVoid>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RacePromiseWithIndexVoid()
                        : obj.UnsafeAs<RacePromiseWithIndexVoid>();
                }

                new internal static RacePromiseWithIndexVoid GetOrCreate()
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    handler.SetCompletionState(state);
                    if (TrySetComplete(handler))
                    {
                        _result = index;
                        RejectContainer = handler.RejectContainer;
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

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RacePromiseWithIndex<TResult> : RacePromise<(int, TResult)>
            {
                private RacePromiseWithIndex() { }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static RacePromiseWithIndex<TResult> GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RacePromiseWithIndex<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RacePromiseWithIndex<TResult>()
                        : obj.UnsafeAs<RacePromiseWithIndex<TResult>>();
                }

                new internal static RacePromiseWithIndex<TResult> GetOrCreate()
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    handler.SetCompletionState(state);
                    if (TrySetComplete(handler))
                    {
                        _result = (index, handler.GetResult<TResult>());
                        RejectContainer = handler.RejectContainer;
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

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial class FirstPromise<TResult> : RacePromise<TResult>
            {
                protected FirstPromise() { }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static FirstPromise<TResult> GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<FirstPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new FirstPromise<TResult>()
                        : obj.UnsafeAs<FirstPromise<TResult>>();
                }

                new internal static FirstPromise<TResult> GetOrCreate()
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    return promise;
                }

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
                    => MarkReady(totalWaiters, ref _waitCount, Promise.State.Canceled);

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    handler.SetCompletionState(state);
                    bool isComplete = state == Promise.State.Resolved
                        ? TrySetComplete(handler)
                        : RemoveWaiterAndGetIsComplete(handler);
                    RejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    if (isComplete)
                    {
                        _result = handler.GetResult<TResult>();
                        handler.MaybeDispose();
                        InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1);
                        HandleNextInternal(state);
                        return;
                    }
                    handler.MaybeDispose();
                    MaybeDispose();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class FirstPromiseWithIndexVoid : FirstPromise<int>
            {
                private FirstPromiseWithIndexVoid() { }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static FirstPromiseWithIndexVoid GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<FirstPromiseWithIndexVoid>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new FirstPromiseWithIndexVoid()
                        : obj.UnsafeAs<FirstPromiseWithIndexVoid>();
                }

                new internal static FirstPromiseWithIndexVoid GetOrCreate()
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    handler.SetCompletionState(state);
                    bool isComplete = handler.State == Promise.State.Resolved
                        ? TrySetComplete(handler)
                        : RemoveWaiterAndGetIsComplete(handler);
                    RejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    if (isComplete)
                    {
                        _result = index;
                        handler.MaybeDispose();
                        InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1);
                        HandleNextInternal(state);
                        return;
                    }
                    handler.MaybeDispose();
                    MaybeDispose();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class FirstPromiseWithIndex<TResult> : FirstPromise<(int, TResult)>
            {
                private FirstPromiseWithIndex() { }

                internal override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static FirstPromiseWithIndex<TResult> GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<FirstPromiseWithIndex<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new FirstPromiseWithIndex<TResult>()
                        : obj.UnsafeAs<FirstPromiseWithIndex<TResult>>();
                }

                new internal static FirstPromiseWithIndex<TResult> GetOrCreate()
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    handler.SetCompletionState(state);
                    bool isComplete = handler.State == Promise.State.Resolved
                        ? TrySetComplete(handler)
                        : RemoveWaiterAndGetIsComplete(handler);
                    RejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    if (isComplete)
                    {
                        _result = (index, handler.GetResult<TResult>());
                        handler.MaybeDispose();
                        InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1);
                        HandleNextInternal(state);
                        return;
                    }
                    handler.MaybeDispose();
                    MaybeDispose();
                }
            }
        }
    }
}