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
		Canceled // This violates Promises/A+ API, but I felt its usefulness outweighs API adherence.
	}

	// If using Unity prior to 5.3, remove "UnityEngine.CustomYieldInstruction". Instead, you can wait for a promise to complete in a coroutine this way:
	// do { yield return null; } while(promise.State == PromiseState.Pending);
	public partial class Promise : UnityEngine.CustomYieldInstruction, IValueContainer, ILinked<Promise>, IPoolable
	{
		internal Promise _nextInternal;
		Promise ILinked<Promise>.Next { get { return _nextInternal; } set { _nextInternal = value; } }

		// This is necessary to override in order to use the generic add function.
		protected virtual void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		// Dictionaries to use less memory for expected less used functions.
		private static Dictionary<Promise, FinallyPromise> finals = new Dictionary<Promise, FinallyPromise>();
		private static Dictionary<Promise, Action> completeVoids = new Dictionary<Promise, Action>();
		protected static Dictionary<Promise, Action<Promise>> completePromises = new Dictionary<Promise, Action<Promise>>(); // Used to prevent anonymous closure allocations.
		private static Dictionary<Promise, Action> cancels = new Dictionary<Promise, Action>();


		//private static int idCounter = 0;
		//public readonly int id;

		protected bool CantPool { get { return poolOptsInternal >= 0; }}

		void IPoolable.OptIn()
		{
			checked
			{
				--poolOptsInternal;
			}
			if (poolOptsInternal == -1 && State != PromiseState.Pending && done)
			{
				AddToPool();
			}
		}

		void IPoolable.OptOut()
		{
			checked
			{
				++poolOptsInternal;
			}
		}

		private Promise previous;

		protected UnhandledException rejectedValue;
		
		private LinkedQueue<Promise> NextBranches = new LinkedQueue<Promise>();
		internal ADeferred DeferredInternal { get; set; }

		private ushort nextCount; // This is a ushort to conserve memory footprint. Change this to uint or ulong if you need to use .Then or .Catch on one promise more than 65,535 times (branching, not chaining).
		private short poolOptsInternal; // This is a short to conserve memory footprint. Change this to int or long if you need to perpetually use one promise in more than 32,768 places.

		protected bool done = false;
		private bool handling = false; // This is to handle new callbacks being added from a callback that is being invoked. e.g. promise.Complete(() => { promise.Complete(DoSomethingElse); DoSomething(); })

		public PromiseState State { get; protected set; }

		internal Promise()
		{
			//id = idCounter++;
		}

		internal void ResetInternal()
		{
			poolOptsInternal = 0;
			State = PromiseState.Pending;
			done = false;
			if (PromisePool.poolType == PoolType.OptOut)
			{
				((IPoolable) this).OptIn();
			}
		}

		private void OnFinally()
		{
			if (nextCount > 0)
			{
				return;
			}

			AddFinal(this);
		}

		private void HandleComplete()
		{
			if (handling)
			{
				// This is already looping higher in the stack, so just return.
				return;
			}
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
					if (rejectedValue != null)
					{
						UnityEngine.Debug.LogError("A new exception was encountered in a Promise.Complete callback before an old exception was handled." +
									   " The new exception will replace the old exception propagating up the promise chain.\nOld exception:\n" +
									   rejectedValue);
					}
					rejectedValue = new UnhandledExceptionException().SetValue(e);
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
				if (nextCount == 0 && AutoDone)
				{
					done = true;
					AddFinal(this);
				}
		}

		public Promise OnCanceled(Action onCanceled)
		{
			Action cancel;
			cancels.TryGetValue(this, out cancel);
			cancel += onCanceled;
			cancels[this] = cancel;
			return this;
		}

		/// <summary>
		/// Cancels this promise and all .Then/.Catch promises that have been chained from this.
		/// Does nothing if this promise isn't pending.
		/// </summary>
		public void Cancel()
		{
			if (State != PromiseState.Pending)
			{
				return;
			}
			State = PromiseState.Canceled;

			// TODO
		}

		private void HandleCancel()
		{

		}

		internal Promise HandleInternal(Promise feed)
		{
			UnhandledException rejectVal = feed.rejectedValue;
			return rejectVal == null ? ResolveInternal(feed) : RejectInternal(rejectVal);
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
				rejectedValue = new UnhandledExceptionException().SetValue(e);
			}
			handling = false;
			OnComplete();
			return promise;
		}

		internal virtual Promise ResolveProtected(IValueContainer feed) // private protected not supported before c# 7.2, so must use internal.
		{
			return null;
		}

		internal Promise RejectInternal(UnhandledException rejectVal)
		{
			handling = true;
			State = PromiseState.Rejected;
			Promise promise = null;
			try
			{
				promise = RejectProtected(rejectVal);
			}
			catch (Exception e)
			{
				rejectedValue = new UnhandledExceptionException().SetValue(e);
			}
			handling = false;
			OnComplete();
			return promise;
		}

		protected virtual Promise RejectProtected(UnhandledException rejectVal)
		{
			rejectedValue = rejectVal;
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

		public Promise Done(Action onComplete)
		{
			Complete(onComplete);
			return Done();
		}

		public Promise Done()
		{
			if (done)
			{
				return this;
			}

			done = true;
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
				if (!objectPool.TryTakeInternal(out promise))
				{
					promise = new FinallyPromise();
				}
				promise.DeferredInternal = DeferredInternal;
				promise.ResetInternal();

				finals[this] = promise;
			}
			return promise;
		}

		public Promise Finally(Action onFinally)
		{
			FinallyPromise promise = (FinallyPromise) Finally();
			promise.finalHandler += onFinally;
			if (promise.State != PromiseState.Pending)
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
			if (State != PromiseState.Pending)
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

			if (State == PromiseState.Pending || NextBranches.Peek() != null)
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
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseFromDeferred();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Action<Deferred<T>>> onResolved)
		{
			PromiseFromDeferred<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseFromDeferred<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action onResolved)
		{
			PromiseVoidFromVoidResolve promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromVoidResolve();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved)
		{
			PromiseArgFromResultResolve<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromResultResolve<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved)
		{
			PromiseVoidFromPromiseResultResolve promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseResultResolve();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved)
		{
			PromiseArgFromPromiseResultResolve<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseResultResolve<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		// TODO: add exception filters
		public Promise Catch(Action onRejected)
		{
			PromiseVoidReject promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidReject();
			}
			promise.ResetInternal();

			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TException>(Action<TException> onRejected) where TException : Exception
		{
			PromiseVoidReject<TException> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidReject<TException>();
			}
			promise.ResetInternal();

			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch(Func<Promise> onRejected)
		{
			PromiseVoidRejectPromise promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidRejectPromise();
			}
			promise.ResetInternal();

			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Catch<TException>(Func<TException, Promise> onRejected) where TException : Exception
		{
			PromiseArgRejectPromise<TException> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgRejectPromise<TException>();
			}
			promise.ResetInternal();

			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}


		public Promise Then(Action onResolved, Action onRejected)
		{
			PromiseVoidFromVoid promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromVoid();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Action onResolved, Action<TException> onRejected) where TException : Exception
		{
			PromiseVoidFromVoid<TException> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromVoid<TException>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved, Func<T> onRejected)
		{
			PromiseArgFromResult<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromResult<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TException>(Func<T> onResolved, Func<TException, T> onRejected) where TException : Exception
		{
			PromiseArgFromResult<T, TException> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromResult<T, TException>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<Promise> onResolved, Func<Promise> onRejected)
		{
			PromiseVoidFromPromiseResult promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseResult();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Func<Promise> onResolved, Func<TException, Promise> onRejected) where TException : Exception
		{
			PromiseVoidFromPromiseResult<TException> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseResult<TException>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved, Func<Promise<T>> onRejected)
		{
			PromiseArgFromPromiseResult<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseResult<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Then<T, TException>(Func<Promise<T>> onResolved, Func<TException, Promise<T>> onRejected) where TException : Exception
		{
			PromiseArgFromPromiseResult<T, TException> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseResult<T, TException>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		// This is to avoid closures to save some GC allocations.
		protected void Complete(Action<Promise> onComplete)
		{
			Action<Promise> callback;
			completePromises.TryGetValue(this, out callback);
			callback += onComplete;
			completePromises[this] = callback;
		}

		protected Promise PromiseHelper(Promise promise)
		{
			if (promise.State == PromiseState.Pending)
			{
				promise.Complete(p =>
				{
					State = p.State;
					rejectedValue = p.rejectedValue;
					OnComplete();
				});
			}
			else
			{
				State = promise.State;
				rejectedValue = promise.rejectedValue;
				OnComplete();
			}

			// Manually add a catch in case promise.done is true.
			PromiseReject rejectPromise;
			if (!objectPool.TryTakeInternal(out rejectPromise))
			{
				rejectPromise = new PromiseReject();
			}
			rejectPromise.ResetInternal();

			promise.HookupNewPromise(rejectPromise);

			return rejectPromise;
		}
	}

	public class Promise<T> : Promise, IValueContainer<T>, ILinked<Promise<T>>
	{
		Promise<T> ILinked<Promise<T>>.Next { get { return (Promise<T>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Promise() : base() { }

		protected T Value { get; set; }
		T IValueContainer<T>.Value { get { return Value; } }

		public new Promise<T> OnCanceled(Action onCanceled)
		{
			base.OnCanceled(onCanceled);
			return this;
		}

		internal void ResolveInternal(T value)
		{
			Value = value;

			State = PromiseState.Resolved;
			OnComplete();
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

		public Promise Then(Func<T, Action<Deferred>> onResolved)
		{
			PromiseFromDeferredT<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseFromDeferredT<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Action<Deferred<TResult>>> onResolved)
		{
			PromiseFromDeferredT<TResult, T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseFromDeferredT<TResult, T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Action<T> onResolved)
		{
			PromiseVoidFromArgResolve<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromArgResolve<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
		{
			PromiseArgFromArgResultResolve<T, TResult> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromArgResultResolve<T, TResult>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved)
		{
			PromiseVoidFromPromiseArgResultResolve<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseArgResultResolve<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
		{
			PromiseArgFromPromiseArgResultResolve<TResult, T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseArgResultResolve<TResult, T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<T> onRejected)
		{
			PromiseArgReject<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgReject<T>();
			}
			promise.ResetInternal();

			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TException>(Func<TException, T> onRejected) where TException : Exception
		{
			PromiseArgReject<TException, T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgReject<TException, T>();
			}
			promise.ResetInternal();

			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch(Func<Promise<T>> onRejected)
		{
			PromiseArgRejectPromiseT<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgRejectPromiseT<T>();
			}
			promise.ResetInternal();

			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<T> Catch<TException>(Func<TException, Promise<T>> onRejected) where TException : Exception
		{
			PromiseArgRejectPromiseT<TException, T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgRejectPromiseT<TException, T>();
			}
			promise.ResetInternal();

			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}


		public Promise Then(Action<T> onResolved, Action onRejected)
		{
			PromiseVoidFromArg<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromArg<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Action<T> onResolved, Action<TException> onRejected) where TException : Exception
		{
			PromiseVoidFromArg<T, TException> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromArg<T, TException>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved, Func<TResult> onRejected)
		{
			PromiseArgFromArgResult<T, TResult> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromArgResult<T, TResult>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TException>(Func<T, TResult> onResolved, Func<TException, TResult> onRejected) where TException : Exception
		{
			PromiseArgFromArgResult<T, TResult, TException> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromArgResult<T, TResult, TException>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved, Func<Promise> onRejected)
		{
			PromiseVoidFromPromiseArgResult<T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseArgResult<T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise Then<TException>(Func<T, Promise> onResolved, Func<TException, Promise> onRejected) where TException : Exception
		{
			PromiseVoidFromPromiseArgResult<T, TException> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseArgResult<T, TException>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved, Func<Promise<TResult>> onRejected)
		{
			PromiseArgFromPromiseArgResult<TResult, T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseArgResult<TResult, T>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		public Promise<TResult> Then<TResult, TException>(Func<T, Promise<TResult>> onResolved, Func<TException, Promise<TResult>> onRejected) where TException : Exception
		{
			PromiseArgFromPromiseArgResult<TResult, T, TException> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseArgResult<TResult, T, TException>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		protected Promise PromiseHelper(Promise<T> promise)
		{
			if (promise.State == PromiseState.Pending)
			{
				promise.Complete(p =>
				{
					Promise<T> pt = (Promise<T>) p;
					State = pt.State;
					Value = pt.Value;
					rejectedValue = pt.rejectedValue;
					OnComplete();
				});
			}
			else
			{
				State = promise.State;
				Value = promise.Value;
				rejectedValue = promise.rejectedValue;
				OnComplete();
			}

			// Manually add a catch that does nothing. This makes the waited promise think it had a branch that resolved its rejection.
			PromiseReject rejectPromise;
			if (!objectPool.TryTakeInternal(out rejectPromise))
			{
				rejectPromise = new PromiseReject();
			}
			rejectPromise.ResetInternal();

			promise.HookupNewPromise(rejectPromise);

			return rejectPromise;
		}
	}
}