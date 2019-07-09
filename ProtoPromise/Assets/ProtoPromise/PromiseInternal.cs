using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	internal interface IValueContainer<T>
	{
		T Value { get; }
	}

	partial class Promise : ILinked<Promise>
	{
		private Promise _next;
		Promise ILinked<Promise>.Next { get { return _next; } set { _next = value; } }

		// Dictionaries to use less memory for expected less used functions, so that every promise object doesn't have empty fields.
		//private static Dictionary<Promise, Internal.FinallyPromise> finals = new Dictionary<Promise, Internal.FinallyPromise>();
		private static Dictionary<Promise, ValueLinkedQueue<Internal.IDelegate>> cancels = new Dictionary<Promise, ValueLinkedQueue<Internal.IDelegate>>();
		private static Dictionary<Promise, uint> retains = new Dictionary<Promise, uint>();

#if DEBUG
		private string _createdStackTrace;
		private static int idCounter;
		protected readonly int _id;
#endif

		protected Promise _previous;
		private ValueLinkedQueue<Promise> _nextBranches;

		private ValueLinkedStack<Internal.Finally> _finallies;

		protected Internal.IValueContainer _rejectedOrCanceledValue;

		private uint _pendingCount; // How many pending branches and (un)dones.

		private bool _wasWaitedOn; // Tells the finally handler that another promise waited on this promise (either by .Then/.Catch from this promise, or by returning this promise in another promise's .Then/.Catch)
		private bool _notHandling = true; // Is not already being handled in ContinueHandling or ContinueCanceling.

		protected PromiseState _state;

#if DEBUG
		private bool _hasBeenHandled; // Check to make sure .Finally is called before _nextBranches is cleared.
		private bool _done; // If true, finally has been invoked and/or uncaught rejections have been reported. If _pendingCount is 0, this promise was added back to the pool if pooling is allowed, and no further actions are allowed.
#endif

		protected Promise()
		{
#if DEBUG
			_id = idCounter++;
#endif
		}

		protected void Reset(int skipFrames = 4)
		{
			if (!Manager.PoolObjects)
			{
				Retain();
			}
			_state = PromiseState.Pending;
			_previous = null;
			_rejectedOrCanceledValue = null;
#if DEBUG
			_hasBeenHandled = false;
			_done = false;
			if (Manager.DebugStacktraceGenerator == GeneratedStacktrace.All)
			{
				_createdStackTrace = GetStackTrace(skipFrames);
			}
#endif
		}

		protected virtual void ReportProgress(float progress) { }

#if DEBUG
		protected void Validate(Promise other)
		{
			if (other == null)
			{
				// Returning a null from the callback is not allowed.
				throw new InvalidReturnException("A null promise was returned.");
			}

			// A promise cannot wait on itself.
			for (var prev = other; prev != null; prev = prev._previous)
			{
				if (prev == this)
				{
					throw new InvalidReturnException("Circular Promise chain detected.", other._createdStackTrace);
				}
				if (prev is Internal.FinallyPromise)
				{
					break;
				}
			}
		}

		protected void Validate(Delegate other)
		{
			if (other == null)
			{
				// Returning a null from the callback is not allowed.
				throw new InvalidReturnException("A null delegate was returned.");
			}
		}

		public override string ToString()
		{
			return string.Format("Type: Promise, Id: {0}, State: {1}", _id, State);
		}
#else
		public override string ToString()
		{
			return string.Format("Type: Promise, State: {0}", State);
		}
#endif

		protected virtual void Dispose()
		{
			if (_rejectedOrCanceledValue != null)
			{
				_rejectedOrCanceledValue.Release();
				_rejectedOrCanceledValue = null;
			}
		}

		protected void OnComplete()
		{
			if (_pendingCount == 0)
			{
				AddFinal(this);
			}
		}

		private Internal.FinallyPromise GetOrCreateFinally()
		{
			Internal.FinallyPromise promise;
			if (!finals.TryGetValue(this, out promise))
			{
				promise = Internal.FinallyPromise.GetOrCreate();
				// TODO
				//promise._previous = this;
				finals[this] = promise;
			}
			return promise;
		}

		protected virtual Promise GetDuplicate()
		{
			return Internal.LitePromise.GetOrCreate();
		}

		// Returns true if this is the first item added to the queue.
		private bool HookUpCancelCallback(Internal.IDelegate onCanceled)
		{
			ValueLinkedQueue<Internal.IDelegate> cancelQueue;
			bool newAdd = !cancels.TryGetValue(this, out cancelQueue);
			cancelQueue.AddLast(onCanceled);
			cancels[this] = cancelQueue;
			return newAdd;
		}

		private void HandleCancel()
		{
			_state = PromiseState.Canceled;

			ValueLinkedQueue<Internal.IDelegate> cancelQueue;
			cancels.TryGetValue(this, out cancelQueue);

			// Note: even though the queue is a value type and is re-written to the dictionary on every delegate add,
			// this will still work because the ILinked.Next is part of the referenced objects.

			while (cancelQueue.IsNotEmpty)
			{
				var del = cancelQueue.TakeFirst();
				del.Dispose();
				del.TryInvoke(_rejectedOrCanceledValue);
			}

			cancels.Remove(this);
			OnComplete();
		}

		private void Resolve()
		{
			_state = PromiseState.Resolved;
			OnComplete();
			ContinueHandlingInternal(this);
		}

		private Promise Resolve(Promise feed)
		{
			// TODO: Report progress 1.0. Ignore 1.0 progress reports from deferred.reportprogress.
			_state = PromiseState.Resolved;
			Promise promise = null;
			try
			{
				promise = ResolveVirtual(feed);
			}
			catch (Exception e)
			{
				_state = PromiseState.Resolved; // In case the callback throws an exception before returning a promise.
				var ex = Internal.UnhandledExceptionException.GetOrCreate(e);
#if DEBUG
				ex.SetStackTrace(_createdStackTrace);
#endif
				_rejectedOrCanceledValue = ex;
			}
			OnComplete();
			return promise;
		}

		protected virtual Promise ResolveVirtual(Promise feed)
		{
			return null;
		}

		private void Reject()
		{
			Internal.UnhandledExceptionVoid rejectValue = Internal.UnhandledExceptionVoid.GetOrCreate();
#if DEBUG
			if (Manager.DebugStacktraceGenerator != GeneratedStacktrace.None)
			{
				rejectValue.SetStackTrace(GetStackTrace(3));
			}
#endif
			RejectDirect(rejectValue);
		}

		protected void RejectDirect(Internal.IValueContainer rejectValue)
		{
			Reject(rejectValue);
			ContinueHandlingInternal(this);
		}

		private void Reject<TReject>(TReject reason)
		{
			Internal.IValueContainer rejectValue;
			// Is reason an exception (including if it's null)?
			if (typeof(Exception).IsAssignableFrom(typeof(TReject)))
			{
				// Behave the same way .Net behaves if you throw null.
				var rejVal = Internal.UnhandledExceptionException.GetOrCreate(reason as Exception ?? new NullReferenceException());
#if DEBUG
				if (Manager.DebugStacktraceGenerator != GeneratedStacktrace.None)
				{
					rejVal.SetStackTrace(GetStackTrace(3));
				}
#endif
				rejectValue = rejVal;
			}
			else
			{
				var rejVal = Internal.UnhandledException<TReject>.GetOrCreate(reason);
#if DEBUG
				if (Manager.DebugStacktraceGenerator != GeneratedStacktrace.None)
				{
					rejVal.SetStackTrace(GetStackTrace(3));
				}
#endif
				rejectValue = rejVal;
			}

			RejectDirect(rejectValue);
		}

		private Promise Reject(Internal.IValueContainer rejectVal)
		{
			_state = PromiseState.Rejected;
			Promise promise = null;
			try
			{
				promise = RejectVirtual(rejectVal);
			}
			catch (Exception e)
			{
				_state = PromiseState.Rejected; // In case the callback throws an exception before returning a promise.
				var ex = Internal.UnhandledExceptionException.GetOrCreate(e);
#if DEBUG
				ex.SetStackTrace(_createdStackTrace);
#endif
				_rejectedOrCanceledValue = ex;
			}
			OnComplete();
			return promise;
		}

		protected virtual Promise RejectVirtual(Internal.IValueContainer rejectVal) // private protected not supported before c# 7.2, so must use internal.
		{
			rejectVal.Retain();
			_rejectedOrCanceledValue = rejectVal;
			return null;
		}

		protected virtual void AdoptState(Promise adoptee)
		{
			_state = adoptee._state;
			_rejectedOrCanceledValue = adoptee._rejectedOrCanceledValue;
			adoptee._wasWaitedOn = true;
		}

		protected void HookupNewPromise(Promise newPromise)
		{
#if DEBUG
			checked
#endif
			{
				++_pendingCount;
			}
			AddWaiter(newPromise);
			newPromise._previous = this;
			SetDepth(newPromise);
			RemoveFinal(this);

			// TODO: Move this to AddWaiter. Also validate returned promises as not disposed.

			// Continue handling if this isn't pending and it's not already being handled.
			if (_notHandling)
			{
				switch (_state)
				{
					case PromiseState.Canceled:
						{
							ContinueCanceling(this);
							break;
						}
					case PromiseState.Resolved:
					case PromiseState.Rejected:
						{
							ContinueHandlingInternal(this);
							break;
						}
				}
			}
		}

		private void AddWaiter(Promise promise)
		{
			_wasWaitedOn = true;

			_nextBranches.AddLast(promise);
		}

		// Handle promises in a breadth-first manner.
		private static ValueLinkedQueue<Promise> nextHandles;

		// This allows infinite .Then/.Catch callbacks, since it avoids recursion.
		protected static void ContinueHandlingInternal(Promise current)
		{
			current._notHandling = false;
			if (nextHandles.IsEmpty)
			{
				nextHandles = new ValueLinkedQueue<Promise>(current);
			}
			else
			{
				// ContinueHandlingInternal is running higher in the program stack, so just return after adding to the queue.
				nextHandles.AddLastRisky(current);
				return;
			}

			for (; current != null; current = current._next)
			{
				var nextBranches = current._nextBranches;
				while (nextBranches.IsNotEmpty)
				{
					Promise next = nextBranches.TakeFirst();

					if (next._state == PromiseState.Canceled)
					{
						// If the next promise is already canceled, don't do anything.
						continue;
					}

					// Resolve or reject the next promise.
					Internal.IValueContainer rejectVal = current._rejectedOrCanceledValue;
					// TODO: clear current._previous.
					Promise waitPromise = rejectVal == null ? next.Resolve(current) : next.Reject(rejectVal);

					// Did the .Then/.Catch callback return a promise?
					if (waitPromise != null)
					{
						switch (waitPromise._state)
						{
							case PromiseState.Pending:
								{
									waitPromise.AddWaiter(next);
									continue;
								}
							case PromiseState.Canceled:
								{
									next._rejectedOrCanceledValue = waitPromise._rejectedOrCanceledValue;
									next.HandleCancel();
									ContinueCanceling(next);
									continue;
								}
							default:
								{
									next.AdoptState(waitPromise);
									break;
								}
						}
					}

					// TODO: clear next._previous.
					next._notHandling = false;
					nextHandles.AddLastRisky(next);
				}
				// TODO: add current back to pool.
				current._nextBranches.Clear();
				current._notHandling = true;
			}
			nextHandles.Clear();
		}

		// Cancel promises in a breadth-first manner.
		private static ValueLinkedQueue<Promise> cancelHandles;

		private static void ContinueCanceling(Promise current)
		{
			current._notHandling = false;
			if (cancelHandles.IsEmpty)
			{
				cancelHandles = new ValueLinkedQueue<Promise>(current);
			}
			else
			{
				// ContinueCanceling is running higher in the program stack, so just return after adding to the queue.
				cancelHandles.AddLastRisky(current);
				return;
			}

			for (; current != null; current = current._next)
			{
				var nextBranches = current._nextBranches;
				while (nextBranches.IsNotEmpty)
				{
					Promise next = nextBranches.TakeFirst();

					current._rejectedOrCanceledValue.Retain();
					next._rejectedOrCanceledValue = current._rejectedOrCanceledValue;
					next.HandleCancel();

					// TODO: clear next._previous.
					next._notHandling = false;
					cancelHandles.AddLastRisky(next);
				}
				// TODO: add current back to pool.
				current._nextBranches.Clear();
				current._notHandling = true;
			}
			cancelHandles.Clear();
		}

		private static readonly UnityEngine.WaitForEndOfFrame waitForEndOfFrame = new UnityEngine.WaitForEndOfFrame();
		private static bool handlingFinally;
		private static HashSet<Promise> finallies = new HashSet<Promise>();
		private static HashSet<Promise> finallies2 = new HashSet<Promise>();

		private static void HandleFinals()
		{
			handlingFinally = false;
			HashSet<Promise> tempFinals = finallies;
			finallies = finallies2;
			finallies2 = tempFinals;

			ValueLinkedStack<Promise> finalPromises = new ValueLinkedStack<Promise>();

			ValueLinkedStack<UnhandledException> exceptions = new ValueLinkedStack<UnhandledException>();
			ValueLinkedStack<UnhandledException> rejections = new ValueLinkedStack<UnhandledException>();
			foreach (Promise promise in tempFinals)
			{
				// TODO: _done is in debug only
				promise._done |= promise._wasWaitedOn;
				if (!promise._done)
				{
					// Only resolve final and throw uncaught exceptions if promise is marked done or another promise waited on it.
					continue;
				}

				Internal.FinallyPromise final;
				if (finals.TryGetValue(promise, out final))
				{
					final.Resolve();
					finalPromises.Push(final);
				}

				for (Promise prev = promise._previous; prev != null; prev = prev._previous)
				{
					if (--prev._pendingCount == 0)
					{
						prev._done = true;
						if (finals.TryGetValue(prev, out final))
						{
							final.Resolve();
							finalPromises.Push(final);
						}
					}
					prev.Dispose();
				}

				// Don't check reject value if another promise waited on it.
				if (!promise._wasWaitedOn)
				{
					var rejection = promise._rejectedOrCanceledValue as UnhandledException; // Null if promise was resolved or canceled.
					if (rejection != null && rejection.unhandled) // Don't log the same rejection twice if it wasn't caught in more than one branch.
					{
						rejection.unhandled = false;
						if (rejection is Internal.UnhandledExceptionException)
						{
							exceptions.Push(rejection);
						}
						else
						{
							rejections.Push(rejection);
						}
					}
				}
				promise.Dispose();
			}
			finallies2.Clear();

			// Debug log all the uncaught rejections, then debug log all the uncaught exceptions except one, then throw the remaining uncaught exception.
			while (rejections.IsNotEmpty)
			{
				var rejection = rejections.Pop();
				rejection.unhandled = true;
				Logger.LogException(rejection);
			}
			if (exceptions.IsNotEmpty)
			{
				UnhandledException exception = exceptions.Pop();
				exception.unhandled = true;
				while (exceptions.IsNotEmpty)
				{
					Logger.LogException(exception);
					exception = exceptions.Pop();
					exception.unhandled = true;
				}
				throw exception;
			}

			// Handle any .Then/.Catch attached to the finallies.
			while (finalPromises.IsNotEmpty)
			{
				var cachedPromise = finalPromises.Pop();

				if (cachedPromise._state == PromiseState.Canceled)
				{
					ContinueCanceling(cachedPromise);
				}
				else
				{
					ContinueHandlingInternal(cachedPromise);
				}
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

	partial class Promise<T> : IValueContainer<T>
	{
		protected T _value;
		T IValueContainer<T>.Value { get { return _value; } }

		protected Promise() : base() { }

		protected override Promise GetDuplicate()
		{
			return Internal.LitePromise<T>.GetOrCreate();
		}

		protected override sealed void AdoptState(Promise adoptee)
		{
			base.AdoptState(adoptee);
			_value = ((Promise<T>) adoptee)._value;
		}

		protected void Resolve(T value)
		{
			_value = value;
			_state = PromiseState.Resolved;
			OnComplete();
			ContinueHandlingInternal(this);
		}
		protected override Promise ResolveVirtual(Promise feed)
		{
			_value = ((IValueContainer<T>) feed).Value;
			return null;
		}

		protected override void Dispose()
		{
			base.Dispose();
			_value = default(T);
		}

		public override string ToString()
		{
#if DEBUG
			return string.Format("Type: Promise<{0}>, Id: {1}, State: {2}", typeof(T), _id, State);
#else
			return string.Format("Type: Promise<{0}>, State: {1}", typeof(T), State);
#endif
		}
	}

	partial class Promise
	{
		protected static partial class Internal
		{
			public static Action OnClearPool;

			public abstract class PoolablePromise<TPromise> : Promise where TPromise : PoolablePromise<TPromise>
			{
#pragma warning disable RECS0108 // Warns about static fields in generic types
				protected static ValueLinkedStack<Promise> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

				static PoolablePromise()
				{
					OnClearPool += () => pool.Clear();
				}

				protected override void Dispose()
				{
					base.Dispose();
					pool.Push(this);
				}
			}

			public abstract class PoolablePromise<T, TPromise> : Promise<T> where TPromise : PoolablePromise<T, TPromise>
			{
#pragma warning disable RECS0108 // Warns about static fields in generic types
				protected static ValueLinkedStack<Promise> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

				static PoolablePromise()
				{
					OnClearPool += () => pool.Clear();
				}

				protected override void Dispose()
				{
					base.Dispose();
					pool.Push(this);
				}
			}

			// TODO: assign previous when this is created, remove this from the finals dictionary when canceled.
			// TODO: handle progress
			public sealed class Finally : ILinked<Finally>, IRetainable
			{
				public Finally Next { get; set; }

				public Promise owner;
				private ValueLinkedQueue<Promise> _nextBranches;

				private FinallyProgressContainer _progressContainer; // How many total waitpromises is this waiting for (used to calculate progress).
				private ulong _retainCounter; // How many non-waitpromises is this waiting for (used to know when to resolve).
				private bool isReporting; // Is the progress in the process of being reported?

				private static ValueLinkedStack<Finally> pool;

				static Finally()
				{
					OnClearPool += () => pool.Clear();
				}

				void Dispose()
				{
					pool.Push(this);
				}

				private Finally() { }

				public static Finally GetOrCreate(Promise owner)
				{
					// TODO: hook up progress 
					var f = pool.IsNotEmpty ? pool.Pop() : new Finally();
					f.owner = owner;
					f._progressContainer = FinallyProgressContainer.GetOrCreate(owner.GetDepth());
					return f;
				}

				public void IncrementPendCount()
				{
					++_progressContainer.pendCount;
				}

				public void IncrementProgress(ulong increment)
				{
					var progress = _progressContainer.progress;
					progress.Increment(increment);

					if (isReporting)
					{
						return;
					}
					isReporting = true;

					ReportNewProgress:

					ulong currentValue = progress.ToUInt64();
					float prog = progress.ToFloat();
					// Report progress
					for (Promise promise = _nextBranches.PeekFirst(); promise != null; promise = promise._next)
					{
						promise.ReportProgress(prog);
					}

					if (currentValue != _progressContainer.progress.ToUInt64())
					{
						// Progress was changed in a callback, report new value.
						goto ReportNewProgress;
					}
				}

				public void Retain()
				{
					++_retainCounter;
				}

				public void Release()
				{
					if (--_retainCounter == 0)
					{
						Dispose();
						// Resolve
						do
						{
							_nextBranches.TakeFirst().Resolve();
						} while (_nextBranches.IsNotEmpty);
					}
				}
			}


			public sealed class LitePromise : PoolablePromise<LitePromise>
			{
				private LitePromise() { }

				public static LitePromise GetOrCreate()
				{
					var promise = pool.IsNotEmpty ? (LitePromise) pool.Pop() : new LitePromise();
					promise.Reset();
					return promise;
				}
			}

			public sealed class LitePromise<T> : PoolablePromise<T, LitePromise<T>>
			{
				private LitePromise() { }

				public static LitePromise<T> GetOrCreate()
				{
					var promise = pool.IsNotEmpty ? (LitePromise<T>) pool.Pop() : new LitePromise<T>();
					promise.Reset();
					return promise;
				}

				public new void Resolve(T value)
				{
					base.Resolve(value);
				}
			}

			public sealed class DeferredPromise : PromiseWaitDeferred<DeferredPromise>
			{
				private DeferredPromise() { }

				public static DeferredPromise GetOrCreate()
				{
					var promise = pool.IsNotEmpty ? (DeferredPromise) pool.Pop() : new DeferredPromise();
					promise.Reset();
					promise.ResetDepth();
					return promise;
				}
			}

			public sealed class DeferredPromise<T> : PromiseWaitDeferred<T, DeferredPromise<T>>
			{
				private DeferredPromise() { }

				public static DeferredPromise<T> GetOrCreate()
				{
					var promise = pool.IsNotEmpty ? (DeferredPromise<T>) pool.Pop() : new DeferredPromise<T>();
					promise.Reset();
					promise.ResetDepth();
					return promise;
				}
			}

#region Resolve Promises
			// Individual types for more common .Then(onResolved) calls to be more efficient.

			public sealed class PromiseVoidResolve : PoolablePromise<PromiseVoidResolve>
			{
				private Action resolveHandler;

				private PromiseVoidResolve() { }

				public static PromiseVoidResolve GetOrCreate(Action resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseVoidResolve) pool.Pop() : new PromiseVoidResolve();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					var tempResolve = resolveHandler;
					resolveHandler = null;
					tempResolve.Invoke();
					return null;
				}
			}

			public sealed class PromiseArgResolve<TArg> : PoolablePromise<PromiseArgResolve<TArg>>
			{
				private Action<TArg> resolveHandler;

				private PromiseArgResolve() { }

				public static PromiseArgResolve<TArg> GetOrCreate(Action<TArg> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseArgResolve<TArg>) pool.Pop() : new PromiseArgResolve<TArg>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					var tempResolve = resolveHandler;
					resolveHandler = null;
					tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
					return null;
				}
			}

			public sealed class PromiseVoidResolve<TResult> : PoolablePromise<TResult, PromiseVoidResolve<TResult>>
			{
				private Func<TResult> resolveHandler;

				private PromiseVoidResolve() { }

				public static PromiseVoidResolve<TResult> GetOrCreate(Func<TResult> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseVoidResolve<TResult>) pool.Pop() : new PromiseVoidResolve<TResult>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					var tempResolve = resolveHandler;
					resolveHandler = null;
					_value = tempResolve.Invoke();
					return null;
				}
			}

			public sealed class PromiseArgResolve<TArg, TResult> : PoolablePromise<TResult, PromiseArgResolve<TArg, TResult>>
			{
				private Func<TArg, TResult> resolveHandler;

				private PromiseArgResolve() { }

				public static PromiseArgResolve<TArg, TResult> GetOrCreate(Func<TArg, TResult> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseArgResolve<TArg, TResult>) pool.Pop() : new PromiseArgResolve<TArg, TResult>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					var tempResolve = resolveHandler;
					resolveHandler = null;
					_value = tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
					return null;
				}
			}

			public sealed class PromiseVoidResolvePromise : PromiseWaitPromise<PromiseVoidResolvePromise>
			{
				private Func<Promise> resolveHandler;

				private PromiseVoidResolvePromise() { }

				public static PromiseVoidResolvePromise GetOrCreate(Func<Promise> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseVoidResolvePromise) pool.Pop() : new PromiseVoidResolvePromise();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (resolveHandler == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					var tempResolve = resolveHandler;
					resolveHandler = null;
					var promise = tempResolve.Invoke();
#if DEBUG
					Validate(promise);
#endif
					SubscribeProgress(promise);
					return promise;
				}
			}

			public sealed class PromiseArgResolvePromise<TArg> : PromiseWaitPromise<PromiseArgResolvePromise<TArg>>
			{
				private Func<TArg, Promise> resolveHandler;

				private PromiseArgResolvePromise() { }

				public static PromiseArgResolvePromise<TArg> GetOrCreate(Func<TArg, Promise> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg>) pool.Pop() : new PromiseArgResolvePromise<TArg>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (resolveHandler == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					var tempResolve = resolveHandler;
					resolveHandler = null;
					var promise = tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
					Validate(promise);
#endif
					SubscribeProgress(promise);
					return promise;
				}
			}

			public sealed class PromiseVoidResolvePromise<TPromise> : PromiseWaitPromise<TPromise, PromiseVoidResolvePromise<TPromise>>
			{
				private Func<Promise<TPromise>> resolveHandler;

				private PromiseVoidResolvePromise() { }

				public static PromiseVoidResolvePromise<TPromise> GetOrCreate(Func<Promise<TPromise>> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseVoidResolvePromise<TPromise>) pool.Pop() : new PromiseVoidResolvePromise<TPromise>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (resolveHandler == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					var tempResolve = resolveHandler;
					resolveHandler = null;
					var promise = tempResolve.Invoke();
#if DEBUG
					Validate(promise);
#endif
					SubscribeProgress(promise);
					return promise;
				}
			}

			public sealed class PromiseArgResolvePromise<TArg, TPromise> : PromiseWaitPromise<TPromise, PromiseArgResolvePromise<TArg, TPromise>>
			{
				private Func<TArg, Promise<TPromise>> resolveHandler;

				private PromiseArgResolvePromise() { }

				public static PromiseArgResolvePromise<TArg, TPromise> GetOrCreate(Func<TArg, Promise<TPromise>> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseArgResolvePromise<TArg, TPromise>) pool.Pop() : new PromiseArgResolvePromise<TArg, TPromise>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (resolveHandler == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					var tempResolve = resolveHandler;
					resolveHandler = null;
					var promise = tempResolve.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
					Validate(promise);
#endif
					SubscribeProgress(promise);
					return promise;
				}
			}

			public sealed class PromiseVoidResolveDeferred : PromiseWaitDeferred<PromiseVoidResolveDeferred>
			{
				private Func<Action<Deferred>> resolveHandler;

				private PromiseVoidResolveDeferred() { }

				public static PromiseVoidResolveDeferred GetOrCreate(Func<Action<Deferred>> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseVoidResolveDeferred) pool.Pop() : new PromiseVoidResolveDeferred();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (resolveHandler == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					var temp = resolveHandler;
					resolveHandler = null;
					Action<Deferred> deferredDelegate = temp.Invoke();
#if DEBUG
					Validate(deferredDelegate);
#endif
					// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
					var promise = New(deferredDelegate);
					return promise;
				}
			}

			public sealed class PromiseArgResolveDeferred<TArg> : PromiseWaitDeferred<PromiseArgResolveDeferred<TArg>>
			{
				private Func<TArg, Action<Deferred>> resolveHandler;

				private PromiseArgResolveDeferred() { }

				public static PromiseArgResolveDeferred<TArg> GetOrCreate(Func<TArg, Action<Deferred>> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseArgResolveDeferred<TArg>) pool.Pop() : new PromiseArgResolveDeferred<TArg>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (resolveHandler == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					var temp = resolveHandler;
					resolveHandler = null;
					Action<Deferred> deferredDelegate = temp.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
					Validate(deferredDelegate);
#endif
					// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
					var promise = New(deferredDelegate);
					return promise;
				}
			}

			public sealed class PromiseVoidResolveDeferred<TDeferred> : PromiseWaitDeferred<TDeferred, PromiseVoidResolveDeferred<TDeferred>>
			{
				private Func<Action<Deferred>> resolveHandler;

				private PromiseVoidResolveDeferred() { }

				public static PromiseVoidResolveDeferred<TDeferred> GetOrCreate(Func<Action<Deferred>> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseVoidResolveDeferred<TDeferred>) pool.Pop() : new PromiseVoidResolveDeferred<TDeferred>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (resolveHandler == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					var temp = resolveHandler;
					resolveHandler = null;
					Action<Deferred> deferredDelegate = temp.Invoke();
#if DEBUG
					Validate(deferredDelegate);
#endif
					// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
					var promise = New(deferredDelegate);
					return promise;
				}
			}

			public sealed class PromiseArgResolveDeferred<TArg, TDeferred> : PromiseWaitDeferred<TDeferred, PromiseArgResolveDeferred<TArg, TDeferred>>
			{
				private Func<TArg, Action<Deferred>> resolveHandler;

				private PromiseArgResolveDeferred() { }

				public static PromiseArgResolveDeferred<TArg, TDeferred> GetOrCreate(Func<TArg, Action<Deferred>> resolveHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseArgResolveDeferred<TArg, TDeferred>) pool.Pop() : new PromiseArgResolveDeferred<TArg, TDeferred>();
					promise.resolveHandler = resolveHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					resolveHandler = null;
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (resolveHandler == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					var temp = resolveHandler;
					resolveHandler = null;
					Action<Deferred> deferredDelegate = temp.Invoke(((IValueContainer<TArg>) feed).Value);
#if DEBUG
					Validate(deferredDelegate);
#endif
					// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
					var promise = New(deferredDelegate);
					return promise;
				}
			}
#endregion

#region Reject Promises
			// Used IDelegate to reduce the amount of classes I would have to write to handle catches (Composition Over Inheritance).
			// I'm less concerned about performance for catches since exceptions are expensive anyway, and they are expected to be used less often than .Then(onResolved).
			public sealed class PromiseReject : PoolablePromise<PromiseReject>
			{
				private IDelegate rejectHandler;

				private PromiseReject() { }

				public static PromiseReject GetOrCreate(IDelegate rejectHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseReject) pool.Pop() : new PromiseReject();
					promise.rejectHandler = rejectHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (rejectHandler.TryInvoke(rejectVal))
					{
						return null;
					}
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					rejectHandler.Dispose();
					return base.ResolveVirtual(feed);
				}
			}

			public sealed class PromiseReject<T> : PoolablePromise<T, PromiseReject<T>>
			{
				private IDelegate<T> rejectHandler;

				private PromiseReject() { }

				public static PromiseReject<T> GetOrCreate(IDelegate<T> rejectHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseReject<T>) pool.Pop() : new PromiseReject<T>();
					promise.rejectHandler = rejectHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					rejectHandler = null;
					if (rejectHandler.TryInvoke(rejectVal, out _value))
					{
						return null;
					}
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					rejectHandler.Dispose();
					return base.ResolveVirtual(feed);
				}
			}

			public sealed class PromiseRejectPromise : PromiseWaitPromise<PromiseRejectPromise>
			{
				private IDelegate<Promise> rejectHandler;

				private PromiseRejectPromise() { }

				public static PromiseRejectPromise GetOrCreate(IDelegate<Promise> rejectHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseRejectPromise) pool.Pop() : new PromiseRejectPromise();
					promise.rejectHandler = rejectHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (rejectHandler == null)
					{
						// The returned promise is rejecting this.
						return base.RejectVirtual(rejectVal);
					}

					_state = PromiseState.Pending;

					var temp = rejectHandler;
					rejectHandler = null;
					Promise promise;
					if (temp.TryInvoke(rejectVal, out promise))
					{
#if DEBUG
						Validate(promise);
#endif
						SubscribeProgress(promise);
						return promise;
					}
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					rejectHandler.Dispose();
					rejectHandler = null;
					return base.ResolveVirtual(feed);
				}
			}

			public sealed class PromiseRejectPromise<TPromise> : PromiseWaitPromise<TPromise, PromiseRejectPromise<TPromise>>
			{
				private IDelegate<Promise<TPromise>> rejectHandler;

				private PromiseRejectPromise() { }

				public static PromiseRejectPromise<TPromise> GetOrCreate(IDelegate<Promise<TPromise>> rejectHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseRejectPromise<TPromise>) pool.Pop() : new PromiseRejectPromise<TPromise>();
					promise.rejectHandler = rejectHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (rejectHandler == null)
					{
						// The returned promise is rejecting this.
						return base.RejectVirtual(rejectVal);
					}

					_state = PromiseState.Pending;

					var temp = rejectHandler;
					rejectHandler = null;
					Promise<TPromise> promise = null;
					if (temp.TryInvoke(rejectVal, out promise))
					{
#if DEBUG
						Validate(promise);
#endif
						SubscribeProgress(promise);
						return promise;
					}
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					rejectHandler.Dispose();
					rejectHandler = null;
					return base.ResolveVirtual(feed);
				}
			}

			public sealed class PromiseRejectDeferred : PromiseWaitDeferred<PromiseRejectDeferred>
			{
				private IDelegate<Action<Deferred>> rejectHandler;

				private PromiseRejectDeferred() { }

				public static PromiseRejectDeferred GetOrCreate(IDelegate<Action<Deferred>> rejectHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseRejectDeferred) pool.Pop() : new PromiseRejectDeferred();
					promise.rejectHandler = rejectHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (rejectHandler == null)
					{
						// The returned promise is rejecting this.
						return base.RejectVirtual(rejectVal);
					}

					_state = PromiseState.Pending;

					var temp = rejectHandler;
					rejectHandler = null;
					Action<Deferred> deferredDelegate;
					if (temp.TryInvoke(rejectVal, out deferredDelegate))
					{
#if DEBUG
						Validate(deferredDelegate);
#endif
						// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
						return New(deferredDelegate);
					}
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					rejectHandler.Dispose();
					rejectHandler = null;
					return base.ResolveVirtual(feed);
				}
			}

			public sealed class PromiseRejectDeferred<TDeferred> : PromiseWaitDeferred<TDeferred, PromiseRejectDeferred<TDeferred>>
			{
				private IDelegate<Action<Deferred>> rejectHandler;

				private PromiseRejectDeferred() { }

				public static PromiseRejectDeferred<TDeferred> GetOrCreate(IDelegate<Action<Deferred>> rejectHandler)
				{
					var promise = pool.IsNotEmpty ? (PromiseRejectDeferred<TDeferred>) pool.Pop() : new PromiseRejectDeferred<TDeferred>();
					promise.rejectHandler = rejectHandler;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (rejectHandler == null)
					{
						// The returned promise is rejecting this.
						return base.RejectVirtual(rejectVal);
					}

					_state = PromiseState.Pending;

					var temp = rejectHandler;
					rejectHandler = null;
					Action<Deferred> deferredDelegate;
					if (rejectHandler.TryInvoke(rejectVal, out deferredDelegate))
					{
#if DEBUG
						Validate(deferredDelegate);
#endif
						// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
						return New(deferredDelegate);
					}
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					rejectHandler.Dispose();
					rejectHandler = null;
					return base.ResolveVirtual(feed);
				}
    			}
