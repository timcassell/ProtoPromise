using System;

namespace Proto.Promises
{
    public abstract partial class Promise : ICancelableAny, IRetainable, IPotentialCancelation
    {
		public void Retain()
		{
            ValidateOperation(this);
#if DEBUG
			checked
#endif
			{
                ++_retainCounter;
			}
		}

		public void Release()
        {
            ValidateOperation(this);
#if DEBUG
            checked
#endif
            {
                if (--_retainCounter == 0 & _state != State.Pending & !_handling)
                {
                    // Place in the handle queue so it can be repooled.
                    AddToHandleQueue(this);
                }
            }
        }

        public YieldInstruction ToYieldInstruction()
        {
            var yield = YieldInstruction.GetOrCreate();
            AddWaiter(yield);
            return yield;
        }

        public Promise Finally(Action onFinally)
		{
            ValidateOperation(this);
            ValidateArgument(onFinally, "onFinally");

            AddWaiter(Internal.FinallyDelegate.GetOrCreate(onFinally, this, 1));
            return this;
		}

#region Resolve Callbacks
		public Promise Then(Action onResolved)
		{
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

            var promise = Internal.PromiseVoidResolve.GetOrCreate(onResolved, 1);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

			var promise = Internal.PromiseVoidResolve<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

            var promise = Internal.PromiseVoidResolvePromise.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

            var promise = Internal.PromiseVoidResolvePromise<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Action<Deferred>> onResolved)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

            var promise = Internal.PromiseVoidResolveDeferred.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Action<Promise<T>.Deferred>> onResolved)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

            var promise = Internal.PromiseVoidResolveDeferred<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}
#endregion

