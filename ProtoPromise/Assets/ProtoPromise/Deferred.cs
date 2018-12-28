using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoPromise
{
	public class UnhandledException : Exception
	{
		public UnhandledException(Exception innerException) : this(innerException, null) { }
		public UnhandledException(Exception innerException, string stackTrace) : base(string.Empty, innerException)
		{
			_stackTrace = stackTrace;
		}

		readonly string _stackTrace;
		public override string StackTrace
		{
			get
			{
				return _stackTrace == null ? base.StackTrace : _stackTrace;
			}
			
		}

		public override string Message
		{
			get
			{
				return "An exception was encountered that was not handled.";
			}
		}
	}

	internal sealed class FinalYield : CustomYieldInstruction
	{
		bool wait = true;
		Action callback;
		
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

	internal enum DeferredState
	{
		Pending,
		Resolving,
		Rejecting,
		Erroring,
		Final
	}

	public abstract class ADeferred
	{
		private Dictionary<Type, IDelegateArg> notifications;
		private FinalYield _finally;
		protected Promise next;

		private ValueContainer unhandledValue;

		internal void TryInvokeDirectInternal(Action callback, DeferredState expectedState)
		{
			if (StateInternal != expectedState)
			{
				return;
			}

			ResolveUnhandledInternal();
			callback.Invoke();
		}

		internal void TryInvokeDirectInternal<TArg>(Action<TArg> callback, DeferredState expectedState)
		{
			if (StateInternal != expectedState)
			{
				return;
			}

			if (unhandledValue is ValueContainer<TArg>)
			{
				ResolveUnhandledInternal();
				callback.Invoke(((ValueContainer<TArg>)unhandledValue).Value);
			}
			else
			{
				unhandledValue.TryInvoke(callback, this);
			}
		}

		internal TResult TryInvokeDirectInternal<TResult>(Func<TResult> callback, DeferredState expectedState)
		{
			if (StateInternal != expectedState)
			{
				return default(TResult);
			}

			ResolveUnhandledInternal();
			return callback.Invoke();
		}

		internal TResult TryInvokeDirectInternal<TArg, TResult>(Func<TArg, TResult> callback, DeferredState expectedState)
		{
			if (StateInternal != expectedState)
			{
				return default(TResult);
			}

			if (unhandledValue is ValueContainer<TArg>)
			{
				ResolveUnhandledInternal();
				return callback.Invoke(((ValueContainer<TArg>)unhandledValue).Value);
			}
			TResult result;
			unhandledValue.TryInvoke(callback, out result, this);
			return result;
		}

		internal void HandleUnhandledRejectionInternal(ADeferred other)
		{
			ValueContainer temp = unhandledValue;
			other.unhandledValue = temp;
			ResolveUnhandledInternal();
			temp.RejectOther(other);
		}

		internal void HandleUnhandledExceptionInternal(ADeferred other)
		{
			ValueContainer temp = unhandledValue;
			other.unhandledValue = temp;
			ResolveUnhandledInternal();
			other.Throw(((ValueContainer<Exception>)temp).Value);
		}

		internal void ResolveUnhandledInternal()
		{
			StateInternal = DeferredState.Resolving;
			unhandledValue = null;
		}

		internal DeferredState StateInternal { get; private set; }

		protected ADeferred()
		{
			StateInternal = DeferredState.Pending;
		}

		protected void OnFinished()
		{
			if (_finally != null)
			{
				StateInternal = DeferredState.Final;
				_finally.Invoke();
			}
		}

		internal FinalYield FinallyInternal()
		{
			if (_finally == null)
			{
				_finally = new FinalYield();
				if (next == null)
				{
					// chain is complete
					OnFinished();
				}
			}
			return _finally;
		}

		internal FinalYield FinallyInternal(Action callback)
		{
			FinalYield final = FinallyInternal();
			if (StateInternal != DeferredState.Final)
			{
				final.AddCallback(callback);
			}
			else
			{
				callback.Invoke();
			}
			return final;
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

		public void Throw<TException>(TException exception) where TException : Exception
		{
			if (StateInternal != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Throw - Deferred is not in the pending state.");
				return;
			}

			Exception ex = exception;
			if (string.IsNullOrEmpty(exception.StackTrace))
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder(new System.Diagnostics.StackTrace(1, true).ToString())
					.Remove(0, 1)
					.Replace("(", " (")
					.Replace(") in", ") [0x00000] in")
					.Replace("\n ", " \n")
					.Replace("line ", string.Empty)
					.Append(" ");
				ex = new UnhandledException(ex, sb.ToString());
			}

			if (TryHandleException(ex))
			{
				ContinueThenChain();
			}
		}

		public void Reject<TFail>(TFail reason)
		{
			if (StateInternal != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
				return;
			}

			if (!TryHandleRejectionInternal(reason))
			{
				return;
			}
			ContinueThenChain();
		}

		public void Reject()
		{
			if (StateInternal != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
				return;
			}

			if (!TryHandleRejectionInternal())
			{
				return;
			}
			ContinueThenChain();
		}

		internal void ContinueResolvedInternal(Promise promise)
		{
			next = promise;
			ContinueThenChain();
		}

		protected void ContinueThenChain()
		{
			StateInternal = DeferredState.Resolving;

			while (next.NextInternal != null)
			{
				Promise current = next;
				next = next.NextInternal;

				try
				{
					next.InvokeInternal(current);

					Promise promise;
					if (next.TryGetPromiseResultInternal(out promise))
					{
						if (!TryHandleOther(promise))
						{
							return;
						}
						StateInternal = DeferredState.Resolving;
						continue;
					}

					next.ResolveInternal();
				}
				catch (Exception e)
				{
					if (!TryHandleException(e))
					{
						return;
					}
					StateInternal = DeferredState.Resolving;
					//typeof(ADeferred).GetMethod("HandleException").MakeGenericMethod(e.GetType()).Invoke(this, new object[] { e });
				}
			}

			next = null;
			OnFinished();
		}

		private bool TryHandleOther(Promise promise)
		{
			if (promise.State == PromiseState.Pending)
			{
				promise.Complete(() =>
				{
					if (TryHandleOther(promise))
					{
						ContinueThenChain();
					}
				});
				return false;
			}

			ADeferred other = promise.DeferredInternal;

			switch (other.StateInternal)
			{
				case DeferredState.Rejecting:
				{
					ValueContainer temp = other.unhandledValue;
					unhandledValue = temp;
					other.ResolveUnhandledInternal();
					return temp.TryHandleRejection(this);
				}
				case DeferredState.Erroring:
				{
					ValueContainer<Exception> temp = (ValueContainer<Exception>)other.unhandledValue;
					unhandledValue = temp;
					other.ResolveUnhandledInternal();
					return TryHandleException(temp.Value);
				}
				case DeferredState.Resolving:
				case DeferredState.Final:
				{
					try
					{
						next.ResolveInternal();
					}
					catch (Exception e)
					{
						if (!TryHandleException(e))
						{
							return false;
						}
					}
					return true;
				}
			}
			return false;
		}

		internal bool TryHandleException(Exception ex)
		{
			Exception exception = ex;
			if (ex is UnhandledException)
			{
				exception = exception.InnerException;
			}
			
			var current = next;
			while (next != null)
			{
				current = next;
				try
				{
					if (current.TryHandleExceptionInternal(exception))
					{
						unhandledValue = null;
						current.CompleteInternal();
						return true;
					}
					next = next.NextInternal;
				}
				catch (Exception e)
				{
					exception = e;
				}
			}

			if (TryHandleRejectionInternal(exception))
			{
				return true;
			}

			StateInternal = DeferredState.Erroring;
			if (unhandledValue == null)
			{
				unhandledValue = new ValueContainer<Exception>(ex);
			}
			current.CompleteInternal();

			// TODO: Subscribe to global error thrower for next frame.
			GlobalMonoBehaviour.Yield<YieldInstruction>(null, () =>
			{
				if (StateInternal != DeferredState.Erroring)
					return;
				if (ex is UnhandledException)
					throw ex;
				throw new UnhandledException(ex);
			});

			return false;
		}

		internal bool TryHandleRejectionInternal()
		{
			if (next == null)
			{
				return false;
			}

			var current = next;
			while (next != null)
			{
				current = next;
				try
				{
					if (current.TryHandleFailInternal())
					{
						unhandledValue = null;
						current.CompleteInternal();
						return true;
					}
					next = next.NextInternal;
				}
				catch (Exception e)
				{
					return TryHandleException(e);
				}
			}

			StateInternal = DeferredState.Rejecting;
			if (unhandledValue == null)
			{
				unhandledValue = new ValueContainer();
			}
			current.CompleteInternal();
			return false;
		}

		internal bool TryHandleRejectionInternal<TFail>(TFail rejectionValue)
		{
			var current = next;
			while (next != null)
			{
				current = next;
				try
				{
					if (current.TryHandleFailInternal(rejectionValue))
					{
						unhandledValue = null;
						current.CompleteInternal();
						return true;
					}
					next = next.NextInternal;
				}
				catch (Exception e)
				{
					return TryHandleException(e);
				}
			}

			StateInternal = DeferredState.Rejecting;
			if (unhandledValue == null)
			{
				unhandledValue = new ValueContainer<TFail>(rejectionValue);
			}
			current.CompleteInternal();
			return false;
		}
	}

	public sealed class Deferred : ADeferred
	{
		public readonly Promise Promise;

		public Deferred()
		{
			next = Promise = new Promise(this);
		}

		public void Resolve()
		{
			if (StateInternal != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			Promise.InvokeInternal(null);
			try
			{
				Promise.ResolveInternal();
			}
			catch(Exception e)
			{
				if (!TryHandleException(e))
				{
					return;
				}
			}
			ContinueThenChain();
		}
	}

	public sealed class Deferred<T> : ADeferred
	{
		public readonly Promise<T> Promise;

		public Deferred()
		{
			next = Promise = new Promise<T>(this);
		}

		public void Resolve(T arg)
		{
			if (StateInternal != DeferredState.Pending)
			{
				Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			Promise.InvokeInternal(null);
			Promise.Value = arg;
			try
			{
				Promise.ResolveInternal();
			}
			catch (Exception e)
			{
				if (!TryHandleException(e))
				{
					return;
				}
			}
			ContinueThenChain();
		}
	}
}