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

	// If using Unity prior to 5.3, remove "UnityEngine.CustomYieldInstruction". Instead, you can wait for a promise to complete in a coroutine this way:
	// do { yield return null; } while(promise.State == PromiseState.Pending);
	public partial class Promise : UnityEngine.CustomYieldInstruction, IValueContainer, ILinked<Promise>, IPoolable, IResetable
	{
		Promise ILinked<Promise>.Next { get { return NextInternal; } set { NextInternal = value; } }
		internal Promise NextInternal { get; set; }

		// Dictionaries to use less memory for expected less used functions.
		private static Dictionary<Promise, FinallyPromise> finals = new Dictionary<Promise, FinallyPromise>();
		private static Dictionary<Promise, Action> completeVoids = new Dictionary<Promise, Action>();
		protected static Dictionary<Promise, Action<Promise>> completePromises = new Dictionary<Promise, Action<Promise>>(); // Used to prevent anonymous closure allocations.


		//private static int idCounter = 0;
		//public readonly int id;

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

		//private FinallyPromise final;

		protected Exception _exception;
		
		private LinkedQueueClass<Promise> NextBranches = new LinkedQueueClass<Promise>();
		internal ADeferred DeferredInternal { get; set; }

		private sbyte poolOptsInternal; // This is an sbyte to conserve memory footprint. Change this to System.Int32(int) if you need to perpetually use one promise in more than 128 places.

		protected bool ended = false;
		protected bool handling = false; // This is to handle any new callbacks being added from a callback that is being invoked. e.g. promise.Done(() => { DoSomething(); promise.Done(DoSomethingElse); })

		public PromiseState State { get; protected set; }

		internal Promise() // TODO: object pooling
		{
			//id = idCounter++;
			if (ObjectPool.poolType == PoolType.OptOut)
			{
				((IPoolable) this).OptIn();
			}
		}

		void IResetable.Reset()
		{
			poolOptsInternal = 0;
			State = PromiseState.Pending;
			ended = false;
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
			while (completeVoids.TryGetValue(this, out temp)) // Keep looping in case more onComplete callbacks are added from the invoke. This avoids recursion to prevent StackOverflows.
			{
				completeVoids.Remove(this);
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
			if (completePromises.TryGetValue(this, out stateAdoptionCallback))
			{
				stateAdoptionCallback.Invoke(this);
				completePromises.Remove(this);
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
			FinallyPromise promise;
			if (!finals.TryGetValue(this, out promise))
			{
				if (!ObjectPool.TryTakeInternal(out promise))
				{
					promise = new FinallyPromise();
				}
				promise.DeferredInternal = DeferredInternal;
				finals[this] = promise;
			}
			return promise;
		}

		public Promise Finally(Action onFinally)
		{
			FinallyPromise promise = (FinallyPromise) Finally();
			promise.finalHandler += onFinally;
			if (promise.State != PromiseState.Pending && !promise.handling)
			{
				promise.HandleFinallies();
			}
			return promise;
		}

		public Promise Complete(Action onComplete)
		{
			Action temp;
			completeVoids.TryGetValue(this, out temp);
			temp += onComplete;
			completeVoids[this] = temp;
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
			newPromise.DeferredInternal = DeferredInternal;
			newPromise.previous = this;
			RemoveFinal(this);

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
			PromiseFromDeferred promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseFromDeferred();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Action<Deferred<T>>> onResolved)
		{
			PromiseFromDeferred<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseFromDeferred<T>();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action onResolved)
		{
			PromiseVoidFromVoidResolve promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromVoidResolve();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved)
		{
			PromiseArgFromResultResolve<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromResultResolve<T>();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved)
		{
			PromiseVoidFromPromiseResultResolve promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseResultResolve();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved)
		{
			PromiseArgFromPromiseResultResolve<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseResultResolve<T>();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		// TODO: add exception filters
		public Promise Catch(Action onRejected)
		{
			PromiseVoidReject promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidReject();
			}
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TException>(Action<TException> onRejected) where TException : Exception
		{
			PromiseVoidReject<TException> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidReject<TException>();
			}
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch(Func<Promise> onRejected)
		{
			PromiseVoidRejectPromise promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidRejectPromise();
			}
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TException>(Func<TException, Promise> onRejected) where TException : Exception
		{
			PromiseArgRejectPromise<TException> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgRejectPromise<TException>();
			}
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}


		public Promise Then(Action onResolved, Action onRejected)
		{
			PromiseVoidFromVoid promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromVoid();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Action onResolved, Action<TException> onRejected) where TException : Exception
		{
			PromiseVoidFromVoid<TException> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromVoid<TException>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved, Func<T> onRejected)
		{
			PromiseArgFromResult<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromResult<T>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TException>(Func<T> onResolved, Func<TException, T> onRejected) where TException : Exception
		{
			PromiseArgFromResult<T, TException> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromResult<T, TException>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected)
		{
			PromiseVoidFromPromiseResult promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseResult();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Func<Promise> onResolved, Func<TException, Promise> onRejected) where TException : Exception
		{
			PromiseVoidFromPromiseResult<TException> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseResult<TException>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved, Func<Promise<T>> onRejected)
		{
			PromiseArgFromPromiseResult<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseResult<T>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TException>(Func<Promise<T>> onResolved, Func<TException, Promise<T>> onRejected) where TException : Exception
		{
			PromiseArgFromPromiseResult<T, TException> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseResult<T, TException>();
			}
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
				completePromises.TryGetValue(promise, out callback);
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

		public Promise Then(Func<T, Action<Deferred>> onResolved)
		{
			PromiseFromDeferredT<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseFromDeferredT<T>();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Action<Deferred<TResult>>> onResolved)
		{
			PromiseFromDeferredT<TResult, T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseFromDeferredT<TResult, T>();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action<T> onResolved)
		{
			PromiseVoidFromArgResolve<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromArgResolve<T>();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
		{
			PromiseArgFromArgResultResolve<T, TResult> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromArgResultResolve<T, TResult>();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved)
		{
			PromiseVoidFromPromiseArgResultResolve<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseArgResultResolve<T>();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
		{
			PromiseArgFromPromiseArgResultResolve<TResult, T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseArgResultResolve<TResult, T>();
			}
			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<T> onRejected)
		{
			PromiseArgReject<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgReject<T>();
			}
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TException>(Func<TException, T> onRejected) where TException : Exception
		{
			PromiseArgReject<TException, T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgReject<TException, T>();
			}
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<Promise<T>> onRejected)
		{
			PromiseArgRejectPromiseT<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgRejectPromiseT<T>();
			}
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TException>(Func<TException, Promise<T>> onRejected) where TException : Exception
		{
			PromiseArgRejectPromiseT<TException, T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgRejectPromiseT<TException, T>();
			}
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}


		public Promise Then(Action<T> onResolved, Action onRejected)
		{
			PromiseVoidFromArg<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromArg<T>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Action<T> onResolved, Action<TException> onRejected) where TException : Exception
		{
			PromiseVoidFromArg<T, TException> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromArg<T, TException>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected)
		{
			PromiseArgFromArgResult<T, TResult> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromArgResult<T, TResult>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TException>(Func<T, TResult> onResolved, Func<TException, TResult> onRejected) where TException : Exception
		{
			PromiseArgFromArgResult<T, TResult, TException> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromArgResult<T, TResult, TException>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected)
		{
			PromiseVoidFromPromiseArgResult<T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseArgResult<T>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Func<T, Promise> onResolved, Func<TException, Promise> onRejected) where TException : Exception
		{
			PromiseVoidFromPromiseArgResult<T, TException> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseArgResult<T, TException>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
		{
			PromiseArgFromPromiseArgResult<TResult, T> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseArgResult<TResult, T>();
			}
			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TException>(Func<T, Promise<TResult>> onResolved, Func<TException, Promise<TResult>> onRejected) where TException : Exception
		{
			PromiseArgFromPromiseArgResult<TResult, T, TException> promise;
			if (!ObjectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseArgResult<TResult, T, TException>();
			}
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
				completePromises.TryGetValue(promise, out callback);
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