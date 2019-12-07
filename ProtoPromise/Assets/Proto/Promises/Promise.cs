// define PROTO_PROMISE_DEBUG_ENABLE to enable debugging options in RELEASE mode. define PROTO_PROMISE_DEBUG_DISABLE to disable debugging options in DEBUG mode.
#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#endif
// define PROTO_PROMISE_CANCEL_DISABLE to disable cancelations on promises.
// If Cancelations are enabled, it breaks the Promises/A+ spec "2.1. Promise States", but allows breaking promise chains. Execution is also a little slower.
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#endif
// define PROTO_PROMISE_PROGRESS_DISABLE to disable progress reports on promises.
// If Progress is enabled, promises use more memory, and it creates an upper bound to the depth of a promise chain (see Config for details).
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#endif

using System;

namespace Proto.Promises
{
    /// <summary>
    /// A <see cref="Promise"/> represents the eventual result of an asynchronous operation.
    /// The primary way of interacting with a <see cref="Promise"/> is through its then method,
    /// which registers callbacks to be invoked when the <see cref="Promise"/> is resolved,
    /// or the reason why the <see cref="Promise"/> cannot be resolved.
    /// </summary>
    public abstract partial class Promise : ICancelableAny, IRetainable, IPotentialCancelation
    {
        public enum State : byte
        {
            Pending,
            Resolved,
            Rejected,
#if !PROMISE_CANCEL
            [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", false)]
#endif
            Canceled // This violates Promises/A+ 2.1 when CANCEL is enabled.
        }

        /// <summary>
        /// Retain this instance. Allows adding more callbacks and prevents uncaught rejections from being thrown until this is released.
        /// <para/>This should always be paired with a call to <see cref="Release"/>
        /// </summary>
        public void Retain()
        {
            ValidateOperation(this, 1);
#if PROMISE_DEBUG
            checked // If this fails, change _userRetainCounter to ulong.
            {
                ++_userRetainCounter;
            }
#endif
            RetainInternal();
        }

        /// <summary>
        /// Release this instance. Allows uncaught rejections to be thrown and prevents adding more callbacks when this is complete (if <see cref="Release"/> has been called for all <see cref="Retain"/> calls).
        /// <para/>This should always be paired with a call to <see cref="Retain"/>
        /// </summary>
		public void Release()
        {
            ValidateOperation(this, 1);
#if PROMISE_DEBUG
            if (_userRetainCounter == 0)
            {
                throw new InvalidOperationException("You must call Retain before you call Release!", GetFormattedStacktrace(1));
            }
            --_userRetainCounter;
#endif
            ReleaseInternal();
        }

        /// <summary>
        /// Mark this instance to not be added to the pool when it is complete and released. This cannot be un-done.
        /// <para/>NOTE: This automatically happens to all promises that are created or released while <see cref="Config.ObjectPooling"/> is not <see cref="PoolType.All"/>.
        /// </summary>
        public Promise DontPool()
        {
            ValidateOperation(this, 1);

            _dontPool = true;
            return this;
        }

