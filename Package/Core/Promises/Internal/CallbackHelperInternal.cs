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
                internal static Promise InvokeCallbackDirect<TDelegate>(TDelegate resolver, in TArg arg)
                    where TDelegate : IAction<TArg>
                {
                    try
                    {
                        resolver.Invoke(arg);
                        return Promise.Resolved();
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, in TArg arg)
                    where TDelegate : IFunc<TArg, Promise>
                {
                    try
                    {
                        return resolver.Invoke(arg).Duplicate();
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
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : InvokeCallbackDirect(resolver, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        var arg = _this._ref._result;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackDirect(resolver, arg)
                            : Promise.Rejected(rejectContainer);
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
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : InvokeCallbackAndAdoptDirect(resolver, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        var arg = _this._ref._result;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackAndAdoptDirect(resolver, arg)
                            : Promise.Rejected(rejectContainer);
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
                    where TDelegateReject : IDelegateRejectSynchronous, IDelegateReject
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : InvokeCallbackDirect(resolver, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        var arg = _this._ref._result;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackDirect(resolver, arg)
                            : CallbackHelperVoid.InvokeRejecter(rejecter, rejectContainer);
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
                    where TDelegateReject : IDelegateRejectSynchronous<Promise>, IDelegateRejectPromise
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : InvokeCallbackAndAdoptDirect(resolver, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        var arg = _this._ref._result;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackAndAdoptDirect(resolver, arg)
                            : CallbackHelperVoid.InvokeRejecterAndAdopt(rejecter, rejectContainer);
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
                    where TDelegateContinue : IAction<Promise<TArg>.ResultContainer>, IDelegateContinue
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : CallbackHelperArg<Promise<TArg>.ResultContainer>.InvokeCallbackDirect(continuer, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var resultContainer = new Promise<TArg>.ResultContainer(_this._ref._result, _this._ref._rejectContainer, _this._ref.State);
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : CallbackHelperArg<Promise<TArg>.ResultContainer>.InvokeCallbackDirect(continuer, resultContainer);
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
                    where TDelegateContinue : IFunc<Promise<TArg>.ResultContainer, Promise>, IDelegateContinuePromise
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : CallbackHelperArg<Promise<TArg>.ResultContainer>.InvokeCallbackAndAdoptDirect(continuer, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var resultContainer = new Promise<TArg>.ResultContainer(_this._ref._result, _this._ref._rejectContainer, _this._ref.State);
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : CallbackHelperArg<Promise<TArg>.ResultContainer>.InvokeCallbackAndAdoptDirect(continuer, resultContainer);
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
                [MethodImpl(MethodImplOptions.NoInlining)]
                internal static Promise<TResult> InvokeRejecter<TDelegateReject>(TDelegateReject rejecter, IRejectContainer rejectContainer)
                    where TDelegateReject : IDelegateRejectSynchronous<TResult>
                {
                    try
                    {
                        return rejecter.TryInvokeRejecter(rejectContainer, out var result)
                            ? Promise<TResult>.Resolved(result)
                            : Promise<TResult>.Rejected(rejectContainer);
                    }
                    catch (RethrowException)
                    {
                        return Promise<TResult>.Rejected(rejectContainer);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                internal static Promise<TResult> InvokeRejecterAndAdopt<TDelegateReject>(TDelegateReject rejecter, IRejectContainer rejectContainer)
                    where TDelegateReject : IDelegateRejectSynchronous<Promise<TResult>>
                {
                    try
                    {
                        return rejecter.TryInvokeRejecter(rejectContainer, out var promise)
                            ? promise.Duplicate()
                            : Promise<TResult>.Rejected(rejectContainer);
                    }
                    catch (RethrowException)
                    {
                        return Promise<TResult>.Rejected(rejectContainer);
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
                        return runner.Invoke().Duplicate();
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

                    if (!forceAsync & synchronizationContext == ts_currentContext)
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

                    if (!forceAsync & synchronizationContext == ts_currentContext)
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
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : InvokeCallbackDirect(resolver);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackDirect(resolver)
                            : Promise<TResult>.Rejected(rejectContainer);
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
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : InvokeCallbackAndAdoptDirect(resolver);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackAndAdoptDirect(resolver)
                            : Promise<TResult>.Rejected(rejectContainer);
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
                    where TDelegateReject : IDelegateRejectSynchronous<TResult>, IDelegateReject
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : InvokeCallbackDirect(resolver);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackDirect(resolver)
                            : InvokeRejecter(rejecter, rejectContainer);
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
                    where TDelegateReject : IDelegateRejectSynchronous<Promise<TResult>>, IDelegateRejectPromise
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : InvokeCallbackAndAdoptDirect(resolver);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackAndAdoptDirect(resolver)
                            : InvokeRejecterAndAdopt(rejecter, rejectContainer);
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
                    where TDelegateContinue : IFunc<Promise.ResultContainer, TResult>, IDelegateContinue
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : CallbackHelper<Promise.ResultContainer, TResult>.InvokeCallbackDirect(continuer, Promise.ResultContainer.Resolved);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var resultContainer = new Promise.ResultContainer(_this._ref._rejectContainer, _this._ref.State);
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : CallbackHelper<Promise.ResultContainer, TResult>.InvokeCallbackDirect(continuer, resultContainer);
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
                    where TDelegateContinue : IFunc<Promise.ResultContainer, Promise<TResult>>, IDelegateContinuePromise
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : CallbackHelper<Promise.ResultContainer, TResult>.InvokeCallbackAndAdoptDirect(continuer, Promise.ResultContainer.Resolved);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var resultContainer = new Promise.ResultContainer(_this._ref._rejectContainer, _this._ref.State);
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : CallbackHelper<Promise.ResultContainer, TResult>.InvokeCallbackAndAdoptDirect(continuer, resultContainer);
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
                    where TDelegateCancel : IFunc<TResult>, IDelegateResolveOrCancel
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : _this;
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var result = _this._ref._result;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? Promise.Resolved(result)
                            : state == Promise.State.Canceled ? InvokeCallbackDirect(canceler)
                            : Promise<TResult>.Rejected(rejectContainer);
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
                    where TDelegateCancel : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : _this;
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var result = _this._ref._result;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? Promise.Resolved(result)
                            : state == Promise.State.Canceled ? InvokeCallbackAndAdoptDirect(canceler)
                            : Promise<TResult>.Rejected(rejectContainer);
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
                internal static Promise<TResult> InvokeCallbackDirect<TDelegate>(TDelegate resolver, in TArg arg)
                    where TDelegate : IFunc<TArg, TResult>
                {
                    try
                    {
                        var result = resolver.Invoke(arg);
                        return Promise.Resolved(result);
                    }
                    catch (Exception e)
                    {
                        return CallbackHelperVoid.FromException<TResult>(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, in TArg arg)
                    where TDelegate : IFunc<TArg, Promise<TResult>>
                {
                    try
                    {
                        return resolver.Invoke(arg).Duplicate();
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
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : InvokeCallbackDirect(resolver, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var arg = _this._ref._result;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackDirect(resolver, arg)
                            : Promise<TResult>.Rejected(rejectContainer);
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
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : InvokeCallbackAndAdoptDirect(resolver, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var arg = _this._ref._result;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackAndAdoptDirect(resolver, arg)
                            : Promise<TResult>.Rejected(rejectContainer);
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
                    where TDelegateReject : IDelegateRejectSynchronous<TResult>, IDelegateReject
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : InvokeCallbackDirect(resolver, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var arg = _this._ref._result;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackDirect(resolver, arg)
                            : CallbackHelperResult<TResult>.InvokeRejecter(rejecter, rejectContainer);
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
                    where TDelegateReject : IDelegateRejectSynchronous<Promise<TResult>>, IDelegateRejectPromise
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : InvokeCallbackAndAdoptDirect(resolver, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var arg = _this._ref._result;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackAndAdoptDirect(resolver, arg)
                            : CallbackHelperResult<TResult>.InvokeRejecterAndAdopt(rejecter, rejectContainer);
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
                    where TDelegateContinue : IFunc<Promise<TArg>.ResultContainer, TResult>, IDelegateContinue
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : CallbackHelper<Promise<TArg>.ResultContainer, TResult>.InvokeCallbackDirect(continuer, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var resultContainer = new Promise<TArg>.ResultContainer(_this._ref._result, _this._ref._rejectContainer, _this._ref.State);
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : CallbackHelper<Promise<TArg>.ResultContainer, TResult>.InvokeCallbackDirect(continuer, resultContainer);
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
                    where TDelegateContinue : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>, IDelegateContinuePromise
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : CallbackHelper<Promise<TArg>.ResultContainer, TResult>.InvokeCallbackAndAdoptDirect(continuer, _this._result);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var resultContainer = new Promise<TArg>.ResultContainer(_this._ref._result, _this._ref._rejectContainer, _this._ref.State);
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested
                            ? Promise<TResult>.Canceled()
                            : CallbackHelper<Promise<TArg>.ResultContainer, TResult>.InvokeCallbackAndAdoptDirect(continuer, resultContainer);
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
                [MethodImpl(MethodImplOptions.NoInlining)]
                internal static Promise InvokeRejecter<TDelegateReject>(TDelegateReject rejecter, IRejectContainer rejectContainer)
                    where TDelegateReject : IDelegateRejectSynchronous
                {
                    try
                    {
                        return rejecter.TryInvokeRejecter(rejectContainer)
                            ? Promise.Resolved()
                            : Promise.Rejected(rejectContainer);
                    }
                    catch (RethrowException)
                    {
                        return Promise.Rejected(rejectContainer);
                    }
                    catch (Exception e)
                    {
                        return FromException(e);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                internal static Promise InvokeRejecterAndAdopt<TDelegateReject>(TDelegateReject rejecter, IRejectContainer rejectContainer)
                    where TDelegateReject : IDelegateRejectSynchronous<Promise>
                {
                    try
                    {
                        return rejecter.TryInvokeRejecter(rejectContainer, out var promise)
                            ? promise.Duplicate()
                            : Promise.Rejected(rejectContainer);
                    }
                    catch (RethrowException)
                    {
                        return Promise.Rejected(rejectContainer);
                    }
                    catch (Exception e)
                    {
                        return FromException(e);
                    }
                }

                internal static Promise FromException(Exception e)
                {
                    return e is OperationCanceledException
                        ? Promise.Canceled()
                        : Promise.Rejected(e);
                }

                internal static Promise<TResult> FromException<TResult>(Exception e)
                {
                    return e is OperationCanceledException
                        ? Promise<TResult>.Canceled()
                        : Promise<TResult>.Rejected(e);
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
                        return runner.Invoke().Duplicate();
                    }
                    catch (Exception e)
                    {
                        return FromException(e);
                    }
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
                            ? Promise.Canceled()
                            : _this;
                    }
                    PromiseRefBase promise;
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise.Canceled()
                            : state == Promise.State.Resolved ? Promise.Resolved()
                            : Promise.Rejected(rejectContainer);
                    }
                    else if (cancelationToken.CanBeCanceled)
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
                            ? Promise<TResult>.Canceled()
                            : _this;
                    }
                    PromiseRef<TResult> promise;
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var result = _this._ref._result;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : state == Promise.State.Resolved ? Promise.Resolved(result)
                            : Promise<TResult>.Rejected(rejectContainer);
                    }
                    else if (cancelationToken.CanBeCanceled)
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

                    if (!forceAsync & synchronizationContext == ts_currentContext)
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

                    if (!forceAsync & synchronizationContext == ts_currentContext)
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
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : InvokeCallbackDirect(resolver);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackDirect(resolver)
                            : Promise.Rejected(rejectContainer);
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
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : InvokeCallbackAndAdoptDirect(resolver);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackAndAdoptDirect(resolver)
                            : Promise.Rejected(rejectContainer);
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
                    where TDelegateReject : IDelegateRejectSynchronous, IDelegateReject
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : InvokeCallbackDirect(resolver);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackDirect(resolver)
                            : InvokeRejecter(rejecter, rejectContainer);
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
                    where TDelegateReject : IDelegateRejectSynchronous<Promise>, IDelegateRejectPromise
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : InvokeCallbackAndAdoptDirect(resolver);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested | state == Promise.State.Canceled ? Promise.Canceled()
                            : state == Promise.State.Resolved ? InvokeCallbackAndAdoptDirect(resolver)
                            : InvokeRejecterAndAdopt(rejecter, rejectContainer);
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
                    where TDelegateContinue : IAction<Promise.ResultContainer>, IDelegateContinue
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : CallbackHelperArg<Promise.ResultContainer>.InvokeCallbackDirect(continuer, Promise.ResultContainer.Resolved);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var resultContainer = new Promise.ResultContainer(_this._ref._rejectContainer, _this._ref.State);
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : CallbackHelperArg<Promise.ResultContainer>.InvokeCallbackDirect(continuer, resultContainer);
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
                    where TDelegateContinue : IFunc<Promise.ResultContainer, Promise>, IDelegateContinuePromise
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : CallbackHelperArg<Promise.ResultContainer>.InvokeCallbackAndAdoptDirect(continuer, Promise.ResultContainer.Resolved);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var resultContainer = new Promise.ResultContainer(_this._ref._rejectContainer, _this._ref.State);
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : CallbackHelperArg<Promise.ResultContainer>.InvokeCallbackAndAdoptDirect(continuer, resultContainer);
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
                    if (_this._ref == null)
                    {
                        return InvokeCallbackDirect(finalizer);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        try
                        {
                            finalizer.Invoke();
                        }
                        catch (Exception e)
                        {
                            // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                            if (state == Promise.State.Rejected)
                            {
                                rejectContainer.ReportUnhandled();
                            }
                            return FromException(e);
                        }
                        return state == Promise.State.Resolved ? Promise.Resolved()
                            : state == Promise.State.Canceled ? Promise.Canceled()
                            : Promise.Rejected(rejectContainer);
                    }
                    var promise = PromiseFinally<VoidResult, TFinalizer>.GetOrCreate(finalizer);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                // This is an uncommon occurrence.
                [MethodImpl(MethodImplOptions.NoInlining)]
                internal static Promise FinallyFromCompleted<TFinalizer>(Promise _this, TFinalizer finalizer)
                    where TFinalizer : IFunc<Promise>
                {
                    var state = _this._ref.State;
                    var rejectContainer = _this._ref._rejectContainer;
                    _this._ref.SuppressRejection = true;
                    _this._ref.MaybeDispose();
                    Promise finallyPromise;
                    try
                    {
                        finallyPromise = finalizer.Invoke();
                    }
                    catch (Exception e)
                    {
                        // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                        if (state == Promise.State.Rejected)
                        {
                            rejectContainer.ReportUnhandled();
                        }
                        return FromException(e);
                    }

                    if (finallyPromise._ref == null)
                    {
                        return state == Promise.State.Resolved ? Promise.Resolved()
                            : state == Promise.State.Canceled ? Promise.Canceled()
                            : Promise.Rejected(rejectContainer);
                    }
                    if (finallyPromise._ref.GetIsCompleted(finallyPromise._id))
                    {
                        var finallyState = finallyPromise._ref.State;
                        var finallyRejectContainer = finallyPromise._ref._rejectContainer;
                        finallyPromise._ref.SuppressRejection = true;
                        finallyPromise._ref.MaybeDispose();
                        if (finallyState == Promise.State.Resolved)
                        {
                            finallyState = state;
                        }
                        else
                        {
                            if (state == Promise.State.Rejected)
                            {
                                // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                                rejectContainer.ReportUnhandled();
                            }
                            rejectContainer = finallyRejectContainer;
                        }
                        return finallyState == Promise.State.Resolved ? Promise.Resolved()
                            : finallyState == Promise.State.Canceled ? Promise.Canceled()
                            : Promise.Rejected(rejectContainer);
                    }

                    var promise = PromiseFinallyWait<VoidResult, DelegatePromiseVoidVoid>.GetOrCreateFromComplete(rejectContainer, state);
                    finallyPromise._ref.HookupNewPromise(finallyPromise._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddFinallyWait<TFinalizer>(Promise _this, TFinalizer finalizer)
                    where TFinalizer : IFunc<Promise>, INullable
                {
                    if (_this._ref == null)
                    {
                        return InvokeCallbackAndAdoptDirect(finalizer);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        return FinallyFromCompleted(_this, finalizer);
                    }
                    var promise = PromiseFinallyWait<VoidResult, TFinalizer>.GetOrCreate(finalizer);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddFinally<TResult, TFinalizer>(Promise<TResult> _this, TFinalizer finalizer)
                    where TFinalizer : IAction
                {
                    if (_this._ref == null)
                    {
                        try
                        {
                            finalizer.Invoke();
                            return Promise.Resolved(_this._result);
                        }
                        catch (Exception e)
                        {
                            return FromException<TResult>(e);
                        }
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var result = _this._ref._result;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        try
                        {
                            finalizer.Invoke();
                        }
                        catch (Exception e)
                        {
                            // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                            if (state == Promise.State.Rejected)
                            {
                                rejectContainer.ReportUnhandled();
                            }
                            return FromException<TResult>(e);
                        }
                        return state == Promise.State.Resolved ? Promise.Resolved(result)
                            : state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : Promise<TResult>.Rejected(rejectContainer);
                    }
                    var promise = PromiseFinally<TResult, TFinalizer>.GetOrCreate(finalizer);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                // This is an uncommon occurrence.
                [MethodImpl(MethodImplOptions.NoInlining)]
                internal static Promise<TResult> FinallyFromCompleted<TResult, TFinalizer>(TFinalizer finalizer, IRejectContainer rejectContainer, TResult result, Promise.State state)
                    where TFinalizer : IFunc<Promise>
                {
                    Promise finallyPromise;
                    try
                    {
                        finallyPromise = finalizer.Invoke();
                    }
                    catch (Exception e)
                    {
                        // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                        if (state == Promise.State.Rejected)
                        {
                            rejectContainer.ReportUnhandled();
                        }
                        return FromException<TResult>(e);
                    }

                    if (finallyPromise._ref == null)
                    {
                        return state == Promise.State.Resolved ? Promise.Resolved(result)
                            : state == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : Promise<TResult>.Rejected(rejectContainer);
                    }
                    if (finallyPromise._ref.GetIsCompleted(finallyPromise._id))
                    {
                        var finallyState = finallyPromise._ref.State;
                        var finallyRejectContainer = finallyPromise._ref._rejectContainer;
                        finallyPromise._ref.SuppressRejection = true;
                        finallyPromise._ref.MaybeDispose();
                        if (finallyState == Promise.State.Resolved)
                        {
                            finallyState = state;
                        }
                        else
                        {
                            if (state == Promise.State.Rejected)
                            {
                                // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                                rejectContainer.ReportUnhandled();
                            }
                            rejectContainer = finallyRejectContainer;
                        }
                        return finallyState == Promise.State.Resolved ? Promise.Resolved(result)
                            : finallyState == Promise.State.Canceled ? Promise<TResult>.Canceled()
                            : Promise<TResult>.Rejected(rejectContainer);
                    }

                    var promise = PromiseFinallyWait<TResult, DelegatePromiseVoidVoid>.GetOrCreateFromComplete(rejectContainer, state);
                    finallyPromise._ref.HookupNewPromise(finallyPromise._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddFinallyWait<TResult, TFinalizer>(Promise<TResult> _this, TFinalizer finalizer)
                    where TFinalizer : IFunc<Promise>, INullable
                {
                    if (_this._ref == null)
                    {
                        return FinallyFromCompleted(finalizer, null, _this._result, Promise.State.Resolved);
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var result = _this._ref._result;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return FinallyFromCompleted(finalizer, rejectContainer, result, state);
                    }
                    var promise = PromiseFinallyWait<TResult, TFinalizer>.GetOrCreate(finalizer);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddCancel<TDelegateCancel>(Promise _this, TDelegateCancel canceler, CancelationToken cancelationToken)
                    where TDelegateCancel : IAction, IDelegateResolveOrCancel
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : _this;
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested ? Promise.Canceled()
                            : state == Promise.State.Resolved ? Promise.Resolved()
                            : state == Promise.State.Canceled ? InvokeCallbackDirect(canceler)
                            : Promise.Rejected(rejectContainer);
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
                    where TDelegateCancel : IFunc<Promise>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null)
                    {
                        return cancelationToken.IsCancelationRequested
                            ? Promise.Canceled()
                            : _this;
                    }
                    if (_this._ref.GetIsCompleted(_this._id))
                    {
                        var state = _this._ref.State;
                        var rejectContainer = _this._ref._rejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeDispose();
                        return cancelationToken.IsCancelationRequested ? Promise.Canceled()
                            : state == Promise.State.Resolved ? Promise.Resolved()
                            : state == Promise.State.Canceled ? InvokeCallbackAndAdoptDirect(canceler)
                            : Promise.Rejected(rejectContainer);
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