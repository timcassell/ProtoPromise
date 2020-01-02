#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System;

namespace Proto.Promises
{
    public abstract partial class Promise
    {
        /// <summary>
        /// Add a progress listener. <paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// Returns this.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
        public Promise ProgressCapture<TCaptureProgress>(TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress)
        {
            SubscribeProgress(progressCaptureValue, onProgress, 1);
            return this;
        }

        /// <summary>
        /// Add a cancel callback.
        /// <para/>If this instance is canceled with any or no reason, <paramref name="onCanceled"/> will be invoked with <paramref name="cancelCaptureValue"/>.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public void CatchCancelationCapture<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Action<TCaptureCancel> onCanceled)
        {
            ValidateCancel(1);
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                AddWaiter(Internal.CancelDelegateAnyCapture<TCaptureCancel>.GetOrCreate(cancelCaptureValue, onCanceled, 1));
                ReleaseWithoutDisposeCheck(); // No need to keep this retained.
            }
        }

        /// <summary>
        /// Add a cancel callback. Returns an <see cref="IPotentialCancelation"/> object.
        /// <para/>If this is canceled with any reason that is convertible to <typeparamref name="TCancel"/>, <paramref name="onCanceled"/> will be invoked with <paramref name="cancelCaptureValue"/> and that reason.
        /// <para/>If this is canceled with any other reason or no reason, the returned <see cref="IPotentialCancelation"/> will be canceled with the same reason.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public IPotentialCancelation CatchCancelationCapture<TCaptureCancel, TCancel>(TCaptureCancel cancelCaptureValue, Action<TCaptureCancel, TCancel> onCanceled)
        {
            ValidateCancel(1);
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                var cancelation = Internal.CancelDelegateCapture<TCaptureCancel, TCancel>.GetOrCreate(cancelCaptureValue, onCanceled, this, 1);
                AddWaiter(cancelation);
                return cancelation;
            }
            return this;
        }

        /// <summary>
        /// Add a finally callback. It will be invoked with <paramref name="finallyCaptureValue"/> when this resolves, rejects, or cancels. Returns this.
        /// </summary>
        public Promise FinallyCapture<TCaptureFinally>(TCaptureFinally finallyCaptureValue, Action<TCaptureFinally> onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            AddWaiter(Internal.FinallyDelegateCapture<TCaptureFinally>.GetOrCreate(finallyCaptureValue, onFinally, this, 1));
            ReleaseWithoutDisposeCheck(); // No need to keep this retained.
            return this;
        }

        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureVoidResolve<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureVoidResolve<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureVoidResolvePromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureVoidResolvePromise<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise CatchCapture<TCaptureReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled or rejected with any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise CatchCapture<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled or rejected with any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise CatchCapture<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise CatchCapture<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled or rejected with any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise CatchCapture<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled or rejected with any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise CatchCapture<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid0.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid0.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid0.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid0.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidVoid<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidResult<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise0.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise0.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise0.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise0.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromise<TCaptureResolve>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Simple Captures
        /// <summary>
        /// Capture a value. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with <paramref name="resolveCaptureValue"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TResult>(TResult resolveCaptureValue)
        {
            ValidateOperation(this, 1);

            var promise = Internal.PromiseCapture<TResult>.GetOrCreate(resolveCaptureValue, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with <paramref name="resolveCaptureValue"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TResult>(TResult resolveCaptureValue, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCapturePreserve0.GetOrCreate();
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with <paramref name="resolveCaptureValue"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TResult, TReject>(TResult resolveCaptureValue, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCapturePreserve0.GetOrCreate();
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with <paramref name="resolveCaptureValue"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TResult, TReject>(TResult resolveCaptureValue, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCapturePreserve0.GetOrCreate();
            var rejectDelegate = Internal.DelegateVoidResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        // TODO: The rest of simple captures.
        #endregion

        #region Complete Callbacks
        /// <summary>
        /// Add a resolve-or-reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved or rejected with any reason, <paramref name="onResolvedOrRejected"/> will be invoked with <paramref name="completeCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// <para/>Note: Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// </summary>
        public Promise CompleteCapture<TCapture>(TCapture completeCaptureValue, Action<TCapture> onResolvedOrRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected", 1);

            var del = Internal.DelegateCaptureVoidVoid<TCapture>.GetOrCreate(completeCaptureValue, onResolvedOrRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(del, del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve-or-reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved or rejected with any reason, <paramref name="onResolvedOrRejected"/> will be invoked with <paramref name="completeCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// <para/>Note: Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// </summary>
        public Promise<TResult> CompleteCapture<TCapture, TResult>(TCapture completeCaptureValue, Func<TCapture, TResult> onResolvedOrRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected", 1);

            var del = Internal.DelegateCaptureVoidResult<TCapture, TResult>.GetOrCreate(completeCaptureValue, onResolvedOrRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(del, del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve-or-reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved or rejected with any reason, <paramref name="onResolvedOrRejected"/> will be invoked with <paramref name="completeCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// <para/>Note: Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// </summary>
        public Promise CompleteCapture<TCapture>(TCapture completeCaptureValue, Func<TCapture, Promise> onResolvedOrRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected", 1);

            var del = Internal.DelegateCaptureVoidPromise<TCapture>.GetOrCreate(completeCaptureValue, onResolvedOrRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(del, del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve-or-reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved or rejected with any reason, <paramref name="onResolvedOrRejected"/> will be invoked with <paramref name="completeCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// <para/>Note: Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// </summary>
        public Promise<TResult> CompleteCapture<TCapture, TResult>(TCapture completeCaptureValue, Func<TCapture, Promise<TResult>> onResolvedOrRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected", 1);

            var del = Internal.DelegateCaptureVoidPromiseT<TCapture, TResult>.GetOrCreate(completeCaptureValue, onResolvedOrRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(del, del, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion
    }

    /// <summary>
    /// A <see cref="Promise{T}"/> represents the eventual result of an asynchronous operation.
    /// The primary way of interacting with a <see cref="Promise{T}"/> is through its then method,
    /// which registers callbacks to be invoked with its resolve value when the <see cref="Promise{T}"/> is resolved,
    /// or the reason why the <see cref="Promise{T}"/> cannot be resolved.
    /// </summary>
    public abstract partial class Promise<T> : Promise
    {
        /// <summary>
        /// Add a progress listener. <paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// Returns this.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
        public new Promise<T> ProgressCapture<TCaptureProgress>(TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress)
        {
            SubscribeProgress(progressCaptureValue, onProgress, 1);
            return this;
        }

        /// <summary>
        /// Add a finally callback. It will be invoked with <paramref name="finallyCaptureValue"/> when this is resolved, rejected, or canceled. Returns this.
        /// </summary>
        public new Promise<T> FinallyCapture<TCaptureFinally>(TCaptureFinally finallyCaptureValue, Action<TCaptureFinally> onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            AddWaiter(Internal.FinallyDelegateCapture<TCaptureFinally>.GetOrCreate(finallyCaptureValue, onFinally, this, 1));
            ReleaseWithoutDisposeCheck(); // No need to keep this retained.
            return this;
        }

        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureArgResolve<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureArgResolve<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureArgResolvePromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseCaptureArgResolvePromise<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> CatchCapture<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, T>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If this is canceled or rejected with any other reason or no reason, the new <see cref="Promise{T}"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> CatchCapture<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, T>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If this is canceled or rejected with any other reason or no reason, the new <see cref="Promise{T}"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> CatchCapture<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TReject, T>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> CatchCapture<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, T>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If this is canceled or rejected with any other reason or no reason, the new <see cref="Promise{T}"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> CatchCapture<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, T>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If this is canceled or rejected with any other reason or no reason, the new <see cref="Promise{T}"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> CatchCapture<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TReject, T>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid0.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgVoid<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidVoid<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgResult<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidResult<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise0.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenCapture<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromise<TCaptureResolve, T>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromise<TCaptureReject, TReject>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenCapture<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>.GetOrCreate(resolveCaptureValue, onResolved);
            var rejectDelegate = Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TReject, TResult>.GetOrCreate(rejectCaptureValue, onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Simple Captures
        /// <summary>
        /// Capture a value. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is rejected with any reason, the new <see cref="Promise{T}"/> will be resolved with <paramref name="rejectCaptureValue"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> CatchCapture(T rejectCaptureValue)
        {
            ValidateOperation(this, 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCapturePreserve0.GetOrCreate();
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            promise._value = rejectCaptureValue;
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, the new <see cref="Promise{T}"/> will be resolved with <paramref name="rejectCaptureValue"/>.
        /// <para/>If this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> CatchCapture<TReject>(T rejectCaptureValue)
        {
            ValidateOperation(this, 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateCapturePreserve<TReject>.GetOrCreate();
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            promise._value = rejectCaptureValue;
            HookupNewPromise(promise);
            return promise;
        }
        #endregion
    }
}