using System;
using System.Collections.Generic;

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

	public abstract class ADeferred : IPoolable, IResetable
	{
		bool IPoolable.CanPool { get { return poolOptsInternal < 0; } }

		void IPoolable.OptIn()
		{
			checked
			{
				--poolOptsInternal;
			}
		}

		void IPoolable.OptOut()
		{
			checked
			{
				++poolOptsInternal;
			}
		}

		void IResetable.Reset()
		{
			poolOptsInternal = 0;
			StateInternal = PromiseState.Pending;
			if (notifications != null)
			{
				notifications.Clear();
			}
		}

		private Dictionary<Type, IDelegateArg> notifications;

		private sbyte poolOptsInternal; // This is an sbyte to preserve memory footprint. Change this to System.Int32(int) if you need to perpetually use one deferred in more than 128 places.

		public PromiseState StateInternal { get; protected set; }

		internal ADeferred()
		{
			StateInternal = PromiseState.Pending;
			if (ObjectPool.poolType == PoolType.OptOut)
			{
				((IPoolable) this).OptIn();
			}
		}

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
			if (StateInternal != PromiseState.Pending)
			{
				UnityEngine.Debug.LogWarning("Deferred.Notify - Deferred is not in the pending state.");
				return;
			}
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
				UnityEngine.Debug.LogError("Deferred.Reject - Deferred is not in the pending state. Attempted reject reason:\n" + reason);
				return;
			}

			Exception ex = reason == null ? new NullReferenceException() : (Exception) reason;
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

	public sealed class Deferred : ADeferred, ILinked<Deferred>
	{
		Deferred ILinked<Deferred>.Next { get; set; }

		public Promise Promise { get; private set; }

		internal void SetPromiseInternal(Promise promise)
		{
			promise.DeferredInternal = this;
			Promise = promise;
		}

		internal Deferred() : base() { }

		protected override void RejectProtected(Exception exception)
		{
			Promise.RejectInternal(exception);
			Promise.ContinueHandlingInternal(Promise);
		}

		public void Resolve()
		{
			if (StateInternal != PromiseState.Pending)
			{
				UnityEngine.Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			StateInternal = PromiseState.Resolved;

			Promise.ResolveInternal(null);
			Promise.ContinueHandlingInternal(Promise);
		}
	}

	public sealed class Deferred<T> : ADeferred, ILinked<Deferred<T>>
	{
		Deferred<T> ILinked<Deferred<T>>.Next { get; set; }

		public Promise<T> Promise { get; private set; }

		internal void SetPromiseInternal(Promise<T> promise)
		{
			promise.DeferredInternal = this;
			Promise = promise;
		}

		internal Deferred() : base() { }

		protected override void RejectProtected(Exception exception)
		{
			Promise.RejectInternal(exception);
			ProtoPromise.Promise.ContinueHandlingInternal(Promise);
		}

		public void Resolve(T arg)
		{
			if (StateInternal != PromiseState.Pending)
			{
				UnityEngine.Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			StateInternal = PromiseState.Resolved;

			Promise.ResolveInternal(arg);
			ProtoPromise.Promise.ContinueHandlingInternal(Promise);
		}
	}
}