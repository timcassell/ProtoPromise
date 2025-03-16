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
                internal static Promise Then<TDelegate>(Promise<TArg> _this, TDelegate onResolve)
                    where TDelegate : IFunc<TArg, VoidResult>
                    => CallbackHelper<TArg, VoidResult>.Then(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegate>(Promise<TArg> _this, TDelegate onResolve)
                    where TDelegate : IFunc<TArg, Promise>
                    => CallbackHelper<TArg, VoidResult>.ContinueWithWait<TArg, Promise, TDelegate, ThenResolveContinuer, TTransformer<TArg>, VoidTransformer>(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TArg, VoidResult>
                    where TDelegateReject : IFunc<VoidResult, VoidResult>
                    => CallbackHelper<TArg, VoidResult>.Then(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TArg, Promise>
                    where TDelegateReject : IFunc<VoidResult, Promise>
                    => CallbackHelper<TArg, VoidResult>.ContinueWithWait
                    <Promise<TArg>.ResultContainer, Promise, ThenDelegate<TArg, Promise, TDelegateResolve, TDelegateReject>, ThenResolveRejectContinuer, TTransformer<TArg>, VoidTransformer>(
                        _this, new ThenDelegate<TArg, Promise, TDelegateResolve, TDelegateReject>(onResolve, onReject));

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    [MethodImpl(InlineOption)]
                    internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, VoidResult>
                        where TDelegateReject : IFunc<TReject, VoidResult>
                        => CallbackHelper<TArg, VoidResult>.Filter<TReject>.Then(_this, onResolve, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, Promise>
                        where TDelegateReject : IFunc<TReject, Promise>
                        => CallbackHelper<TArg, VoidResult>.ContinueWithWait
                        <Promise<TArg>.ResultContainer, Promise, ThenFilteredDelegate<TArg, Promise, TReject, TDelegateResolve, TDelegateReject>, ThenResolveRejectFilteredContinuer<TReject>, TTransformer<TArg>, VoidTransformer>(
                            _this, new ThenFilteredDelegate<TArg, Promise, TReject, TDelegateResolve, TDelegateReject>(onResolve, onReject));
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, VoidResult>
                    => CallbackHelper<TArg, VoidResult>.ContinueWith<Promise<TArg>.ResultContainer, VoidResult, TDelegate, ContinueWithContinuer, TTransformer<TArg>, VoidTransformer>(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
                    => CallbackHelper<TArg, VoidResult>.ContinueWithWait<Promise<TArg>.ResultContainer, Promise, TDelegate, ContinueWithContinuer, TTransformer<TArg>, VoidTransformer>(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, VoidResult>
                    => CallbackHelper<TArg, VoidResult>.ContinueWith<Promise<TArg>.ResultContainer, VoidResult, TDelegate, ContinueWithContinuer, TTransformer<TArg>, VoidTransformer>(_this, onContinue, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
                    => CallbackHelper<TArg, VoidResult>.ContinueWithWait<Promise<TArg>.ResultContainer, Promise, TDelegate, ContinueWithContinuer, TTransformer<TArg>, VoidTransformer>(_this, onContinue, cancelationToken);
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
                internal static Promise<TResult> Then<TDelegate>(Promise _this, TDelegate onResolve)
                    where TDelegate : IFunc<VoidResult, TResult>
                    => CallbackHelper<VoidResult, TResult>.ContinueWith<VoidResult, TResult, TDelegate, ThenResolveContinuer, VoidTransformer, TTransformer<TResult>>(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegate>(Promise _this, TDelegate onResolve)
                    where TDelegate : IFunc<VoidResult, Promise<TResult>>
                    => CallbackHelper<VoidResult, TResult>.ContinueWithWait<VoidResult, Promise<TResult>, TDelegate, ThenResolveContinuer, VoidTransformer, TTransformer<TResult>>(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<VoidResult, TResult>
                    where TDelegateReject : IFunc<VoidResult, TResult>
                    => CallbackHelper<VoidResult, TResult>.Then(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<VoidResult, Promise<TResult>>
                    where TDelegateReject : IFunc<VoidResult, Promise<TResult>>
                    => CallbackHelper<VoidResult, TResult>.ThenWait(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Catch<TDelegate>(Promise<TResult> _this, TDelegate onReject)
                    where TDelegate : IFunc<VoidResult, TResult>
                    => Catch<VoidResult, TResult, TDelegate, CatchContinuer, CatchTransformer<TResult>, TTransformer<TResult>>(_this, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> CatchWait<TDelegate>(Promise<TResult> _this, TDelegate onReject)
                    where TDelegate : IFunc<VoidResult, Promise<TResult>>
                    => CatchWait<VoidResult, Promise<TResult>, TDelegate, CatchContinuer, CatchTransformer<TResult>, TTransformer<TResult>>(_this, onReject);

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<VoidResult, TResult>
                        where TDelegateReject : IFunc<TReject, TResult>
                        => CallbackHelper<VoidResult, TResult>.Filter<TReject>.Then(_this, onResolve, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<VoidResult, Promise<TResult>>
                        where TDelegateReject : IFunc<TReject, Promise<TResult>>
                        => CallbackHelper<VoidResult, TResult>.Filter<TReject>.ThenWait(_this, onResolve, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> Catch<TDelegate>(Promise<TResult> _this, TDelegate onReject)
                        where TDelegate : IFunc<TReject, TResult>
                        => Catch<TReject, TResult, TDelegate, CatchFilteredContinuer<TReject>, CatchTransformer<TResult, TReject>, TTransformer<TResult>>(_this, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> CatchWait<TDelegate>(Promise<TResult> _this, TDelegate onReject)
                        where TDelegate : IFunc<TReject, Promise<TResult>>
                        => CatchWait<TReject, Promise<TResult>, TDelegate, CatchFilteredContinuer<TReject>, CatchTransformer<TResult, TReject>, TTransformer<TResult>>(_this, onReject);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> CatchCancelation<TDelegate>(Promise<TResult> _this, TDelegate onCancel)
                    where TDelegate : IFunc<VoidResult, TResult>
                    => Catch<VoidResult, TResult, TDelegate, CatchCancelationContinuer, CatchCancelationTransformer<TResult>, TTransformer<TResult>>(_this, onCancel);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> CatchCancelationWait<TDelegate>(Promise<TResult> _this, TDelegate onCancel)
                    where TDelegate : IFunc<VoidResult, Promise<TResult>>
                    => CatchWait<VoidResult, Promise<TResult>, TDelegate, CatchCancelationContinuer, CatchCancelationTransformer<TResult>, TTransformer<TResult>>(_this, onCancel);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise.ResultContainer, TResult>
                    => CallbackHelper<VoidResult, TResult>.ContinueWith<Promise.ResultContainer, TResult, TDelegate, ContinueWithContinuer, VoidTransformer, TTransformer<TResult>>(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
                    => CallbackHelper<VoidResult, TResult>.ContinueWithWait<Promise.ResultContainer, Promise<TResult>, TDelegate, ContinueWithContinuer, VoidTransformer, TTransformer<TResult>>(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, TResult>
                    => CallbackHelper<VoidResult, TResult>.ContinueWith<Promise.ResultContainer, TResult, TDelegate, ContinueWithContinuer, VoidTransformer, TTransformer<TResult>>(_this, onContinue, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
                    => CallbackHelper<VoidResult, TResult>.ContinueWithWait<Promise.ResultContainer, Promise<TResult>, TDelegate, ContinueWithContinuer, VoidTransformer, TTransformer<TResult>>(
                        _this, onContinue, cancelationToken);

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static PromiseWrapper<TResult> MaybeInvokeCatchFromCompletedReference<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                    in PromiseWrapper<TResult> _this, in TDelegate callback, TContinuer continuer, TArgTransformer argTransformer, TResultTransformer resultTransformer)
                    where TDelegate : IFunc<TDelegateArg, TDelegateResult>
                    where TContinuer : struct, IContinuer
                    where TArgTransformer : struct, ITransformer<Promise<TResult>.ResultContainer, TDelegateArg>
                    where TResultTransformer : struct, ITransformer<TDelegateResult, PromiseWrapper<TResult>>
                {
                    var state = _this._ref.State;
                    var rejectContainer = _this._ref.RejectContainer;
                    if (!continuer.ShouldInvoke(rejectContainer, state, out var invokeTypes))
                    {
                        var duplicate = _this._ref.GetDuplicate(_this._id);
                        return new PromiseWrapper<TResult>(duplicate, duplicate.Id, default);
                    }

                    _this._ref.SuppressRejection = true;
                    var arg = _this._ref.GetResult<TResult>();
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    var delArg = argTransformer.Transform(new Promise<TResult>.ResultContainer(arg, rejectContainer, state));
                    TDelegateResult result;
                    try
                    {
                        result = callback.Invoke(delArg);
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        return state == Promise.State.Rejected && (invokeTypes & InvokeTypes.Rejected) != 0 && (invokeTypes & InvokeTypes.Canceled) == 0
                            ? Promise<TResult>.Rejected(rejectContainer)
                            : Promise<TResult>.Rejected(e);
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.FromException(e);
                    }
                    return resultTransformer.Transform(result).Duplicate();
                }

                internal static PromiseWrapper<TResult> Catch<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                    PromiseWrapper<TResult> _this, in TDelegate callback,
                    TContinuer continuer = default, TArgTransformer argTransformer = default, TResultTransformer resultTransformer = default)
                    where TDelegate : IFunc<TDelegateArg, TDelegateResult>
                    where TContinuer : struct, IContinuer
                    where TArgTransformer : struct, ITransformer<Promise<TResult>.ResultContainer, TDelegateArg>
                    where TResultTransformer : struct, ITransformer<TDelegateResult, PromiseWrapper<TResult>>
                {
                    // .Catch(Cancelation) APIs must not invoke if the state is resolved.
                    Debug.Assert(!continuer.ShouldInvoke(null, Promise.State.Resolved, out var invokeTypes));
                    Debug.Assert((invokeTypes & InvokeTypes.Resolved) == 0);

                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeCatchFromCompletedReference<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                            _this, callback, continuer, argTransformer, resultTransformer);
                    }

                    var promise = ContinuePromise<TResult, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> CatchWait<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                    PromiseWrapper<TResult> _this, in TDelegate callback,
                    TContinuer continuer = default, TArgTransformer argTransformer = default, TResultTransformer resultTransformer = default)
                    where TDelegate : IFunc<TDelegateArg, TDelegateResult>
                    where TContinuer : struct, IContinuer
                    where TArgTransformer : struct, ITransformer<Promise<TResult>.ResultContainer, TDelegateArg>
                    where TResultTransformer : struct, ITransformer<TDelegateResult, PromiseWrapper<TResult>>
                {
                    // .Catch(Cancelation) APIs must not invoke if the state is resolved.
                    Debug.Assert(!continuer.ShouldInvoke(null, Promise.State.Resolved, out var invokeTypes));
                    Debug.Assert((invokeTypes & InvokeTypes.Resolved) == 0);

                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeCatchFromCompletedReference<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                            _this, callback, continuer, argTransformer, resultTransformer);
                    }

                    var promise = ContinueWaitPromise<TResult, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }
            } // class CallbackHelperResult<TResult>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelper<TArg, TResult>
            {
                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Then<TDelegate>(PromiseWrapper<TArg> _this, TDelegate onResolve)
                    where TDelegate : IFunc<TArg, TResult>
                    => ContinueWith<TArg, TResult, TDelegate, ThenResolveContinuer, TTransformer<TArg>, TTransformer<TResult>>(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegate>(Promise<TArg> _this, TDelegate onResolve)
                    where TDelegate : IFunc<TArg, Promise<TResult>>
                    => ContinueWithWait<TArg, Promise<TResult>, TDelegate, ThenResolveContinuer, TTransformer<TArg>, TTransformer<TResult>>(_this, onResolve);

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(PromiseWrapper<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, TResult>
                        where TDelegateReject : IFunc<TReject, TResult>
                    => ContinueWith<Promise<TArg>.ResultContainer, TResult, ThenFilteredDelegate<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>, ThenResolveRejectFilteredContinuer<TReject>, TTransformer<TArg>, TTransformer<TResult>>(
                        _this, new ThenFilteredDelegate<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>(onResolve, onReject));

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(PromiseWrapper<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, Promise<TResult>>
                        where TDelegateReject : IFunc<TReject, Promise<TResult>>
                    => ContinueWithWait<Promise<TArg>.ResultContainer, Promise<TResult>, ThenFilteredDelegate<TArg, Promise<TResult>, TReject, TDelegateResolve, TDelegateReject>, ThenResolveRejectFilteredContinuer<TReject>, TTransformer<TArg>, TTransformer<TResult>>(
                        _this, new ThenFilteredDelegate<TArg, Promise<TResult>, TReject, TDelegateResolve, TDelegateReject>(onResolve, onReject));
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
                    => ContinueWith<Promise<TArg>.ResultContainer, TResult, TDelegate, ContinueWithContinuer, TTransformer<TArg>, TTransformer<TResult>>(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
                    => ContinueWithWait<Promise<TArg>.ResultContainer, Promise<TResult>, TDelegate, ContinueWithContinuer, TTransformer<TArg>, TTransformer<TResult>>(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
                    => ContinueWith<Promise<TArg>.ResultContainer, TResult, TDelegate, ContinueWithContinuer, TTransformer<TArg>, TTransformer<TResult>>(_this, onContinue, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
                    => ContinueWithWait<Promise<TArg>.ResultContainer, Promise<TResult>, TDelegate, ContinueWithContinuer, TTransformer<TArg>, TTransformer<TResult>>(_this, onContinue, cancelationToken);


                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(PromiseWrapper<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TArg, TResult>
                    where TDelegateReject : IFunc<VoidResult, TResult>
                    => ContinueWith<Promise<TArg>.ResultContainer, TResult, ThenDelegate<TArg, TResult, TDelegateResolve, TDelegateReject>, ThenResolveRejectContinuer, TTransformer<TArg>, TTransformer<TResult>>(
                        _this, new ThenDelegate<TArg, TResult, TDelegateResolve, TDelegateReject>(onResolve, onReject));

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(PromiseWrapper<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TArg, Promise<TResult>>
                    where TDelegateReject : IFunc<VoidResult, Promise<TResult>>
                    => ContinueWithWait<Promise<TArg>.ResultContainer, Promise<TResult>, ThenDelegate<TArg, Promise<TResult>, TDelegateResolve, TDelegateReject>, ThenResolveRejectContinuer, TTransformer<TArg>, TTransformer<TResult>>(
                        _this, new ThenDelegate<TArg, Promise<TResult>, TDelegateResolve, TDelegateReject>(onResolve, onReject));

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static PromiseWrapper<TResult> InvokeFromCompletedReference<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                    in PromiseWrapper<TArg> _this, in TDelegate callback, TContinuer continuer, TArgTransformer argTransformer, TResultTransformer resultTransformer)
                    where TDelegate : IFunc<TDelegateArg, TDelegateResult>
                    where TContinuer : struct, IContinuer
                    where TArgTransformer : struct, ITransformer<Promise<TArg>.ResultContainer, TDelegateArg>
                    where TResultTransformer : struct, ITransformer<TDelegateResult, PromiseWrapper<TResult>>
                {
                    var state = _this._ref.State;
                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    var arg = _this._ref.GetResult<TArg>();
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    if (continuer.ShouldInvoke(rejectContainer, state, out var invokeTypes))
                    {
                        var delArg = argTransformer.Transform(new Promise<TArg>.ResultContainer(arg, rejectContainer, state));
                        TDelegateResult result;
                        try
                        {
                            result = callback.Invoke(delArg);
                        }
                        catch (RethrowException e)
                        {
                            // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                            return state == Promise.State.Rejected && (invokeTypes & InvokeTypes.Rejected) != 0 && (invokeTypes & InvokeTypes.Canceled) == 0
                                ? Promise<TResult>.Rejected(rejectContainer)
                                : Promise<TResult>.Rejected(e);
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                        return resultTransformer.Transform(result).Duplicate();
                    }

                    Debug.Assert(state == Promise.State.Canceled || state == Promise.State.Rejected);
                    return state == Promise.State.Canceled
                        ? Promise<TResult>.Canceled()
                        : Promise<TResult>.Rejected(rejectContainer);
                }

                // Unity IL2CPP doesn't generate necessary code when simply using `default(TType).Method`, so we have to pass in instances of TContinuer, TArgTransformer, TResultTransformer.
                // They are not stored as fields, so they do not consume any memory, and it is later safe to use `default(TType).Method` in the ContinuePromise class.
                internal static PromiseWrapper<TResult> ContinueWith<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
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
                        var delArg = argTransformer.Transform(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved));
                        TDelegateResult result;
                        try
                        {
                            result = callback.Invoke(delArg);
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                        return resultTransformer.Transform(result).Duplicate();
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                            _this, callback, continuer, argTransformer, resultTransformer);
                    }

                    var promise = ContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> ContinueWith<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
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
                        var delArg = argTransformer.Transform(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved));
                        TDelegateResult result;
                        try
                        {
                            result = callback.Invoke(delArg);
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                        return resultTransformer.Transform(result).Duplicate();
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                            _this, callback, continuer, argTransformer, resultTransformer);
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

                internal static PromiseWrapper<TResult> ContinueWithWait<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
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
                        var delArg = argTransformer.Transform(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved));
                        TDelegateResult result;
                        try
                        {
                            result = callback.Invoke(delArg);
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                        return resultTransformer.Transform(result).Duplicate();
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                            _this, callback, continuer, argTransformer, resultTransformer);
                    }

                    var promise = ContinueWaitPromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> ContinueWithWait<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
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
                        var delArg = argTransformer.Transform(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved));
                        TDelegateResult result;
                        try
                        {
                            result = callback.Invoke(delArg);
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                        return resultTransformer.Transform(result).Duplicate();
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference<TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>(
                            _this, callback, continuer, argTransformer, resultTransformer);
                    }

                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueWaitPromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>.GetOrCreate(callback);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueWaitPromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>.GetOrCreate(callback);
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
                internal static Promise Then<TDelegate>(Promise _this, TDelegate onResolve)
                    where TDelegate : IFunc<VoidResult, VoidResult>
                    => CallbackHelper<VoidResult, VoidResult>.Then(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegate>(Promise _this, TDelegate onResolve)
                    where TDelegate : IFunc<VoidResult, Promise>
                    => CallbackHelper<VoidResult, VoidResult>.ContinueWithWait<VoidResult, Promise, TDelegate, ThenResolveContinuer, VoidTransformer, VoidTransformer>(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<VoidResult, VoidResult>
                    where TDelegateReject : IFunc<VoidResult, VoidResult>
                    => CallbackHelper<VoidResult, VoidResult>.Then(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<VoidResult, Promise>
                    where TDelegateReject : IFunc<VoidResult, Promise>
                    => CallbackHelper<VoidResult, VoidResult>.ContinueWithWait
                    <Promise<VoidResult>.ResultContainer, Promise, ThenDelegate<VoidResult, Promise, TDelegateResolve, TDelegateReject>, ThenResolveRejectContinuer, TTransformer<VoidResult>, VoidTransformer>(
                        _this, new ThenDelegate<VoidResult, Promise, TDelegateResolve, TDelegateReject>(onResolve, onReject));

                [MethodImpl(InlineOption)]
                internal static Promise Catch<TDelegateReject>(Promise _this, TDelegateReject onReject)
                    where TDelegateReject : IFunc<VoidResult, VoidResult>
                    => CallbackHelperResult<VoidResult>.Catch<VoidResult, VoidResult, TDelegateReject, CatchContinuer, VoidTransformer, VoidTransformer>(_this, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise CatchWait<TDelegateReject>(Promise _this, TDelegateReject onReject)
                    where TDelegateReject : IFunc<VoidResult, Promise>
                    => CallbackHelperResult<VoidResult>.CatchWait<VoidResult, Promise, TDelegateReject, CatchContinuer, VoidTransformer, VoidTransformer>(_this, onReject);

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    [MethodImpl(InlineOption)]
                    internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<VoidResult, VoidResult>
                        where TDelegateReject : IFunc<TReject, VoidResult>
                        => CallbackHelper<VoidResult, VoidResult>.Filter<TReject>.Then(_this, onResolve, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<VoidResult, Promise>
                        where TDelegateReject : IFunc<TReject, Promise>
                        => CallbackHelper<VoidResult, VoidResult>.ContinueWithWait
                        <Promise<VoidResult>.ResultContainer, Promise, ThenFilteredDelegate<VoidResult, Promise, TReject, TDelegateResolve, TDelegateReject>, ThenResolveRejectFilteredContinuer<TReject>, TTransformer<VoidResult>, VoidTransformer>(
                            _this, new ThenFilteredDelegate<VoidResult, Promise, TReject, TDelegateResolve, TDelegateReject>(onResolve, onReject));

                    [MethodImpl(InlineOption)]
                    internal static Promise Catch<TDelegate>(Promise _this, TDelegate onReject)
                        where TDelegate : IFunc<TReject, VoidResult>
                        => CallbackHelperResult<VoidResult>.Catch<TReject, VoidResult, TDelegate, CatchFilteredContinuer<TReject>, CatchTransformer<VoidResult, TReject>, VoidTransformer>(_this, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise CatchWait<TDelegate>(Promise _this, TDelegate onReject)
                        where TDelegate : IFunc<TReject, Promise>
                        => CallbackHelperResult<VoidResult>.CatchWait<TReject, Promise, TDelegate, CatchFilteredContinuer<TReject>, CatchTransformer<VoidResult, TReject>, VoidTransformer>(_this, onReject);
                }

                [MethodImpl(InlineOption)]
                internal static Promise CatchCancelation<TDelegateReject>(Promise _this, TDelegateReject onReject)
                    where TDelegateReject : IFunc<VoidResult, VoidResult>
                    => CallbackHelperResult<VoidResult>.Catch<VoidResult, VoidResult, TDelegateReject, CatchCancelationContinuer, VoidTransformer, VoidTransformer>(_this, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise CatchCancelationWait<TDelegateReject>(Promise _this, TDelegateReject onReject)
                    where TDelegateReject : IFunc<VoidResult, Promise>
                    => CallbackHelperResult<VoidResult>.CatchWait<VoidResult, Promise, TDelegateReject, CatchCancelationContinuer, VoidTransformer, VoidTransformer>(_this, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise.ResultContainer, VoidResult>
                    => CallbackHelper<VoidResult, VoidResult>.ContinueWith<Promise.ResultContainer, VoidResult, TDelegate, ContinueWithContinuer, VoidTransformer, VoidTransformer>(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise>
                    => CallbackHelper<VoidResult, VoidResult>.ContinueWithWait<Promise.ResultContainer, Promise, TDelegate, ContinueWithContinuer, VoidTransformer, VoidTransformer>(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, VoidResult>
                    => CallbackHelper<VoidResult, VoidResult>.ContinueWith<Promise.ResultContainer, VoidResult, TDelegate, ContinueWithContinuer, VoidTransformer, VoidTransformer>(_this, onContinue, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise>
                    => CallbackHelper<VoidResult, VoidResult>.ContinueWithWait<Promise.ResultContainer, Promise, TDelegate, ContinueWithContinuer, VoidTransformer, VoidTransformer>(_this, onContinue, cancelationToken);
            } // class CallbackHelperVoid
        } // PromiseRefBase
    } // Internal
} // namespace Proto.Promises