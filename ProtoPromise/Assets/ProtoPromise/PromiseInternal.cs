using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	public partial class Promise
	{
		// This allows infinite .Then/.Catch callbacks, since it avoids recursion.
		internal static void ContinueHandlingInternal(Promise current)
		{
			ValueLinkedQueue<Promise> nextHandles = new ValueLinkedQueue<Promise>(current);
			for (; current != null; current = current._nextInternal)
			{
				LinkedQueue<Promise> branches = current.NextBranchesInternal;
				for (Promise next = branches.Peek(); next != null;)
				{
					Promise cachedPromise = next;
					next = next._nextInternal;
					PromiseWaitHelper waitPromise = cachedPromise.HandleInternal(current);

					if (waitPromise == null)
					{
						nextHandles.EnqueueRisky(cachedPromise);
					}
					else
					{
						switch (waitPromise.State)
						{
							case PromiseState.Pending:
							{
								waitPromise.AddWaiter(cachedPromise);
								break;
							}
							case PromiseState.Canceled:
							{
								cachedPromise.Cancel();
								break;
							}
							default:
							{
								waitPromise.AdoptState(cachedPromise);
								nextHandles.EnqueueRisky(cachedPromise);
								break;
							}
						}
					}
				}
				branches.Clear();
			}
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

			ValueLinkedStack<UnhandledException> exceptions = new ValueLinkedStack<UnhandledException>();
			ValueLinkedQueue<UnhandledException> rejections = new ValueLinkedQueue<UnhandledException>();
			foreach (Promise promise in tempFinals)
			{
				promise.done |= AutoDone;
				if (!promise.done)
				{
					// Only resolve final and throw uncaught exceptions if promise is marked done.
					continue;
				}
				FinallyPromise final;
				if (finals.TryGetValue(promise, out final))
				{
					final.ResolveInternal();
				}

				for (Promise prev = promise.previous; prev != null; prev = prev.previous)
				{
					if (--prev.nextCount == 0)
					{
						prev.done = true;
						if (finals.TryGetValue(prev, out final))
						{
							final.ResolveInternal();
						}
					}

					prev.AddToPool();
				}

				var rejection = promise.rejectedValueInternal;
				if (rejection != null)
				{
					if (rejection is UnhandledExceptionException)
					{
						exceptions.Push(rejection);
					}
					else
					{
						rejections.Enqueue(rejection);
					}
				}
				promise.AddToPool();
			}
			finallies2.Clear();

			// Debug log all the uncaught rejections, then debug log all the uncaught exceptions except the first, then throw the first uncaught exception.
			for (var rejection = rejections.Peek(); rejection != null; rejection = rejection.nextInternal)
			{
				UnityEngine.Debug.LogException(rejection);

			}
			if (!exceptions.IsEmpty)
			{
				UnhandledException exception = exceptions.Pop();
				while (!exceptions.IsEmpty)
				{
					UnityEngine.Debug.LogException(exception);
					exception = exceptions.Pop();
				}
				throw exception;
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


	// This is used to notify the waiting promise that the previous promise has completed.
	// An instance of this class will never be exposed publicly.
	internal class PromiseWaitHelper : Promise, ILinked<PromiseWaitHelper>
	{
		internal static Dictionary<Promise, PromiseWaitHelper> helpers = new Dictionary<Promise, PromiseWaitHelper>();

		PromiseWaitHelper ILinked<PromiseWaitHelper>.Next { get { return (PromiseWaitHelper) _nextInternal; } set { _nextInternal = value; } }

		internal PromiseWaitHelper() : base()
		{
			done = true;
		}

		protected override void AddToPool()
		{
			helpers.Remove(previous);
			objectPool.AddInternal(this);
		}

		public virtual void AdoptState(Promise adopter)
		{
			adopter.State = State;
			adopter.rejectedValueInternal = previous.rejectedValueInternal;
		}

		public void AddWaiter(Promise promise)
		{
			NextBranchesInternal.Enqueue(promise);
		}

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			for (Promise promise = NextBranchesInternal.Peek(); promise != null; promise = promise._nextInternal)
			{
				promise.State = State;
				ContinueHandlingInternal(promise);
			}
			NextBranchesInternal.Clear();
			return null;
		}

		internal override sealed PromiseWaitHelper RejectProtectedInternal(UnhandledException rejectVal)
		{
			for (Promise promise = NextBranchesInternal.Peek(); promise != null; promise = promise._nextInternal)
			{
				promise.State = State;
				promise.rejectedValueInternal = rejectVal;
				ContinueHandlingInternal(promise);
			}
			NextBranchesInternal.Clear();
			return null;
		}

		public override sealed void Cancel()
		{
			State = PromiseState.Canceled;
			for (Promise promise = NextBranchesInternal.Peek(); promise != null; promise = promise._nextInternal)
			{
				promise.Cancel();
			}
			NextBranchesInternal.Clear();
		}
	}

	// This is used to notify the waiting promise that the previous promise has completed.
	// An instance of this class will never be exposed publicly.
	internal sealed class PromiseWaitHelper<T> : PromiseWaitHelper, ILinked<PromiseWaitHelper<T>>
	{
		PromiseWaitHelper<T> ILinked<PromiseWaitHelper<T>>.Next { get { return (PromiseWaitHelper<T>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			helpers.Remove(previous);
			objectPool.AddInternal(this);
		}

		public override void AdoptState(Promise adopter)
		{
			base.AdoptState(adopter);
			((Promise<T>) adopter).ValueInternal = ((Promise<T>) previous).ValueInternal;
		}

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			Promise<T> prevt = (Promise<T>) previous;
			for (Promise promise = NextBranchesInternal.Peek(); promise != null; promise = promise._nextInternal)
			{
				Promise<T> pt = (Promise<T>) promise;

				pt.State = State;
				pt.ValueInternal = prevt.ValueInternal;
				ContinueHandlingInternal(pt);
			}
			NextBranchesInternal.Clear();
			return null;
		}
	}



	// TODO: add exception filters

	internal class PromiseVoidReject : Promise, ILinked<PromiseVoidReject>
	{
		PromiseVoidReject ILinked<PromiseVoidReject>.Next { get { return (PromiseVoidReject) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Action rejectHandler;

		internal override sealed PromiseWaitHelper RejectProtectedInternal(UnhandledException rejectVal)
		{
			rejectHandler.Invoke();
			rejectHandler = null;
			return null;
		}
	}

	internal class PromiseVoidReject<TReject> : Promise, ILinked<PromiseVoidReject<TReject>>
	{
		PromiseVoidReject<TReject> ILinked<PromiseVoidReject<TReject>>.Next { get { return (PromiseVoidReject<TReject>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Action<TReject> rejectHandler;

		internal override sealed PromiseWaitHelper RejectProtectedInternal(UnhandledException rejectVal)
		{
			TReject val;
			if (rejectVal.TryGetValueAs(out val))
			{
				rejectHandler.Invoke(val);
			}
			else
			{
				rejectedValueInternal = rejectVal;
			}
			rejectHandler = null;
			return null;
		}
	}

	internal class PromiseArgReject<TArg> : Promise<TArg>, ILinked<PromiseArgReject<TArg>>
	{
		PromiseArgReject<TArg> ILinked<PromiseArgReject<TArg>>.Next { get { return (PromiseArgReject<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TArg> rejectHandler;

		internal override sealed PromiseWaitHelper RejectProtectedInternal(UnhandledException rejectVal)
		{
			ValueInternal = rejectHandler.Invoke();
			rejectHandler = null;
			return null;
		}

		// Just pass the value through.
		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			ValueInternal = ((IValueContainer<TArg>) feed).Value;
			return null;
		}
	}

	internal class PromiseVoidRejectPromise : Promise, ILinked<PromiseVoidRejectPromise>
	{
		PromiseVoidRejectPromise ILinked<PromiseVoidRejectPromise>.Next { get { return (PromiseVoidRejectPromise) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<Promise> rejectHandler;

		internal override sealed PromiseWaitHelper RejectProtectedInternal(UnhandledException rejectVal)
		{
			State = PromiseState.Pending;
			var temp = rejectHandler;
			rejectHandler = null;
			return WaitHelperInternal(temp.Invoke());
		}
	}

	internal class PromiseArgReject<TReject, TArg> : Promise<TArg>, ILinked<PromiseArgReject<TReject, TArg>>
	{
		PromiseArgReject<TReject, TArg> ILinked<PromiseArgReject<TReject, TArg>>.Next { get { return (PromiseArgReject<TReject, TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TReject, TArg> rejectHandler;

		internal override sealed PromiseWaitHelper RejectProtectedInternal(UnhandledException rejectVal)
		{
			TReject val;
			if (rejectVal.TryGetValueAs(out val))
			{
				ValueInternal = rejectHandler.Invoke(val);
			}
			else
			{
				rejectedValueInternal = rejectVal;
			}
			rejectHandler = null;
			return null;
		}

		// Just pass the value through.
		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			ValueInternal = ((IValueContainer<TArg>) feed).Value;
			return null;
		}
	}

	internal class PromiseArgRejectPromise<TReject> : Promise, ILinked<PromiseArgRejectPromise<TReject>>
	{
		PromiseArgRejectPromise<TReject> ILinked<PromiseArgRejectPromise<TReject>>.Next { get { return (PromiseArgRejectPromise<TReject>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TReject, Promise> rejectHandler;

		internal override sealed PromiseWaitHelper RejectProtectedInternal(UnhandledException rejectVal)
		{
			State = PromiseState.Pending;
			PromiseWaitHelper promise;

			TReject val;
			if (rejectVal.TryGetValueAs(out val))
			{
				promise = WaitHelperInternal(rejectHandler.Invoke(val));
			}
			else
			{
				rejectedValueInternal = rejectVal;
				promise = null;
			}
			rejectHandler = null;
			return promise;
		}
	}

	internal class PromiseArgRejectPromiseT<TArg> : Promise<TArg>, ILinked<PromiseArgRejectPromiseT<TArg>>
	{
		PromiseArgRejectPromiseT<TArg> ILinked<PromiseArgRejectPromiseT<TArg>>.Next { get { return (PromiseArgRejectPromiseT<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<Promise<TArg>> rejectHandler;

		internal override sealed PromiseWaitHelper RejectProtectedInternal(UnhandledException rejectVal)
		{
			State = PromiseState.Pending;
			var temp = rejectHandler;
			rejectHandler = null;
			return WaitHelperInternal(temp.Invoke());
		}

		// Just pass the value through.
		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			ValueInternal = ((IValueContainer<TArg>) feed).Value;
			return null;
		}
	}

	internal class PromiseArgRejectPromiseT<TReject, TArg> : Promise<TArg>, ILinked<PromiseArgRejectPromiseT<TReject, TArg>>
	{
		PromiseArgRejectPromiseT<TReject, TArg> ILinked<PromiseArgRejectPromiseT<TReject, TArg>>.Next { get { return (PromiseArgRejectPromiseT<TReject, TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TReject, Promise<TArg>> rejectHandler;

		internal override sealed PromiseWaitHelper RejectProtectedInternal(UnhandledException rejectVal)
		{
			State = PromiseState.Pending;
			PromiseWaitHelper promise;

			TReject val;
			if (rejectVal.TryGetValueAs(out val))
			{
				promise = WaitHelperInternal(rejectHandler.Invoke(val));
			}
			else
			{
				rejectedValueInternal = rejectVal;
				promise = null;
			}
			rejectHandler = null;
			return promise;
		}

		// Just pass the value through.
		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			ValueInternal = ((IValueContainer<TArg>) feed).Value;
			return null;
		}
	}



	internal sealed class FinallyPromise : Promise, ILinked<FinallyPromise>
	{
		private bool handlingFinals;

		FinallyPromise ILinked<FinallyPromise>.Next { get { return (FinallyPromise) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
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
					if (rejectedValueInternal != null)
					{
						UnityEngine.Debug.LogError("A new exception was encountered in a Promise.Finally callback before an old exception was handled." +
									   " The new exception will replace the old exception propagating up the final promise chain.\nOld exception:\n" +
									   rejectedValueInternal);
					}
					rejectedValueInternal = new UnhandledExceptionException().SetValue(e);
				}
			}
			handlingFinals = false;
		}

		internal void ResolveInternal()
		{
			State = PromiseState.Resolved;
			HandleFinallies();
			OnComplete();
		}
	}


	internal sealed class PromiseFromDeferred : Promise, ILinked<PromiseFromDeferred>
	{
		PromiseFromDeferred ILinked<PromiseFromDeferred>.Next { get { return (PromiseFromDeferred) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<Action<Deferred>> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			var temp = resolveHandler;
			resolveHandler = null;
			// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
			return WaitHelperInternal(New(temp.Invoke()));
		}
	}

	internal sealed class PromiseFromDeferred<T> : Promise<T>, ILinked<PromiseFromDeferred<T>>
	{
		PromiseFromDeferred<T> ILinked<PromiseFromDeferred<T>>.Next { get { return (PromiseFromDeferred<T>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<Action<Deferred<T>>> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(New(temp.Invoke()));
		}
	}

	internal sealed class PromiseFromDeferredT<TValue> : Promise, ILinked<PromiseFromDeferredT<TValue>>
	{
		PromiseFromDeferredT<TValue> ILinked<PromiseFromDeferredT<TValue>>.Next { get { return (PromiseFromDeferredT<TValue>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TValue, Action<Deferred>> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(New(temp.Invoke(((IValueContainer<TValue>) feed).Value)));
		}
	}

	internal sealed class PromiseFromDeferredT<T, TValue> : Promise<T>, ILinked<PromiseFromDeferredT<T, TValue>>
	{
		PromiseFromDeferredT<T, TValue> ILinked<PromiseFromDeferredT<T, TValue>>.Next { get { return (PromiseFromDeferredT<T, TValue>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TValue, Action<Deferred<T>>> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(New(temp.Invoke(((IValueContainer<TValue>) feed).Value)));
		}
	}


	internal sealed class PromiseVoidFromVoidResolve : Promise, ILinked<PromiseVoidFromVoidResolve>
	{
		PromiseVoidFromVoidResolve ILinked<PromiseVoidFromVoidResolve>.Next { get { return (PromiseVoidFromVoidResolve) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Action resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseVoidFromVoid : PromiseVoidReject, ILinked<PromiseVoidFromVoid>
	{
		PromiseVoidFromVoid ILinked<PromiseVoidFromVoid>.Next { get { return (PromiseVoidFromVoid) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Action resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseVoidFromVoid<TReject> : PromiseVoidReject<TReject>, ILinked<PromiseVoidFromVoid<TReject>>
	{
		PromiseVoidFromVoid<TReject> ILinked<PromiseVoidFromVoid<TReject>>.Next { get { return (PromiseVoidFromVoid<TReject>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Action resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}



	internal sealed class PromiseVoidFromArgResolve<TArg> : Promise, ILinked<PromiseVoidFromArgResolve<TArg>>
	{
		PromiseVoidFromArgResolve<TArg> ILinked<PromiseVoidFromArgResolve<TArg>>.Next { get { return (PromiseVoidFromArgResolve<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Action<TArg> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseVoidFromArg<TArg> : PromiseVoidReject, ILinked<PromiseVoidFromArg<TArg>>
	{
		PromiseVoidFromArg<TArg> ILinked<PromiseVoidFromArg<TArg>>.Next { get { return (PromiseVoidFromArg<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Action<TArg> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseVoidFromArg<TArg, TReject> : PromiseVoidReject<TReject>, ILinked<PromiseVoidFromArg<TArg, TReject>>
	{
		PromiseVoidFromArg<TArg, TReject> ILinked<PromiseVoidFromArg<TArg, TReject>>.Next { get { return (PromiseVoidFromArg<TArg, TReject>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Action<TArg> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}



	internal sealed class PromiseArgFromResultResolve<TResult> : Promise<TResult>, ILinked<PromiseArgFromResultResolve<TResult>>
	{
		PromiseArgFromResultResolve<TResult> ILinked<PromiseArgFromResultResolve<TResult>>.Next { get { return (PromiseArgFromResultResolve<TResult>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TResult> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			ValueInternal = resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseArgFromResult<TResult> : PromiseArgReject<TResult>, ILinked<PromiseArgFromResult<TResult>>
	{
		PromiseArgFromResult<TResult> ILinked<PromiseArgFromResult<TResult>>.Next { get { return (PromiseArgFromResult<TResult>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TResult> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			ValueInternal = resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseArgFromResult<TResult, TReject> : PromiseArgReject<TReject, TResult>, ILinked<PromiseArgFromResult<TResult, TReject>>
	{
		PromiseArgFromResult<TResult, TReject> ILinked<PromiseArgFromResult<TResult, TReject>>.Next { get { return (PromiseArgFromResult<TResult, TReject>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TResult> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			ValueInternal = resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}



	internal sealed class PromiseArgFromArgResultResolve<TArg, TResult> : Promise<TResult>, ILinked<PromiseArgFromArgResultResolve<TArg, TResult>>
	{
		PromiseArgFromArgResultResolve<TArg, TResult> ILinked<PromiseArgFromArgResultResolve<TArg, TResult>>.Next { get { return (PromiseArgFromArgResultResolve<TArg, TResult>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TArg, TResult> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			//UnityEngine.Debug.LogError("sending " + ((IValueContainer<TArg>) feed).Value);
			ValueInternal = resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			//UnityEngine.Debug.LogError("receiving " + ValueInternal);
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseArgFromArgResult<TArg, TResult> : PromiseArgReject<TResult>, ILinked<PromiseArgFromArgResult<TArg, TResult>>
	{
		PromiseArgFromArgResult<TArg, TResult> ILinked<PromiseArgFromArgResult<TArg, TResult>>.Next { get { return (PromiseArgFromArgResult<TArg, TResult>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TArg, TResult> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			ValueInternal = resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseArgFromArgResult<TArg, TResult, TReject> : PromiseArgReject<TReject, TResult>, ILinked<PromiseArgFromArgResult<TArg, TResult, TReject>>
	{
		PromiseArgFromArgResult<TArg, TResult, TReject> ILinked<PromiseArgFromArgResult<TArg, TResult, TReject>>.Next { get { return (PromiseArgFromArgResult<TArg, TResult, TReject>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TArg, TResult> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			ValueInternal = resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}



	internal sealed class PromiseVoidFromPromiseResultResolve : Promise, ILinked<PromiseVoidFromPromiseResultResolve>
	{
		PromiseVoidFromPromiseResultResolve ILinked<PromiseVoidFromPromiseResultResolve>.Next { get { return (PromiseVoidFromPromiseResultResolve) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<Promise> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(temp.Invoke());
		}
	}

	internal sealed class PromiseVoidFromPromiseResult : PromiseVoidRejectPromise, ILinked<PromiseVoidFromPromiseResult>
	{
		PromiseVoidFromPromiseResult ILinked<PromiseVoidFromPromiseResult>.Next { get { return (PromiseVoidFromPromiseResult) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<Promise> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(temp.Invoke());
		}
	}

	internal sealed class PromiseVoidFromPromiseResult<TReject> : PromiseArgRejectPromise<TReject>, ILinked<PromiseVoidFromPromiseResult<TReject>>
	{
		PromiseVoidFromPromiseResult<TReject> ILinked<PromiseVoidFromPromiseResult<TReject>>.Next { get { return (PromiseVoidFromPromiseResult<TReject>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<Promise> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(temp.Invoke());
		}
	}



	internal sealed class PromiseVoidFromPromiseArgResultResolve<TArg> : Promise, ILinked<PromiseVoidFromPromiseArgResultResolve<TArg>>
	{
		PromiseVoidFromPromiseArgResultResolve<TArg> ILinked<PromiseVoidFromPromiseArgResultResolve<TArg>>.Next { get { return (PromiseVoidFromPromiseArgResultResolve<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TArg, Promise> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(temp.Invoke(((IValueContainer<TArg>) feed).Value));
		}
	}

	internal sealed class PromiseVoidFromPromiseArgResult<TArg> : PromiseVoidRejectPromise, ILinked<PromiseVoidFromPromiseArgResult<TArg>>
	{
		PromiseVoidFromPromiseArgResult<TArg> ILinked<PromiseVoidFromPromiseArgResult<TArg>>.Next { get { return (PromiseVoidFromPromiseArgResult<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TArg, Promise> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(temp.Invoke(((IValueContainer<TArg>) feed).Value));
		}
	}

	internal sealed class PromiseVoidFromPromiseArgResult<TArg, TReject> : PromiseArgRejectPromise<TReject>, ILinked<PromiseVoidFromPromiseArgResult<TArg, TReject>>
	{
		PromiseVoidFromPromiseArgResult<TArg, TReject> ILinked<PromiseVoidFromPromiseArgResult<TArg, TReject>>.Next { get { return (PromiseVoidFromPromiseArgResult<TArg, TReject>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<TArg, Promise> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(temp.Invoke(((IValueContainer<TArg>) feed).Value));
		}
	}



	internal sealed class PromiseArgFromPromiseResultResolve<TArg> : Promise<TArg>, ILinked<PromiseArgFromPromiseResultResolve<TArg>>
	{
		PromiseArgFromPromiseResultResolve<TArg> ILinked<PromiseArgFromPromiseResultResolve<TArg>>.Next { get { return (PromiseArgFromPromiseResultResolve<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<Promise<TArg>> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(temp.Invoke());
		}
	}

	internal sealed class PromiseArgFromPromiseResult<TArg> : PromiseArgRejectPromiseT<TArg>, ILinked<PromiseArgFromPromiseResult<TArg>>
	{
		PromiseArgFromPromiseResult<TArg> ILinked<PromiseArgFromPromiseResult<TArg>>.Next { get { return (PromiseArgFromPromiseResult<TArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<Promise<TArg>> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(temp.Invoke());
		}
	}

	internal sealed class PromiseArgFromPromiseResult<TArg, TReject> : PromiseArgRejectPromiseT<TReject, TArg>, ILinked<PromiseArgFromPromiseResult<TArg, TReject>>
	{
		PromiseArgFromPromiseResult<TArg, TReject> ILinked<PromiseArgFromPromiseResult<TArg, TReject>>.Next { get { return (PromiseArgFromPromiseResult<TArg, TReject>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<Promise<TArg>> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return WaitHelperInternal(temp.Invoke());
		}
	}



	internal sealed class PromiseArgFromPromiseArgResultResolve<TArg, PArg> : Promise<TArg>, ILinked<PromiseArgFromPromiseArgResultResolve<TArg, PArg>>
	{
		PromiseArgFromPromiseArgResultResolve<TArg, PArg> ILinked<PromiseArgFromPromiseArgResultResolve<TArg, PArg>>.Next { get { return (PromiseArgFromPromiseArgResultResolve<TArg, PArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<PArg, Promise<TArg>> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return WaitHelperInternal(resolveHandler.Invoke(((IValueContainer<PArg>) feed).Value));
		}
	}

	internal sealed class PromiseArgFromPromiseArgResult<TArg, PArg> : PromiseArgRejectPromiseT<TArg>, ILinked<PromiseArgFromPromiseArgResult<TArg, PArg>>
	{
		PromiseArgFromPromiseArgResult<TArg, PArg> ILinked<PromiseArgFromPromiseArgResult<TArg, PArg>>.Next { get { return (PromiseArgFromPromiseArgResult<TArg, PArg>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<PArg, Promise<TArg>> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return WaitHelperInternal(resolveHandler.Invoke(((IValueContainer<PArg>) feed).Value));
		}
	}

	internal sealed class PromiseArgFromPromiseArgResult<TArg, PArg, TReject> : PromiseArgRejectPromiseT<TReject, TArg>, ILinked<PromiseArgFromPromiseArgResult<TArg, PArg, TReject>>
	{
		PromiseArgFromPromiseArgResult<TArg, PArg, TReject> ILinked<PromiseArgFromPromiseArgResult<TArg, PArg, TReject>>.Next { get { return (PromiseArgFromPromiseArgResult<TArg, PArg, TReject>) _nextInternal; } set { _nextInternal = value; } }

		protected override void AddToPool()
		{
			if (CantPool) return;
			objectPool.AddInternal(this);
		}

		internal Func<PArg, Promise<TArg>> resolveHandler;

		internal override PromiseWaitHelper ResolveProtectedInternal(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return WaitHelperInternal(resolveHandler.Invoke(((IValueContainer<PArg>) feed).Value));
		}
	}
}