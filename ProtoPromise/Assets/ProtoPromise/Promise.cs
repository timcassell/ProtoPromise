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
		//Canceled // Debating whether to implement this or not since it violates Promises/A+ API
	}

	public partial class Promise : UnityEngine.CustomYieldInstruction, IValueContainer, ILinked<Promise>
	{
		Promise ILinked<Promise>.Next { get { return NextInternal; } set { NextInternal = value; } }
		internal Promise NextInternal { get; set; }

		private static int idCounter = 0;
		public readonly int id;

		protected uint nextCount;
		private Promise previous;

		private FinallyPromise final;

		protected Exception _exception;
		
		protected Action onComplete;
		private LinkedQueueStruct<IDelegate> doneHandlers;
		private LinkedQueueClass<Promise> NextBranches = new LinkedQueueClass<Promise>();
		internal ADeferred DeferredInternal { get; set; }

		protected bool ended = false;
		protected bool handling = false; // This is to handle any new callbacks being added from a callback that is being invoked. e.g. promise.Done(() => { DoSomething(); promise.Done(DoSomethingElse); })

		public PromiseState State { get; protected set; }

		internal Promise(ADeferred deferred) // TODO: object pooling
		{
			id = idCounter++;
			DeferredInternal = deferred;
		}

		private void OnFinally()
		{
			if (!ended || nextCount > 0)
			{
				return;
			}

			AddFinal(this);
		}

		private void HandleComplete()
		{
			//Debug.LogWarning(id + " Complete, deferred state: " + DeferredInternal.StateInternal + ", invoking onComplete: " + (onComplete != null));

			for (Action temp = onComplete; temp != null; temp = onComplete) // Keep looping in case more onComplete callbacks are added from the invoke. This avoids recursion to prevent StackOverflows.
			{
				handling = true;

				onComplete = null;
				try
				{
					temp.Invoke();

					if (State == PromiseState.Resolved && _exception == null)
					{
						// Just in case a .Done callback was added during the onComplete invocation.
						ResolveDones();
					}
				}
				catch (Exception e)
				{
					if (_exception != null)
					{
						UnityEngine.Debug.LogError("A new exception was encountered in a Promise.Complete callback before an old exception was handled." +
									   " The new exception will replace the old exception propagating up the promise chain.\nOld exception:\n" +
									   _exception);
					}
					_exception = e;
				}
			}
			handling = false;

		}

		protected void OnComplete()
		{
			HandleComplete();
			OnFinally();
		}

		internal Promise HandleInternal(Promise feed)
		{
			Exception exception = feed._exception;
			var promise = exception == null ? ResolveInternal(feed) : RejectInternal(exception);

			return promise;
		}

		protected void ResolveDones()
		{
			handling = true;
			try
			{
				for (IDelegate del = doneHandlers.Peek(); del != null; del = del.Next)
				{
					del.Invoke(this);
				}
			}
			catch (Exception e)
			{
				_exception = e;
			}
			doneHandlers.Clear();
			handling = false;
		}

		internal Promise ResolveInternal(IValueContainer feed)
		{
			handling = true;
			State = PromiseState.Resolved;
			Promise promise = null;
			try
			{
				promise = ResolveProtected(feed);
				ResolveDones();
			}
			catch (Exception e)
			{
				_exception = e;
			}
			OnComplete();
			handling = false;
			return promise;
		}

		internal virtual Promise ResolveProtected(IValueContainer feed) // private protected not supported before c# 7.2, so must use internal.
		{
			return null;
		}

		internal Promise RejectInternal(Exception exception)
		{
			handling = true;
			State = PromiseState.Rejected;
			Promise promise = null;
			try
			{
				promise = RejectProtected(exception);
				if (_exception == null)
				{
					ResolveDones();
				}
			}
			catch (Exception e)
			{
				_exception = e;
			}
			OnComplete();
			handling = false;
			return promise;
		}

		protected virtual Promise RejectProtected(Exception exception)
		{
			_exception = exception;
			return null;
		}

		public override bool keepWaiting
		{
			get
			{
				return State == PromiseState.Pending;
			}
		}

		public Promise Notification<TNotify>(Action<TNotify> onNotification)
		{
			DeferredInternal.NotificationInternal(onNotification);
			return this;
		}

		public Promise End()
		{
			if (ended)
			{
				return this;
			}

			ended = true;
			if (State != PromiseState.Pending && !handling)
			{
				OnFinally();
			}
			return this;
		}

		public Promise Finally()
		{
			if (final == null)
			{
				final = new FinallyPromise(DeferredInternal);
			}
			return final;
		}

		public Promise Finally(Action onFinally)
		{
			if (final == null)
			{
				final = new FinallyPromise(DeferredInternal);
			}
			final.finalHandler += onFinally;
			if (final.State != PromiseState.Pending && !final.handling)
			{
				final.HandleFinallies();
			}
			return final;
		}

		public Promise Complete(Action onComplete)
		{
			this.onComplete += onComplete;
			if (State != PromiseState.Pending && !handling)
			{
				HandleComplete();
			}
			return this;
		}

		public Promise Done(Action onResolved)
		{
			switch (State)
			{
				case PromiseState.Pending:
					doneHandlers.Enqueue(new DelegateVoid(onResolved));
					break;
				case PromiseState.Resolved:
					if (_exception != null)
					{
						break;
					}
					doneHandlers.Enqueue(new DelegateVoid(onResolved));
					if (!handling)
					{
						ResolveDones();
						HandleComplete(); // Just in case a .Complete callback was added during a .Done callback invocation.
					}
					break;
			}
			return this;
		}

		protected void DoneT<T>(Action<T> onResolved)
		{
			switch (State)
			{
				case PromiseState.Pending:
					doneHandlers.Enqueue(new DelegateArg<T>(onResolved));
					break;
				case PromiseState.Resolved:
					if (_exception != null)
					{
						break;
					}
					doneHandlers.Enqueue(new DelegateArg<T>(onResolved));
					if (!handling)
					{
						ResolveDones();
						HandleComplete(); // Just in case a .Complete callback was added during a .Done callback invocation.
					}
					break;
			}
		}

		protected void HookupNewPromise(Promise newPromise)
		{
			checked
			{
				++nextCount;
			}
			RemoveFinal(this);
			newPromise.previous = this;

			if (State == PromiseState.Pending | NextBranches.Peek() != null)
			{
				NextBranches.Enqueue(newPromise);
			}
			else
			{
				//NextBranches.Enqueue(newPromise);
				//ContinueHandlingInternal(this);
				newPromise.HandleInternal(this);
			}
		}

		public Promise Then(Func<Action<Deferred>> onResolved)
		{
			var promise = new PromiseFromDeferred(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Action<Deferred<T>>> onResolved)
		{
			var promise = new PromiseFromDeferred<T>(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action onResolved)
		{
			var promise = new PromiseVoidFromVoidResolve(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved)
		{
			var promise = new PromiseArgFromResultResolve<T>(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved)
		{
			var promise = new PromiseVoidFromPromiseResultResolve(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved)
		{
			var promise = new PromiseArgFromPromiseResultResolve<T>(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		// TODO: add exception filters
		public Promise Catch(Action onRejected)
		{
			var promise = new PromiseVoidReject(DeferredInternal)
			{
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TException>(Action<TException> onRejected) where TException : Exception
		{
			var promise = new PromiseVoidReject<TException>(DeferredInternal)
			{
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch(Func<Promise> onRejected)
		{
			var promise = new PromiseVoidRejectPromise(DeferredInternal)
			{
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TException>(Func<TException, Promise> onRejected) where TException : Exception
		{
			var promise = new PromiseArgRejectPromise<TException>(DeferredInternal)
			{
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}


		public Promise Then(Action onResolved, Action onRejected)
		{
			var promise = new PromiseVoidFromVoid(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Action onResolved, Action<TException> onRejected) where TException : Exception
		{
			var promise = new PromiseVoidFromVoid<TException>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved, Func<T> onRejected)
		{
			var promise = new PromiseArgFromResult<T>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TException>(Func<T> onResolved, Func<TException, T> onRejected) where TException : Exception
		{
			var promise = new PromiseArgFromResult<T, TException>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected)
		{
			var promise = new PromiseVoidFromPromiseResult(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Func<Promise> onResolved, Func<TException, Promise> onRejected) where TException : Exception
		{
			var promise = new PromiseVoidFromPromiseResult<TException>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved, Func<Promise<T>> onRejected)
		{
			var promise = new PromiseArgFromPromiseResult<T>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TException>(Func<Promise<T>> onResolved, Func<TException, Promise<T>> onRejected) where TException : Exception
		{
			var promise = new PromiseArgFromPromiseResult<T, TException>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		protected Promise PromiseHelper(Promise promise)
		{
			return promise
				.Then<Exception>(() =>
				{
					State = PromiseState.Resolved;
					try
					{
						ResolveDones();
					}
					catch (Exception e)
					{
						_exception = e;
					}
				}, e =>
				{
					State = PromiseState.Rejected;
					_exception = e;
				})
				.Complete(OnComplete)
				.End();
		}
	}

	public class Promise<T> : Promise, IValueContainer<T>
	{
		internal Promise(ADeferred deferred) : base(deferred) { }

		public T Value { get; protected set; }

		internal void ResolveInternal(T value)
		{
			Value = value;

			handling = true;
			State = PromiseState.Resolved;
			try
			{
				ResolveDones();
			}
			catch (Exception e)
			{
				_exception = e;
			}
			OnComplete();
			handling = false;
		}

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = ((IValueContainer<T>) feed).Value;
			return null;
		}

		public new Promise<T> Notification<TNotify>(Action<TNotify> onNotification)
		{
			DeferredInternal.NotificationInternal(onNotification);
			return this;
		}

		public new Promise<T> Complete(Action onComplete)
		{
			base.Complete(onComplete);
			return this;
		}

		public new Promise<T> End()
		{
			base.End();
			return this;
		}

		public new Promise<T> Done(Action onResolved)
		{
			base.Done(onResolved);
			return this;
		}

		public Promise<T> Done(Action<T> onResolved)
		{
			DoneT(onResolved);
			return this;
		}

		public Promise Then(Func<T, Action<Deferred>> onResolved)
		{
			var promise = new PromiseFromDeferredT<T>(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Action<Deferred<TResult>>> onResolved)
		{
			var promise = new PromiseFromDeferredT<TResult, T>(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action<T> onResolved)
		{
			var promise = new PromiseVoidFromArgResolve<T>(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
		{
			var promise = new PromiseArgFromArgResultResolve<T, TResult>(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved)
		{
			var promise = new PromiseVoidFromPromiseArgResultResolve<T>(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
		{
			var promise = new PromiseArgFromPromiseArgResultResolve<TResult, T>(DeferredInternal)
			{
				resolveHandler = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<T> onRejected)
		{
			var promise = new PromiseArgReject<T>(DeferredInternal)
			{
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TException>(Func<TException, T> onRejected) where TException : Exception
		{
			var promise = new PromiseArgReject<TException, T>(DeferredInternal)
			{
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<Promise<T>> onRejected)
		{
			var promise = new PromiseArgRejectPromiseT<T>(DeferredInternal)
			{
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TException>(Func<TException, Promise<T>> onRejected) where TException : Exception
		{
			var promise = new PromiseArgRejectPromiseT<TException, T>(DeferredInternal)
			{
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}


		public Promise Then(Action<T> onResolved, Action onRejected)
		{
			var promise = new PromiseVoidFromArg<T>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Action<T> onResolved, Action<TException> onRejected) where TException : Exception
		{
			var promise = new PromiseVoidFromArg<T, TException>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected)
		{
			var promise = new PromiseArgFromArgResult<T, TResult>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TException>(Func<T, TResult> onResolved, Func<TException, TResult> onRejected) where TException : Exception
		{
			var promise = new PromiseArgFromArgResult<T, TResult, TException>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected)
		{
			var promise = new PromiseVoidFromPromiseArgResult<T>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Func<T, Promise> onResolved, Func<TException, Promise> onRejected) where TException : Exception
		{
			var promise = new PromiseVoidFromPromiseArgResult<T, TException>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
		{
			var promise = new PromiseArgFromPromiseArgResult<TResult, T>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TException>(Func<T, Promise<TResult>> onResolved, Func<TException, Promise<TResult>> onRejected) where TException : Exception
		{
			var promise = new PromiseArgFromPromiseArgResult<TResult, T, TException>(DeferredInternal)
			{
				resolveHandler = onResolved,
				rejectHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		protected Promise PromiseHelper(Promise<T> promise)
		{
			return promise
				.Then<Exception>(x =>
				{
					State = PromiseState.Resolved;
					Value = x;
					try
					{
						ResolveDones();
					}
					catch (Exception e)
					{
						_exception = e;
					}
				}, e =>
				{
					State = PromiseState.Rejected;
					_exception = e;
				})
				.Complete(OnComplete)
				.End();
		}
	}
}