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
            internal static partial class PromiseImplVoid
            {
                internal static void Progress(Promise _this, Action<float> onProgress, CancelationToken cancelationToken)
                {
#if !PROMISE_PROGRESS
                    ThrowProgressException(2);
#else
                    ValidateOperation(_this, 2);
                    ValidateArgument(onProgress, "onProgress", 2);

                    SubscribeProgress(_this, onProgress, cancelationToken);
#endif
                }

                internal static void CatchCancelation(Promise _this, Promise.CanceledAction onCanceled, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onCanceled, "onCanceled", 2);

                    var _ref = _this._ref;
                    if (_ref == null) return;

                    _ref.IncrementId(_this._id, 0); // Increment 0 just as an extra thread safety validation in RELEASE mode.
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
                            cancelationToken.TryRegisterInternal(cancelDelegate, out cancelDelegate.canceler.cancelationRegistration);
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
                        _this._ref.IncrementId(_this._id, 0); // Increment 0 just as an extra thread safety validation in RELEASE mode.
                        _this._ref.AddWaiter(del);
                    }
                    else
                    {
                        AddToHandleQueueBack(del);
                    }
                }

                // Capture values below.

                internal static void Progress<TCaptureProgress>(Promise _this, ref TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress, CancelationToken cancelationToken)
                {
#if !PROMISE_PROGRESS
                    ThrowProgressException(1);
#else
                    ValidateOperation(_this, 2);
                    ValidateArgument(onProgress, "onProgress", 2);

                    SubscribeProgress(_this, progressCaptureValue, onProgress, cancelationToken);
#endif
                }

                internal static void CatchCancelation<TCaptureCancel>(Promise _this, ref TCaptureCancel cancelCaptureValue, Promise.CanceledAction<TCaptureCancel> onCanceled, CancelationToken cancelationToken)
                {
                    ValidateOperation(_this, 2);
                    ValidateArgument(onCanceled, "onCanceled", 2);

                    var _ref = _this._ref;
                    if (_ref == null) return;

                    _ref.IncrementId(_this._id, 0); // Increment 0 just as an extra thread safety validation in RELEASE mode.
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
                            cancelationToken.TryRegisterInternal(cancelDelegate, out cancelDelegate.canceler.cancelationRegistration);
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
                        _this._ref.IncrementId(_this._id, 0); // Increment 0 just as an extra thread safety validation in RELEASE mode.
                        _this._ref.AddWaiter(del);
                    }
                    else
                    {
                        AddToHandleQueueBack(del);
                    }
                }
            }
        }
    }
}