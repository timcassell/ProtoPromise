using System;

namespace ProtoPromise
{
	public enum PromiseState : sbyte
	{
		/// <summary>
		/// Promise is waiting to be resolved or rejected.
		/// </summary>
		Pending,
		/// <summary>
		/// Promise was resolved.
		/// </summary>
		Resolved,
		/// <summary>
		/// Promise was rejected.
		/// </summary>
		Rejected,
		/// <summary>
		/// Promise was canceled.
		/// </summary>
		Canceled // This violates Promises/A+ API, but I felt its usefulness outweighs API adherence.
	}

	// If using Unity prior to 5.3, remove "UnityEngine.CustomYieldInstruction". Instead, you can wait for a promise to complete in a coroutine this way:
	// do { yield return null; } while (promise.State == PromiseState.Pending);
	public abstract partial class Promise : UnityEngine.CustomYieldInstruction, ICancelableAny, IRetainable
	{
		public PromiseState State { get { return _state; } }

		override public bool keepWaiting
		{
			get
			{
				return _state == PromiseState.Pending;
			}
		}

		public void Retain()
		{

			uint retain;
			retains.TryGetValue(this, out retain);
#if DEBUG
			checked
#endif
			{
				++retain;
			}
			retains[this] = retain;
		}

		public void Release()
		{
			uint retain = retains[this];
			--retain;
			if (retain > 0u)
			{
				retains[this] = retain;
			}
			else
			{
				retains.Remove(this);
				if (_state != PromiseState.Pending && _notHandling)
				{
					// TODO: Continue handling so that the loop can add this back to the pool.
				}
			}
		}

		// TODO: Check every method call in DEBUG mode to see if the promise is marked done. Throw InvalidOperationException if it is done.

		/// <summary>
		/// Prevent Finally promises from resolving and uncaught rejections getting reported. This allows to keep chaining from this promise until <see cref="Done"/> is called.
		/// Calls to <see cref="ContinueUsing"/> should always be paired with calls to <see cref="Done"/>.
		/// </summary>
		public void ContinueUsing()
		{
#if DEBUG
			checked
#endif
			{
				++_pendingCount;
			}
		}

		/// <summary>
		/// Allow Finally promises to resolve and uncaught rejections to get reported. Further promise chaining from this is no longer allowed.
		/// This should always be called some time after a call to <see cref="ContinueUsing"/>, never before.
		/// </summary>
		public void Done()
		{
#if DEBUG
			checked
#endif
			{
				--_pendingCount;
			}
			// TODO: mark done in the continuehandling execution.
			//if (_done)
			//{
			//	return;
			//}

			//_done = true;
			if (_pendingCount == 0 & _state != PromiseState.Pending)
			{
				// TODO: Continue handling so that the loop can add this back to the pool.
				OnComplete();
			}
		}

		public Promise Canceled(Action onCanceled)
		{
			ValidateCancel();

			switch (_state)
			{
				case PromiseState.Pending:
				{
					HookUpCancelCallback(Internal.DelegateVoid.GetOrCreate(onCanceled));
					break;
				}
				case PromiseState.Canceled:
				{
					if (HookUpCancelCallback(Internal.DelegateVoid.GetOrCreate(onCanceled)))
					{
						HandleCancel();
					}
					break;
				}
			}
			return this;
		}

		public Promise Canceled<TCancel>(Action<TCancel> onCanceled)
		{
			ValidateCancel();

			switch (_state)
			{
				case PromiseState.Pending:
				{
					HookUpCancelCallback(Internal.DelegateArg<TCancel>.GetOrCreate(onCanceled));
					break;
				}
				case PromiseState.Canceled:
				{
					if (HookUpCancelCallback(Internal.DelegateArg<TCancel>.GetOrCreate(onCanceled)))
					{
						HandleCancel();
					}
					break;
				}
			}
			return this;
		}

		/// <summary>
		/// Cancels this promise and all promises that have been chained from this.
		/// Does nothing if this promise isn't pending.
		/// </summary>
		public void Cancel()
		{
			ValidateCancel();

			if (_state != PromiseState.Pending)
			{
				return;
			}

			// TODO: Cancel finally promise

			_rejectedOrCanceledValue = Internal.CancelVoid.GetOrCreate();
			_rejectedOrCanceledValue.Retain();

			HandleCancel();
			ContinueCanceling(this);
		}

		/// <summary>
		/// Cancels this promise and all promises that have been chained from this with the provided cancel reason.
		/// Does nothing if this promise isn't pending.
		/// </summary>
		public void Cancel<TCancel>(TCancel reason)
		{
			ValidateCancel();

			if (_state != PromiseState.Pending)
			{
				return;
			}

			_rejectedOrCanceledValue = Internal.ValueContainer<TCancel>.GetOrCreate(reason);
			_rejectedOrCanceledValue.Retain();

			HandleCancel();
			ContinueCanceling(this);
		}

