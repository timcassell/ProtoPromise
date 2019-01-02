using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoPromise
{
	public enum PromiseState
	{
		/// <summary>
		/// Waiting to be resolved or rejected.
		/// </summary>
		Pending,
		/// <summary>
		/// .Done and .Then have resolved successfully.
		/// </summary>
		Resolved,
		/// <summary>
		/// This promise has been rejected or an earlier promise in the chain was rejected and didn't handle the rejection.
		/// </summary>
		Rejected,
		/// <summary>
		/// An exception was encountered in the chain (and no promises before this handled the exception).
		/// </summary>
		Errored
	}

	public partial class Promise : CustomYieldInstruction, IDelegate, ILinked<Promise>
	{
		Promise ILinked<Promise>.Next { get { return NextInternal; } set { NextInternal = value; } }
		IDelegate ILinked<IDelegate>.Next { get { return NextInternal; } set { NextInternal = (Promise) value; } }
		internal Promise NextInternal { get; set; }

		private static int idCounter = 0;
		
		public int id;
		
		protected Action onComplete;
		internal LinkedListStruct<IDelegate> doneHandlersInternal;
		internal LinkedListStruct<ITryInvokable> exceptionHandlersInternal;
		internal LinkedListStruct<ITryInvokable> failHandlersInternal;
		//internal LinkedListStruct<Promise> NextBranchesInternal = new LinkedListStruct<Promise>();
		internal ADeferred DeferredInternal { get; private set; }
		public PromiseState State { get; protected set; }

		internal Promise(ADeferred deferred)
		{
			id = idCounter++;
			DeferredInternal = deferred;
		}

		internal virtual void InvokeInternal(IDelegate feed)
		{
			throw new NotImplementedException();
		}

		void IDelegate.Invoke(IDelegate feed)
		{
			InvokeInternal(feed);
		}

		internal void CompleteInternal()
		{
			Debug.LogWarning(id + " Complete, deferred state: " + DeferredInternal.StateInternal + ", invoking onComplete: " + (onComplete != null));
			if (onComplete != null)
			{
				onComplete.Invoke();
			}
		}

		internal virtual void ResolveInternal()
		{
			for (IDelegate del = doneHandlersInternal.PeekFirst(); del != null; del = del.Next, doneHandlersInternal.TakeFirst())
			{
				del.Invoke(this);
			}

			State = PromiseState.Resolved;
		}

		internal virtual bool TryInvokeInternal<U>(ITryInvokable tryInvoke, U value, out bool invoked) // private protected not supported before c# 7.2, so must use internal.
		{
			return tryInvoke.TryInvoke(value, out invoked);
		}

		internal virtual bool TryInvokeInternal(ITryInvokable tryInvoke) // private protected not supported before c# 7.2, so must use internal.
		{
			return tryInvoke.TryInvoke();
		}

		internal bool TryHandleExceptionInternal(Exception exception, out bool handled) // Use out to let the caller know that the exception was handled, just in case a callback throws another exception.
		{
			handled = TryHandleException(exception, out handled) || TryHandleFail(exception);

			State = PromiseState.Errored;

			return handled;
		}

		private bool TryHandleException(Exception exception, out bool handled)
		{
			for (ITryInvokable del = exceptionHandlersInternal.PeekFirst(); del != null; del = del.Next, exceptionHandlersInternal.TakeFirst())
			{
				if (TryInvokeInternal(del, exception, out handled))
				{
					return true;
				}
			}

			return handled = false;
		}

		internal bool TryHandleFailInternal<TFail>(TFail failValue)
		{
			bool success;
			success = TryHandleFail(failValue) || (typeof(Exception).IsAssignableFrom(typeof(TFail)) && TryHandleException(failValue as Exception, out success));

			State = PromiseState.Rejected;

			return success;
		}

		private bool TryHandleFail<TFail>(TFail failValue)
		{
			bool success;
			for (ITryInvokable del = failHandlersInternal.PeekFirst(); del != null; del = del.Next, failHandlersInternal.TakeFirst())
			{
				if (TryInvokeInternal(del, failValue, out success))
				{
					return true;
				}
			}

			return false;
		}

		internal bool TryHandleFailInternal()
		{
			bool success = TryHandleFail();

			State = PromiseState.Rejected;

			return success;
		}

		bool TryHandleFail()
		{
			for (ITryInvokable del = failHandlersInternal.PeekFirst(); del != null; del = del.Next, failHandlersInternal.TakeFirst())
			{
				if (TryInvokeInternal(del))
				{
					return true;
				}
			}

			return false;
		}

		internal virtual bool TryGetPromiseResultInternal(out Promise promise)
		{
			promise = null;
			return false;
		}

		public override bool keepWaiting
		{
			get
			{
				return State == PromiseState.Pending;
			}
		}

		public Promise Notification<TNotify>(Action<TNotify> onNotification)
		{
			DeferredInternal.NotificationInternal(onNotification);
			return this;
		}

		public void End()
		{
			DeferredInternal.End();
		}

		public Promise Complete(Action onComplete)
		{
			Debug.LogError(id + " Complete, State: " + State);
			if (State == PromiseState.Pending)
			{
				this.onComplete += onComplete;
			}
			else
			{
				onComplete.Invoke();
			}
			return this;
		}

		public Promise Done(Action onResolved)
		{
			switch (State)
			{
				case PromiseState.Pending:
					doneHandlersInternal.AddLast(new DelegateVoid(onResolved));
					break;
				case PromiseState.Resolved:
					onResolved.Invoke();
					break;
			}
			return this;
		}

		public Promise Fail(Action onRejected)
		{
			switch (State)
			{
				case PromiseState.Pending:
					failHandlersInternal.AddLast(new DelegateVoid(onRejected));
					break;
				case PromiseState.Rejected:
				case PromiseState.Errored:
					DeferredInternal.TryInvokeDirectInternal(onRejected, DeferredState.Rejecting | DeferredState.Erroring);
					break;
			}

			return this;
		}

		public Promise Fail<TFail>(Action<TFail> onRejected)
		{
			switch (State)
			{
				case PromiseState.Pending:
					failHandlersInternal.AddLast(new DelegateArg<TFail>(onRejected));
					break;
				case PromiseState.Rejected:
				case PromiseState.Errored:
					DeferredInternal.TryInvokeDirectInternal(onRejected, DeferredState.Rejecting | DeferredState.Erroring);
					break;
			}

			return this;
		}

		public Promise Catch<TException>(Action<TException> onException) where TException : Exception
		{
			switch (State)
			{
				case PromiseState.Pending:
					exceptionHandlersInternal.AddLast(new DelegateArg<TException>(onException));
					break;
				case PromiseState.Errored:
					DeferredInternal.TryInvokeDirectInternal(onException, DeferredState.Erroring);
					break;
			}

			return this;
		}

		public CustomYieldInstruction Finally()
		{
			return DeferredInternal.FinallyInternal();
		}

		public CustomYieldInstruction Finally(Action onFinally)
		{
			return DeferredInternal.FinallyInternal(onFinally);
		}

		public Promise Then(Action onResolved)
		{
			PromiseVoidFromVoid promise = new PromiseVoidFromVoid(DeferredInternal)
			{
				callback = onResolved
			};
			NextInternal = promise;
			if (State != PromiseState.Pending)
			{
				DeferredInternal.ContinueResolvedInternal(this);
			}
			return promise;
		}

		public Promise<T> Then<T>(Func<T> onResolved)
		{
			PromiseArgFromResult<T> promise = new PromiseArgFromResult<T>(DeferredInternal)
			{
				callback = onResolved
			};
			NextInternal = promise;
			if (State != PromiseState.Pending)
			{
				DeferredInternal.ContinueResolvedInternal(this);
			}
			return promise;
		}

		public Promise Then(Func<Promise> onResolved)
		{
			PromiseVoidFromResultPromise promise = new PromiseVoidFromResultPromise(DeferredInternal)
			{
				callback = onResolved
			};
			NextInternal = promise;
			if (State != PromiseState.Pending)
			{
				DeferredInternal.ContinueResolvedInternal(this);
			}
			return promise;
		}

		public Promise<T> Then<T>(Func<Promise<T>> onResolved)
		{
			PromiseArgFromResultPromise<T> promise = new PromiseArgFromResultPromise<T>(DeferredInternal)
			{
				callback = onResolved
			};
			NextInternal = promise;
			if (State != PromiseState.Pending)
			{
				DeferredInternal.ContinueResolvedInternal(this);
			}
			return promise;
		}

		//[System.Diagnostics.Conditional("DEBUG")]
		//void Check()
		//{

		//}
	}

	public class Promise<T> : Promise, IValueContainer<T>
	{
		internal Promise(ADeferred deferred) : base(deferred) { }

		public T Value { get; protected set; }

		// Only used for the first link.
		internal void SetValueInternal(T value)
		{
			Value = value;
		}

		internal override bool TryInvokeInternal<U>(ITryInvokable tryInvoke, U value, out bool invoked) // private protected not supported before c# 7.2, so must use internal.
		{
			bool success = base.TryInvokeInternal(tryInvoke, value, out invoked);
			if (tryInvoke is IValueContainer<T>)
			{
				Value = ((IValueContainer<T>) tryInvoke).Value;
			}
			return success;
		}

		internal override bool TryInvokeInternal(ITryInvokable tryInvoke) // private protected not supported before c# 7.2, so must use internal.
		{
			bool success = base.TryInvokeInternal(tryInvoke);
			if (tryInvoke is IValueContainer<T>)
			{
				Value = ((IValueContainer<T>) tryInvoke).Value;
			}
			return success;
		}

		public new Promise<T> Notification<TNotify>(Action<TNotify> onNotification)
		{
			DeferredInternal.NotificationInternal(onNotification);
			return this;
		}

		public new Promise<T> Complete(Action onComplete)
		{
			base.Complete(onComplete);
			return this;
		}

		public new Promise<T> Done(Action onResolved)
		{
			base.Done(onResolved);
			return this;
		}

		public Promise<T> Done(Action<T> onResolved)
		{
			switch (State)
			{
				case PromiseState.Pending:
					doneHandlersInternal.AddLast(new DelegateArg<T>(onResolved));
					break;
				case PromiseState.Resolved:
					onResolved.Invoke(Value);
					break;
			}
			return this;
		}

		public Promise<T> Fail(Func<T> onRejected)
		{
			switch (State)
			{
				case PromiseState.Pending:
					failHandlersInternal.AddLast(new DelegateVoidResult<T>(onRejected));
					break;
				case PromiseState.Rejected:
				case PromiseState.Errored:
					Value = DeferredInternal.TryInvokeDirectInternal(onRejected, DeferredState.Rejecting | DeferredState.Erroring);
					break;
			}

			return this;
		}

		public Promise<T> Fail<TFail>(Func<TFail, T> onRejected)
		{
			switch (State)
			{
				case PromiseState.Pending:
					failHandlersInternal.AddLast(new DelegateArgResult<TFail, T>(onRejected));
					break;
				case PromiseState.Rejected:
				case PromiseState.Errored:
					Value = DeferredInternal.TryInvokeDirectInternal(onRejected, DeferredState.Rejecting | DeferredState.Erroring);
					break;
			}

			return this;
		}

		public Promise<T> Catch<TException>(Func<TException, T> onException) where TException : Exception
		{
			switch (State)
			{
				case PromiseState.Pending:
					exceptionHandlersInternal.AddLast(new DelegateArgResult<TException, T>(onException));
					break;
				case PromiseState.Errored:
					Value = DeferredInternal.TryInvokeDirectInternal(onException, DeferredState.Erroring);
					break;
			}

			return this;
		}

		public Promise Then(Action<T> onResolved)
		{
			PromiseVoidFromArg<T> promise = new PromiseVoidFromArg<T>(DeferredInternal)
			{
				callback = onResolved
			};
			NextInternal = promise;
			if (State != PromiseState.Pending)
			{
				DeferredInternal.ContinueResolvedInternal(this);
			}
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, TResult> onResolved)
		{
			PromiseArgFromArgResult<T, TResult> promise = new PromiseArgFromArgResult<T, TResult>(DeferredInternal)
			{
				callback = onResolved
			};
			NextInternal = promise;
			if (State != PromiseState.Pending)
			{
				DeferredInternal.ContinueResolvedInternal(this);
			}
			return promise;
		}

		public Promise Then(Func<T, Promise> onResolved)
		{
			PromiseVoidFromArgResultPromise<T> promise = new PromiseVoidFromArgResultPromise<T>(DeferredInternal)
			{
				callback = onResolved
			};
			NextInternal = promise;
			if (State != PromiseState.Pending)
			{
				DeferredInternal.ContinueResolvedInternal(this);
			}
			return promise;
		}

		public Promise<TResult> Then<TResult>(Func<T, Promise<TResult>> onResolved)
		{
			PromiseArgFromArgResultPromise<TResult, T> promise = new PromiseArgFromArgResultPromise<TResult, T>(DeferredInternal)
			{
				callback = onResolved
			};
			NextInternal = promise;
			if (State != PromiseState.Pending)
			{
				DeferredInternal.ContinueResolvedInternal(this);
			}
			return promise;
		}
	}
}