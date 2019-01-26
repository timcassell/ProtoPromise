using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	public partial class Promise
	{
		// This allows infinite .Then resolveHandlers, since it avoids recursion.
		internal static void ContinueHandlingInternal(Promise current)
		{
			LinkedQueueStruct<Promise> nextHandles = new LinkedQueueStruct<Promise>(current);
			for (; current != null; current = current.NextInternal)
			{
				LinkedQueueClass<Promise> branches = current.NextBranches;
				for (Promise next = branches.Peek(); next != null;)
				{
					Promise waitPromise = next.HandleInternal(current);
					if (waitPromise == null || waitPromise.State != PromiseState.Pending)
					{
						Promise temp = next;
						next = next.NextInternal; // This is necessary because enqueue sets temp.next to null.
						nextHandles.EnqueueRisky(temp);
					}
					else
					{
						Promise cachedPromise = next;
						next = next.NextInternal;
						waitPromise.Complete(() =>
						{
							ContinueHandlingInternal(cachedPromise);
						});
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

			Exception exception = null;
			foreach (var promise in tempFinals)
			{
				var final = promise.final;
				if (final != null)
				{
					final.ResolveInternal();
				}

				for (Promise prev = promise.previous; prev != null; prev = prev.previous)
				{
					if (--prev.nextCount == 0)
					{
						final = prev.final;
						if (final != null)
						{
							final.ResolveInternal();
						}
					}

					// TODO: repool prev here
				}

				exception = promise._exception;
				// TODO: repool promise here
				if (exception != null)
				{
					break;
				}
			}
			finallies2.Clear();
			if (exception != null)
			{
				throw exception is UnhandledException ? exception : new UnhandledException(exception);
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

		internal PromiseVoidReject(ADeferred deferred) : base(deferred) { }

		protected override sealed Promise RejectProtected(Exception exception)
		{
			rejectHandler.Invoke();
			return null;
		}
	}

	internal class PromiseVoidReject<TException> : Promise, ILinked<PromiseVoidReject<TException>> where TException : Exception
	{
		PromiseVoidReject<TException> ILinked<PromiseVoidReject<TException>>.Next { get { return (PromiseVoidReject<TException>) NextInternal; } set { NextInternal = value; } }

		internal Action<TException> rejectHandler;

		internal PromiseVoidReject(ADeferred deferred) : base(deferred) { }

		protected override sealed Promise RejectProtected(Exception exception)
		{
			if (exception is UnhandledException)
			{
				exception = exception.InnerException;
			}

			if (exception is TException)
			{
				rejectHandler.Invoke((TException) exception);
			}
			else
			{
				_exception = exception;
			}
			return null;
		}
	}

	internal class PromiseArgReject<TArg> : Promise<TArg>, ILinked<PromiseArgReject<TArg>>
	{
		PromiseArgReject<TArg> ILinked<PromiseArgReject<TArg>>.Next { get { return (PromiseArgReject<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg> rejectHandler;

		internal PromiseArgReject(ADeferred deferred) : base(deferred) { }

		protected override sealed Promise RejectProtected(Exception exception)
		{
			Value = rejectHandler.Invoke();
			return null;
		}
	}

	internal class PromiseVoidRejectPromise : Promise, ILinked<PromiseVoidRejectPromise>
	{
		PromiseVoidRejectPromise ILinked<PromiseVoidRejectPromise>.Next { get { return (PromiseVoidRejectPromise) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> rejectHandler;

		internal PromiseVoidRejectPromise(ADeferred deferred) : base(deferred) { }

		protected override sealed Promise RejectProtected(Exception exception)
		{
			State = PromiseState.Pending;
			return PromiseHelper(rejectHandler.Invoke());
		}
	}

	internal class PromiseArgReject<TException, TArg> : Promise<TArg>, ILinked<PromiseArgReject<TException, TArg>> where TException : Exception
	{
		PromiseArgReject<TException, TArg> ILinked<PromiseArgReject<TException, TArg>>.Next { get { return (PromiseArgReject<TException, TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TException, TArg> rejectHandler;

		internal PromiseArgReject(ADeferred deferred) : base(deferred) { }

		protected override sealed Promise RejectProtected(Exception exception)
		{
			if (exception is UnhandledException)
			{
				exception = exception.InnerException;
			}

			if (exception is TException)
			{
				Value = rejectHandler.Invoke((TException) exception);
			}
			else
			{
				_exception = exception;
			}
			return null;
		}
	}

	internal class PromiseArgRejectPromise<TException> : Promise, ILinked<PromiseArgRejectPromise<TException>> where TException : Exception
	{
		PromiseArgRejectPromise<TException> ILinked<PromiseArgRejectPromise<TException>>.Next { get { return (PromiseArgRejectPromise<TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<TException, Promise> rejectHandler;

		internal PromiseArgRejectPromise(ADeferred deferred) : base(deferred) { }

		protected override sealed Promise RejectProtected(Exception exception)
		{
			if (exception is UnhandledException)
			{
				exception = exception.InnerException;
			}

			State = PromiseState.Pending;
			Promise promise;
			if (exception is TException)
			{
				promise = PromiseHelper(rejectHandler.Invoke((TException) exception));
			}
			else
			{
				_exception = exception;
				promise = null;
			}
			return promise;
		}
	}

	internal class PromiseArgRejectPromiseT<TArg> : Promise<TArg>, ILinked<PromiseArgRejectPromiseT<TArg>>
	{
		PromiseArgRejectPromiseT<TArg> ILinked<PromiseArgRejectPromiseT<TArg>>.Next { get { return (PromiseArgRejectPromiseT<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> rejectHandler;

		internal PromiseArgRejectPromiseT(ADeferred deferred) : base(deferred) { }

		protected override sealed Promise RejectProtected(Exception exception)
		{
			State = PromiseState.Pending;
			return PromiseHelper(rejectHandler.Invoke());
		}
	}

	internal class PromiseArgRejectPromiseT<TException, TArg> : Promise<TArg>, ILinked<PromiseArgRejectPromiseT<TException, TArg>> where TException : Exception
	{
		PromiseArgRejectPromiseT<TException, TArg> ILinked<PromiseArgRejectPromiseT<TException, TArg>>.Next { get { return (PromiseArgRejectPromiseT<TException, TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TException, Promise<TArg>> rejectHandler;

		internal PromiseArgRejectPromiseT(ADeferred deferred) : base(deferred) { }

		protected override sealed Promise RejectProtected(Exception exception)
		{
			if (exception is UnhandledException)
			{
				exception = exception.InnerException;
			}

			State = PromiseState.Pending;
			Promise promise;
			if (exception is TException)
			{
				promise = PromiseHelper(rejectHandler.Invoke((TException) exception));
			}
			else
			{
				_exception = exception;
				promise = null;
			}
			return promise;
		}
	}



	internal sealed class FinallyPromise : Promise, ILinked<FinallyPromise>
	{
		FinallyPromise ILinked<FinallyPromise>.Next { get { return (FinallyPromise) NextInternal; } set { NextInternal = value; } }

		internal Action finalHandler;

		internal FinallyPromise(ADeferred deferred) : base(deferred) { }

		internal void HandleFinallies()
		{
			handling = true;
			for (Action temp = finalHandler; temp != null; temp = finalHandler) // Keep looping in case more finally callbacks are added from the invoke. This avoids recursion to prevent StackOverflows.
			{
				finalHandler = null;
				try
				{
					temp.Invoke();
				}
				catch (Exception e)
				{
					if (_exception != null)
					{
						UnityEngine.Debug.LogError("A new exception was encountered in a Promise.Finally callback before an old exception was handled." +
									   " The new exception will replace the old exception propagating up the final promise chain.\nOld exception:\n" +
									   _exception);
					}
					_exception = e;
				}
			}
			handling = false;
		}

		internal void ResolveInternal()
		{
			State = PromiseState.Resolved;
			HandleFinallies();
			if (_exception == null)
			{
				try
				{
					ResolveDones();
				}
				catch (Exception e)
				{
					_exception = e;
				}
			}
			OnComplete();
		}
	}


	internal sealed class PromiseFromDeferred : Promise, ILinked<PromiseFromDeferred>
	{
		PromiseFromDeferred ILinked<PromiseFromDeferred>.Next { get { return (PromiseFromDeferred) NextInternal; } set { NextInternal = value; } }

		internal Func<Action<Deferred>> resolveHandler;

		internal PromiseFromDeferred(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			return New(resolveHandler.Invoke());
		}
	}

	internal sealed class PromiseFromDeferred<T> : Promise<T>, ILinked<PromiseFromDeferred<T>>
	{
		PromiseFromDeferred<T> ILinked<PromiseFromDeferred<T>>.Next { get { return (PromiseFromDeferred<T>) NextInternal; } set { NextInternal = value; } }

		internal Func<Action<Deferred<T>>> resolveHandler;

		internal PromiseFromDeferred(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			return New(resolveHandler.Invoke());
		}
	}

	internal sealed class PromiseFromDeferredT<TValue> : Promise, ILinked<PromiseFromDeferredT<TValue>>
	{
		PromiseFromDeferredT<TValue> ILinked<PromiseFromDeferredT<TValue>>.Next { get { return (PromiseFromDeferredT<TValue>) NextInternal; } set { NextInternal = value; } }

		internal Func<TValue, Action<Deferred>> resolveHandler;

		internal PromiseFromDeferredT(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			return New(resolveHandler.Invoke(((IValueContainer<TValue>) feed).Value));
		}
	}

	internal sealed class PromiseFromDeferredT<T, TValue> : Promise<T>, ILinked<PromiseFromDeferredT<T, TValue>>
	{
		PromiseFromDeferredT<T, TValue> ILinked<PromiseFromDeferredT<T, TValue>>.Next { get { return (PromiseFromDeferredT<T, TValue>) NextInternal; } set { NextInternal = value; } }

		internal Func<TValue, Action<Deferred<T>>> resolveHandler;

		internal PromiseFromDeferredT(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			return New(resolveHandler.Invoke(((IValueContainer<TValue>) feed).Value));
		}
	}


	internal sealed class PromiseVoidFromVoidResolve : Promise, ILinked<PromiseVoidFromVoidResolve>
	{
		PromiseVoidFromVoidResolve ILinked<PromiseVoidFromVoidResolve>.Next { get { return (PromiseVoidFromVoidResolve) NextInternal; } set { NextInternal = value; } }

		internal Action resolveHandler;

		internal PromiseVoidFromVoidResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke();
			return null;
		}
	}

	internal sealed class PromiseVoidFromVoid : PromiseVoidReject, ILinked<PromiseVoidFromVoid>
	{
		PromiseVoidFromVoid ILinked<PromiseVoidFromVoid>.Next { get { return (PromiseVoidFromVoid) NextInternal; } set { NextInternal = value; } }

		internal Action resolveHandler;

		internal PromiseVoidFromVoid(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke();
			return null;
		}
	}

	internal sealed class PromiseVoidFromVoid<TException> : PromiseVoidReject<TException>, ILinked<PromiseVoidFromVoid<TException>> where TException : Exception
	{
		PromiseVoidFromVoid<TException> ILinked<PromiseVoidFromVoid<TException>>.Next { get { return (PromiseVoidFromVoid<TException>) NextInternal; } set { NextInternal = value; } }

		internal Action resolveHandler;

		internal PromiseVoidFromVoid(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke();
			return null;
		}
	}



	internal sealed class PromiseVoidFromArgResolve<TArg> : Promise, ILinked<PromiseVoidFromArgResolve<TArg>>
	{
		PromiseVoidFromArgResolve<TArg> ILinked<PromiseVoidFromArgResolve<TArg>>.Next { get { return (PromiseVoidFromArgResolve<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Action<TArg> resolveHandler;

		internal PromiseVoidFromArgResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}

	internal sealed class PromiseVoidFromArg<TArg> : PromiseVoidReject, ILinked<PromiseVoidFromArg<TArg>>
	{
		PromiseVoidFromArg<TArg> ILinked<PromiseVoidFromArg<TArg>>.Next { get { return (PromiseVoidFromArg<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Action<TArg> resolveHandler;

		internal PromiseVoidFromArg(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}

	internal sealed class PromiseVoidFromArg<TArg, TException> : PromiseVoidReject<TException>, ILinked<PromiseVoidFromArg<TArg, TException>> where TException : Exception
	{
		PromiseVoidFromArg<TArg, TException> ILinked<PromiseVoidFromArg<TArg, TException>>.Next { get { return (PromiseVoidFromArg<TArg, TException>) NextInternal; } set { NextInternal = value; } }

		internal Action<TArg> resolveHandler;

		internal PromiseVoidFromArg(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}



	internal sealed class PromiseArgFromResultResolve<TResult> : Promise<TResult>, ILinked<PromiseArgFromResultResolve<TResult>>
	{
		PromiseArgFromResultResolve<TResult> ILinked<PromiseArgFromResultResolve<TResult>>.Next { get { return (PromiseArgFromResultResolve<TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TResult> resolveHandler;

		internal PromiseArgFromResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke();
			return null;
		}
	}

	internal sealed class PromiseArgFromResult<TResult> : PromiseArgReject<TResult>, ILinked<PromiseArgFromResult<TResult>>
	{
		PromiseArgFromResult<TResult> ILinked<PromiseArgFromResult<TResult>>.Next { get { return (PromiseArgFromResult<TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TResult> resolveHandler;

		internal PromiseArgFromResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke();
			return null;
		}
	}

	internal sealed class PromiseArgFromResult<TResult, TException> : PromiseArgReject<TException, TResult>, ILinked<PromiseArgFromResult<TResult, TException>> where TException : Exception
	{
		PromiseArgFromResult<TResult, TException> ILinked<PromiseArgFromResult<TResult, TException>>.Next { get { return (PromiseArgFromResult<TResult, TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<TResult> resolveHandler;

		internal PromiseArgFromResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke();
			return null;
		}
	}



	internal sealed class PromiseArgFromArgResultResolve<TArg, TResult> : Promise<TResult>, ILinked<PromiseArgFromArgResultResolve<TArg, TResult>>
	{
		PromiseArgFromArgResultResolve<TArg, TResult> ILinked<PromiseArgFromArgResultResolve<TArg, TResult>>.Next { get { return (PromiseArgFromArgResultResolve<TArg, TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, TResult> resolveHandler;

		internal PromiseArgFromArgResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}

	internal sealed class PromiseArgFromArgResult<TArg, TResult> : PromiseArgReject<TResult>, ILinked<PromiseArgFromArgResult<TArg, TResult>>
	{
		PromiseArgFromArgResult<TArg, TResult> ILinked<PromiseArgFromArgResult<TArg, TResult>>.Next { get { return (PromiseArgFromArgResult<TArg, TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, TResult> resolveHandler;

		internal PromiseArgFromArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}

	internal sealed class PromiseArgFromArgResult<TArg, TResult, TException> : PromiseArgReject<TException, TResult>, ILinked<PromiseArgFromArgResult<TArg, TResult, TException>> where TException : Exception
	{
		PromiseArgFromArgResult<TArg, TResult, TException> ILinked<PromiseArgFromArgResult<TArg, TResult, TException>>.Next { get { return (PromiseArgFromArgResult<TArg, TResult, TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, TResult> resolveHandler;

		internal PromiseArgFromArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}



	internal sealed class PromiseVoidFromPromiseResultResolve : Promise, ILinked<PromiseVoidFromPromiseResultResolve>
	{
		PromiseVoidFromPromiseResultResolve ILinked<PromiseVoidFromPromiseResultResolve>.Next { get { return (PromiseVoidFromPromiseResultResolve) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> resolveHandler;

		internal PromiseVoidFromPromiseResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke());
		}
	}

	internal sealed class PromiseVoidFromPromiseResult : PromiseVoidRejectPromise, ILinked<PromiseVoidFromPromiseResult>
	{
		PromiseVoidFromPromiseResult ILinked<PromiseVoidFromPromiseResult>.Next { get { return (PromiseVoidFromPromiseResult) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> resolveHandler;

		internal PromiseVoidFromPromiseResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke());
		}
	}

	internal sealed class PromiseVoidFromPromiseResult<TException> : PromiseArgRejectPromise<TException>, ILinked<PromiseVoidFromPromiseResult> where TException : Exception
	{
		PromiseVoidFromPromiseResult ILinked<PromiseVoidFromPromiseResult>.Next { get { return (PromiseVoidFromPromiseResult) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> resolveHandler;

		internal PromiseVoidFromPromiseResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke());
		}
	}



	internal sealed class PromiseVoidFromPromiseArgResultResolve<TArg> : Promise, ILinked<PromiseVoidFromPromiseArgResultResolve<TArg>>
	{
		PromiseVoidFromPromiseArgResultResolve<TArg> ILinked<PromiseVoidFromPromiseArgResultResolve<TArg>>.Next { get { return (PromiseVoidFromPromiseArgResultResolve<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, Promise> resolveHandler;

		internal PromiseVoidFromPromiseArgResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value));
		}
	}

	internal sealed class PromiseVoidFromPromiseArgResult<TArg> : PromiseVoidRejectPromise, ILinked<PromiseVoidFromPromiseArgResult<TArg>>
	{
		PromiseVoidFromPromiseArgResult<TArg> ILinked<PromiseVoidFromPromiseArgResult<TArg>>.Next { get { return (PromiseVoidFromPromiseArgResult<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, Promise> resolveHandler;

		internal PromiseVoidFromPromiseArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value));
		}
	}

	internal sealed class PromiseVoidFromPromiseArgResult<TArg, TException> : PromiseArgRejectPromise<TException>, ILinked<PromiseVoidFromPromiseArgResult<TArg, TException>> where TException : Exception
	{
		PromiseVoidFromPromiseArgResult<TArg, TException> ILinked<PromiseVoidFromPromiseArgResult<TArg, TException>>.Next { get { return (PromiseVoidFromPromiseArgResult<TArg, TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, Promise> resolveHandler;

		internal PromiseVoidFromPromiseArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke(((IValueContainer<TArg>) feed).Value));
		}
	}



	internal sealed class PromiseArgFromPromiseResultResolve<TArg> : Promise<TArg>, ILinked<PromiseArgFromPromiseResultResolve<TArg>>
	{
		PromiseArgFromPromiseResultResolve<TArg> ILinked<PromiseArgFromPromiseResultResolve<TArg>>.Next { get { return (PromiseArgFromPromiseResultResolve<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> resolveHandler;

		internal PromiseArgFromPromiseResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke());
		}
	}

	internal sealed class PromiseArgFromPromiseResult<TArg> : PromiseArgRejectPromiseT<TArg>, ILinked<PromiseArgFromPromiseResult<TArg>>
	{
		PromiseArgFromPromiseResult<TArg> ILinked<PromiseArgFromPromiseResult<TArg>>.Next { get { return (PromiseArgFromPromiseResult<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> resolveHandler;

		internal PromiseArgFromPromiseResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke());
		}
	}

	internal sealed class PromiseArgFromPromiseResult<TArg, TException> : PromiseArgRejectPromiseT<TException, TArg>, ILinked<PromiseArgFromPromiseResult<TArg, TException>> where TException : Exception
	{
		PromiseArgFromPromiseResult<TArg, TException> ILinked<PromiseArgFromPromiseResult<TArg, TException>>.Next { get { return (PromiseArgFromPromiseResult<TArg, TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> resolveHandler;

		internal PromiseArgFromPromiseResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke());
		}
	}



	internal sealed class PromiseArgFromPromiseArgResultResolve<TArg, PArg> : Promise<TArg>, ILinked<PromiseArgFromPromiseArgResultResolve<TArg, PArg>>
	{
		PromiseArgFromPromiseArgResultResolve<TArg, PArg> ILinked<PromiseArgFromPromiseArgResultResolve<TArg, PArg>>.Next { get { return (PromiseArgFromPromiseArgResultResolve<TArg, PArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<PArg, Promise<TArg>> resolveHandler;

		internal PromiseArgFromPromiseArgResultResolve(ADeferred deferred) : base(deferred) { }

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

		internal PromiseArgFromPromiseArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke(((IValueContainer<PArg>) feed).Value));
		}
	}

	internal sealed class PromiseArgFromPromiseArgResult<TArg, PArg, TException> : PromiseArgRejectPromiseT<TException, TArg>, ILinked<PromiseArgFromPromiseArgResult<TArg, PArg, TException>> where TException : Exception
	{
		PromiseArgFromPromiseArgResult<TArg, PArg, TException> ILinked<PromiseArgFromPromiseArgResult<TArg, PArg, TException>>.Next { get { return (PromiseArgFromPromiseArgResult<TArg, PArg, TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<PArg, Promise<TArg>> resolveHandler;

		internal PromiseArgFromPromiseArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			State = PromiseState.Pending;
			return PromiseHelper(resolveHandler.Invoke(((IValueContainer<PArg>) feed).Value));
		}
	}
}