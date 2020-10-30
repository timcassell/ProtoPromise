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

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0017 // Simplify object initialization

using System;

namespace Proto.Promises
{
    public abstract partial class Promise
    {
        /// <summary>
        /// Capture a value and add a progress listener. Returns this.
        /// <para/><paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onProgress"/> will stop being invoked.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
        public Promise Progress<TCaptureProgress>(TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress, CancelationToken cancelationToken = default(CancelationToken))
        {
            SubscribeProgress(progressCaptureValue, onProgress, cancelationToken);
            return this;
        }

        /// <summary>
        /// Capture a value and add a cancel callback. Returns this.
        /// <para/>If/when this instance is canceled, <paramref name="onCanceled"/> will be invoked with <paramref name="cancelCaptureValue"/> and the cancelation reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onCanceled"/> will not be invoked.
        /// </summary>
        public Promise CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Action<TCaptureCancel, ReasonContainer> onCanceled, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                if (cancelationToken.CanBeCanceled)
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        // Don't hook up callback if token is already canceled.
                        return this;
                    }
                    var cancelDelegate = Internal.CancelDelegate<Internal.CancelDelegatePromiseCancel<TCaptureCancel>>.GetOrCreate();
                    cancelDelegate.canceler = new Internal.CancelDelegatePromiseCancel<TCaptureCancel>(ref cancelCaptureValue, onCanceled, this);
                    cancelDelegate.canceler.cancelationRegistration = cancelationToken.RegisterInternal(cancelDelegate);
                    AddWaiter(cancelDelegate);
                }
                else
                {
                    var cancelDelegate = Internal.CancelDelegate<Internal.CancelDelegatePromise<TCaptureCancel>>.GetOrCreate();
                    cancelDelegate.canceler = new Internal.CancelDelegatePromise<TCaptureCancel>(ref cancelCaptureValue, onCanceled);
                    AddWaiter(cancelDelegate);
                }
            }
            return this;
        }

        /// <summary>
        /// Capture a value and add a finally callback. Returns this.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked with <paramref name="finallyCaptureValue"/>.
        /// </summary>
        public Promise Finally<TCaptureFinally>(TCaptureFinally finallyCaptureValue, Action<TCaptureFinally> onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            AddWaiter(InternalProtected.FinallyDelegateCapture<TCaptureFinally>.GetOrCreate(ref finallyCaptureValue, onFinally));
            return this;
        }

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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolve<InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolve<InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolve<TResult, InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolve<TResult, InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolvePromise<InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolvePromise<InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolvePromise<TResult, InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolvePromise<TResult, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateResolvePassthroughCancel, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthroughCancel(true);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateResolvePassthrough, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthrough();
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateResolvePassthroughCancel, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthroughCancel(true);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateResolvePassthrough, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthrough();
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateResolvePassthroughCancel, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthroughCancel(true);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateResolvePassthrough, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthrough();
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateResolvePassthroughCancel, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthroughCancel(true);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateResolvePassthrough, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthrough();
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateVoidVoidCancel, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoidCancel(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateVoidVoid, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoid(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateVoidVoidCancel, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoidCancel(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateVoidVoid, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoid(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateVoidResultCancel<TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResultCancel<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateVoidResult<TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResult<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateVoidResultCancel<TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResultCancel<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateVoidResult<TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResult<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromiseCancel, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseCancel(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromise, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromise(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromiseCancel, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseCancel(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromise, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromise(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseTCancel<TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseTCancel<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseT<TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseT<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseTCancel<TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseTCancel<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseT<TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseT<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidVoidCancel, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoidCancel(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidVoid, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoid(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject, TReject>(Action onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidVoidCancel, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoidCancel(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidVoid, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoid(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoidCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidVoid<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidResultCancel<TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResultCancel<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidResult<TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResult<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidResultCancel<TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResultCancel<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidResult<TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResult<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResultCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidResult<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromiseCancel, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseCancel(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromise, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromise(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject, TReject>(Func<Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromiseCancel, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseCancel(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromise, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromise(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseCancel<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromise<TCaptureResolve>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseTCancel<TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseTCancel<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseT<TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseT<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseTCancel<TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseTCancel<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseT<TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseT<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseTCancel<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureResolve, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Action<TCapture, ResultContainer> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinue<InternalProtected.DelegateContinueCaptureVoidVoidCancel<TCapture>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureVoidVoidCancel<TCapture>(ref continueCaptureValue, onContinue);
                promise.continuer.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinue<InternalProtected.DelegateContinueCaptureVoidVoid<TCapture>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureVoidVoid<TCapture>(ref continueCaptureValue, onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, TResult> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinue<TResult, InternalProtected.DelegateContinueCaptureVoidResultCancel<TCapture, TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureVoidResultCancel<TCapture, TResult>(ref continueCaptureValue, onContinue);
                promise.continuer.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinue<TResult, InternalProtected.DelegateContinueCaptureVoidResult<TCapture, TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureVoidResult<TCapture, TResult>(ref continueCaptureValue, onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinuePromise<InternalProtected.DelegateContinueCaptureVoidPromiseCancel<TCapture>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureVoidPromiseCancel<TCapture>(ref continueCaptureValue, onContinue);
                promise.continuer.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinuePromise<InternalProtected.DelegateContinueCaptureVoidPromise<TCapture>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureVoidPromise<TCapture>(ref continueCaptureValue, onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise<TResult>> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinuePromise<TResult, InternalProtected.DelegateContinueCaptureVoidPromiseTCancel<TCapture, TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureVoidPromiseTCancel<TCapture, TResult>(ref continueCaptureValue, onContinue);
                promise.continuer.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinuePromise<TResult, InternalProtected.DelegateContinueCaptureVoidPromiseT<TCapture, TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureVoidPromiseT<TCapture, TResult>(ref continueCaptureValue, onContinue);
                HookupNewPromise(promise);
                return promise;
            }
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
        /// Capture a value and add a progress listener. Returns this.
        /// <para/><paramref name="onProgress"/> will be invoked with <paramref name="progressCaptureValue"/> and progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onProgress"/> will stop being invoked.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
        public new Promise<T> Progress<TCaptureProgress>(TCaptureProgress progressCaptureValue, Action<TCaptureProgress, float> onProgress, CancelationToken cancelationToken = default(CancelationToken))
        {
            SubscribeProgress(progressCaptureValue, onProgress, cancelationToken);
            return this;
        }

        /// <summary>
        /// Capture a value and add a cancel callback. Returns this.
        /// <para/>If/when this instance is canceled, <paramref name="onCanceled"/> will be invoked with <paramref name="cancelCaptureValue"/> and the cancelation reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onCanceled"/> will not be invoked.
        /// </summary>
        public new Promise<T> CatchCancelation<TCaptureCancel>(TCaptureCancel cancelCaptureValue, Action<TCaptureCancel, ReasonContainer> onCanceled, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            base.CatchCancelation(cancelCaptureValue, onCanceled, cancelationToken);
            return this;
        }

        /// <summary>
        /// Capture a value and add a finally callback. Returns this.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked with <paramref name="finallyCaptureValue"/>.
        /// </summary>
        public new Promise<T> Finally<TCaptureFinally>(TCaptureFinally finallyCaptureValue, Action<TCaptureFinally> onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            base.Finally(finallyCaptureValue, onFinally);
            return this;
        }

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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolve<InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolve<InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolve<TResult, InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolve<TResult, InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolvePromise<InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolvePromise<InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolvePromise<TResult, InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolvePromise<TResult, InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<T> Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, T> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<T, InternalProtected.DelegateResolvePassthroughCancel, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthroughCancel(true);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, T>(ref rejectCaptureValue, onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<T, InternalProtected.DelegateResolvePassthrough, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthrough();
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, T>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<T> Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, T> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<T, InternalProtected.DelegateResolvePassthroughCancel, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthroughCancel(true);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, T>(ref rejectCaptureValue, onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<T, InternalProtected.DelegateResolvePassthrough, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthrough();
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, T>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<T> Catch<TCaptureReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<T>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<T, InternalProtected.DelegateResolvePassthroughCancel, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthroughCancel(true);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, T>(ref rejectCaptureValue, onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<T, InternalProtected.DelegateResolvePassthrough, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthrough();
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, T>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<T> Catch<TCaptureReject, TReject>(TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<T>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<T, InternalProtected.DelegateResolvePassthroughCancel, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthroughCancel(true);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, T>(ref rejectCaptureValue, onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<T, InternalProtected.DelegateResolvePassthrough, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateResolvePassthrough();
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, T>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateArgVoidCancel<T>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoidCancel<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateArgVoid<T>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoid<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateArgVoidCancel<T>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoidCancel<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateArgVoid<T>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoid<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateArgResultCancel<T, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResultCancel<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateArgResult<T, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResult<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateArgResultCancel<T, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResultCancel<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateArgResult<T, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResult<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromiseCancel<T>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseCancel<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromise<T>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromise<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromiseCancel<T>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseCancel<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromise<T>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromise<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseTCancel<T, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseTCancel<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseT<T, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseT<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseTCancel<T, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseTCancel<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseT<T, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseT<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgVoidCancel<T>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoidCancel<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgVoid<T>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoid<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>, InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromise<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject, TReject>(Action<T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgVoidCancel<T>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoidCancel<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgVoid<T>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoid<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Action<TCaptureResolve, T> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoidCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>, InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgVoid<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromise<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgResultCancel<T, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResultCancel<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgResult<T, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResult<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidPromiseT<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgResultCancel<T, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResultCancel<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgResult<T, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResult<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, TResult> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResultCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgResult<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromiseCancel<T>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseCancel<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromise<T>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromise<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>, InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidVoid<TCaptureReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureReject, TReject>(Func<T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromiseCancel<T>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseCancel<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromise<T>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromise<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TCaptureResolve, TCaptureReject, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise> onResolved, TCaptureReject rejectCaptureValue, Action<TCaptureReject, TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseCancel<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>, InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromise<TCaptureResolve, T>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgVoid<TCaptureReject, TReject>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseTCancel<T, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseTCancel<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseT<T, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseT<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureVoidResult<TCaptureReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureReject, TResult, TReject>(Func<T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseTCancel<T, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseTCancel<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseT<T, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseT<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TCaptureResolve, TCaptureReject, TResult, TReject>(TCaptureResolve resolveCaptureValue, Func<TCaptureResolve, T, Promise<TResult>> onResolved, TCaptureReject rejectCaptureValue, Func<TCaptureReject, TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseTCancel<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                promise.resolver.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>, InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateCaptureArgPromiseT<TCaptureResolve, T, TResult>(ref resolveCaptureValue, onResolved);
                promise.rejecter = new InternalProtected.DelegateCaptureArgResult<TCaptureReject, TReject, TResult>(ref rejectCaptureValue, onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Action<TCapture, ResultContainer> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinue<InternalProtected.DelegateContinueCaptureArgVoidCancel<TCapture, T>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureArgVoidCancel<TCapture, T>(ref continueCaptureValue, onContinue);
                promise.continuer.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinue<InternalProtected.DelegateContinueCaptureArgVoid<TCapture, T>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureArgVoid<TCapture, T>(ref continueCaptureValue, onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, TResult> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinue<TResult, InternalProtected.DelegateContinueCaptureArgResultCancel<TCapture, T, TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureArgResultCancel<TCapture, T, TResult>(ref continueCaptureValue, onContinue);
                promise.continuer.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinue<TResult, InternalProtected.DelegateContinueCaptureArgResult<TCapture, T, TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureArgResult<TCapture, T, TResult>(ref continueCaptureValue, onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Capture a value and add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith<TCapture>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinuePromise<InternalProtected.DelegateContinueCaptureArgPromiseCancel<TCapture, T>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureArgPromiseCancel<TCapture, T>(ref continueCaptureValue, onContinue);
                promise.continuer.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinuePromise<InternalProtected.DelegateContinueCaptureArgPromise<TCapture, T>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureArgPromise<TCapture, T>(ref continueCaptureValue, onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with <paramref name="continueCaptureValue"/> and the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TCapture, TResult>(TCapture continueCaptureValue, Func<TCapture, ResultContainer, Promise<TResult>> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinuePromise<TResult, InternalProtected.DelegateContinueCaptureArgPromiseTCancel<TCapture, T, TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureArgPromiseTCancel<TCapture, T, TResult>(ref continueCaptureValue, onContinue);
                promise.continuer.cancelationRegistration = cancelationToken.RegisterInternal(promise);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinuePromise<TResult, InternalProtected.DelegateContinueCaptureArgPromiseT<TCapture, T, TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueCaptureArgPromiseT<TCapture, T, TResult>(ref continueCaptureValue, onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }
        #endregion
    }
}