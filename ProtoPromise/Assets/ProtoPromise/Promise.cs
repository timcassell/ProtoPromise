using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoPromise
{
	public enum PromiseState
	{
		/// <summary>
		/// Waiting to be resolved or rejected.
		/// </summary>
		Pending,
		/// <summary>
		/// This promise was resolved successfully.
		/// </summary>
		Resolved,
		/// <summary>
		/// This promise was rejected.
		/// </summary>
		Rejected,
		//Canceled
	}

	public partial class Promise : CustomYieldInstruction, IValueContainer, ILinked<Promise>
	{
		Promise ILinked<Promise>.Next { get { return NextInternal; } set { NextInternal = value; } }
		internal Promise NextInternal { get; set; }

		private static int idCounter = 0;
		public readonly int id;

		protected Exception _exception; // TODO: Track how many nextbranches are subscribed and how many resolved this.
		
		protected Action onComplete;
		private LinkedQueueStruct<IDelegate> doneHandlers;
		private LinkedQueueClass<Promise> NextBranches = new LinkedQueueClass<Promise>();
		internal ADeferred DeferredInternal { get; set; }

		protected bool handling = false; // This is to handle any new callbacks being added from a callback that is being invoked. e.g. promise.Done(() => { DoSomething(); promise.Done(DoSomethingElse); })

		public PromiseState State { get; protected set; }

		internal Promise(ADeferred deferred) // TODO: object pooling
		{
			id = idCounter++;
			DeferredInternal = deferred;
		}

		internal void OnComplete()
		{
			//Debug.LogWarning(id + " Complete, deferred state: " + DeferredInternal.StateInternal + ", invoking onComplete: " + (onComplete != null));
			for (Action temp = onComplete; temp != null; temp = onComplete) // Keep looping in case more onComplete callbacks are added from the invoke. This should avoid any recursion.
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
						Debug.LogError("A new exception was encountered in a Promise.Complete callback before an old exception was handled." +
						               " The new exception will replace the old exception propagating up the promise chain.\nOld exception:\n" +
						               _exception);
					}
					_exception = e;
				}
			}
			handling = false;

			// TODO: Subscribe to global error thrower for next frame.
			GlobalMonoBehaviour.Yield(() =>
			{
				if (_exception == null) return;
				throw _exception is UnhandledException ? _exception : new UnhandledException(_exception);
			});
		}

		internal Promise HandleInternal(Promise feed)
		{
			Exception exception = feed._exception;
			return exception == null ? ResolveInternal(feed) : RejectInternal(exception);
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
			}
			catch (Exception e)
			{
				_exception = e;
			}
			OnComplete();
			handling = false;
			return promise;
		}

		internal virtual Promise RejectProtected(Exception exception) // private protected not supported before c# 7.2, so must use internal.
		{
			_exception = exception;
			return null;
		}

		internal bool RejectionNotHandled()
		{
			return _exception != null;
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

		// TODO Implement End
		public Promise End()
		{
			//DeferredInternal.End();
			return this;
		}

		// TODO Implement Finally
		//public CustomYieldInstruction Finally()
		//{
		//	return DeferredInternal.FinallyInternal();
		//}

		//public CustomYieldInstruction Finally(Action onFinally)
		//{
		//	return DeferredInternal.FinallyInternal(onFinally);
		//}

		public Promise Complete(Action onComplete)
		{
			this.onComplete += onComplete;
			if (State != PromiseState.Pending && !handling)
			{
				OnComplete();
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
						OnComplete(); // Just in case a .Complete callback was added during a .Done callback invocation.
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
						OnComplete(); // Just in case a .Complete callback was added during a .Done callback invocation.
					}
					break;
			}
		}

		protected void HookupNewPromise(Promise newPromise)
		{
			if (State != PromiseState.Pending && NextBranches.Peek() == null)
			{
				NextBranches.Enqueue(newPromise);
				ContinueHandlingInternal(this);
			}
			else
			{
				NextBranches.Enqueue(newPromise);
			}
		}

		public Promise Catch(Action onRejected)
		{
			PromiseVoidReject promise = new PromiseVoidReject(DeferredInternal)
			{
				rejectionHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TException>(Action<TException> onRejected) where TException : Exception
		{
			PromiseVoidReject<TException> promise = new PromiseVoidReject<TException>(DeferredInternal)
			{
				rejectionHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch(Func<Promise> onRejected)
		{
			PromiseVoidRejectPromise promise = new PromiseVoidRejectPromise(DeferredInternal)
			{
				rejectionHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TException>(Func<TException, Promise> onRejected) where TException : Exception
		{
			PromiseArgRejectPromise<TException> promise = new PromiseArgRejectPromise<TException>(DeferredInternal)
			{
				rejectionHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action onResolved)
		{
			PromiseVoidFromVoid promise = new PromiseVoidFromVoid(DeferredInternal)
			{
				callback = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved)
		{
			PromiseArgFromResultResolve<T> promise = new PromiseArgFromResultResolve<T>(DeferredInternal)
			{
				callback = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved)
		{
			PromiseVoidFromPromiseResultResolve promise = new PromiseVoidFromPromiseResultResolve(DeferredInternal)
			{
				callback = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved)
		{
			PromiseArgFromPromiseResultResolve<T> promise = new PromiseArgFromPromiseResultResolve<T>(DeferredInternal)
			{
				callback = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		protected void PromiseHelper(Promise promise)
		{
			promise
				.Done(() =>
				{
					try
					{
						ResolveDones();
					}
					catch (Exception e)
					{
						State = PromiseState.Resolved;
						_exception = e;
					}
				})
				.Catch<Exception>(e =>
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

			ResolveInternal(this);
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

		public Promise<T> Catch(Func<T> onRejected)
		{
			PromiseArgReject<T> promise = new PromiseArgReject<T>(DeferredInternal)
			{
				rejectionHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TException>(Func<TException, T> onRejected) where TException : Exception
		{
			PromiseArgReject<TException, T> promise = new PromiseArgReject<TException, T>(DeferredInternal)
			{
				rejectionHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<Promise<T>> onRejected)
		{
			PromiseArgRejectPromiseT<T> promise = new PromiseArgRejectPromiseT<T>(DeferredInternal)
			{
				rejectionHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TException>(Func<TException, Promise<T>> onRejected) where TException : Exception
		{
			PromiseArgRejectPromiseT<TException, T> promise = new PromiseArgRejectPromiseT<TException, T>(DeferredInternal)
			{
				rejectionHandler = onRejected
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action<T> onResolved)
		{
			PromiseVoidFromArgResolve<T> promise = new PromiseVoidFromArgResolve<T>(DeferredInternal)
			{
				callback = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
		{
			PromiseArgFromArgResultResolve<T, TResult> promise = new PromiseArgFromArgResultResolve<T, TResult>(DeferredInternal)
			{
				callback = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved)
		{
			PromiseVoidFromPromiseArgResultResolve<T> promise = new PromiseVoidFromPromiseArgResultResolve<T>(DeferredInternal)
			{
				callback = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
		{
			PromiseArgFromPromiseArgResultResolve<TResult, T> promise = new PromiseArgFromPromiseArgResultResolve<TResult, T>(DeferredInternal)
			{
				callback = onResolved
			};
			HookupNewPromise(promise);
			return promise;
		}

		protected void PromiseHelper(Promise<T> promise)
		{
			promise
				.Done(x =>
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
				})
				.Catch<Exception>(e =>
				{
					State = PromiseState.Rejected;
					_exception = e;
				})
				.Complete(OnComplete)
				.End();
		}
	}
}