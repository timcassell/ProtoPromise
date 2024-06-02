#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0074 // Use compound assignment

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        internal enum SynchronizationOption : byte
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
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelperArg<TArg>
            {
                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackDirect<TDelegate>(TDelegate resolver, in Promise<TArg> resolved)
                    where TDelegate : IAction<TArg>
                {
                    try
                    {
                        resolver.Invoke(CallbackHelperVoid.GetResultFromResolved(resolved));
                        return Promise.Resolved();
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, in Promise<TArg> resolved)
                    where TDelegate : IFunc<TArg, Promise>
                {
                    try
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        return resolver.Invoke(CallbackHelperVoid.GetResultFromResolved(resolved)).Duplicate();
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TArg>(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolve<TDelegate>(Promise<TArg> _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IAction<TArg>, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolve<TArg, TDelegate>.GetOrCreate(resolver);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolve<TArg, TDelegate>.GetOrCreate(resolver);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveWait<TDelegate>(Promise<TArg> _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolvePromise<VoidResult, TDelegate>.GetOrCreate(resolver);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolvePromise<VoidResult, TDelegate>.GetOrCreate(resolver);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
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
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolveReject<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolveReject<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolveRejectPromise<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolveRejectPromise<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddContinue<TDelegateContinue>(Promise<TArg> _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IAction<TArg>, IDelegateContinue
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(continuer, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseContinue<VoidResult, TDelegateContinue>.GetOrCreate(continuer);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseContinue<VoidResult, TDelegateContinue>.GetOrCreate(continuer);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddContinueWait<TDelegateContinue>(Promise<TArg> _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<TArg, Promise>, IDelegateContinuePromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperVoid.Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(continuer, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseContinuePromise<VoidResult, TDelegateContinue>.GetOrCreate(continuer);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseContinuePromise<VoidResult, TDelegateContinue>.GetOrCreate(continuer);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelperResult<TResult>
            {
                internal static Promise<TResult> Canceled(PromiseRefBase _ref, short promiseId)
                {
                    _ref?.MaybeMarkAwaitedAndDispose(promiseId);
                    var deferred = DeferredPromise<TResult>.GetOrCreate();
                    deferred.CancelDirect();
                    return new Promise<TResult>(deferred, deferred.Id);
                }

                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackDirect<TDelegate>(TDelegate resolver, in Promise resolved)
                    where TDelegate : IFunc<TResult>
                {
                    try
                    {
                        resolved._ref?.MaybeMarkAwaitedAndDispose(resolved._id);
                        TResult result = resolver.Invoke();
                        return Promise.Resolved(result);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, in Promise resolved)
                    where TDelegate : IFunc<Promise<TResult>>
                {
                    try
                    {
                        resolved._ref?.MaybeMarkAwaitedAndDispose(resolved._id);
#pragma warning disable CS0618 // Type or member is obsolete
                        return resolver.Invoke().Duplicate();
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackDirect<TDelegate>(TDelegate runner) where TDelegate : IFunc<TResult>
                {
                    try
                    {
                        TResult result = runner.Invoke();
                        return Promise.Resolved(result);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate runner) where TDelegate : IFunc<Promise<TResult>>
                {
                    try
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        return runner.Invoke().Duplicate();
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> New<TDelegate>(TDelegate runner, SynchronizationOption invokeOption, SynchronizationContext synchronizationContext, bool forceAsync)
                    where TDelegate : IDelegateNew<TResult>
                {
                    var promise = DeferredNewPromise<TResult, TDelegate>.GetOrCreate(runner);
                    promise.RunOrScheduleOnContext(invokeOption, synchronizationContext, forceAsync);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Run<TDelegate>(TDelegate runner, SynchronizationOption invokeOption, SynchronizationContext synchronizationContext, bool forceAsync)
                    where TDelegate : IFunc<TResult>, IDelegateRun
                {
                    switch (invokeOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            return InvokeCallbackDirect(runner);
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
                            break;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            if (synchronizationContext == null)
                            {
                                synchronizationContext = BackgroundSynchronizationContextSentinel.s_instance;
                            }
                            break;
                        }
                    }

                    if (!forceAsync & synchronizationContext == Promise.Manager.ThreadStaticSynchronizationContext)
                    {
                        return InvokeCallbackDirect(runner);
                    }

                    var promise = RunPromise<TResult, TDelegate>.GetOrCreate(runner);
                    promise.ScheduleOnContext(synchronizationContext);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> RunWait<TDelegate>(TDelegate runner, SynchronizationOption invokeOption, SynchronizationContext synchronizationContext, bool forceAsync)
                    where TDelegate : IFunc<Promise<TResult>>, IDelegateRunPromise
                {
                    switch (invokeOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            return InvokeCallbackAndAdoptDirect(runner);
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
                            break;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            if (synchronizationContext == null)
                            {
                                synchronizationContext = BackgroundSynchronizationContextSentinel.s_instance;
                            }
                            break;
                        }
                    }

                    if (!forceAsync & synchronizationContext == Promise.Manager.ThreadStaticSynchronizationContext)
                    {
                        return InvokeCallbackAndAdoptDirect(runner);
                    }

                    var promise = RunWaitPromise<TResult, TDelegate>.GetOrCreate(runner);
                    promise.ScheduleOnContext(synchronizationContext);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TDelegate>(Promise _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<TResult>, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolve<TResult, TDelegate>.GetOrCreate(resolver);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolve<TResult, TDelegate>.GetOrCreate(resolver);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TDelegate>(Promise _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
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
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolveReject<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolveReject<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolveRejectPromise<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolveRejectPromise<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TDelegateContinue>(Promise _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<TResult>, IDelegateContinue
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(continuer, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseContinue<TResult, TDelegateContinue>.GetOrCreate(continuer);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseContinue<TResult, TDelegateContinue>.GetOrCreate(continuer);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TDelegateContinue>(Promise _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<Promise<TResult>>, IDelegateContinuePromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(continuer, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseContinuePromise<TResult, TDelegateContinue>.GetOrCreate(continuer);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseContinuePromise<TResult, TDelegateContinue>.GetOrCreate(continuer);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancel<TDelegateCancel>(Promise<TResult> _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : CallbackHelperVoid.Duplicate(_this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseCancel<TResult, TDelegateCancel>.GetOrCreate(canceler);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseCancel<TResult, TDelegateCancel>.GetOrCreate(canceler);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancelWait<TDelegateCancel>(Promise<TResult> _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : new Promise<TResult>(_this._ref, _this._id, _this._result);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseCancelPromise<TResult, TDelegateCancel>.GetOrCreate(canceler);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseCancelPromise<TResult, TDelegateCancel>.GetOrCreate(canceler);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelper<TArg, TResult>
            {
                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackDirect<TDelegate>(TDelegate resolver, in Promise<TArg> resolved)
                    where TDelegate : IFunc<TArg, TResult>
                {
                    try
                    {
                        var result = resolver.Invoke(CallbackHelperVoid.GetResultFromResolved(resolved));
                        return Promise.Resolved(result);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, in Promise<TArg> resolved)
                    where TDelegate : IFunc<TArg, Promise<TResult>>
                {
                    try
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        return resolver.Invoke(CallbackHelperVoid.GetResultFromResolved(resolved)).Duplicate();
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TDelegate>(Promise<TArg> _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<TArg, TResult>, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolve<TResult, TDelegate>.GetOrCreate(resolver);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolve<TResult, TDelegate>.GetOrCreate(resolver);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TDelegate>(Promise<TArg> _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
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
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolveReject<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolveReject<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolveRejectPromise<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolveRejectPromise<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TDelegateContinue>(Promise<TArg> _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<TArg, TResult>, IDelegateContinue
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(continuer, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseContinue<TResult, TDelegateContinue>.GetOrCreate(continuer);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseContinue<TResult, TDelegateContinue>.GetOrCreate(continuer);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TDelegateContinue>(Promise<TArg> _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<TArg, Promise<TResult>>, IDelegateContinuePromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(continuer, _this);
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseContinuePromise<TResult, TDelegateContinue>.GetOrCreate(continuer);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseContinuePromise<TResult, TDelegateContinue>.GetOrCreate(continuer);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelperVoid
            {
                internal static Promise FromException(Exception e)
                {
                    if (e is OperationCanceledException)
                    {
                        var promise = Promise.Canceled();
                        return new Promise(promise._ref, promise._id);
                    }
                    else
                    {
                        var promise = Promise.Rejected(e);
                        return new Promise(promise._ref, promise._id);
                    }
                }

                internal static Promise<TResult> FromException<TResult>(Exception e)
                {
                    if (e is OperationCanceledException)
                    {
                        var promise = Promise<TResult>.Canceled();
                        return new Promise<TResult>(promise._ref, promise._id);
                    }
                    else
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise._id);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackDirect<TDelegate>(TDelegate resolver, in Promise resolved)
                    where TDelegate : IAction
                {
                    try
                    {
                        resolved._ref?.MaybeMarkAwaitedAndDispose(resolved._id);
                        resolver.Invoke();
                        return Promise.Resolved();
                    }
                    catch (Exception e)
                    {
                        return FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, in Promise resolved)
                    where TDelegate : IFunc<Promise>
                {
                    try
                    {
                        resolved._ref?.MaybeMarkAwaitedAndDispose(resolved._id);
#pragma warning disable CS0618 // Type or member is obsolete
                        return resolver.Invoke().Duplicate();
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    catch (Exception e)
                    {
                        return FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackDirect<TDelegate>(TDelegate runner) where TDelegate : IAction
                {
                    try
                    {
                        runner.Invoke();
                        return Promise.Resolved(0);
                    }
                    catch (Exception e)
                    {
                        return FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate runner) where TDelegate : IFunc<Promise>
                {
                    try
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        return runner.Invoke().Duplicate();
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    catch (Exception e)
                    {
                        return FromException(e);
                    }
                }

                internal static Promise Canceled(PromiseRefBase _ref, short promiseId)
                {
                    _ref?.MaybeMarkAwaitedAndDispose(promiseId);
                    var deferred = DeferredPromise<VoidResult>.GetOrCreate();
                    deferred.CancelDirect();
                    return new Promise(deferred, deferred.Id);
                }

                [MethodImpl(InlineOption)]
                internal static TResult GetResultFromResolved<TResult>(in Promise<TResult> promise)
                {
                    if (promise._ref == null)
                    {
                        return promise._result;
                    }
                    var result = promise._ref._result;
                    promise._ref.MaybeMarkAwaitedAndDispose(promise._id);
                    return result;
                }

                internal static Promise Duplicate(Promise _this)
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }
                    var newRef = _this._ref.GetDuplicate(_this._id);
                    return new Promise(newRef, newRef.Id);
                }

                internal static Promise<TResult> Duplicate<TResult>(Promise<TResult> _this)
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }
                    var newRef = _this._ref.GetDuplicateT(_this._id);
                    return new Promise<TResult>(newRef, newRef.Id, _this._result);
                }

                internal static Promise WaitAsync(Promise _this, CancelationToken cancelationToken)
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : _this;
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = PromiseDuplicateCancel<VoidResult>.GetOrCreate();
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = _this._ref.GetDuplicate(_this._id);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> WaitAsync<TResult>(Promise<TResult> _this, CancelationToken cancelationToken)
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id)
                            : _this;
                    }
                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = PromiseDuplicateCancel<TResult>.GetOrCreate();
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = _this._ref.GetDuplicateT(_this._id);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this._result);
                }

                internal static Promise WaitAsync(Promise _this, SynchronizationOption continuationOption, SynchronizationContext synchronizationContext, bool forceAsync, CancelationToken cancelationToken)
                {
                    switch (continuationOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            return WaitAsync(_this, cancelationToken);
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
                            break;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            if (synchronizationContext == null)
                            {
                                synchronizationContext = BackgroundSynchronizationContextSentinel.s_instance;
                            }
                            break;
                        }
                    }

                    PromiseConfigured<VoidResult> promise;
                    if (_this._ref == null)
                    {
                        promise = PromiseConfigured<VoidResult>.GetOrCreateFromResolved(synchronizationContext, new VoidResult(), forceAsync);
                        promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    }
                    else
                    {
                        promise = PromiseConfigured<VoidResult>.GetOrCreate(synchronizationContext, forceAsync);
                        _this._ref.HookupCancelablePromise(promise, _this._id, cancelationToken, ref promise._cancelationHelper);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> WaitAsync<TResult>(Promise<TResult> _this, SynchronizationOption continuationOption, SynchronizationContext synchronizationContext, bool forceAsync, CancelationToken cancelationToken)
                {
                    switch (continuationOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            return WaitAsync(_this, cancelationToken);
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
                            break;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            if (synchronizationContext == null)
                            {
                                synchronizationContext = BackgroundSynchronizationContextSentinel.s_instance;
                            }
                            break;
                        }
                    }

                    PromiseConfigured<TResult> promise;
                    if (_this._ref == null)
                    {
                        promise = PromiseConfigured<TResult>.GetOrCreateFromResolved(synchronizationContext, _this._result, forceAsync);
                        promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    }
                    else
                    {
                        promise = PromiseConfigured<TResult>.GetOrCreate(synchronizationContext, forceAsync);
                        _this._ref.HookupCancelablePromise(promise, _this._id, cancelationToken, ref promise._cancelationHelper);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this._result);
                }

                [MethodImpl(InlineOption)]
                internal static Promise New<TDelegate>(TDelegate runner, SynchronizationOption invokeOption, SynchronizationContext synchronizationContext, bool forceAsync)
                    where TDelegate : IDelegateNew<VoidResult>
                {
                    var promise = DeferredNewPromise<VoidResult, TDelegate>.GetOrCreate(runner);
                    promise.RunOrScheduleOnContext(invokeOption, synchronizationContext, forceAsync);
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Run<TDelegate>(TDelegate runner, SynchronizationOption invokeOption, SynchronizationContext synchronizationContext, bool forceAsync)
                    where TDelegate : IAction, IDelegateRun
                {
                    switch (invokeOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                        return InvokeCallbackDirect(runner);
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
                            break;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            if (synchronizationContext == null)
                            {
                                synchronizationContext = BackgroundSynchronizationContextSentinel.s_instance;
                            }
                            break;
                        }
                    }

                    if (!forceAsync & synchronizationContext == Promise.Manager.ThreadStaticSynchronizationContext)
                    {
                        return InvokeCallbackDirect(runner);
                    }

                    var promise = RunPromise<VoidResult, TDelegate>.GetOrCreate(runner);
                    promise.ScheduleOnContext(synchronizationContext);
                    return new Promise(promise, promise.Id);
                }

                internal static Promise RunWait<TDelegate>(TDelegate runner, SynchronizationOption invokeOption, SynchronizationContext synchronizationContext, bool forceAsync)
                    where TDelegate : IFunc<Promise>, IDelegateRunPromise
                {
                    switch (invokeOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            return InvokeCallbackAndAdoptDirect(runner);
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
                            break;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext;
                            goto default;
                        }
                        default: // SynchronizationOption.Explicit
                        {
                            if (synchronizationContext == null)
                            {
                                synchronizationContext = BackgroundSynchronizationContextSentinel.s_instance;
                            }
                            break;
                        }
                    }

                    if (!forceAsync & synchronizationContext == Promise.Manager.ThreadStaticSynchronizationContext)
                    {
                        return InvokeCallbackAndAdoptDirect(runner);
                    }

                    var promise = RunWaitPromise<VoidResult, TDelegate>.GetOrCreate(runner);
                    promise.ScheduleOnContext(synchronizationContext);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolve<TDelegate>(Promise _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IAction, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolve<VoidResult, TDelegate>.GetOrCreate(resolver);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolve<VoidResult, TDelegate>.GetOrCreate(resolver);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveWait<TDelegate>(Promise _this, TDelegate resolver, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolvePromise<VoidResult, TDelegate>.GetOrCreate(resolver);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolvePromise<VoidResult, TDelegate>.GetOrCreate(resolver);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
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
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(resolver, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolveReject<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolveReject<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise _this,
                    TDelegateResolve resolver,
                    TDelegateReject rejecter,
                    CancelationToken cancelationToken)
                    where TDelegateResolve : IFunc<Promise>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseResolveRejectPromise<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseResolveRejectPromise<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddContinue<TDelegateContinue>(Promise _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IAction, IDelegateContinue
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackDirect(continuer, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseContinue<VoidResult, TDelegateContinue>.GetOrCreate(continuer);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseContinue<VoidResult, TDelegateContinue>.GetOrCreate(continuer);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddContinueWait<TDelegateContinue>(Promise _this, TDelegateContinue continuer, CancelationToken cancelationToken)
                    where TDelegateContinue : IFunc<Promise>, IDelegateContinuePromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : InvokeCallbackAndAdoptDirect(continuer, _this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseContinuePromise<VoidResult, TDelegateContinue>.GetOrCreate(continuer);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseContinuePromise<VoidResult, TDelegateContinue>.GetOrCreate(continuer);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddFinally<TFinalizer>(Promise _this, TFinalizer finalizer)
                    where TFinalizer : IAction
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackDirect(finalizer, _this);
                    }
                    var promise = PromiseFinally<VoidResult, TFinalizer>.GetOrCreate(finalizer);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddFinallyWait<TFinalizer>(Promise _this, TFinalizer finalizer)
                    where TFinalizer : IFunc<Promise>, INullable
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackAndAdoptDirect(finalizer, _this);
                    }
                    var promise = PromiseFinallyWait<VoidResult, TFinalizer>.GetOrCreate(finalizer);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddFinally<TResult, TFinalizer>(Promise<TResult> _this, TFinalizer finalizer)
                    where TFinalizer : IAction
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        TResult result = GetResultFromResolved(_this);
                        try
                        {
                            finalizer.Invoke();
                            return Promise.Resolved(result);
                        }
                        catch (Exception e)
                        {
                            return FromException<TResult>(e);
                        }
                    }
                    var promise = PromiseFinally<TResult, TFinalizer>.GetOrCreate(finalizer);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddFinallyWait<TResult, TFinalizer>(Promise<TResult> _this, TFinalizer finalizer)
                    where TFinalizer : IFunc<Promise>, INullable
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        TResult result = GetResultFromResolved(_this);
                        try
                        {
                            var finallyPromise = finalizer.Invoke();
                            finallyPromise = new Promise(finallyPromise._ref, finallyPromise._id);
                            return finallyPromise
                                .Then(result, r => r);
                        }
                        catch (Exception e)
                        {
                            return FromException<TResult>(e);
                        }
                    }
                    var promise = PromiseFinallyWait<TResult, TFinalizer>.GetOrCreate(finalizer);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddCancel<TDelegateCancel>(Promise _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : Duplicate(_this);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseCancel<VoidResult, TDelegateCancel>.GetOrCreate(canceler);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseCancel<VoidResult, TDelegateCancel>.GetOrCreate(canceler);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddCancelWait<TDelegateCancel>(Promise _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Canceled(_this._ref, _this._id)
                            : new Promise(_this._ref, _this._id);
                    }
                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelablePromiseCancelPromise<VoidResult, TDelegateCancel>.GetOrCreate(canceler);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = PromiseCancelPromise<VoidResult, TDelegateCancel>.GetOrCreate(canceler);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }
            } // CallbackHelper
        } // PromiseRefBase
    } // Internal
}