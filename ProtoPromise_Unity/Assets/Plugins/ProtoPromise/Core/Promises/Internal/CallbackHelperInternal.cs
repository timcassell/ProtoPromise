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

        partial class PromiseRef
        {
            // NOTE: `where TResolver : IDelegate<TArg, TResult>` is much cleaner, but doesn't play well with AOT compilation (IL2CPP), making these repeated functions unfortunately necessary.
            // IDelegate<TArg, TResult> actually works properly with IL2CPP in Unity 2021.2, so we may switch to that implementation if we decide to drop support for older Unity versions (possibly when Unity finally adopts .Net 5+ runtime).

            private static class Invoker<TArg, TResult>
            {
                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackDirect(Delegate<TArg, TResult> resolver, Promise<TArg> resolved)
                {
                    try
                    {
                        TResult result = resolver.Invoke(resolved.Result);
                        return new Promise<TResult>(null, ValidIdFromApi, resolved.Depth, result);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth, promise.Result);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackDirect<TCapture>(DelegateCapture<TCapture, TArg, TResult> resolver, Promise<TArg> resolved)
                {
                    try
                    {
                        TResult result = resolver.Invoke(resolved.Result);
                        return new Promise<TResult>(null, ValidIdFromApi, resolved.Depth, result);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth, promise.Result);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect(DelegatePromise<TArg, TResult> resolver, Promise<TArg> resolved)
                {
                    ushort nextDepth = CallbackHelper.GetNextDepth(resolved.Depth);
                    try
                    {
                        return CallbackHelper.AdoptDirect(resolver.Invoke(resolved.Result), nextDepth);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, nextDepth, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, nextDepth, promise.Result);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect<TCapture>(DelegatePromiseCapture<TCapture, TArg, TResult> resolver, Promise<TArg> resolved)
                {
                    ushort nextDepth = CallbackHelper.GetNextDepth(resolved.Depth);
                    try
                    {
                        return CallbackHelper.AdoptDirect(resolver.Invoke(resolved.Result), nextDepth);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, nextDepth, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, nextDepth, promise.Result);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackDirect(DelegateContinue<TArg, TResult> resolver, Promise<TArg> resolved)
                {
                    try
                    {
                        TResult result = resolver.Invoke(resolved.Result);
                        return new Promise<TResult>(null, ValidIdFromApi, resolved.Depth, result);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth, promise.Result);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackDirect<TCapture>(DelegateContinueCapture<TCapture, TArg, TResult> resolver, Promise<TArg> resolved)
                {
                    try
                    {
                        TResult result = resolver.Invoke(resolved.Result);
                        return new Promise<TResult>(null, ValidIdFromApi, resolved.Depth, result);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth, promise.Result);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect(DelegateContinuePromise<TArg, TResult> resolver, Promise<TArg> resolved)
                {
                    ushort nextDepth = CallbackHelper.GetNextDepth(resolved.Depth);
                    try
                    {
                        return CallbackHelper.AdoptDirect(resolver.Invoke(resolved.Result), nextDepth);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, nextDepth, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, nextDepth, promise.Result);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect<TCapture>(DelegateContinuePromiseCapture<TCapture, TArg, TResult> resolver, Promise<TArg> resolved)
                {
                    ushort nextDepth = CallbackHelper.GetNextDepth(resolved.Depth);
                    try
                    {
                        return CallbackHelper.AdoptDirect(resolver.Invoke(resolved.Result), nextDepth);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, nextDepth, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, nextDepth, promise.Result);
                    }
                }
            }

            // This helps reduce typed out generics.

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class CallbackHelper
            {
                private static Promise<TResult> Canceled<TResult>(ushort depth)
                {
                    var deferred = DeferredPromise<TResult>.GetOrCreate();
                    deferred.CancelDirect();
                    return new Promise<TResult>(deferred, deferred.Id, depth);
                }

#if !PROMISE_PROGRESS
                [MethodImpl(InlineOption)]
#endif
                internal static Promise<TResult> AdoptDirect<TResult>(Promise<TResult> promise, ushort nextDepth)
                {
#if !PROMISE_PROGRESS
                    return promise;
#else
                    if (promise._ref == null)
                    {
                        return new Promise<TResult>(null, ValidIdFromApi, nextDepth, promise.Result);
                    }
                    // TODO
//#if !PROMISE_DEBUG
//                    if (promise._ref.State == Promise.State.Resolved)
//                    {
//                        promise._ref.MarkAwaited(promise.Id);
//                        TResult result = ((IValueContainer) promise._ref._valueOrPrevious).GetValue<TResult>();
//                        promise._ref.MaybeDispose();
//                        return new Promise<TResult>(null, ValidIdFromApi, nextDepth, result);
//                    }
//#endif
                    // Normalize progress. Passing a default resolver makes the Execute method adopt the promise's state without attempting to invoke.
                    var newRef = PromiseResolvePromise<DelegateResolvePassthrough>.GetOrCreate(default(DelegateResolvePassthrough), nextDepth);
                    newRef.WaitForWithProgress(promise);
                    return new Promise<TResult>(newRef, newRef.Id, nextDepth);
#endif
                }

                internal static Promise<TResult> Duplicate<TResult>(Promise<TResult> _this)
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }
                    var newRef = _this._ref.GetDuplicate(_this.Id, _this.Depth);
                    return new Promise<TResult>(newRef, newRef.Id, _this.Depth, _this.Result);
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
                            synchronizationContext = Promise.Config.BackgroundContext;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            var newRef = _this._ref == null
                                ? PromiseConfigured.GetOrCreateFromNull(synchronizationContext, _this.Result, _this.Depth)
                                : _this._ref.GetConfigured(_this.Id, synchronizationContext, _this.Depth);
                            return new Promise<TResult>(newRef, newRef.Id, _this.Depth, _this.Result);
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TArg, TResult>(Promise<TArg> _this, Delegate<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this.Depth)
                            : Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    // TODO: sync callback if ref-backed and already complete in RELEASE mode only.
                    // else if (_this._ref.State != Promise.State.Pending) { }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolve<Delegate<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                            : (PromiseRef) PromiseResolve<Delegate<TArg, TResult>>.GetOrCreate(resolver, _this.Depth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TArg, TResult>(Promise<TArg> _this, DelegatePromise<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(nextDepth)
                            : Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolvePromise<DelegatePromise<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth)
                            : (PromiseRef) PromiseResolvePromise<DelegatePromise<TArg, TResult>>.GetOrCreate(resolver, nextDepth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TCapture, TArg, TResult>(Promise<TArg> _this, DelegateCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this.Depth)
                            : Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    // TODO: sync callback if ref-backed and already complete in RELEASE mode only.
                    // else if (_this._ref.State != Promise.State.Pending) { }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolve<DelegateCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                            : (PromiseRef) PromiseResolve<DelegateCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, _this.Depth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TCapture, TArg, TResult>(Promise<TArg> _this, DelegatePromiseCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(nextDepth)
                            : Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolvePromise<DelegatePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth)
                            : (PromiseRef) PromiseResolvePromise<DelegatePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, nextDepth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TArgResolve, TDelegateReject, TResult>(Promise<TArgResolve> _this,
                    Delegate<TArgResolve, TResult> resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateReject
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this.Depth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolveReject<Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, _this.Depth)
                            : (PromiseRef) PromiseResolveReject<Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, _this.Depth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TCaptureResolve, TArgResolve, TDelegateReject, TResult>(Promise<TArgResolve> _this,
                    DelegateCapture<TCaptureResolve, TArgResolve, TResult> resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateReject
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this.Depth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolveReject<DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, _this.Depth)
                            : (PromiseRef) PromiseResolveReject<DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, _this.Depth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TArgResolve, TDelegateReject, TResult>(Promise<TArgResolve> _this,
                    Delegate<TArgResolve, TResult> resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateRejectPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(nextDepth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolveRejectPromise<Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                            : (PromiseRef) PromiseResolveRejectPromise<Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TCaptureResolve, TArgResolve, TDelegateReject, TResult>(Promise<TArgResolve> _this,
                    DelegateCapture<TCaptureResolve, TArgResolve, TResult> resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateRejectPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(nextDepth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolveRejectPromise<DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                            : (PromiseRef) PromiseResolveRejectPromise<DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TArgResolve, TDelegateReject, TResult>(Promise<TArgResolve> _this,
                    DelegatePromise<TArgResolve, TResult> resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateRejectPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(nextDepth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolveRejectPromise<DelegatePromise<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                            : (PromiseRef) PromiseResolveRejectPromise<DelegatePromise<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TCaptureResolve, TArgResolve, TDelegateReject, TResult>(Promise<TArgResolve> _this,
                    DelegatePromiseCapture<TCaptureResolve, TArgResolve, TResult> resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateRejectPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(nextDepth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolveRejectPromise<DelegatePromiseCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                            : (PromiseRef) PromiseResolveRejectPromise<DelegatePromiseCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TDelegateReject, TResult>(Promise<TResult> _this,
                    DelegateResolvePassthrough resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateReject
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this.Depth)
                            : _this;
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolveReject<DelegateResolvePassthrough, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, _this.Depth)
                            : (PromiseRef) PromiseResolveReject<DelegateResolvePassthrough, TDelegateReject>.GetOrCreate(resolver, rejecter, _this.Depth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TDelegateReject, TResult>(Promise<TResult> _this,
                    DelegateResolvePassthrough resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateRejectPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(nextDepth)
                            : new Promise<TResult>(_this._ref, _this.Id, nextDepth);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseResolveRejectPromise<DelegateResolvePassthrough, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                            : (PromiseRef) PromiseResolveRejectPromise<DelegateResolvePassthrough, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TArg, TResult>(Promise<TArg> _this, DelegateContinue<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this.Depth)
                            : Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseContinue<DelegateContinue<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                            : (PromiseRef) PromiseContinue<DelegateContinue<TArg, TResult>>.GetOrCreate(resolver, _this.Depth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TArg, TResult>(Promise<TArg> _this, DelegateContinuePromise<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(nextDepth)
                            : Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseContinuePromise<DelegateContinuePromise<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth)
                            : (PromiseRef) PromiseContinuePromise<DelegateContinuePromise<TArg, TResult>>.GetOrCreate(resolver, nextDepth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TCapture, TArg, TResult>(Promise<TArg> _this, DelegateContinueCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this.Depth)
                            : Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseContinue<DelegateContinueCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                            : (PromiseRef) PromiseContinue<DelegateContinueCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, _this.Depth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TCapture, TArg, TResult>(Promise<TArg> _this, DelegateContinuePromiseCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(nextDepth)
                            : Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseContinuePromise<DelegateContinuePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth)
                            : (PromiseRef) PromiseContinuePromise<DelegateContinuePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, nextDepth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddFinally<TResult>(Promise<TResult> _this, Action onFinally)
                {
                    if (_this._ref == null)
                    {
                        var p = Invoker<VoidResult, VoidResult>.InvokeCallbackDirect(DelegateWrapper.Create(onFinally), _this.AsPromise()._target);
                        return new Promise<TResult>(p._ref, p.Id, p.Depth, _this.Result);
                    }
                    PromiseRef promise = PromiseFinally<DelegateFinally>.GetOrCreate(new DelegateFinally(onFinally), _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddFinally<TCapture, TResult>(Promise<TResult> _this,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture> onFinally)
                {
                    if (_this._ref == null)
                    {
                        var p = Invoker<VoidResult, VoidResult>.InvokeCallbackDirect(DelegateWrapper.Create(capturedValue, onFinally), _this.AsPromise()._target);
                        return new Promise<TResult>(p._ref, p.Id, p.Depth, _this.Result);
                    }
                    PromiseRef promise = PromiseFinally<DelegateCaptureFinally<TCapture>>.GetOrCreate(new DelegateCaptureFinally<TCapture>(capturedValue, onFinally), _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancel<TDelegateCancel, TResult>(Promise<TResult> _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancel
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this.Depth)
                            : _this;
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseCancel<TDelegateCancel>.GetOrCreate(canceler, cancelationToken, _this.Depth)
                            : (PromiseRef) PromiseCancel<TDelegateCancel>.GetOrCreate(canceler, _this.Depth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancelWait<TDelegateCancel, TResult>(Promise<TResult> _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancelPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(nextDepth)
                            : new Promise<TResult>(_this._ref, _this.Id, nextDepth, _this.Result);
                    }
                    else
                    {
                        promise = cancelationToken.CanBeCanceled
                            ? CancelablePromiseCancelPromise<TDelegateCancel>.GetOrCreate(canceler, cancelationToken, nextDepth)
                            : (PromiseRef) PromiseCancelPromise<TDelegateCancel>.GetOrCreate(canceler, nextDepth);
                        _this._ref.HookupNewPromise(_this.Id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static ushort GetNextDepth(ushort depth)
                {
#if PROMISE_PROGRESS
                    return Fixed32.GetNextDepth(depth);
#else
                    // Depth is unused, but it can help with debugging.
                    unchecked
                    {
                        return (ushort) (depth + 1);
                    }
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
                        AddRejectionToUnhandledStack(e, traceable);
                    }
                    ClearCurrentInvoker();
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

                    PromiseProgress<TProgress> promise;
                    switch (invokeOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            if (_this._ref == null)
                            {
                                InvokeAndCatchProgress(progress, 1, null);
                                return new Promise<TResult>(null, ValidIdFromApi, _this.Depth, _this.Result);
                            }
                            // TODO:
//#if !PROMISE_DEBUG
//                            else if (_this._ref.State == Promise.State.Resolved)
//                            {
//                                InvokeAndCatchProgress(progress, 1, null);
//                            }
//#endif
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
                            synchronizationContext = Promise.Config.BackgroundContext;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            if (_this._ref == null)
                            {
                                promise = PromiseProgress<TProgress>.GetOrCreateFromNull(progress, cancelationToken, _this.Depth, synchronizationContext, CreateResolveContainer(_this.Result));
                                ExecutionScheduler.ScheduleOnContextStatic(synchronizationContext, promise);
                                return new Promise<TResult>(promise, promise.Id, _this.Depth);
                            }
                            break;
                        }
                    }

                    promise = PromiseProgress<TProgress>.GetOrCreate(progress, cancelationToken, _this.Depth, invokeOption == SynchronizationOption.Synchronous, synchronizationContext);
#if PROMISE_DEBUG
                    promise._previous = _this._ref;
#endif
                    var executionScheduler = new ExecutionScheduler(true);
                    _this._ref.InterlockedIncrementProgressReportingCount();
                    HandleablePromiseBase previousWaiter;
                    promise._smallFields._currentProgress = _this._ref._smallFields._currentProgress;
                    PromiseSingleAwait promiseSingleAwait = _this._ref.AddWaiter(_this.Id, promise, out previousWaiter, ref executionScheduler);
                    if (previousWaiter == null)
                    {
                        promise.MaybeReportProgress(ref executionScheduler);
                        _this._ref.InterlockedDecrementProgressReportingCount();
                    }
                    else
                    {
                        _this._ref.InterlockedDecrementProgressReportingCount();
                        if (!PromiseSingleAwait.VerifyWaiter(promiseSingleAwait))
                        {
                            // We're throwing InvalidOperationException here, so we don't want the new object to also add exceptions from its finalizer.
                            GC.SuppressFinalize(promise);
                            throw new InvalidOperationException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", GetFormattedStacktrace(2));
                        }
                        PromiseRef handler = _this._ref;
                        HandleablePromiseBase _;
                        promise.Handle(ref handler, out _, ref executionScheduler);
                    }
                    executionScheduler.Execute();
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }
#endif
            } // CallbackHelper
        } // PromiseRef
    } // Internal
}