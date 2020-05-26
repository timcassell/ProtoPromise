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
    partial class Promise
    {
        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback and a cancelation token. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then(Action onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolve<Internal.DelegateVoidVoidCancel>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidVoidCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback and a cancelation token. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolve<TResult, Internal.DelegateVoidResultCancel<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback and a cancelation token. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then(Func<Promise> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolvePromise<Internal.DelegateVoidPromiseCancel>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback and a cancelation token. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolvePromise<TResult, Internal.DelegateVoidPromiseTCancel<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch(Action onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegatePassthroughCancel, Internal.DelegateVoidVoid>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidVoid(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TReject>(Action<TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegatePassthroughCancel, Internal.DelegateArgVoid<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgVoid<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch(Func<Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegatePassthroughCancel, Internal.DelegateVoidPromise>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromise(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Catch<TReject>(Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegatePassthroughCancel, Internal.DelegateArgPromise<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromise<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Action onResolved, Action onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateVoidVoidCancel, Internal.DelegateVoidVoid>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidVoidCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidVoid(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Action onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateVoidVoidCancel, Internal.DelegateArgVoid<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidVoidCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgVoid<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateVoidResultCancel<TResult>, Internal.DelegateVoidResult<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidResult<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateVoidResultCancel<TResult>, Internal.DelegateArgResult<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgResult<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidPromiseCancel, Internal.DelegateVoidPromise>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromise(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Func<Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidPromiseCancel, Internal.DelegateArgPromise<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromise<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidPromiseTCancel<TResult>, Internal.DelegateVoidPromiseT<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromiseT<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidPromiseTCancel<TResult>, Internal.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromiseT<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Action onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidVoidCancel, Internal.DelegateVoidPromise>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidVoidCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromise(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Action onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidVoidCancel, Internal.DelegateArgPromise<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidVoidCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromise<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidResultCancel<TResult>, Internal.DelegateVoidPromiseT<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromiseT<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidResultCancel<TResult>, Internal.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromiseT<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Func<Promise> onResolved, Action onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidPromiseCancel, Internal.DelegateVoidVoid>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidVoid(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Func<Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateVoidPromiseCancel, Internal.DelegateArgVoid<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseCancel(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgVoid<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidPromiseTCancel<TResult>, Internal.DelegateVoidResult<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidResult<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateVoidPromiseTCancel<TResult>, Internal.DelegateArgResult<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgResult<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith(Action<ResultContainer> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(onContinue);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseContinue<Internal.DelegateContinueVoidVoidCancel>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueVoidVoidCancel(onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, TResult> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(onContinue);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseContinue<TResult, Internal.DelegateContinueVoidResultCancel<TResult>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueVoidResultCancel<TResult>(onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith(Func<ResultContainer, Promise> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(onContinue);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseContinuePromise<Internal.DelegateContinueVoidResultCancel<Promise>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueVoidResultCancel<Promise>(onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, Promise<TResult>> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(onContinue);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseContinuePromise<TResult, Internal.DelegateContinueVoidResultCancel<Promise<TResult>>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueVoidResultCancel<Promise<TResult>>(onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion
    }

    public abstract partial class Promise<T> : Promise
    {
        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then(Action<T> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolve<Internal.DelegateArgVoidCancel<T>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgVoidCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolve<TResult, Internal.DelegateArgResultCancel<T, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolvePromise<Internal.DelegateArgPromiseCancel<T>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolvePromise<TResult, Internal.DelegateArgPromiseTCancel<T, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch(Func<T> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<T, Internal.DelegatePassthroughCancel, Internal.DelegateVoidResult<T>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidResult<T>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch<TReject>(Func<TReject, T> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<T, Internal.DelegatePassthroughCancel, Internal.DelegateArgResult<TReject, T>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgResult<TReject, T>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch(Func<Promise<T>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<T, Internal.DelegatePassthroughCancel, Internal.DelegateVoidPromiseT<T>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromiseT<T>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is canceled or rejected with any other reason or no reason, the new <see cref="Promise{T}"/> will be canceled or rejected with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<T> Catch<TReject>(Func<TReject, Promise<T>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Catch(onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<T, Internal.DelegatePassthroughCancel, Internal.DelegateArgPromiseT<TReject, T>>.GetOrCreate();
            promise.resolver = new Internal.DelegatePassthroughCancel(cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromiseT<TReject, T>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Action<T> onResolved, Action onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateArgVoidCancel<T>, Internal.DelegateVoidVoid>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgVoidCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidVoid(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Action<T> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<Internal.DelegateArgVoidCancel<T>, Internal.DelegateArgVoid<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgVoidCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgVoid<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateArgResultCancel<T, TResult>, Internal.DelegateVoidResult<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidResult<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveReject<TResult, Internal.DelegateArgResultCancel<T, TResult>, Internal.DelegateArgResult<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgResult<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgPromiseCancel<T>, Internal.DelegateVoidPromise>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromise(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Func<T, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgPromiseCancel<T>, Internal.DelegateArgPromise<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromise<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgPromiseTCancel<T, TResult>, Internal.DelegateVoidPromiseT<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromiseT<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgPromiseTCancel<T, TResult>, Internal.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromiseT<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Action<T> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgVoidCancel<T>, Internal.DelegateVoidPromise>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgVoidCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromise(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Action<T> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgVoidCancel<T>, Internal.DelegateArgPromise<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgVoidCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromise<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgResultCancel<T, TResult>, Internal.DelegateVoidPromiseT<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidPromiseT<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgResultCancel<T, TResult>, Internal.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgPromiseT<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved, Action onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgPromiseCancel<T>, Internal.DelegateVoidVoid>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidVoid(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise Then<TReject>(Func<T, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<Internal.DelegateArgPromiseCancel<T>, Internal.DelegateArgVoid<TReject>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseCancel<T>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgVoid<TReject>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgPromiseTCancel<T, TResult>, Internal.DelegateVoidResult<TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateVoidResult<TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> and <paramref name="onRejected"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return Then(onResolved, onRejected);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseResolveRejectPromise<TResult, Internal.DelegateArgPromiseTCancel<T, TResult>, Internal.DelegateArgResult<TReject, TResult>>.GetOrCreate();
            promise.resolver = new Internal.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            promise.rejecter = new Internal.DelegateArgResult<TReject, TResult>(onRejected);
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith(Action<ResultContainer> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(onContinue);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseContinue<Internal.DelegateContinueArgVoidCancel<T>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueArgVoidCancel<T>(onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, TResult> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(onContinue);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseContinue<TResult, Internal.DelegateContinueArgResultCancel<T, TResult>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueArgResultCancel<T, TResult>(onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }


        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith(Func<ResultContainer, Promise> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(onContinue);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseContinuePromise<Internal.DelegateContinueArgResultCancel<T, Promise>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueArgResultCancel<T, Promise>(onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, Promise<TResult>> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (!cancelationToken.CanBeCanceled)
            {
                return ContinueWith(onContinue);
            }

            cancelationToken.Retain();
            var promise = Internal.PromiseContinuePromise<TResult, Internal.DelegateContinueArgResultCancel<T, Promise<TResult>>>.GetOrCreate();
            promise.continuer = new Internal.DelegateContinueArgResultCancel<T, Promise<TResult>>(onContinue, cancelationToken, RegisterForCancelation(promise, cancelationToken));
            MaybeHookupNewPromise(promise);
            return promise;
        }
        #endregion
    }
}