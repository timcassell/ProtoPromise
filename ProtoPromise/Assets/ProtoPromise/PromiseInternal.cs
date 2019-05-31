using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	public partial class Promise : IValueContainer, ILinked<Promise>, IPoolable
	{
		public enum GeneratedStacktrace
		{
			/// <summary>
			/// Don't generate any extra stack traces.
			/// </summary>
			None,
			/// <summary>
			/// Generate stack traces when Deferred.Reject is called.
			/// If Reject is called with an exception, the generated stack trace is appended to the exception's stacktrace.
			/// </summary>
			Rejections,
			/// <summary>
			/// Generate stack traces when Deferred.Reject is called.
			/// Also generate stack traces every time a promise is created (i.e. with .Then). This can help debug where an invalid object was returned from a .Then delegate.
			/// NOTE: This can be extremely expensive, so you should only enable this if you ran into an error and you are not sure where it came from.
			/// </summary>
			All
		}

		internal Promise _nextInternal;
		Promise ILinked<Promise>.Next { get { return _nextInternal; } set { _nextInternal = value; } }

		// This is necessary to override in order to use the generic add function.
		protected virtual void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		// Dictionaries to use less memory for expected less used functions.
		private static Dictionary<Promise, FinallyPromise> finals = new Dictionary<Promise, FinallyPromise>();
		private static Dictionary<Promise, ValueLinkedQueue<IDelegate>> cancels = new Dictionary<Promise, ValueLinkedQueue<IDelegate>>();

		// TODO: rip this out.
		private static Dictionary<Promise, Action> completeVoids = new Dictionary<Promise, Action>();

#if DEBUG
		public static GeneratedStacktrace DebugStacktraceGenerator { get; set; }
#else
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
        public static GeneratedStacktrace DebugStacktraceGenerator { get { return default(GeneratedStacktrace); } set { } }
#pragma warning restore RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
#endif

        internal void ResetInternal(int skipFrames = 3)
		{
			_state = PromiseState.Pending;
			done = false;
			previous = null;
			poolOptsInternal = (short) PromisePool.poolType; // Set to -1 to opt in if the poolType is OptOut.
			rejectedOrCanceledValueInternal = null;

#if DEBUG
			if (DebugStacktraceGenerator == GeneratedStacktrace.All)
			{
				createdStackTrace = GetStackTrace(skipFrames);
			}
#endif
		}

#if DEBUG
		protected void Validate(Promise other)
		{
			if (other == null)
			{
				// Returning a null from the callback is not allowed.
				throw new InvalidReturnException()
					.SetMessage("A null promise was returned.");
			}

			// A promise cannot wait on itself.
			for (var prev = other; prev != null; prev = prev.previous)
			{
				if (prev == this)
				{
					throw new InvalidReturnException()
						.SetMessage(string.Format("Circular Promise chain detected."))
						.SetStackTrace(other.createdStackTrace);
				}
			}
		}

		protected void Validate(Delegate other)
		{
			if (other == null)
			{
				// Returning a null from the callback is not allowed.
				throw new InvalidReturnException()
					.SetMessage("A null delegate was returned.");
			}
		}

		internal string createdStackTrace;
		private static int idCounter = 0;
		protected readonly int id;

		public override string ToString()
		{
			return string.Format("Type: Promise, Id: {0}, State: {1}", id, State);
		}
#else
		public override string ToString()
		{
			return string.Format("Type: Promise, State: {0}", State);
		}
#endif

		protected bool CantPool { get { return poolOptsInternal >= 0; } }

		void IPoolable.OptIn()
		{
			checked
			{
				--poolOptsInternal;
			}
			if (poolOptsInternal == -1 && _state != PromiseState.Pending && done)
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

		internal UnhandledException rejectedOrCanceledValueInternal;

		internal ValueLinkedQueue<Promise> NextBranchesInternal = new ValueLinkedQueue<Promise>();
		internal ADeferred deferredInternal;

		private ushort nextCount; // This is a ushort to conserve memory footprint. Change this to uint or ulong if you need to use .Then or .Catch on one promise more than 65,535 times (branching, not chaining).
		private short poolOptsInternal; // This is a short to conserve memory footprint. Change this to int or long if you need to perpetually use one promise in more than 32,768 places.

		private bool done; // This tells the finally handler whether any more .Then or .Catch will be chained from this promise.
		private bool handling; // This is to handle new callbacks being added from a callback that is being invoked. e.g. promise.Complete(() => { promise.Complete(DoSomethingElse); DoSomething(); })
		private bool wasWaitedOn; // This tells the finally handler that another promise waited on this promise (either by .Then/.Catch from this promise, or by returning this promise in another promise's .Then/.Catch)

		internal PromiseState _state;

		internal Promise()
		{
#if DEBUG
			id = idCounter++;
#endif
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
					if (rejectedOrCanceledValueInternal != null)
					{
						Logger.LogError("A new exception was encountered in a Promise.Complete callback before an old exception was handled." +
									   " The new exception will replace the old exception propagating up the promise chain.\nOld exception:\n" +
									   rejectedOrCanceledValueInternal);
					}
					rejectedOrCanceledValueInternal = new UnhandledExceptionException(e);
				}
			}
			handling = false;
		}

		protected void OnComplete()
		{
			HandleComplete();
			OnFinally();
		}

		private void HandleCancel()
		{
			_state = PromiseState.Canceled;

			ValueLinkedQueue<IDelegate> cancelQueue;
			cancels.TryGetValue(this, out cancelQueue);

			// Note: even though the queue is a value type and is re-written to the dictionary on every delegate add,
			// this will still work because the ILinked.Next is part of the referenced objects.

			for (IDelegate current = cancelQueue.Peek(); current != null; current = current.Next)
			{
				current.TryInvoke(rejectedOrCanceledValueInternal);
				// TODO: add cachedDelegate to pool here.
			}

			cancels.Remove(this);
			OnFinally();
		}

		internal Promise ResolveInternal(IValueContainer feed)
		{
			handling = true;
			_state = PromiseState.Resolved;
			Promise promise = null;
			try
			{
				promise = ResolveProtectedInternal(feed);
			}
			catch (Exception e)
			{
				_state = PromiseState.Resolved; // In case the callback throws an exception before returning a promise.
				var ex = new UnhandledExceptionException(e);
#if DEBUG
				ex.SetStackTrace(createdStackTrace);
#endif
				rejectedOrCanceledValueInternal = ex;
			}
			handling = false;
			OnComplete();
			return promise;
		}

		internal virtual Promise ResolveProtectedInternal(IValueContainer feed) // private protected not supported before c# 7.2, so must use internal.
		{
			return null;
		}

		internal Promise RejectInternal(UnhandledException rejectVal)
		{
			handling = true;
			_state = PromiseState.Rejected;
			Promise promise = null;
			try
			{
				promise = RejectProtectedInternal(rejectVal);
			}
			catch (Exception e)
			{
				_state = PromiseState.Rejected; // In case the callback throws an exception before returning a promise.
				var ex = new UnhandledExceptionException(e);
#if DEBUG
				ex.SetStackTrace(createdStackTrace);
#endif
				rejectedOrCanceledValueInternal = ex;
			}
			handling = false;
			OnComplete();
			return promise;
		}

		internal virtual Promise RejectProtectedInternal(UnhandledException rejectVal) // private protected not supported before c# 7.2, so must use internal.
		{
			rejectedOrCanceledValueInternal = rejectVal;
			return null;
		}

		public virtual void AdoptState(Promise adoptee)
		{
			_state = adoptee._state;
			rejectedOrCanceledValueInternal = adoptee.rejectedOrCanceledValueInternal;
			adoptee.wasWaitedOn = true;
		}

		protected void HookupNewPromise(Promise newPromise)
		{
			bool shouldContinue = _state != PromiseState.Pending & NextBranchesInternal.IsEmpty;
			AddWaiter(newPromise);
			newPromise.deferredInternal = deferredInternal;
			newPromise.previous = this;
			RemoveFinal(this);

			// Continue handling if this isn't pending and it's not already looping in ContinueHandlingInternal.
			if (shouldContinue)
			{
				ContinueHandlingInternal(this);
			}
		}

		private void AddWaiter(Promise promise)
		{
			checked
			{
				++nextCount;
			}
			wasWaitedOn = true;

			NextBranchesInternal.Enqueue(promise);
		}

		// Handle promises in a breadth-first manner.
		//private static ValueLinkedQueue<Promise> nextHandles;
		
		// This allows infinite .Then/.Catch callbacks, since it avoids recursion.
		internal static void ContinueHandlingInternal(Promise current)
		{
			var nextHandles = new ValueLinkedQueue<Promise>(current);
			//if (nextHandles.IsEmpty)
			//{
			//	nextHandles = new ValueLinkedQueue<Promise>(current);
			//}
			//else
			//{
			//	// ContinueHandlingInternal is running higher in the program stack, so just return after adding to the queue.
			//	nextHandles.EnqueueRisky(current);
			//	return;
			//}

			for (; current != null; current = current._nextInternal)
			{
				Promise next = current.NextBranchesInternal.Peek();
				while (next != null)
				{
					Promise cached = next;
					next = cached._nextInternal;

					if (cached._state == PromiseState.Canceled)
					{
						// If the next promise is already canceled, don't do anything.
						continue;
					}

					// TODO: Handle all cancels before handling other resolves/rejects.
					if (current.State == PromiseState.Canceled)
					{
						// If current is canceled, cancel its branches.
						cached.rejectedOrCanceledValueInternal = current.rejectedOrCanceledValueInternal;
						cached.HandleCancel();
						goto EnqueueAndContinue;
					}

					// Resolve or reject the next promise.
					UnhandledException rejectVal = current.rejectedOrCanceledValueInternal;
					Promise waitPromise = rejectVal == null ? cached.ResolveInternal(current) : cached.RejectInternal(rejectVal);

					// Did the .Then/.Catch callback return a promise?
					if (waitPromise != null)
					{
						switch (waitPromise._state)
						{
							case PromiseState.Pending:
							{
								waitPromise.AddWaiter(cached);
								continue;
							}
							case PromiseState.Canceled:
							{
								cached.HandleCancel();
								break;
							}
							default:
							{
								cached.AdoptState(waitPromise);
								break;
							}
						}
					}

					EnqueueAndContinue:
					nextHandles.EnqueueRisky(cached);
				}
				current.NextBranchesInternal.Clear();
			}
			nextHandles.Clear();
		}

		private static readonly UnityEngine.WaitForEndOfFrame waitForEndOfFrame = new UnityEngine.WaitForEndOfFrame();
		private static bool handlingFinally = false;
		private static HashSet<Promise> finallies = new HashSet<Promise>();
		private static HashSet<Promise> finallies2 = new HashSet<Promise>();

		private static void HandleFinals()
		{
			handlingFinally = false;
			HashSet<Promise> tempFinals = finallies;
			finallies = finallies2;
			finallies2 = tempFinals;

			ValueLinkedQueue<Promise> finalPromises = new ValueLinkedQueue<Promise>();

			ValueLinkedStack<UnhandledException> exceptions = new ValueLinkedStack<UnhandledException>();
			ValueLinkedQueue<UnhandledException> rejections = new ValueLinkedQueue<UnhandledException>();
			foreach (Promise promise in tempFinals)
			{
				promise.done |= promise.wasWaitedOn | AutoDone;
				if (!promise.done)
				{
					// Only resolve final and throw uncaught exceptions if promise is marked done or another promise waited on it.
					continue;
				}

				FinallyPromise final;
				if (finals.TryGetValue(promise, out final))
				{
					final.ResolveInternal();
					finalPromises.Enqueue(final);
				}

				for (Promise prev = promise.previous; prev != null; prev = prev.previous)
				{
					if (--prev.nextCount == 0)
					{
						prev.done = true;
						if (finals.TryGetValue(prev, out final))
						{
							final.ResolveInternal();
							finalPromises.Enqueue(final);
						}
					}

					prev.AddToPool();
				}

				// Don't check reject value if promise was canceled or another promise waited on it.
				if (promise.State != PromiseState.Canceled & !promise.wasWaitedOn)
				{
					var rejection = promise.rejectedOrCanceledValueInternal;
					if (rejection != null && rejection.unhandled) // Don't log the same rejection twice if it wasn't caught in more than one branch.
					{
						rejection.unhandled = false;
						if (rejection is UnhandledExceptionException)
						{
							exceptions.Push(rejection);
						}
						else
						{
							rejections.Enqueue(rejection);
						}
					}
				}
				promise.AddToPool();
			}
			finallies2.Clear();

			// Debug log all the uncaught rejections, then debug log all the uncaught exceptions except the first, then throw the first uncaught exception.
			for (var rejection = rejections.Peek(); rejection != null; rejection = rejection.nextInternal)
			{
				Logger.LogException(rejection);
			}
			if (!exceptions.IsEmpty)
			{
				UnhandledException exception = exceptions.Pop();
				while (!exceptions.IsEmpty)
				{
					Logger.LogException(exception);
					exception = exceptions.Pop();
				}
				throw exception;
			}

			// Handle any .Then/.Catch attached to the finallies.
			var finalPromise = finalPromises.Peek();
			while (finalPromise != null)
			{
				var cachedPromise = finalPromise;
				finalPromise = finalPromise._nextInternal;

				ContinueHandlingInternal(cachedPromise);
			}
		}

		private static void AddFinal(Promise finallyPromise)
		{
			if (!handlingFinally)
			{
				handlingFinally = true;
				GlobalMonoBehaviour.Yield(waitForEndOfFrame, HandleFinals);
			}
			finallies.Add(finallyPromise);
		}

		private static void RemoveFinal(Promise finallyPromise)
		{
			finallies.Remove(finallyPromise);
		}
	}

	public partial class Promise<T> : ILinked<Promise<T>>
	{
		Promise<T> ILinked<Promise<T>>.Next { get { return (Promise<T>) _nextInternal; } set { _nextInternal = value; } }

		internal Promise() : base() { }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		public override sealed void AdoptState(Promise adoptee)
		{
			base.AdoptState(adoptee);
			_valueInternal = ((Promise<T>) adoptee)._valueInternal;
		}

		internal void ResolveInternal(T value)
		{
			_valueInternal = value;

			_state = PromiseState.Resolved;
			OnComplete();
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			_valueInternal = ((IValueContainer<T>) feed).Value;
			return null;
		}

		public override string ToString()
		{
#if DEBUG
			return string.Format("Type: Promise<{0}>, Id: {1}, State: {2}", typeof(T), id, State);
#else
			return string.Format("Type: Promise<{0}>, State: {1}", typeof(T), State);
#endif
		}
	}


	internal sealed class FinallyPromise : Promise, ILinked<FinallyPromise>
	{
		private bool handlingFinals;

		FinallyPromise ILinked<FinallyPromise>.Next { get { return (FinallyPromise) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Action finalHandler;

		internal void HandleFinallies()
		{
			if (handlingFinals)
			{
				// This is already looping higher in the stack, so just return.
				return;
			}
			handlingFinals = true;
			for (Action temp = finalHandler; temp != null; temp = finalHandler) // Keep looping in case more finally callbacks are added from the invoke. This avoids recursion to prevent StackOverflows.
			{
				finalHandler = null;
				try
				{
					temp.Invoke();
				}
				catch (Exception e)
				{
					if (rejectedOrCanceledValueInternal != null)
					{
						Logger.LogError("A new exception was encountered in a Promise.Finally callback before an old exception was handled." +
									   " The new exception will replace the old exception propagating up the final promise chain.\nOld exception:\n" +
									   rejectedOrCanceledValueInternal);
					}
					rejectedOrCanceledValueInternal = new UnhandledExceptionException(e);
				}
			}
			handlingFinals = false;
		}

		internal void ResolveInternal()
		{
			_state = PromiseState.Resolved;
			HandleFinallies();
			OnComplete();
		}
	}

	// Sadly, C# does not allow multi-inheritance. Hence all the copy-pasted code...

#region RejectHandlers
	// Used IFilter and IDelegate(Result) to reduce the amount of classes I would have to generate to handle catches. I'm less concerned about performance for catches since exceptions are expensive anyway.
	internal class PromiseReject : Promise, ILinked<PromiseReject>
	{
		PromiseReject ILinked<PromiseReject>.Next { get { return (PromiseReject) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal IFilter filter;
		internal IDelegate rejectHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			var tempRej = rejectHandler;
			rejectHandler = null;
			var tempFilter = filter;
			filter = null;
			// Try to run rejectVal through the filter, then try to run it through the rejectHandler.
			if ((tempFilter == null || tempFilter.RunThroughFilter(rejectVal)) && tempRej.TryInvoke(rejectVal))
			{
				return null;
			}
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;
			return base.ResolveProtectedInternal(feed);
		}
	}

	internal class PromiseReject<T> : Promise<T>, ILinked<PromiseReject<T>>
	{
		PromiseReject<T> ILinked<PromiseReject<T>>.Next { get { return (PromiseReject<T>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal IFilter filter;
		internal IDelegateResult rejectHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			var tempRej = rejectHandler;
			rejectHandler = null;
			var tempFilter = filter;
			filter = null;
			// Try to run rejectVal through the filter, then try to run it through the rejectHandler.
			if ((tempFilter == null || tempFilter.RunThroughFilter(rejectVal)) && tempRej.TryInvoke(rejectVal, out _valueInternal))
			{
				return null;
			}
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;
			return base.ResolveProtectedInternal(feed);
		}
	}

	internal class PromiseRejectPromise : Promise, ILinked<PromiseRejectPromise>
	{
		PromiseRejectPromise ILinked<PromiseRejectPromise>.Next { get { return (PromiseRejectPromise) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal IFilter filter;
		internal IDelegateResult rejectHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			if (rejectHandler == null)
			{
				// The returned promise is rejecting this.
				return base.RejectProtectedInternal(rejectVal);
			}

			_state = PromiseState.Pending;
			Promise promise = null;
			var tempRej = rejectHandler;
			rejectHandler = null;
			var tempFilter = filter;
			filter = null;
			// Try to run rejectVal through the filter, then try to run it through the rejectHandler.
			if ((tempFilter == null || tempFilter.RunThroughFilter(rejectVal)) && tempRej.TryInvoke(rejectVal, out promise))
			{
#if DEBUG
				Validate(promise);
#endif
				return promise;
			}
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;
			return base.ResolveProtectedInternal(feed);
		}
	}

	internal class PromiseRejectPromise<TPromise> : Promise<TPromise>, ILinked<PromiseRejectPromise<TPromise>>
	{
		PromiseRejectPromise<TPromise> ILinked<PromiseRejectPromise<TPromise>>.Next { get { return (PromiseRejectPromise<TPromise>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal IFilter filter;
		internal IDelegateResult rejectHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			if (rejectHandler == null)
			{
				// The returned promise is rejecting this.
				return base.RejectProtectedInternal(rejectVal);
			}

			_state = PromiseState.Pending;
			Promise<TPromise> promise = null;
			var tempRej = rejectHandler;
			rejectHandler = null;
			var tempFilter = filter;
			filter = null;
			// Try to run rejectVal through the filter, then try to run it through the rejectHandler.
			if ((tempFilter == null || tempFilter.RunThroughFilter(rejectVal)) && tempRej.TryInvoke(rejectVal, out promise))
			{
#if DEBUG
				Validate(promise);
#endif
				return promise;
			}
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;
			return base.ResolveProtectedInternal(feed);
		}
	}

	internal class PromiseRejectDeferred : Promise, ILinked<PromiseRejectDeferred>
	{
		PromiseRejectDeferred ILinked<PromiseRejectDeferred>.Next { get { return (PromiseRejectDeferred) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal IFilter filter;
		internal IDelegateResult rejectHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			if (rejectHandler == null)
			{
				// The returned promise is rejecting this.
				return base.RejectProtectedInternal(rejectVal);
			}

			_state = PromiseState.Pending;
			Action<Deferred> deferred = null;
			var tempRej = rejectHandler;
			rejectHandler = null;
			var tempFilter = filter;
			filter = null;
			// Try to run rejectVal through the filter, then try to run it through the rejectHandler.
			if ((tempFilter == null || tempFilter.RunThroughFilter(rejectVal)) && tempRej.TryInvoke(rejectVal, out deferred))
			{
#if DEBUG
				Validate(deferred);
#endif
				// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
				return New(deferred);
			}
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;
			return base.ResolveProtectedInternal(feed);
		}
	}

	internal class PromiseRejectDeferred<TDeferred> : Promise<TDeferred>, ILinked<PromiseRejectDeferred<TDeferred>>
	{
		PromiseRejectDeferred<TDeferred> ILinked<PromiseRejectDeferred<TDeferred>>.Next { get { return (PromiseRejectDeferred<TDeferred>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal IFilter filter;
		internal IDelegateResult rejectHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			if (rejectHandler == null)
			{
				// The returned promise is rejecting this.
				return base.RejectProtectedInternal(rejectVal);
			}

			_state = PromiseState.Pending;
			Action<Deferred<TDeferred>> deferred = null;
			var tempRej = rejectHandler;
			rejectHandler = null;
			var tempFilter = filter;
			filter = null;
			// Try to run rejectVal through the filter, then try to run it through the rejectHandler.
			if ((tempFilter == null || tempFilter.RunThroughFilter(rejectVal)) && tempRej.TryInvoke(rejectVal, out deferred))
			{
#if DEBUG
				Validate(deferred);
#endif
				// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
				return New(deferred);
			}
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;
			return base.ResolveProtectedInternal(feed);
		}
	}
#endregion

#region NormalCallbacks
	internal sealed class PromiseVoidResolve : Promise, ILinked<PromiseVoidResolve>
	{
		PromiseVoidResolve ILinked<PromiseVoidResolve>.Next { get { return (PromiseVoidResolve) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Action resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			var tempResolve = resolveHandler;
			resolveHandler = null;
			tempResolve.Invoke();
			return null;
		}
	}

	internal sealed class PromiseVoidResolveReject : PromiseReject, ILinked<PromiseVoidResolveReject>
	{
		PromiseVoidResolveReject ILinked<PromiseVoidResolveReject>.Next { get { return (PromiseVoidResolveReject) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Action resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			var tempResolve = resolveHandler;
			resolveHandler = null;
			tempResolve.Invoke();
			return null;
		}
	}


	internal sealed class PromiseArgResolve<TArg> : Promise, ILinked<PromiseArgResolve<TArg>>
	{
		PromiseArgResolve<TArg> ILinked<PromiseArgResolve<TArg>>.Next { get { return (PromiseArgResolve<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Action<TArg> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			var tempResolve = resolveHandler;
			resolveHandler = null;
			tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}

	internal sealed class PromiseArgResolveReject<TArg> : PromiseReject, ILinked<PromiseArgResolveReject<TArg>>
	{
		PromiseArgResolveReject<TArg> ILinked<PromiseArgResolveReject<TArg>>.Next { get { return (PromiseArgResolveReject<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Action<TArg> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			var tempResolve = resolveHandler;
			resolveHandler = null;
			tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}


	internal sealed class PromiseVoidResolve<TResult> : Promise<TResult>, ILinked<PromiseVoidResolve<TResult>>
	{
		PromiseVoidResolve<TResult> ILinked<PromiseVoidResolve<TResult>>.Next { get { return (PromiseVoidResolve<TResult>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TResult> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			var tempResolve = resolveHandler;
			resolveHandler = null;
			_valueInternal = tempResolve.Invoke();
			return null;
		}
	}

	internal sealed class PromiseVoidResolveReject<TResult> : PromiseReject<TResult>, ILinked<PromiseVoidResolveReject<TResult>>
	{
		PromiseVoidResolveReject<TResult> ILinked<PromiseVoidResolveReject<TResult>>.Next { get { return (PromiseVoidResolveReject<TResult>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TResult> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			var tempResolve = resolveHandler;
			resolveHandler = null;
			_valueInternal = tempResolve.Invoke();
			return null;
		}
	}


	internal sealed class PromiseArgResolve<TArg, TResult> : Promise<TResult>, ILinked<PromiseArgResolve<TArg, TResult>>
	{
		PromiseArgResolve<TArg, TResult> ILinked<PromiseArgResolve<TArg, TResult>>.Next { get { return (PromiseArgResolve<TArg, TResult>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TArg, TResult> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			var tempResolve = resolveHandler;
			resolveHandler = null;
			_valueInternal = tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}

	internal sealed class PromiseArgResolveReject<TArg, TResult> : PromiseReject<TResult>, ILinked<PromiseArgResolveReject<TArg, TResult>>
	{
		PromiseArgResolveReject<TArg, TResult> ILinked<PromiseArgResolveReject<TArg, TResult>>.Next { get { return (PromiseArgResolveReject<TArg, TResult>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TArg, TResult> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			var tempResolve = resolveHandler;
			resolveHandler = null;
			_valueInternal = tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}
#endregion

#region PromiseReturns
	internal sealed class PromiseVoidResolvePromise : Promise, ILinked<PromiseVoidResolvePromise>
	{
		PromiseVoidResolvePromise ILinked<PromiseVoidResolvePromise>.Next { get { return (PromiseVoidResolvePromise) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<Promise> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}

			_state = PromiseState.Pending;
			var tempResolve = resolveHandler;
			resolveHandler = null;
			var promise = tempResolve.Invoke();
#if DEBUG
			Validate(promise);
#endif
			return promise;
		}
	}

	internal sealed class PromiseVoidResolveRejectPromise : PromiseRejectPromise, ILinked<PromiseVoidResolveRejectPromise>
	{
		PromiseVoidResolveRejectPromise ILinked<PromiseVoidResolveRejectPromise>.Next { get { return (PromiseVoidResolveRejectPromise) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<Promise> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;


			_state = PromiseState.Pending;
			var tempResolve = resolveHandler;
			resolveHandler = null;
			var promise = tempResolve.Invoke();
#if DEBUG
			Validate(promise);
#endif
			return promise;
		}
	}


	internal sealed class PromiseArgResolvePromise<TArg> : Promise, ILinked<PromiseArgResolvePromise<TArg>>
	{
		PromiseArgResolvePromise<TArg> ILinked<PromiseArgResolvePromise<TArg>>.Next { get { return (PromiseArgResolvePromise<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TArg, Promise> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}

			_state = PromiseState.Pending;
			var tempResolve = resolveHandler;
			resolveHandler = null;
			var promise = tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
			Validate(promise);
#endif
			return promise;
		}
	}

	internal sealed class PromiseArgResolveRejectPromise<TArg> : PromiseRejectPromise, ILinked<PromiseArgResolveRejectPromise<TArg>>
	{
		PromiseArgResolveRejectPromise<TArg> ILinked<PromiseArgResolveRejectPromise<TArg>>.Next { get { return (PromiseArgResolveRejectPromise<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TArg, Promise> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			_state = PromiseState.Pending;
			var tempResolve = resolveHandler;
			resolveHandler = null;
			var promise = tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
			Validate(promise);
#endif
			return promise;
		}
	}


	internal sealed class PromiseVoidResolvePromise<TPromise> : Promise<TPromise>, ILinked<PromiseVoidResolvePromise<TPromise>>
	{
		PromiseVoidResolvePromise<TPromise> ILinked<PromiseVoidResolvePromise<TPromise>>.Next { get { return (PromiseVoidResolvePromise<TPromise>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<Promise<TPromise>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}
			_state = PromiseState.Pending;
			var tempResolve = resolveHandler;
			resolveHandler = null;
			var promise = tempResolve.Invoke();
#if DEBUG
			Validate(promise);
#endif
			return promise;
		}
	}

	internal sealed class PromiseVoidResolveRejectPromise<TPromise> : PromiseRejectPromise<TPromise>, ILinked<PromiseVoidResolveRejectPromise<TPromise>>
	{
		PromiseVoidResolveRejectPromise<TPromise> ILinked<PromiseVoidResolveRejectPromise<TPromise>>.Next { get { return (PromiseVoidResolveRejectPromise<TPromise>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<Promise<TPromise>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			_state = PromiseState.Pending;
			var tempResolve = resolveHandler;
			resolveHandler = null;
			var promise = tempResolve.Invoke();
#if DEBUG
			Validate(promise);
#endif
			return promise;
		}
	}


	internal sealed class PromiseArgResolvePromise<TArg, TPRomise> : Promise<TPRomise>, ILinked<PromiseArgResolvePromise<TArg, TPRomise>>
	{
		PromiseArgResolvePromise<TArg, TPRomise> ILinked<PromiseArgResolvePromise<TArg, TPRomise>>.Next { get { return (PromiseArgResolvePromise<TArg, TPRomise>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TArg, Promise<TPRomise>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}

			_state = PromiseState.Pending;
			var tempResolve = resolveHandler;
			resolveHandler = null;
			var promise = tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
			Validate(promise);
#endif
			return promise;
		}
	}

	internal sealed class PromiseArgResolveRejectPromise<TArg, TPromise> : PromiseRejectPromise<TPromise>, ILinked<PromiseArgResolveRejectPromise<TArg, TPromise>>
	{
		PromiseArgResolveRejectPromise<TArg, TPromise> ILinked<PromiseArgResolveRejectPromise<TArg, TPromise>>.Next { get { return (PromiseArgResolveRejectPromise<TArg, TPromise>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TArg, Promise<TPromise>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			_state = PromiseState.Pending;
			var tempResolve = resolveHandler;
			resolveHandler = null;
			var promise = tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
			Validate(promise);
#endif
			return promise;
		}
	}
#endregion

#region DeferredReturns
	internal sealed class PromiseVoidResolveDeferred : Promise, ILinked<PromiseVoidResolveDeferred>
	{
		PromiseVoidResolveDeferred ILinked<PromiseVoidResolveDeferred>.Next { get { return (PromiseVoidResolveDeferred) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<Action<Deferred>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}

			_state = PromiseState.Pending;
			Action<Deferred> deferred = null;
			var temp = resolveHandler;
			resolveHandler = null;
			deferred = temp.Invoke();
#if DEBUG
			Validate(deferred);
#endif
			// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
			var promise = New(deferred);
			return promise;
		}
	}

	internal sealed class PromiseVoidResolveRejectDeferred : PromiseRejectDeferred, ILinked<PromiseVoidResolveRejectDeferred>
	{
		PromiseVoidResolveRejectDeferred ILinked<PromiseVoidResolveRejectDeferred>.Next { get { return (PromiseVoidResolveRejectDeferred) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<Action<Deferred>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			_state = PromiseState.Pending;
			Action<Deferred> deferred = null;
			var temp = resolveHandler;
			resolveHandler = null;
			deferred = temp.Invoke();
#if DEBUG
			Validate(deferred);
#endif
			// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
			var promise = New(deferred);
			return promise;
		}
	}


	internal sealed class PromiseVoidResolveDeferred<TDeferred> : Promise<TDeferred>, ILinked<PromiseVoidResolveDeferred<TDeferred>>
	{
		PromiseVoidResolveDeferred<TDeferred> ILinked<PromiseVoidResolveDeferred<TDeferred>>.Next { get { return (PromiseVoidResolveDeferred<TDeferred>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<Action<Deferred<TDeferred>>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}

			_state = PromiseState.Pending;
			Action<Deferred<TDeferred>> deferred = null;
			var temp = resolveHandler;
			resolveHandler = null;
			deferred = temp.Invoke();
#if DEBUG
			Validate(deferred);
#endif
			// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
			var promise = New(deferred);
			return promise;
		}
	}

	internal sealed class PromiseVoidResolveRejectDeferred<TDeferred> : PromiseRejectDeferred<TDeferred>, ILinked<PromiseVoidResolveRejectDeferred<TDeferred>>
	{
		PromiseVoidResolveRejectDeferred<TDeferred> ILinked<PromiseVoidResolveRejectDeferred<TDeferred>>.Next { get { return (PromiseVoidResolveRejectDeferred<TDeferred>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<Action<Deferred<TDeferred>>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			_state = PromiseState.Pending;
			Action<Deferred<TDeferred>> deferred = null;
			var temp = resolveHandler;
			resolveHandler = null;
			deferred = temp.Invoke();
#if DEBUG
			Validate(deferred);
#endif
			// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
			var promise = New(deferred);
			return promise;
		}
	}


	internal sealed class PromiseArgResolveDeferred<TArg> : Promise, ILinked<PromiseArgResolveDeferred<TArg>>
	{
		PromiseArgResolveDeferred<TArg> ILinked<PromiseArgResolveDeferred<TArg>>.Next { get { return (PromiseArgResolveDeferred<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TArg, Action<Deferred>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}

			_state = PromiseState.Pending;
			Action<Deferred> deferred = null;
			var temp = resolveHandler;
			resolveHandler = null;
			deferred = temp.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
			Validate(deferred);
#endif
			// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
			var promise = New(deferred);
			return promise;
		}
	}

	internal sealed class PromiseArgResolveRejectDeferred<TArg> : PromiseRejectDeferred, ILinked<PromiseArgResolveRejectDeferred<TArg>>
	{
		PromiseArgResolveRejectDeferred<TArg> ILinked<PromiseArgResolveRejectDeferred<TArg>>.Next { get { return (PromiseArgResolveRejectDeferred<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TArg, Action<Deferred>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			_state = PromiseState.Pending;
			Action<Deferred> deferred = null;
			var temp = resolveHandler;
			resolveHandler = null;
			deferred = temp.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
			Validate(deferred);
#endif
			// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
			var promise = New(deferred);
			return promise;
		}
	}


	internal sealed class PromiseArgResolveDeferred<TArg, TDeferred> : Promise<TDeferred>, ILinked<PromiseArgResolveDeferred<TArg, TDeferred>>
	{
		PromiseArgResolveDeferred<TArg, TDeferred> ILinked<PromiseArgResolveDeferred<TArg, TDeferred>>.Next { get { return (PromiseArgResolveDeferred<TArg, TDeferred>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TArg, Action<Deferred<TDeferred>>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}

			_state = PromiseState.Pending;
			Action<Deferred<TDeferred>> deferred = null;
			var temp = resolveHandler;
			resolveHandler = null;
			deferred = temp.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
			Validate(deferred);
#endif
			// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
			var promise = New(deferred);
			return promise;
		}
	}

	internal sealed class PromiseArgResolveRejectDeferred<TArg, TDeferred> : PromiseRejectDeferred<TDeferred>, ILinked<PromiseArgResolveRejectDeferred<TArg, TDeferred>>
	{
		PromiseArgResolveRejectDeferred<TArg, TDeferred> ILinked<PromiseArgResolveRejectDeferred<TArg, TDeferred>>.Next { get { return (PromiseArgResolveRejectDeferred<TArg, TDeferred>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPoolInternal.AddInternal(this);
		}

		internal Func<TArg, Action<Deferred<TDeferred>>> resolveHandler;

		internal override Promise RejectProtectedInternal(UnhandledException rejectVal)
		{
			resolveHandler = null;
			return base.RejectProtectedInternal(rejectVal);
		}

		internal override Promise ResolveProtectedInternal(IValueContainer feed)
		{
			if (resolveHandler == null)
			{
				// The returned promise is resolving this.
				return base.ResolveProtectedInternal(feed);
			}
			// Null out the reject delegates
			filter = null;
			rejectHandler = null;

			_state = PromiseState.Pending;
			Action<Deferred<TDeferred>> deferred = null;
			var temp = resolveHandler;
			resolveHandler = null;
			deferred = temp.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
			Validate(deferred);
#endif
			// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
			var promise = New(deferred);
			return promise;
		}
	}
#endregion
}