using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoPromise
{
	internal class UnhandledException : Exception
	{
		public UnhandledException(Exception innerException) : this(innerException, string.Empty) { }
		public UnhandledException(Exception innerException, string stackTrace) : base("An exception was encountered that was not handled.", innerException)
		{
			_stackTrace = stackTrace;
		}

		readonly string _stackTrace;
		public override string StackTrace
		{
			get
			{
				return _stackTrace;
			}
		}
	}

	public sealed class FinalYield : CustomYieldInstruction
	{
		bool wait = true;
		Action callback;

		internal FinalYield() { }

		public override bool keepWaiting
		{
			get
			{
				return wait;
			}
		}

		internal void AddCallback(Action action)
		{
			callback += action;
			wait = true;
		}

		internal void Invoke()
		{
			if (callback != null)
			{
				callback.Invoke();
				callback = null;
			}
			wait = false;
		}
	}

	public abstract class ADeferred
	{
		private Dictionary<Type, IDelegateArg> notifications;
		//private FinalYield _finally;

		public PromiseState StateInternal { get; protected set; }

		internal ADeferred()
		{
			StateInternal = PromiseState.Pending;
		}

		//protected void OnFinished()
		//{
		//	if (_finally != null)
		//	{
		//		//StateInternal = PromiseState.Final;
		//		_finally.Invoke();
		//	}
		//}

		//internal FinalYield FinallyInternal()
		//{
		//	if (_finally == null)
		//	{
		//		_finally = new FinalYield();
		//		//if (StateInternal == PromiseState.Final)
		//		//{
		//		//	// chain is complete
		//		//	OnFinished();
		//		//}
		//	}
		//	return _finally;
		//}

		//internal FinalYield FinallyInternal(Action callback)
		//{
		//	FinalYield final = FinallyInternal();
		//	if (StateInternal == PromiseState.Pending)
		//	{
		//		final.AddCallback(callback);
		//	}
		//	else
		//	{
		//		callback.Invoke();
		//	}
		//	return final;
		//}

		internal void NotificationInternal<T>(Action<T> onNotification)
		{
			Type type = typeof(T);
			if (notifications == null)
			{
				notifications = new Dictionary<Type, IDelegateArg>(1)
				{
					{ type, new DelegateArg<T>(onNotification) }
				};
				return;
			}
			IDelegateArg del;
			if (notifications.TryGetValue(type, out del))
			{
				((DelegateArg<T>) del).AddCallback(onNotification);
			}
			else
			{
				notifications.Add(type, new DelegateArg<T>(onNotification));
			}
		}

		public void Notify<T>(T value)
		{
			if (notifications == null)
			{
				return;
			}
			IDelegateArg del;
			if (notifications.TryGetValue(typeof(T), out del))
			{
				((DelegateArg<T>) del).Invoke(value);
			}
		}

		protected abstract void RejectProtected(Exception exception);

		public void Reject<TException>(TException reason) where TException : Exception
		{
			if (StateInternal != PromiseState.Pending)
			{
				Debug.LogWarning("Deferred.Throw - Deferred is not in the pending state.");
				return;
			}

			Exception ex;
			if (reason == null)
			{
				ex = new NullReferenceException();
			}
			else
			{
				ex = reason;
			}
			if (string.IsNullOrEmpty(ex.StackTrace))
			{
				// Format stacktrace to match "throw exception" so that double-clicking log in Unity console will go to the proper line.
				System.Text.StringBuilder sb = new System.Text.StringBuilder(new System.Diagnostics.StackTrace(1, true).ToString())
					.Remove(0, 1)
					.Replace(":line ", ":")
					.Replace("\n ", " \n")
					.Replace("(", " (")
					.Replace(") in", ") [0x00000] in") // Not sure what "[0x00000]" is, but it's necessary for Unity's parsing.
					.Append(" ");
				ex = new UnhandledException(ex, sb.ToString());
			}

			RejectProtected(ex);
		}
	}

	public sealed class Deferred : ADeferred
	{
		public readonly Promise Promise;

		internal Deferred()
		{
			Promise = new Promise(this);
		}

		protected override void RejectProtected(Exception exception)
		{
			Promise.RejectInternal(exception);
		}

		public void Resolve()
		{
			if (StateInternal != PromiseState.Pending)
			{
				Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			StateInternal = PromiseState.Resolved;

			Promise.ResolveInternal(null);
			Promise.ContinueHandlingInternal(Promise);
		}
	}

	public sealed class Deferred<T> : ADeferred
	{
		public readonly Promise<T> Promise;

		internal Deferred()
		{
			Promise = new Promise<T>(this);
		}

		protected override void RejectProtected(Exception exception)
		{
			Promise.RejectInternal(exception);
		}

		public void Resolve(T arg)
		{
			if (StateInternal != PromiseState.Pending)
			{
				Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			StateInternal = PromiseState.Resolved;

			Promise.ResolveInternal(arg);
			ProtoPromise.Promise.ContinueHandlingInternal(Promise);
		}
	}
}