#endregion

#region Resolve or Reject Promises
			public sealed class PromiseResolveReject : PoolablePromise<PromiseResolveReject>
			{
				IDelegate onResolved, onRejected;

				private PromiseResolveReject() { }

				public static PromiseResolveReject GetOrCreate(IDelegate onResolved, IDelegate onRejected)
				{
					var promise = pool.IsNotEmpty ? (PromiseResolveReject) pool.Pop() : new PromiseResolveReject();
					promise.onResolved = onResolved;
					promise.onRejected = onRejected;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					onResolved.Dispose();

					if (onRejected.TryInvoke(rejectVal))
					{
						return null;
					}
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					onRejected.Dispose();
					
					onResolved.Invoke(feed);
					return null;
				}
			}

			public sealed class PromiseResolveReject<T> : PoolablePromise<T, PromiseResolveReject<T>>
			{
				IDelegate<T> onResolved, onRejected;

				private PromiseResolveReject() { }

				public static PromiseResolveReject<T> GetOrCreate(IDelegate<T> onResolved, IDelegate<T> onRejected)
				{
					var promise = pool.IsNotEmpty ? (PromiseResolveReject<T>) pool.Pop() : new PromiseResolveReject<T>();
					promise.onResolved = onResolved;
					promise.onRejected = onRejected;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					onResolved.Dispose();

					if (onRejected.TryInvoke(rejectVal, out _value))
					{
						return null;
					}
					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					onRejected.Dispose();

					_value = onResolved.Invoke(feed);
					return null;
				}
			}

			public sealed class PromiseResolveRejectPromise : PromiseWaitPromise<PromiseResolveRejectPromise>
			{
				IDelegate<Promise> onResolved, onRejected;

				private PromiseResolveRejectPromise() { }

				public static PromiseResolveRejectPromise GetOrCreate(IDelegate<Promise> onResolved, IDelegate<Promise> onRejected)
				{
					var promise = pool.IsNotEmpty ? (PromiseResolveRejectPromise) pool.Pop() : new PromiseResolveRejectPromise();
					promise.onRejected = onRejected;
					promise.onRejected = onRejected;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (onResolved == null)
					{
						// The returned promise is resolving this.
						return base.RejectVirtual(rejectVal);
					}

					_state = PromiseState.Pending;

					onResolved.Dispose();
					onResolved = null;

					var temp = onRejected;
					onRejected = null;
					Promise promise;
					if (temp.TryInvoke(rejectVal, out promise))
					{
#if DEBUG
						Validate(promise);
#endif
						SubscribeProgress(promise);
						return promise;
					}

					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (onResolved == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					onRejected.Dispose();
					onRejected = null;

					var temp = onResolved;
					onResolved = null;
					var promise = temp.Invoke(feed);
#if DEBUG
					Validate(promise);
#endif
					SubscribeProgress(promise);
					return promise;
				}
			}

			public sealed class PromiseResolveRejectPromise<TPromise> : PromiseWaitPromise<TPromise, PromiseResolveRejectPromise<TPromise>>
			{
				IDelegate<Promise<TPromise>> onResolved, onRejected;

				private PromiseResolveRejectPromise() { }

				public static PromiseResolveRejectPromise<TPromise> GetOrCreate(IDelegate<Promise<TPromise>> onResolved, IDelegate<Promise<TPromise>> onRejected)
				{
					var promise = pool.IsNotEmpty ? (PromiseResolveRejectPromise<TPromise>) pool.Pop() : new PromiseResolveRejectPromise<TPromise>();
					promise.onRejected = onRejected;
					promise.onRejected = onRejected;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (onResolved == null)
					{
						// The returned promise is resolving this.
						return base.RejectVirtual(rejectVal);
					}

					_state = PromiseState.Pending;

					onResolved.Dispose();
					onResolved = null;

					var temp = onRejected;
					onRejected = null;
					Promise<TPromise> promise;
					if (temp.TryInvoke(rejectVal, out promise))
					{
#if DEBUG
						Validate(promise);
#endif
						SubscribeProgress(promise);
						return promise;
					}

					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (onResolved == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					onRejected.Dispose();
					onRejected = null;

					var temp = onResolved;
					onResolved = null;
					var promise = temp.Invoke(feed);
#if DEBUG
					Validate(promise);
#endif
					SubscribeProgress(promise);
					return promise;
				}
			}

			public sealed class PromiseResolveRejectDeferred : PromiseWaitDeferred<PromiseResolveRejectDeferred>
			{
				IDelegate<Action<Deferred>> onResolved, onRejected;

				private PromiseResolveRejectDeferred() { }

				public static PromiseResolveRejectDeferred GetOrCreate(IDelegate<Action<Deferred>> onResolved, IDelegate<Action<Deferred>> onRejected)
				{
					var promise = pool.IsNotEmpty ? (PromiseResolveRejectDeferred) pool.Pop() : new PromiseResolveRejectDeferred();
					promise.onRejected = onRejected;
					promise.onRejected = onRejected;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (onResolved == null)
					{
						// The returned promise is resolving this.
						return base.RejectVirtual(rejectVal);
					}

					_state = PromiseState.Pending;

					onResolved.Dispose();
					onResolved = null;

					var temp = onRejected;
					onRejected = null;
					Action<Deferred> deferredDelegate;
					if (temp.TryInvoke(rejectVal, out deferredDelegate))
					{
#if DEBUG
						Validate(deferredDelegate);
#endif
						// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
						return New(deferredDelegate);
					}

					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (onResolved == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					onRejected.Dispose();
					onRejected = null;

					var temp = onResolved;
					onResolved = null;
					var deferredDelegate = temp.Invoke(feed);
#if DEBUG
					Validate(deferredDelegate);
#endif
					// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
					return New(deferredDelegate);
				}
			}

			public sealed class PromiseResolveRejectDeferred<TDeferred> : PromiseWaitDeferred<TDeferred, PromiseResolveRejectDeferred<TDeferred>>
			{
				IDelegate<Action<Deferred>> onResolved, onRejected;

				private PromiseResolveRejectDeferred() { }

				public static PromiseResolveRejectDeferred<TDeferred> GetOrCreate(IDelegate<Action<Deferred>> onResolved, IDelegate<Action<Deferred>> onRejected)
				{
					var promise = pool.IsNotEmpty ? (PromiseResolveRejectDeferred<TDeferred>) pool.Pop() : new PromiseResolveRejectDeferred<TDeferred>();
					promise.onRejected = onRejected;
					promise.onRejected = onRejected;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (onResolved == null)
					{
						// The returned promise is resolving this.
						return base.RejectVirtual(rejectVal);
					}

					_state = PromiseState.Pending;

					onResolved.Dispose();
					onResolved = null;

					var temp = onRejected;
					onRejected = null;
					Action<Deferred> deferredDelegate;
					if (temp.TryInvoke(rejectVal, out deferredDelegate))
					{
#if DEBUG
						Validate(deferredDelegate);
#endif
						// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
						return New(deferredDelegate);
					}

					return base.RejectVirtual(rejectVal);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (onResolved == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					_state = PromiseState.Pending;

					onRejected.Dispose();
					onRejected = null;

					var temp = onResolved;
					onResolved = null;
					var deferredDelegate = temp.Invoke(feed);
#if DEBUG
					Validate(deferredDelegate);
#endif
					// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
					return New(deferredDelegate);
				}
			}
#endregion

#region Complete Promises
			public sealed class PromiseComplete : PoolablePromise<PromiseComplete>
			{
				private Action onComplete;

				private PromiseComplete() { }

				public static PromiseComplete GetOrCreate(Action onComplete)
				{
					var promise = pool.IsNotEmpty ? (PromiseComplete) pool.Pop() : new PromiseComplete();
					promise.onComplete = onComplete;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					return ResolveVirtual(null);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					var tempResolve = onComplete;
					onComplete = null;
					tempResolve.Invoke();
					return null;
				}
			}

			public sealed class PromiseComplete<T> : PoolablePromise<T, PromiseComplete<T>>
			{
				private Func<T> onComplete;

				private PromiseComplete() { }

				public static PromiseComplete<T> GetOrCreate(Func<T> onComplete)
				{
					var promise = pool.IsNotEmpty ? (PromiseComplete<T>) pool.Pop() : new PromiseComplete<T>();
					promise.onComplete = onComplete;
					promise.Reset();
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					return ResolveVirtual(null);
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					var tempResolve = onComplete;
					onComplete = null;
					_value = tempResolve.Invoke();
					return null;
				}
			}

			public sealed class PromiseCompletePromise : PromiseWaitPromise<PromiseCompletePromise>
			{
				private Func<Promise> onComplete;

				private PromiseCompletePromise() { }

				public static PromiseCompletePromise GetOrCreate(Func<Promise> onComplete)
				{
					var promise = pool.IsNotEmpty ? (PromiseCompletePromise) pool.Pop() : new PromiseCompletePromise();
					promise.onComplete = onComplete;
					promise.Reset();
					return promise;
				}

				private Promise Complete()
				{
					_state = PromiseState.Pending;

					var tempResolve = onComplete;
					onComplete = null;
					var promise = tempResolve.Invoke();
#if DEBUG
					Validate(promise);
#endif
					SubscribeProgress(promise);
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (onComplete == null)
					{
						// The returned promise is resolving this.
						return base.RejectVirtual(rejectVal);
					}

					return Complete();
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (onComplete == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					return Complete();
				}
			}

			public sealed class PromiseCompletePromise<T> : PromiseWaitPromise<T, PromiseCompletePromise<T>>
			{
				private Func<Promise<T>> onComplete;

				private PromiseCompletePromise() { }

				public static PromiseCompletePromise<T> GetOrCreate(Func<Promise<T>> onComplete)
				{
					var promise = pool.IsNotEmpty ? (PromiseCompletePromise<T>) pool.Pop() : new PromiseCompletePromise<T>();
					promise.onComplete = onComplete;
					promise.Reset();
					return promise;
				}

				private Promise Complete()
				{
					_state = PromiseState.Pending;

					var tempResolve = onComplete;
					onComplete = null;
					var promise = tempResolve.Invoke();
#if DEBUG
					Validate(promise);
#endif
					SubscribeProgress(promise);
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (onComplete == null)
					{
						// The returned promise is resolving this.
						return base.RejectVirtual(rejectVal);
					}

					return Complete();
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (onComplete == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					return Complete();
				}
			}

			public sealed class PromiseCompleteDeferred : PromiseWaitDeferred<PromiseCompleteDeferred>
			{
				Func<Action<Deferred>> onComplete;

				private PromiseCompleteDeferred() { }

				public static PromiseCompleteDeferred GetOrCreate(Func<Action<Deferred>> onComplete)
				{
					var promise = pool.IsNotEmpty ? (PromiseCompleteDeferred) pool.Pop() : new PromiseCompleteDeferred();
					promise.onComplete = onComplete;
					promise.Reset();
					return promise;
				}

				private Promise Complete()
				{
					_state = PromiseState.Pending;

					var temp = onComplete;
					onComplete = null;
					Action<Deferred> deferredDelegate = temp.Invoke();
#if DEBUG
					Validate(deferredDelegate);
#endif
					// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
					var promise = New(deferredDelegate);
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (onComplete == null)
					{
						// The returned promise is resolving this.
						return base.RejectVirtual(rejectVal);
					}

					return Complete();
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (onComplete == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					return Complete();
				}
			}

			public sealed class PromiseCompleteDeferred<T> : PromiseWaitDeferred<T, PromiseCompleteDeferred<T>>
			{
				Func<Action<Deferred>> onComplete;

				private PromiseCompleteDeferred() { }

				public static PromiseCompleteDeferred<T> GetOrCreate(Func<Action<Deferred>> onComplete)
				{
					var promise = pool.IsNotEmpty ? (PromiseCompleteDeferred<T>) pool.Pop() : new PromiseCompleteDeferred<T>();
					promise.onComplete = onComplete;
					promise.Reset();
					return promise;
				}

				private Promise Complete()
				{
					_state = PromiseState.Pending;

					var temp = onComplete;
					onComplete = null;
					Action<Deferred> deferredDelegate = temp.Invoke();
#if DEBUG
					Validate(deferredDelegate);
#endif
					// TODO: optimize this. Don't return a new promise, just hook up a new deferred to this promise.
					var promise = New(deferredDelegate);
					return promise;
				}

				protected override Promise RejectVirtual(IValueContainer rejectVal)
				{
					if (onComplete == null)
					{
						// The returned promise is resolving this.
						return base.RejectVirtual(rejectVal);
					}

					return Complete();
				}

				protected override Promise ResolveVirtual(Promise feed)
				{
					if (onComplete == null)
					{
						// The returned promise is resolving this.
						return base.ResolveVirtual(feed);
					}

					return Complete();
				}
			}
#endregion

#region Control Promises
			// TODO
			public sealed class AllPromise : PromiseWaitPromise<AllPromise>
			{
			}
#endregion
		}
	}
}