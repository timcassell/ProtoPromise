using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	public abstract class ADeferred
	{
		internal Dictionary<Type, IDelegateArg> notificationsInternal;

		public PromiseState State { get { return _promise.State; } }

		protected Promise _promise;

		internal ADeferred(Promise promise)
		{
			promise.DeferredInternal = this;
			_promise = promise;
		}

		internal void NotificationInternal<T>(Action<T> onNotification)
		{
			Type type = typeof(T);
			if (notificationsInternal == null)
			{
				notificationsInternal = new Dictionary<Type, IDelegateArg>(1)
				{
					{ type, new DelegateArg<T>(onNotification) }
				};
				return;
			}
			IDelegateArg del;
			if (notificationsInternal.TryGetValue(type, out del))
			{
				((DelegateArg<T>) del).AddCallback(onNotification);
			}
			else
			{
				notificationsInternal.Add(type, new DelegateArg<T>(onNotification));
			}
		}

		public void Notify<T>(T value)
		{
			if (State != PromiseState.Pending)
			{
				UnityEngine.Debug.LogWarning("Deferred.Notify - Deferred is not in the pending state.");
				return;
			}
			if (notificationsInternal == null)
			{
				return;
			}
			IDelegateArg del;
			if (notificationsInternal.TryGetValue(typeof(T), out del))
			{
				((DelegateArg<T>) del).Invoke(value);
			}
		}

		private static System.Text.StringBuilder sb;

		public void Reject<TReject>(TReject reason)
		{
			if (State != PromiseState.Pending)
			{
				UnityEngine.Debug.LogError("Deferred.Reject - Deferred is not in the pending state. Attempted reject reason:\n" + reason);
				return;
			}

			if (sb == null)
			{
				sb = new System.Text.StringBuilder(new System.Diagnostics.StackTrace(1, true).ToString());
			}
			else
			{
				sb.Append(new System.Diagnostics.StackTrace(1, true).ToString());
			}

			// Format stacktrace to match "throw exception" so that double-clicking log in Unity console will go to the proper line.
			string stackTrace =
				sb.Remove(0, 1)
				.Replace(":line ", ":")
				.Replace("\n ", " \n")
				.Replace("(", " (")
				.Replace(") in", ") [0x00000] in") // Not sure what "[0x00000]" is, but it's necessary for Unity's parsing.
				.Append(" ")
				.ToString();

			UnhandledException rejectValue;
			if (typeof(Exception).IsAssignableFrom(typeof(TReject)))
			{
				var temp = new UnhandledExceptionException();
				temp.SetValue(reason as Exception, stackTrace);
				rejectValue = temp;
			}
			else
			{
				var temp = new UnhandledException<TReject>();
				temp.SetValue(reason, stackTrace);
				rejectValue = temp;
			}

			_promise.RejectInternal(rejectValue);
			Promise.ContinueHandlingInternal(_promise);
		}
	}

	public sealed class Deferred : ADeferred, IPoolable
	{
		bool IPoolable.CanPool { get { return ((IPoolable) Promise).CanPool; } }

		void IPoolable.OptIn()
		{
			((IPoolable) Promise).OptIn();
		}

		void IPoolable.OptOut()
		{
			((IPoolable) Promise).OptOut();
		}

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

	public sealed class Deferred<T> : ADeferred, IPoolable
	{
		bool IPoolable.CanPool { get { return ((IPoolable) Promise).CanPool; } }

		void IPoolable.OptIn()
		{
			((IPoolable) Promise).OptIn();
		}

		void IPoolable.OptOut()
		{
			((IPoolable) Promise).OptOut();
		}

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