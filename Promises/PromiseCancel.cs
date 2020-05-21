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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidVoidCancel.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidResultCancel<TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidPromiseCancel.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidPromiseTCancel<TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegatePassthroughCancel.GetOrCreate(cancelationToken);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegatePassthroughCancel.GetOrCreate(cancelationToken);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegatePassthroughCancel.GetOrCreate(cancelationToken);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegatePassthroughCancel.GetOrCreate(cancelationToken);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidVoidCancel.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidVoidCancel.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidResultCancel<TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidResultCancel<TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidPromiseCancel.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidPromiseCancel.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidPromiseTCancel<TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidPromiseTCancel<TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidVoidCancel.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidVoidCancel.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidResultCancel<TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidResultCancel<TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidPromiseCancel.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidPromiseCancel.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidPromiseTCancel<TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateVoidPromiseTCancel<TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var del = Internal.DelegateContinueVoidVoidCancel.GetOrCreate(onContinue, cancelationToken);
            var promise = Internal.PromiseContinue0.GetOrCreate(del);
            HookupNewPromise(promise);
            del.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, TResult> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var del = Internal.DelegateContinueVoidResultCancel<TResult>.GetOrCreate(onContinue, cancelationToken);
            var promise = Internal.PromiseContinue<TResult>.GetOrCreate(del);
            HookupNewPromise(promise);
            del.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var del = Internal.DelegateContinueVoidResultCancel<Promise>.GetOrCreate(onContinue, cancelationToken);
            var promise = Internal.PromiseContinuePromise0.GetOrCreate(del);
            HookupNewPromise(promise);
            del.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var del = Internal.DelegateContinueVoidResultCancel<Promise<TResult>>.GetOrCreate(onContinue, cancelationToken);
            var promise = Internal.PromiseContinuePromise<TResult>.GetOrCreate(del);
            HookupNewPromise(promise);
            del.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgVoidCancel<T>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgResultCancel<T, TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgPromiseCancel<T>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgPromiseTCancel<T, TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegatePassthroughCancel.GetOrCreate(cancelationToken);
            var rejectDelegate = Internal.DelegateVoidResult<T>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegatePassthroughCancel.GetOrCreate(cancelationToken);
            var rejectDelegate = Internal.DelegateArgResult<TReject, T>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegatePassthroughCancel.GetOrCreate(cancelationToken);
            var rejectDelegate = Internal.DelegateVoidPromiseT<T>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegatePassthroughCancel.GetOrCreate(cancelationToken);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, T>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgVoidCancel<T>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgVoidCancel<T>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgResultCancel<T, TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgResultCancel<T, TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgPromiseCancel<T>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgPromiseCancel<T>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgPromiseTCancel<T, TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgPromiseTCancel<T, TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgVoidCancel<T>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgVoidCancel<T>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgResultCancel<T, TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgResultCancel<T, TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgPromiseCancel<T>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgPromiseCancel<T>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgPromiseTCancel<T, TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var resolveDelegate = Internal.DelegateArgPromiseTCancel<T, TResult>.GetOrCreate(onResolved, cancelationToken);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate);
            HookupNewPromise(promise);
            resolveDelegate.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var del = Internal.DelegateContinueArgVoidCancel<T>.GetOrCreate(onContinue, cancelationToken);
            var promise = Internal.PromiseContinue0.GetOrCreate(del);
            HookupNewPromise(promise);
            del.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        ///
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, TResult> onContinue, CancelationToken cancelationToken)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var del = Internal.DelegateContinueArgResultCancel<T, TResult>.GetOrCreate(onContinue, cancelationToken);
            var promise = Internal.PromiseContinue<TResult>.GetOrCreate(del);
            HookupNewPromise(promise);
            del.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var del = Internal.DelegateContinueArgResultCancel<T, Promise>.GetOrCreate(onContinue, cancelationToken);
            var promise = Internal.PromiseContinuePromise0.GetOrCreate(del);
            HookupNewPromise(promise);
            del.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
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

            if (cancelationToken.CanBeCanceled)
            {
                cancelationToken.Retain();
            }

            var del = Internal.DelegateContinueArgResultCancel<T, Promise<TResult>>.GetOrCreate(onContinue, cancelationToken);
            var promise = Internal.PromiseContinuePromise<TResult>.GetOrCreate(del);
            HookupNewPromise(promise);
            del.cancelationRegistration = RegisterForCancelation(promise, cancelationToken);
            return promise;
        }
        #endregion
    }
}