        /// <summary>
        /// Returns a new <see cref="YieldInstruction"/> that can be yielded in a coroutine to wait until this is complete.
        /// </summary>
        public YieldInstruction ToYieldInstruction()
        {
            ValidateOperation(this, 1);

            var yield = InternalYieldInstruction.GetOrCreate(this);
            AddWaiter(yield);
            ReleaseWithoutDisposeCheck(); // No need to keep this retained.
            return yield;
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/> that adopts the state of this. This is mostly useful for branches that you expect might be canceled, and you don't want all branches to be canceled.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. This is mostly useful for cancelations of branched promises. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", false)]
#endif
        public Promise ThenDuplicate()
        {
            ValidateOperation(this, 1);

            var promise = GetDuplicate();
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a progress listener. <paramref name="onProgress"/> will be invoked with progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// Returns this.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROMISE_PROGRESS from your compiler symbols to enable progress reports.", true)]
#endif
        public Promise Progress(Action<float> onProgress)
        {
            SubscribeProgress(onProgress, 1);
            return this;
        }

        /// <summary>
        /// Add a cancel callback.
        /// <para/>If this instance is canceled for any or no reason, <paramref name="onCanceled"/> will be invoked.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public void CatchCancelation(Action onCanceled)
        {
            ValidateCancel(1);
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                AddWaiter(Internal.CancelDelegateAny.GetOrCreate(onCanceled, 1));
                ReleaseWithoutDisposeCheck(); // No need to keep this retained.
            }
        }

        /// <summary>
        /// Add a cancel callback. Returns an <see cref="IPotentialCancelation"/> object.
        /// <para/>If this is canceled with any reason that is convertible to <typeparamref name="TCancel"/>, <paramref name="onCanceled"/> will be invoked with that reason.
        /// <para/>If this is canceled for any other reason or no reason, the returned <see cref="IPotentialCancelation"/> will be canceled with the same reason.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public IPotentialCancelation CatchCancelation<TCancel>(Action<TCancel> onCanceled)
        {
            ValidateCancel(1);
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                var cancelation = Internal.CancelDelegate<TCancel>.GetOrCreate(onCanceled, this, 1);
                AddWaiter(cancelation);
                return cancelation;
            }
            return this;
        }

        /// <summary>
        /// Cancels this promise and all promises that have been chained from this without a reason.
        /// Does nothing if this promise isn't pending.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public void Cancel()
        {
            ValidateCancel(1);
            ValidateOperation(this, 1);

            if (_state != State.Pending)
            {
                return;
            }

            CancelInternal(Internal.CancelVoid.GetOrCreate());
        }

        /// <summary>
        /// Cancels this promise and all promises that have been chained from this with the provided cancel reason.
        /// Does nothing if this promise isn't pending.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public void Cancel<TCancel>(TCancel reason)
        {
            ValidateCancel(1);
            ValidateOperation(this, 1);

            if (_state != State.Pending)
            {
                return;
            }

            CancelInternal(Internal.CancelValue<TCancel>.GetOrCreate(reason));
        }

