// define PROTO_PROMISE_DEBUG_ENABLE to enable debugging options in RELEASE mode. define PROTO_PROMISE_DEBUG_DISABLE to disable debugging options in DEBUG mode.
#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
// define PROTO_PROMISE_PROGRESS_DISABLE to disable progress reports on promises.
// If Progress is enabled, promises use more memory, and it creates an upper bound to the depth of a promise chain (see Config for details).
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

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
    public abstract partial class Promise : IRetainable
    {
        public enum State : byte
        {
            Pending,
            Resolved,
            Rejected,
            Canceled
        }

        internal Promise()
        {
#if PROMISE_DEBUG
            _id = idCounter++;
#endif
        }

        /// <summary>
        /// Get the type of the result of the asynchronous operation. Returns null if this instance is a non-value <see cref="Promise"/>.
        /// </summary>
        public virtual Type ResultType { get { return null; } }

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
                throw new InvalidOperationException("You must call Retain before you call Release!", Internal.GetFormattedStacktrace(1));
            }
            --_userRetainCounter;
#endif
            if (ReleaseWithoutDisposeCheck() == 0)
            {
                // Set retain count to 1 and add to handle queue so this will be disposed asynchronously.
                // This means the Promise object will still be usable until the next handle is ran.
                _retainCounter = 1;
                Internal.AddToHandleQueueFront(this);
            }
        }

        /// <summary>
        /// Add a progress listener. Returns this.
        /// <para/><paramref name="onProgress"/> will be invoked with progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onProgress"/> will stop being invoked.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
        public Promise Progress(Action<float> onProgress, CancelationToken cancelationToken = default(CancelationToken))
        {
            SubscribeProgress(onProgress, cancelationToken);
            return this;
        }

        /// <summary>
        /// Add a cancel callback. Returns this.
        /// <para/>If/when this instance is canceled, <paramref name="onCanceled"/> will be invoked with the cancelation reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onCanceled"/> will not be invoked.
        /// </summary>
        public Promise CatchCancelation(Action<ReasonContainer> onCanceled, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            if (_state == State.Pending | _state == State.Canceled)
            {
                if (cancelationToken.CanBeCanceled)
                {
                    var cancelDelegate = Internal.CancelDelegate<Internal.CancelDelegatePromiseCancel>.GetOrCreate();
                    cancelDelegate.canceler = new Internal.CancelDelegatePromiseCancel(onCanceled, this, cancelationToken.RegisterInternal(cancelDelegate));
                    AddWaiter(cancelDelegate);
                }
                else
                {
                    var cancelDelegate = Internal.CancelDelegate<Internal.CancelDelegatePromise>.GetOrCreate();
                    cancelDelegate.canceler = new Internal.CancelDelegatePromise(onCanceled);
                    AddWaiter(cancelDelegate);
                }
            }
            return this;
        }

        /// <summary>
        /// Add a finally callback. Returns this.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onFinally"/> will be invoked.
        /// </summary>
        public Promise Finally(Action onFinally)
        {
            ValidateOperation(this, 1);
            ValidateArgument(onFinally, "onFinally", 1);

            AddWaiter(InternalProtected.FinallyDelegate.GetOrCreate(onFinally));
            return this;
        }

        #region Resolve Callbacks
        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will be resolved when it returns.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then(Action onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolve<InternalProtected.DelegateVoidVoidCancel>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoidCancel(onResolved, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolve<InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoid(onResolved);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolve<TResult, InternalProtected.DelegateVoidResultCancel<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolve<TResult, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResult<TResult>(onResolved);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise Then(Func<Promise> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolvePromise<InternalProtected.DelegateVoidPromiseCancel>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseCancel(onResolved, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolvePromise<InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromise(onResolved);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Add a resolve callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>If/when this is resolved, <paramref name="onResolved"/> will be invoked, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If it throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// <para/>If/when this is rejected with any reason, the new <see cref="Promise{T}"/> will be rejected with the same reason.
        /// <para/>If/when this is canceled with any reason or no reason, the new <see cref="Promise{T}"/> will be canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onResolved"/> will not be invoked.
        /// </summary>
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolvePromise<TResult, InternalProtected.DelegateVoidPromiseTCancel<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolvePromise<TResult, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseT<TResult>(onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Catch(Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegatePassthroughCancel, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthroughCancel(cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegatePassthrough, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthrough(true);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Catch<TReject>(Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegatePassthroughCancel, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthroughCancel(cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegatePassthrough, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthrough(true);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Catch(Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegatePassthroughCancel, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthroughCancel(cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegatePassthrough, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthrough(true);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Catch<TReject>(Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegatePassthroughCancel, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthroughCancel(cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegatePassthrough, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthrough(true);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then(Action onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateVoidVoidCancel, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoidCancel(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateVoidVoid, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoid(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TReject>(Action onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateVoidVoidCancel, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoidCancel(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateVoidVoid, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoid(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateVoidResultCancel<TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateVoidResult<TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResult<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateVoidResultCancel<TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateVoidResult<TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResult<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromiseCancel, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseCancel(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromise, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromise(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TReject>(Func<Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromiseCancel, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseCancel(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromise, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromise(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseTCancel<TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseT<TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseT<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseTCancel<TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseT<TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseT<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then(Action onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidVoidCancel, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoidCancel(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidVoid, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoid(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TReject>(Action onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidVoidCancel, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoidCancel(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidVoid, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidVoid(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult>(Func<TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidResultCancel<TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidResult<TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResult<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult, TReject>(Func<TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidResultCancel<TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResultCancel<TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidResult<TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidResult<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then(Func<Promise> onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromiseCancel, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseCancel(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromise, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromise(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TReject>(Func<Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromiseCancel, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseCancel(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateVoidPromise, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromise(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult>(Func<Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseTCancel<TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseT<TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseT<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult, TReject>(Func<Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseTCancel<TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseTCancel<TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateVoidPromiseT<TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateVoidPromiseT<TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise ContinueWith(Action<ResultContainer> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {

                var promise = InternalProtected.PromiseContinue<InternalProtected.DelegateContinueVoidVoidCancel>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueVoidVoidCancel(onContinue, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinue<InternalProtected.DelegateContinueVoidVoid>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueVoidVoid(onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, TResult> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinue<TResult, InternalProtected.DelegateContinueVoidResultCancel<TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueVoidResultCancel<TResult>(onContinue, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinue<TResult, InternalProtected.DelegateContinueVoidResult<TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueVoidResult<TResult>(onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith(Func<ResultContainer, Promise> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinuePromise<InternalProtected.DelegateContinueVoidResultCancel<Promise>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueVoidResultCancel<Promise>(onContinue, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinuePromise<InternalProtected.DelegateContinueVoidResult<Promise>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueVoidResult<Promise>(onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, Promise<TResult>> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinuePromise<TResult, InternalProtected.DelegateContinueVoidResultCancel<Promise<TResult>>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueVoidResultCancel<Promise<TResult>>(onContinue, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinuePromise<TResult, InternalProtected.DelegateContinueVoidResult<Promise<TResult>>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueVoidResult<Promise<TResult>>(onContinue);
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
    [System.Diagnostics.DebuggerNonUserCode]
    public abstract partial class Promise<T> : Promise
    {
        internal Promise() { }

        /// <summary>
        /// Get the type of the result of the asynchronous operation. Returns typeof(<typeparamref name="T"/>).
        /// </summary>
        public override sealed Type ResultType { get { return typeof(T); } }

        /// <summary>
        /// Add a progress listener. Returns this.
        /// <para/><paramref name="onProgress"/> will be invoked with progress that is normalized between 0 and 1 from this and all previous waiting promises in the chain.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onProgress"/> will stop being invoked.
        /// </summary>
#if !PROMISE_PROGRESS
        [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", true)]
#endif
        public new Promise<T> Progress(Action<float> onProgress, CancelationToken cancelationToken = default(CancelationToken))
        {
            SubscribeProgress(onProgress, cancelationToken);
            return this;
        }

        /// <summary>
        /// Add a cancel callback. Returns this.
        /// <para/>If/when this instance is canceled, <paramref name="onCanceled"/> will be invoked with the cancelation reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, <paramref name="onCanceled"/> will not be invoked.
        /// </summary>
        public new Promise<T> CatchCancelation(Action<ReasonContainer> onCanceled, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onCanceled, "onCanceled", 1);

            base.CatchCancelation(onCanceled, cancelationToken);
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

            base.Finally(onFinally);
            return this;
        }

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
        public Promise Then(Action<T> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolve<InternalProtected.DelegateArgVoidCancel<T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoidCancel<T>(onResolved, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolve<InternalProtected.DelegateArgVoid<T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoid<T>(onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolve<TResult, InternalProtected.DelegateArgResultCancel<T, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolve<TResult, InternalProtected.DelegateArgResult<T, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResult<T, TResult>(onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then(Func<T, Promise> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolvePromise<InternalProtected.DelegateArgPromiseCancel<T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseCancel<T>(onResolved, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolvePromise<InternalProtected.DelegateArgPromise<T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromise<T>(onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolvePromise<TResult, InternalProtected.DelegateArgPromiseTCancel<T, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolvePromise<TResult, InternalProtected.DelegateArgPromiseT<T, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseT<T, TResult>(onResolved);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<T> Catch(Func<T> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<T, InternalProtected.DelegatePassthroughCancel, InternalProtected.DelegateVoidResult<T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthroughCancel(cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidResult<T>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<T, InternalProtected.DelegatePassthrough, InternalProtected.DelegateVoidResult<T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthrough(true);
                promise.rejecter = new InternalProtected.DelegateVoidResult<T>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<T> Catch<TReject>(Func<TReject, T> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<T, InternalProtected.DelegatePassthroughCancel, InternalProtected.DelegateArgResult<TReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthroughCancel(cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, T>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<T, InternalProtected.DelegatePassthrough, InternalProtected.DelegateArgResult<TReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthrough(true);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, T>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<T> Catch(Func<Promise<T>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<T, InternalProtected.DelegatePassthroughCancel, InternalProtected.DelegateVoidPromiseT<T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthroughCancel(cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<T>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<T, InternalProtected.DelegatePassthrough, InternalProtected.DelegateVoidPromiseT<T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthrough(true);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<T>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<T> Catch<TReject>(Func<TReject, Promise<T>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<T, InternalProtected.DelegatePassthroughCancel, InternalProtected.DelegateArgPromiseT<TReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthroughCancel(cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, T>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<T, InternalProtected.DelegatePassthrough, InternalProtected.DelegateArgPromiseT<TReject, T>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegatePassthrough(true);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, T>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then(Action<T> onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateArgVoidCancel<T>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoidCancel<T>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateArgVoid<T>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoid<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TReject>(Action<T> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateArgVoidCancel<T>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoidCancel<T>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<InternalProtected.DelegateArgVoid<T>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoid<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateArgResultCancel<T, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateArgResult<T, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResult<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateArgResultCancel<T, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveReject<TResult, InternalProtected.DelegateArgResult<T, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResult<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromiseCancel<T>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseCancel<T>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromise<T>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromise<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TReject>(Func<T, Promise> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromiseCancel<T>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseCancel<T>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromise<T>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromise<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseTCancel<T, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseT<T, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseT<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseTCancel<T, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseT<T, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseT<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then(Action<T> onResolved, Func<Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgVoidCancel<T>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoidCancel<T>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgVoid<T>, InternalProtected.DelegateVoidPromise>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoid<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromise(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TReject>(Action<T> onResolved, Func<TReject, Promise> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgVoidCancel<T>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoidCancel<T>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgVoid<T>, InternalProtected.DelegateArgPromise<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgVoid<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromise<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgResultCancel<T, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgResult<T, TResult>, InternalProtected.DelegateVoidPromiseT<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResult<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidPromiseT<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, Promise<TResult>> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgResultCancel<T, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResultCancel<T, TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgResult<T, TResult>, InternalProtected.DelegateArgPromiseT<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgResult<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgPromiseT<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then(Func<T, Promise> onResolved, Action onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromiseCancel<T>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseCancel<T>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromise<T>, InternalProtected.DelegateVoidVoid>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromise<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidVoid(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise Then<TReject>(Func<T, Promise> onResolved, Action<TReject> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromiseCancel<T>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseCancel<T>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<InternalProtected.DelegateArgPromise<T>, InternalProtected.DelegateArgVoid<TReject>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromise<T>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgVoid<TReject>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseTCancel<T, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseT<T, TResult>, InternalProtected.DelegateVoidResult<TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseT<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateVoidResult<TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, TResult> onRejected, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onResolved, "onResolved", 1);
            ValidateArgument(onRejected, "onRejected", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseTCancel<T, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseTCancel<T, TResult>(onResolved, cancelationToken.RegisterInternal(promise));
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseResolveRejectPromise<TResult, InternalProtected.DelegateArgPromiseT<T, TResult>, InternalProtected.DelegateArgResult<TReject, TResult>>.GetOrCreate();
                promise.resolver = new InternalProtected.DelegateArgPromiseT<T, TResult>(onResolved);
                promise.rejecter = new InternalProtected.DelegateArgResult<TReject, TResult>(onRejected);
                HookupNewPromise(promise);
                return promise;
            }
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
        public Promise ContinueWith(Action<ResultContainer> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinue<InternalProtected.DelegateContinueArgVoidCancel<T>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueArgVoidCancel<T>(onContinue, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinue<InternalProtected.DelegateContinueArgVoid<T>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueArgVoid<T>(onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will be resolved with the returned value.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, TResult> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinue<TResult, InternalProtected.DelegateContinueArgResultCancel<T, TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueArgResultCancel<T, TResult>(onContinue, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinue<TResult, InternalProtected.DelegateContinueArgResult<T, TResult>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueArgResult<T, TResult>(onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }


        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise"/> will adopt the state of the returned <see cref="Promise"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise ContinueWith(Func<ResultContainer, Promise> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinuePromise<InternalProtected.DelegateContinueArgResultCancel<T, Promise>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueArgResultCancel<T, Promise>(onContinue, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinuePromise<InternalProtected.DelegateContinueArgResult<T, Promise>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueArgResult<T, Promise>(onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }

        /// <summary>
        /// Add a continuation callback. Returns a new <see cref="Promise{T}"/>.
        /// <para/>When this is resolved, rejected, or canceled, <paramref name="onContinue"/> will be invoked with the <see cref="ResultContainer"/>, and the new <see cref="Promise{T}"/> will adopt the state of the returned <see cref="Promise{T}"/>.
        /// If if throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>, unless it is a Special Exception (see README).
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while this is pending, the new <see cref="Promise{T}"/> will be canceled with its reason, and <paramref name="onContinue"/> will not be invoked.
        /// </summary>
        public Promise<TResult> ContinueWith<TResult>(Func<ResultContainer, Promise<TResult>> onContinue, CancelationToken cancelationToken = default(CancelationToken))
        {
            ValidateOperation(this, 1);
            ValidateArgument(onContinue, "onContinue", 1);

            if (cancelationToken.CanBeCanceled)
            {
                var promise = InternalProtected.PromiseContinuePromise<TResult, InternalProtected.DelegateContinueArgResultCancel<T, Promise<TResult>>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueArgResultCancel<T, Promise<TResult>>(onContinue, cancelationToken.RegisterInternal(promise));
                MaybeHookupNewPromise(promise);
                return promise;
            }
            else
            {
                var promise = InternalProtected.PromiseContinuePromise<TResult, InternalProtected.DelegateContinueArgResult<T, Promise<TResult>>>.GetOrCreate();
                promise.continuer = new InternalProtected.DelegateContinueArgResult<T, Promise<TResult>>(onContinue);
                HookupNewPromise(promise);
                return promise;
            }
        }
        #endregion
    }
}