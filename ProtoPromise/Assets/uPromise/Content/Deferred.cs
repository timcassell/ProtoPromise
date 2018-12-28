using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace uPromise
{
	public enum DeferredState
	{
		Pending,
		Resolved,
		Rejected
	}

[StructLayout(LayoutKind.Sequential)]
	public class Deferred
	{
		private event Action<object> _done;
		private event Action<object> _progress;
		private readonly Queue<Func<object, object>> _thenQueue;
		private readonly Dictionary<Type, Action<object>> _failHandlers;

		private static readonly Type _objectType = typeof(object);

		public Deferred()
		{
			_thenQueue = new Queue<Func<object, object>>();
			_failHandlers = new Dictionary<Type, Action<object>>();
			Promise = new Promise(this);
			State = DeferredState.Pending;
		}

		#region Events Invoker

		protected void OnThen(object arg)
		{
			if (_thenQueue != null)
				HandleNextThen(arg);
		}

		protected void OnDone(object arg)
		{
			if (_done != null)
				_done(arg);
		}


		protected void OnFail<TFail>(TFail arg)
		{
			Type argType = arg != null ? arg.GetType() : _objectType;

			bool isHandled = false;

			// Fire Fail event for typed.
			if (_failHandlers.ContainsKey(argType))
			{
				_failHandlers[argType](arg);
				isHandled = true;

				if (argType == _objectType)
					return; // Exit if argType is object so it doesn't fire twice.
			}

			// Fire Fail for object.
			if (_failHandlers.ContainsKey(_objectType))
			{
				_failHandlers[_objectType](arg);
				isHandled = true;
			}

			if (!isHandled)
				Debug.LogWarning("Deferred.OnFail<TFail> - Fail invoked and no one handled it.");
		}

		protected void OnProgress(object arg)
		{
			if (_progress != null)
				_progress(arg);
		}

		#endregion

		public DeferredState State { get; private set; }
		public Promise Promise { get; protected set; }

		#region Public Events API

		public void Done(Action<object> callback)
		{
			_done += callback;
		}

		public void Done(Action callback)
		{
			Action<object> internalCallback = x => callback();
			Done(internalCallback);
		}

		public Promise Fail(Action callback)
		{
			Action<object> internalCallback = x => callback();
			return Fail(internalCallback);
		}

		public Promise Fail(Action<object> callback)
		{
			AddFailHandler(typeof(object), callback);
			return Promise;
		}

		public Promise Fail<TFail>(Action<TFail> callback)
		{
			Action<object> internalCallback = x => callback((TFail)x);
			AddFailHandler(typeof(TFail), internalCallback);
			return Promise;
		}

		public Promise Finally(Action<object> callback)
		{
			Done(callback);
			return Fail(callback);
		}

		public Promise Finally(Action callback)
		{
			Done(callback);
			return Fail(callback);
		}

		public Promise Progress(Action<object> callback)
		{
			_progress += callback;
			return Promise;
		}

		public Promise Then(Func<object, object> callback)
		{
			_thenQueue.Enqueue(callback);
			return Promise;
		}

		public Promise Then(Action callback)
		{
			return Then(x =>
			{
				callback();
				return x;
			});
		}

		#endregion

		#region Public Deferred Command

		public void Resolve(object arg)
		{
			if (State != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			OnThen(arg);
		}

		public void Resolve()
		{
			Resolve(null);
		}

		public void Reject(object arg)
		{
			RejectInternal(arg);
		}

		public void Reject<TFail>(TFail arg)
		{
			RejectInternal(arg);
		}

		public void Reject()
		{
			Reject(null);
		}

		private void RejectInternal<TFail>(TFail arg)
		{
			if (State != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
				return;
			}

			State = DeferredState.Rejected;

			OnFail(arg);
		}

		public void Notify(object arg)
		{
			if (State != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Notify - Deferred is not in the pending state.");
				return;
			}

			OnProgress(arg);
		}

		#endregion

		private void HandleNextThen(object arg)
		{
			if (_thenQueue.Count == 0)
			{
				State = DeferredState.Resolved;
				OnDone(arg);
				return;
			}

			var then = _thenQueue.Dequeue();
			try
			{
				var result = then(arg);
				var promiseResult = result as Promise;
				if (promiseResult == null)
				{
					HandleNextThen(result);
					return;
				}

				promiseResult.Fail(OnFail)
					.Done(HandleNextThen);
			}
			catch (Exception ex)
			{
				Debug.LogError(string.Format("Deferred.HandleDeferred - EXCEPTION! ex={0}", ex));
				OnFail(ex);
				// Exit and it should never be marked as done.
				return;
			}
		}

		private void AddFailHandler(Type type, Action<object> callback)
		{
			if (_failHandlers.ContainsKey(type))
				_failHandlers[type] += callback;
			else
				_failHandlers[type] = callback;
		}
	}

[StructLayout(LayoutKind.Sequential)]
	public class Deferred<T> : Deferred
	{
		public new Promise<T> Promise { get; protected set; }

		public Deferred()
		{
			Promise = new Promise<T>(this);
		}

		public void Resolve(T arg)
		{
			base.Resolve(arg);
		}

		public void Notify(T arg)
		{
			base.Notify(arg);
		}
	}
}