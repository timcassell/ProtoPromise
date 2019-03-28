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
	public partial class Promise : UnityEngine.CustomYieldInstruction
	{
		public PromiseState State { get { return _state; } }

		public Promise Canceled(Action onCanceled)
		{
			switch (_state)
			{
				case PromiseState.Pending:
				{
					HookUpCancelCallback(onCanceled);
					break;
				}
				case PromiseState.Canceled:
				{
					if (HookUpCancelCallback(onCanceled))
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
			switch (_state)
			{
				case PromiseState.Pending:
					{
						HookUpCancelCallback(onCanceled);
						break;
					}
				case PromiseState.Canceled:
					{
						if (HookUpCancelCallback(onCanceled))
						{
							HandleCancel();
						}
						break;
					}
			}
			return this;
		}

		// Returns true if this is the first item added to the queue.
		bool HookUpCancelCallback(Action onCanceled)
		{
			ValueLinkedQueue<IDelegate> cancelQueue;
			bool newAdd = !cancels.TryGetValue(this, out cancelQueue);
			// TODO: pool delegate
			cancelQueue.Enqueue(new DelegateVoidVoid() { callback = onCanceled });
			cancels[this] = cancelQueue;
			return newAdd;
		}

		// Returns true if this is the first item added to the queue.
		bool HookUpCancelCallback<TCancel>(Action<TCancel> onCanceled)
		{
			ValueLinkedQueue<IDelegate> cancelQueue;
			bool newAdd = !cancels.TryGetValue(this, out cancelQueue);
			// TODO: pool delegate
			cancelQueue.Enqueue(new DelegateArgVoid<TCancel>() { callback = onCanceled });
			cancels[this] = cancelQueue;
			return newAdd;
		}

		/// <summary>
		/// Cancels this promise and all .Then/.Catch promises that have been chained from this.
		/// Does nothing if this promise isn't pending.
		/// </summary>
		public virtual void Cancel()
		{
			if (_state != PromiseState.Pending)
			{
				return;
			}
			// TODO: pool exception.
			// Use reject value as cancel value.
			rejectedOrCanceledValueInternal = new UnhandledException();

			HandleCancel();
			ContinueHandlingInternal(this);
		}

		/// <summary>
		/// Cancels this promise and all .Then/.Catch promises that have been chained from this with the provided cancel reason.
		/// Does nothing if this promise isn't pending.
		/// </summary>
		public virtual void Cancel<TCancel>(TCancel reason)
		{
			if (_state != PromiseState.Pending)
			{
				return;
			}
			// TODO: pool exception.
			// Use reject value as cancel value.
			rejectedOrCanceledValueInternal = new UnhandledException<TCancel>().SetValue(reason);

			HandleCancel();
			ContinueHandlingInternal(this);
		}

		public override bool keepWaiting
		{
			get
			{
				return _state == PromiseState.Pending;
			}
		}

		public Promise Progress(Action<float> onProgress)
		{
			// TODO
			return this;
		}

		public Promise Done(Action onComplete)
		{
			return Complete(onComplete).Done();
		}

		public Promise Done()
		{
			if (done)
			{
				return this;
			}

			done = true;
			if (!handling)
			{
				switch(_state)
				{
					case PromiseState.Resolved:
					case PromiseState.Rejected:
					case PromiseState.Canceled:
					{
						OnFinally();
						break;
					}
				}
			}
			return this;
		}

		public Promise Finally()
		{
			FinallyPromise promise;
			if (!finals.TryGetValue(this, out promise))
			{
				if (!objectPoolInternal.TryTakeInternal(out promise))
				{
					promise = new FinallyPromise();
				}
				promise.deferredInternal = deferredInternal;
				promise.ResetInternal();

				finals[this] = promise;
			}
			return promise;
		}

		public Promise Finally(Action onFinally)
		{
			FinallyPromise promise = (FinallyPromise) Finally();
			promise.finalHandler += onFinally;
			switch (promise._state)
			{
				case PromiseState.Rejected:
				case PromiseState.Resolved:
				{
					promise.HandleFinallies();
					break;
				}
			}
		
			return promise;
		}

		// TODO: treat this the same as Then(onComplete, onComplete).
		public Promise Complete(Action onComplete)
		{
			Action temp;
			completeVoids.TryGetValue(this, out temp);
			temp += onComplete;
			completeVoids[this] = temp;
			switch (_state)
			{
				case PromiseState.Rejected:
				case PromiseState.Resolved:
					{
						HandleComplete();
						break;
					}
			}
			return this;
		}

		public Promise Then(Func<Action<Deferred>> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			PromiseVoidResolveDeferred promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolveDeferred();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Action<Deferred<T>>> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			PromiseVoidResolveRejectDeferred<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolveRejectDeferred<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			PromiseVoidResolve promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolve();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
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

			PromiseVoidResolve<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolve<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
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

			PromiseVoidResolvePromise promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolvePromise();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
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

			PromiseVoidResolvePromise<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolvePromise<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		// TODO: add filters
		public Promise Catch(Action onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseReject promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseReject();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateVoidVoid del = new DelegateVoidVoid();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TReject>(Action<TReject> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseReject promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseReject();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateArgVoid<TReject> del = new DelegateArgVoid<TReject>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch(Func<Promise> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseRejectPromise promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseRejectPromise();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateVoidResult<Promise> del = new DelegateVoidResult<Promise>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TReject>(Func<TReject, Promise> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseRejectPromise promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseRejectPromise();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateArgResult<TReject, Promise> del = new DelegateArgResult<TReject, Promise>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch(Func<Action<Deferred>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseRejectDeferred promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseRejectDeferred();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateVoidResult<Action<Deferred>> del = new DelegateVoidResult<Action<Deferred>>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TReject>(Func<TReject, Action<Deferred>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseRejectDeferred promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseRejectDeferred();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateArgResult<TReject, Action<Deferred>> del = new DelegateArgResult<TReject, Action<Deferred>>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}


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


			PromiseVoidResolveReject promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolveReject();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateVoidVoid del = new DelegateVoidVoid();
			del.callback = onRejected;
			promise.rejectHandler = del;
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


			PromiseVoidResolveReject promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolveReject();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateArgVoid<TReject> del = new DelegateArgVoid<TReject>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		// TODO
		//public Promise Then<TReject>(Action onResolved, Func<Promise> onRejected)
		//{
		//	
		//}

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


			PromiseVoidResolveReject<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolveReject<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateVoidResult<T> del = new DelegateVoidResult<T>();
			del.callback = onRejected;
			promise.rejectHandler = del;
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


			PromiseVoidResolveReject<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolveReject<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateArgResult<TReject, T> del = new DelegateArgResult<TReject, T>();
			del.callback = onRejected;
			promise.rejectHandler = del;
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


			PromiseVoidResolveRejectPromise promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolveRejectPromise();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateVoidResult<Promise> del = new DelegateVoidResult<Promise>();
			del.callback = onRejected;
			promise.rejectHandler = del;
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


			PromiseVoidResolveRejectPromise promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolveRejectPromise();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateArgResult<TReject, Promise> del = new DelegateArgResult<TReject, Promise>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		// TODO
		//public Promise Then(Func<Promise> onResolved, Action onRejected)
		//{
		//
		//}

		//public Promise Then<TReject>(Func<Promise> onResolved, Action<TReject> onRejected)
		//{
		//	
		//}

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


			PromiseVoidResolveRejectPromise<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolveRejectPromise<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateVoidResult<Promise<T>> del = new DelegateVoidResult<Promise<T>>();
			del.callback = onRejected;
			promise.rejectHandler = del;
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


			PromiseVoidResolveRejectPromise<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidResolveRejectPromise<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateArgResult<TReject, Promise<T>> del = new DelegateArgResult<TReject, Promise<T>>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}
	}

	public partial class Promise<T> : Promise, IValueContainer<T>
	{
		// TODO: Set this to default(T) when finally runs.
		internal T _valueInternal;
		T IValueContainer<T>.Value { get { return _valueInternal; } }

		public new Promise<T> Canceled(Action onCanceled)
		{
			base.Canceled(onCanceled);
			return this;
		}

		public new Promise<T> Progress(Action<float> onProgress)
		{
			base.Progress(onProgress);
			return this;
		}

		public new Promise<T> Complete(Action onComplete)
		{
			base.Complete(onComplete);
			return this;
		}

		public Promise Then(Func<T, Action<Deferred>> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			PromiseArgResolveDeferred<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolveDeferred<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Action<Deferred<TResult>>> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			PromiseArgResolveDeferred<T, TResult> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolveDeferred<T, TResult>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action<T> onResolved)
		{
			// TODO: wrap in "#if DEBUG"
			if (onResolved == null)
			{
				throw new ArgumentNullException("onResolved");
			}

			PromiseArgResolve<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolve<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
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

			PromiseArgResolve<T, TResult> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolve<T, TResult>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
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

			PromiseArgResolvePromise<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolvePromise<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
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

			PromiseArgResolvePromise<T, TResult> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolvePromise<T, TResult>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		// TODO: Add filters.
		public Promise<T> Catch(Func<T> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseReject<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseReject<T>();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateVoidResult<T> del = new DelegateVoidResult<T>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TReject>(Func<TReject, T> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseReject<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseReject<T>();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateArgResult<TReject, T> del = new DelegateArgResult<TReject, T>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<Promise<T>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseRejectPromise<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseRejectPromise<T>();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateVoidResult<Promise<T>> del = new DelegateVoidResult<Promise<T>>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TReject>(Func<TReject, Promise<T>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseRejectPromise<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseRejectPromise<T>();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateArgResult<TReject, Promise<T>> del = new DelegateArgResult<TReject, Promise<T>>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<Action<Deferred<T>>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseRejectDeferred<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseRejectDeferred<T>();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateVoidResult<Action<Deferred<T>>> del = new DelegateVoidResult<Action<Deferred<T>>>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TReject>(Func<TReject, Action<Deferred<T>>> onRejected)
		{
			if (onRejected == null)
			{
				throw new ArgumentNullException("onRejected");
			}

			PromiseRejectDeferred<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseRejectDeferred<T>();
			}
			promise.ResetInternal();

			// TODO: pool delegates.
			DelegateArgResult<TReject, Action<Deferred<T>>> del = new DelegateArgResult<TReject, Action<Deferred<T>>>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}


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

			PromiseArgResolveReject<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolveReject<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateVoidVoid del = new DelegateVoidVoid();
			del.callback = onRejected;
			promise.rejectHandler = del;
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

			PromiseArgResolveReject<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolveReject<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateArgVoid<TReject> del = new DelegateArgVoid<TReject>();
			del.callback = onRejected;
			promise.rejectHandler = del;
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

			PromiseArgResolveReject<T, TResult> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolveReject<T, TResult>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateVoidResult<TResult> del = new DelegateVoidResult<TResult>();
			del.callback = onRejected;
			promise.rejectHandler = del;
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

			PromiseArgResolveReject<T, TResult> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolveReject<T, TResult>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateArgResult<TReject, TResult> del = new DelegateArgResult<TReject, TResult>();
			del.callback = onRejected;
			promise.rejectHandler = del;
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

			PromiseArgResolveRejectPromise<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolveRejectPromise<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateVoidResult<Promise> del = new DelegateVoidResult<Promise>();
			del.callback = onRejected;
			promise.rejectHandler = del;
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

			PromiseArgResolveRejectPromise<T> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolveRejectPromise<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateArgResult<TReject, Promise> del = new DelegateArgResult<TReject, Promise>();
			del.callback = onRejected;
			promise.rejectHandler = del;
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

			PromiseArgResolveRejectPromise<T, TResult> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolveRejectPromise<T, TResult>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateVoidResult<Promise<TResult>> del = new DelegateVoidResult<Promise<TResult>>();
			del.callback = onRejected;
			promise.rejectHandler = del;
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

			PromiseArgResolveRejectPromise<T, TResult> promise;
			if (!objectPoolInternal.TryTakeInternal(out promise))
			{
				promise = new PromiseArgResolveRejectPromise<T, TResult>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			// TODO: pool delegates.
			DelegateArgResult<TReject, Promise<TResult>> del = new DelegateArgResult<TReject, Promise<TResult>>();
			del.callback = onRejected;
			promise.rejectHandler = del;
			HookupNewPromise(promise);
			return promise;
		}
	}
}