using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoPromise
{
	internal class UnhandledException : Exception
	{
		public UnhandledException(Exception innerException) : this(innerException, string.Empty) { }
		public UnhandledException(Exception innerException, string stackTrace) : base(string.Empty, innerException)
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

		public override string Message
		{
			get
			{
				return "An exception was encountered that was not handled.";
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

	[Flags]
	internal enum DeferredState
	{
		Pending = 1 << 0,
		Resolving = 1 << 1,
		Rejecting = 1 << 2,
		Erroring = 1 << 3,
		Final = 1 << 4,

		PendingFinal = Pending | Final,
		ResolvingFinal = Resolving | Final,
		RejectingFinal = Rejecting | Final,
		ErroringFinal = Erroring | Final
	}

	public abstract class ADeferred
	{
		private Dictionary<Type, IDelegateArg> notifications;
		private FinalYield _finally;
		protected Promise next;

		private ValueContainer unhandledValue;

		internal DeferredState StateInternal { get; private set; }

		internal ADeferred()
		{
			StateInternal = DeferredState.Pending;
		}

		internal void SetStatePreserveFinalInternal(DeferredState newState) // private protected not supported before c# 7.2, so must use internal.
		{
			Debug.LogWarning("Set state to: " + newState);
			StateInternal = newState | (StateInternal & DeferredState.Final); // Change state, preserving final.
		}

		internal void ResolveUnhandledInternal()
		{
			SetStatePreserveFinalInternal(DeferredState.Resolving);
			unhandledValue = null;
		}

		internal void TryInvokeDirectInternal(Action callback, DeferredState expectedState)
		{
			if ((StateInternal & expectedState) == 0)
			{
				return;
			}

			ResolveUnhandledInternal(); // You never know what someone might do in a callback, so make sure deferred is in a clean state before invoking.
			callback.Invoke();
		}

		internal void TryInvokeDirectInternal<TArg>(Action<TArg> callback, DeferredState expectedState)
		{
			if ((StateInternal & expectedState) == 0)
			{
				return;
			}

			unhandledValue.TryInvoke(callback, this);
		}

		internal TResult TryInvokeDirectInternal<TResult>(Func<TResult> callback, DeferredState expectedState)
		{
			if ((StateInternal & expectedState) == 0)
			{
				return default(TResult);
			}

			ResolveUnhandledInternal(); // You never know what someone might do in a callback, so make sure deferred is in a clean state before invoking.
			return callback.Invoke();
		}

		internal TResult TryInvokeDirectInternal<TArg, TResult>(Func<TArg, TResult> callback, DeferredState expectedState)
		{
			if ((StateInternal & expectedState) == 0)
			{
				return default(TResult);
			}

			return unhandledValue.TryInvoke(callback, this);
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
			other.Throw(((ValueContainer<Exception>) temp).Value);
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
			if ((StateInternal & DeferredState.Final) == 0)
			{
				final.AddCallback(callback);
			}
			else
			{
				callback.Invoke();
			}
			return final;
		}

		internal void End()
		{
			StateInternal |= DeferredState.Final; // Mark that no more promises will be added to the chain.
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
			if ((StateInternal & DeferredState.Pending) == 0)
			{
				Debug.LogWarning("Deferred.Throw - Deferred is not in the pending state.");
				return;
			}

			Exception ex = exception;
			if (string.IsNullOrEmpty(exception.StackTrace))
			{
				// Format stacktrace to match "throw exception" so that double-clicking log in console will go to the proper line.
				System.Text.StringBuilder sb = new System.Text.StringBuilder(new System.Diagnostics.StackTrace(1, true).ToString())
					.Remove(0, 1)
					.Replace(":line ", ":")
					.Replace("\n ", " \n")
					.Replace("(", " (")
					.Replace(") in", ") [0x00000] in") // Not sure what "[0x00000]" is, but it's necessary for Unity's parsing.
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
			if ((StateInternal & DeferredState.Pending) == 0)
			{
				Debug.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
				return;
			}

			if (TryHandleRejectionInternal(reason))
			{
				ContinueThenChain();
			}
		}

		public void Reject()
		{
			if ((StateInternal & DeferredState.Pending) == 0)
			{
				Debug.LogWarning("Deferred.Reject - Deferred is not in the pending state.");
				return;
			}

			if (TryHandleRejectionInternal())
			{
				ContinueThenChain();
			}
		}

		internal void ContinueResolvedInternal(Promise promise)
		{
			next = promise;
			ContinueThenChain();
		}

		protected void ContinueThenChain()
		{
			SetStatePreserveFinalInternal(DeferredState.Resolving);

			Promise current = next;
			next = next.NextInternal;
			while (next != null)
			{
				try
				{
					next.InvokeInternal(current);

					Promise promise;
					if (next.TryGetPromiseResultInternal(out promise))
					{
						if (!TryHandleOther(promise))
						{
							Debug.LogError(promise.id + " Failed to tryhandleother");
							return;
						}

						SetStatePreserveFinalInternal(DeferredState.Resolving);

						current = next;
						next = next.NextInternal;

						continue;
					}

					next.ResolveInternal();

					current = next;
					next = next.NextInternal;

					current.CompleteInternal();
				}
				catch (Exception e)
				{
					if (!TryHandleException(e))
					{
						return;
					}
					SetStatePreserveFinalInternal(DeferredState.Resolving);
					//typeof(ADeferred).GetMethod("HandleException").MakeGenericMethod(e.GetType()).Invoke(this, new object[] { e });
				}
			}

			//OnFinished();
		}

		private bool TryHandleOther(Promise promise)
		{
			Debug.LogError(promise.id + " other state: " + promise.State);
			if (promise.State == PromiseState.Pending)
			{
				promise.Complete(() =>
				{
					Debug.LogError(promise.id + " other deferred state: " + promise.DeferredInternal.StateInternal);
					if (TryHandleOther(promise))
					{
						ContinueThenChain();
					}
					Debug.LogError(promise.id + " Failed to tryhandleother");
				});
				return false;
			}

			ADeferred other = promise.DeferredInternal;

			Debug.LogError(promise.id + " other deferred state: " + other.StateInternal);
			switch (other.StateInternal)
			{
				case DeferredState.Rejecting:
				case DeferredState.RejectingFinal:
					{
						Debug.LogWarning("Reject");
						ValueContainer temp = other.unhandledValue;
						Debug.LogWarning("temp type: " + (temp == null ? "null" : temp.GetType().ToString()));
						unhandledValue = temp;
						other.ResolveUnhandledInternal();
						return temp.TryHandleRejection(this);
					}
				case DeferredState.Erroring:
				case DeferredState.ErroringFinal:
					{
						Debug.LogWarning("error");
						ValueContainer<Exception> temp = (ValueContainer<Exception>) other.unhandledValue;
						Debug.LogWarning("temp type: " + temp.GetType());
						unhandledValue = temp;
						other.ResolveUnhandledInternal();
						return TryHandleException(temp.Value);
					}
				case DeferredState.Resolving:
				case DeferredState.ResolvingFinal:
				case DeferredState.Final:
					{
						Debug.LogWarning("Resolve");
						try
						{
							next.ResolveInternal();
							return true;
						}
						catch (Exception e)
						{
							return TryHandleException(e);
						}
					}
			}
			return false;
		}

		internal bool TryHandleException(Exception ex)
		{
			ResolveUnhandledInternal();
			ValueContainer<Exception> cached;
			unhandledValue = cached = new ValueContainer<Exception>(ex);
			SetStatePreserveFinalInternal(DeferredState.Erroring);

			Exception exception = ex is UnhandledException ? ex.InnerException : ex;
			bool handled = false;

			while (next != null)
			{
				Promise current = next;
				try
				{
					if (current.TryHandleExceptionInternal(exception, out handled))
					{
						ResolveUnhandledInternal();
						current.CompleteInternal();
						return true;
					}
					next = next.NextInternal;
					current.CompleteInternal();
				}
				catch (Exception e)
				{
					if (!handled)
					{
						Debug.LogErrorFormat("A new exception was encountered in a Promise.Complete callback before an old exception was handled. The new exception will replace the old exception propagating up the promise chain.\nOld exception:\n{0}", ex);
					}
					cached.Value = ex = exception = e;
				}
			}

			unhandledValue = cached;

			// TODO: Subscribe to global error thrower for next frame.
			GlobalMonoBehaviour.Yield(() =>
			{
				if ((StateInternal & DeferredState.Erroring) == 0)
					return;
				if (ex is UnhandledException)
					throw ex;
				throw new UnhandledException(ex);
			});

			return false;
		}

		internal bool TryHandleRejectionInternal()
		{
			ResolveUnhandledInternal();
			unhandledValue = new ValueContainer();
			SetStatePreserveFinalInternal(DeferredState.Rejecting);

			while (true)
			{
				Promise current = next;
				try
				{
					if (current.TryHandleFailInternal())
					{
						ResolveUnhandledInternal();
						current.CompleteInternal();
						return true;
					}
					next = next.NextInternal;
					current.CompleteInternal();
					if (next == null)
					{
						return false;
					}
				}
				catch (Exception e)
				{
					return TryHandleException(e);
				}
			}
		}

		internal bool TryHandleRejectionInternal<TFail>(TFail rejectionValue)
		{
			ResolveUnhandledInternal();
			unhandledValue = new ValueContainer<TFail>(rejectionValue);
			SetStatePreserveFinalInternal(DeferredState.Rejecting);

			while (true)
			{
				Promise current = next;
				try
				{
					if (current.TryHandleFailInternal(rejectionValue))
					{
						ResolveUnhandledInternal();
						current.CompleteInternal();
						return true;
					}
					next = next.NextInternal;
					current.CompleteInternal();
					if (next == null)
					{
						return false;
					}
				}
				catch (Exception e)
				{
					return TryHandleException(e);
				}
			}
		}
	}

	public sealed class Deferred : ADeferred
	{
		public readonly Promise Promise;

		internal Deferred()
		{
			next = Promise = new Promise(this);
		}

		public void Resolve()
		{
			if ((StateInternal & DeferredState.Pending) == 0)
			{
				Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			SetStatePreserveFinalInternal(DeferredState.Resolving);

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

	public sealed class Deferred<T> : ADeferred
	{
		public readonly Promise<T> Promise;

		internal Deferred()
		{
			next = Promise = new Promise<T>(this);
		}

		public void Resolve(T arg)
		{
			if ((StateInternal & DeferredState.Pending) == 0)
			{
				Debug.LogWarning("Deferred.Resolve - Deferred is not in the pending state.");
				return;
			}

			SetStatePreserveFinalInternal(DeferredState.Resolving);

			Promise.SetValueInternal(arg);
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