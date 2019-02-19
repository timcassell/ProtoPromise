using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	public partial class Promise
	{
		// This is to avoid closures to save some GC allocations.
		private static Dictionary<Promise, ValueLinkedQueue<Promise>> continuePromises = new Dictionary<Promise, ValueLinkedQueue<Promise>>();

		private static void ContinueFrom(Promise promise)
		{
			ValueLinkedQueue<Promise> promises = continuePromises[promise];
			for (Promise pr = promises.Peek(); pr != null; pr = pr.NextInternal)
			{
				ContinueHandlingInternal(pr);
			}
			continuePromises.Remove(promise);
		}

		// This allows infinite .Then/.Catch callbacks, since it avoids recursion.
		internal static void ContinueHandlingInternal(Promise current)
		{
			ValueLinkedQueue<Promise> nextHandles = new ValueLinkedQueue<Promise>(current);
			for (; current != null; current = current.NextInternal)
			{
				LinkedQueue<Promise> branches = current.NextBranches;
				for (Promise next = branches.Peek(); next != null;)
				{
					Promise cachedPromise = next;
					next = next.NextInternal;
					Promise waitPromise = cachedPromise.HandleInternal(current);

					if (waitPromise == null || waitPromise.State != PromiseState.Pending)
					{
						nextHandles.EnqueueRisky(cachedPromise);
					}
					else
					{
						ValueLinkedQueue<Promise> waitingPromises;
						bool queued = continuePromises.TryGetValue(waitPromise, out waitingPromises);
						waitingPromises.Enqueue(cachedPromise);
						continuePromises[waitPromise] = waitingPromises;

						if (!queued) // If queued is true, this is already going to continue, so don't subscribe to complete.
						{
							waitPromise.Complete(ContinueFrom);
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

			ValueLinkedQueue<UnhandledException> exceptions = new ValueLinkedQueue<UnhandledException>();
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

					ObjectPool.AddInternal(prev);
				}

				var rejection = promise.rejectedValue;
				if (typeof(UnhandledExceptionException).IsAssignableFrom(typeof(UnhandledExceptionException)))
				{
					exceptions.Enqueue(rejection);
				}
				else
				{
					rejections.Enqueue(rejection);
				}
				ObjectPool.AddInternal(promise);
			}
			finallies2.Clear();

			// Debug log all the uncaught rejections, then throw the first uncaught exception.
			for (var exception = exceptions.Peek(); exception != null; exception = exception.nextInternal)
			{
				UnityEngine.Debug.LogException(exception);

			}
			if (rejections.Peek() != null)
			{
				throw rejections.Peek();
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


	// TODO: add exception filters
	internal class PromiseVoidReject : Promise, ILinked<PromiseVoidReject>
	{
		PromiseVoidReject ILinked<PromiseVoidReject>.Next { get { return (PromiseVoidReject) NextInternal; } set { NextInternal = value; } }

		internal Action rejectHandler;

		public PromiseVoidReject() : base() { }

		protected override sealed Promise RejectProtected(UnhandledException rejectVal)
		{
			rejectHandler.Invoke();
			rejectHandler = null;
			return null;
		}
	}

	internal class PromiseVoidReject<TReject> : Promise, ILinked<PromiseVoidReject<TReject>>
	{
		PromiseVoidReject<TReject> ILinked<PromiseVoidReject<TReject>>.Next { get { return (PromiseVoidReject<TReject>) NextInternal; } set { NextInternal = value; } }

		internal Action<TReject> rejectHandler;

		protected override sealed Promise RejectProtected(UnhandledException rejectVal)
		{
			TReject val;
			if (rejectVal.TryGetValueAs(out val))
			{
				rejectHandler.Invoke(val);
			}
			else
			{
				rejectedValue = rejectVal;
			}
			rejectHandler = null;
			return null;
		}
	}

	internal class PromiseArgReject<TArg> : Promise<TArg>, ILinked<PromiseArgReject<TArg>>
	{
		PromiseArgReject<TArg> ILinked<PromiseArgReject<TArg>>.Next { get { return (PromiseArgReject<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg> rejectHandler;

		protected override sealed Promise RejectProtected(UnhandledException rejectVal)
		{
			Value = rejectHandler.Invoke();
			rejectHandler = null;
			return null;
		}
	}

	internal class PromiseVoidRejectPromise : Promise, ILinked<PromiseVoidRejectPromise>
	{
		PromiseVoidRejectPromise ILinked<PromiseVoidRejectPromise>.Next { get { return (PromiseVoidRejectPromise) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> rejectHandler;

		protected override sealed Promise RejectProtected(UnhandledException rejectVal)
		{
			State = PromiseState.Pending;
			var temp = rejectHandler;
			rejectHandler = null;
			return PromiseHelper(temp.Invoke());
		}
	}

	internal class PromiseArgReject<TReject, TArg> : Promise<TArg>, ILinked<PromiseArgReject<TReject, TArg>>
	{
		PromiseArgReject<TReject, TArg> ILinked<PromiseArgReject<TReject, TArg>>.Next { get { return (PromiseArgReject<TReject, TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TReject, TArg> rejectHandler;

		protected override sealed Promise RejectProtected(UnhandledException rejectVal)
		{
			TReject val;
			if (rejectVal.TryGetValueAs(out val))
			{
				Value = rejectHandler.Invoke(val);
			}
			else
			{
				rejectedValue = rejectVal;
			}
			rejectHandler = null;
			return null;
		}
	}

	internal class PromiseArgRejectPromise<TReject> : Promise, ILinked<PromiseArgRejectPromise<TReject>>
	{
		PromiseArgRejectPromise<TReject> ILinked<PromiseArgRejectPromise<TReject>>.Next { get { return (PromiseArgRejectPromise<TReject>) NextInternal; } set { NextInternal = value; } }

		internal Func<TReject, Promise> rejectHandler;

		protected override sealed Promise RejectProtected(UnhandledException rejectVal)
		{
			State = PromiseState.Pending;
			Promise promise;

			TReject val;
			if (rejectVal.TryGetValueAs(out val))
			{
				promise = PromiseHelper(rejectHandler.Invoke(val));
			}
			else
			{
				rejectedValue = rejectVal;
				promise = null;
			}
			rejectHandler = null;
			return promise;
		}
	}

	internal class PromiseArgRejectPromiseT<TArg> : Promise<TArg>, ILinked<PromiseArgRejectPromiseT<TArg>>
	{
		PromiseArgRejectPromiseT<TArg> ILinked<PromiseArgRejectPromiseT<TArg>>.Next { get { return (PromiseArgRejectPromiseT<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> rejectHandler;

		protected override sealed Promise RejectProtected(UnhandledException rejectVal)
		{
			State = PromiseState.Pending;
			var temp = rejectHandler;
			rejectHandler = null;
			return PromiseHelper(temp.Invoke());
		}
	}

	internal class PromiseArgRejectPromiseT<TReject, TArg> : Promise<TArg>, ILinked<PromiseArgRejectPromiseT<TReject, TArg>>
	{
		PromiseArgRejectPromiseT<TReject, TArg> ILinked<PromiseArgRejectPromiseT<TReject, TArg>>.Next { get { return (PromiseArgRejectPromiseT<TReject, TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TReject, Promise<TArg>> rejectHandler;

		protected override sealed Promise RejectProtected(UnhandledException rejectVal)
		{
			State = PromiseState.Pending;
			Promise promise;

			TReject val;
			if (rejectVal.TryGetValueAs(out val))
			{
				promise = PromiseHelper(rejectHandler.Invoke(val));
			}
			else
			{
				rejectedValue = rejectVal;
				promise = null;
			}
			rejectHandler = null;
			return promise;
		}
	}



	internal sealed class FinallyPromise : Promise, ILinked<FinallyPromise>
	{
		private bool handlingFinals;

		FinallyPromise ILinked<FinallyPromise>.Next { get { return (FinallyPromise) NextInternal; } set { NextInternal = value; } }

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
					if (rejectedValue != null)
					{
						UnityEngine.Debug.LogError("A new exception was encountered in a Promise.Finally callback before an old exception was handled." +
									   " The new exception will replace the old exception propagating up the final promise chain.\nOld exception:\n" +
									   rejectedValue);
					}
					rejectedValue = new UnhandledExceptionException().SetValue(e);
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
		PromiseFromDeferred ILinked<PromiseFromDeferred>.Next { get { return (PromiseFromDeferred) NextInternal; } set { NextInternal = value; } }

		internal Func<Action<Deferred>> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(New(temp.Invoke()));
		}
	}

	internal sealed class PromiseFromDeferred<T> : Promise<T>, ILinked<PromiseFromDeferred<T>>
	{
		PromiseFromDeferred<T> ILinked<PromiseFromDeferred<T>>.Next { get { return (PromiseFromDeferred<T>) NextInternal; } set { NextInternal = value; } }

		internal Func<Action<Deferred<T>>> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(New(temp.Invoke()));
		}
	}

	internal sealed class PromiseFromDeferredT<TValue> : Promise, ILinked<PromiseFromDeferredT<TValue>>
	{
		PromiseFromDeferredT<TValue> ILinked<PromiseFromDeferredT<TValue>>.Next { get { return (PromiseFromDeferredT<TValue>) NextInternal; } set { NextInternal = value; } }

		internal Func<TValue, Action<Deferred>> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(New(temp.Invoke(((IValueContainer<TValue>) feed).Value)));
		}
	}

	internal sealed class PromiseFromDeferredT<T, TValue> : Promise<T>, ILinked<PromiseFromDeferredT<T, TValue>>
	{
		PromiseFromDeferredT<T, TValue> ILinked<PromiseFromDeferredT<T, TValue>>.Next { get { return (PromiseFromDeferredT<T, TValue>) NextInternal; } set { NextInternal = value; } }

		internal Func<TValue, Action<Deferred<T>>> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(New(temp.Invoke(((IValueContainer<TValue>) feed).Value)));
		}
	}


	internal sealed class PromiseVoidFromVoidResolve : Promise, ILinked<PromiseVoidFromVoidResolve>
	{
		PromiseVoidFromVoidResolve ILinked<PromiseVoidFromVoidResolve>.Next { get { return (PromiseVoidFromVoidResolve) NextInternal; } set { NextInternal = value; } }

		internal Action resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseVoidFromVoid : PromiseVoidReject, ILinked<PromiseVoidFromVoid>
	{
		PromiseVoidFromVoid ILinked<PromiseVoidFromVoid>.Next { get { return (PromiseVoidFromVoid) NextInternal; } set { NextInternal = value; } }

		internal Action resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseVoidFromVoid<TReject> : PromiseVoidReject<TReject>, ILinked<PromiseVoidFromVoid<TReject>>
	{
		PromiseVoidFromVoid<TReject> ILinked<PromiseVoidFromVoid<TReject>>.Next { get { return (PromiseVoidFromVoid<TReject>) NextInternal; } set { NextInternal = value; } }

		internal Action resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}



	internal sealed class PromiseVoidFromArgResolve<TArg> : Promise, ILinked<PromiseVoidFromArgResolve<TArg>>
	{
		PromiseVoidFromArgResolve<TArg> ILinked<PromiseVoidFromArgResolve<TArg>>.Next { get { return (PromiseVoidFromArgResolve<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Action<TArg> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseVoidFromArg<TArg> : PromiseVoidReject, ILinked<PromiseVoidFromArg<TArg>>
	{
		PromiseVoidFromArg<TArg> ILinked<PromiseVoidFromArg<TArg>>.Next { get { return (PromiseVoidFromArg<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Action<TArg> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseVoidFromArg<TArg, TReject> : PromiseVoidReject<TReject>, ILinked<PromiseVoidFromArg<TArg, TReject>>
	{
		PromiseVoidFromArg<TArg, TReject> ILinked<PromiseVoidFromArg<TArg, TReject>>.Next { get { return (PromiseVoidFromArg<TArg, TReject>) NextInternal; } set { NextInternal = value; } }

		internal Action<TArg> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}



	internal sealed class PromiseArgFromResultResolve<TResult> : Promise<TResult>, ILinked<PromiseArgFromResultResolve<TResult>>
	{
		PromiseArgFromResultResolve<TResult> ILinked<PromiseArgFromResultResolve<TResult>>.Next { get { return (PromiseArgFromResultResolve<TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TResult> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseArgFromResult<TResult> : PromiseArgReject<TResult>, ILinked<PromiseArgFromResult<TResult>>
	{
		PromiseArgFromResult<TResult> ILinked<PromiseArgFromResult<TResult>>.Next { get { return (PromiseArgFromResult<TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TResult> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseArgFromResult<TResult, TReject> : PromiseArgReject<TReject, TResult>, ILinked<PromiseArgFromResult<TResult, TReject>>
	{
		PromiseArgFromResult<TResult, TReject> ILinked<PromiseArgFromResult<TResult, TReject>>.Next { get { return (PromiseArgFromResult<TResult, TReject>) NextInternal; } set { NextInternal = value; } }

		internal Func<TResult> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke();
			resolveHandler = null;
			return null;
		}
	}



	internal sealed class PromiseArgFromArgResultResolve<TArg, TResult> : Promise<TResult>, ILinked<PromiseArgFromArgResultResolve<TArg, TResult>>
	{
		PromiseArgFromArgResultResolve<TArg, TResult> ILinked<PromiseArgFromArgResultResolve<TArg, TResult>>.Next { get { return (PromiseArgFromArgResultResolve<TArg, TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, TResult> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseArgFromArgResult<TArg, TResult> : PromiseArgReject<TResult>, ILinked<PromiseArgFromArgResult<TArg, TResult>>
	{
		PromiseArgFromArgResult<TArg, TResult> ILinked<PromiseArgFromArgResult<TArg, TResult>>.Next { get { return (PromiseArgFromArgResult<TArg, TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, TResult> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}

	internal sealed class PromiseArgFromArgResult<TArg, TResult, TReject> : PromiseArgReject<TReject, TResult>, ILinked<PromiseArgFromArgResult<TArg, TResult, TReject>>
	{
		PromiseArgFromArgResult<TArg, TResult, TReject> ILinked<PromiseArgFromArgResult<TArg, TResult, TReject>>.Next { get { return (PromiseArgFromArgResult<TArg, TResult, TReject>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, TResult> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			resolveHandler = null;
			return null;
		}
	}



	internal sealed class PromiseVoidFromPromiseResultResolve : Promise, ILinked<PromiseVoidFromPromiseResultResolve>
	{
		PromiseVoidFromPromiseResultResolve ILinked<PromiseVoidFromPromiseResultResolve>.Next { get { return (PromiseVoidFromPromiseResultResolve) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(temp.Invoke());
		}
	}

	internal sealed class PromiseVoidFromPromiseResult : PromiseVoidRejectPromise, ILinked<PromiseVoidFromPromiseResult>
	{
		PromiseVoidFromPromiseResult ILinked<PromiseVoidFromPromiseResult>.Next { get { return (PromiseVoidFromPromiseResult) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(temp.Invoke());
		}
	}

	internal sealed class PromiseVoidFromPromiseResult<TReject> : PromiseArgRejectPromise<TReject>, ILinked<PromiseVoidFromPromiseResult<TReject>>
	{
		PromiseVoidFromPromiseResult<TReject> ILinked<PromiseVoidFromPromiseResult<TReject>>.Next { get { return (PromiseVoidFromPromiseResult<TReject>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(temp.Invoke());
		}
	}



	internal sealed class PromiseVoidFromPromiseArgResultResolve<TArg> : Promise, ILinked<PromiseVoidFromPromiseArgResultResolve<TArg>>
	{
		PromiseVoidFromPromiseArgResultResolve<TArg> ILinked<PromiseVoidFromPromiseArgResultResolve<TArg>>.Next { get { return (PromiseVoidFromPromiseArgResultResolve<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, Promise> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(temp.Invoke(((IValueContainer<TArg>) feed).Value));
		}
	}

	internal sealed class PromiseVoidFromPromiseArgResult<TArg> : PromiseVoidRejectPromise, ILinked<PromiseVoidFromPromiseArgResult<TArg>>
	{
		PromiseVoidFromPromiseArgResult<TArg> ILinked<PromiseVoidFromPromiseArgResult<TArg>>.Next { get { return (PromiseVoidFromPromiseArgResult<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, Promise> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(temp.Invoke(((IValueContainer<TArg>) feed).Value));
		}
	}

	internal sealed class PromiseVoidFromPromiseArgResult<TArg, TReject> : PromiseArgRejectPromise<TReject>, ILinked<PromiseVoidFromPromiseArgResult<TArg, TReject>>
	{
		PromiseVoidFromPromiseArgResult<TArg, TReject> ILinked<PromiseVoidFromPromiseArgResult<TArg, TReject>>.Next { get { return (PromiseVoidFromPromiseArgResult<TArg, TReject>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, Promise> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(temp.Invoke(((IValueContainer<TArg>) feed).Value));
		}
	}



	internal sealed class PromiseArgFromPromiseResultResolve<TArg> : Promise<TArg>, ILinked<PromiseArgFromPromiseResultResolve<TArg>>
	{
		PromiseArgFromPromiseResultResolve<TArg> ILinked<PromiseArgFromPromiseResultResolve<TArg>>.Next { get { return (PromiseArgFromPromiseResultResolve<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(temp.Invoke());
		}
	}

	internal sealed class PromiseArgFromPromiseResult<TArg> : PromiseArgRejectPromiseT<TArg>, ILinked<PromiseArgFromPromiseResult<TArg>>
	{
		PromiseArgFromPromiseResult<TArg> ILinked<PromiseArgFromPromiseResult<TArg>>.Next { get { return (PromiseArgFromPromiseResult<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(temp.Invoke());
		}
	}

	internal sealed class PromiseArgFromPromiseResult<TArg, TReject> : PromiseArgRejectPromiseT<TReject, TArg>, ILinked<PromiseArgFromPromiseResult<TArg, TReject>>
	{
		PromiseArgFromPromiseResult<TArg, TReject> ILinked<PromiseArgFromPromiseResult<TArg, TReject>>.Next { get { return (PromiseArgFromPromiseResult<TArg, TReject>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			var temp = resolveHandler;
			resolveHandler = null;
			return PromiseHelper(temp.Invoke());
		}
	}



	internal sealed class PromiseArgFromPromiseArgResultResolve<TArg, PArg> : Promise<TArg>, ILinked<PromiseArgFromPromiseArgResultResolve<TArg, PArg>>
	{
		PromiseArgFromPromiseArgResultResolve<TArg, PArg> ILinked<PromiseArgFromPromiseArgResultResolve<TArg, PArg>>.Next { get { return (PromiseArgFromPromiseArgResultResolve<TArg, PArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<PArg, Promise<TArg>> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke(((IValueContainer<PArg>) feed).Value));
		}
	}

	internal sealed class PromiseArgFromPromiseArgResult<TArg, PArg> : PromiseArgRejectPromiseT<TArg>, ILinked<PromiseArgFromPromiseArgResult<TArg, PArg>>
	{
		PromiseArgFromPromiseArgResult<TArg, PArg> ILinked<PromiseArgFromPromiseArgResult<TArg, PArg>>.Next { get { return (PromiseArgFromPromiseArgResult<TArg, PArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<PArg, Promise<TArg>> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke(((IValueContainer<PArg>) feed).Value));
		}
	}

	internal sealed class PromiseArgFromPromiseArgResult<TArg, PArg, TReject> : PromiseArgRejectPromiseT<TReject, TArg>, ILinked<PromiseArgFromPromiseArgResult<TArg, PArg, TReject>>
	{
		PromiseArgFromPromiseArgResult<TArg, PArg, TReject> ILinked<PromiseArgFromPromiseArgResult<TArg, PArg, TReject>>.Next { get { return (PromiseArgFromPromiseArgResult<TArg, PArg, TReject>) NextInternal; } set { NextInternal = value; } }

		internal Func<PArg, Promise<TArg>> resolveHandler;

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke(((IValueContainer<PArg>) feed).Value));
		}
	}
}