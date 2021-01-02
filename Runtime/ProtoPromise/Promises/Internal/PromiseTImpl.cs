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
            internal static partial class PromiseImpl<T>
            {
                #region Resolve Callbacks
                internal static Promise Then(Promise<T> _this, Action<T> onResolved, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TResult>(Promise<T> _this, Func<T, TResult> onResolved, CancelationToken cancelationToken)
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

                internal static Promise Then(Promise<T> _this, Func<T, Promise> onResolved, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TResult>(Promise<T> _this, Func<T, Promise<TResult>> onResolved, CancelationToken cancelationToken)
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
                internal static Promise<T> Catch(Promise<T> _this, Func<T> onRejected, CancelationToken cancelationToken)
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
                    return new Promise<T>(promise, promise.Id);
                }

                internal static Promise<T> Catch<TReject>(Promise<T> _this, Func<TReject, T> onRejected, CancelationToken cancelationToken)
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
                    return new Promise<T>(promise, promise.Id);
                }

                internal static Promise<T> Catch(Promise<T> _this, Func<Promise<T>> onRejected, CancelationToken cancelationToken)
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
                    return new Promise<T>(promise, promise.Id);
                }

                internal static Promise<T> Catch<TReject>(Promise<T> _this, Func<TReject, Promise<T>> onRejected, CancelationToken cancelationToken)
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
                    return new Promise<T>(promise, promise.Id);
                }
                #endregion

                #region Resolve or Reject Callbacks
                internal static Promise Then(Promise<T> _this, Action<T> onResolved, Action onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TReject>(Promise<T> _this, Action<T> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TResult>(Promise<T> _this, Func<T, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TResult, TReject>(Promise<T> _this, Func<T, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then(Promise<T> _this, Func<T, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TReject>(Promise<T> _this, Func<T, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TResult>(Promise<T> _this, Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TResult, TReject>(Promise<T> _this, Func<T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then(Promise<T> _this, Action<T> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TReject>(Promise<T> _this, Action<T> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TResult>(Promise<T> _this, Func<T, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TResult, TReject>(Promise<T> _this, Func<T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then(Promise<T> _this, Func<T, Promise> onResolved, Action onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TReject>(Promise<T> _this, Func<T, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TResult>(Promise<T> _this, Func<T, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TResult, TReject>(Promise<T> _this, Func<T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
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
                internal static Promise ContinueWith(Promise<T> _this, Promise<T>.ContinueAction onContinue, CancelationToken cancelationToken)
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
                        promise = RefCreator.CreateContinue(new DelegateContinueArgVoidCancel<T>(onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueArgVoid<T>(onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> ContinueWith<TResult>(Promise<T> _this, Promise<T>.ContinueFunc<TResult> onContinue, CancelationToken cancelationToken)
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
                        promise = RefCreator.CreateContinue(new DelegateContinueArgResultCancel<T, TResult>(onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueArgResult<T, TResult>(onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise ContinueWith(Promise<T> _this, Promise<T>.ContinueFunc<Promise> onContinue, CancelationToken cancelationToken)
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
                        promise = RefCreator.CreateContinueWait(new DelegateContinueArgPromiseCancel<T>(onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueArgPromise<T>(onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> ContinueWith<TResult>(Promise<T> _this, Promise<T>.ContinueFunc<Promise<TResult>> onContinue, CancelationToken cancelationToken)
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
                        promise = RefCreator.CreateContinueWait(new DelegateContinueArgPromiseTCancel<T, TResult>(onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueArgPromiseT<T, TResult>(onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
                #endregion

                // Capture values below.

                #region Resolve Callbacks
                internal static Promise Then<TCaptureResolve>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, CancelationToken cancelationToken)
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
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, CancelationToken cancelationToken)
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
                internal static Promise<T> Catch<TCaptureReject>(Promise<T> _this, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, T> onRejected, CancelationToken cancelationToken)
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
                    return new Promise<T>(promise, promise.Id);
                }

                internal static Promise<T> Catch<TCaptureReject, TReject>(Promise<T> _this, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, T> onRejected, CancelationToken cancelationToken)
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
                    return new Promise<T>(promise, promise.Id);
                }

                internal static Promise<T> Catch<TCaptureReject>(Promise<T> _this, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<T>> onRejected, CancelationToken cancelationToken)
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
                    return new Promise<T>(promise, promise.Id);
                }

                internal static Promise<T> Catch<TCaptureReject, TReject>(Promise<T> _this, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<T>> onRejected, CancelationToken cancelationToken)
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
                    return new Promise<T>(promise, promise.Id);
                }
                #endregion

                #region Resolve or Reject Callbacks
                internal static Promise Then<TCaptureResolve>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureReject>(Promise<T> _this, Action<T> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TCaptureReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureReject, TReject>(Promise<T> _this, Action<T> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TCaptureReject, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureReject, TResult>(Promise<T> _this, Func<T, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TResult, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureReject, TResult, TReject>(Promise<T> _this, Func<T, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureReject>(Promise<T> _this, Func<T, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TCaptureReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureReject, TReject>(Promise<T> _this, Func<T, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TCaptureReject, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureReject, TResult>(Promise<T> _this, Func<T, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TResult, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureReject, TResult, TReject>(Promise<T> _this, Func<T, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureReject>(Promise<T> _this, Action<T> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TCaptureReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureReject, TReject>(Promise<T> _this, Action<T> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TCaptureReject, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureReject, TResult>(Promise<T> _this, Func<T, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TResult, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureReject, TResult, TReject>(Promise<T> _this, Func<T, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureReject>(Promise<T> _this, Func<T, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TCaptureReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureReject, TReject>(Promise<T> _this, Func<T, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise Then<TCaptureResolve, TCaptureReject, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, ref TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TResult>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureReject, TResult>(Promise<T> _this, Func<T, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TResult, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureReject, TResult, TReject>(Promise<T> _this, Func<T, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
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

                internal static Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(Promise<T> _this, ref TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, ref TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
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
                internal static Promise ContinueWith<TCapture>(Promise<T> _this, ref TCapture continueCaptureValue, Promise<T>.ContinueAction<TCapture> onContinue, CancelationToken cancelationToken)
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
                        promise = RefCreator.CreateContinue(new DelegateContinueCaptureArgVoidCancel<TCapture, T>(ref continueCaptureValue, onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueCaptureArgVoid<TCapture, T>(ref continueCaptureValue, onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> ContinueWith<TCapture, TResult>(Promise<T> _this, ref TCapture continueCaptureValue, Promise<T>.ContinueFunc<TCapture, TResult> onContinue, CancelationToken cancelationToken)
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
                        promise = RefCreator.CreateContinue(new DelegateContinueCaptureArgResultCancel<TCapture, T, TResult>(ref continueCaptureValue, onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinue(new DelegateContinueCaptureArgResult<TCapture, T, TResult>(ref continueCaptureValue, onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }

                internal static Promise ContinueWith<TCapture>(Promise<T> _this, ref TCapture continueCaptureValue, Promise<T>.ContinueFunc<TCapture, Promise> onContinue, CancelationToken cancelationToken)
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
                        promise = RefCreator.CreateContinueWait(new DelegateContinueCaptureArgPromiseCancel<TCapture, T>(ref continueCaptureValue, onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueCaptureArgPromise<TCapture, T>(ref continueCaptureValue, onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                internal static Promise<TResult> ContinueWith<TCapture, TResult>(Promise<T> _this, ref TCapture continueCaptureValue, Promise<T>.ContinueFunc<TCapture, Promise<TResult>> onContinue, CancelationToken cancelationToken)
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
                        promise = RefCreator.CreateContinueWait(new DelegateContinueCaptureArgPromiseTCancel<TCapture, T, TResult>(ref continueCaptureValue, onContinue), cancelationToken);
                        MaybeHookupNewPromise(_this, promise);
                    }
                    else
                    {
                        promise = RefCreator.CreateContinueWait(new DelegateContinueCaptureArgPromiseT<TCapture, T, TResult>(ref continueCaptureValue, onContinue));
                        HookupNewPromise(_this, promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
                }
                #endregion
            }
        }
    }
}