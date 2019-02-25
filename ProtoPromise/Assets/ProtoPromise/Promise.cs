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

		protected Promise previous;

		internal UnhandledException rejectedValueInternal;
		
		internal LinkedQueue<Promise> NextBranchesInternal = new LinkedQueue<Promise>();
		internal ADeferred DeferredInternal { get; set; }

		private ushort nextCount; // This is a ushort to conserve memory footprint. Change this to uint or ulong if you need to use .Then or .Catch on one promise more than 65,535 times (branching, not chaining).
		private short poolOptsInternal; // This is a short to conserve memory footprint. Change this to int or long if you need to perpetually use one promise in more than 32,768 places.

		protected bool done = false;
		private bool handling = false; // This is to handle new callbacks being added from a callback that is being invoked. e.g. promise.Complete(() => { promise.Complete(DoSomethingElse); DoSomething(); })

		public PromiseState State { get; internal set; }

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
			if (nextCount == 0)
			{
				AddFinal(this);
			}
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
					if (rejectedValueInternal != null)
					{
						UnityEngine.Debug.LogError("A new exception was encountered in a Promise.Complete callback before an old exception was handled." +
									   " The new exception will replace the old exception propagating up the promise chain.\nOld exception:\n" +
									   rejectedValueInternal);
					}
					rejectedValueInternal = new UnhandledExceptionException().SetValue(e);
				}
			}
			handling = false;
		}

		protected void OnComplete()
		{
			HandleComplete();
			OnFinally();
		}

		public Promise OnCanceled(Action onCanceled)
		{
			switch (State)
			{
				case PromiseState.Pending:
				{
					Action cancel;
					cancels.TryGetValue(this, out cancel);
					cancel += onCanceled;
					cancels[this] = cancel;
					break;
				}
				case PromiseState.Canceled:
				{
					Action cancel;
					cancels.TryGetValue(this, out cancel);
					cancel += onCanceled;
					cancels[this] = cancel;
					HandleCancel();
					break;
				}
			}
			return this;
		}

		/// <summary>
		/// Cancels this promise and all .Then/.Catch promises that have been chained from this.
		/// Does nothing if this promise isn't pending.
		/// </summary>
		public virtual void Cancel()
		{
			if (State != PromiseState.Pending)
			{
				return;
			}
			State = PromiseState.Canceled;
			//if (previous != null)
			//{
			//	--previous.nextCount; // Just mark the count as if this was removed, but don't actually remove this from the next branches (because would be O(N) operation).
			//}
			NextBranchesInternal.Clear();
			nextCount = 0;
			// TODO: cancel all branches promises as well.
			HandleCancel();
			OnFinally();
		}

		private void HandleCancel()
		{
			if (handling)
			{
				// This is already looping higher in the stack, so just return.
				return;
			}
			handling = true;
			Action cancel;
			while (cancels.TryGetValue(this, out cancel))
			{
				cancels.Remove(this);
				cancel.Invoke();
			}
			handling = false;
		}

		internal PromiseWaitHelper HandleInternal(Promise feed)
		{
			UnhandledException rejectVal = feed.rejectedValueInternal;
			return rejectVal == null ? ResolveInternal(feed) : RejectInternal(rejectVal);
		}

		internal PromiseWaitHelper ResolveInternal(IValueContainer feed)
		{
			handling = true;
			State = PromiseState.Resolved;
			PromiseWaitHelper promise = null;
			try
			{
				promise = ResolveProtectedInternal(feed);
			}
			catch (Exception e)
			{
				rejectedValueInternal = new UnhandledExceptionException().SetValue(e);
			}
			handling = false;
			OnComplete();
			return promise;
		}

		internal virtual PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed) // private protected not supported before c# 7.2, so must use internal.
		{
			return null;
		}

		internal PromiseWaitHelper RejectInternal(UnhandledException rejectVal)
		{
			handling = true;
			State = PromiseState.Rejected;
			PromiseWaitHelper promise = null;
			try
			{
				promise = RejectProtectedInternal(rejectVal);
			}
			catch (Exception e)
			{
				rejectedValueInternal = new UnhandledExceptionException().SetValue(e);
			}
			handling = false;
			OnComplete();
			return promise;
		}

		internal virtual PromiseWaitHelper RejectProtectedInternal(UnhandledException rejectVal) // private protected not supported before c# 7.2, so must use internal.
		{
			rejectedValueInternal = rejectVal;
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
			if (!handling)
			{
				switch(State)
				{
					case PromiseState.Rejected:
					case PromiseState.Resolved:
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
			switch (promise.State)
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

		public Promise Complete(Action onComplete)
		{
			Action temp;
			completeVoids.TryGetValue(this, out temp);
			temp += onComplete;
			completeVoids[this] = temp;
			switch (State)
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

		protected void HookupNewPromise(Promise newPromise)
		{
			checked
			{
				++nextCount;
			}
			newPromise.DeferredInternal = DeferredInternal;
			newPromise.previous = this;
			RemoveFinal(this);

			if (State == PromiseState.Pending || NextBranchesInternal.Peek() != null)
			{
				NextBranchesInternal.Enqueue(newPromise);
			}
			else
			{
				NextBranchesInternal.Enqueue(newPromise);
				ContinueHandlingInternal(this);
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

		public Promise Catch<TReject>(Action<TReject> onRejected)
		{
			PromiseVoidReject<TReject> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidReject<TReject>();
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

		public Promise Catch<TReject>(Func<TReject, Promise> onRejected)
		{
			PromiseArgRejectPromise<TReject> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgRejectPromise<TReject>();
			}
			promise.ResetInternal();

			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		// TODO: Implement deferred catches.
		//public Promise Catch(Func<Action<Deferred>> onRejected)
		//{
		//	PromiseVoidRejectPromise promise;
		//	if (!objectPool.TryTakeInternal(out promise))
		//	{
		//		promise = new PromiseVoidRejectPromise();
		//	}
		//	promise.ResetInternal();

		//	promise.rejectHandler = onRejected;
		//	HookupNewPromise(promise);
		//	return promise;
		//}


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

		public Promise Then<TReject>(Action onResolved, Action<TReject> onRejected)
		{
			PromiseVoidFromVoid<TReject> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromVoid<TReject>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		// TODO
		//public Promise Then<TReject>(Action onResolved, Func<TReject, Promise> onRejected)
		//{
		//	PromiseVoidFromVoid<TReject> promise;
		//	if (!objectPool.TryTakeInternal(out promise))
		//	{
		//		promise = new PromiseVoidFromVoid<TReject>();
		//	}
		//	promise.ResetInternal();

		//	promise.resolveHandler = onResolved;
		//	promise.rejectHandler = onRejected;
		//	HookupNewPromise(promise);
		//	return promise;
		//}

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

		public Promise<T> Then<T, TReject>(Func<T> onResolved, Func<TReject, T> onRejected)
		{
			PromiseArgFromResult<T, TReject> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromResult<T, TReject>();
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

		public Promise Then<TReject>(Func<Promise> onResolved, Func<TReject, Promise> onRejected)
		{
			PromiseVoidFromPromiseResult<TReject> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseResult<TReject>();
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

		public Promise<T> Then<T, TReject>(Func<Promise<T>> onResolved, Func<TReject, Promise<T>> onRejected)
		{
			PromiseArgFromPromiseResult<T, TReject> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseResult<T, TReject>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		internal PromiseWaitHelper WaitHelperInternal(Promise promise)
		{
			// If promise already has a wait helper attached, just return it.
			PromiseWaitHelper waitHelper;
			if (!PromiseWaitHelper.helpers.TryGetValue(promise, out waitHelper))
			{
				if (!objectPool.TryTakeInternal(out waitHelper))
				{
					waitHelper = new PromiseWaitHelper();
				}
				waitHelper.ResetInternal();

				promise.HookupNewPromise(waitHelper);
			}
			return waitHelper;
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

		internal T ValueInternal { get; set; }
		T IValueContainer<T>.Value { get { return ValueInternal; } }

		public new Promise<T> OnCanceled(Action onCanceled)
		{
			base.OnCanceled(onCanceled);
			return this;
		}

		internal void ResolveInternal(T value)
		{
			ValueInternal = value;

			State = PromiseState.Resolved;
			OnComplete();
		}

		//internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		//{
		//	Value = ((IValueContainer<T>) feed).Value;
		//	return null;
		//}

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

		public Promise<T> Catch<TReject>(Func<TReject, T> onRejected)
		{
			PromiseArgReject<TReject, T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgReject<TReject, T>();
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

		public Promise<T> Catch<TReject>(Func<TReject, Promise<T>> onRejected)
		{
			PromiseArgRejectPromiseT<TReject, T> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgRejectPromiseT<TReject, T>();
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

		public Promise Then<TReject>(Action<T> onResolved, Action<TReject> onRejected)
		{
			PromiseVoidFromArg<T, TReject> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromArg<T, TReject>();
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

		public Promise<TResult> Then<TResult, TReject>(Func<T, TResult> onResolved, Func<TReject, TResult> onRejected)
		{
			PromiseArgFromArgResult<T, TResult, TReject> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromArgResult<T, TResult, TReject>();
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

		public Promise Then<TReject>(Func<T, Promise> onResolved, Func<TReject, Promise> onRejected)
		{
			PromiseVoidFromPromiseArgResult<T, TReject> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseVoidFromPromiseArgResult<T, TReject>();
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

		public Promise<TResult> Then<TResult, TReject>(Func<T, Promise<TResult>> onResolved, Func<TReject, Promise<TResult>> onRejected)
		{
			PromiseArgFromPromiseArgResult<TResult, T, TReject> promise;
			if (!objectPool.TryTakeInternal(out promise))
			{
				promise = new PromiseArgFromPromiseArgResult<TResult, T, TReject>();
			}
			promise.ResetInternal();

			promise.resolveHandler = onResolved;
			promise.rejectHandler = onRejected;
			HookupNewPromise(promise);
			return promise;
		}

		internal PromiseWaitHelper WaitHelperInternal(Promise<T> promise)
		{
			// If promise already has a wait helper attached, just return it.
			PromiseWaitHelper waitHelper;
			if (!PromiseWaitHelper.helpers.TryGetValue(promise, out waitHelper))
			{
				PromiseWaitHelper<T> waitHelperT;
				if (!objectPool.TryTakeInternal(out waitHelperT))
				{
					waitHelperT = new PromiseWaitHelper<T>();
				}
				waitHelper = waitHelperT;
				waitHelper.ResetInternal();

				promise.HookupNewPromise(waitHelper);
			}
			return waitHelper;
		}
	}
}