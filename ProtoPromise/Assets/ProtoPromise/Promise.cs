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

	public partial class Promise : CustomYieldInstruction, IDelegate
	{
		protected Action onComplete;
		internal Queue<IDelegate> doneHandlersInternal;
		internal Queue<IDelegate> exceptionHandlersInternal;
		internal Queue<IDelegate> failHandlersInternal;
		internal Promise NextInternal { get; set; }
		internal ADeferred DeferredInternal { get; private set; }
		public PromiseState State { get; protected set; }

		internal Promise(ADeferred deferred)
		{
			DeferredInternal = deferred;
		}

		internal virtual void InvokeInternal(IDelegate feed)
		{
			State = PromiseState.Resolved;
		}

		void IDelegate.Invoke(IDelegate feed)
		{
			InvokeInternal(feed);
		}

		internal void CompleteInternal()
		{
			if (onComplete != null)
			{
				onComplete.Invoke();
			}
		}

		internal virtual void ResolveInternal()
		{
			State = PromiseState.Resolved;

			if (doneHandlersInternal != null)
			{
				while (doneHandlersInternal.Count > 0)
				{
					doneHandlersInternal.Dequeue().Invoke(this);
				}
			}
			CompleteInternal();
		}

		internal virtual bool TryInvokeInternal<U>(IDelegate delegateArg, U value)
		{
			if (delegateArg is IDelegateArg<U>)
			{
				((IDelegateArg<U>) delegateArg).Invoke(value);
				return true;
			}
			return delegateArg.TryInvoke(value);
		}

		internal virtual bool TryInvokeInternal(IDelegate delegateArg)
		{
			if (delegateArg is IDelegateVoid)
			{
				((IDelegateVoid)delegateArg).Invoke();
				return true;
			}
			return false;
		}

		internal bool TryHandleExceptionInternal(Exception exception)
		{
			State = PromiseState.Errored;

			return TryHandleException(exception) || TryHandleFail(exception);
		}

		private bool TryHandleException(Exception exception)
		{
			if (exceptionHandlersInternal != null)
			{
				while (exceptionHandlersInternal.Count > 0)
				{
					if (TryInvokeInternal(exceptionHandlersInternal.Dequeue(), exception))
					{
						return true;
					}
				}
			}

			return false;
		}

		internal bool TryHandleFailInternal<TFail>(TFail failValue)
		{
			State = PromiseState.Rejected;

			return TryHandleFail(failValue) || (typeof(Exception).IsAssignableFrom(typeof(TFail)) && TryHandleException(failValue as Exception));
		}

		private bool TryHandleFail<TFail>(TFail failValue)
		{
			if (failHandlersInternal != null)
			{
				while (failHandlersInternal.Count > 0)
				{
					if (TryInvokeInternal(failHandlersInternal.Dequeue(), failValue))
					{
						return true;
					}
				}
			}

			return false;
		}

		internal bool TryHandleFailInternal()
		{
			if (failHandlersInternal != null)
			{
				while (failHandlersInternal.Count > 0)
				{
					if (TryInvokeInternal(failHandlersInternal.Dequeue()))
					{
						return true;
					}
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
			this.onComplete += onComplete;
			if (State != PromiseState.Pending)
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
					if (doneHandlersInternal == null)
					{
						doneHandlersInternal = new Queue<IDelegate>(1);
					}
					doneHandlersInternal.Enqueue(new DelegateVoid(onResolved));
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
					if (failHandlersInternal == null)
					{
						failHandlersInternal = new Queue<IDelegate>(1);
					}
					failHandlersInternal.Enqueue(new DelegateVoid(onRejected));
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
					if (failHandlersInternal == null)
					{
						failHandlersInternal = new Queue<IDelegate>(1);
					}
					failHandlersInternal.Enqueue(new DelegateArg<TFail>(onRejected));
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
					if (exceptionHandlersInternal == null)
					{
						exceptionHandlersInternal = new Queue<IDelegate>(1);
					}
					exceptionHandlersInternal.Enqueue(new DelegateArg<TException>(onException));
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
			if (State == PromiseState.Resolved)
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
			if (State == PromiseState.Resolved)
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
			if (State == PromiseState.Resolved)
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
			if (State == PromiseState.Resolved)
			{
				DeferredInternal.ContinueResolvedInternal(this);
			}
			return promise;
		}

		bool IDelegate.TryInvoke<U>(U arg)
		{
			throw new NotImplementedException();
		}

		//[System.Diagnostics.Conditional("DEBUG")]
		//void Check()
		//{

		//}
	}

	public class Promise<T> : Promise, IValueContainer<T>
	{
		protected T _value;

		internal Promise(ADeferred deferred) : base(deferred) { }

		public T Value
		{
			get
			{
				return _value;
			}
		}

		// Only used for the first link.
		internal void SetValueInternal(T value)
		{
			_value = value;
		}

		internal override bool TryInvokeInternal<U>(IDelegate delegateArg, U value)
		{
			bool success = base.TryInvokeInternal(delegateArg, value);
			if (delegateArg is IValueContainer<T>)
			{
				_value = ((IValueContainer<T>) delegateArg).Value;
			}
			return success;
		}

		internal override bool TryInvokeInternal(IDelegate delegateArg)
		{
			bool success = base.TryInvokeInternal(delegateArg);
			if (delegateArg is IValueContainer<T>)
			{
				_value = ((IValueContainer<T>) delegateArg).Value;
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
					if (doneHandlersInternal == null)
					{
						doneHandlersInternal = new Queue<IDelegate>(1);
					}
					doneHandlersInternal.Enqueue(new DelegateArg<T>(onResolved));
					break;
				case PromiseState.Resolved:
					onResolved.Invoke(_value);
					break;
			}
			return this;
		}

		public Promise<T> Fail(Func<T> onRejected)
		{
			switch (State)
			{
				case PromiseState.Pending:
					if (failHandlersInternal == null)
					{
						failHandlersInternal = new Queue<IDelegate>(1);
					}
					failHandlersInternal.Enqueue(new DelegateVoidResult<T>(onRejected));
					break;
				case PromiseState.Rejected:
				case PromiseState.Errored:
					_value = DeferredInternal.TryInvokeDirectInternal(onRejected, DeferredState.Rejecting | DeferredState.Erroring);
					break;
			}

			return this;
		}

		public Promise<T> Fail<TFail>(Func<TFail, T> onRejected)
		{
			switch (State)
			{
				case PromiseState.Pending:
					if (failHandlersInternal == null)
					{
						failHandlersInternal = new Queue<IDelegate>(1);
					}
					failHandlersInternal.Enqueue(new DelegateArgResult<TFail, T>(onRejected));
					break;
				case PromiseState.Rejected:
				case PromiseState.Errored:
					_value = DeferredInternal.TryInvokeDirectInternal(onRejected, DeferredState.Rejecting | DeferredState.Erroring);
					break;
			}

			return this;
		}

		public Promise<T> Catch<TException>(Func<TException, T> onException) where TException : Exception
		{
			switch (State)
			{
				case PromiseState.Pending:
					if (exceptionHandlersInternal == null)
					{
						exceptionHandlersInternal = new Queue<IDelegate>(1);
					}
					exceptionHandlersInternal.Enqueue(new DelegateArgResult<TException, T>(onException));
					break;
				case PromiseState.Errored:
					_value = DeferredInternal.TryInvokeDirectInternal(onException, DeferredState.Erroring);
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
			if (State == PromiseState.Resolved)
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
			if (State == PromiseState.Resolved)
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
			if (State == PromiseState.Resolved)
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
			if (State == PromiseState.Resolved)
			{
				DeferredInternal.ContinueResolvedInternal(this);
			}
			return promise;
		}
	}
}