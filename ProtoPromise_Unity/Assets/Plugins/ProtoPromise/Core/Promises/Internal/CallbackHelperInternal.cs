﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

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
                    try
                    {
                        return CallbackHelper.AdoptDirect(resolver.Invoke(resolved.Result), resolved.Depth);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect<TCapture>(DelegatePromiseCapture<TCapture, TArg, TResult> resolver, Promise<TArg> resolved)
                {
                    try
                    {
                        return CallbackHelper.AdoptDirect(resolver.Invoke(resolved.Result), resolved.Depth);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
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
                    try
                    {
                        return CallbackHelper.AdoptDirect(resolver.Invoke(resolved.Result), resolved.Depth);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect<TCapture>(DelegateContinuePromiseCapture<TCapture, TArg, TResult> resolver, Promise<TArg> resolved)
                {
                    try
                    {
                        return CallbackHelper.AdoptDirect(resolver.Invoke(resolved.Result), resolved.Depth);
                    }
                    catch (OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                }
            }

            // This helps reduce typed out generics.

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class CallbackHelper
            {
#if !PROMISE_PROGRESS
                [MethodImpl(InlineOption)]
#endif
                internal static Promise<TResult> AdoptDirect<TResult>(Promise<TResult> promise, int currentDepth)
                {
#if !PROMISE_PROGRESS
                    return promise;
#else
                    if (promise._ref == null)
                    {
                        return new Promise<TResult>(null, ValidIdFromApi, currentDepth + 1, promise.Result);
                    }
                    // TODO
//#if !PROMISE_DEBUG
//                    if (promise._ref.State == Promise.State.Resolved)
//                    {
//                        promise._ref.MarkAwaited(promise.Id);
//                        TResult result = ((IValueContainer) promise._ref._valueOrPrevious).GetValue<TResult>();
//                        promise._ref.MaybeDispose();
//                        return new Promise<TResult>(null, ValidIdFromApi, currentDepth + 1, result);
//                    }
//#endif
                    // Normalize progress. Passing a default resolver makes the Execute method adopt the promise's state without attempting to invoke.
                    var newRef = PromiseResolvePromise<DelegateResolvePassthrough>.GetOrCreate(null, default(DelegateResolvePassthrough), currentDepth + 1);
                    ExecutionScheduler executionScheduler = new ExecutionScheduler(true);
                    promise._ref.HookupWaitPromise(newRef, promise.Id, promise.Depth, ref executionScheduler);
                    executionScheduler.Execute();
                    return new Promise<TResult>(newRef, newRef.Id, currentDepth + 1);
#endif
                }

                internal static Promise<TResult> Duplicate<TResult>(Promise<TResult> _this)
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }
                    var newRef = _this._ref.GetDuplicate(_this.Id);
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
                                ? PromiseConfigured.GetOrCreateFromNull(synchronizationContext, _this.Result)
                                : _this._ref.GetConfigured(_this.Id, synchronizationContext);
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
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolve<Delegate<TArg, TResult>>.GetOrCreate(resolver, cancelationToken);
                            promise._smallFields.InterlockedTryReleaseComplete();
                            //Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                            //AddToHandleQueueBack(promise);
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    // TODO: sync callback if ref-backed and already complete in RELEASE mode only.
                    // else if (_this._ref.State != Promise.State.Pending) { }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<Delegate<TArg, TResult>>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolve<Delegate<TArg, TResult>>.GetOrCreate(_this._ref, resolver);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TArg, TResult>(Promise<TArg> _this, DelegatePromise<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    int nextDepth = GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolvePromise<DelegatePromise<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<DelegatePromise<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<DelegatePromise<TArg, TResult>>.GetOrCreate(_this._ref, resolver, nextDepth);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TCapture, TArg, TResult>(Promise<TArg> _this, DelegateCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolve<DelegateCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken);
                            promise._smallFields.InterlockedTryReleaseComplete();
                            //Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                            //AddToHandleQueueBack(promise);
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    // TODO: sync callback if ref-backed and already complete in RELEASE mode only.
                    // else if (_this._ref.State != Promise.State.Pending) { }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<DelegateCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolve<DelegateCapture<TCapture, TArg, TResult>>.GetOrCreate(_this._ref, resolver);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TCapture, TArg, TResult>(Promise<TArg> _this, DelegatePromiseCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    int nextDepth = GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolvePromise<DelegatePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<DelegatePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<DelegatePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(_this._ref, resolver, nextDepth);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
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
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveReject<Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolveReject<Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(_this._ref, resolver, rejecter);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
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
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveReject<DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolveReject<DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(_this._ref, resolver, rejecter);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
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
                    int nextDepth = GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveRejectPromise<Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(_this._ref, resolver, rejecter, nextDepth);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
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
                    int nextDepth = GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveRejectPromise<DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(_this._ref, resolver, rejecter, nextDepth);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
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
                    int nextDepth = GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveRejectPromise<DelegatePromise<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArgResolve, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<DelegatePromise<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<DelegatePromise<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(_this._ref, resolver, rejecter, nextDepth);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
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
                    int nextDepth = GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveRejectPromise<DelegatePromiseCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArgResolve, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<DelegatePromiseCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<DelegatePromiseCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(_this._ref, resolver, rejecter, nextDepth);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
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
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveReject<DelegateResolvePassthrough, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return _this;
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<DelegateResolvePassthrough, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolveReject<DelegateResolvePassthrough, TDelegateReject>.GetOrCreate(_this._ref, resolver, rejecter);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
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
                    int nextDepth = GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveRejectPromise<DelegateResolvePassthrough, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return new Promise<TResult>(_this._ref, _this.Id, nextDepth);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<DelegateResolvePassthrough, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<DelegateResolvePassthrough, TDelegateReject>.GetOrCreate(_this._ref, resolver, rejecter, nextDepth);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TArg, TResult>(Promise<TArg> _this, DelegateContinue<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseContinue<DelegateContinue<TArg, TResult>>.GetOrCreate(resolver, cancelationToken);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinue<DelegateContinue<TArg, TResult>>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseContinue<DelegateContinue<TArg, TResult>>.GetOrCreate(_this._ref, resolver);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TArg, TResult>(Promise<TArg> _this, DelegateContinuePromise<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    int nextDepth = GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseContinuePromise<DelegateContinuePromise<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinuePromise<DelegateContinuePromise<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseContinuePromise<DelegateContinuePromise<TArg, TResult>>.GetOrCreate(_this._ref, resolver, nextDepth);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TCapture, TArg, TResult>(Promise<TArg> _this, DelegateContinueCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseContinue<DelegateContinueCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinue<DelegateContinueCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseContinue<DelegateContinueCapture<TCapture, TArg, TResult>>.GetOrCreate(_this._ref, resolver);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TCapture, TArg, TResult>(Promise<TArg> _this, DelegateContinuePromiseCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    int nextDepth = GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseContinuePromise<DelegateContinuePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinuePromise<DelegateContinuePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseContinuePromise<DelegateContinuePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(_this._ref, resolver, nextDepth);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
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
                    PromiseRef promise = PromiseFinally<DelegateFinally>.GetOrCreate(_this._ref, new DelegateFinally(onFinally));
                    _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
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
                    PromiseRef promise = PromiseFinally<DelegateCaptureFinally<TCapture>>.GetOrCreate(_this._ref, new DelegateCaptureFinally<TCapture>(capturedValue, onFinally));
                    _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancel<TDelegateCancel, TResult>(Promise<TResult> _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancel
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseCancel<TDelegateCancel>.GetOrCreate(canceler, cancelationToken);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return _this;
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseCancel<TDelegateCancel>.GetOrCreate(canceler, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseCancel<TDelegateCancel>.GetOrCreate(_this._ref, canceler);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancelWait<TDelegateCancel, TResult>(Promise<TResult> _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancelPromise
                {
                    int nextDepth = GetNextDepth(_this.Depth);
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseCancelPromise<TDelegateCancel>.GetOrCreate(canceler, cancelationToken, nextDepth);
                            promise._smallFields.InterlockedTryReleaseComplete();
                        }
                        else
                        {
                            return _this;
                        }
                    }
                    else
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseCancelPromise<TDelegateCancel>.GetOrCreate(canceler, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise, _this.Id);
                        }
                        else
                        {
                            promise = PromiseCancelPromise<TDelegateCancel>.GetOrCreate(_this._ref, canceler, nextDepth);
                            _this._ref.HookupNewWaiter(promise, _this.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static int GetNextDepth(int depth)
                {
#if !PROMISE_PROGRESS
                    return 0;
#else
                    return Fixed32.GetNextDepth(depth);
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
                                promise = PromiseProgress<TProgress>.GetOrCreate(CreateResolveContainer(_this.Result, 1), progress, cancelationToken, _this.Depth, false, synchronizationContext);
                                promise.IsComplete = true;
                                ExecutionScheduler.ScheduleOnContextStatic(synchronizationContext, promise);
                                return new Promise<TResult>(promise, promise.Id, _this.Depth);
                            }
                            break;
                        }
                    }

                    promise = PromiseProgress<TProgress>.GetOrCreate(_this._ref, progress, cancelationToken, _this.Depth, invokeOption == SynchronizationOption.Synchronous, synchronizationContext);
                    _this._ref.HookupNewWaiterWithProgress(promise, _this.Id, _this.Depth, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }
#endif
            } // CallbackHelper
        } // PromiseRef
    } // Internal
}