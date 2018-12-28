using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoL.Core.Structures.Promise2
{
	public enum DeferredState
	{
		Pending,
		Resolving,
		Resolved,
		Rejected,
		Excepted
	}
	
	internal interface IDelegate
	{
		void Invoke(IDelegate feed);

		bool ContainsCallback { get; }

		void AddCallback(IDelegate source);
	}
	internal interface IDelegateWithArg : IDelegate { }
	internal interface IDelegateWithResult : IDelegate
	{
		Type GetReturnType();
	}
	internal interface IDelegateWithArgAndResult : IDelegateWithArg, IDelegateWithResult { }
	
	internal sealed class DelegateAction : IDelegate
	{
		Action callback;

		public bool ContainsCallback
		{
			get
			{
				return callback != null;
			}
		}

		public void SetCallback(Action action)
		{
			callback = action;
		}

		public void AddCallback(Action action)
		{
			callback += action;
		}

		public void Invoke(IDelegate feed)
		{
			if (callback == null) return;

			callback.Invoke();
		}

		public void Invoke()
		{
			if (callback == null) return;

			callback.Invoke();
		}

		public void AddCallback(IDelegate source)
		{
			callback += ((DelegateAction)source).callback;
		}
	}

	internal sealed class DelegateAction<TArg> : IDelegateWithArg
	{
		Action<TArg> callback;

		public bool ContainsCallback
		{
			get
			{
				return callback != null;
			}
		}

		public void SetCallback(Action<TArg> action)
		{
			callback = action;
		}

		public void AddCallback(Action<TArg> action)
		{
			callback += action;
		}

		public void Invoke(IDelegate feed)
		{
			Invoke(((DelegateFunc<TArg>)feed).value);
		}

		public void Invoke(TArg arg)
		{
			if (callback == null) return;

			callback.Invoke(arg);
		}

		public void AddCallback(IDelegate source)
		{
			callback += ((DelegateAction<TArg>)source).callback;
		}
	}

	internal class DelegateFunc<TResult> : IDelegateWithResult
	{
		Func<TResult> callback;

		public virtual bool ContainsCallback
		{
			get
			{
				return callback != null;
			}
		}

		internal TResult value;

		public void SetCallback(Func<TResult> func)
		{
			callback = func;
		}

		public void AddCallback(Func<TResult> func)
		{
			callback += func;
		}

		public virtual void Invoke(IDelegate feed)
		{
			Invoke();
		}

		public void Invoke()
		{
			if (callback == null) return;

			value = callback.Invoke();
		}

		public virtual void AddCallback(IDelegate source)
		{
			callback += ((DelegateFunc<TResult>)source).callback;
		}

		public Type GetReturnType()
		{
			return typeof(TResult);
		}
	}

	internal sealed class DelegateFunc<TArg, TResult> : DelegateFunc<TResult>, IDelegateWithArgAndResult
	{
		Func<TArg, TResult> callback;

		public override bool ContainsCallback
		{
			get
			{
				return callback != null;
			}
		}

		public void SetCallback(Func<TArg, TResult> func)
		{
			callback = func;
		}

		public void AddCallback(Func<TArg, TResult> func)
		{
			callback += func;
		}

		public override void Invoke(IDelegate feed)
		{
			Invoke(((DelegateFunc<TArg>)feed).value);
		}

		public void Invoke(TArg arg)
		{
			if (callback == null) return;

			value = callback.Invoke(arg);
		}

		public override void AddCallback(IDelegate source)
		{
			callback += ((DelegateFunc<TArg, TResult>)source).callback;
		}
	}

	public abstract class ADeferred
	{
		public DeferredState State { get; private set; }

		private event Action<int, int> _progress;
		private event Action _finally;
		private event Action _done;
		private IDelegate _doneHandler;
		private readonly Dictionary<Type, IDelegateWithArg> _doneHandlers = new Dictionary<Type, IDelegateWithArg>();
		// Contains the last called delegate with result of its type. Used to pass the results into the _doneHandlers callbacks.
		private readonly Dictionary<Type, IDelegate> _doneArgs = new Dictionary<Type, IDelegate>();
		private event Action _fail;
		private readonly Dictionary<Type, IDelegateWithArg> _failHandlers = new Dictionary<Type, IDelegateWithArg>();
		private event Action<Exception> _exception;
		private readonly List<IDelegate> _thenQueue = new List<IDelegate>();
		internal Promise last;

		private IDelegate GetLastDelegate()
		{
			return _thenQueue[_thenQueue.Count - 1];
		}

		protected Dictionary<Type, Promise> cachedPromises = new Dictionary<Type, Promise>();

		internal Promise<T> GetPromise<T>()
		{
			Type type = typeof(Promise<T>);
			Promise promise;
			if (!cachedPromises.TryGetValue(type, out promise))
			{
				promise = new Promise<T>(this);
				cachedPromises.Add(type, promise);
			}
			return (Promise<T>) promise;
		}

		internal Promise GetPromise()
		{
			Type type = typeof(Promise);
			Promise promise;
			if (!cachedPromises.TryGetValue(type, out promise))
			{
				promise = new Promise(this);
				cachedPromises.Add(type, promise);
			}
			return promise;
		}

		internal IDelegate GetDoneDelegate(Type type)
		{
			return _doneArgs[type];
		}

		private static void FillDelegates<TDelegate>(Dictionary<Type, TDelegate> source, Dictionary<Type, TDelegate> destination) where TDelegate : IDelegate
		{
			foreach(var kvp in source)
			{
				TDelegate del;
				if (destination.TryGetValue(kvp.Key, out del))
				{
					del.AddCallback(kvp.Value);
				}
				else
				{
					destination.Add(kvp.Key, kvp.Value);
				}
			}
		}
		
		private int callbacksCompleted = 0;

		protected ADeferred()
		{
			State = DeferredState.Pending;
		}

		protected void OnThen()
		{
			State = DeferredState.Resolving;

			//if (_thenQueue != null)
				//HandleNextThen();
		}

		protected void OnThen<T>(T arg)
		{
			State = DeferredState.Resolving;

			if (_thenQueue != null)
			{
				DelegateFunc<T> del = new DelegateFunc<T>();
				del.value = arg;
				HandleNextThen(del);
			}
		}

		protected void OnDone()
		{
			State = DeferredState.Resolved;

			if (_doneHandler != null)
				_doneHandler.Invoke(GetLastDelegate());

			IDelegate doneResult;
			foreach(var kvp in _doneHandlers)
			{
				if (_doneArgs.TryGetValue(kvp.Key, out doneResult))
				{
					// Pass type result into type callback.
					kvp.Value.Invoke(doneResult);
				}
			}
			
			if (_done != null)
				_done.Invoke();
			
			OnFinished();
		}

		protected void OnFail()
		{
			State = DeferredState.Rejected;

			if (_fail != null)
				_fail.Invoke();
			OnFinished();
		}

		protected void OnFail<TFail>(TFail arg)
		{
			// Fire Fail event for typed.
			IDelegateWithArg del;
			if (_failHandlers.TryGetValue(typeof(TFail), out del))
			{
				((DelegateAction<TFail>)del).Invoke(arg);
			}
			OnFail();
		}

		protected void OnException(Exception exception)
		{
			State = DeferredState.Excepted;

			if (_exception != null)
				_exception.Invoke(exception);
		}

		protected void OnFinished()
		{
			if (_finally != null)
				_finally.Invoke();
		}

		protected void OnProgress()
		{
			if (_progress != null)
				_progress.Invoke(callbacksCompleted, _thenQueue.Count);
		}

		internal void Done(Action callback)
		{
			var del = new DelegateAction();
			del.SetCallback(callback);

			_done += callback;
		}

		internal void Done<T>(Action<T> callback)
		{
			var newDel = new DelegateAction<T>();
			newDel.SetCallback(callback);

			Type type = typeof(T);
			IDelegateWithArg del;
			if (_doneHandlers.TryGetValue(type, out del))
			{
				((DelegateAction<T>)del).AddCallback(callback);
			}
			else
			{
				DelegateAction<T> actionDel = new DelegateAction<T>();
				actionDel.SetCallback(callback);
				_doneHandlers.Add(type, actionDel);
			}
		}

		internal void Fail(Action callback)
		{
			_fail += callback;
		}

		internal void Fail<TFail>(Action<TFail> callback)
		{
			Type type = typeof(TFail);

			IDelegateWithArg del;
			if (_failHandlers.TryGetValue(type, out del))
			{
				((DelegateAction<TFail>)del).AddCallback(callback);
			}
			else
			{
				DelegateAction<TFail> actionDel = new DelegateAction<TFail>();
				actionDel.SetCallback(callback);
				_failHandlers.Add(type, actionDel);
			}
		}

		internal void Fail(Action<Exception> callback)
		{
			_exception += callback;
		}

		internal void Finally(Action callback)
		{
			_finally += callback;
		}

		internal void Progress(Action<int, int> callback)
		{
			_progress += callback;
		}

		internal void Then<T>(Func<T> callback)
		{
			DelegateFunc<T> del = new DelegateFunc<T>();
			del.SetCallback(callback);
			_thenQueue.Add(del);
		}

		internal void Then<T>(Action<T> callback)
		{
			DelegateAction<T> del = new DelegateAction<T>();
			del.SetCallback(callback);
			_thenQueue.Add(del);
		}

		internal void Then(Action callback)
		{
			DelegateAction del = new DelegateAction();
			del.SetCallback(callback);
			_thenQueue.Add(del);
		}

		internal void Then<T, TResult>(Func<T, TResult> callback)
		{
			DelegateFunc<T, TResult> del = new DelegateFunc<T, TResult>();
			del.SetCallback(callback);
			_thenQueue.Add(del);
		}

		public void Reject<TFail>(TFail arg)
		{
			if (State != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
				return;
			}

			OnFail(arg);
		}

		public void Reject()
		{
			if (State != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
				return;
			}

			OnFail();
		}

		private void HandleNextThen(IDelegate lastRunCallback = null)
		{
			if (_thenQueue.Count == callbacksCompleted)
			{
				OnDone();
				return;
			}

			var nextCallback = _thenQueue[callbacksCompleted];

			if (_exception != null)
			{
				try
				{
					InvokeDelegate(nextCallback, lastRunCallback);
				}
				catch (Exception e)
				{
					OnException(e);
				}
			}
			else
			{
				InvokeDelegate(nextCallback, lastRunCallback);
			}
		}

		private void InvokeDelegate(IDelegate invoker, IDelegate valueContainer)
		{
			invoker.Invoke(valueContainer);
			++callbacksCompleted;
			OnProgress();

			if (invoker is DelegateFunc<Promise>)
			{
				var promise = ((DelegateFunc<Promise>) invoker).value;

				FillDelegates(_failHandlers, (promise).Deferred._failHandlers);

				promise
					.Done(() =>
					{
						HandleNextThen(promise.Deferred.GetLastDelegate());

						var otherFinalDelegate = promise.GetDoneDelegate();
						if (otherFinalDelegate is IDelegateWithResult)
						{
							_doneArgs[((IDelegateWithResult) otherFinalDelegate).GetReturnType()] = otherFinalDelegate;
						}
						HandleNextThen(otherFinalDelegate);
					})
					.Fail((Action)OnFail);
				if (_exception != null)
				{
					promise.Fail(OnException);
				}
			}
			else
			{
				if (invoker is IDelegateWithResult)
				{
					_doneArgs[((IDelegateWithResult) invoker).GetReturnType()] = invoker;
				}
				HandleNextThen(invoker);
			}
		}
	}

	public sealed class Deferred : ADeferred
	{
		public readonly Promise Promise;

		public Deferred()
		{
			Promise = new Promise(this);
			cachedPromises[typeof(Promise)] = Promise;
		}

		public void Resolve()
		{
			if (State != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			OnThen();
		}
	}

	public sealed class Deferred<T> : ADeferred
	{
		public readonly Promise<T> Promise;

		public Deferred()
		{
			Promise = new Promise<T>(this);
			cachedPromises[typeof(Promise<T>)] = Promise;
		}

		public void Resolve(T arg)
		{
			if (State != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			OnThen(arg);
		}
	}
}