#region Reject Callbacks
		public Promise Catch(Action onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseReject.GetOrCreate(Internal.DelegateVoidVoid.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TReject>(Action<TReject> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseReject.GetOrCreate(Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
        }

        public Promise Catch<TReject>(Action onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseReject.GetOrCreate(Internal.DelegateVoidVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        public Promise Catch(Func<Promise> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseRejectPromise.GetOrCreate(Internal.DelegateVoidResult<Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TReject>(Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseRejectPromise.GetOrCreate(Internal.DelegateArgResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
        }

        public Promise Catch<TReject>(Func<Promise> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseRejectPromise.GetOrCreate(Internal.DelegateVoidResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        public Promise Catch(Func<Action<Deferred>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseRejectDeferred.GetOrCreate(Internal.DelegateVoidResult<Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TReject>(Func<TReject, Action<Deferred>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseRejectDeferred.GetOrCreate(Internal.DelegateArgResult<TReject, Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
        }

        public Promise Catch<TReject>(Func<Action<Deferred>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseRejectDeferred.GetOrCreate(Internal.DelegateVoidResult<TReject, Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }
#endregion

#region Resolve or Reject Callbacks
        public Promise Then(Action onResolved, Action onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

			var promise = Internal.PromiseResolveReject.GetOrCreate(Internal.DelegateVoidVoid.GetOrCreate(onResolved), Internal.DelegateVoidVoid.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TReject>(Action onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveReject.GetOrCreate(Internal.DelegateVoidVoid.GetOrCreate(onResolved), Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
        }

        public Promise Then<TReject>(Action onResolved, Action onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveReject.GetOrCreate(Internal.DelegateVoidVoid.GetOrCreate(onResolved), Internal.DelegateVoidVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        public Promise<T> Then<T>(Func<T> onResolved, Func<T> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(Internal.DelegateVoidResult<T>.GetOrCreate(onResolved), Internal.DelegateVoidResult<T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TReject>(Func<T> onResolved, Func<TReject, T> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(Internal.DelegateVoidResult<T>.GetOrCreate(onResolved), Internal.DelegateArgResult<TReject, T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
        }

        public Promise<T> Then<T, TReject>(Func<T> onResolved, Func<T> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveReject<T>.GetOrCreate(Internal.DelegateVoidResult<T>.GetOrCreate(onResolved), Internal.DelegateVoidResult<TReject, T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveRejectPromise.GetOrCreate(Internal.DelegateVoidResult<Promise>.GetOrCreate(onResolved), Internal.DelegateVoidResult<Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TReject>(Func<Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveRejectPromise.GetOrCreate(Internal.DelegateVoidResult<Promise>.GetOrCreate(onResolved), Internal.DelegateArgResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
        }

        public Promise Then<TReject>(Func<Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveRejectPromise.GetOrCreate(Internal.DelegateVoidResult<Promise>.GetOrCreate(onResolved), Internal.DelegateVoidResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        public Promise<T> Then<T>(Func<Promise<T>> onResolved, Func<Promise<T>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(Internal.DelegateVoidResult<Promise<T>>.GetOrCreate(onResolved), Internal.DelegateVoidResult<Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TReject>(Func<Promise<T>> onResolved, Func<TReject, Promise<T>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(Internal.DelegateVoidResult<Promise<T>>.GetOrCreate(onResolved), Internal.DelegateArgResult<TReject, Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
        }

        public Promise<T> Then<T, TReject>(Func<Promise<T>> onResolved, Func<Promise<T>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(Internal.DelegateVoidResult<Promise<T>>.GetOrCreate(onResolved), Internal.DelegateVoidResult<TReject, Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }
#endregion

#region Complete Callbacks
        /// <summary>
        /// Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
        /// onResolvedOrRejected is invoked when this promise is resolved or rejected. It does not get invoked if this promise is canceled.
        /// </summary>
        public Promise Complete(Action onResolvedOrRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected");

            var promise = Internal.PromiseComplete.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
			return promise;
		}

		/// <summary>
		/// Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
		/// onResolvedOrRejected is invoked when this promise is resolved or rejected. It does not get invoked if this promise is canceled.
		/// </summary>
		public Promise<T> Complete<T>(Func<T> onResolvedOrRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected");

            var promise = Internal.PromiseComplete<T>.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
			return promise;
		}

		/// <summary>
		/// Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
		/// onResolvedOrRejected is invoked when this promise is resolved or rejected. It does not get invoked if this promise is canceled.
		/// The returned promise will wait for the promise returned by onResolvedOrRejected to be resolved, rejected, or canceled.
		/// </summary>
		public Promise Complete(Func<Promise> onResolvedOrRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected");

            var promise = Internal.PromiseCompletePromise.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
			return promise;
		}

		/// <summary>
		/// Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
		/// onResolvedOrRejected is invoked when this promise is resolved or rejected. It does not get invoked if this promise is canceled.
		/// The returned promise will wait for the promise returned by onResolvedOrRejected to be resolved, rejected, or canceled.
		/// </summary>
		public Promise<T> Complete<T>(Func<Promise<T>> onResolvedOrRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected");

            var promise = Internal.PromiseCompletePromise<T>.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
			return promise;
		}

		/// <summary>
		/// Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
		/// onResolvedOrRejected is invoked when this promise is resolved or rejected. It does not get invoked if this promise is canceled.
		/// The Action returned by onResolvedOrRejected will be immediately invoked with a Deferred object.
		/// The returned promise will wait for that Deferred object to be resolved, rejected, or canceled.
		/// </summary>
		public Promise Complete(Func<Action<Deferred>> onResolvedOrRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected");

            var promise = Internal.PromiseCompleteDeferred.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
			return promise;
		}

		/// <summary>
		/// Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
		/// onResolvedOrRejected is invoked when this promise is resolved or rejected. It does not get invoked if this promise is canceled.
		/// The Action returned by onResolvedOrRejected will be immediately invoked with a Deferred object.
		/// The returned promise will wait for that Deferred object to be resolved, rejected, or canceled.
		/// </summary>
		public Promise<T> Complete<T>(Func<Action<Promise<T>.Deferred>> onResolvedOrRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolvedOrRejected, "onResolvedOrRejected");

            var promise = Internal.PromiseCompleteDeferred<T>.GetOrCreate(onResolvedOrRejected, 1);
            HookupNewPromise(promise);
			return promise;
		}
#endregion
	}

	public abstract partial class Promise<T> : Promise
    {
        public new Promise<T> Finally(Action onFinally)
        {
            ValidateOperation(this);
            ValidateArgument(onFinally, "onFinally");

            AddWaiter(Promise.Internal.FinallyDelegate.GetOrCreate(onFinally, this, 1));
            return this;
        }

#region Resolve Callbacks
		public Promise Then(Action<T> onResolved)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

            var promise = Promise.Internal.PromiseArgResolve<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

            var promise = Promise.Internal.PromiseArgResolve<T, TResult>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

            var promise = Promise.Internal.PromiseArgResolvePromise<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
		{
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

			var promise = Promise.Internal.PromiseArgResolvePromise<T, TResult>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Action<Promise.Deferred>> onResolved)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

            var promise = Promise.Internal.PromiseArgResolveDeferred<T>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Action<Promise<TResult>.Deferred>> onResolved)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");

            var promise = Promise.Internal.PromiseArgResolveDeferred<T, TResult>.GetOrCreate(onResolved, 1);
            HookupNewPromise(promise);
			return promise;
		}
#endregion

#region Reject Callbacks
		public Promise<T> Catch(Func<T> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseReject<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TReject>(Func<TReject, T> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseReject<T>.GetOrCreate(Promise.Internal.DelegateArgResult<TReject, T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
        }

        public Promise<T> Catch<TReject>(Func<T> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseReject<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<TReject, T>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        public Promise<T> Catch(Func<Promise<T>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseRejectPromise<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TReject>(Func<TReject, Promise<T>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseRejectPromise<T>.GetOrCreate(Promise.Internal.DelegateArgResult<TReject, Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
        }

        public Promise<T> Catch<TReject>(Func<Promise<T>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseRejectPromise<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<TReject, Promise<T>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        public Promise<T> Catch(Func<Action<Deferred>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseRejectDeferred<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TReject>(Func<TReject, Action<Deferred>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseRejectDeferred<T>.GetOrCreate(Promise.Internal.DelegateArgResult<TReject, Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}
        
        public Promise<T> Catch<TReject>(Func<Action<Deferred>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseRejectDeferred<T>.GetOrCreate(Promise.Internal.DelegateVoidResult<TReject, Action<Deferred>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }
#endregion

#region Reject or Reject Callbacks
		public Promise Then(Action<T> onResolved, Action onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveReject.GetOrCreate(Promise.Internal.DelegateArgVoid<T>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidVoid.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TReject>(Action<T> onResolved, Action<TReject> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveReject.GetOrCreate(Promise.Internal.DelegateArgVoid<T>.GetOrCreate(onResolved), Promise.Internal.DelegateArgVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

        public Promise Then<TReject>(Action<T> onResolved, Action onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveReject.GetOrCreate(Promise.Internal.DelegateArgVoid<T>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidVoid<TReject>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveReject<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<TResult>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, TResult> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveReject<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved), Promise.Internal.DelegateArgResult<TReject, TResult>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

        public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TResult> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveReject<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, TResult>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<TReject, TResult>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveRejectPromise.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TReject>(Func<T, Promise> onResolved, Func<TReject, Promise> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveRejectPromise.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise>.GetOrCreate(onResolved), Promise.Internal.DelegateArgResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

        public Promise Then<TReject>(Func<T, Promise> onResolved, Func<Promise> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveRejectPromise.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<TReject, Promise>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }

        public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise<TResult>>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<Promise<TResult>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise<TResult>>.GetOrCreate(onResolved), Promise.Internal.DelegateArgResult<TReject, Promise<TResult>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
			return promise;
		}
        
        public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
        {
            ValidateOperation(this);
            ValidateArgument(onResolved, "onResolved");
            ValidateArgument(onRejected, "onRejected");

            var promise = Promise.Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(Promise.Internal.DelegateArgResult<T, Promise<TResult>>.GetOrCreate(onResolved), Promise.Internal.DelegateVoidResult<TReject, Promise<TResult>>.GetOrCreate(onRejected), 1);
            HookupNewPromise(promise);
            return promise;
        }
#endregion
	}
}