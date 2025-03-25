#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0016 // Use 'throw' expression

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
            private abstract partial class CatchPromiseBase<TResult, TReject, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<TReject, PromiseWrapper<TResult>>
            {
                protected void HandleCore(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    var callback = _callback;
                    _callback = default;

                    var rejectContainer = handler.RejectContainer;
                    if (state != Promise.State.Rejected || !GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    InvokeAndAdopt(rejectArg, callback, rejectContainer);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CatchPromise<TResult, TReject, TDelegate> : CatchPromiseBase<TResult, TReject, TDelegate>
                where TDelegate : IFunc<TReject, PromiseWrapper<TResult>>
            {
                private CatchPromise() { }

                [MethodImpl(InlineOption)]
                private static CatchPromise<TResult, TReject, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CatchPromise<TResult, TReject, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CatchPromise<TResult, TReject, TDelegate>()
                        : obj.UnsafeAs<CatchPromise<TResult, TReject, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CatchPromise<TResult, TReject, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
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
                    HandleCore(handler, state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CatchWaitPromise<TResult, TReject, TDelegate> : CatchPromiseBase<TResult, TReject, TDelegate>
                where TDelegate : IFunc<TReject, PromiseWrapper<TResult>>
            {
                private CatchWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CatchWaitPromise<TResult, TReject, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CatchWaitPromise<TResult, TReject, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CatchWaitPromise<TResult, TReject, TDelegate>()
                        : obj.UnsafeAs<CatchWaitPromise<TResult, TReject, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CatchWaitPromise<TResult, TReject, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
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

                    if (!_firstContinue)
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    HandleCore(handler, state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private abstract partial class CatchCancelationPromiseBase<TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<VoidResult, PromiseWrapper<TResult>>
            {
                protected void HandleCore(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    var callback = _callback;
                    _callback = default;

                    if (state != Promise.State.Canceled)
                    {
                        HandleSelf(handler, state);
                        return;
                    }

                    handler.MaybeDispose();
                    InvokeAndAdopt(default(VoidResult), callback, null);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CatchCancelationPromise<TResult, TDelegate> : CatchCancelationPromiseBase<TResult, TDelegate>
                where TDelegate : IFunc<VoidResult, PromiseWrapper<TResult>>
            {
                private CatchCancelationPromise() { }

                [MethodImpl(InlineOption)]
                private static CatchCancelationPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CatchCancelationPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CatchCancelationPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<CatchCancelationPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CatchCancelationPromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
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
                    HandleCore(handler, state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CatchCancelationWaitPromise<TResult, TDelegate> : CatchCancelationPromiseBase<TResult, TDelegate>
                where TDelegate : IFunc<VoidResult, PromiseWrapper<TResult>>
            {
                private CatchCancelationWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CatchCancelationWaitPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CatchCancelationWaitPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CatchCancelationWaitPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<CatchCancelationWaitPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CatchCancelationWaitPromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
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

                    if (!_firstContinue)
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    HandleCore(handler, state);
                }
            }
        } // class PromiseRefBase
    } // class Internal
} // namespace Proto.Promises