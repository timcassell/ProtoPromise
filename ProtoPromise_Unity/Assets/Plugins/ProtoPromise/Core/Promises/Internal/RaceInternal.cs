#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;

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

                internal static RacePromise<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ushort depth)
                {
                    var promise = ObjectPool.TryTake<RacePromise<TResult>>()
                        ?? new RacePromise<TResult>();
                    promise.Setup(promisePassThroughs, pendingAwaits, depth);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, int index)
                {
                    if (TrySetComplete())
                    {
                        _result = handler.GetResult<TResult>();
                        ReleaseAndHandleNext(handler);
                        return;
                    }
                    handler.MaybeDispose();
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

                new internal static RacePromiseWithIndexVoid GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ushort depth)
                {
                    var promise = ObjectPool.TryTake<RacePromiseWithIndexVoid>()
                        ?? new RacePromiseWithIndexVoid();
                    promise.Setup(promisePassThroughs, pendingAwaits, depth);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, int index)
                {
                    if (TrySetComplete())
                    {
                        _result = index;
                        ReleaseAndHandleNext(handler);
                        return;
                    }
                    handler.MaybeDispose();
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

                new internal static RacePromiseWithIndex<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ushort depth)
                {
                    var promise = ObjectPool.TryTake<RacePromiseWithIndex<TResult>>()
                        ?? new RacePromiseWithIndex<TResult>();
                    promise.Setup(promisePassThroughs, pendingAwaits, depth);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, int index)
                {
                    if (TrySetComplete())
                    {
                        _result = new ValueTuple<int, TResult>(index, handler.GetResult<TResult>());
                        ReleaseAndHandleNext(handler);
                        return;
                    }
                    handler.MaybeDispose();
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

                new internal static FirstPromise<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ushort depth)
                {
                    var promise = ObjectPool.TryTake<FirstPromise<TResult>>()
                        ?? new FirstPromise<TResult>();
                    promise.Setup(promisePassThroughs, pendingAwaits, depth);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, int index)
                {
                    bool isComplete = handler.State == Promise.State.Resolved
                        ? TrySetComplete()
                        : RemoveWaiterAndGetIsComplete();
                    if (isComplete)
                    {
                        _result = handler.GetResult<TResult>();
                        ReleaseAndHandleNext(handler);
                        return;
                    }
                    handler.SuppressRejection = true;
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

                new internal static FirstPromiseWithIndexVoid GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ushort depth)
                {
                    var promise = ObjectPool.TryTake<FirstPromiseWithIndexVoid>()
                        ?? new FirstPromiseWithIndexVoid();
                    promise.Setup(promisePassThroughs, pendingAwaits, depth);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, int index)
                {
                    bool isComplete = handler.State == Promise.State.Resolved
                        ? TrySetComplete()
                        : RemoveWaiterAndGetIsComplete();
                    if (isComplete)
                    {
                        _result = index;
                        ReleaseAndHandleNext(handler);
                        return;
                    }
                    handler.SuppressRejection = true;
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

                new internal static FirstPromiseWithIndex<TResult> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ushort depth)
                {
                    var promise = ObjectPool.TryTake<FirstPromiseWithIndex<TResult>>()
                        ?? new FirstPromiseWithIndex<TResult>();
                    promise.Setup(promisePassThroughs, pendingAwaits, depth);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, int index)
                {
                    bool isComplete = handler.State == Promise.State.Resolved
                        ? TrySetComplete()
                        : RemoveWaiterAndGetIsComplete();
                    if (isComplete)
                    {
                        _result = new ValueTuple<int, TResult>(index, handler.GetResult<TResult>());
                        ReleaseAndHandleNext(handler);
                        return;
                    }
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    MaybeDispose();
                }
            }
        }
    }
}