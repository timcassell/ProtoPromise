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
                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise MaybeInvokeResolve<TDelegate>(in Promise<TArg> _this, in TDelegate callback)
                    where TDelegate : IAction<TArg>
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        var arg = _this._ref._result;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return CallbackHelperVoid.Invoke(arg, callback, null);
                    }
                    return _this.Duplicate();
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise MaybeInvokeAndAdoptResolve<TDelegate>(in Promise<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<TArg, Promise>
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        var arg = _this._ref._result;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return CallbackHelperVoid.InvokeAndAdopt(arg, callback, null);
                    }
                    return _this.Duplicate();
                }

                [MethodImpl(InlineOption)]
                internal static Promise Then<TDelegate>(Promise<TArg> _this, TDelegate onResolve)
                    where TDelegate : IAction<TArg>, IFunc<TArg, VoidResult>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperVoid.Invoke(_this._result, onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeResolve(_this, onResolve);
                    }

                    var promise = ThenPromise<TArg, VoidResult, TDelegate>.GetOrCreate(onResolve);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegate>(Promise<TArg> _this, TDelegate onResolve)
                    where TDelegate : IFunc<TArg, Promise>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperVoid.InvokeAndAdopt(_this._result, onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeAndAdoptResolve(_this, onResolve);
                    }

                    var promise = ThenWaitPromise<TArg, TDelegate>.GetOrCreate(onResolve);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise MaybeInvokeThen<TDelegateResolve, TDelegateReject>(in Promise<TArg> _this, in TDelegateResolve onResolve, in TDelegateReject onReject)
                        where TDelegateResolve : IAction<TArg>
                        where TDelegateReject : IAction<TReject>
                    {
                        if (_this._ref.State == Promise.State.Resolved)
                        {
                            var arg = _this._ref._result;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return CallbackHelperVoid.Invoke(arg, onResolve, null);
                        }

                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            _this._ref.SuppressRejection = true;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return CallbackHelperVoid.Invoke(rejectArg, onReject, rejectContainer);
                        }

                        return _this.Duplicate();
                    }

                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise MaybeInvokeAndAdoptThen<TDelegateResolve, TDelegateReject>(in Promise<TArg> _this, in TDelegateResolve onResolve, in TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, Promise>
                        where TDelegateReject : IFunc<TReject, Promise>
                    {
                        if (_this._ref.State == Promise.State.Resolved)
                        {
                            var arg = _this._ref._result;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return CallbackHelperVoid.InvokeAndAdopt(arg, onResolve, null);
                        }

                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            _this._ref.SuppressRejection = true;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return CallbackHelperVoid.InvokeAndAdopt(rejectArg, onReject, rejectContainer);
                        }

                        return _this.Duplicate();
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IAction<TArg>, IFunc<TArg, VoidResult>
                        where TDelegateReject : IAction<TReject>, IFunc<TReject, VoidResult>
                    {
                        if (_this._ref == null)
                        {
                            return CallbackHelperVoid.Invoke(_this._result, onResolve, null);
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeThen(_this, onResolve, onReject);
                        }

                        var promise = ThenPromise<TArg, VoidResult, TReject, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise(promise, promise.Id);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, Promise>
                        where TDelegateReject : IFunc<TReject, Promise>
                    {
                        if (_this._ref == null)
                        {
                            return CallbackHelperVoid.InvokeAndAdopt(_this._result, onResolve, null);
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeAndAdoptThen(_this, onResolve, onReject);
                        }

                        var promise = ThenWaitPromise<TArg, TReject, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise(promise, promise.Id);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IAction<TArg>, IFunc<TArg, VoidResult>
                    where TDelegateReject : IAction, IAction<VoidResult>, IFunc<VoidResult, VoidResult>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperVoid.Invoke(_this._result, onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeThen(_this, onResolve, onReject);
                    }

                    var promise = ThenPromise<TArg, VoidResult, VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TArg, Promise>
                    where TDelegateReject : IFunc<Promise>, IFunc<VoidResult, Promise>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperVoid.InvokeAndAdopt(_this._result, onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeAndAdoptThen(_this, onResolve, onReject);
                    }

                    var promise = ThenWaitPromise<TArg, VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise InvokeContinueWith<TDelegate>(PromiseRef<TArg> promise, short promiseId, in TDelegate callback)
                    where TDelegate : IAction<Promise<TArg>.ResultContainer>, IFunc<Promise<TArg>.ResultContainer, VoidResult>
                {
                    var arg = new Promise<TArg>.ResultContainer(promise._result, promise.RejectContainer, promise.State);
                    promise.SuppressRejection = true;
                    promise.MaybeMarkAwaitedAndDispose(promiseId);
                    return CallbackHelperVoid.Invoke(arg, callback, null);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise InvokeAndAdoptContinueWith<TDelegate>(PromiseRef<TArg> promise, short promiseId, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
                {
                    var arg = new Promise<TArg>.ResultContainer(promise._result, promise.RejectContainer, promise.State);
                    promise.SuppressRejection = true;
                    promise.MaybeMarkAwaitedAndDispose(promiseId);
                    return CallbackHelperVoid.InvokeAndAdopt(arg, callback, null);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IAction<Promise<TArg>.ResultContainer>, IFunc<Promise<TArg>.ResultContainer, VoidResult>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperVoid.Invoke(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved), onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeContinueWith(_this._ref, _this._id, onContinue);
                    }

                    var promise = ContinueArgResultPromise<TArg, VoidResult, TDelegate>.GetOrCreate(onContinue);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperVoid.InvokeAndAdopt(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved), onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeAndAdoptContinueWith(_this._ref, _this._id, onContinue);
                    }

                    var promise = ContinueArgVoidWaitPromise<TArg, TDelegate>.GetOrCreate(onContinue);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IAction<Promise<TArg>.ResultContainer>, IFunc<Promise<TArg>.ResultContainer, VoidResult>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return CallbackHelperVoid.Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        return CallbackHelperVoid.Invoke(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved), onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeContinueWith(_this._ref, _this._id, onContinue);
                    }

                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueArgResultPromise<TArg, VoidResult, TDelegate>.GetOrCreate(onContinue);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueArgResultPromise<TArg, VoidResult, TDelegate>.GetOrCreate(onContinue);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return CallbackHelperVoid.Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        return CallbackHelperVoid.InvokeAndAdopt(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved), onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeAndAdoptContinueWith(_this._ref, _this._id, onContinue);
                    }

                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueArgVoidWaitPromise<TArg, TDelegate>.GetOrCreate(onContinue);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueArgVoidWaitPromise<TArg, TDelegate>.GetOrCreate(onContinue);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }
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
                internal static Promise<TResult> Invoke<TDelegate>(in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IFunc<TResult>
                {
                    try
                    {
                        return Promise.Resolved(callback.Invoke());
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        return rejectContainer != null
                            ? Promise<TResult>.Rejected(rejectContainer)
                            : Promise<TResult>.Rejected(e);
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Invoke<TArg, TDelegate>(in TArg arg, in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IFunc<TArg, TResult>
                {
                    try
                    {
                        return Promise.Resolved(callback.Invoke(arg));
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        return rejectContainer != null
                            ? Promise<TResult>.Rejected(rejectContainer)
                            : Promise<TResult>.Rejected(e);
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeAndAdopt<TDelegate>(in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IFunc<Promise<TResult>>
                {
                    try
                    {
                        return callback.Invoke().Duplicate();
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        return rejectContainer != null
                            ? Promise<TResult>.Rejected(rejectContainer)
                            : Promise<TResult>.Rejected(e);
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeAndAdopt<TArg, TDelegate>(in TArg arg, in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IFunc<TArg, Promise<TResult>>
                {
                    try
                    {
                        return callback.Invoke(arg).Duplicate();
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        return rejectContainer != null
                            ? Promise<TResult>.Rejected(rejectContainer)
                            : Promise<TResult>.Rejected(e);
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

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise<TResult> MaybeInvokeResolve<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IFunc<TResult>
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return Invoke(callback, null);
                    }

                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    return _this._ref.State == Promise.State.Canceled
                        ? Promise<TResult>.Canceled()
                        : Promise<TResult>.Rejected(rejectContainer);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise<TResult> MaybeInvokeAndAdoptResolve<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TResult>>
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return InvokeAndAdopt(callback, null);
                    }

                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    return _this._ref.State == Promise.State.Canceled
                        ? Promise<TResult>.Canceled()
                        : Promise<TResult>.Rejected(rejectContainer);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Then<TDelegate>(Promise _this, TDelegate onResolve)
                    where TDelegate : IFunc<TResult>, IFunc<VoidResult, TResult>
                {
                    if (_this._ref == null)
                    {
                        return Invoke(onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeResolve(_this, onResolve);
                    }

                    var promise = ThenPromise<VoidResult, TResult, TDelegate>.GetOrCreate(onResolve);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegate>(Promise _this, TDelegate onResolve)
                    where TDelegate : IFunc<Promise<TResult>>, IFunc<VoidResult, Promise<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return InvokeAndAdopt(onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeAndAdoptResolve(_this, onResolve);
                    }

                    var promise = ThenWaitPromise<VoidResult, TResult, TDelegate>.GetOrCreate(onResolve);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Catch<TDelegate>(Promise<TResult> _this, TDelegate onReject)
                    where TDelegate : IFunc<TResult>, IFunc<VoidResult, TResult>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeCatch(_this, onReject, Promise.State.Rejected);
                    }

                    var promise = CatchPromise<TResult, VoidResult, TDelegate>.GetOrCreate(onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> CatchWait<TDelegate>(Promise<TResult> _this, TDelegate onReject)
                    where TDelegate : IFunc<Promise<TResult>>, IFunc<VoidResult, Promise<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeAndAdoptCatch(_this, onReject, Promise.State.Rejected);
                    }

                    var promise = CatchWaitPromise<TResult, VoidResult, TDelegate>.GetOrCreate(onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise<TResult> MaybeInvokeCatch<TDelegate>(in Promise<TResult> _this, in TDelegate callback, Promise.State invokeState)
                        where TDelegate : IFunc<TReject, TResult>
                    {
                        // Support both Catch and CatchCancelation to reduce code duplication.
                        Debug.Assert(invokeState == Promise.State.Rejected || invokeState == Promise.State.Canceled);
                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State != invokeState || !GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            return _this.Duplicate();
                        }

                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return Invoke(rejectArg, callback, rejectContainer);
                    }

                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise<TResult> MaybeInvokeAndAdoptCatch<TDelegate>(in Promise<TResult> _this, in TDelegate callback, Promise.State invokeState)
                        where TDelegate : IFunc<TReject, Promise<TResult>>
                    {
                        Debug.Assert(invokeState == Promise.State.Rejected || invokeState == Promise.State.Canceled);
                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State != invokeState || !GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            return _this.Duplicate();
                        }

                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return InvokeAndAdopt(rejectArg, callback, rejectContainer);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> Catch<TDelegate>(Promise<TResult> _this, TDelegate onReject)
                        where TDelegate : IFunc<TReject, TResult>
                    {
                        if (_this._ref == null)
                        {
                            return _this;
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeCatch(_this, onReject, Promise.State.Rejected);
                        }

                        var promise = CatchPromise<TResult, TReject, TDelegate>.GetOrCreate(onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise<TResult>(promise, promise.Id);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> CatchWait<TDelegate>(Promise<TResult> _this, TDelegate onReject)
                        where TDelegate : IFunc<TReject, Promise<TResult>>
                    {
                        if (_this._ref == null)
                        {
                            return _this;
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeAndAdoptCatch(_this, onReject, Promise.State.Rejected);
                        }

                        var promise = CatchWaitPromise<TResult, TReject, TDelegate>.GetOrCreate(onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise<TResult>(promise, promise.Id);
                    }

                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise<TResult> MaybeInvokeThen<TDelegateResolve, TDelegateReject>(Promise _this, in TDelegateResolve onResolve, in TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TResult>
                        where TDelegateReject : IFunc<TReject, TResult>
                    {
                        if (_this._ref.State == Promise.State.Resolved)
                        {
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return Invoke(onResolve, null);
                        }

                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            _this._ref.SuppressRejection = true;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return Invoke(rejectArg, onReject, rejectContainer);
                        }

                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return _this._ref.State == Promise.State.Canceled
                            ? Promise<TResult>.Canceled()
                            : Promise<TResult>.Rejected(rejectContainer);
                    }

                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise<TResult> MaybeInvokeAndAdoptThen<TDelegateResolve, TDelegateReject>(Promise _this, in TDelegateResolve onResolve, in TDelegateReject onReject)
                        where TDelegateResolve : IFunc<Promise<TResult>>
                        where TDelegateReject : IFunc<TReject, Promise<TResult>>
                    {
                        if (_this._ref.State == Promise.State.Resolved)
                        {
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return InvokeAndAdopt(onResolve, null);
                        }

                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            _this._ref.SuppressRejection = true;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return InvokeAndAdopt(rejectArg, onReject, rejectContainer);
                        }

                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return _this._ref.State == Promise.State.Canceled
                            ? Promise<TResult>.Canceled()
                            : Promise<TResult>.Rejected(rejectContainer);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TResult>, IFunc<VoidResult, TResult>
                        where TDelegateReject : IFunc<TReject, TResult>
                    {
                        if (_this._ref == null)
                        {
                            return Invoke(onResolve, null);
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeThen(_this, onResolve, onReject);
                        }

                        var promise = ThenPromise<VoidResult, TResult, TReject, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise<TResult>(promise, promise.Id);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<Promise<TResult>>, IFunc<VoidResult, Promise<TResult>>
                        where TDelegateReject : IFunc<TReject, Promise<TResult>>
                    {
                        if (_this._ref == null)
                        {
                            return InvokeAndAdopt(onResolve, null);
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeAndAdoptThen(_this, onResolve, onReject);
                        }

                        var promise = ThenWaitPromise<VoidResult, TResult, TReject, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise<TResult>(promise, promise.Id);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TResult>, IFunc<VoidResult, TResult>
                    where TDelegateReject : IFunc<TResult>, IFunc<VoidResult, TResult>
                {
                    if (_this._ref == null)
                    {
                        return Invoke(onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeThen(_this, onResolve, onReject);
                    }

                    var promise = ThenPromise<VoidResult, TResult, VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<Promise<TResult>>, IFunc<VoidResult, Promise<TResult>>
                    where TDelegateReject : IFunc<Promise<TResult>>, IFunc<VoidResult, Promise<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return InvokeAndAdopt(onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeAndAdoptThen(_this, onResolve, onReject);
                    }

                    var promise = ThenWaitPromise<VoidResult, TResult, VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> CatchCancelation<TDelegate>(Promise<TResult> _this, TDelegate onCancel)
                    where TDelegate : IFunc<TResult>, IFunc<VoidResult, TResult>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeCatch(_this, onCancel, Promise.State.Canceled);
                    }

                    var promise = CatchCancelationPromise<TResult, TDelegate>.GetOrCreate(onCancel);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> CatchCancelationWait<TDelegate>(Promise<TResult> _this, TDelegate onCancel)
                    where TDelegate : IFunc<Promise<TResult>>, IFunc<VoidResult, Promise<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeAndAdoptCatch(_this, onCancel, Promise.State.Canceled);
                    }

                    var promise = CatchCancelationWaitPromise<TResult, TDelegate>.GetOrCreate(onCancel);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise<TResult> InvokeContinueWith<TDelegate>(PromiseRefBase promise, short promiseId, in TDelegate callback)
                    where TDelegate : IFunc<Promise.ResultContainer, TResult>
                {
                    var arg = new Promise.ResultContainer(promise.RejectContainer, promise.State);
                    promise.SuppressRejection = true;
                    promise.MaybeMarkAwaitedAndDispose(promiseId);
                    return Invoke(arg, callback, null);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise<TResult> InvokeAndAdoptContinueWith<TDelegate>(PromiseRefBase promise, short promiseId, in TDelegate callback)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
                {
                    var arg = new Promise.ResultContainer(promise.RejectContainer, promise.State);
                    promise.SuppressRejection = true;
                    promise.MaybeMarkAwaitedAndDispose(promiseId);
                    return InvokeAndAdopt(arg, callback, null);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise.ResultContainer, TResult>
                {
                    if (_this._ref == null)
                    {
                        return Invoke(Promise.ResultContainer.Resolved, onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeContinueWith(_this._ref, _this._id, onContinue);
                    }

                    var promise = ContinueVoidResultPromise<TResult, TDelegate>.GetOrCreate(onContinue);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return InvokeAndAdopt(Promise.ResultContainer.Resolved, onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeAndAdoptContinueWith(_this._ref, _this._id, onContinue);
                    }

                    var promise = ContinueVoidResultWaitPromise<TResult, TDelegate>.GetOrCreate(onContinue);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, TResult>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        return Invoke(Promise.ResultContainer.Resolved, onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeContinueWith(_this._ref, _this._id, onContinue);
                    }

                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueVoidResultPromise<TResult, TDelegate>.GetOrCreate(onContinue);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueVoidResultPromise<TResult, TDelegate>.GetOrCreate(onContinue);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        return InvokeAndAdopt(Promise.ResultContainer.Resolved, onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeAndAdoptContinueWith(_this._ref, _this._id, onContinue);
                    }

                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueVoidResultWaitPromise<TResult, TDelegate>.GetOrCreate(onContinue);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueVoidResultWaitPromise<TResult, TDelegate>.GetOrCreate(onContinue);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
            } // class CallbackHelperResult<TResult>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal static class CallbackHelper<TArg, TResult>
            {
                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise<TResult> MaybeInvokeResolve<TDelegate>(in Promise<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<TArg, TResult>
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        var arg = _this._ref._result;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return CallbackHelperResult<TResult>.Invoke(arg, callback, null);
                    }

                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    return _this._ref.State == Promise.State.Canceled
                        ? Promise<TResult>.Canceled()
                        : Promise<TResult>.Rejected(rejectContainer);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise<TResult> MaybeInvokeAndAdoptResolve<TDelegate>(in Promise<TArg> _this, in TDelegate callback)
                    where TDelegate : IFunc<TArg, Promise<TResult>>
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        var arg = _this._ref._result;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return CallbackHelperResult<TResult>.InvokeAndAdopt(arg, callback, null);
                    }

                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    return _this._ref.State == Promise.State.Canceled
                        ? Promise<TResult>.Canceled()
                        : Promise<TResult>.Rejected(rejectContainer);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Then<TDelegate>(Promise<TArg> _this, TDelegate onResolve)
                    where TDelegate : IFunc<TArg, TResult>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperResult<TResult>.Invoke(_this._result, onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeResolve(_this, onResolve);
                    }

                    var promise = ThenPromise<TArg, TResult, TDelegate>.GetOrCreate(onResolve);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegate>(Promise<TArg> _this, TDelegate onResolve)
                    where TDelegate : IFunc<TArg, Promise<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperResult<TResult>.InvokeAndAdopt(_this._result, onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeAndAdoptResolve(_this, onResolve);
                    }

                    var promise = ThenWaitPromise<TArg, TResult, TDelegate>.GetOrCreate(onResolve);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise<TResult> MaybeInvokeThen<TDelegateResolve, TDelegateReject>(in Promise<TArg> _this, in TDelegateResolve onResolve, in TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, TResult>
                        where TDelegateReject : IFunc<TReject, TResult>
                    {
                        if (_this._ref.State == Promise.State.Resolved)
                        {
                            var arg = _this._ref._result;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return CallbackHelperResult<TResult>.Invoke(arg, onResolve, null);
                        }

                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            _this._ref.SuppressRejection = true;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return CallbackHelperResult<TResult>.Invoke(rejectArg, onReject, rejectContainer);
                        }

                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return _this._ref.State == Promise.State.Canceled
                            ? Promise<TResult>.Canceled()
                            : Promise<TResult>.Rejected(rejectContainer);
                    }

                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise<TResult> MaybeInvokeAndAdoptThen<TDelegateResolve, TDelegateReject>(in Promise<TArg> _this, in TDelegateResolve onResolve, in TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, Promise<TResult>>
                        where TDelegateReject : IFunc<TReject, Promise<TResult>>
                    {
                        if (_this._ref.State == Promise.State.Resolved)
                        {
                            var arg = _this._ref._result;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return CallbackHelperResult<TResult>.InvokeAndAdopt(arg, onResolve, null);
                        }

                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            _this._ref.SuppressRejection = true;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return CallbackHelperResult<TResult>.InvokeAndAdopt(rejectArg, onReject, rejectContainer);
                        }

                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return _this._ref.State == Promise.State.Canceled
                            ? Promise<TResult>.Canceled()
                            : Promise<TResult>.Rejected(rejectContainer);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, TResult>
                        where TDelegateReject : IFunc<TReject, TResult>
                    {
                        if (_this._ref == null)
                        {
                            return CallbackHelperResult<TResult>.Invoke(_this._result, onResolve, null);
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeThen(_this, onResolve, onReject);
                        }

                        var promise = ThenPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise<TResult>(promise, promise.Id);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<TArg, Promise<TResult>>
                        where TDelegateReject : IFunc<TReject, Promise<TResult>>
                    {
                        if (_this._ref == null)
                        {
                            return CallbackHelperResult<TResult>.InvokeAndAdopt(_this._result, onResolve, null);
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeAndAdoptThen(_this, onResolve, onReject);
                        }

                        var promise = ThenWaitPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise<TResult>(promise, promise.Id);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> Then<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TArg, TResult>
                    where TDelegateReject : IFunc<TResult>, IFunc<VoidResult, TResult>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperResult<TResult>.Invoke(_this._result, onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeThen(_this, onResolve, onReject);
                    }

                    var promise = ThenPromise<TArg, TResult, VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ThenWait<TDelegateResolve, TDelegateReject>(Promise<TArg> _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<TArg, Promise<TResult>>
                    where TDelegateReject : IFunc<Promise<TResult>>, IFunc<VoidResult, Promise<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperResult<TResult>.InvokeAndAdopt(_this._result, onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeAndAdoptThen(_this, onResolve, onReject);
                    }

                    var promise = ThenWaitPromise<TArg, TResult, VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise<TResult> InvokeContinueWith<TDelegate>(PromiseRef<TArg> promise, short promiseId, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
                {
                    var arg = new Promise<TArg>.ResultContainer(promise._result, promise.RejectContainer, promise.State);
                    promise.SuppressRejection = true;
                    promise.MaybeMarkAwaitedAndDispose(promiseId);
                    return CallbackHelperResult<TResult>.Invoke(arg, callback, null);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise<TResult> InvokeAndAdoptContinueWith<TDelegate>(PromiseRef<TArg> promise, short promiseId, in TDelegate callback)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
                {
                    var arg = new Promise<TArg>.ResultContainer(promise._result, promise.RejectContainer, promise.State);
                    promise.SuppressRejection = true;
                    promise.MaybeMarkAwaitedAndDispose(promiseId);
                    return CallbackHelperResult<TResult>.InvokeAndAdopt(arg, callback, null);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperResult<TResult>.Invoke(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved), onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeContinueWith(_this._ref, _this._id, onContinue);
                    }

                    var promise = ContinueArgResultPromise<TArg, TResult, TDelegate>.GetOrCreate(onContinue);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
                {
                    if (_this._ref == null)
                    {
                        return CallbackHelperResult<TResult>.InvokeAndAdopt(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved), onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeAndAdoptContinueWith(_this._ref, _this._id, onContinue);
                    }

                    var promise = ContinueArgResultWaitPromise<TArg, TResult, TDelegate>.GetOrCreate(onContinue);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWith<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        return CallbackHelperResult<TResult>.Invoke(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved), onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeContinueWith(_this._ref, _this._id, onContinue);
                    }

                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueArgResultPromise<TArg, TResult, TDelegate>.GetOrCreate(onContinue);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueArgResultPromise<TArg, TResult, TDelegate>.GetOrCreate(onContinue);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> ContinueWithWait<TDelegate>(Promise<TArg> _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return CallbackHelperResult<TResult>.Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        return CallbackHelperResult<TResult>.InvokeAndAdopt(new Promise<TArg>.ResultContainer(_this._result, null, Promise.State.Resolved), onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeAndAdoptContinueWith(_this._ref, _this._id, onContinue);
                    }

                    PromiseRef<TResult> promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueArgResultWaitPromise<TArg, TResult, TDelegate>.GetOrCreate(onContinue);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueArgResultWaitPromise<TArg, TResult, TDelegate>.GetOrCreate(onContinue);
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
                internal static Promise Invoke<TDelegate>(in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IAction
                {
                    try
                    {
                        callback.Invoke();
                        return Promise.Resolved();
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        return rejectContainer != null
                            ? Promise.Rejected(rejectContainer)
                            : Promise.Rejected(e);
                    }
                    catch (Exception e)
                    {
                        return Promise.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise Invoke<TArg, TDelegate>(in TArg arg, in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IAction<TArg>
                {
                    try
                    {
                        callback.Invoke(arg);
                        return Promise.Resolved();
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        return rejectContainer != null
                            ? Promise.Rejected(rejectContainer)
                            : Promise.Rejected(e);
                    }
                    catch (Exception e)
                    {
                        return Promise.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise InvokeAndAdopt<TDelegate>(in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IFunc<Promise>
                {
                    try
                    {
                        return callback.Invoke().Duplicate();
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        return rejectContainer != null
                            ? Promise.Rejected(rejectContainer)
                            : Promise.Rejected(e);
                    }
                    catch (Exception e)
                    {
                        return Promise.FromException(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise InvokeAndAdopt<TArg, TDelegate>(in TArg arg, in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IFunc<TArg, Promise>
                {
                    try
                    {
                        return callback.Invoke(arg).Duplicate();
                    }
                    catch (RethrowException e)
                    {
                        // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                        return rejectContainer != null
                            ? Promise.Rejected(rejectContainer)
                            : Promise.Rejected(e);
                    }
                    catch (Exception e)
                    {
                        return Promise.FromException(e);
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

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise MaybeInvokeResolve<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IAction
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return Invoke(callback, null);
                    }

                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    return _this._ref.State == Promise.State.Canceled
                        ? Promise.Canceled()
                        : Promise.Rejected(rejectContainer);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise MaybeInvokeAndAdoptResolve<TDelegate>(Promise _this, in TDelegate callback)
                    where TDelegate : IFunc<Promise>
                {
                    if (_this._ref.State == Promise.State.Resolved)
                    {
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return InvokeAndAdopt(callback, null);
                    }

                    var rejectContainer = _this._ref.RejectContainer;
                    _this._ref.SuppressRejection = true;
                    _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                    return _this._ref.State == Promise.State.Canceled
                        ? Promise.Canceled()
                        : Promise.Rejected(rejectContainer);
                }

                [MethodImpl(InlineOption)]
                internal static Promise Then<TDelegate>(Promise _this, TDelegate onResolve)
                    where TDelegate : IAction, IFunc<VoidResult, VoidResult>
                {
                    if (_this._ref == null)
                    {
                        return Invoke(onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeResolve(_this, onResolve);
                    }

                    var promise = ThenPromise<VoidResult, VoidResult, TDelegate>.GetOrCreate(onResolve);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegate>(Promise _this, TDelegate onResolve)
                    where TDelegate : IFunc<Promise>, IFunc<VoidResult, Promise>
                {
                    if (_this._ref == null)
                    {
                        return InvokeAndAdopt(onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return MaybeInvokeAndAdoptResolve(_this, onResolve);
                    }

                    var promise = ThenWaitPromise<VoidResult, TDelegate>.GetOrCreate(onResolve);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise Catch<TDelegate>(Promise _this, TDelegate onReject)
                    where TDelegate : IAction, IAction<VoidResult>, IFunc<VoidResult, VoidResult>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeCatch(_this, onReject, Promise.State.Rejected);
                    }

                    var promise = CatchPromise<VoidResult, VoidResult, TDelegate>.GetOrCreate(onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise CatchWait<TDelegate>(Promise _this, TDelegate onReject)
                    where TDelegate : IFunc<Promise>, IFunc<VoidResult, Promise>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeAndAdoptCatch(_this, onReject, Promise.State.Rejected);
                    }

                    var promise = CatchWaitPromise<VoidResult, TDelegate>.GetOrCreate(onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                internal static class Filter<TReject>
                {
                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise MaybeInvokeCatch<TDelegate>(Promise _this, in TDelegate callback, Promise.State invokeState)
                        where TDelegate : IAction<TReject>
                    {
                        // Support both Catch and CatchCancelation to reduce code duplication.
                        Debug.Assert(invokeState == Promise.State.Rejected || invokeState == Promise.State.Canceled);
                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State != invokeState || !GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            return _this.Duplicate();
                        }

                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return Invoke(rejectArg, callback, rejectContainer);
                    }

                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise MaybeInvokeAndAdoptCatch<TDelegate>(Promise _this, in TDelegate callback, Promise.State invokeState)
                        where TDelegate : IFunc<TReject, Promise>
                    {
                        Debug.Assert(invokeState == Promise.State.Rejected || invokeState == Promise.State.Canceled);
                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State != invokeState || !GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            return _this.Duplicate();
                        }

                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return InvokeAndAdopt(rejectArg, callback, rejectContainer);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise Catch<TDelegate>(Promise _this, TDelegate onReject)
                        where TDelegate : IAction<TReject>, IFunc<TReject, VoidResult>
                    {
                        if (_this._ref == null)
                        {
                            return _this;
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeCatch(_this, onReject, Promise.State.Rejected);
                        }

                        var promise = CatchPromise<VoidResult, TReject, TDelegate>.GetOrCreate(onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise(promise, promise.Id);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise CatchWait<TDelegate>(Promise _this, TDelegate onReject)
                        where TDelegate : IFunc<TReject, Promise>
                    {
                        if (_this._ref == null)
                        {
                            return _this;
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeAndAdoptCatch(_this, onReject, Promise.State.Rejected);
                        }

                        var promise = CatchWaitPromise<TReject, TDelegate>.GetOrCreate(onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise(promise, promise.Id);
                    }

                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise MaybeInvokeThen<TDelegateResolve, TDelegateReject>(Promise _this, in TDelegateResolve onResolve, in TDelegateReject onReject)
                        where TDelegateResolve : IAction
                        where TDelegateReject : IAction<TReject>
                    {
                        if (_this._ref.State == Promise.State.Resolved)
                        {
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return Invoke(onResolve, null);
                        }

                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            _this._ref.SuppressRejection = true;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return Invoke(rejectArg, onReject, rejectContainer);
                        }

                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return _this._ref.State == Promise.State.Canceled
                            ? Promise.Canceled()
                            : Promise.Rejected(rejectContainer);
                    }

                    // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static Promise MaybeInvokeAndAdoptThen<TDelegateResolve, TDelegateReject>(Promise _this, in TDelegateResolve onResolve, in TDelegateReject onReject)
                        where TDelegateResolve : IFunc<Promise>
                        where TDelegateReject : IFunc<TReject, Promise>
                    {
                        if (_this._ref.State == Promise.State.Resolved)
                        {
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return InvokeAndAdopt(onResolve, null);
                        }

                        var rejectContainer = _this._ref.RejectContainer;
                        if (_this._ref.State == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                        {
                            _this._ref.SuppressRejection = true;
                            _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                            return InvokeAndAdopt(rejectArg, onReject, rejectContainer);
                        }

                        _this._ref.SuppressRejection = true;
                        _this._ref.MaybeMarkAwaitedAndDispose(_this._id);
                        return _this._ref.State == Promise.State.Canceled
                            ? Promise.Canceled()
                            : Promise.Rejected(rejectContainer);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IAction, IFunc<VoidResult, VoidResult>
                        where TDelegateReject : IAction<TReject>, IFunc<TReject, VoidResult>
                    {
                        if (_this._ref == null)
                        {
                            return Invoke(onResolve, null);
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeThen(_this, onResolve, onReject);
                        }

                        var promise = ThenPromise<VoidResult, VoidResult, TReject, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise(promise, promise.Id);
                    }

                    [MethodImpl(InlineOption)]
                    internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                        where TDelegateResolve : IFunc<Promise>, IFunc<VoidResult, Promise>
                        where TDelegateReject : IFunc<TReject, Promise>
                    {
                        if (_this._ref == null)
                        {
                            return InvokeAndAdopt(onResolve, null);
                        }

                        if (_this._ref.State != Promise.State.Pending)
                        {
                            return MaybeInvokeAndAdoptThen(_this, onResolve, onReject);
                        }

                        var promise = ThenWaitPromise<VoidResult, TReject, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                        _this._ref.HookupNewPromise(_this._id, promise);
                        return new Promise(promise, promise.Id);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise Then<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IAction, IFunc<VoidResult, VoidResult>
                    where TDelegateReject : IAction, IAction<VoidResult>, IFunc<VoidResult, VoidResult>
                {
                    if (_this._ref == null)
                    {
                        return Invoke(onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeThen(_this, onResolve, onReject);
                    }

                    var promise = ThenPromise<VoidResult, VoidResult, VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ThenWait<TDelegateResolve, TDelegateReject>(Promise _this, TDelegateResolve onResolve, TDelegateReject onReject)
                    where TDelegateResolve : IFunc<Promise>, IFunc<VoidResult, Promise>
                    where TDelegateReject : IFunc<Promise>, IFunc<VoidResult, Promise>
                {
                    if (_this._ref == null)
                    {
                        return InvokeAndAdopt(onResolve, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeAndAdoptThen(_this, onResolve, onReject);
                    }

                    var promise = ThenWaitPromise<VoidResult, VoidResult, TDelegateResolve, TDelegateReject>.GetOrCreate(onResolve, onReject);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise CatchCancelation<TDelegate>(Promise _this, TDelegate onCancel)
                    where TDelegate : IAction, IAction<VoidResult>, IFunc<VoidResult, VoidResult>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeCatch(_this, onCancel, Promise.State.Canceled);
                    }

                    var promise = CatchCancelationPromise<VoidResult, TDelegate>.GetOrCreate(onCancel);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise CatchCancelationWait<TDelegate>(Promise _this, TDelegate onCancel)
                    where TDelegate : IFunc<Promise>, IFunc<VoidResult, Promise>
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return Filter<VoidResult>.MaybeInvokeAndAdoptCatch(_this, onCancel, Promise.State.Canceled);
                    }

                    var promise = CatchCancelationWaitPromise<TDelegate>.GetOrCreate(onCancel);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise InvokeContinueWith<TDelegate>(PromiseRefBase promise, short promiseId, in TDelegate callback)
                    where TDelegate : IAction<Promise.ResultContainer>
                {
                    var arg = new Promise.ResultContainer(promise.RejectContainer, promise.State);
                    promise.SuppressRejection = true;
                    promise.MaybeMarkAwaitedAndDispose(promiseId);
                    return Invoke(arg, callback, null);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static Promise InvokeAndAdoptContinueWith<TDelegate>(PromiseRefBase promise, short promiseId, in TDelegate callback)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise>
                {
                    var arg = new Promise.ResultContainer(promise.RejectContainer, promise.State);
                    promise.SuppressRejection = true;
                    promise.MaybeMarkAwaitedAndDispose(promiseId);
                    return InvokeAndAdopt(arg, callback, null);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IAction<Promise.ResultContainer>, IFunc<Promise.ResultContainer, VoidResult>
                {
                    if (_this._ref == null)
                    {
                        return Invoke(Promise.ResultContainer.Resolved, onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeContinueWith(_this._ref, _this._id, onContinue);
                    }

                    var promise = ContinueVoidResultPromise<VoidResult, TDelegate>.GetOrCreate(onContinue);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise>
                {
                    if (_this._ref == null)
                    {
                        return InvokeAndAdopt(Promise.ResultContainer.Resolved, onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeAndAdoptContinueWith(_this._ref, _this._id, onContinue);
                    }

                    var promise = ContinueVoidVoidWaitPromise<TDelegate>.GetOrCreate(onContinue);
                    _this._ref.HookupNewPromise(_this._id, promise);
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWith<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IAction<Promise.ResultContainer>, IFunc<Promise.ResultContainer, VoidResult>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        return Invoke(Promise.ResultContainer.Resolved, onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeContinueWith(_this._ref, _this._id, onContinue);
                    }

                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueVoidResultPromise<VoidResult, TDelegate>.GetOrCreate(onContinue);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueVoidResultPromise<VoidResult, TDelegate>.GetOrCreate(onContinue);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise ContinueWithWait<TDelegate>(Promise _this, in TDelegate onContinue, CancelationToken cancelationToken)
                    where TDelegate : IFunc<Promise.ResultContainer, Promise>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return Canceled(_this._ref, _this._id);
                    }

                    if (_this._ref == null)
                    {
                        return InvokeAndAdopt(Promise.ResultContainer.Resolved, onContinue, null);
                    }

                    if (_this._ref.State != Promise.State.Pending)
                    {
                        return InvokeAndAdoptContinueWith(_this._ref, _this._id, onContinue);
                    }

                    PromiseRefBase promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        var p = CancelableContinueVoidVoidWaitPromise<TDelegate>.GetOrCreate(onContinue);
                        promise = _this._ref.HookupCancelablePromise(p, _this._id, cancelationToken, ref p._cancelationHelper);
                    }
                    else
                    {
                        promise = ContinueVoidVoidWaitPromise<TDelegate>.GetOrCreate(onContinue);
                        _this._ref.HookupNewPromise(_this._id, promise);
                    }
                    return new Promise(promise, promise.Id);
                }
            } // class CallbackHelperVoid
        } // PromiseRefBase
    } // Internal
} // namespace Proto.Promises