#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

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
                public static DelegateResolvePassthrough CreatePassthrough()
                {
                    return new DelegateResolvePassthrough(true);
                }

                [MethodImpl(InlineOption)]
                public static DelegateResolvePassthrough<TResult> CreatePassthrough<TResult>()
                {
                    return new DelegateResolvePassthrough<TResult>(true);
                }

                [MethodImpl(InlineOption)]
                public static DelegateVoidVoid Create(Action callback)
                {
                    return new DelegateVoidVoid(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateVoidResult<TResult> Create<TResult>(Func<TResult> callback)
                {
                    return new DelegateVoidResult<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateArgVoid<TArg> Create<TArg>(Action<TArg> callback)
                {
                    return new DelegateArgVoid<TArg>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateArgResult<TArg, TResult> Create<TArg, TResult>(Func<TArg, TResult> callback)
                {
                    return new DelegateArgResult<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromiseVoidVoid Create(Func<Promise> callback)
                {
                    return new DelegatePromiseVoidVoid(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromiseVoidResult<TResult> Create<TResult>(Func<Promise<TResult>> callback)
                {
                    return new DelegatePromiseVoidResult<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromiseArgVoid<TArg> Create<TArg>(Func<TArg, Promise> callback)
                {
                    return new DelegatePromiseArgVoid<TArg>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegatePromiseArgResult<TArg, TResult> Create<TArg, TResult>(Func<TArg, Promise<TResult>> callback)
                {
                    return new DelegatePromiseArgResult<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureVoidVoid<TCapture> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture> callback)
                {
                    return new DelegateCaptureVoidVoid<TCapture>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    return new DelegateCaptureVoidResult<TCapture, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    return new DelegateCaptureArgVoid<TCapture, TArg>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    return new DelegateCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCapturePromiseVoidVoid<TCapture> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    return new DelegateCapturePromiseVoidVoid<TCapture>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCapturePromiseVoidResult<TCapture, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateCapturePromiseVoidResult<TCapture, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCapturePromiseArgVoid<TCapture, TArg> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    return new DelegateCapturePromiseArgVoid<TCapture, TArg>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCapturePromiseArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
                {
                    return new DelegateCapturePromiseArgResult<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueVoidVoid Create(Promise.ContinueAction callback)
                {
                    return new DelegateContinueVoidVoid(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueVoidResult<TResult> Create<TResult>(Promise.ContinueFunc<TResult> callback)
                {
                    return new DelegateContinueVoidResult<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueArgVoid<TArg> Create<TArg>(Promise<TArg>.ContinueAction callback)
                {
                    return new DelegateContinueArgVoid<TArg>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueArgResult<TArg, TResult> Create<TArg, TResult>(Promise<TArg>.ContinueFunc<TResult> callback)
                {
                    return new DelegateContinueArgResult<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueCaptureVoidVoid<TCapture> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueAction<TCapture> callback)
                {
                    return new DelegateContinueCaptureVoidVoid<TCapture>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, TResult> callback)
                {
                    return new DelegateContinueCaptureVoidResult<TCapture, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueAction<TCapture> callback)
                {
                    return new DelegateContinueCaptureArgVoid<TCapture, TArg>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinueCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, TResult> callback)
                {
                    return new DelegateContinueCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseVoidVoid Create(Promise.ContinueFunc<Promise> callback)
                {
                    return new DelegateContinuePromiseVoidVoid(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseVoidResult<TResult> Create<TResult>(Promise.ContinueFunc<Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseVoidResult<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseArgVoid<TArg> Create<TArg>(Promise<TArg>.ContinueFunc<Promise> callback)
                {
                    return new DelegateContinuePromiseArgVoid<TArg>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseArgResult<TArg, TResult> Create<TArg, TResult>(Promise<TArg>.ContinueFunc<Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseArgResult<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseCaptureVoidVoid<TCapture> Create<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise> callback)
                {
                    return new DelegateContinuePromiseCaptureVoidVoid<TCapture>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseCaptureVoidResult<TCapture, TResult>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise> callback)
                {
                    return new DelegateContinuePromiseCaptureArgVoid<TCapture, TArg>(capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateContinuePromiseCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateContinuePromiseCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);
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
                    return new Promise();
                }

                private void Handle(PromiseRefBase handler, PromiseRefBase owner)
                {
                    handler.SuppressRejection = true;
                    owner._rejectContainer = handler._rejectContainer;
                    // Very important, write State must come after write _result and _rejectContainer. This is a volatile write, so we don't need a full memory barrier.
                    // State is checked for completion, and if it is read not pending on another thread, _result and _rejectContainer must have already been written so the other thread can read them.
                    owner.State = handler.State;
                    handler.MaybeDispose();
                    owner.HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Handle(handler, owner);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Handle(handler, owner);
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
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    owner.UnsafeAs<PromiseRef<TResult>>().HandleSelf(handler);
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    owner.UnsafeAs<PromiseRef<TResult>>().HandleSelf(handler);
                }
            }

            #region Regular Delegates

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateVoidVoid : IAction, IFunc<Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
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
                    return new Promise();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Invoke();
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Invoke();
                    MaybeDisposePreviousAfterSecondWait(handler);
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }

                void IDelegateReject.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Invoke();
                    handler.MaybeDispose();
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Invoke();
                    handler.MaybeDispose();
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateVoidResult<TResult> : IFunc<TResult>, IFunc<Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
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
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    TResult result = Invoke();
                    MaybeDisposePreviousAfterSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }

                void IDelegateReject.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TResult result = Invoke();
                    handler.MaybeDispose();
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TResult result = Invoke();
                    handler.MaybeDispose();
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateArgVoid<TArg> : IAction<TArg>, IFunc<TArg, Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
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
                    return new Promise();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    Invoke(arg);
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Invoke(arg);
                    MaybeDisposePreviousAfterSecondWait(handler);
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }

                private void InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        Invoke(arg);
                        handler.MaybeDispose();
                        owner.State = Promise.State.Resolved;
                        owner.HandleNextInternal();
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(handler);
                    }
                }

                void IDelegateReject.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    InvokeRejecter(handler, owner);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    InvokeRejecter(handler, owner);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateArgResult<TArg, TResult> : IFunc<TArg, TResult>, IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
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
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    TResult result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    TResult result = Invoke(arg);
                    MaybeDisposePreviousAfterSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }

                private void InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        handler.MaybeDispose();
                        owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                        owner.HandleNextInternal();
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(handler);
                    }
                }

                void IDelegateReject.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    InvokeRejecter(handler, owner);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    InvokeRejecter(handler, owner);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegatePromiseVoidVoid : IFunc<Promise>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
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
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Promise result = Invoke();
                    owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise result = Invoke();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegatePromiseVoidResult<TResult> : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
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
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Promise<TResult> result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise<TResult> result = Invoke();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegatePromiseArgVoid<TArg> : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
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
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Promise result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        Promise result = Invoke(arg);
                        MaybeDisposePreviousBeforeSecondWait(handler);
                        owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(handler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegatePromiseArgResult<TArg, TResult> : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
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
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Promise<TResult> result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        Promise<TResult> result = Invoke(arg);
                        MaybeDisposePreviousBeforeSecondWait(handler);
                        owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(handler);
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueVoidVoid : IAction, IDelegateContinue
            {
                private readonly Promise.ContinueAction _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueVoidVoid(Promise.ContinueAction callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private void Invoke(Promise.ResultContainer resultContainer)
                {
                    _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Invoke(new Promise.ResultContainer(handler));
                    handler.MaybeDispose();
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueVoidResult<TResult> : IFunc<TResult>, IDelegateContinue
            {
                private readonly Promise.ContinueFunc<TResult> _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueVoidResult(Promise.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke()
                {
                    return Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private TResult Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TResult result = Invoke(new Promise.ResultContainer(handler));
                    handler.MaybeDispose();
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueArgVoid<TArg> : IAction<TArg>, IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueAction _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueArgVoid(Promise<TArg>.ContinueAction callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(TArg arg)
                {
                    Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private void Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Invoke(new Promise<TArg>.ResultContainer(handler));
                    handler.MaybeDispose();
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueArgResult<TArg, TResult> : IFunc<TArg, TResult>, IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueFunc<TResult> _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueArgResult(Promise<TArg>.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg arg)
                {
                    return Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private TResult Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TResult result = Invoke(new Promise<TArg>.ResultContainer(handler));
                    handler.MaybeDispose();
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseVoidVoid : IFunc<Promise>, IDelegateContinuePromise
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
                public Promise Invoke()
                {
                    return Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private Promise Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise result = Invoke(new Promise.ResultContainer(handler));
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseVoidResult<TResult> : IFunc<Promise<TResult>>, IDelegateContinuePromise
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
                public Promise<TResult> Invoke()
                {
                    return Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private Promise<TResult> Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise<TResult> result = Invoke(new Promise.ResultContainer(handler));
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseArgVoid<TArg> : IFunc<TArg, Promise>, IDelegateContinuePromise
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
                public Promise Invoke(TArg arg)
                {
                    return Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private Promise Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise result = Invoke(new Promise<TArg>.ResultContainer(handler));
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseArgResult<TArg, TResult> : IFunc<TArg, Promise<TResult>>, IDelegateContinuePromise
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
                public Promise<TResult> Invoke(TArg arg)
                {
                    return Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private Promise<TResult> Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise<TResult> result = Invoke(new Promise<TArg>.ResultContainer(handler));
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
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

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateProgress : IProgress<float>
            {
                private readonly Action<float> _callback;

                [MethodImpl(InlineOption)]
                public DelegateProgress(Action<float> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Report(float value)
                {
                    _callback.Invoke(value);
                }
            }
            #endregion

            #region Delegates with capture value

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCaptureVoidVoid<TCapture> : IAction, IFunc<Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
            {
                private readonly Action<TCapture> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureVoidVoid(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture> callback)
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
                    return new Promise();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    Invoke();
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Invoke();
                    MaybeDisposePreviousAfterSecondWait(handler);
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }

                void IDelegateReject.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Invoke();
                    handler.MaybeDispose();
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Invoke();
                    handler.MaybeDispose();
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCaptureVoidResult<TCapture, TResult> : IFunc<TResult>, IFunc<Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
            {
                private readonly Func<TCapture, TResult> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureVoidResult(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TResult> callback)
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
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    handler.MaybeDispose();
                    TResult result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    TResult result = Invoke();
                    MaybeDisposePreviousAfterSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }

                void IDelegateReject.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TResult result = Invoke();
                    handler.MaybeDispose();
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TResult result = Invoke();
                    handler.MaybeDispose();
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCaptureArgVoid<TCapture, TArg> : IAction<TArg>, IFunc<TArg, Promise>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
            {
                private readonly Action<TCapture, TArg> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureArgVoid(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture, TArg> callback)
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
                    return new Promise();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    Invoke(arg);
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Invoke(arg);
                    MaybeDisposePreviousAfterSecondWait(handler);
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }

                private void InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        Invoke(arg);
                        handler.MaybeDispose();
                        owner.State = Promise.State.Resolved;
                        owner.HandleNextInternal();
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(handler);
                    }
                }

                void IDelegateReject.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    InvokeRejecter(handler, owner);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    InvokeRejecter(handler, owner);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCaptureArgResult<TCapture, TArg, TResult> : IFunc<TArg, TResult>, IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancel, IDelegateResolveOrCancelPromise, IDelegateReject, IDelegateRejectPromise
            {
                private readonly Func<TCapture, TArg, TResult> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureArgResult(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
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
                void IDelegateResolveOrCancel.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    handler.MaybeDispose();
                    TResult result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    TResult result = Invoke(arg);
                    MaybeDisposePreviousAfterSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }

                private void InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        TResult result = Invoke(arg);
                        handler.MaybeDispose();
                        owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                        owner.HandleNextInternal();
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(handler);
                    }
                }

                void IDelegateReject.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    InvokeRejecter(handler, owner);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    InvokeRejecter(handler, owner);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCapturePromiseVoidVoid<TCapture> : IFunc<Promise>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
            {
                private readonly Func<TCapture, Promise> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCapturePromiseVoidVoid(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, Promise> callback)
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
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Promise result = Invoke();
                    owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise result = Invoke();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCapturePromiseVoidResult<TCapture, TResult> : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
            {
                private readonly Func<TCapture, Promise<TResult>> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCapturePromiseVoidResult(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
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
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Promise<TResult> result = Invoke();
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise<TResult> result = Invoke();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCapturePromiseArgVoid<TCapture, TArg> : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
            {
                private readonly Func<TCapture, TArg, Promise> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCapturePromiseArgVoid(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
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
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Promise result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        Promise result = Invoke(arg);
                        MaybeDisposePreviousBeforeSecondWait(handler);
                        owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(handler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateCapturePromiseArgResult<TCapture, TArg, TResult> : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise, IDelegateRejectPromise
            {
                private readonly Func<TCapture, TArg, Promise<TResult>> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCapturePromiseArgResult(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
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
                void IDelegateResolveOrCancelPromise.InvokeResolver(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg = handler.GetResult<TArg>();
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    Promise<TResult> result = Invoke(arg);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
                }

                void IDelegateRejectPromise.InvokeRejecter(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TArg arg;
                    if (handler.TryGetRejectValue(out arg))
                    {
                        Promise<TResult> result = Invoke(arg);
                        MaybeDisposePreviousBeforeSecondWait(handler);
                        owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
                    }
                    else
                    {
                        owner.HandleIncompatibleRejection(handler);
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueCaptureVoidVoid<TCapture> : IAction, IDelegateContinue
            {
                private readonly Promise.ContinueAction<TCapture> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureVoidVoid(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueAction<TCapture> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public void Invoke()
                {
                    Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private void Invoke(Promise.ResultContainer resultContainer)
                {
                    _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Invoke(new Promise.ResultContainer(handler));
                    handler.MaybeDispose();
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueCaptureVoidResult<TCapture, TResult> : IFunc<TResult>, IDelegateContinue
            {
                private readonly Promise.ContinueFunc<TCapture, TResult> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureVoidResult(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, TResult> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke()
                {
                    return Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private TResult Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TResult result = Invoke(new Promise.ResultContainer(handler));
                    handler.MaybeDispose();
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueCaptureArgVoid<TCapture, TArg> : IAction<TArg>, IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueAction<TCapture> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureArgVoid(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueAction<TCapture> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(TArg arg)
                {
                    Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private void Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Invoke(new Promise<TArg>.ResultContainer(handler));
                    handler.MaybeDispose();
                    owner.State = Promise.State.Resolved;
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinueCaptureArgResult<TCapture, TArg, TResult> : IFunc<TArg, TResult>, IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueFunc<TCapture, TResult> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureArgResult(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, TResult> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg arg)
                {
                    return Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private TResult Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    TResult result = Invoke(new Promise<TArg>.ResultContainer(handler));
                    handler.MaybeDispose();
                    owner.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                    owner.HandleNextInternal();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseCaptureVoidVoid<TCapture> : IFunc<Promise>, IDelegateContinuePromise
            {
                private readonly Promise.ContinueFunc<TCapture, Promise> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseCaptureVoidVoid(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise Invoke()
                {
                    return Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private Promise Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise result = Invoke(new Promise.ResultContainer(handler));
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseCaptureVoidResult<TCapture, TResult> : IFunc<Promise<TResult>>, IDelegateContinuePromise
            {
                private readonly Promise.ContinueFunc<TCapture, Promise<TResult>> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseCaptureVoidResult(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke()
                {
                    return Invoke(new Promise.ResultContainer(null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private Promise<TResult> Invoke(Promise.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise<TResult> result = Invoke(new Promise.ResultContainer(handler));
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseCaptureArgVoid<TCapture, TArg> : IFunc<TArg, Promise>, IDelegateContinuePromise
            {
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseCaptureArgVoid(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise Invoke(TArg arg)
                {
                    return Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private Promise Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise result = Invoke(new Promise<TArg>.ResultContainer(handler));
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<VoidResult>>().WaitFor(result, handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal struct DelegateContinuePromiseCaptureArgResult<TCapture, TArg, TResult> : IFunc<TArg, Promise<TResult>>, IDelegateContinuePromise
            {
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinuePromiseCaptureArgResult(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise<TResult>> callback)
                {
                    _callback = callback;
                    _capturedValue = capturedValue;
                }

                [MethodImpl(InlineOption)]
                public Promise<TResult> Invoke(TArg arg)
                {
                    return Invoke(new Promise<TArg>.ResultContainer(arg, null, Promise.State.Resolved));
                }

                [MethodImpl(InlineOption)]
                private Promise<TResult> Invoke(Promise<TArg>.ResultContainer resultContainer)
                {
                    return _callback.Invoke(_capturedValue, resultContainer);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(PromiseRefBase handler, PromiseRefBase owner)
                {
                    Promise<TResult> result = Invoke(new Promise<TArg>.ResultContainer(handler));
                    MaybeDisposePreviousBeforeSecondWait(handler);
                    owner.UnsafeAs<PromiseRef<TResult>>().WaitFor(result, handler);
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
                public DelegateCaptureFinally(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture> callback)
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
                public DelegateCaptureCancel(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture> callback)
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
            internal struct DelegateCaptureProgress<TCapture> : IProgress<float>
            {
                private readonly Action<TCapture, float> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateCaptureProgress(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture, float> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Report(float value)
                {
                    _callback.Invoke(_capturedValue, value);
                }
            }
            #endregion
        }
    }
}