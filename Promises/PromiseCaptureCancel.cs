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
    public abstract partial class Promise
    {
        #region Resolve Callbacks
        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolve<Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolve<TResult, Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolvePromise<Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolvePromise<TResult, Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegatePassthroughCancel, Internal.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegatePassthroughCancel, Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegatePassthroughCancel, Internal.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegatePassthroughCancel, Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>, Internal.DelegateVoidVoid>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidVoid(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateVoidVoidCancel, Internal.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidVoidCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>, Internal.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>, Internal.DelegateArgVoid<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgVoid<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateVoidVoidCancel, Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidVoidCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>, Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, Internal.DelegateVoidResult<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidResult<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateVoidResultCancel<TResult>, Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, Internal.DelegateArgResult<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgResult<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateVoidResultCancel<TResult>, Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, Internal.DelegateVoidPromise>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromise(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidPromiseCancel, Internal.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, Internal.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, Internal.DelegateArgPromise<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromise<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidPromiseCancel, Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, Internal.DelegateVoidPromiseT<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromiseT<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidPromiseTCancel<TResult>, Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, Internal.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromiseT<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidPromiseTCancel<TResult>, Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>, Internal.DelegateVoidPromise>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromise(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidVoidCancel, Internal.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidVoidCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>, Internal.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>, Internal.DelegateArgPromise<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromise<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidVoidCancel, Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidVoidCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>, Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, Internal.DelegateVoidPromiseT<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromiseT<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidResultCancel<TResult>, Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, Internal.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromiseT<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidResultCancel<TResult>, Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, Internal.DelegateVoidVoid>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidVoid(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidPromiseCancel, Internal.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, Internal.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, Internal.DelegateArgVoid<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgVoid<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidPromiseCancel, Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, Internal.DelegateVoidResult<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidResult<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidPromiseTCancel<TResult>, Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, Internal.DelegateArgResult<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgResult<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidPromiseTCancel<TResult>, Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Action<TCapture, ResultContainer> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(continueCaptureValue, onContinue);
            }

            var promise = Internal.PromiseContinue<Internal.DelegateContinueCaptureVoidVoidCancel<TCapture>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueCaptureVoidVoidCancel<TCapture>(ref continueCaptureValue, onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, TResult> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(continueCaptureValue, onContinue);
            }

            var promise = Internal.PromiseContinue<TResult, Internal.DelegateContinueCaptureVoidResultCancel<TCapture, TResult>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueCaptureVoidResultCancel<TCapture, TResult>(ref continueCaptureValue, onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(continueCaptureValue, onContinue);
            }

            var promise = Internal.PromiseContinuePromise<Internal.DelegateContinueCaptureVoidResultCancel<TCapture, Promise>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueCaptureVoidResultCancel<TCapture, Promise>(ref continueCaptureValue, onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise<TResult>> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(continueCaptureValue, onContinue);
            }

            var promise = Internal.PromiseContinuePromise<TResult, Internal.DelegateContinueCaptureVoidResultCancel<TCapture, Promise<TResult>>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueCaptureVoidResultCancel<TCapture, Promise<TResult>>(ref continueCaptureValue, onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
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
        #region Resolve Callbacks
        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolve<Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolve<TResult, Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolvePromise<Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolvePromise<TResult, Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, T> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<T, Internal.DelegatePassthroughCancel, Internal.DelegateCaptureVoidResult<TCaptureReject, T>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidResult<TCaptureReject, T>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, T> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<T, Internal.DelegatePassthroughCancel, Internal.DelegateCaptureArgResult<TCaptureReject, TReject, T>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgResult<TCaptureReject, TReject, T>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<T>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<T, Internal.DelegatePassthroughCancel, Internal.DelegateCaptureVoidPromiseT<TCaptureReject, T>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromiseT<TCaptureReject, T>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<T>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<T, Internal.DelegatePassthroughCancel, Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, T>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, T>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, Internal.DelegateVoidVoid>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidVoid(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateArgVoidCancel<T>, Internal.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgVoidCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, Internal.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, Internal.DelegateArgVoid<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgVoid<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateArgVoidCancel<T>, Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgVoidCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, Internal.DelegateVoidResult<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidResult<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateArgResultCancel<T, TResult>, Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, Internal.DelegateArgResult<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgResult<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateArgResultCancel<T, TResult>, Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, Internal.DelegateVoidPromise>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromise(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgPromiseCancel<T>, Internal.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, Internal.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, Internal.DelegateArgPromise<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromise<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgPromiseCancel<T>, Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, Internal.DelegateVoidPromiseT<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromiseT<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgPromiseTCancel<T, TResult>, Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, Internal.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromiseT<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgPromiseTCancel<T, TResult>, Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, Internal.DelegateVoidPromise>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromise(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgVoidCancel<T>, Internal.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgVoidCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, Internal.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, Internal.DelegateArgPromise<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromise<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgVoidCancel<T>, Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgVoidCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, Internal.DelegateVoidPromiseT<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromiseT<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgResultCancel<T, TResult>, Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, Internal.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromiseT<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgResultCancel<T, TResult>, Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, Internal.DelegateVoidVoid>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidVoid(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgPromiseCancel<T>, Internal.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, Internal.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, Internal.DelegateArgVoid<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgVoid<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgPromiseCancel<T>, Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, Internal.DelegateVoidResult<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidResult<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgPromiseTCancel<T, TResult>, Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, Internal.DelegateArgResult<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgResult<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgPromiseTCancel<T, TResult>, Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture 2 values and add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with <paramref name="resolveCaptureValue"/> and the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with <paramref name="rejectCaptureValue"/> and that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(resolveCaptureValue, onResolved, rejectCaptureValue, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Action<TCapture, ResultContainer> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(continueCaptureValue, onContinue);
            }

            var promise = Internal.PromiseContinue<Internal.DelegateContinueCaptureArgVoidCancel<TCapture, T>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueCaptureArgVoidCancel<TCapture, T>(ref continueCaptureValue, onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, TResult> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(continueCaptureValue, onContinue);
            }

            var promise = Internal.PromiseContinue<TResult, Internal.DelegateContinueCaptureArgResultCancel<TCapture, T, TResult>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueCaptureArgResultCancel<TCapture, T, TResult>(ref continueCaptureValue, onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(continueCaptureValue, onContinue);
            }

            var promise = Internal.PromiseContinuePromise<Internal.DelegateContinueCaptureArgResultCancel<TCapture, T, Promise>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueCaptureArgResultCancel<TCapture, T, Promise>(ref continueCaptureValue, onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise<TResult>> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(continueCaptureValue, onContinue);
            }

            var promise = Internal.PromiseContinuePromise<TResult, Internal.DelegateContinueCaptureArgResultCancel<TCapture, T, Promise<TResult>>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueCaptureArgResultCancel<TCapture, T, Promise<TResult>>(ref continueCaptureValue, onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            HookupNewPromise(promise);
            return promise;
        }
        #endregion
    }
}