		public Promise Progress(Action<float> onProgress)
		{
			ProgressPrivate(onProgress);
			return this;
		}

		public Promise Finally()
		{
			// TODO: Validate
			return GetOrCreateFinally().ThenDuplicate();
		}

		public Promise Finally(Action onFinally)
		{
			// TODO: Validate
			return GetOrCreateFinally().Then(onFinally);
		}

		public Promise ThenDuplicate()
		{
			var promise = GetDuplicate();
			HookupNewPromise(promise);
			return promise;
		}

#region Resolve Callbacks
		public Promise Then(Action onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseVoidResolve.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseVoidResolve<T>.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseVoidResolvePromise.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseVoidResolvePromise<T>.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Action<Deferred>> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseVoidResolveDeferred.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Action<Promise<T>.Deferred>> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseVoidResolveDeferred<T>.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}
#endregion

#region Reject Callbacks
		// TODO: add filters
		public Promise Catch(Action onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseReject.GetOrCreate(Internal.DelegateVoid.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TReject>(Action<TReject> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseReject.GetOrCreate(Internal.DelegateArg<TReject>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch(Func<Promise> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseRejectPromise.GetOrCreate(Internal.DelegateVoid<Promise>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TReject>(Func<TReject, Promise> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseRejectPromise.GetOrCreate(Internal.DelegateArg<TReject, Promise>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch(Func<Action<Deferred>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseRejectDeferred.GetOrCreate(Internal.DelegateVoid<Action<Deferred>>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TReject>(Func<TReject, Action<Deferred>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseRejectDeferred.GetOrCreate(Internal.DelegateArg<TReject, Action<Deferred>>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}
#endregion

#region Resolve or Reject Callbacks
		public Promise Then(Action onResolved, Action onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveReject.GetOrCreate(Internal.DelegateVoid.GetOrCreate(onResolved), Internal.DelegateVoid.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TReject>(Action onResolved, Action<TReject> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveReject.GetOrCreate(Internal.DelegateVoid.GetOrCreate(onResolved), Internal.DelegateArg<TReject>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved, Func<T> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveReject<T>.GetOrCreate(Internal.DelegateVoid<T>.GetOrCreate(onResolved), Internal.DelegateVoid<T>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TReject>(Func<T> onResolved, Func<TReject, T> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveReject<T>.GetOrCreate(Internal.DelegateVoid<T>.GetOrCreate(onResolved), Internal.DelegateArg<TReject, T>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveRejectPromise.GetOrCreate(Internal.DelegateVoid<Promise>.GetOrCreate(onResolved), Internal.DelegateVoid<Promise>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TReject>(Func<Promise> onResolved, Func<TReject, Promise> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveRejectPromise.GetOrCreate(Internal.DelegateVoid<Promise>.GetOrCreate(onResolved), Internal.DelegateArg<TReject, Promise>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved, Func<Promise<T>> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(Internal.DelegateVoid<Promise<T>>.GetOrCreate(onResolved), Internal.DelegateVoid<Promise<T>>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TReject>(Func<Promise<T>> onResolved, Func<TReject, Promise<T>> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveRejectPromise<T>.GetOrCreate(Internal.DelegateVoid<Promise<T>>.GetOrCreate(onResolved), Internal.DelegateArg<TReject, Promise<T>>.GetOrCreate(onRejected));
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
			// TODO: wrap in "#if DEBUG"
			if (onResolvedOrRejected == null)
			{
				throw new ArgumentNullException("onResolvedOrRejected");
			}

			var promise = Internal.PromiseComplete.GetOrCreate(onResolvedOrRejected);
			HookupNewPromise(promise);
			return promise;
		}

		/// <summary>
		/// Functionally the same as Then(onResolvedOrRejected, onResolvedOrRejected), but more efficient.
		/// onResolvedOrRejected is invoked when this promise is resolved or rejected. It does not get invoked if this promise is canceled.
		/// </summary>
		public Promise<T> Complete<T>(Func<T> onResolvedOrRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolvedOrRejected == null)
			{
				throw new ArgumentNullException("onResolvedOrRejected");
			}

			var promise = Internal.PromiseComplete<T>.GetOrCreate(onResolvedOrRejected);
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
			// TODO: wrap in "#if DEBUG"
			if (onResolvedOrRejected == null)
			{
				throw new ArgumentNullException("onResolvedOrRejected");
			}

			var promise = Internal.PromiseCompletePromise.GetOrCreate(onResolvedOrRejected);
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
			// TODO: wrap in "#if DEBUG"
			if (onResolvedOrRejected == null)
			{
				throw new ArgumentNullException("onResolvedOrRejected");
			}

			var promise = Internal.PromiseCompletePromise<T>.GetOrCreate(onResolvedOrRejected);
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
			// TODO: wrap in "#if DEBUG"
			if (onResolvedOrRejected == null)
			{
				throw new ArgumentNullException("onResolvedOrRejected");
			}

			var promise = Internal.PromiseCompleteDeferred.GetOrCreate(onResolvedOrRejected);
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
			// TODO: wrap in "#if DEBUG"
			if (onResolvedOrRejected == null)
			{
				throw new ArgumentNullException("onResolvedOrRejected");
			}

			var promise = Internal.PromiseCompleteDeferred<T>.GetOrCreate(onResolvedOrRejected);
			HookupNewPromise(promise);
			return promise;
		}
#endregion

		// TODO: Allow onResolved and onRejected to return void, Promise, or Action<Deferred> independently, or T, Promise<T>, or Action<Promise<T>.Deferred> independently.
		//public Promise Then<TReject>(Action onResolved, Func<Promise> onRejected)
		//{
		//	
		//}
	}

	public abstract partial class Promise<T> : Promise
	{
		public new Promise<T> Canceled(Action onCanceled)
		{
			base.Canceled(onCanceled);
			return this;
		}

		public new Promise<T> Canceled<TCancel>(Action<TCancel> onCanceled)
		{
			base.Canceled(onCanceled);
			return this;
		}

		public new Promise<T> Progress(Action<float> onProgress)
		{
			base.Progress(onProgress);
			return this;
		}

		public new Promise<T> ThenDuplicate()
		{
			var promise = Internal.LitePromise<T>.GetOrCreate();
			HookupNewPromise(promise);
			return promise;
		}

#region Resolve Callbacks
		public Promise Then(Action<T> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseArgResolve<T>.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseArgResolve<T, TResult>.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseArgResolvePromise<T>.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseArgResolvePromise<T, TResult>.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Action<Promise.Deferred>> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseArgResolveDeferred<T>.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Action<Promise<TResult>.Deferred>> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			var promise = Internal.PromiseArgResolveDeferred<T, TResult>.GetOrCreate(onResolved);
			HookupNewPromise(promise);
			return promise;
		}
#endregion

#region Reject Callbacks
		// TODO: Add filters.
		public Promise<T> Catch(Func<T> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseReject<T>.GetOrCreate(Internal.DelegateVoid<T>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TReject>(Func<TReject, T> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseReject<T>.GetOrCreate(Internal.DelegateArg<TReject, T>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<Promise<T>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseRejectPromise<T>.GetOrCreate(Internal.DelegateVoid<Promise<T>>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TReject>(Func<TReject, Promise<T>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseRejectPromise<T>.GetOrCreate(Internal.DelegateArg<TReject, Promise<T>>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<Action<Deferred>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseRejectDeferred<T>.GetOrCreate(Internal.DelegateVoid<Action<Deferred>>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TReject>(Func<TReject, Action<Deferred>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseRejectDeferred<T>.GetOrCreate(Internal.DelegateArg<TReject, Action<Deferred>>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}
#endregion

#region Reject or Reject Callbacks
		public Promise Then(Action<T> onResolved, Action onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveReject.GetOrCreate(Internal.DelegateArg<T>.GetOrCreate(onResolved), Internal.DelegateVoid.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TReject>(Action<T> onResolved, Action<TReject> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveReject.GetOrCreate(Internal.DelegateArg<T>.GetOrCreate(onResolved), Internal.DelegateArg<TReject>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(Internal.DelegateArg<T, TResult>.GetOrCreate(onResolved), Internal.DelegateVoid<TResult>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, TResult> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveReject<TResult>.GetOrCreate(Internal.DelegateArg<T, TResult>.GetOrCreate(onResolved), Internal.DelegateArg<TReject, TResult>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveRejectPromise.GetOrCreate(Internal.DelegateArg<T, Promise>.GetOrCreate(onResolved), Internal.DelegateVoid<Promise>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TReject>(Func<T, Promise> onResolved, Func<TReject, Promise> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveRejectPromise.GetOrCreate(Internal.DelegateArg<T, Promise>.GetOrCreate(onResolved), Internal.DelegateArg<TReject, Promise>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(Internal.DelegateArg<T, Promise<TResult>>.GetOrCreate(onResolved), Internal.DelegateVoid<Promise<TResult>>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			var promise = Internal.PromiseResolveRejectPromise<TResult>.GetOrCreate(Internal.DelegateArg<T, Promise<TResult>>.GetOrCreate(onResolved), Internal.DelegateArg<TReject, Promise<TResult>>.GetOrCreate(onRejected));
			HookupNewPromise(promise);
			return promise;
		}
#endregion
	}
}