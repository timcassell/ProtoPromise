// define PROTO_PROMISE_DEBUG_ENABLE to enable debugging options in RELEASE mode. define PROTO_PROMISE_DEBUG_DISABLE to disable debugging options in DEBUG mode.
#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
// define PROTO_PROMISE_CANCEL_DISABLE to disable cancelations on promises.
// If Cancelations are enabled, it breaks the Promises/A+ spec "2.1. Promise States", but allows breaking promise chains. Execution is also a little slower.
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
// define PROTO_PROMISE_PROGRESS_DISABLE to disable progress reports on promises.
// If Progress is enabled, promises use more memory, and it creates an upper bound to the depth of a promise chain (see Config for details).
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
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
    [System.Diagnostics.DebuggerNonUserCode]
    public abstract partial class Promise : ICancelableAny, IRetainable
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
            // Make sure Retain doesn't overflow the ushort. 4 retains are reserved for internal use.
            if (_userRetainCounter == ushort.MaxValue - 4)
            {
                throw new OverflowException();
            }
            ++_userRetainCounter;
#endif
            RetainInternal();
        }

        /// <summary>
        /// Release this instance. Allows uncaught rejections to be thrown and prevents adding more callbacks when this is settled (if <see cref="Release"/> has been called for all <see cref="Retain"/> calls).
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
            if (ReleaseWithoutDisposeCheck() == 0)
            {
                // Set retain count to 1 and add to handle queue so this will be disposed asynchronously.
                // This means the Promise object will still be usable until the next handle is ran.
                _retainCounter = 1;
                AddToHandleQueueFront(this);
            }
        }

        /// <summary>
        /// Returns a new <see cref="YieldInstruction"/> that can be yielded in a coroutine to wait until this is settled.
        /// </summary>
        public YieldInstruction ToYieldInstruction()
        {
            ValidateOperation(this, 1);

            var yield = Internal.YieldInstructionVoid.GetOrCreate(this);
            AddWaiter(yield);
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
        /// Add a progress listener. Returns this.
        /// <para/><paramref name="onProgress"/> will be invoked with progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
        public Promise Progress(Action<float> onProgress)
        {
            SubscribeProgress(onProgress, 1);
            return this;
        }

        /// <summary>
        /// Add a cancel callback. Returns this.
        /// <para/>If/when this instance is canceled, <paramref name="onCanceled"/> will be invoked with the cancelation reason.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public Promise CatchCancelation(Action<ReasonContainer> onCanceled)
        {
            ValidateCancel(1);
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                AddWaiter(Internal.CancelDelegate.GetOrCreate(onCanceled, 1));
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

            CancelDirectIfPending();
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

            CancelDirectIfPending(reason);
        }

        /// <summary>
        /// Add a finally callback. Returns this.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked.
        /// </summary>
        public Promise Finally(Action onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            AddWaiter(Internal.FinallyDelegate.GetOrCreate(onFinally, 1));
            return this;
        }

        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
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
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseVoidResolve<TResult>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
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
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseVoidResolvePromise<TResult>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise Catch(Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Catch<TReject>(Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Catch(Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise"/> will be resolved.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Catch<TReject>(Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Action onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Action onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Func<Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Action onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Action onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidVoid.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Func<Promise> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Func<Promise> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromise.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// </summary>
        public Promise ContinueWith(Action<ResultContainer> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueVoidVoid.GetOrCreate(onContinue);
            var promise = Internal.PromiseContinue0.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved with the returned value.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, TResult> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueVoidResult<TResult>.GetOrCreate(onContinue);
            var promise = Internal.PromiseContinue<TResult>.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// </summary>
        public Promise ContinueWith(Func<ResultContainer, Promise> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueVoidResult<Promise>.GetOrCreate(onContinue);
            var promise = Internal.PromiseContinuePromise0.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, Promise<TResult>> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueVoidResult<Promise<TResult>>.GetOrCreate(onContinue);
            var promise = Internal.PromiseContinuePromise<TResult>.GetOrCreate(del, 1);
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
    [System.Diagnostics.DebuggerNonUserCode]
    public abstract partial class Promise<T> : Promise
    {
        /// <summary>
        /// Returns a new <see cref="Promise{T}.YieldInstruction"/> that can be yielded in a coroutine to wait until this is settled.
        /// </summary>
        public new YieldInstruction ToYieldInstruction()
        {
            ValidateOperation(this, 1);
            
            var yield = Internal.YieldInstruction<T>.GetOrCreate(this);
            AddWaiter(yield);
            return yield;
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

            var promise = Internal.DuplicatePromise<T>.GetOrCreate(1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a progress listener. Returns this.
        /// <para/><paramref name="onProgress"/> will be invoked with progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
        public new Promise<T> Progress(Action<float> onProgress)
        {
            SubscribeProgress(onProgress, 1);
            return this;
        }

        /// <summary>
        /// Add a cancel callback. Returns this.
        /// <para/>If/when this instance is canceled, <paramref name="onCanceled"/> will be invoked with the cancelation reason.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public new Promise<T> CatchCancelation(Action<ReasonContainer> onCanceled)
        {
            ValidateCancel(1);
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                AddWaiter(Internal.CancelDelegate.GetOrCreate(onCanceled, 1));
            }
            return this;
        }

        /// <summary>
        /// Add a finally callback. Returns this.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked.
        /// </summary>
        public new Promise<T> Finally(Action onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            AddWaiter(Internal.FinallyDelegate.GetOrCreate(onFinally, 1));
            return this;
        }

        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Action<T> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseArgResolve<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseArgResolve<T, TResult>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseArgResolvePromise<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            var promise = Internal.PromiseArgResolvePromise<T, TResult>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Reject Callbacks
        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Catch(Func<T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateVoidResult<T>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Catch<TReject>(Func<TReject, T> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateArgResult<TReject, T>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<T> Catch(Func<Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateVoidPromiseT<T>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is resolved, the new <see cref="Promise{T}"/> will be resolved with the resolve value.
        /// <para/>If/when this is canceled or rejected with any other reason or no reason, the new <see cref="Promise{T}"/> will be canceled or rejected with the same reason.
        /// </summary>
        public Promise<T> Catch<TReject>(Func<TReject, Promise<T>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegatePassthrough.GetOrCreate();
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, T>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Resolve or Reject Callbacks
        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Action<T> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Action<T> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Func<T, Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Action<T> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidPromise.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Action<T> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgVoid<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgPromise<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidPromiseT<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgPromiseT<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then(Func<T, Promise> onResolved, Action onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidVoid.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// </summary>
        public Promise Then<TReject>(Func<T, Promise> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromise<T>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise0.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason, <paramref name="onRejected"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a resolve and a reject callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked with the resolve value, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// <para/>If/when this is rejected with any reason that is convertible to <typeparamref name="TReject"/>, <paramref name="onRejected"/> will be invoked with that reason, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// If this is rejected with any other reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// </summary>
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            var resolveDelegate = Internal.DelegateArgPromiseT<T, TResult>.GetOrCreate(onResolved);
            var rejectDelegate = Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected);
            var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(resolveDelegate, rejectDelegate, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion

        #region Continue Callbacks
        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved when it returns.
        /// </summary>
        public Promise ContinueWith(Action<ResultContainer> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueArgVoid<T>.GetOrCreate(onContinue);
            var promise = Internal.PromiseContinue0.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will be resolved with the returned value.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, TResult> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueArgResult<T, TResult>.GetOrCreate(onContinue);
            var promise = Internal.PromiseContinue<TResult>.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }


        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// </summary>
        public Promise ContinueWith(Func<ResultContainer, Promise> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueArgResult<T, Promise>.GetOrCreate(onContinue);
            var promise = Internal.PromiseContinuePromise0.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, Promise<TResult>> onContinue)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            var del = Internal.DelegateContinueArgResult<T, Promise<TResult>>.GetOrCreate(onContinue);
            var promise = Internal.PromiseContinuePromise<TResult>.GetOrCreate(del, 1);
            HookupNewPromise(promise);
            return promise;
        }
        #endregion
    }
}