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

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        internal enum SynchronizationOption
        {
            Synchronous,
            Foreground,
            Background,
            Explicit
        }

        partial class PromiseRefBase
        {
            // These help reduce typed out generics.

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class CallbackHelperArg<TArg>
            {
                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackDirect<TDelegate>(TDelegate resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved) where TDelegate : IAction<TArg>
                {
                    try
                    {
                        if (resolved._ref == null)
                        {
                            resolver.Invoke(resolved._result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved._id);
                            resolver.Invoke(arg);
                        }
                        return CreateResolved(resolved.Depth);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException(e, resolved.Depth);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved, ushort nextDepth) where TDelegate : IFunc<TArg, Promise>
                {
                    try
                    {
                        Promise result;
                        if (resolved._ref == null)
                        {
                            result = resolver.Invoke(resolved._result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved._id);
                            result = resolver.Invoke(arg);
                        }
                        return CallbackHelperVoid.AdoptDirect(result, nextDepth);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TArg>(e, nextDepth);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolve<TDelegate>(Promise<TArg> _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IAction<TArg>, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolve<TArg, TDelegate>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolve<TArg, TDelegate>.GetOrCreate(resolver, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TArg>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveWait<TDelegate>(Promise<TArg> _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise
                {
                    ushort nextDepth = CallbackHelperVoid.GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolvePromise<VoidResult, TDelegate>.GetOrCreate(resolver, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolvePromise<VoidResult, TDelegate>.GetOrCreate(resolver, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveReject<TDelegateResolve, TDelegateReject>(Promise<TArg> _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IAction<TArg>, IDelegateResolveOrCancel
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveReject<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolveReject<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    ushort nextDepth = CallbackHelperVoid.GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveRejectPromise<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolveRejectPromise<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddContinue<TDelegateContinue>(Promise<TArg> _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IAction<TArg>, IDelegateContinue
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(continuer, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinue<VoidResult, TDelegateContinue>.GetOrCreate(continuer, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseContinue<VoidResult, TDelegateContinue>.GetOrCreate(continuer, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddContinueWait<TDelegateContinue>(Promise<TArg> _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<TArg, Promise>, IDelegateContinuePromise
                {
                    ushort nextDepth = CallbackHelperVoid.GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(continuer, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinuePromise<VoidResult, TDelegateContinue>.GetOrCreate(continuer, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseContinuePromise<VoidResult, TDelegateContinue>.GetOrCreate(continuer, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, nextDepth);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class CallbackHelperResult<TResult>
            {
                internal static Promise<TResult> Canceled(PromiseRefBase _ref, short promiseId, ushort depth)
                {
                    if (_ref != null)
                    {
                        _ref.MaybeMarkAwaitedAndDispose(promiseId);
                    }
                    var deferred = DeferredPromise<TResult>.GetOrCreate();
                    deferred.CancelDirect();
                    return new Promise<TResult>(deferred, deferred.Id, depth);
                }

                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackDirect<TDelegate>(TDelegate resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise resolved) where TDelegate : IFunc<TResult>
                {
                    try
                    {
                        if (resolved._ref != null)
                        {
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved._id);
                        }
                        TResult result = resolver.Invoke();
                        return CreateResolved(result, resolved.Depth);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e, resolved.Depth);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise resolved, ushort nextDepth) where TDelegate : IFunc<Promise<TResult>>
                {
                    try
                    {
                        Promise<TResult> result;
                        if (resolved._ref == null)
                        {
                            result = resolver.Invoke();
                        }
                        else
                        {
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved._id);
                            result = resolver.Invoke();
                        }
                        return CallbackHelperVoid.AdoptDirect(result, nextDepth);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e, nextDepth);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TDelegate>(Promise _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<TResult>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TDelegate>(Promise _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise
                {
                    ushort nextDepth = CallbackHelperVoid.GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TDelegateResolve, TDelegateReject>(Promise _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IFunc<TResult>, IDelegateResolveOrCancel
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveReject<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolveReject<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    ushort nextDepth = CallbackHelperVoid.GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveRejectPromise<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolveRejectPromise<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TDelegateContinue>(Promise _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<TResult>, IDelegateContinue
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(continuer, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinue<TResult, TDelegateContinue>.GetOrCreate(continuer, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseContinue<TResult, TDelegateContinue>.GetOrCreate(continuer, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TDelegateContinue>(Promise _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<Promise<TResult>>, IDelegateContinuePromise
                {
                    ushort nextDepth = CallbackHelperVoid.GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(continuer, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinuePromise<TResult, TDelegateContinue>.GetOrCreate(continuer, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseContinuePromise<TResult, TDelegateContinue>.GetOrCreate(continuer, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancel<TDelegateCancel>(Promise<TResult> _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, _this.Depth)
                            : CallbackHelperVoid.Duplicate(_this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseCancel<TResult, TDelegateCancel>.GetOrCreate(canceler, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseCancel<TResult, TDelegateCancel>.GetOrCreate(canceler, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancelWait<TDelegateCancel>(Promise<TResult> _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancelPromise
                {
                    ushort nextDepth = CallbackHelperVoid.GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id, nextDepth)
                            : new Promise<TResult>(_this._ref, _this._id, nextDepth, _this._result);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseCancelPromise<TResult, TDelegateCancel>.GetOrCreate(canceler, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseCancelPromise<TResult, TDelegateCancel>.GetOrCreate(canceler, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class CallbackHelper<TArg, TResult>
            {
                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackDirect<TDelegate>(TDelegate resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved) where TDelegate : IFunc<TArg, TResult>
                {
                    try
                    {
                        TResult result;
                        if (resolved._ref == null)
                        {
                            result = resolver.Invoke(resolved._result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved._id);
                            result = resolver.Invoke(arg);
                        }
                        return CreateResolved(result, resolved.Depth);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e, resolved.Depth);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved, ushort nextDepth) where TDelegate : IFunc<TArg, Promise<TResult>>
                {
                    try
                    {
                        Promise<TResult> result;
                        if (resolved._ref == null)
                        {
                            result = resolver.Invoke(resolved._result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved._id);
                            result = resolver.Invoke(arg);
                        }
                        return CallbackHelperVoid.AdoptDirect(result, nextDepth);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e, nextDepth);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TDelegate>(Promise<TArg> _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<TArg, TResult>, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolve<TResult, TDelegate>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolve<TResult, TDelegate>.GetOrCreate(resolver, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TDelegate>(Promise<TArg> _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise
                {
                    ushort nextDepth = CallbackHelperVoid.GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TDelegateResolve, TDelegateReject>(Promise<TArg> _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IFunc<TArg, TResult>, IDelegateResolveOrCancel
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveReject<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolveReject<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    ushort nextDepth = CallbackHelperVoid.GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveRejectPromise<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolveRejectPromise<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TDelegateContinue>(Promise<TArg> _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<TArg, TResult>, IDelegateContinue
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(continuer, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinue<TResult, TDelegateContinue>.GetOrCreate(continuer, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseContinue<TResult, TDelegateContinue>.GetOrCreate(continuer, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TDelegateContinue>(Promise<TArg> _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<TArg, Promise<TResult>>, IDelegateContinuePromise
                {
                    ushort nextDepth = CallbackHelperVoid.GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(continuer, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinuePromise<TResult, TDelegateContinue>.GetOrCreate(continuer, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseContinuePromise<TResult, TDelegateContinue>.GetOrCreate(continuer, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class CallbackHelperVoid
            {
                internal static Promise FromException(Exception e, ushort depth)
                {
                    if (e is OperationCanceledException)
                    {
                        var promise = Promise.Canceled();
                        return new Promise(promise._ref, promise._id, depth);
                    }
                    else
                    {
                        var promise = Promise.Rejected(e);
                        return new Promise(promise._ref, promise._id, depth);
                    }
                }

                internal static Promise<TResult> FromException<TResult>(Exception e, ushort depth)
                {
                    if (e is OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise._id, depth);
                    }
                    else
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise._id, depth);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackDirect<TDelegate>(TDelegate resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise resolved) where TDelegate : IAction
                {
                    try
                    {
                        if (resolved._ref != null)
                        {
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved._id);
                        }
                        resolver.Invoke();
                        return CreateResolved(resolved.Depth);
                    }
                    catch (Exception e)
                    {
                        return FromException(e, resolved.Depth);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise resolved, ushort nextDepth) where TDelegate : IFunc<Promise>
                {
                    try
                    {
                        if (resolved._ref != null)
                        {
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved._id);
                        }
                        Promise result = resolver.Invoke();
                        return AdoptDirect(result, nextDepth);
                    }
                    catch (Exception e)
                    {
                        return FromException(e, nextDepth);
                    }
                }

                internal static Promise Canceled(PromiseRefBase _ref, short promiseId, ushort depth)
                {
                    if (_ref != null)
                    {
                        _ref.MaybeMarkAwaitedAndDispose(promiseId);
                    }
                    var deferred = DeferredPromise<VoidResult>.GetOrCreate();
                    deferred.CancelDirect();
                    return new Promise(deferred, deferred.Id, depth);
                }

#if !PROMISE_PROGRESS
                [MethodImpl(InlineOption)]
#endif
                internal static Promise<TResult> AdoptDirect<TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TResult> promise, ushort nextDepth)
                {
#if !PROMISE_PROGRESS
                    return promise;
#else
                    var _ref = promise._ref;
                    if (_ref == null)
                    {
                        return new Promise<TResult>(null, 0, nextDepth, promise._result);
                    }
                    if (_ref.State == Promise.State.Resolved)
                    {
                        return CreateResolved(GetResultFromResolved(promise), nextDepth);
                    }
                    // Normalize progress. Passing a default resolver makes the Execute method adopt the promise's state without attempting to invoke.
                    var newRef = PromiseResolvePromise<TResult, DelegateResolvePassthrough<TResult>>.GetOrCreate(default(DelegateResolvePassthrough<TResult>), nextDepth);
                    newRef.WaitForWithProgress(_ref, promise._id);
                    return new Promise<TResult>(newRef, newRef.Id, nextDepth);
#endif
                }

#if !PROMISE_PROGRESS
                [MethodImpl(InlineOption)]
#endif
                internal static Promise AdoptDirect(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise promise, ushort nextDepth)
                {
#if !PROMISE_PROGRESS
                    return promise;
#else
                    var _ref = promise._ref;
                    if (_ref == null)
                    {
                        return new Promise(null, 0, nextDepth);
                    }
                    if (_ref.State == Promise.State.Resolved)
                    {
                        _ref.MaybeMarkAwaitedAndDispose(promise._id);
                        return CreateResolved(nextDepth);
                    }
                    // Normalize progress. Passing a default resolver makes the Execute method adopt the promise's state without attempting to invoke.
                    var newRef = PromiseResolvePromise<VoidResult, DelegateResolvePassthrough<VoidResult>>.GetOrCreate(default(DelegateResolvePassthrough<VoidResult>), nextDepth);
                    newRef.WaitForWithProgress(_ref, promise._id);
                    return new Promise(newRef, newRef.Id, nextDepth);
#endif
                }

                [MethodImpl(InlineOption)]
                private static TResult GetResultFromResolved<TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TResult> promise)
                {
                    if (promise._ref == null)
                    {
                        return promise._result;
                    }
                    var result = promise._ref.GetResult<TResult>();
                    promise._ref.MaybeMarkAwaitedAndDispose(promise._id);
                    return result;
                }

                internal static Promise Duplicate(Promise _this)
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }
                    var newRef = _this._ref.GetDuplicate(_this._id, _this.Depth);
                    return new Promise(newRef, newRef.Id, _this.Depth);
                }

                internal static Promise<TResult> Duplicate<TResult>(Promise<TResult> _this)
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }
                    var newRef = _this._ref.GetDuplicate(_this._id, _this.Depth);
                    return new Promise<TResult>(newRef, newRef.Id, _this.Depth, _this._result);
                }

                internal static Promise WaitAsync(Promise _this, SynchronizationOption continuationOption, SynchronizationContext synchronizationContext)
                {
                    switch (continuationOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            return Duplicate(_this);
                        }
                        case SynchronizationOption.Foreground:
                        {
                            synchronizationContext = Promise.Config.ForegroundContext;
                            if (synchronizationContext == null)
                            {
                                throw new InvalidOperationException(
                                    "SynchronizationOption.Foreground was provided, but Promise.Config.ForegroundContext was null. " +
                                    "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                                    GetFormattedStacktrace(2));
                            }
                            goto default;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext ?? BackgroundSynchronizationContextSentinel.s_instance;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            var newRef = _this._ref == null
                                ? PromiseConfigured<VoidResult>.GetOrCreateFromResolved(synchronizationContext, new VoidResult(), _this.Depth)
                                : _this._ref.GetConfigured(_this._id, synchronizationContext, _this.Depth);
                            return new Promise(newRef, newRef.Id, _this.Depth);
                        }
                    }
                }

                internal static Promise<TResult> WaitAsync<TResult>(Promise<TResult> _this, SynchronizationOption continuationOption, SynchronizationContext synchronizationContext)
                {
                    switch (continuationOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            return Duplicate(_this);
                        }
                        case SynchronizationOption.Foreground:
                        {
                            synchronizationContext = Promise.Config.ForegroundContext;
                            if (synchronizationContext == null)
                            {
                                throw new InvalidOperationException(
                                    "SynchronizationOption.Foreground was provided, but Promise.Config.ForegroundContext was null. " +
                                    "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                                    GetFormattedStacktrace(2));
                            }
                            goto default;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext ?? BackgroundSynchronizationContextSentinel.s_instance;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            var newRef = _this._ref == null
                                ? PromiseConfigured<TResult>.GetOrCreateFromResolved(synchronizationContext, _this._result, _this.Depth)
                                : _this._ref.GetConfigured(_this._id, synchronizationContext, _this.Depth);
                            return new Promise<TResult>(newRef, newRef.Id, _this.Depth, _this._result);
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolve<TDelegate>(Promise _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IAction, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolve<VoidResult, TDelegate>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolve<VoidResult, TDelegate>.GetOrCreate(resolver, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveWait<TDelegate>(Promise _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise>, IDelegateResolveOrCancelPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolvePromise<VoidResult, TDelegate>.GetOrCreate(resolver, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolvePromise<VoidResult, TDelegate>.GetOrCreate(resolver, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveReject<TDelegateResolve, TDelegateReject>(Promise _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IAction, IDelegateResolveOrCancel
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveReject<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolveReject<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IFunc<Promise>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveRejectPromise<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolveRejectPromise<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddContinue<TDelegateContinue>(Promise _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IAction, IDelegateContinue
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, _this.Depth)
                            : InvokeCallbackDirect(continuer, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinue<VoidResult, TDelegateContinue>.GetOrCreate(continuer, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseContinue<VoidResult, TDelegateContinue>.GetOrCreate(continuer, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddContinueWait<TDelegateContinue>(Promise _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<Promise>, IDelegateContinuePromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, nextDepth)
                            : InvokeCallbackAndAdoptDirect(continuer, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinuePromise<VoidResult, TDelegateContinue>.GetOrCreate(continuer, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseContinuePromise<VoidResult, TDelegateContinue>.GetOrCreate(continuer, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddFinally<TFinalizer>(Promise _this, TFinalizer finalizer)
                    where TFinalizer : IAction
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackDirect(finalizer, _this);
                    }
                    PromiseRefBase promise = PromiseFinally<VoidResult, TFinalizer>.GetOrCreate(finalizer, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddFinally<TResult, TFinalizer>(Promise<TResult> _this, TFinalizer finalizer)
                    where TFinalizer : IAction
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        TResult result = GetResultFromResolved(_this);
                        var p = InvokeCallbackDirect(finalizer, new Promise());
                        return new Promise<TResult>(p._ref, p._id, p.Depth, result);
                    }
                    PromiseRefBase promise = PromiseFinally<TResult, TFinalizer>.GetOrCreate(finalizer, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddCancel<TDelegateCancel>(Promise _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, _this.Depth)
                            : Duplicate(_this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseCancel<VoidResult, TDelegateCancel>.GetOrCreate(canceler, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseCancel<VoidResult, TDelegateCancel>.GetOrCreate(canceler, _this.Depth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddCancelWait<TDelegateCancel>(Promise _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancelPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id, nextDepth)
                            : new Promise(_this._ref, _this._id, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseCancelPromise<VoidResult, TDelegateCancel>.GetOrCreate(canceler, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseCancelPromise<VoidResult, TDelegateCancel>.GetOrCreate(canceler, nextDepth);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static ushort GetNextDepth(ushort depth)
                {
#if PROMISE_PROGRESS
                    return Fixed32.GetNextDepth(depth);
#elif PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // Depth is unused, but it can help with debugging.
                    unchecked
                    {
                        return (ushort) (depth + 1);
                    }
#else
                    return 0;
#endif
                }

#if PROMISE_PROGRESS
                internal static void InvokeAndCatchProgress<TProgress>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TProgress progress,
                    float value,
                    ITraceable traceable)
                    where TProgress : IProgress<float>
                {
                    SetCurrentInvoker(traceable);
                    try
                    {
                        progress.Report(value);
                    }
                    catch (Exception e)
                    {
                        ReportRejection(e, traceable);
                    }
                    ClearCurrentInvoker();
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddProgress<TProgress>(
                    Promise _this,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TProgress progress,
                    CancelationToken cancelationToken,
                    SynchronizationOption invokeOption,
                    SynchronizationContext synchronizationContext)
                    where TProgress : IProgress<float>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return WaitAsync(_this, invokeOption, synchronizationContext);
                    }

                    PromiseProgress<VoidResult, TProgress> promise;
                    switch (invokeOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                            {
                                if (_this._ref != null)
                                {
                                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                                }
                                InvokeAndCatchProgress(progress, 1, null);
                                return CreateResolved(_this.Depth);
                            }
                            break;
                        }
                        case SynchronizationOption.Foreground:
                        {
                            synchronizationContext = Promise.Config.ForegroundContext;
                            if (synchronizationContext == null)
                            {
                                throw new InvalidOperationException(
                                    "SynchronizationOption.Foreground was provided, but Promise.Config.ForegroundContext was null. " +
                                    "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                                    GetFormattedStacktrace(2));
                            }
                            goto default;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext ?? BackgroundSynchronizationContextSentinel.s_instance;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                            {
                                if (_this._ref != null)
                                {
                                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                                }
                                promise = PromiseProgress<VoidResult, TProgress>.GetOrCreateFromResolved(progress, cancelationToken, _this.Depth, synchronizationContext, new VoidResult());
                                ScheduleForHandle(promise, synchronizationContext);
                                return new Promise(promise, promise.Id, _this.Depth);
                            }
                            break;
                        }
                    }

                    promise = PromiseProgress<VoidResult, TProgress>.GetOrCreate(progress, cancelationToken, _this.Depth, invokeOption == SynchronizationOption.Synchronous, synchronizationContext);
#if PROMISE_DEBUG
                    promise._previous = _this._ref;
#endif
                    promise._smallFields._currentProgress = _this._ref._smallFields._currentProgress;
                    _this._ref.InterlockedIncrementProgressReportingCount();
                    HandleablePromiseBase previousWaiter;
                    PromiseRefBase promiseSingleAwait = _this._ref.AddWaiter(_this._id, promise, out previousWaiter);
                    if (previousWaiter == null)
                    {
                        promise.MaybeScheduleProgress();
                        _this._ref.InterlockedDecrementProgressReportingCount();
                        StackUnwindHelper.InvokeProgressors();
                    }
                    else
                    {
                        _this._ref.InterlockedDecrementProgressReportingCount();
                        if (!VerifyWaiter(promiseSingleAwait))
                        {
                            // We're throwing InvalidOperationException here, so we don't want the new object to also add exceptions from its finalizer.
                            Discard(promise);
                            throw new InvalidOperationException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", GetFormattedStacktrace(2));
                        }
                        _this._ref.HandleNext(promise);
                    }
                    return new Promise(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddProgress<TResult, TProgress>(
                    Promise<TResult> _this,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TProgress progress,
                    CancelationToken cancelationToken,
                    SynchronizationOption invokeOption,
                    SynchronizationContext synchronizationContext)
                    where TProgress : IProgress<float>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return WaitAsync(_this, invokeOption, synchronizationContext);
                    }

                    PromiseProgress<TResult, TProgress> promise;
                    switch (invokeOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                            {
                                TResult result = GetResultFromResolved(_this);
                                InvokeAndCatchProgress(progress, 1, null);
                                return CreateResolved(result, _this.Depth);
                            }
                            break;
                        }
                        case SynchronizationOption.Foreground:
                        {
                            synchronizationContext = Promise.Config.ForegroundContext;
                            if (synchronizationContext == null)
                            {
                                throw new InvalidOperationException(
                                    "SynchronizationOption.Foreground was provided, but Promise.Config.ForegroundContext was null. " +
                                    "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                                    GetFormattedStacktrace(2));
                            }
                            goto default;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext ?? BackgroundSynchronizationContextSentinel.s_instance;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                            {
                                TResult result = GetResultFromResolved(_this);
                                promise = PromiseProgress<TResult, TProgress>.GetOrCreateFromResolved(progress, cancelationToken, _this.Depth, synchronizationContext, result);
                                ScheduleForHandle(promise, synchronizationContext);
                                return new Promise<TResult>(promise, promise.Id, _this.Depth);
                            }
                            break;
                        }
                    }

                    promise = PromiseProgress<TResult, TProgress>.GetOrCreate(progress, cancelationToken, _this.Depth, invokeOption == SynchronizationOption.Synchronous, synchronizationContext);
#if PROMISE_DEBUG
                    promise._previous = _this._ref;
#endif
                    promise._smallFields._currentProgress = _this._ref._smallFields._currentProgress;
                    _this._ref.InterlockedIncrementProgressReportingCount();
                    HandleablePromiseBase previousWaiter;
                    PromiseRefBase promiseSingleAwait = _this._ref.AddWaiter(_this._id, promise, out previousWaiter);
                    if (previousWaiter == null)
                    {
                        promise.MaybeScheduleProgress();
                        _this._ref.InterlockedDecrementProgressReportingCount();
                        StackUnwindHelper.InvokeProgressors();
                    }
                    else
                    {
                        _this._ref.InterlockedDecrementProgressReportingCount();
                        if (!VerifyWaiter(promiseSingleAwait))
                        {
                            // We're throwing InvalidOperationException here, so we don't want the new object to also add exceptions from its finalizer.
                            Discard(promise);
                            throw new InvalidOperationException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", GetFormattedStacktrace(2));
                        }
                        _this._ref.HandleNext(promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }
#endif
            } // CallbackHelper
        } // PromiseRefBase
    } // Internal
}