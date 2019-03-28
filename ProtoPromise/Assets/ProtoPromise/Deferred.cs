using System;
using System.Collections.Generic;

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

		/// <summary>
		/// Report progress between 0 and 1.
		/// </summary>
		public void ReportProgress(float progress)
		{
#if DEBUG
			if (progress < 0f || progress > 1f)
			{
				throw new ArgumentOutOfRangeException("progress", "Must be between 0 and 1 inclusive.");
			}
#endif
			if (State != PromiseState.Pending)
			{
				Logger.LogWarning("Deferred.ReportProgress - Deferred is not in the pending state.");
				return;
			}

			// TODO: Hook this up.
			// promise1.Then(() => promise2).Then(() => promise3).Progress(value => {})
			// should be reported as (promise1Progress + promise2Progress + promise3Progress)/3
		}

		public void Cancel()
		{
			if (State != PromiseState.Pending)
			{
				Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
				return;
			}

			_promise.Cancel();
		}

		public void Cancel<TCancel>(TCancel reason)
		{
			if (State != PromiseState.Pending)
			{
				Logger.LogWarning("Deferred.Cancel - Deferred is not in the pending state.");
				return;
			}

			_promise.Cancel(reason);
		}

		public void Reject<TReject>(TReject reason)
		{
			RejectInternal(reason);
		}

		public void Reject()
		{
			if (State != PromiseState.Pending)
			{
				Logger.LogError("Deferred.Reject - Deferred is not in the pending state.");
				return;
			}

			RejectInternal();
		}

		internal void RejectInternal()
		{
			// TODO: pool exceptions
			UnhandledException rejectValue = new UnhandledException();
#if DEBUG
			rejectValue.SetStackTrace(Promise.GetStackTrace(3));
#endif
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
				Logger.LogError("Deferred.Reject - Deferred is not in the pending state. Attempted reject reason:\n" + reason);
				return;
			}

			UnhandledException rejectValue;
			// Is reason an exception (including if it's null)?
			if (typeof(Exception).IsAssignableFrom(typeof(TReject)))
			{
				rejectValue = new UnhandledExceptionException().SetValue(reason as Exception);
#if DEBUG
				rejectValue.SetStackTrace(Promise.GetStackTrace(3));
#endif
			}
			else
			{
				rejectValue = new UnhandledException<TReject>().SetValue(reason);
#if DEBUG
				rejectValue.SetStackTrace(Promise.GetStackTrace(3));
#endif
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
				Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
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
				Logger.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			Promise.ResolveInternal(arg);
			ProtoPromise.Promise.ContinueHandlingInternal(Promise);
		}
	}
}