        /// <summary>
        /// Add a finally callback. It will be invoked when this resolves, rejects, or cancels. Returns this.
        /// </summary>
        public Promise Finally(Action onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            AddWaiter(Internal.FinallyDelegate.GetOrCreate(onFinally, this, 1));
            ReleaseWithoutDisposeCheck(); // No need to keep this retained.
            return this;
        }

        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise Then(Action onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseVoidResolve0.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<T> Then<T>(Func<T> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseVoidResolve<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise Then(Func<Promise> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseVoidResolvePromise0.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<T> Then<T>(Func<Promise<T>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseVoidResolvePromise<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise ThenDefer(Func<Action<Deferred>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseVoidResolveDeferred0.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise{T}"/> will be controlled by the <see cref="Promise{T}.Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<T> ThenDefer<T>(Func<Action<Promise<T>.Deferred>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseVoidResolveDeferred<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Catch(Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseReject0.GetOrCreate(Internal.DelegateVoidVoid0.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise Catch<TReject>(Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseReject0.GetOrCreate(Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise Catch<TReject>(Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseReject0.GetOrCreate(Internal.DelegateVoidVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Catch(Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseRejectPromise0.GetOrCreate(Internal.DelegateVoidResult<Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise Catch<TReject>(Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseRejectPromise0.GetOrCreate(Internal.DelegateArgResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise Catch<TReject>(Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseRejectPromise0.GetOrCreate(Internal.DelegateVoidResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise CatchDefer(Func<Action<Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseRejectDeferred0.GetOrCreate(Internal.DelegateVoidResult<Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise CatchDefer<TReject>(Func<TReject, Action<Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseRejectDeferred0.GetOrCreate(Internal.DelegateArgResult<TReject, Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise CatchDefer<TReject>(Func<Action<Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseRejectDeferred0.GetOrCreate(Internal.DelegateVoidResult<TReject, Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Action onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveReject0.GetOrCreate(Internal.DelegateVoidVoid0.GetOrCreate(onResolved), Internal.DelegateVoidVoid0.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Action onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveReject0.GetOrCreate(Internal.DelegateVoidVoid0.GetOrCreate(onResolved), Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Action onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveReject0.GetOrCreate(Internal.DelegateVoidVoid0.GetOrCreate(onResolved), Internal.DelegateVoidVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Then<T>(Func<T> onResolved, Func<T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(Internal.DelegateVoidResult<T>.GetOrCreate(onResolved), Internal.DelegateVoidResult<T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Then<T, TReject>(Func<T> onResolved, Func<TReject, T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(Internal.DelegateVoidResult<T>.GetOrCreate(onResolved), Internal.DelegateArgResult<TReject, T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Then<T, TReject>(Func<T> onResolved, Func<T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(Internal.DelegateVoidResult<T>.GetOrCreate(onResolved), Internal.DelegateVoidResult<TReject, T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(Internal.DelegateVoidResult<Promise>.GetOrCreate(onResolved), Internal.DelegateVoidResult<Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Func<Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(Internal.DelegateVoidResult<Promise>.GetOrCreate(onResolved), Internal.DelegateArgResult<TReject, Promise>.GetOrCreate(onRejected), 1);
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
        public Promise Then<TReject>(Func<Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(Internal.DelegateVoidResult<Promise>.GetOrCreate(onResolved), Internal.DelegateVoidResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Then<T>(Func<Promise<T>> onResolved, Func<Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(Internal.DelegateVoidResult<Promise<T>>.GetOrCreate(onResolved), Internal.DelegateVoidResult<Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Then<T, TReject>(Func<Promise<T>> onResolved, Func<TReject, Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(Internal.DelegateVoidResult<Promise<T>>.GetOrCreate(onResolved), Internal.DelegateArgResult<TReject, Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Then<T, TReject>(Func<Promise<T>> onResolved, Func<Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(Internal.DelegateVoidResult<Promise<T>>.GetOrCreate(onResolved), Internal.DelegateVoidResult<TReject, Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenDefer(Func<Action<Deferred>> onResolved, Func<Action<Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectDeferred0.GetOrCreate(Internal.DelegateVoidResult<Action<Deferred>>.GetOrCreate(onResolved), Internal.DelegateVoidResult<Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenDefer<TReject>(Func<Action<Deferred>> onResolved, Func<TReject, Action<Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectDeferred0.GetOrCreate(Internal.DelegateVoidResult<Action<Deferred>>.GetOrCreate(onResolved), Internal.DelegateArgResult<TReject, Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenDefer<TReject>(Func<Action<Deferred>> onResolved, Func<Action<Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectDeferred0.GetOrCreate(Internal.DelegateVoidResult<Action<Deferred>>.GetOrCreate(onResolved), Internal.DelegateVoidResult<TReject, Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise{T}"/> will be controlled by the <see cref="Promise{T}.Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> ThenDefer<T>(Func<Action<Promise<T>.Deferred>> onResolved, Func<Action<Promise<T>.Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectDeferred<T>.GetOrCreate(Internal.DelegateVoidResult<Action<Promise<T>.Deferred>>.GetOrCreate(onResolved), Internal.DelegateVoidResult<Action<Promise<T>.Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise{T}"/> will be controlled by the <see cref="Promise{T}.Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> ThenDefer<T, TReject>(Func<Action<Promise<T>.Deferred>> onResolved, Func<TReject, Action<Promise<T>.Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectDeferred<T>.GetOrCreate(Internal.DelegateVoidResult<Action<Promise<T>.Deferred>>.GetOrCreate(onResolved), Internal.DelegateArgResult<TReject, Action<Promise<T>.Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise{T}"/> will be controlled by the <see cref="Promise{T}.Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> ThenDefer<T, TReject>(Func<Action<Promise<T>.Deferred>> onResolved, Func<Action<Promise<T>.Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Internal.PromiseResolveRejectDeferred<T>.GetOrCreate(Internal.DelegateVoidResult<Action<Promise<T>.Deferred>>.GetOrCreate(onResolved), Internal.DelegateVoidResult<TReject, Action<Promise<T>.Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Complete Callbacks
        /// <summary>
        /// Add a resolve-or-reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved or rejected with any reason or no reason, <paramref name="onResolvedOrRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// <para/>Note: Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// </summary>
        public Promise Complete(Action onResolvedOrRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected", 1);

            var promise = Internal.PromiseComplete0.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve-or-reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved or rejected with any reason or no reason, <paramref name="onResolvedOrRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// <para/>Note: Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// </summary>
        public Promise<T> Complete<T>(Func<T> onResolvedOrRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected", 1);

            var promise = Internal.PromiseComplete<T>.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve-or-reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved or rejected with any reason or no reason, <paramref name="onResolvedOrRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// <para/>Note: Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// </summary>
        public Promise Complete(Func<Promise> onResolvedOrRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected", 1);

            var promise = Internal.PromiseCompletePromise0.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve-or-reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved or rejected with any reason or no reason, <paramref name="onResolvedOrRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// <para/>Note: Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// </summary>
		public Promise<T> Complete<T>(Func<Promise<T>> onResolvedOrRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected", 1);

            var promise = Internal.PromiseCompletePromise<T>.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve-or-reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved or rejected with any reason or no reason, <paramref name="onResolvedOrRejected"/> will be invoked.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// <para/>Note: Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// </summary>
        public Promise CompleteDefer(Func<Action<Deferred>> onResolvedOrRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected", 1);

            var promise = Internal.PromiseCompleteDeferred0.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve-or-reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved or rejected with any reason or no reason, <paramref name="onResolvedOrRejected"/> will be invoked.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise{T}"/> will be controlled by the <see cref="Promise{T}.Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// <para/>Note: Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// </summary>
		public Promise<T> CompleteDefer<T>(Func<Action<Promise<T>.Deferred>> onResolvedOrRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected", 1);

            var promise = Internal.PromiseCompleteDeferred<T>.GetOrCreate(onResolvedOrRejected, 1);
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
        /// Mark this instance to not be added to the pool when it is complete and released. This cannot be un-done.
        /// <para/>NOTE: This automatically happens to all promises that are created or released while <see cref="Promise.Config.ObjectPooling"/> is not <see cref="Promise.PoolType.All"/>.
        /// </summary>
        public new Promise<T> DontPool()
        {
            ValidateOperation(this, 1);

            _dontPool = true;
            return this;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/> that adopts the state of this. This is mostly useful for branches that you expect might be canceled, and you don't want all branches to be canceled.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. This is mostly useful for cancelations of branched promises. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", false)]
#endif
        public new Promise<T> ThenDuplicate()
        {
            ValidateOperation(this, 1);

            var promise = Promise.Internal.DuplicatePromise<T>.GetOrCreate(1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a finally callback. It will be invoked when this resolves, rejects, or cancels. Returns this.
        /// </summary>
        public new Promise<T> Finally(Action onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            AddWaiter(Promise.Internal.FinallyDelegate.GetOrCreate(onFinally, this, 1));
            ReleaseWithoutDisposeCheck(); // No need to keep this retained.
            return this;
        }

        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise Then(Action<T> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Promise.Internal.PromiseArgResolve<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Promise.Internal.PromiseArgResolve<T, TResult>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Promise.Internal.PromiseArgResolvePromise<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Promise.Internal.PromiseArgResolvePromise<T, TResult>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise ThenDefer(Func<T, Action<Promise.Deferred>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Promise.Internal.PromiseArgResolveDeferred<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise{T}"/> will be controlled by the <see cref="Promise{T}.Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is rejected or canceled, the new <see cref="Promise{T}"/> will be rejected or canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenDefer<TResult>(Func<T, Action<Promise<TResult>.Deferred>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Promise.Internal.PromiseArgResolveDeferred<T, TResult>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved with the resolve value.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Catch(Func<T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseReject<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved with the resolve value.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> Catch<TReject>(Func<TReject, T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseReject<T>.GetOrCreate(Promise.Internal.DelegateArgResult<TReject, T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved with the resolve value.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> Catch<TReject>(Func<T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseReject<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<TReject, T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved with the resolve value.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Catch(Func<Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseRejectPromise<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved with the resolve value.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> Catch<TReject>(Func<TReject, Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseRejectPromise<T>.GetOrCreate(Promise.Internal.DelegateArgResult<TReject, Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved with the resolve value.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> Catch<TReject>(Func<Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseRejectPromise<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<TReject, Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved with the resolve value.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> CatchDefer(Func<Action<Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseRejectDeferred<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved with the resolve value.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> CatchDefer<TReject>(Func<TReject, Action<Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseRejectDeferred<T>.GetOrCreate(Promise.Internal.DelegateArgResult<TReject, Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked.
        /// If it returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is resolved, the new <see cref="Promise"/> will be resolved with the resolve value.
        /// <para/>If this is canceled or rejected for any other reason or no reason, the new <see cref="Promise"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> CatchDefer<TReject>(Func<Action<Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseRejectDeferred<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<TReject, Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Action<T> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveReject0.GetOrCreate(Promise.Internal.DelegateArgVoid<T>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidVoid0.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Action<T> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveReject0.GetOrCreate(Promise.Internal.DelegateArgVoid<T>.GetOrCreate(onResolved), Promise.Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Action<T> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveReject0.GetOrCreate(Promise.Internal.DelegateArgVoid<T>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveReject<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveReject<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved), Promise.Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveReject<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<TReject, TResult>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectPromise0.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<Promise>.GetOrCreate(onRejected), 1);
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
        public Promise Then<TReject>(Func<T, Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectPromise0.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise>.GetOrCreate(onResolved), Promise.Internal.DelegateArgResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Func<T, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectPromise0.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise<TResult>>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<Promise<TResult>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise<TResult>>.GetOrCreate(onResolved), Promise.Internal.DelegateArgResult<TReject, Promise<TResult>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If either delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise<TResult>>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<TReject, Promise<TResult>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenDefer(Func<T, Action<Promise.Deferred>> onResolved, Func<Action<Promise.Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectDeferred0.GetOrCreate(Promise.Internal.DelegateArgResult<T, Action<Promise.Deferred>>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<Action<Promise.Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenDefer<TReject>(Func<T, Action<Promise.Deferred>> onResolved, Func<TReject, Action<Promise.Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectDeferred0.GetOrCreate(Promise.Internal.DelegateArgResult<T, Action<Promise.Deferred>>.GetOrCreate(onResolved), Promise.Internal.DelegateArgResult<TReject, Action<Promise.Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise"/> will be controlled by the <see cref="Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise ThenDefer<TReject>(Func<T, Action<Promise.Deferred>> onResolved, Func<Action<Promise.Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectDeferred0.GetOrCreate(Promise.Internal.DelegateArgResult<T, Action<Promise.Deferred>>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<TReject, Action<Promise.Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value.
        /// If this is rejected with any reason or no reason, <paramref name="onRejected"/> will be invoked.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise{T}"/> will be controlled by the <see cref="Promise{T}.Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenDefer<TResult>(Func<T, Action<Promise<TResult>.Deferred>> onResolved, Func<Action<Promise<TResult>.Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectDeferred<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, Action<Promise<TResult>.Deferred>>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<Action<Promise<TResult>.Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise{T}"/> will be controlled by the <see cref="Promise{T}.Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenDefer<TResult, TReject>(Func<T, Action<Promise<TResult>.Deferred>> onResolved, Func<TReject, Action<Promise<TResult>.Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectDeferred<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, Action<Promise<TResult>.Deferred>>.GetOrCreate(onResolved), Promise.Internal.DelegateArgResult<TReject, Action<Promise<TResult>.Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value.
        /// If this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked.
        /// If either returns successfully, the returned action will be invoked immediately, and the new <see cref="Promise{T}"/> will be controlled by the <see cref="Promise{T}.Deferred"/> that is passed in.
        /// If any delegate throws an <see cref="Exception"/>, the new <see cref="Promise{T"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If this is canceled, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> ThenDefer<TResult, TReject>(Func<T, Action<Promise<TResult>.Deferred>> onResolved, Func<Action<Promise<TResult>.Deferred>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var promise = Promise.Internal.PromiseResolveRejectDeferred<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, Action<Promise<TResult>.Deferred>>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<TReject, Action<Promise<TResult>.Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion
    }
}