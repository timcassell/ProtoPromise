using System;

namespace ProtoPromise
{
	public abstract class ADeferred : IPoolable
	{
		void IPoolable.OptIn()
		{
			((IPoolable) _promise).OptIn();
		}

		void IPoolable.OptOut()
		{
			((IPoolable) _promise).OptOut();
		}

		//internal Dictionary<Type, IDelegateArg> notificationsInternal;

		public PromiseState State { get { return _promise._state; } }

		protected Promise _promise;

		internal ADeferred(Promise promise)
		{
			promise.deferredInternal = this;
			_promise = promise;
		}

		// TODO
		internal void NotificationInternal<T>(Action<T> onNotification)
		{
			//Type type = typeof(T);
			//if (notificationsInternal == null)
			//{
			//	notificationsInternal = new Dictionary<Type, IDelegateArg>(1)
			//	{
			//		{ type, new DelegateArgVoid<T>(onNotification) }
			//	};
			//	return;
			//}
			//IDelegateArg del;
			//if (notificationsInternal.TryGetValue(type, out del))
			//{
			//	((DelegateArgVoid<T>) del).AddCallback(onNotification);
			//}
			//else
			//{
			//	notificationsInternal.Add(type, new DelegateArgVoid<T>(onNotification));
			//}
		}

		public void Notify<T>(T value)
		{
			if (State != PromiseState.Pending)
			{
				UnityEngine.Debug.LogWarning("Deferred.Notify - Deferred is not in the pending state.");
				return;
			}
			//if (notificationsInternal == null)
			//{
			//	return;
			//}
			//IDelegateArg del;
			//if (notificationsInternal.TryGetValue(typeof(T), out del))
			//{
			//	((DelegateArgVoid<T>) del).Invoke(value);
			//}
		}

		public void Cancel()
		{
			if (State != PromiseState.Pending)
			{
				UnityEngine.Debug.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
				return;
			}

			_promise.Cancel();
		}

		public void Reject<TReject>(TReject reason)
		{
			RejectInternal(reason);
		}

		public void Reject()
		{
			if (State != PromiseState.Pending)
			{
				UnityEngine.Debug.LogError("Deferred.Reject - Deferred is not in the pending state.");
				return;
			}

			RejectInternal();
		}

		internal void RejectInternal()
		{
			string stackTrace = Promise.GetStackTrace(3);
			UnhandledException rejectValue = new UnhandledException().SetStackTrace(stackTrace);
			RejectInternal(rejectValue);
		}

		internal void RejectInternal(UnhandledException rejectValue)
		{
			_promise.RejectInternal(rejectValue);
			Promise.ContinueHandlingInternal(_promise);
		}

		internal void RejectInternal<TReject>(TReject reason)
		{
			if (State != PromiseState.Pending)
			{
				UnityEngine.Debug.LogError("Deferred.Reject - Deferred is not in the pending state. Attempted reject reason:\n" + reason);
				return;
			}

			string stackTrace = Promise.GetStackTrace(3);

			UnhandledException rejectValue;
			if (typeof(Exception).IsAssignableFrom(typeof(TReject)))
			{
				rejectValue = new UnhandledExceptionException().SetValue(reason as Exception, stackTrace);
			}
			else
			{
				rejectValue = new UnhandledException<TReject>().SetValue(reason, stackTrace);
			}

			RejectInternal(rejectValue);
		}
	}

	public sealed class Deferred : ADeferred
	{
		public Promise Promise { get { return _promise; } }

		internal Deferred(Promise promise) : base(promise) { }

		public void Resolve()
		{
			if (State != PromiseState.Pending)
			{
				UnityEngine.Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			Promise.ResolveInternal(null);
			Promise.ContinueHandlingInternal(Promise);
		}
	}

	public sealed class Deferred<T> : ADeferred
	{
		public Promise<T> Promise { get { return (Promise<T>) _promise; } }

		internal Deferred(Promise<T> promise) : base(promise) { }

		public void Resolve(T arg)
		{
			if (State != PromiseState.Pending)
			{
				UnityEngine.Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			Promise.ResolveInternal(arg);
			ProtoPromise.Promise.ContinueHandlingInternal(Promise);
		}
	}
}