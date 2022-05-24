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
            // NOTE: `where TResolver : IDelegate<TArg, TResult>` is much cleaner, but doesn't play well with AOT compilation (IL2CPP), making these repeated functions unfortunately necessary.
            // IDelegate<TArg, TResult> actually works properly with IL2CPP in Unity 2021.2, so we may switch to that implementation if we decide to drop support for older Unity versions (possibly when Unity finally adopts .Net 5+ runtime).

            private static class Invoker<TArg, TResult>
            {
                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackDirect(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Delegate<TArg, TResult> resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved)
                {
                    try
                    {
                        TResult result;
                        if (resolved._ref == ResolvedSentinel.s_instance)
                        {
                            result = resolver.Invoke(resolved.Result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved.Id);
                            result = resolver.Invoke(arg);
                        }
                        return CreateResolved(result, resolved.Depth);
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
                internal static Promise<TResult> InvokeCallbackDirect<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    DelegateCapture<TCapture, TArg, TResult> resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved)
                {
                    try
                    {
                        TResult result;
                        if (resolved._ref == ResolvedSentinel.s_instance)
                        {
                            result = resolver.Invoke(resolved.Result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved.Id);
                            result = resolver.Invoke(arg);
                        }
                        return CreateResolved(result, resolved.Depth);
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
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    DelegatePromise<TArg, TResult> resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved, ushort nextDepth)
                {
                    try
                    {
                        Promise<TResult> result;
                        if (resolved._ref == ResolvedSentinel.s_instance)
                        {
                            result = resolver.Invoke(resolved.Result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved.Id);
                            result = resolver.Invoke(arg);
                        }
                        return CallbackHelper.AdoptDirect(result, nextDepth);
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
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    DelegatePromiseCapture<TCapture, TArg, TResult> resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved, ushort nextDepth)
                {
                    try
                    {
                        Promise<TResult> result;
                        if (resolved._ref == ResolvedSentinel.s_instance)
                        {
                            result = resolver.Invoke(resolved.Result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved.Id);
                            result = resolver.Invoke(arg);
                        }
                        return CallbackHelper.AdoptDirect(result, nextDepth);
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
                internal static Promise<TResult> InvokeCallbackDirect(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    DelegateContinue<TArg, TResult> resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved)
                {
                    try
                    {
                        TResult result;
                        if (resolved._ref == ResolvedSentinel.s_instance)
                        {
                            result = resolver.Invoke(resolved.Result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved.Id);
                            result = resolver.Invoke(arg);
                        }
                        return CreateResolved(result, resolved.Depth);
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
                internal static Promise<TResult> InvokeCallbackDirect<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    DelegateContinueCapture<TCapture, TArg, TResult> resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved)
                {
                    try
                    {
                        TResult result;
                        if (resolved._ref == ResolvedSentinel.s_instance)
                        {
                            result = resolver.Invoke(resolved.Result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved.Id);
                            result = resolver.Invoke(arg);
                        }
                        return CreateResolved(result, resolved.Depth);
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
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    DelegateContinuePromise<TArg, TResult> resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved, ushort nextDepth)
                {
                    try
                    {
                        Promise<TResult> result;
                        if (resolved._ref == ResolvedSentinel.s_instance)
                        {
                            result = resolver.Invoke(resolved.Result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved.Id);
                            result = resolver.Invoke(arg);
                        }
                        return CallbackHelper.AdoptDirect(result, nextDepth);
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
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect<TCapture>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    DelegateContinuePromiseCapture<TCapture, TArg, TResult> resolver,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TArg> resolved, ushort nextDepth)
                {
                    try
                    {
                        Promise<TResult> result;
                        if (resolved._ref == ResolvedSentinel.s_instance)
                        {
                            result = resolver.Invoke(resolved.Result);
                        }
                        else
                        {
                            TArg arg = resolved._ref.GetResult<TArg>();
                            resolved._ref.MaybeMarkAwaitedAndDispose(resolved.Id);
                            result = resolver.Invoke(arg);
                        }
                        return CallbackHelper.AdoptDirect(result, nextDepth);
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
                private static Promise<TResult> Canceled<TResult>(PromiseRefBase _ref, short promiseId, ushort depth)
                {
                    _ref.MaybeMarkAwaitedAndDispose(promiseId);
                    var deferred = DeferredPromise<TResult>.GetOrCreate();
                    deferred.CancelDirect();
                    return new Promise<TResult>(deferred, deferred.Id, depth);
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
                    if (_ref.State != Promise.State.Resolved)
                    {
                        // Normalize progress. Passing a default resolver makes the Execute method adopt the promise's state without attempting to invoke.
                        var newRef = PromiseResolvePromise<TResult, DelegateResolvePassthrough<TResult>>.GetOrCreate(default(DelegateResolvePassthrough<TResult>), nextDepth);
                        newRef.WaitForWithProgress(promise);
                        return new Promise<TResult>(newRef, newRef.Id, nextDepth);
                    }
                    return CreateResolved(GetResultFromResolved(promise), nextDepth);
#endif
                }

                [MethodImpl(InlineOption)]
                private static TResult GetResultFromResolved<TResult>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    Promise<TResult> promise)
                {
                    if (promise._ref == ResolvedSentinel.s_instance)
                    {
                        return promise.Result;
                    }
                    var result = promise._ref.GetResult<TResult>();
                    promise._ref.MaybeMarkAwaitedAndDispose(promise.Id);
                    return result;
                }

                internal static Promise<TResult> Duplicate<TResult>(Promise<TResult> _this)
                {
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
                            synchronizationContext = Promise.Config.BackgroundContext ?? BackgroundSynchronizationContextSentinel.s_instance;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            var newRef = _this._ref == ResolvedSentinel.s_instance
                                ? PromiseConfigured<TResult>.GetOrCreateFromResolved(synchronizationContext, _this.Result, _this.Depth)
                                : _this._ref.GetConfigured(_this.Id, synchronizationContext, _this.Depth);
                            return new Promise<TResult>(newRef, newRef.Id, _this.Depth, _this.Result);
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TArg, TResult>(Promise<TArg> _this, Delegate<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, _this.Depth)
                            : Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolve<TResult, Delegate<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolve<TResult, Delegate<TArg, TResult>>.GetOrCreate(resolver, _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TArg, TResult>(Promise<TArg> _this, DelegatePromise<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, nextDepth)
                            : Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolvePromise<TResult, DelegatePromise<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolvePromise<TResult, DelegatePromise<TArg, TResult>>.GetOrCreate(resolver, nextDepth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TCapture, TArg, TResult>(Promise<TArg> _this, DelegateCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, _this.Depth)
                            : Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolve<TResult, DelegateCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolve<TResult, DelegateCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TCapture, TArg, TResult>(Promise<TArg> _this, DelegatePromiseCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, nextDepth)
                            : Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolvePromise<TResult, DelegatePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolvePromise<TResult, DelegatePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, nextDepth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TArgResolve, TDelegateReject, TResult>(Promise<TArgResolve> _this,
                    Delegate<TArgResolve, TResult> resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, _this.Depth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveReject<TResult, Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolveReject<TResult, Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TCaptureResolve, TArgResolve, TDelegateReject, TResult>(Promise<TArgResolve> _this,
                    DelegateCapture<TCaptureResolve, TArgResolve, TResult> resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, _this.Depth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveReject<TResult, DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolveReject<TResult, DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
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
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, nextDepth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveRejectPromise<TResult, Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolveRejectPromise<TResult, Delegate<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
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
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, nextDepth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveRejectPromise<TResult, DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolveRejectPromise<TResult, DelegateCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
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
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, nextDepth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveRejectPromise<TResult, DelegatePromise<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolveRejectPromise<TResult, DelegatePromise<TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
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
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, nextDepth)
                            : Invoker<TArgResolve, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveRejectPromise<TResult, DelegatePromiseCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolveRejectPromise<TResult, DelegatePromiseCapture<TCaptureResolve, TArgResolve, TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TDelegateReject, TResult>(Promise<TResult> _this,
                    DelegateResolvePassthrough<TResult> resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, _this.Depth)
                            : Duplicate(_this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveReject<TResult, DelegateResolvePassthrough<TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseResolveReject<TResult, DelegateResolvePassthrough<TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TDelegateReject, TResult>(Promise<TResult> _this,
                    DelegateResolvePassthrough<TResult> resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateReject : IDelegateRejectPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, nextDepth)
                            : new Promise<TResult>(_this._ref, _this.Id, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseResolveRejectPromise<TResult, DelegateResolvePassthrough<TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseResolveRejectPromise<TResult, DelegateResolvePassthrough<TResult>, TDelegateReject>.GetOrCreate(resolver, rejecter, nextDepth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TArg, TResult>(Promise<TArg> _this, DelegateContinue<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, _this.Depth)
                            : Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinue<TResult, DelegateContinue<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseContinue<TResult, DelegateContinue<TArg, TResult>>.GetOrCreate(resolver, _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TArg, TResult>(Promise<TArg> _this, DelegateContinuePromise<TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, nextDepth)
                            : Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinuePromise<TResult, DelegateContinuePromise<TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseContinuePromise<TResult, DelegateContinuePromise<TArg, TResult>>.GetOrCreate(resolver, nextDepth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TCapture, TArg, TResult>(Promise<TArg> _this, DelegateContinueCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, _this.Depth)
                            : Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinue<TResult, DelegateContinueCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseContinue<TResult, DelegateContinueCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TCapture, TArg, TResult>(Promise<TArg> _this, DelegateContinuePromiseCapture<TCapture, TArg, TResult> resolver, CancelationToken cancelationToken)
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, nextDepth)
                            : Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this, nextDepth);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseContinuePromise<TResult, DelegateContinuePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseContinuePromise<TResult, DelegateContinuePromiseCapture<TCapture, TArg, TResult>>.GetOrCreate(resolver, nextDepth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddFinally<TResult>(Promise<TResult> _this, Action onFinally)
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        var p = Invoker<VoidResult, VoidResult>.InvokeCallbackDirect(DelegateWrapper.Create(onFinally), _this.AsPromise()._target);
                        return new Promise<TResult>(p._ref, p.Id, p.Depth, _this.Result);
                    }
                    PromiseRefBase promise = PromiseFinally<TResult, DelegateFinally>.GetOrCreate(new DelegateFinally(onFinally), _this.Depth);
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
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        var p = Invoker<VoidResult, VoidResult>.InvokeCallbackDirect(DelegateWrapper.Create(capturedValue, onFinally), _this.AsPromise()._target);
                        return new Promise<TResult>(p._ref, p.Id, p.Depth, _this.Result);
                    }
                    PromiseRefBase promise = PromiseFinally<TResult, DelegateCaptureFinally<TCapture>>.GetOrCreate(new DelegateCaptureFinally<TCapture>(capturedValue, onFinally), _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancel<TDelegateCancel, TResult>(Promise<TResult> _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancel
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, _this.Depth)
                            : Duplicate(_this);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseCancel<TResult, TDelegateCancel>.GetOrCreate(canceler, cancelationToken, _this.Depth)
                        : (PromiseRefBase) PromiseCancel<TResult, TDelegateCancel>.GetOrCreate(canceler, _this.Depth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancelWait<TDelegateCancel, TResult>(Promise<TResult> _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancelPromise
                {
                    ushort nextDepth = GetNextDepth(_this.Depth);
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled<TResult>(_this._ref, _this.Id, nextDepth)
                            : new Promise<TResult>(_this._ref, _this.Id, nextDepth, _this.Result);
                    }
                    var promise = cancelationToken.CanBeCanceled
                        ? CancelablePromiseCancelPromise<TResult, TDelegateCancel>.GetOrCreate(canceler, cancelationToken, nextDepth)
                        : (PromiseRefBase) PromiseCancelPromise<TResult, TDelegateCancel>.GetOrCreate(canceler, nextDepth);
                    _this._ref.HookupNewPromise(_this.Id, promise);
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
                        ReportRejection(e, traceable);
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

                    PromiseProgress<TResult, TProgress> promise;
                    switch (invokeOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            if (_this._ref.State == Promise.State.Resolved)
                            {
                                InvokeAndCatchProgress(progress, 1, null);
                                return CreateResolved(GetResultFromResolved(_this), _this.Depth);
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
                            if (_this._ref.State == Promise.State.Resolved)
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
                    PromiseRefBase promiseSingleAwait = _this._ref.AddWaiter(_this.Id, promise, out previousWaiter);
                    if (previousWaiter == null)
                    {
                        promise.MaybeScheduleProgress();
                        _this._ref.InterlockedDecrementProgressReportingCount();
                        StackUnwindHelper.InvokeProgressors();
                    }
                    else
                    {
                        _this._ref.InterlockedDecrementProgressReportingCount();
                        if (!PromiseRefBase.VerifyWaiter(promiseSingleAwait))
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