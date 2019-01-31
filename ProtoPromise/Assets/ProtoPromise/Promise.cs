using System;
using System.Collections.Generic;

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

	public partial class Promise : UnityEngine.CustomYieldInstruction, IValueContainer, ILinked<Promise>, IPoolable, IResetable
	{
		Promise ILinked<Promise>.Next { get { return NextInternal; } set { NextInternal = value; } }
		internal Promise NextInternal { get; set; }

		private static Dictionary<Promise, Action> CompleteVoids = new Dictionary<Promise, Action>();
		protected static Dictionary<Promise, Action<Promise>> CompletePromises = new Dictionary<Promise, Action<Promise>>(); // Used to prevent anonymous closure allocations.

		//private static int idCounter = 0;
		//public readonly int id;

		private int poolOptsInternal;
		bool IPoolable.CanPool { get { return poolOptsInternal < 0; }}

		void IPoolable.OptIn()
		{
			checked
			{
				--poolOptsInternal;
			}
		}

		void IPoolable.OptOut()
		{
			checked
			{
				++poolOptsInternal;
			}
		}

		protected uint nextCount;
		private Promise previous;

		private FinallyPromise final;

		protected Exception _exception;
		
		private LinkedQueueClass<Promise> NextBranches = new LinkedQueueClass<Promise>();
		internal ADeferred DeferredInternal { get; set; }

		protected bool ended = false;
		protected bool handling = false; // This is to handle any new callbacks being added from a callback that is being invoked. e.g. promise.Done(() => { DoSomething(); promise.Done(DoSomethingElse); })

		public PromiseState State { get; protected set; }

		internal Promise() // TODO: object pooling
		{
			//id = idCounter++;
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
			handling = true;
			Action temp;
			while (CompleteVoids.TryGetValue(this, out temp)) // Keep looping in case more onComplete callbacks are added from the invoke. This avoids recursion to prevent StackOverflows.
			{
				CompleteVoids.Remove(this);
				try
				{
					temp.Invoke();
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
			Action<Promise> stateAdoptionCallback;
			if (CompletePromises.TryGetValue(this, out stateAdoptionCallback))
			{
				stateAdoptionCallback.Invoke(this);
				CompletePromises.Remove(this);
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

		internal Promise ResolveInternal(IValueContainer feed)
		{
			handling = true;
			State = PromiseState.Resolved;
			Promise promise = null;
			try
			{
				promise = ResolveProtected(feed);
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
				final = PromisePool.TakePromiseInternal<FinallyPromise>(DeferredInternal);
			}
			return final;
		}

		public Promise Finally(Action onFinally)
		{
			Finally();
			final.finalHandler += onFinally;
			if (final.State != PromiseState.Pending && !final.handling)
			{
				final.HandleFinallies();
			}
			return final;
		}

		public Promise Complete(Action onComplete)
		{
			Action temp;
			CompleteVoids.TryGetValue(this, out temp);
			temp += onComplete;
			CompleteVoids[this] = temp;
			if (State != PromiseState.Pending && !handling)
			{
				HandleComplete();
			}
			return this;
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
			var promise = PromisePool.TakePromiseInternal<PromiseFromDeferred>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Action<Deferred<T>>> onResolved)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseFromDeferred<T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action onResolved)
		{
			var promise = new PromiseVoidFromVoidResolve();
			promise.DeferredInternal = DeferredInternal;
			//var promise = PromisePool.TakePromiseInternal<PromiseVoidFromVoidResolve>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved)
		{
			var promise = new PromiseArgFromResultResolve<T>();
			promise.DeferredInternal = DeferredInternal;
			//var promise = PromisePool.TakePromiseInternal<PromiseArgFromResultResolve<T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidFromPromiseResultResolve>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgFromPromiseResultResolve<T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		// TODO: add exception filters
		public Promise Catch(Action onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidReject>(DeferredInternal);
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TException>(Action<TException> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidReject<TException>>(DeferredInternal);
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch(Func<Promise> onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidRejectPromise>(DeferredInternal);
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TException>(Func<TException, Promise> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgRejectPromise<TException>>(DeferredInternal);
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}


		public Promise Then(Action onResolved, Action onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidFromVoid>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Action onResolved, Action<TException> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidFromVoid<TException>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved, Func<T> onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgFromResult<T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TException>(Func<T> onResolved, Func<TException, T> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgFromResult<T, TException>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidFromPromiseResult>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Func<Promise> onResolved, Func<TException, Promise> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidFromPromiseResult<TException>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved, Func<Promise<T>> onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgFromPromiseResult<T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TException>(Func<Promise<T>> onResolved, Func<TException, Promise<T>> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgFromPromiseResult<T, TException>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		protected Promise PromiseHelper(Promise promise)
		{
			if (promise.State == PromiseState.Pending)
			{
				Action<Promise> callback;
				CompletePromises.TryGetValue(promise, out callback);
				callback += p =>
				{
					State = p.State;
					_exception = p._exception;
					OnComplete();
				};
			}
			else
			{
				State = promise.State;
				_exception = promise._exception;
				OnComplete();
			}
			return promise;
		}
	}

	public class Promise<T> : Promise, IValueContainer<T>, ILinked<Promise<T>>
	{
		Promise<T> ILinked<Promise<T>>.Next { get { return (Promise<T>) NextInternal; } set { NextInternal = value; } }

		internal Promise() : base() { }

		public T Value { get; protected set; }

		internal void ResolveInternal(T value)
		{
			Value = value;

			handling = true;
			State = PromiseState.Resolved;
			//try
			//{
			//	ResolveDones();
			//}
			//catch (Exception e)
			//{
			//	_exception = e;
			//}
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

		//public new Promise<T> Done(Action onResolved)
		//{
		//	base.Done(onResolved);
		//	return this;
		//}

		//public Promise<T> Done(Action<T> onResolved)
		//{
		//	DoneT(onResolved);
		//	return this;
		//}

		public Promise Then(Func<T, Action<Deferred>> onResolved)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseFromDeferredT<T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Action<Deferred<TResult>>> onResolved)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseFromDeferredT<TResult, T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action<T> onResolved)
		{
			var promise = new PromiseVoidFromArgResolve<T>();
			promise.DeferredInternal = DeferredInternal;
			//var promise = PromisePool.TakePromiseInternal<PromiseVoidFromArgResolve<T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
		{
			var promise = new PromiseArgFromArgResultResolve<T, TResult>();
			promise.DeferredInternal = DeferredInternal;
			//var promise = PromisePool.TakePromiseInternal<PromiseArgFromArgResultResolve<T, TResult>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidFromPromiseArgResultResolve<T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgFromPromiseArgResultResolve<TResult, T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<T> onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgReject<T>>(DeferredInternal);
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TException>(Func<TException, T> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgReject<TException, T>>(DeferredInternal);
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<Promise<T>> onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgRejectPromiseT<T>>(DeferredInternal);
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TException>(Func<TException, Promise<T>> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgRejectPromiseT<TException, T>>(DeferredInternal);
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}


		public Promise Then(Action<T> onResolved, Action onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidFromArg<T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Action<T> onResolved, Action<TException> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidFromArg<T, TException>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgFromArgResult<T, TResult>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TException>(Func<T, TResult> onResolved, Func<TException, TResult> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgFromArgResult<T, TResult, TException>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidFromPromiseArgResult<T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Func<T, Promise> onResolved, Func<TException, Promise> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseVoidFromPromiseArgResult<T, TException>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgFromPromiseArgResult<TResult, T>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TException>(Func<T, Promise<TResult>> onResolved, Func<TException, Promise<TResult>> onRejected) where TException : Exception
		{
			var promise = PromisePool.TakePromiseInternal<PromiseArgFromPromiseArgResult<TResult, T, TException>>(DeferredInternal);
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		protected Promise PromiseHelper(Promise<T> promise)
		{
			if (promise.State == PromiseState.Pending)
			{
				Action<Promise> callback;
				CompletePromises.TryGetValue(promise, out callback);
				callback += p =>
				{
					Promise<T> pt = (Promise<T>) p;
					State = pt.State;
					Value = pt.Value;
					_exception = pt._exception;
					OnComplete();
				};
			}
			else
			{
				State = promise.State;
				_exception = promise._exception;
				OnComplete();
			}
			return promise;
		}
	}
}