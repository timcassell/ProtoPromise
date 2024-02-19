#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0250 // Make struct 'readonly'
#pragma warning disable IDE0251 // Make member 'readonly'

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
            internal static class DelegateWrapper
            {
                // These static functions help with the implementation so we don't need to type the generics in every method.

                [MethodImpl(InlineOption)]
                internal static DelegateResolvePassthrough CreatePassthrough()
                {
                    return new DelegateResolvePassthrough(true);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateResolvePassthrough<TResult> CreatePassthrough<TResult>()
                {
                    return new DelegateResolvePassthrough<TResult>(true);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateVoidVoid Create(Action callback)
                {
                    return new DelegateVoidVoid(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateVoidResult<TResult> Create<TResult>(Func<TResult> callback)
                {
                    return new DelegateVoidResult<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateArgVoid<TArg> Create<TArg>(Action<TArg> callback)
                {
                    return new DelegateArgVoid<TArg>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateArgResult<TArg, TResult> Create<TArg, TResult>(Func<TArg, TResult> callback)
                {
                    return new DelegateArgResult<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromiseVoidVoid Create(Func<Promise> callback)
                {
                    return new DelegatePromiseVoidVoid(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromiseVoidResult<TResult> Create<TResult>(Func<Promise<TResult>> callback)
                {
                    return new DelegatePromiseVoidResult<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromiseArgVoid<TArg> Create<TArg>(Func<TArg, Promise> callback)
                {
                    return new DelegatePromiseArgVoid<TArg>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegatePromiseArgResult<TArg, TResult> Create<TArg, TResult>(Func<TArg, Promise<TResult>> callback)
                {
                    return new DelegatePromiseArgResult<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCaptureVoidVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Action<TCapture> callback)
                {
                    return new DelegateCaptureVoidVoid<TCapture>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    return new DelegateCaptureVoidResult<TCapture, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    return new DelegateCaptureArgVoid<TCapture, TArg>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    return new DelegateCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCapturePromiseVoidVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    return new DelegateCapturePromiseVoidVoid<TCapture>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCapturePromiseVoidResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateCapturePromiseVoidResult<TCapture, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCapturePromiseArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    return new DelegateCapturePromiseArgVoid<TCapture, TArg>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateCapturePromiseArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
                {
                    return new DelegateCapturePromiseArgResult<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueVoidVoid Create(Promise.ContinueAction callback)
                {
                    return new DelegateContinueVoidVoid(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueVoidResult<TResult> Create<TResult>(Promise.ContinueFunc<TResult> callback)
                {
                    return new DelegateContinueVoidResult<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueArgVoid<TArg> Create<TArg>(Promise<TArg>.ContinueAction callback)
                {
                    return new DelegateContinueArgVoid<TArg>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueArgResult<TArg, TResult> Create<TArg, TResult>(Promise<TArg>.ContinueFunc<TResult> callback)
                {
                    return new DelegateContinueArgResult<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueCaptureVoidVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Promise.ContinueAction<TCapture> callback)
                {
                    return new DelegateContinueCaptureVoidVoid<TCapture>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Promise.ContinueFunc<TCapture, TResult> callback)
                {
                    return new DelegateContinueCaptureVoidResult<TCapture, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Promise<TArg>.ContinueAction<TCapture> callback)
                {
                    return new DelegateContinueCaptureArgVoid<TCapture, TArg>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinueCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, TResult> callback)
                {
                    return new DelegateContinueCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseVoidVoid Create(Promise.ContinueFunc<Promise> callback)
                {
                    return new DelegateContinuePromiseVoidVoid(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseVoidResult<TResult> Create<TResult>(Promise.ContinueFunc<Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseVoidResult<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseArgVoid<TArg> Create<TArg>(Promise<TArg>.ContinueFunc<Promise> callback)
                {
                    return new DelegateContinuePromiseArgVoid<TArg>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseArgResult<TArg, TResult> Create<TArg, TResult>(Promise<TArg>.ContinueFunc<Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseArgResult<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseCaptureVoidVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise> callback)
                {
                    return new DelegateContinuePromiseCaptureVoidVoid<TCapture>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseCaptureVoidResult<TCapture, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise> callback)
                {
                    return new DelegateContinuePromiseCaptureArgVoid<TCapture, TArg>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateContinuePromiseCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateNewPromiseVoid Create(Action<Promise.Deferred> callback)
                {
                    return new DelegateNewPromiseVoid(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateNewPromiseResult<TResult> Create<TResult>(Action<Promise<TResult>.Deferred> callback)
                {
                    return new DelegateNewPromiseResult<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateNewPromiseCaptureVoid<TCapture> Create<TCapture>(in TCapture capturedValue, Action<TCapture, Promise.Deferred> callback)
                {
                    return new DelegateNewPromiseCaptureVoid<TCapture>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static DelegateNewPromiseCaptureResult<TCapture, TResult> Create<TCapture, TResult>(in TCapture capturedValue, Action<TCapture, Promise<TResult>.Deferred> callback)
                {
                    return new DelegateNewPromiseCaptureResult<TCapture, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                internal static Func2ArgResult<TArg1, TArg2, TResult> Create<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> callback)
                {
                    return new Func2ArgResult<TArg1, TArg2, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                internal static Func2ArgResultCapture<TCapture, TArg1, TArg2, TResult> Create<TCapture, TArg1, TArg2, TResult>(in TCapture capturedValue, Func<TCapture, TArg1, TArg2, TResult> callback)
                {
                    return new Func2ArgResultCapture<TCapture, TArg1, TArg2, TResult>(capturedValue, callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateResolvePassthrough : IAction, IFunc<Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise
            {
                private readonly bool _isActive;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return !_isActive; }
                }

                [MethodImpl(InlineOption)]
                internal DelegateResolvePassthrough(bool isActive)
                {
                    _isActive = isActive;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                }

                [MethodImpl(InlineOption)]
                Promise IFunc<Promise>.Invoke()
                {
                    return Promise.Resolved();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    owner.HandleNextInternal(state);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    owner.HandleNextInternal(state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateResolvePassthrough<TResult> : IFunc<TResult, TResult>, IFunc<TResult, Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise
            {
                private readonly bool _isActive;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return !_isActive; }
                }

                [MethodImpl(InlineOption)]
                internal DelegateResolvePassthrough(bool isActive)
                {
                    _isActive = isActive;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TResult arg)
                {
                    return arg;
                }

                [MethodImpl(InlineOption)]
                Promise<TResult> IFunc<TResult, Promise<TResult>>.Invoke(TResult arg)
                {
                    return new Promise<TResult>(arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    owner.UnsafeAs<PromiseRef<TResult>>().HandleSelf(handler, state);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    owner.UnsafeAs<PromiseRef<TResult>>().HandleSelf(handler, state);
                }
            }

            #region Regular Delegates

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateNewPromiseVoid : IDelegateNew<VoidResult>
            {
                private readonly Action<Promise.Deferred> _callback;

                [MethodImpl(InlineOption)]
                public DelegateNewPromiseVoid(Action<Promise.Deferred> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                void IDelegateNew<VoidResult>.Invoke(DeferredPromise<VoidResult> owner)
                {
                    _callback.Invoke(new Promise.Deferred(owner, owner.Id, owner.DeferredId));
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateNewPromiseResult<TResult> : IDelegateNew<TResult>
            {
                private readonly Action<Promise<TResult>.Deferred> _callback;

                [MethodImpl(InlineOption)]
                public DelegateNewPromiseResult(Action<Promise<TResult>.Deferred> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                void IDelegateNew<TResult>.Invoke(DeferredPromise<TResult> owner)
                {
                    _callback.Invoke(new Promise<TResult>.Deferred(owner, owner.Id, owner.DeferredId));
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateVoidVoid : IAction, IFunc<Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise,
                IDelegateReject, IDelegateRejectPromise, IDelegateRejectSynchronous, IDelegateRejectSynchronous<Promise>, IDelegateRun
            {
                private readonly Action _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateVoidVoid(Action callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    _callback.Invoke();
                }

                [MethodImpl(InlineOption)]
                Promise IFunc<Promise>.Invoke()
                {
                    Invoke();
                    return Promise.Resolved();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Invoke();
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Invoke();
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    Invoke();
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Invoke();
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateRun.Invoke(PromiseRefBase owner)
                {
                    Invoke();
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                bool IDelegateRejectSynchronous.TryInvokeRejecter(IRejectContainer rejectContainer)
                {
                    Invoke();
                    return true;
                }

                bool IDelegateRejectSynchronous<Promise>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise result)
                {
                    Invoke();
                    result = Promise.Resolved();
                    return true;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateVoidResult<TResult> : IFunc<TResult>, IFunc<Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise,
                IDelegateReject, IDelegateRejectPromise, IDelegateRejectSynchronous<TResult>, IDelegateRejectSynchronous<Promise<TResult>>, IDelegateRun
            {
                private readonly Func<TResult> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateVoidResult(Func<TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke()
                {
                    return _callback.Invoke();
                }

                [MethodImpl(InlineOption)]
                Promise<TResult> IFunc<Promise<TResult>>.Invoke()
                {
                    return new Promise<TResult>(Invoke());
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateRun.Invoke(PromiseRefBase owner)
                {
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                bool IDelegateRejectSynchronous<TResult>.TryInvokeRejecter(IRejectContainer rejectContainer, out TResult result)
                {
                    result = Invoke();
                    return true;
                }

                bool IDelegateRejectSynchronous<Promise<TResult>>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise<TResult> result)
                {
                    result = Promise.Resolved(Invoke());
                    return true;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateArgVoid<TArg> : IAction<TArg>, IFunc<TArg, Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise,
                IDelegateReject, IDelegateRejectPromise, IDelegateRejectSynchronous, IDelegateRejectSynchronous<Promise>
            {
                private readonly Action<TArg> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateArgVoid(Action<TArg> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(TArg arg)
                {
                    _callback.Invoke(arg);
                }

                [MethodImpl(InlineOption)]
                Promise IFunc<TArg, Promise>.Invoke(TArg arg)
                {
                    Invoke(arg);
                    return Promise.Resolved();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    Invoke(arg);
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    Invoke(arg);
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                private void InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        Invoke(arg);
                        owner.HandleNextInternal(Promise.State.Resolved);
                    }
                    else
                    {
                        owner._rejectContainer = rejectContainer;
                        owner.HandleNextInternal(Promise.State.Rejected);
                    }
                }

                void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    InvokeRejecter(rejectContainer, owner);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    InvokeRejecter(rejectContainer, owner);
                }

                bool IDelegateRejectSynchronous.TryInvokeRejecter(IRejectContainer rejectContainer)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        Invoke(arg);
                        return true;
                    }
                    return false;
                }

                bool IDelegateRejectSynchronous<Promise>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise result)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        Invoke(arg);
                        result = Promise.Resolved();
                        return true;
                    }
                    result = default;
                    return false;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateArgResult<TArg, TResult> : IFunc<TArg, TResult>, IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise,
                IDelegateReject, IDelegateRejectPromise, IDelegateRejectSynchronous<TResult>, IDelegateRejectSynchronous<Promise<TResult>>
            {
                private readonly Func<TArg, TResult> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateArgResult(Func<TArg, TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg arg)
                {
                    return _callback.Invoke(arg);
                }

                [MethodImpl(InlineOption)]
                Promise<TResult> IFunc<TArg, Promise<TResult>>.Invoke(TArg arg)
                {
                    return new Promise<TResult>(Invoke(arg));
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    TResult result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    TResult result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                private void InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                        owner.HandleNextInternal(Promise.State.Resolved);
                    }
                    else
                    {
                        owner._rejectContainer = rejectContainer;
                        owner.HandleNextInternal(Promise.State.Rejected);
                    }
                }

                void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    InvokeRejecter(rejectContainer, owner);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    InvokeRejecter(rejectContainer, owner);
                }

                bool IDelegateRejectSynchronous<TResult>.TryInvokeRejecter(IRejectContainer rejectContainer, out TResult result)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        result = Invoke(arg);
                        return true;
                    }
                    result = default;
                    return false;
                }

                bool IDelegateRejectSynchronous<Promise<TResult>>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise<TResult> result)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        result = Promise.Resolved(Invoke(arg));
                        return true;
                    }
                    result = default;
                    return false;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegatePromiseVoidVoid : IFunc<Promise>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise, IDelegateRejectSynchronous<Promise>, IDelegateRunPromise
            {
                private readonly Func<Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegatePromiseVoidVoid(Func<Promise> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise Invoke()
                {
                    return _callback.Invoke();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Promise result = Invoke();
                    owner.WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Promise result = Invoke();
                    owner.WaitFor(result, handler);
                }

                [MethodImpl(InlineOption)]
                void IDelegateRunPromise.Invoke(PromiseRefBase owner)
                {
                    Promise result = Invoke();
                    owner.WaitFor(result, null);
                }

                bool IDelegateRejectSynchronous<Promise>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise result)
                {
                    result = Invoke();
                    return true;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegatePromiseVoidResult<TResult> : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise, IDelegateRejectSynchronous<Promise<TResult>>, IDelegateRunPromise
            {
                private readonly Func<Promise<TResult>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegatePromiseVoidResult(Func<Promise<TResult>> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke()
                {
                    return _callback.Invoke();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Promise<TResult> result = Invoke();
                    owner.WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Promise<TResult> result = Invoke();
                    owner.WaitFor(result, handler);
                }

                [MethodImpl(InlineOption)]
                void IDelegateRunPromise.Invoke(PromiseRefBase owner)
                {
                    Promise<TResult> result = Invoke();
                    owner.WaitFor(result, null);
                }

                bool IDelegateRejectSynchronous<Promise<TResult>>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise<TResult> result)
                {
                    result = Invoke();
                    return true;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegatePromiseArgVoid<TArg> : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise, IDelegateRejectSynchronous<Promise>
            {
                private readonly Func<TArg, Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegatePromiseArgVoid(Func<TArg, Promise> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise Invoke(TArg arg)
                {
                    return _callback.Invoke(arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    Promise result = Invoke(arg);
                    owner.WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        Promise result = Invoke(arg);
                        owner.WaitFor(result, handler);
                    }
                    else
                    {
                        owner._rejectContainer = rejectContainer;
                        owner.HandleNextInternal(Promise.State.Rejected);
                    }
                }

                bool IDelegateRejectSynchronous<Promise>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise result)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        result = Invoke(arg);
                        return true;
                    }
                    result = default;
                    return false;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegatePromiseArgResult<TArg, TResult> : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise, IDelegateRejectSynchronous<Promise<TResult>>
            {
                private readonly Func<TArg, Promise<TResult>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegatePromiseArgResult(Func<TArg, Promise<TResult>> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(TArg arg)
                {
                    return _callback.Invoke(arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    Promise<TResult> result = Invoke(arg);
                    owner.WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        Promise<TResult> result = Invoke(arg);
                        owner.WaitFor(result, handler);
                    }
                    else
                    {
                        owner._rejectContainer = rejectContainer;
                        owner.HandleNextInternal(Promise.State.Rejected);
                    }
                }

                bool IDelegateRejectSynchronous<Promise<TResult>>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise<TResult> result)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        result = Invoke(arg);
                        return true;
                    }
                    result = default;
                    return false;
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueVoidVoid : IAction<Promise.ResultContainer>, IDelegateContinue
            {
                private readonly Promise.ContinueAction _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueVoidVoid(Promise.ContinueAction callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(Promise.ResultContainer resultContainer)
                {
                    _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise.ResultContainer(rejectContainer, state);
                    handler.MaybeDispose();
                    Invoke(resultContainer);
                    owner.HandleNextInternal(Promise.State.Resolved);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueVoidResult<TResult> : IFunc<Promise.ResultContainer, TResult>, IDelegateContinue
            {
                private readonly Promise.ContinueFunc<TResult> _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueVoidResult(Promise.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise.ResultContainer(rejectContainer, state);
                    handler.MaybeDispose();
                    TResult result = Invoke(resultContainer);
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueArgVoid<TArg> : IAction<Promise<TArg>.ResultContainer>, IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueAction _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueArgVoid(Promise<TArg>.ContinueAction callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                    handler.MaybeDispose();
                    Invoke(resultContainer);
                    owner.HandleNextInternal(Promise.State.Resolved);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueArgResult<TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, TResult>, IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueFunc<TResult> _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueArgResult(Promise<TArg>.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                    handler.MaybeDispose();
                    TResult result = Invoke(resultContainer);
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseVoidVoid : IFunc<Promise.ResultContainer, Promise>, IDelegateContinuePromise
            {
                private readonly Promise.ContinueFunc<Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseVoidVoid(Promise.ContinueFunc<Promise> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise.ResultContainer(rejectContainer, state);
                    handler.MaybeDispose();
                    Promise result = Invoke(resultContainer);
                    owner.WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseVoidResult<TResult> : IFunc<Promise.ResultContainer, Promise<TResult>>, IDelegateContinuePromise
            {
                private readonly Promise.ContinueFunc<Promise<TResult>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseVoidResult(Promise.ContinueFunc<Promise<TResult>> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise.ResultContainer(rejectContainer, state);
                    handler.MaybeDispose();
                    Promise<TResult> result = Invoke(resultContainer);
                    owner.WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseArgVoid<TArg> : IFunc<Promise<TArg>.ResultContainer, Promise>, IDelegateContinuePromise
            {
                private readonly Promise<TArg>.ContinueFunc<Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseArgVoid(Promise<TArg>.ContinueFunc<Promise> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                    handler.MaybeDispose();
                    Promise result = Invoke(resultContainer);
                    owner.WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseArgResult<TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>, IDelegateContinuePromise
            {
                private readonly Promise<TArg>.ContinueFunc<Promise<TResult>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseArgResult(Promise<TArg>.ContinueFunc<Promise<TResult>> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                    handler.MaybeDispose();
                    Promise<TResult> result = Invoke(resultContainer);
                    owner.WaitFor(result, handler);
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateFinally : IAction
            {
                private readonly Action _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateFinally(Action callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    _callback.Invoke();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCancel : IAction
            {
                private readonly Action _callback;

                [MethodImpl(InlineOption)]
                public DelegateCancel(Action callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    _callback.Invoke();
                }
            }
            #endregion

            #region Delegates with capture value

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateNewPromiseCaptureVoid<TCapture> : IDelegateNew<VoidResult>
            {
                private readonly Action<TCapture, Promise.Deferred> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateNewPromiseCaptureVoid(in TCapture capturedValue, Action<TCapture, Promise.Deferred> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                void IDelegateNew<VoidResult>.Invoke(DeferredPromise<VoidResult> owner)
                {
                    _callback.Invoke(_capturedValue, new Promise.Deferred(owner, owner.Id, owner.DeferredId));
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateNewPromiseCaptureResult<TCapture, TResult> : IDelegateNew<TResult>
            {
                private readonly Action<TCapture, Promise<TResult>.Deferred> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateNewPromiseCaptureResult(in TCapture capturedValue, Action<TCapture, Promise<TResult>.Deferred> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                void IDelegateNew<TResult>.Invoke(DeferredPromise<TResult> owner)
                {
                    _callback.Invoke(_capturedValue, new Promise<TResult>.Deferred(owner, owner.Id, owner.DeferredId));
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCaptureVoidVoid<TCapture> : IAction, IFunc<Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise,
                IDelegateReject, IDelegateRejectPromise, IDelegateRejectSynchronous, IDelegateRejectSynchronous<Promise>, IDelegateRun
            {
                private readonly Action<TCapture> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureVoidVoid(in TCapture capturedValue, Action<TCapture> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    _callback.Invoke(_capturedValue);
                }

                [MethodImpl(InlineOption)]
                Promise IFunc<Promise>.Invoke()
                {
                    Invoke();
                    return Promise.Resolved();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Invoke();
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Invoke();
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    Invoke();
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Invoke();
                    owner.HandleNextInternal(Promise.State.Resolved);
                }
                
                [MethodImpl(InlineOption)]
                void IDelegateRun.Invoke(PromiseRefBase owner)
                {
                    Invoke();
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                bool IDelegateRejectSynchronous.TryInvokeRejecter(IRejectContainer rejectContainer)
                {
                    Invoke();
                    return true;
                }

                bool IDelegateRejectSynchronous<Promise>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise result)
                {
                    Invoke();
                    result = Promise.Resolved();
                    return true;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCaptureVoidResult<TCapture, TResult> : IFunc<TResult>, IFunc<Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise,
                IDelegateReject, IDelegateRejectPromise, IDelegateRejectSynchronous<TResult>, IDelegateRejectSynchronous<Promise<TResult>>, IDelegateRun
            {
                private readonly Func<TCapture, TResult> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureVoidResult(in TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke()
                {
                    return _callback.Invoke(_capturedValue);
                }

                [MethodImpl(InlineOption)]
                Promise<TResult> IFunc<Promise<TResult>>.Invoke()
                {
                    return new Promise<TResult>(Invoke());
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateRun.Invoke(PromiseRefBase owner)
                {
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                bool IDelegateRejectSynchronous<TResult>.TryInvokeRejecter(IRejectContainer rejectContainer, out TResult result)
                {
                    result = Invoke();
                    return true;
                }

                bool IDelegateRejectSynchronous<Promise<TResult>>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise<TResult> result)
                {
                    result = Promise.Resolved(Invoke());
                    return true;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCaptureArgVoid<TCapture, TArg> : IAction<TArg>, IFunc<TArg, Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise,
                IDelegateReject, IDelegateRejectPromise, IDelegateRejectSynchronous, IDelegateRejectSynchronous<Promise>
            {
                private readonly Action<TCapture, TArg> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureArgVoid(in TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(TArg arg)
                {
                    _callback.Invoke(_capturedValue, arg);
                }

                [MethodImpl(InlineOption)]
                Promise IFunc<TArg, Promise>.Invoke(TArg arg)
                {
                    Invoke(arg);
                    return Promise.Resolved();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    Invoke(arg);
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    Invoke(arg);
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                private void InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        Invoke(arg);
                        owner.HandleNextInternal(Promise.State.Resolved);
                    }
                    else
                    {
                        owner._rejectContainer = rejectContainer;
                        owner.HandleNextInternal(Promise.State.Rejected);
                    }
                }

                void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    InvokeRejecter(rejectContainer, owner);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    InvokeRejecter(rejectContainer, owner);
                }

                bool IDelegateRejectSynchronous.TryInvokeRejecter(IRejectContainer rejectContainer)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        Invoke(arg);
                        return true;
                    }
                    return false;
                }

                bool IDelegateRejectSynchronous<Promise>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise result)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        Invoke(arg);
                        result = Promise.Resolved();
                        return true;
                    }
                    result = default;
                    return false;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCaptureArgResult<TCapture, TArg, TResult> : IFunc<TArg, TResult>, IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise,
                IDelegateReject, IDelegateRejectPromise, IDelegateRejectSynchronous<TResult>, IDelegateRejectSynchronous<Promise<TResult>>
            {
                private readonly Func<TCapture, TArg, TResult> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureArgResult(in TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg arg)
                {
                    return _callback.Invoke(_capturedValue, arg);
                }

                [MethodImpl(InlineOption)]
                Promise<TResult> IFunc<TArg, Promise<TResult>>.Invoke(TArg arg)
                {
                    return new Promise<TResult>(Invoke(arg));
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    TResult result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    TResult result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }

                private void InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                        owner.HandleNextInternal(Promise.State.Resolved);
                    }
                    else
                    {
                        owner._rejectContainer = rejectContainer;
                        owner.HandleNextInternal(Promise.State.Rejected);
                    }
                }

                void IDelegateReject.InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    InvokeRejecter(rejectContainer, owner);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    InvokeRejecter(rejectContainer, owner);
                }

                bool IDelegateRejectSynchronous<TResult>.TryInvokeRejecter(IRejectContainer rejectContainer, out TResult result)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        result = Invoke(arg);
                        return true;
                    }
                    result = default;
                    return false;
                }

                bool IDelegateRejectSynchronous<Promise<TResult>>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise<TResult> result)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        result = Promise.Resolved(Invoke(arg));
                        return true;
                    }
                    result = default;
                    return false;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCapturePromiseVoidVoid<TCapture> : IFunc<Promise>, IDelegateResolveOrCancelPromise,
                IDelegateRejectPromise, IDelegateRejectSynchronous<Promise>, IDelegateRunPromise
            {
                private readonly Func<TCapture, Promise> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCapturePromiseVoidVoid(in TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise Invoke()
                {
                    return _callback.Invoke(_capturedValue);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Promise result = Invoke();
                    owner.WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Promise result = Invoke();
                    owner.WaitFor(result, handler);
                }
                
                [MethodImpl(InlineOption)]
                void IDelegateRunPromise.Invoke(PromiseRefBase owner)
                {
                    Promise result = Invoke();
                    owner.WaitFor(result, null);
                }

                bool IDelegateRejectSynchronous<Promise>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise result)
                {
                    result = Invoke();
                    return true;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCapturePromiseVoidResult<TCapture, TResult> : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise,
                IDelegateRejectPromise, IDelegateRejectSynchronous<Promise<TResult>>, IDelegateRunPromise
            {
                private readonly Func<TCapture, Promise<TResult>> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCapturePromiseVoidResult(in TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke()
                {
                    return _callback.Invoke(_capturedValue);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Promise<TResult> result = Invoke();
                    owner.WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Promise<TResult> result = Invoke();
                    owner.WaitFor(result, handler);
                }
                
                [MethodImpl(InlineOption)]
                void IDelegateRunPromise.Invoke(PromiseRefBase owner)
                {
                    Promise<TResult> result = Invoke();
                    owner.WaitFor(result, null);
                }

                bool IDelegateRejectSynchronous<Promise<TResult>>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise<TResult> result)
                {
                    result = Invoke();
                    return true;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCapturePromiseArgVoid<TCapture, TArg> : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise,
                IDelegateRejectPromise, IDelegateRejectSynchronous<Promise>
            {
                private readonly Func<TCapture, TArg, Promise> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCapturePromiseArgVoid(in TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise Invoke(TArg arg)
                {
                    return _callback.Invoke(_capturedValue, arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    Promise result = Invoke(arg);
                    owner.WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        Promise result = Invoke(arg);
                        owner.WaitFor(result, handler);
                    }
                    else
                    {
                        owner._rejectContainer = rejectContainer;
                        owner.HandleNextInternal(Promise.State.Rejected);
                    }
                }

                bool IDelegateRejectSynchronous<Promise>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise result)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        result = Invoke(arg);
                        return true;
                    }
                    result = default;
                    return false;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCapturePromiseArgResult<TCapture, TArg, TResult> : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise,
                IDelegateRejectPromise, IDelegateRejectSynchronous<Promise<TResult>>
            {
                private readonly Func<TCapture, TArg, Promise<TResult>> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCapturePromiseArgResult(in TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(TArg arg)
                {
                    return _callback.Invoke(_capturedValue, arg);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    Promise<TResult> result = Invoke(arg);
                    owner.WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        Promise<TResult> result = Invoke(arg);
                        owner.WaitFor(result, handler);
                    }
                    else
                    {
                        owner._rejectContainer = rejectContainer;
                        owner.HandleNextInternal(Promise.State.Rejected);
                    }
                }

                bool IDelegateRejectSynchronous<Promise<TResult>>.TryInvokeRejecter(IRejectContainer rejectContainer, out Promise<TResult> result)
                {
                    TArg arg;
                    if (rejectContainer.TryGetValue(out arg))
                    {
                        result = Invoke(arg);
                        return true;
                    }
                    result = default;
                    return false;
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueCaptureVoidVoid<TCapture> : IAction<Promise.ResultContainer>, IDelegateContinue
            {
                private readonly Promise.ContinueAction<TCapture> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureVoidVoid(in TCapture capturedValue, Promise.ContinueAction<TCapture> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(Promise.ResultContainer resultContainer)
                {
                    _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise.ResultContainer(rejectContainer, state);
                    handler.MaybeDispose();
                    Invoke(resultContainer);
                    owner.HandleNextInternal(Promise.State.Resolved);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueCaptureVoidResult<TCapture, TResult> : IFunc<Promise.ResultContainer, TResult>, IDelegateContinue
            {
                private readonly Promise.ContinueFunc<TCapture, TResult> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureVoidResult(in TCapture capturedValue, Promise.ContinueFunc<TCapture, TResult> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise.ResultContainer(rejectContainer, state);
                    handler.MaybeDispose();
                    TResult result = Invoke(resultContainer);
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueCaptureArgVoid<TCapture, TArg> : IAction<Promise<TArg>.ResultContainer>, IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueAction<TCapture> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureArgVoid(in TCapture capturedValue, Promise<TArg>.ContinueAction<TCapture> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                    handler.MaybeDispose();
                    Invoke(resultContainer);
                    owner.HandleNextInternal(Promise.State.Resolved);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueCaptureArgResult<TCapture, TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, TResult>, IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueFunc<TCapture, TResult> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureArgResult(in TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, TResult> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                    handler.MaybeDispose();
                    TResult result = Invoke(resultContainer);
                    owner.UnsafeAs<PromiseRef<TResult>>()._result = result;
                    owner.HandleNextInternal(Promise.State.Resolved);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseCaptureVoidVoid<TCapture> : IFunc<Promise.ResultContainer, Promise>, IDelegateContinuePromise
            {
                private readonly Promise.ContinueFunc<TCapture, Promise> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseCaptureVoidVoid(in TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise.ResultContainer(rejectContainer, state);
                    handler.MaybeDispose();
                    Promise result = Invoke(resultContainer);
                    owner.WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseCaptureVoidResult<TCapture, TResult> : IFunc<Promise.ResultContainer, Promise<TResult>>, IDelegateContinuePromise
            {
                private readonly Promise.ContinueFunc<TCapture, Promise<TResult>> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseCaptureVoidResult(in TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise.ResultContainer(rejectContainer, state);
                    handler.MaybeDispose();
                    Promise<TResult> result = Invoke(resultContainer);
                    owner.WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseCaptureArgVoid<TCapture, TArg> : IFunc<Promise<TArg>.ResultContainer, Promise>, IDelegateContinuePromise
            {
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseCaptureArgVoid(in TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                    handler.MaybeDispose();
                    Promise result = Invoke(resultContainer);
                    owner.WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseCaptureArgResult<TCapture, TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>, IDelegateContinuePromise
            {
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseCaptureArgResult(in TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner)
                {
                    var resultContainer = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                    handler.MaybeDispose();
                    Promise<TResult> result = Invoke(resultContainer);
                    owner.WaitFor(result, handler);
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCaptureFinally<TCapture> : IAction
            {
                private readonly Action<TCapture> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureFinally(in TCapture capturedValue, Action<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    _callback.Invoke(_capturedValue);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCaptureCancel<TCapture> : IAction
            {
                private readonly Action<TCapture> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateCaptureCancel(in TCapture capturedValue, Action<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    _callback.Invoke(_capturedValue);
                }
            }
            #endregion



#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct Func2ArgResult<TArg1, TArg2, TResult> : IFunc<TArg1, TArg2, TResult>
            {
                private readonly Func<TArg1, TArg2, TResult> _callback;

                [MethodImpl(InlineOption)]
                public Func2ArgResult(Func<TArg1, TArg2, TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg1 arg1, TArg2 arg2)
                {
                    return _callback.Invoke(arg1, arg2);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct Func2ArgResultCapture<TCapture, TArg1, TArg2, TResult> : IFunc<TArg1, TArg2, TResult>
            {
                private readonly Func<TCapture, TArg1, TArg2, TResult> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public Func2ArgResultCapture(in TCapture capturedValue, Func<TCapture, TArg1, TArg2, TResult> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg1 arg1, TArg2 arg2)
                {
                    return _callback.Invoke(_capturedValue, arg1, arg2);
                }
            }
        }
    }
}