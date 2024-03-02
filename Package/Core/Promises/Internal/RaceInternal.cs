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
                private static RacePromise<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RacePromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RacePromise<TResult>()
                        : obj.UnsafeAs<RacePromise<TResult>>();
                }

                internal static RacePromise<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits)
                {
                    var promise = GetOrCreate();
                    promise.Setup(promisePassThroughs, pendingAwaits);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    if (TrySetComplete(handler))
                    {
                        _result = handler.GetResult<TResult>();
                        _rejectContainer = handler._rejectContainer;
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
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
                private static RacePromiseWithIndexVoid GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RacePromiseWithIndexVoid>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RacePromiseWithIndexVoid()
                        : obj.UnsafeAs<RacePromiseWithIndexVoid>();
                }

                new internal static RacePromiseWithIndexVoid GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits)
                {
                    var promise = GetOrCreate();
                    promise.Setup(promisePassThroughs, pendingAwaits);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    if (TrySetComplete(handler))
                    {
                        _result = index;
                        _rejectContainer = handler._rejectContainer;
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
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
            internal sealed partial class RacePromiseWithIndex<TResult> : RacePromise<ValueTuple<int, TResult>>
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
                private static RacePromiseWithIndex<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RacePromiseWithIndex<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RacePromiseWithIndex<TResult>()
                        : obj.UnsafeAs<RacePromiseWithIndex<TResult>>();
                }

                new internal static RacePromiseWithIndex<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits)
                {
                    var promise = GetOrCreate();
                    promise.Setup(promisePassThroughs, pendingAwaits);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    if (TrySetComplete(handler))
                    {
                        _result = new ValueTuple<int, TResult>(index, handler.GetResult<TResult>());
                        _rejectContainer = handler._rejectContainer;
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
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
                private static FirstPromise<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<FirstPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new FirstPromise<TResult>()
                        : obj.UnsafeAs<FirstPromise<TResult>>();
                }

                new internal static FirstPromise<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits)
                {
                    var promise = GetOrCreate();
                    promise.Setup(promisePassThroughs, pendingAwaits);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    bool isComplete = state == Promise.State.Resolved
                        ? TrySetComplete(handler)
                        : RemoveWaiterAndGetIsComplete(handler);
                    handler.SuppressRejection = true;
                    if (isComplete)
                    {
                        _result = handler.GetResult<TResult>();
                        _rejectContainer = handler._rejectContainer;
                        handler.MaybeDispose();
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
                private static FirstPromiseWithIndexVoid GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<FirstPromiseWithIndexVoid>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new FirstPromiseWithIndexVoid()
                        : obj.UnsafeAs<FirstPromiseWithIndexVoid>();
                }

                new internal static FirstPromiseWithIndexVoid GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits)
                {
                    var promise = GetOrCreate();
                    promise.Setup(promisePassThroughs, pendingAwaits);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    bool isComplete = handler.State == Promise.State.Resolved
                        ? TrySetComplete(handler)
                        : RemoveWaiterAndGetIsComplete(handler);
                    handler.SuppressRejection = true;
                    if (isComplete)
                    {
                        _result = index;
                        _rejectContainer = handler._rejectContainer;
                        handler.MaybeDispose();
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
            internal sealed partial class FirstPromiseWithIndex<TResult> : FirstPromise<ValueTuple<int, TResult>>
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
                private static FirstPromiseWithIndex<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<FirstPromiseWithIndex<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new FirstPromiseWithIndex<TResult>()
                        : obj.UnsafeAs<FirstPromiseWithIndex<TResult>>();
                }

                new internal static FirstPromiseWithIndex<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits)
                {
                    var promise = GetOrCreate();
                    promise.Setup(promisePassThroughs, pendingAwaits);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state, int index)
                {
                    bool isComplete = handler.State == Promise.State.Resolved
                        ? TrySetComplete(handler)
                        : RemoveWaiterAndGetIsComplete(handler);
                    handler.SuppressRejection = true;
                    if (isComplete)
                    {
                        _result = new ValueTuple<int, TResult>(index, handler.GetResult<TResult>());
                        _rejectContainer = handler._rejectContainer;
                        handler.MaybeDispose();
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