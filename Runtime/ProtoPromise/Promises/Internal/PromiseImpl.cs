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

#pragma warning disable IDE0017 // Simplify object initialization

using System;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static partial class PromiseImpl
            {
                internal static void Progress(Promise _this, Action<float> onProgress, CancelationToken cancelationToken)
                {
#if !PROMISE_PROGRESS
                    ThrowProgressException(2);
#else
                    ValidateOperation(_this, 2);
                    ValidateArgument(onProgress, "onProgress", 2);

                    SubscribeProgress(_this._ref, onProgress, cancelationToken);
#endif
                }

                internal static void CatchCancelation(Promise _this, Promise.CanceledAction onCanceled, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onCanceled, "onCanceled", 2);

                    var _ref = _this._ref;
                    if (_ref == null) return;

                    var state = _ref._state;
                    if (state == Promise.State.Pending | state == Promise.State.Canceled)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            if (cancelationToken.IsCancelationRequested)
                            {
                                // Don't hook up callback if token is already canceled.
                                return;
                            }
                            var cancelDelegate = CancelDelegate<CancelDelegatePromiseCancel>.GetOrCreate();
                            cancelDelegate.canceler = new CancelDelegatePromiseCancel(onCanceled, _ref);
                            cancelDelegate.canceler.cancelationRegistration = cancelationToken.RegisterInternal(cancelDelegate);
                            _ref.AddWaiter(cancelDelegate);
                        }
                        else
                        {
                            var cancelDelegate = CancelDelegate<CancelDelegatePromise>.GetOrCreate();
                            cancelDelegate.canceler = new CancelDelegatePromise(onCanceled);
                            _ref.AddWaiter(cancelDelegate);
                        }
                    }
                }

                internal static void Finally(Promise _this, Action onFinally)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onFinally, "onFinally", 2);

                    var del = FinallyDelegate.GetOrCreate(onFinally);
                    if (_this._ref != null)
                    {
                        _this._ref.AddWaiter(del);
                    }
                    else
                    {
                        AddToHandleQueueBack(del);
                    }
                }

                #region Resolve Callbacks
                internal static Promise Then(Promise _this, Action onResolved, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolve(DelegateWrapper.CreateCancelable(onResolved), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolve(DelegateWrapper.Create(onResolved));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TResult>(Promise _this, Func<TResult> onResolved, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolve(DelegateWrapper.CreateCancelable(onResolved), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolve(DelegateWrapper.Create(onResolved));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise Then(Promise _this, Func<Promise> onResolved, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveWait(DelegateWrapper.CreateCancelable(onResolved), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveWait(DelegateWrapper.Create(onResolved));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TResult>(Promise _this, Func<Promise<TResult>> onResolved, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveWait(DelegateWrapper.CreateCancelable(onResolved), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveWait(DelegateWrapper.Create(onResolved));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
                #endregion

                #region Reject Callbacks
                internal static Promise Catch(Promise _this, Action onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreatePassthroughCancelable(),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreatePassthrough(),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Catch<TReject>(Promise _this, Action<TReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreatePassthroughCancelable(),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreatePassthrough(),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Catch(Promise _this, Func<Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreatePassthroughCancelable(),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreatePassthrough(),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Catch<TReject>(Promise _this, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreatePassthroughCancelable(),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreatePassthrough(),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }
                #endregion

                #region Resolve or Reject Callbacks
                internal static Promise Then(Promise _this, Action onResolved, Action onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TReject>(Promise _this, Action onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TResult>(Promise _this, Func<TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TResult, TReject>(Promise _this, Func<TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise Then(Promise _this, Func<Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TReject>(Promise _this, Func<Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TResult>(Promise _this, Func<Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TResult, TReject>(Promise _this, Func<Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise Then(Promise _this, Action onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TReject>(Promise _this, Action onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TResult>(Promise _this, Func<TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TResult, TReject>(Promise _this, Func<TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise Then(Promise _this, Func<Promise> onResolved, Action onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TReject>(Promise _this, Func<Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TResult>(Promise _this, Func<Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TResult, TReject>(Promise _this, Func<Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
                #endregion

                #region Continue Callbacks
                internal static Promise ContinueWith(Promise _this, Promise.ContinueAction onContinue, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onContinue, "onContinue", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueVoidVoidCancel(onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueVoidVoid(onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> ContinueWith<TResult>(Promise _this, Promise.ContinueFunc<TResult> onContinue, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onContinue, "onContinue", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueVoidResultCancel<TResult>(onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueVoidResult<TResult>(onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise ContinueWith(Promise _this, Promise.ContinueFunc<Promise> onContinue, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onContinue, "onContinue", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueVoidPromiseCancel(onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueVoidPromise(onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> ContinueWith<TResult>(Promise _this, Promise.ContinueFunc<Promise<TResult>> onContinue, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onContinue, "onContinue", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueVoidPromiseTCancel<TResult>(onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueVoidPromiseT<TResult>(onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
                #endregion

                // Capture values below.

                internal static void Progress<TCaptureProgress>(Promise _this, ref TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress, CancelationToken cancelationToken)
                {
#if !PROMISE_PROGRESS
                    ThrowProgressException(1);
#else
                    ValidateOperation(_this, 2);
                    ValidateArgument(onProgress, "onProgress", 2);

                    SubscribeProgress(_this._ref, progressCaptureValue, onProgress, cancelationToken);
#endif
                }

                internal static void CatchCancelation<TCaptureCancel>(Promise _this, ref TCaptureCancel cancelCaptureValue, Promise.CanceledAction<TCaptureCancel> onCanceled, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onCanceled, "onCanceled", 2);

                    var _ref = _this._ref;
                    if (_ref == null) return;

                    var state = _ref._state;
                    if (state == Promise.State.Pending | state == Promise.State.Canceled)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            if (cancelationToken.IsCancelationRequested)
                            {
                                // Don't hook up callback if token is already canceled.
                                return;
                            }
                            var cancelDelegate = CancelDelegate<CancelDelegatePromiseCancel<TCaptureCancel>>.GetOrCreate();
                            cancelDelegate.canceler = new CancelDelegatePromiseCancel<TCaptureCancel>(ref cancelCaptureValue, onCanceled, _ref);
                            cancelDelegate.canceler.cancelationRegistration = cancelationToken.RegisterInternal(cancelDelegate);
                            _ref.AddWaiter(cancelDelegate);
                        }
                        else
                        {
                            var cancelDelegate = CancelDelegate<CancelDelegatePromise<TCaptureCancel>>.GetOrCreate();
                            cancelDelegate.canceler = new CancelDelegatePromise<TCaptureCancel>(ref cancelCaptureValue, onCanceled);
                            _ref.AddWaiter(cancelDelegate);
                        }
                    }
                }

                internal static void Finally<TCaptureFinally>(Promise _this, ref TCaptureFinally finallyCaptureValue, Action<TCaptureFinally> onFinally)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onFinally, "onFinally", 2);

                    var del = FinallyDelegateCapture<TCaptureFinally>.GetOrCreate(ref finallyCaptureValue, onFinally);
                    if (_this._ref != null)
                    {
                        _this._ref.AddWaiter(del);
                    }
                    else
                    {
                        AddToHandleQueueBack(del);
                    }
                }

                #region Resolve Callbacks
                internal static Promise Then<TCaptureResolve>(Promise _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolve(DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolve(DelegateWrapper.Create(ref resolveCaptureValue, onResolved));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolve(DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolve(DelegateWrapper.Create(ref resolveCaptureValue, onResolved));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);

                    if (_this._ref != null) { _this._ref.MarkAwaited(_this._id); }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveWait(DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveWait(DelegateWrapper.Create(ref resolveCaptureValue, onResolved));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveWait(DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveWait(DelegateWrapper.Create(ref resolveCaptureValue, onResolved));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
                #endregion

                #region Reject Callbacks
                internal static Promise Catch<TCaptureReject>(Promise _this, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreatePassthroughCancelable(),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreatePassthrough(),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Catch<TCaptureReject, TReject>(Promise _this, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreatePassthroughCancelable(),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreatePassthrough(),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Catch<TCaptureReject>(Promise _this, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreatePassthroughCancelable(),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreatePassthrough(),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Catch<TCaptureReject, TReject>(Promise _this, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreatePassthroughCancelable(),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreatePassthrough(),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }
                #endregion

                #region Resolve or Reject Callbacks
                internal static Promise Then<TCaptureResolve>(Promise _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureReject>(Promise _this, Action onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TCaptureReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureReject, TReject>(Promise _this, Action onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TCaptureReject, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureReject, TResult>(Promise _this, Func<TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureReject, TResult, TReject>(Promise _this, Func<TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveReject(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureReject>(Promise _this, Func<Promise> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TCaptureReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureReject, TReject>(Promise _this, Func<Promise> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TCaptureReject, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureReject, TResult>(Promise _this, Func<Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureReject, TResult, TReject>(Promise _this, Func<Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve>(Promise _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureReject>(Promise _this, Action onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TCaptureReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureReject, TReject>(Promise _this, Action onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TCaptureReject, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureReject, TResult>(Promise _this, Func<TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureReject, TResult, TReject>(Promise _this, Func<TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureReject>(Promise _this, Func<Promise> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TCaptureReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureReject, TReject>(Promise _this, Func<Promise> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise Then<TCaptureResolve, TCaptureReject, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureReject, TResult>(Promise _this, Func<Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureReject, TResult, TReject>(Promise _this, Func<Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(Promise _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onResolved, "onResolved", 2);
                    ValidateArgument(onRejected, "onRejected", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.CreateCancelable(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected),
                            cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateResolveRejectWait(
                            DelegateWrapper.Create(ref resolveCaptureValue, onResolved),
                            DelegateWrapper.Create(ref rejectCaptureValue, onRejected));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
                #endregion

                #region Continue Callbacks
                internal static Promise ContinueWith<TCapture>(Promise _this, ref TCapture continueCaptureValue, Promise.ContinueAction<TCapture> onContinue, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onContinue, "onContinue", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueCaptureVoidVoidCancel<TCapture>(ref continueCaptureValue, onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueCaptureVoidVoid<TCapture>(ref continueCaptureValue, onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> ContinueWith<TCapture, TResult>(Promise _this, ref TCapture continueCaptureValue, Promise.ContinueFunc<TCapture, TResult> onContinue, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onContinue, "onContinue", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueCaptureVoidResultCancel<TCapture, TResult>(ref continueCaptureValue, onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueCaptureVoidResult<TCapture, TResult>(ref continueCaptureValue, onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise ContinueWith<TCapture>(Promise _this, ref TCapture continueCaptureValue, Promise.ContinueFunc<TCapture, Promise> onContinue, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onContinue, "onContinue", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueCaptureVoidPromiseCancel<TCapture>(ref continueCaptureValue, onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueCaptureVoidPromise<TCapture>(ref continueCaptureValue, onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> ContinueWith<TCapture, TResult>(Promise _this, ref TCapture continueCaptureValue, Promise.ContinueFunc<TCapture, Promise<TResult>> onContinue, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onContinue, "onContinue", 2);

                    if (_this._ref != null)
                    {
                        _this._ref.MarkAwaited(_this._id);
                    }
                    PromiseRef promise;
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueCaptureVoidPromiseTCancel<TCapture, TResult>(ref continueCaptureValue, onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueCaptureVoidPromiseT<TCapture, TResult>(ref continueCaptureValue, onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
                #endregion
            }
        }
    }
}