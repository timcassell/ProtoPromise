#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable CS0618 // Type or member is obsolete - .Duplicate()

using Proto.Timers;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
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
                        return Promise.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, in Promise<TArg> resolved)
                    where TDelegate : IFunc<TArg, Promise>
                {
                    try
                    {
                        return resolver.Invoke(CallbackHelperVoid.GetResultFromResolved(resolved)).Duplicate();
                    }
                    catch (Exception e)
                    {
                        return Promise.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolve<TDelegate>(Promise<TArg> _this, TDelegate resolver)
                    where TDelegate : IAction<TArg>, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = PromiseResolve<TArg, TDelegate>.GetOrCreate(resolver);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveWait<TDelegate>(Promise<TArg> _this, TDelegate resolver)
                    where TDelegate : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    var promise = PromiseResolvePromise<VoidResult, TDelegate>.GetOrCreate(resolver);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveReject<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve resolver, TDelegateReject rejecter)
                    where TDelegateResolve : IAction<TArg>, IDelegateResolveOrCancel
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = PromiseResolveReject<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve resolver, TDelegateReject rejecter)
                    where TDelegateResolve : IFunc<TArg, Promise>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    var promise = PromiseResolveRejectPromise<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, VoidResult>
                    => CallbackHelper<TArg, VoidResult>.ContinueWith<Promise<TArg>.ResultContainer, VoidResult, TDelegate, ContinueWithContinuer, TTransformer<TArg>, VoidTransformer>(_this, callback);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
                    => CallbackHelper<TArg, VoidResult>.ContinueWith<Promise<TArg>.ResultContainer, Promise, TDelegate, ContinueWithContinuer, TTransformer<TArg>, VoidTransformer>(_this, callback);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, VoidResult>
                    => CallbackHelper<TArg, VoidResult>.ContinueWith<Promise<TArg>.ResultContainer, VoidResult, TDelegate, ContinueWithContinuer, TTransformer<TArg>, VoidTransformer>(_this, callback, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
                    => CallbackHelper<TArg, VoidResult>.ContinueWith<Promise<TArg>.ResultContainer, Promise, TDelegate, ContinueWithContinuer, TTransformer<TArg>, VoidTransformer>(_this, callback, cancelationToken);
            } // class CallbackHelperArg<TArg>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelperResult<TResult>
            {
                internal static Promise<TResult> Canceled(PromiseRefBase _ref, short promiseId)
                {
                    _ref?.MaybeMarkAwaitedAndDispose(promiseId);
                    return Promise<TResult>.Canceled();
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
                        return Promise<TResult>.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, in Promise resolved)
                    where TDelegate : IFunc<Promise<TResult>>
                {
                    try
                    {
                        resolved._ref?.MaybeMarkAwaitedAndDispose(resolved._id);
                        return resolver.Invoke().Duplicate();
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.FromException(e);
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
                        return Promise<TResult>.FromException(e);
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
                        return Promise<TResult>.FromException(e);
                    }
                }

                internal static Promise<TResult> Duplicate(Promise<TResult> _this)
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }
                    var newRef = _this._ref.GetDuplicateT(_this._id);
                    return new Promise<TResult>(newRef, newRef.Id, _this._result);
                }

                internal static Promise<TResult> WaitAsync(Promise<TResult> _this, CancelationToken cancelationToken)
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Canceled(_this._ref, _this._id);
                    }
                    if (_this._ref?.State != Promise.State.Pending || !cancelationToken.CanBeCanceled)
                    {
                        return Duplicate(_this);
                    }
                    var promise = WaitAsyncWithCancelationPromise<TResult>.GetOrCreate();
                    _this._ref.HookupCancelablePromise(promise, _this._id, cancelationToken, ref promise._cancelationHelper);
                    return new Promise<TResult>(promise, promise.Id, _this._result);
                }

                internal static Promise<TResult> WaitAsync(Promise<TResult> _this, TimeSpan timeout, TimerFactory timerFactory)
                {
                    if (_this._ref?.State != Promise.State.Pending || timeout == Timeout.InfiniteTimeSpan)
                    {
                        return Duplicate(_this);
                    }
                    if (timeout == TimeSpan.Zero)
                    {
                        _this._ref?.MaybeMarkAwaitedAndDispose(_this._id);
                        return Promise<TResult>.Rejected(new TimeoutException());
                    }
                    var promise = WaitAsyncWithTimeoutPromise<TResult>.GetOrCreate(timeout, timerFactory);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> WaitAsync(Promise<TResult> _this, TimeSpan timeout, TimerFactory timerFactory, CancelationToken cancelationToken)
                {
                    if (!cancelationToken.CanBeCanceled)
                    {
                        return WaitAsync(_this, timeout, timerFactory);
                    }
                    if (timeout == Timeout.InfiniteTimeSpan)
                    {
                        return WaitAsync(_this, cancelationToken);
                    }

                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Canceled(_this._ref, _this._id);
                    }
                    if (_this._ref?.State != Promise.State.Pending)
                    {
                        return Duplicate(_this);
                    }
                    if (timeout == TimeSpan.Zero)
                    {
                        _this._ref?.MaybeMarkAwaitedAndDispose(_this._id);
                        return Promise<TResult>.Rejected(new TimeoutException());
                    }
                    var promise = WaitAsyncWithTimeoutAndCancelationPromise<TResult>.GetOrCreateAndHookup(_this._ref, _this._id, timeout, timerFactory, cancelationToken);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> ConfigureContinuation(Promise<TResult> _this, ContinuationOptions continuationOptions)
                {
                    if (continuationOptions.IsSynchronous)
                    {
                        return Duplicate(_this);
                    }

                    if (continuationOptions.CompletedBehavior == CompletedContinuationBehavior.Synchronous
                        && (_this._ref == null || _this._ref.State != Promise.State.Pending))
                    {
                        return Duplicate(_this);
                    }

                    var synchronizationContext = continuationOptions.GetContinuationContext();
                    ConfiguredPromise<TResult> promise;
                    if (_this._ref == null)
                    {
                        promise = ConfiguredPromise<TResult>.GetOrCreateFromResolved(synchronizationContext, _this._result, continuationOptions.CompletedBehavior);
                    }
                    else
                    {
                        promise = ConfiguredPromise<TResult>.GetOrCreate(synchronizationContext, continuationOptions.CompletedBehavior);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, _this._result);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> New<TDelegate>(TDelegate runner, ContinuationOptions invokeOptions)
                    where TDelegate : IDelegateNew<TResult>
                {
                    var promise = DeferredNewPromise<TResult, TDelegate>.GetOrCreate(runner);
                    promise.RunOrScheduleOnContext(invokeOptions);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Run<TDelegate>(TDelegate runner, ContinuationOptions invokeOptions)
                    where TDelegate : IFunc<TResult>, IDelegateRun
                {
                    if (invokeOptions.GetShouldContinueImmediately(out var context))
                    {
                        return InvokeCallbackDirect(runner);
                    }

                    var promise = RunPromise<TResult, TDelegate>.GetOrCreate(runner);
                    promise.ScheduleOnContext(context);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> RunWait<TDelegate>(TDelegate runner, ContinuationOptions invokeOptions)
                    where TDelegate : IFunc<Promise<TResult>>, IDelegateRunPromise
                {
                    if (invokeOptions.GetShouldContinueImmediately(out var context))
                    {
                        return InvokeCallbackAndAdoptDirect(runner);
                    }

                    var promise = RunWaitPromise<TResult, TDelegate>.GetOrCreate(runner);
                    promise.ScheduleOnContext(context);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TDelegate>(Promise _this, TDelegate resolver)
                    where TDelegate : IFunc<TResult>, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = PromiseResolve<TResult, TDelegate>.GetOrCreate(resolver);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TDelegate>(Promise _this, TDelegate resolver)
                    where TDelegate : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    var promise = PromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve resolver, TDelegateReject rejecter)
                    where TDelegateResolve : IFunc<TResult>, IDelegateResolveOrCancel
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = PromiseResolveReject<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve resolver, TDelegateReject rejecter)
                    where TDelegateResolve : IFunc<Promise<TResult>>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    var promise = PromiseResolveRejectPromise<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancel<TDelegateCancel>(Promise<TResult> _this, TDelegateCancel canceler)
                    where TDelegateCancel : IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return Duplicate(_this);
                    }
                    var promise = PromiseCancel<TResult, TDelegateCancel>.GetOrCreate(canceler);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancelWait<TDelegateCancel>(Promise<TResult> _this, TDelegateCancel canceler)
                    where TDelegateCancel : IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return Duplicate(_this);
                    }
                    var promise = PromiseCancelPromise<TResult, TDelegateCancel>.GetOrCreate(canceler);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise.ResultContainer, TResult>
                    => CallbackHelper<VoidResult, TResult>.ContinueWith<Promise.ResultContainer, TResult, TDelegate, ContinueWithContinuer, VoidTransformer, TTransformer<TResult>>(_this, callback);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
                    => CallbackHelper<VoidResult, TResult>.ContinueWith<Promise.ResultContainer, Promise<TResult>, TDelegate, ContinueWithContinuer, VoidTransformer, TTransformer<TResult>>(_this, callback);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, TResult>
                    => CallbackHelper<VoidResult, TResult>.ContinueWith<Promise.ResultContainer, TResult, TDelegate, ContinueWithContinuer, VoidTransformer, TTransformer<TResult>>(_this, callback, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
                    => CallbackHelper<VoidResult, TResult>.ContinueWith<Promise.ResultContainer, Promise<TResult>, TDelegate, ContinueWithContinuer, VoidTransformer, TTransformer<TResult>>(_this, callback, cancelationToken);
            } // class CallbackHelperResult<TResult>

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
                        return Promise<TResult>.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise<TResult> InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, in Promise<TArg> resolved)
                    where TDelegate : IFunc<TArg, Promise<TResult>>
                {
                    try
                    {
                        return resolver.Invoke(CallbackHelperVoid.GetResultFromResolved(resolved)).Duplicate();
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TDelegate>(Promise<TArg> _this, TDelegate resolver)
                    where TDelegate : IFunc<TArg, TResult>, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = PromiseResolve<TResult, TDelegate>.GetOrCreate(resolver);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TDelegate>(Promise<TArg> _this, TDelegate resolver)
                    where TDelegate : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    var promise = PromiseResolvePromise<TResult, TDelegate>.GetOrCreate(resolver);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve resolver, TDelegateReject rejecter)
                    where TDelegateResolve : IFunc<TArg, TResult>, IDelegateResolveOrCancel
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = PromiseResolveReject<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve resolver, TDelegateReject rejecter)
                    where TDelegateResolve : IFunc<TArg, Promise<TResult>>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    var promise = PromiseResolveRejectPromise<TResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
                    => ContinueWith<Promise<TArg>.ResultContainer, TResult, TDelegate, ContinueWithContinuer, TTransformer<TArg>, TTransformer<TResult>>(_this, callback);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
                    => ContinueWith<Promise<TArg>.ResultContainer, Promise<TResult>, TDelegate, ContinueWithContinuer, TTransformer<TArg>, TTransformer<TResult>>(_this, callback);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
                    => ContinueWith<Promise<TArg>.ResultContainer, TResult, TDelegate, ContinueWithContinuer, TTransformer<TArg>, TTransformer<TResult>>(_this, callback, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
                    => ContinueWith<Promise<TArg>.ResultContainer, Promise<TResult>, TDelegate, ContinueWithContinuer, TTransformer<TArg>, TTransformer<TResult>>(_this, callback, cancelationToken);

                // Unity IL2CPP doesn't generate necessary code when simply using `default(TType).Method`, so we have to pass in instances of TContinuer, TArgTransformer, TResultTransformer.
                // They are not stored as fields, so they do not consume any memory, and it is later safe to use `default(TType).Method` in the ContinuePromise class.
                internal static Promise<TResult> ContinueWith<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                    PromiseWrapper<TArg> _this, in TDelegate callback,
                    TContinuer continuer = default, TArgTransformer argTransformer = default, TResultTransformer resultTransformer = default)
                    where TDelegate : IFunc<TDelegateArg, TDelegateResult>
                    where TContinuer : struct, IContinuer
                    where TArgTransformer : struct, ITransformer<Promise<TArg>.ResultContainer, TDelegateArg>
                    where TResultTransformer : struct, ITransformer<TDelegateResult, PromiseWrapper<TResult>>
                {
                    // .Catch(Cancelation) APIs return the same Promise type, so the continuer must be a type that should invoke if the state is resolved.
                    Debug.Assert(continuer.ShouldInvoke(null, Promise.State.Resolved, out _));

                    if (_this._ref == null)
                    {
                        try
                        {
                            var delArg = argTransformer.Transform(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved));
                            var delResult = callback.Invoke(delArg);
                            return resultTransformer.Transform(delResult).AsPromise().Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    var state = _this._ref.State;
                    if (state != Promise.State.Pending)
                    {
                        var arg = _this._ref.GetResult<TArg>();
                        var rejectContainer = _this._ref.RejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        if (continuer.ShouldInvoke(rejectContainer, state, out var invokeType))
                        {
                            var delArg = argTransformer.Transform(new Promise<TArg>.ResultContainer(arg, rejectContainer, state));
                            try
                            {
                                var delResult = callback.Invoke(delArg);
                                return resultTransformer.Transform(delResult).AsPromise().Duplicate();
                            }
                            catch (RethrowException e)
                            {
                                // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                                return state == Promise.State.Rejected && invokeType == Promise.State.Rejected
                                    ? Promise<TResult>.Rejected(rejectContainer)
                                    : Promise<TResult>.Rejected(e);
                            }
                            catch (Exception e)
                            {
                                return Promise<TResult>.FromException(e);
                            }
                        }

                        Debug.Assert(state == Promise.State.Canceled || state == Promise.State.Rejected);
                        return state == Promise.State.Canceled
                            ? Promise<TResult>.Canceled()
                            : Promise<TResult>.Rejected(rejectContainer);
                    }

                    var promise = ContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> ContinueWith<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                    PromiseWrapper<TArg> _this, in TDelegate callback, CancelationToken cancelationToken,
                    TContinuer continuer = default, TArgTransformer argTransformer = default, TResultTransformer resultTransformer = default)
                    where TDelegate : IFunc<TDelegateArg, TDelegateResult>
                    where TContinuer : struct, IContinuer
                    where TArgTransformer : struct, ITransformer<Promise<TArg>.ResultContainer, TDelegateArg>
                    where TResultTransformer : struct, ITransformer<TDelegateResult, PromiseWrapper<TResult>>
                {
                    // .Catch(Cancelation) APIs return the same Promise type, so the continuer must be a type that should invoke if the state is resolved.
                    Debug.Assert(continuer.ShouldInvoke(null, Promise.State.Resolved, out _));

                    if (cancelationToken.IsCancelationRequested)
                    {
                        return CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        try
                        {
                            var delArg = argTransformer.Transform(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved));
                            var delResult = callback.Invoke(delArg);
                            return resultTransformer.Transform(delResult).AsPromise().Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    var state = _this._ref.State;
                    if (state != Promise.State.Pending)
                    {
                        var arg = _this._ref.GetResult<TArg>();
                        var rejectContainer = _this._ref.RejectContainer;
                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        if (continuer.ShouldInvoke(rejectContainer, state, out var invokeType))
                        {
                            var delArg = argTransformer.Transform(new Promise<TArg>.ResultContainer(arg, rejectContainer, state));
                            try
                            {
                                var delResult = callback.Invoke(delArg);
                                return resultTransformer.Transform(delResult).AsPromise().Duplicate();
                            }
                            catch (RethrowException e)
                            {
                                // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                                return state == Promise.State.Rejected && invokeType == Promise.State.Rejected
                                    ? Promise<TResult>.Rejected(rejectContainer)
                                    : Promise<TResult>.Rejected(e);
                            }
                            catch (Exception e)
                            {
                                return Promise<TResult>.FromException(e);
                            }
                        }

                        Debug.Assert(state == Promise.State.Canceled || state == Promise.State.Rejected);
                        return state == Promise.State.Canceled
                            ? Promise<TResult>.Canceled()
                            : Promise<TResult>.Rejected(rejectContainer);
                    }

                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>.GetOrCreate(callback);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>.GetOrCreate(callback);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
            } // class CallbackHelper<TArg, TResult>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelperVoid
            {
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
                        return Promise.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                private static Promise InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, in Promise resolved)
                    where TDelegate : IFunc<Promise>
                {
                    try
                    {
                        resolved._ref?.MaybeMarkAwaitedAndDispose(resolved._id);
                        return resolver.Invoke().Duplicate();
                    }
                    catch (Exception e)
                    {
                        return Promise.FromException(e);
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
                        return Promise.FromException(e);
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
                        return Promise.FromException(e);
                    }
                }

                internal static Promise Canceled(PromiseRefBase _ref, short promiseId)
                {
                    _ref?.MaybeMarkAwaitedAndDispose(promiseId);
                    return Promise.Canceled();
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

                internal static Promise WaitAsync(Promise _this, CancelationToken cancelationToken)
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Canceled(_this._ref, _this._id);
                    }
                    if (_this._ref?.State != Promise.State.Pending || !cancelationToken.CanBeCanceled)
                    {
                        return Duplicate(_this);
                    }
                    var promise = WaitAsyncWithCancelationPromise<VoidResult>.GetOrCreate();
                    _this._ref.HookupCancelablePromise(promise, _this._id, cancelationToken, ref promise._cancelationHelper);
                    return new Promise(promise, promise.Id);
                }

                internal static Promise WaitAsync(Promise _this, TimeSpan timeout, TimerFactory timerFactory)
                {
                    if (_this._ref?.State != Promise.State.Pending || timeout == Timeout.InfiniteTimeSpan)
                    {
                        return Duplicate(_this);
                    }
                    if (timeout == TimeSpan.Zero)
                    {
                        _this._ref?.MaybeMarkAwaitedAndDispose(_this._id);
                        return Promise.Rejected(new TimeoutException());
                    }
                    var promise = WaitAsyncWithTimeoutPromise<VoidResult>.GetOrCreate(timeout, timerFactory);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                internal static Promise WaitAsync(Promise _this, TimeSpan timeout, TimerFactory timerFactory, CancelationToken cancelationToken)
                {
                    if (!cancelationToken.CanBeCanceled)
                    {
                        return WaitAsync(_this, timeout, timerFactory);
                    }
                    if (timeout == Timeout.InfiniteTimeSpan)
                    {
                        return WaitAsync(_this, cancelationToken);
                    }

                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Canceled(_this._ref, _this._id);
                    }
                    if (_this._ref?.State != Promise.State.Pending)
                    {
                        return Duplicate(_this);
                    }
                    if (timeout == TimeSpan.Zero)
                    {
                        _this._ref?.MaybeMarkAwaitedAndDispose(_this._id);
                        return Promise.Rejected(new TimeoutException());
                    }
                    var promise = WaitAsyncWithTimeoutAndCancelationPromise<VoidResult>.GetOrCreateAndHookup(_this._ref, _this._id, timeout, timerFactory, cancelationToken);
                    return new Promise(promise, promise.Id);
                }

                internal static Promise ConfigureContinuation(Promise _this, ContinuationOptions continuationOptions)
                {
                    if (continuationOptions.IsSynchronous)
                    {
                        return Duplicate(_this);
                    }

                    if (continuationOptions.CompletedBehavior == CompletedContinuationBehavior.Synchronous
                        && (_this._ref == null || _this._ref.State != Promise.State.Pending))
                    {
                        return Duplicate(_this);
                    }

                    var synchronizationContext = continuationOptions.GetContinuationContext();
                    ConfiguredPromise<VoidResult> promise;
                    if (_this._ref == null)
                    {
                        promise = ConfiguredPromise<VoidResult>.GetOrCreateFromResolved(synchronizationContext, default, continuationOptions.CompletedBehavior);
                    }
                    else
                    {
                        promise = ConfiguredPromise<VoidResult>.GetOrCreate(synchronizationContext, continuationOptions.CompletedBehavior);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise New<TDelegate>(TDelegate runner, ContinuationOptions invokeOptions)
                    where TDelegate : IDelegateNew<VoidResult>
                {
                    var promise = DeferredNewPromise<VoidResult, TDelegate>.GetOrCreate(runner);
                    promise.RunOrScheduleOnContext(invokeOptions);
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Run<TDelegate>(TDelegate runner, ContinuationOptions invokeOptions)
                    where TDelegate : IAction, IDelegateRun
                {
                    if (invokeOptions.GetShouldContinueImmediately(out var context))
                    {
                        return InvokeCallbackDirect(runner);
                    }

                    var promise = RunPromise<VoidResult, TDelegate>.GetOrCreate(runner);
                    promise.ScheduleOnContext(context);
                    return new Promise(promise, promise.Id);
                }

                internal static Promise RunWait<TDelegate>(TDelegate runner, ContinuationOptions invokeOptions)
                    where TDelegate : IFunc<Promise>, IDelegateRunPromise
                {
                    if (invokeOptions.GetShouldContinueImmediately(out var context))
                    {
                        return InvokeCallbackAndAdoptDirect(runner);
                    }

                    var promise = RunWaitPromise<VoidResult, TDelegate>.GetOrCreate(runner);
                    promise.ScheduleOnContext(context);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolve<TDelegate>(Promise _this, TDelegate resolver)
                    where TDelegate : IAction, IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = PromiseResolve<VoidResult, TDelegate>.GetOrCreate(resolver);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveWait<TDelegate>(Promise _this, TDelegate resolver)
                    where TDelegate : IFunc<Promise>, IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    var promise = PromiseResolvePromise<VoidResult, TDelegate>.GetOrCreate(resolver);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveReject<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve resolver, TDelegateReject rejecter)
                    where TDelegateResolve : IAction, IDelegateResolveOrCancel
                    where TDelegateReject : IDelegateReject
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackDirect(resolver, _this);
                    }
                    var promise = PromiseResolveReject<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddResolveRejectWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve resolver, TDelegateReject rejecter)
                    where TDelegateResolve : IFunc<Promise>, IDelegateResolveOrCancelPromise
                    where TDelegateReject : IDelegateRejectPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return InvokeCallbackAndAdoptDirect(resolver, _this);
                    }
                    var promise = PromiseResolveRejectPromise<VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(resolver, rejecter);
                    _this._ref.HookupNewPromise(_this._id, promise);
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
                            return Promise<TResult>.FromException(e);
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
                            return Promise<TResult>.FromException(e);
                        }
                    }
                    var promise = PromiseFinallyWait<TResult, TFinalizer>.GetOrCreate(finalizer);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddCancel<TDelegateCancel>(Promise _this, TDelegateCancel canceler)
                    where TDelegateCancel : IDelegateResolveOrCancel
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return Duplicate(_this);
                    }
                    var promise = PromiseCancel<VoidResult, TDelegateCancel>.GetOrCreate(canceler);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddCancelWait<TDelegateCancel>(Promise _this, TDelegateCancel canceler)
                    where TDelegateCancel : IDelegateResolveOrCancelPromise
                {
                    if (_this._ref == null || _this._ref.State == Promise.State.Resolved)
                    {
                        return Duplicate(_this);
                    }
                    var promise = PromiseCancelPromise<VoidResult, TDelegateCancel>.GetOrCreate(canceler);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise.ResultContainer, VoidResult>
                    => CallbackHelper<VoidResult, VoidResult>.ContinueWith<Promise.ResultContainer, VoidResult, TDelegate, ContinueWithContinuer, VoidTransformer, VoidTransformer>(_this, callback);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise>
                    => CallbackHelper<VoidResult, VoidResult>.ContinueWith<Promise.ResultContainer, Promise, TDelegate, ContinueWithContinuer, VoidTransformer, VoidTransformer>(_this, callback);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, VoidResult>
                    => CallbackHelper<VoidResult, VoidResult>.ContinueWith<Promise.ResultContainer, VoidResult, TDelegate, ContinueWithContinuer, VoidTransformer, VoidTransformer>(_this, callback, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise>
                    => CallbackHelper<VoidResult, VoidResult>.ContinueWith<Promise.ResultContainer, Promise, TDelegate, ContinueWithContinuer, VoidTransformer, VoidTransformer>(_this, callback, cancelationToken);
            } // class CallbackHelperVoid
        } // PromiseRefBase
    } // Internal
} // namespace Proto.Promises