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
                    where TDelegate : IAction<TArg>, IFunc<TArg, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<TArg, VoidResult>.Then(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegate>(Promise<TArg> _this, TDelegate onResolve)
                    where TDelegate : IFunc<TArg, Promise>, IFunc<TArg, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<TArg, VoidResult>.ThenWait(_this, onResolve);

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    [MethodImpl(InlineOption)]
                    internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IAction<TArg>, IFunc<TArg, PromiseWrapper<VoidResult>>
                        where TDelegateReject : IAction<TReject>, IFunc<TReject, PromiseWrapper<VoidResult>>
                        => CallbackHelperImpl<TArg, VoidResult>.Then<TReject, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, Promise>, IFunc<TArg, PromiseWrapper<VoidResult>>
                        where TDelegateReject : IFunc<TReject, Promise>, IFunc<TReject, PromiseWrapper<VoidResult>>
                        => CallbackHelperImpl<TArg, VoidResult>.ThenWait<TReject, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);
                }

                [MethodImpl(InlineOption)]
                internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IAction<TArg>, IFunc<TArg, PromiseWrapper<VoidResult>>
                    where TDelegateReject : IAction, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<TArg, VoidResult>.Then<VoidResult, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TArg, Promise>, IFunc<TArg, PromiseWrapper<VoidResult>>
                    where TDelegateReject : IFunc<Promise>, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<TArg, VoidResult>.ThenWait<VoidResult, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IAction<Promise<TArg>.ResultContainer>, IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<TArg, VoidResult>.ContinueWith(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>, IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<TArg, VoidResult>.ContinueWithWait(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IAction<Promise<TArg>.ResultContainer>, IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<TArg, VoidResult>.ContinueWith(_this, onContinue, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>, IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<TArg, VoidResult>.ContinueWithWait(_this, onContinue, cancelationToken);
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
                    where TDelegate : IFunc<TResult>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<VoidResult, TResult>.Then(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegate>(Promise _this, TDelegate onResolve)
                    where TDelegate : IFunc<Promise<TResult>>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<VoidResult, TResult>.ThenWait(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Catch<TDelegate>(PromiseWrapper<TResult> _this, TDelegate onReject)
                    where TDelegate : IFunc<TResult>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TResult>.Catch<VoidResult, TDelegate>(_this, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> CatchWait<TDelegate>(PromiseWrapper<TResult> _this, TDelegate onReject)
                    where TDelegate : IFunc<Promise<TResult>>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TResult>.CatchWait<VoidResult, TDelegate>(_this, onReject);

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> Catch<TDelegate>(Promise<TResult> _this, TDelegate onReject)
                        where TDelegate : IFunc<TReject, TResult>, IFunc<TReject, PromiseWrapper<TResult>>
                        => CallbackHelperImpl<TResult>.Catch<TReject, TDelegate>(_this, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> CatchWait<TDelegate>(Promise<TResult> _this, TDelegate onReject)
                        where TDelegate : IFunc<TReject, Promise<TResult>>, IFunc<TReject, PromiseWrapper<TResult>>
                        => CallbackHelperImpl<TResult>.CatchWait<TReject, TDelegate>(_this, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TResult>, IFunc<VoidResult, PromiseWrapper<TResult>>
                        where TDelegateReject : IFunc<TReject, TResult>, IFunc<TReject, PromiseWrapper<TResult>>
                        => CallbackHelperImpl<VoidResult, TResult>.Then<TReject, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<Promise<TResult>>, IFunc<VoidResult, PromiseWrapper<TResult>>
                        where TDelegateReject : IFunc<TReject, Promise<TResult>>, IFunc<TReject, PromiseWrapper<TResult>>
                        => CallbackHelperImpl<VoidResult, TResult>.ThenWait<TReject, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TResult>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    where TDelegateReject : IFunc<TResult>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<VoidResult, TResult>.Then<VoidResult, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<Promise<TResult>>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    where TDelegateReject : IFunc<Promise<TResult>>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<VoidResult, TResult>.ThenWait<VoidResult, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> CatchCancelation<TDelegate>(Promise<TResult> _this, TDelegate onCancel)
                    where TDelegate : IFunc<TResult>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TResult>.CatchCancelation(_this, onCancel);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> CatchCancelationWait<TDelegate>(Promise<TResult> _this, TDelegate onCancel)
                    where TDelegate : IFunc<Promise<TResult>>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TResult>.CatchCancelationWait(_this, onCancel);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise.ResultContainer, TResult>, IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TResult>.ContinueWith(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>, IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TResult>.ContinueWithWait(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, TResult>, IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TResult>.ContinueWith(_this, onContinue, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>, IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TResult>.ContinueWithWait(_this, onContinue, cancelationToken);
            } // class CallbackHelperResult<TResult>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelper<TArg, TResult>
            {
                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Then<TDelegate>(PromiseWrapper<TArg> _this, TDelegate onResolve)
                    where TDelegate : IFunc<TArg, TResult>, IFunc<TArg, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TArg, TResult>.Then(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegate>(PromiseWrapper<TArg> _this, TDelegate onResolve)
                    where TDelegate : IFunc<TArg, Promise<TResult>>, IFunc<TArg, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TArg, TResult>.ThenWait(_this, onResolve);

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(PromiseWrapper<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, TResult>, IFunc<TArg, PromiseWrapper<TResult>>
                        where TDelegateReject : IFunc<TReject, TResult>, IFunc<TReject, PromiseWrapper<TResult>>
                        => CallbackHelperImpl<TArg, TResult>.Then<TReject, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(PromiseWrapper<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, Promise<TResult>>, IFunc<TArg, PromiseWrapper<TResult>>
                        where TDelegateReject : IFunc<TReject, Promise<TResult>>, IFunc<TReject, PromiseWrapper<TResult>>
                        => CallbackHelperImpl<TArg, TResult>.ThenWait<TReject, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(PromiseWrapper<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TArg, TResult>, IFunc<TArg, PromiseWrapper<TResult>>
                    where TDelegateReject : IFunc<TResult>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TArg, TResult>.Then<VoidResult, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(PromiseWrapper<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TArg, Promise<TResult>>, IFunc<TArg, PromiseWrapper<TResult>>
                    where TDelegateReject : IFunc<Promise<TResult>>, IFunc<VoidResult, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TArg, TResult>.ThenWait<VoidResult, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>, IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TArg, TResult>.ContinueWith(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>, IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TArg, TResult>.ContinueWithWait(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>, IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TArg, TResult>.ContinueWith(_this, onContinue, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>, IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
                    => CallbackHelperImpl<TArg, TResult>.ContinueWithWait(_this, onContinue, cancelationToken);
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
                    where TDelegate : IAction, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult, VoidResult>.Then(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegate>(Promise _this, TDelegate onResolve)
                    where TDelegate : IFunc<Promise>, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult, VoidResult>.ThenWait(_this, onResolve);

                [MethodImpl(InlineOption)]
                internal static Promise Catch<TDelegate>(Promise _this, TDelegate onReject)
                    where TDelegate : IAction, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult>.Catch<VoidResult, TDelegate>(_this, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise CatchWait<TDelegate>(Promise _this, TDelegate onReject)
                    where TDelegate : IFunc<Promise>, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult>.CatchWait<VoidResult, TDelegate>(_this, onReject);

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    [MethodImpl(InlineOption)]
                    internal static Promise Catch<TDelegate>(Promise _this, TDelegate onReject)
                        where TDelegate : IAction<TReject>, IFunc<TReject, PromiseWrapper<VoidResult>>
                        => CallbackHelperImpl<VoidResult>.Catch<TReject, TDelegate>(_this, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise CatchWait<TDelegate>(Promise _this, TDelegate onReject)
                        where TDelegate : IFunc<TReject, Promise>, IFunc<TReject, PromiseWrapper<VoidResult>>
                        => CallbackHelperImpl<VoidResult>.CatchWait<TReject, TDelegate>(_this, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IAction, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                        where TDelegateReject : IAction<TReject>, IFunc<TReject, PromiseWrapper<VoidResult>>
                        => CallbackHelperImpl<VoidResult, VoidResult>.Then<TReject, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                    [MethodImpl(InlineOption)]
                    internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<Promise>, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                        where TDelegateReject : IFunc<TReject, Promise>, IFunc<TReject, PromiseWrapper<VoidResult>>
                        => CallbackHelperImpl<VoidResult, VoidResult>.ThenWait<TReject, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);
                }

                [MethodImpl(InlineOption)]
                internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IAction, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    where TDelegateReject : IAction, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult, VoidResult>.Then<VoidResult, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<Promise>, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    where TDelegateReject : IFunc<Promise>, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult, VoidResult>.ThenWait<VoidResult, TDelegateResolve, TDelegateReject>(_this, onResolve, onReject);

                [MethodImpl(InlineOption)]
                internal static Promise CatchCancelation<TDelegateReject>(Promise _this, TDelegateReject onCancel)
                    where TDelegateReject : IAction, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult>.CatchCancelation(_this, onCancel);

                [MethodImpl(InlineOption)]
                internal static Promise CatchCancelationWait<TDelegateReject>(Promise _this, TDelegateReject onCancel)
                    where TDelegateReject : IFunc<Promise>, IFunc<VoidResult, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult>.CatchCancelationWait(_this, onCancel);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IAction<Promise.ResultContainer>, IFunc<Promise.ResultContainer, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult>.ContinueWith(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise>, IFunc<Promise.ResultContainer, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult>.ContinueWithWait(_this, onContinue);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IAction<Promise.ResultContainer>, IFunc<Promise.ResultContainer, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult>.ContinueWith(_this, onContinue, cancelationToken);

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise>, IFunc<Promise.ResultContainer, PromiseWrapper<VoidResult>>
                    => CallbackHelperImpl<VoidResult>.ContinueWithWait(_this, onContinue, cancelationToken);
            } // class CallbackHelperVoid

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelperImpl<TArg, TResult>
            {
                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static PromiseWrapper<TResult> InvokeFromCompletedReference<TDelegate>(in PromiseWrapper<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
                {
                    var state = _this._ref.State;
                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    var arg = _this._ref.GetResult<TArg>();
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    try
                    {
                        return callback.Invoke(new Promise<TArg>.ResultContainer(arg, rejectContainer, state)).Duplicate();
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.FromException(e);
                    }
                }

                internal static PromiseWrapper<TResult> ContinueWith<TDelegate>(PromiseWrapper<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        try
                        {
                            return callback.Invoke(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved)).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference(_this, callback);
                    }

                    var promise = ContinuePromise<TArg, TResult, TDelegate>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> ContinueWith<TDelegate>(PromiseWrapper<TArg> _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        try
                        {
                            return callback.Invoke(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved)).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference(_this, callback);
                    }

                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinuePromise<TArg, TResult, TDelegate>.GetOrCreate(callback);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinuePromise<TArg, TResult, TDelegate>.GetOrCreate(callback);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> ContinueWithWait<TDelegate>(PromiseWrapper<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        try
                        {
                            return callback.Invoke(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved)).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference(_this, callback);
                    }

                    var promise = ContinueWaitPromise<TArg, TResult, TDelegate>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> ContinueWithWait<TDelegate>(PromiseWrapper<TArg> _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, PromiseWrapper<TResult>>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        try
                        {
                            return callback.Invoke(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved)).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference(_this, callback);
                    }

                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueWaitPromise<TArg, TResult, TDelegate>.GetOrCreate(callback);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueWaitPromise<TArg, TResult, TDelegate>.GetOrCreate(callback);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }


                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static PromiseWrapper<TResult> MaybeInvokeThenFromCompletedReference<TDelegate>(in PromiseWrapper<TArg> _this, in TDelegate resolveCallback)
                    where TDelegate : IFunc<TArg, PromiseWrapper<TResult>>
                {
                    var state = _this._ref.State;
                    if (state == Promise.State.Resolved)
                    {
                        var arg = _this._ref.GetResult<TArg>();
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        try
                        {
                            return resolveCallback.Invoke(arg).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    Debug.Assert(state == Promise.State.Canceled || state == Promise.State.Rejected);
                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    return state == Promise.State.Canceled
                        ? Promise<TResult>.Canceled()
                        : Promise<TResult>.Rejected(rejectContainer);
                }

                internal static PromiseWrapper<TResult> Then<TDelegate>(PromiseWrapper<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<TArg, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        try
                        {
                            return callback.Invoke(_this._result).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeThenFromCompletedReference(_this, callback);
                    }

                    var promise = ThenPromise<TArg, TResult, TDelegate>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> ThenWait<TDelegate>(PromiseWrapper<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<TArg, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        try
                        {
                            return callback.Invoke(_this._result).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeThenFromCompletedReference(_this, callback);
                    }

                    var promise = ThenWaitPromise<TArg, TResult, TDelegate>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }


                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static PromiseWrapper<TResult> MaybeInvokeThenFromCompletedReference<TReject, TDelegateResolve, TDelegateReject>(
                    in PromiseWrapper<TArg> _this, in TDelegateResolve resolveCallback, in TDelegateReject rejectCallback)
                    where TDelegateResolve : IFunc<TArg, PromiseWrapper<TResult>>
                    where TDelegateReject : IFunc<TReject, PromiseWrapper<TResult>>
                {
                    var state = _this._ref.State;
                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    try
                    {
                        if (state == Promise.State.Resolved)
                        {
                            var arg = _this._ref.GetResult<TArg>();
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return resolveCallback.Invoke(arg).Duplicate();
                        }
                        if (state == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return rejectCallback.Invoke(rejectArg).Duplicate();
                        }
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        return state == Promise.State.Rejected
                            ? Promise<TResult>.Rejected(rejectContainer)
                            : Promise<TResult>.Rejected(e);
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.FromException(e);
                    }

                    Debug.Assert(state == Promise.State.Canceled || state == Promise.State.Rejected);
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    return state == Promise.State.Canceled
                        ? Promise<TResult>.Canceled()
                        : Promise<TResult>.Rejected(rejectContainer);
                }

                internal static PromiseWrapper<TResult> Then<TReject, TDelegateResolve, TDelegateReject>(PromiseWrapper<TArg> _this, in TDelegateResolve resolveCallback, in TDelegateReject rejectCallback)
                    where TDelegateResolve : IFunc<TArg, PromiseWrapper<TResult>>
                    where TDelegateReject : IFunc<TReject, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        try
                        {
                            return resolveCallback.Invoke(_this._result).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeThenFromCompletedReference<TReject, TDelegateResolve, TDelegateReject>(_this, resolveCallback, rejectCallback);
                    }

                    var promise = ThenPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>.GetOrCreate(resolveCallback, rejectCallback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> ThenWait<TReject, TDelegateResolve, TDelegateReject>(PromiseWrapper<TArg> _this, in TDelegateResolve resolveCallback, in TDelegateReject rejectCallback)
                    where TDelegateResolve : IFunc<TArg, PromiseWrapper<TResult>>
                    where TDelegateReject : IFunc<TReject, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        try
                        {
                            return resolveCallback.Invoke(_this._result).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeThenFromCompletedReference<TReject, TDelegateResolve, TDelegateReject>(_this, resolveCallback, rejectCallback);
                    }

                    var promise = ThenWaitPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>.GetOrCreate(resolveCallback, rejectCallback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }
            } // class CallbackHelperImpl<TArg, TResult>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelperImpl<TResult>
            {
                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static PromiseWrapper<TResult> InvokeFromCompletedReference<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
                {
                    var state = _this._ref.State;
                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    try
                    {
                        return callback.Invoke(new Promise.ResultContainer(rejectContainer, state)).Duplicate();
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.FromException(e);
                    }
                }

                internal static PromiseWrapper<TResult> ContinueWith<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        try
                        {
                            return callback.Invoke(Promise.ResultContainer.Resolved).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference(_this, callback);
                    }

                    var promise = ContinuePromise<TResult, TDelegate>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> ContinueWith<TDelegate>(Promise _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        try
                        {
                            return callback.Invoke(Promise.ResultContainer.Resolved).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference(_this, callback);
                    }

                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinuePromise<TResult, TDelegate>.GetOrCreate(callback);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinuePromise<TResult, TDelegate>.GetOrCreate(callback);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> ContinueWithWait<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        try
                        {
                            return callback.Invoke(Promise.ResultContainer.Resolved).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference(_this, callback);
                    }

                    var promise = ContinueWaitPromise<TResult, TDelegate>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> ContinueWithWait<TDelegate>(Promise _this, in TDelegate callback, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, PromiseWrapper<TResult>>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        try
                        {
                            return callback.Invoke(Promise.ResultContainer.Resolved).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeFromCompletedReference(_this, callback);
                    }

                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueWaitPromise<TResult, TDelegate>.GetOrCreate(callback);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueWaitPromise<TResult, TDelegate>.GetOrCreate(callback);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }


                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static PromiseWrapper<TResult> MaybeInvokeCatchFromCompletedReference<TReject, TDelegate>(in PromiseWrapper<TResult> _this, in TDelegate callback)
                    where TDelegate : IFunc<TReject, PromiseWrapper<TResult>>
                {
                    var state = _this._ref.State;
                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    if (state == Promise.State.Resolved)
                    {
                        var result = _this._ref.GetResult<TResult>();
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return Promise.Resolved(result);
                    }

                    if (state == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                    {
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        try
                        {
                            return callback.Invoke(rejectArg).Duplicate();
                        }
                        catch (RethrowException)
                        {
                            return Promise<TResult>.Rejected(rejectContainer);
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    return state == Promise.State.Canceled
                        ? Promise<TResult>.Canceled()
                        : Promise<TResult>.Rejected(rejectContainer);
                }

                internal static PromiseWrapper<TResult> Catch<TReject, TDelegate>(PromiseWrapper<TResult> _this, in TDelegate callback)
                    where TDelegate : IFunc<TReject, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeCatchFromCompletedReference<TReject, TDelegate>(_this, callback);
                    }

                    var promise = CatchPromise<TResult, TReject, TDelegate>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> CatchWait<TReject, TDelegate>(PromiseWrapper<TResult> _this, in TDelegate callback)
                    where TDelegate : IFunc<TReject, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeCatchFromCompletedReference<TReject, TDelegate>(_this, callback);
                    }

                    var promise = CatchWaitPromise<TResult, TReject, TDelegate>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static PromiseWrapper<TResult> MaybeInvokeCatchCancelationFromCompletedReference<TDelegate>(in PromiseWrapper<TResult> _this, in TDelegate callback)
                    where TDelegate : IFunc<VoidResult, PromiseWrapper<TResult>>
                {
                    var state = _this._ref.State;
                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    if (state == Promise.State.Resolved)
                    {
                        var result = _this._ref.GetResult<TResult>();
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return Promise.Resolved(result);
                    }

                    if (state == Promise.State.Canceled)
                    {
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        try
                        {
                            return callback.Invoke(default).Duplicate();
                        }
                        catch (Exception e)
                        {
                            return Promise<TResult>.FromException(e);
                        }
                    }

                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    return Promise<TResult>.Rejected(rejectContainer);
                }

                internal static PromiseWrapper<TResult> CatchCancelation<TDelegate>(PromiseWrapper<TResult> _this, in TDelegate callback)
                    where TDelegate : IFunc<VoidResult, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeCatchCancelationFromCompletedReference(_this, callback);
                    }

                    var promise = CatchCancelationPromise<TResult, TDelegate>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static PromiseWrapper<TResult> CatchCancelationWait<TDelegate>(PromiseWrapper<TResult> _this, in TDelegate callback)
                    where TDelegate : IFunc<VoidResult, PromiseWrapper<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeCatchCancelationFromCompletedReference(_this, callback);
                    }

                    var promise = CatchCancelationWaitPromise<TResult, TDelegate>.GetOrCreate(callback);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }
            } // class CallbackHelperImpl<TResult>
        } // PromiseRefBase
    } // Internal
} // namespace Proto.Promises