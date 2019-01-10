using System;

namespace ProtoPromise
{
	public partial class Promise
	{
		// This allows infinite .Then callbacks, since it avoids recursion.
		internal static void ContinueHandlingInternal(Promise current)
		{
			LinkedQueueStruct<Promise> nextHandles = new LinkedQueueStruct<Promise>(current);
			for (; current != null; current = current.NextInternal)
			{
				LinkedQueueClass<Promise> branches = current.NextBranches;
				for (Promise next = branches.Peek(); next != null; next = next.NextInternal)
				{
					Promise waitPromise = next.HandleInternal(current);
					if (waitPromise == null || waitPromise.State != PromiseState.Pending)
					{
						nextHandles.EnqueueRisky(next);
					}
					else
					{
						Promise cachedPromise = next;
						waitPromise.Complete(() => ContinueHandlingInternal(cachedPromise));
					}
				}
				branches.Clear();
			}
		}
	}

	internal interface ITryInvokable : ILinked<ITryInvokable>
	{
		bool TryInvoke<U>(U arg, out bool invoked);
		bool TryInvoke(out bool invoked);
	}
	internal interface IDelegate : ILinked<IDelegate>
	{
		void Invoke(IValueContainer feed);
	}
	internal interface IDelegateVoid : IDelegate
	{
		void Invoke();
	}
	internal interface IDelegateArg : IDelegate { }
	internal interface IDelegateArg<TArg> : IDelegateArg
	{
		void Invoke(TArg arg);
	}
	internal interface IDelegateArgResult<TArg, TResult> : IDelegateArg<TArg>, IValueContainer<TResult> { }

	internal interface IDelegateVoidResult<TResult> : IDelegateVoid, IValueContainer<TResult> { }

	internal interface IValueContainer { }

	internal interface IValueContainer<T> : IValueContainer
	{
		T Value { get; }
	}

	internal class ValueContainer : IValueContainer
	{
	}

	internal sealed class ValueContainer<T> : ValueContainer, IValueContainer<T>
	{
		public ValueContainer(T value)
		{
			Value = value;
		}

		public T Value { get; set; }
	}

	internal sealed class DelegateVoid : IDelegateVoid, ITryInvokable, ILinked<DelegateVoid>
	{
		public DelegateVoid Next { get; set; }

		IDelegate ILinked<IDelegate>.Next
		{
			get
			{
				return Next;
			}
			set
			{
				Next = (DelegateVoid) value;
			}
		}

		ITryInvokable ILinked<ITryInvokable>.Next
		{
			get
			{
				return Next;
			}
			set
			{
				Next = (DelegateVoid) value;
			}
		}

		Action callback;

		public DelegateVoid(Action action)
		{
			SetCallback(action);
		}

		public void SetCallback(Action action)
		{
			callback = action;
		}

		public void Invoke(IValueContainer feed)
		{
			Invoke();
		}

		public void Invoke()
		{
			callback.Invoke();
		}

		public bool TryInvoke<U>(U arg, out bool invoked)
		{
			return TryInvoke(out invoked);
		}

		public bool TryInvoke(out bool invoked)
		{
			invoked = true;
			Invoke();
			return true;
		}
	}

	internal sealed class DelegateArg<TArg> : IDelegateArg<TArg>, ITryInvokable, ILinked<DelegateArg<TArg>>
	{
		public DelegateArg<TArg> Next { get; set; }

		IDelegate ILinked<IDelegate>.Next
		{
			get
			{
				return Next;
			}
			set
			{
				Next = (DelegateArg<TArg>) value;
			}
		}

		ITryInvokable ILinked<ITryInvokable>.Next
		{
			get
			{
				return Next;
			}
			set
			{
				Next = (DelegateArg<TArg>) value;
			}
		}

		Action<TArg> callback;

		public DelegateArg(Action<TArg> action)
		{
			SetCallback(action);
		}

		public void SetCallback(Action<TArg> action)
		{
			callback = action;
		}

		public void AddCallback(Action<TArg> action)
		{
			callback += action;
		}

		public void Invoke(IValueContainer feed)
		{
			Invoke(((IValueContainer<TArg>) feed).Value);
		}

		public void Invoke(TArg arg)
		{
			callback.Invoke(arg);
		}

		public bool TryInvoke<U>(U arg, out bool invoked)
		{
			if (typeof(TArg).IsValueType)
			{
				// This avoids boxing value types.
				if (this is DelegateArg<U>)
				{
					invoked = true;
					(this as DelegateArg<U>).Invoke(arg);
					return true;
				}
			}
			else
			{
				object val = arg;
				if (typeof(TArg).IsAssignableFrom(typeof(U)) || (val != null && arg is TArg))
				{
					invoked = true;
					Invoke((TArg) val);
					return true;
				}
			}
			return invoked = false;
		}

		public bool TryInvoke(out bool invoked)
		{
			return invoked = false;
		}
	}

	internal class DelegateVoidResult<TResult> : IDelegateVoidResult<TResult>, ITryInvokable, ILinked<DelegateVoidResult<TResult>>
	{
		public DelegateVoidResult<TResult> Next { get; set; }

		IDelegate ILinked<IDelegate>.Next
		{
			get
			{
				return Next;
			}
			set
			{
				Next = (DelegateVoidResult<TResult>) value;
			}
		}

		ITryInvokable ILinked<ITryInvokable>.Next
		{
			get
			{
				return Next;
			}
			set
			{
				Next = (DelegateVoidResult<TResult>) value;
			}
		}

		Func<TResult> callback;

		public TResult Value { get; private set; }

		public DelegateVoidResult(Func<TResult> func)
		{
			SetCallback(func);
		}

		public void SetCallback(Func<TResult> func)
		{
			callback = func;
		}

		public virtual void Invoke(IValueContainer feed)
		{
			Invoke();
		}

		public void Invoke()
		{
			Value = callback.Invoke();
		}

		public bool TryInvoke<U>(U arg, out bool invoked)
		{
			return TryInvoke(out invoked);
		}

		public bool TryInvoke(out bool invoked)
		{
			invoked = true;
			Invoke();
			return true;
		}
	}

	internal sealed class DelegateArgResult<TArg, TResult> : IDelegateArgResult<TArg, TResult>, ITryInvokable, ILinked<DelegateArgResult<TArg, TResult>>
	{
		public DelegateArgResult<TArg, TResult> Next { get; set; }

		IDelegate ILinked<IDelegate>.Next
		{
			get
			{
				return Next;
			}
			set
			{
				Next = (DelegateArgResult<TArg, TResult>) value;
			}
		}

		ITryInvokable ILinked<ITryInvokable>.Next
		{
			get
			{
				return Next;
			}
			set
			{
				Next = (DelegateArgResult<TArg, TResult>) value;
			}
		}

		Func<TArg, TResult> callback;

		public TResult Value { get; private set; }

		public DelegateArgResult(Func<TArg, TResult> func)
		{
			SetCallback(func);
		}

		public void SetCallback(Func<TArg, TResult> func)
		{
			callback = func;
		}

		public void Invoke(IValueContainer feed)
		{
			Invoke(((IValueContainer<TArg>) feed).Value);
		}

		public void Invoke(TArg arg)
		{
			Value = callback.Invoke(arg);
		}

		public bool TryInvoke<U>(U arg, out bool invoked)
		{
			if (typeof(TArg).IsValueType)
			{
				// This avoids boxing value types.
				if (this is DelegateArg<U>)
				{
					invoked = true;
					(this as DelegateArg<U>).Invoke(arg);
					return true;
				}
			}
			else
			{
				object val = arg;
				if (typeof(TArg).IsAssignableFrom(typeof(U)) || (val != null && arg is TArg))
				{
					invoked = true;
					Invoke((TArg) val);
					return true;
				}
			}
			return invoked = false;
		}

		public bool TryInvoke(out bool invoked)
		{
			return invoked = false;
		}
	}



	internal class PromiseVoidReject : Promise, ILinked<PromiseVoidReject>
	{
		PromiseVoidReject ILinked<PromiseVoidReject>.Next { get { return (PromiseVoidReject) NextInternal; } set { NextInternal = value; } }

		internal Action rejectionHandler;

		internal PromiseVoidReject(ADeferred deferred) : base(deferred) { }

		internal override sealed Promise RejectProtected(Exception exception)
		{
			rejectionHandler.Invoke();
			return null;
		}
	}

	internal class PromiseVoidReject<TException> : Promise, ILinked<PromiseVoidReject<TException>> where TException : Exception
	{
		PromiseVoidReject<TException> ILinked<PromiseVoidReject<TException>>.Next { get { return (PromiseVoidReject<TException>) NextInternal; } set { NextInternal = value; } }

		internal Action<TException> rejectionHandler;

		internal PromiseVoidReject(ADeferred deferred) : base(deferred) { }

		internal override sealed Promise RejectProtected(Exception exception)
		{
			if (exception != null && exception is TException)
			{
				rejectionHandler.Invoke((TException) exception);
			}
			else
			{
				_exception = exception;
			}
			return null;
		}
	}

	internal class PromiseArgReject<TArg> : Promise<TArg>, ILinked<PromiseArgReject<TArg>>
	{
		PromiseArgReject<TArg> ILinked<PromiseArgReject<TArg>>.Next { get { return (PromiseArgReject<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg> rejectionHandler;

		internal PromiseArgReject(ADeferred deferred) : base(deferred) { }

		internal override sealed Promise RejectProtected(Exception exception)
		{
			Value = rejectionHandler.Invoke();
			return null;
		}
	}

	internal class PromiseVoidRejectPromise : Promise, ILinked<PromiseVoidRejectPromise>
	{
		PromiseVoidRejectPromise ILinked<PromiseVoidRejectPromise>.Next { get { return (PromiseVoidRejectPromise) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> rejectionHandler;

		internal PromiseVoidRejectPromise(ADeferred deferred) : base(deferred) { }

		internal override sealed Promise RejectProtected(Exception exception)
		{
			PromiseHelper(rejectionHandler.Invoke());
			return null;
		}
	}

	internal class PromiseArgReject<TException, TArg> : Promise<TArg>, ILinked<PromiseArgReject<TException, TArg>> where TException : Exception
	{
		PromiseArgReject<TException, TArg> ILinked<PromiseArgReject<TException, TArg>>.Next { get { return (PromiseArgReject<TException, TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TException, TArg> rejectionHandler;

		internal PromiseArgReject(ADeferred deferred) : base(deferred) { }

		internal override sealed Promise RejectProtected(Exception exception)
		{
			if (exception != null && exception is TException)
			{
				Value = rejectionHandler.Invoke((TException) exception);
			}
			else
			{
				_exception = exception;
			}
			return null;
		}
	}

	internal class PromiseArgRejectPromise<TException> : Promise, ILinked<PromiseArgRejectPromise<TException>> where TException : Exception
	{
		PromiseArgRejectPromise<TException> ILinked<PromiseArgRejectPromise<TException>>.Next { get { return (PromiseArgRejectPromise<TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<TException, Promise> rejectionHandler;

		internal PromiseArgRejectPromise(ADeferred deferred) : base(deferred) { }

		internal override sealed Promise RejectProtected(Exception exception)
		{
			Promise promise;
			if (exception != null && exception is TException)
			{
				promise = rejectionHandler.Invoke((TException) exception);
				PromiseHelper(promise);
			}
			else
			{
				_exception = exception;
				promise = null;
			}
			return promise;
		}
	}

	internal class PromiseArgRejectPromiseT<TArg> : Promise<TArg>, ILinked<PromiseArgRejectPromiseT<TArg>>
	{
		PromiseArgRejectPromiseT<TArg> ILinked<PromiseArgRejectPromiseT<TArg>>.Next { get { return (PromiseArgRejectPromiseT<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> rejectionHandler;

		internal PromiseArgRejectPromiseT(ADeferred deferred) : base(deferred) { }

		internal override sealed Promise RejectProtected(Exception exception)
		{
			Promise promise = rejectionHandler.Invoke();
			PromiseHelper(promise);
			return promise;
		}
	}

	internal class PromiseArgRejectPromiseT<TException, TArg> : Promise<TArg>, ILinked<PromiseArgRejectPromiseT<TException, TArg>> where TException : Exception
	{
		PromiseArgRejectPromiseT<TException, TArg> ILinked<PromiseArgRejectPromiseT<TException, TArg>>.Next { get { return (PromiseArgRejectPromiseT<TException, TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TException, Promise<TArg>> rejectionHandler;

		internal PromiseArgRejectPromiseT(ADeferred deferred) : base(deferred) { }

		internal override sealed Promise RejectProtected(Exception exception)
		{
			Promise promise;
			if (exception != null && exception is TException)
			{
				promise = rejectionHandler.Invoke((TException) exception);
				PromiseHelper(promise);
			}
			else
			{
				_exception = exception;
				promise = null;
			}
			return promise;
		}
	}



	internal sealed class PromiseVoidFromVoidResolve : Promise, ILinked<PromiseVoidFromVoidResolve>
	{
		PromiseVoidFromVoidResolve ILinked<PromiseVoidFromVoidResolve>.Next { get { return (PromiseVoidFromVoidResolve) NextInternal; } set { NextInternal = value; } }

		internal Action callback;

		internal PromiseVoidFromVoidResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			callback.Invoke();
			return null;
		}
	}

	internal sealed class PromiseVoidFromVoid : PromiseVoidReject, ILinked<PromiseVoidFromVoid>
	{
		PromiseVoidFromVoid ILinked<PromiseVoidFromVoid>.Next { get { return (PromiseVoidFromVoid) NextInternal; } set { NextInternal = value; } }

		internal Action callback;

		internal PromiseVoidFromVoid(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			callback.Invoke();
			return null;
		}
	}

	internal sealed class PromiseVoidFromVoid<TException> : PromiseVoidReject<TException>, ILinked<PromiseVoidFromVoid<TException>> where TException : Exception
	{
		PromiseVoidFromVoid<TException> ILinked<PromiseVoidFromVoid<TException>>.Next { get { return (PromiseVoidFromVoid<TException>) NextInternal; } set { NextInternal = value; } }

		internal Action callback;

		internal PromiseVoidFromVoid(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			callback.Invoke();
			return null;
		}
	}



	internal sealed class PromiseVoidFromArgResolve<TArg> : Promise, ILinked<PromiseVoidFromArgResolve<TArg>>
	{
		PromiseVoidFromArgResolve<TArg> ILinked<PromiseVoidFromArgResolve<TArg>>.Next { get { return (PromiseVoidFromArgResolve<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Action<TArg> callback;

		internal PromiseVoidFromArgResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			callback.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}

	internal sealed class PromiseVoidFromArg<TArg> : PromiseVoidReject, ILinked<PromiseVoidFromArg<TArg>>
	{
		PromiseVoidFromArg<TArg> ILinked<PromiseVoidFromArg<TArg>>.Next { get { return (PromiseVoidFromArg<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Action<TArg> callback;

		internal PromiseVoidFromArg(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			callback.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}

	internal sealed class PromiseVoidFromArg<TArg, TException> : PromiseVoidReject<TException>, ILinked<PromiseVoidFromArg<TArg, TException>> where TException : Exception
	{
		PromiseVoidFromArg<TArg, TException> ILinked<PromiseVoidFromArg<TArg, TException>>.Next { get { return (PromiseVoidFromArg<TArg, TException>) NextInternal; } set { NextInternal = value; } }

		internal Action<TArg> callback;

		internal PromiseVoidFromArg(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			callback.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}



	internal sealed class PromiseArgFromResultResolve<TResult> : Promise<TResult>, ILinked<PromiseArgFromResultResolve<TResult>>
	{
		PromiseArgFromResultResolve<TResult> ILinked<PromiseArgFromResultResolve<TResult>>.Next { get { return (PromiseArgFromResultResolve<TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TResult> callback;

		internal PromiseArgFromResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = callback.Invoke();
			return null;
		}
	}

	internal sealed class PromiseArgFromResult<TResult> : PromiseArgReject<TResult>, ILinked<PromiseArgFromResult<TResult>>
	{
		PromiseArgFromResult<TResult> ILinked<PromiseArgFromResult<TResult>>.Next { get { return (PromiseArgFromResult<TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TResult> callback;

		internal PromiseArgFromResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = callback.Invoke();
			return null;
		}
	}

	internal sealed class PromiseArgFromResult<TResult, TException> : PromiseArgReject<TException, TResult>, ILinked<PromiseArgFromResult<TResult, TException>> where TException : Exception
	{
		PromiseArgFromResult<TResult, TException> ILinked<PromiseArgFromResult<TResult, TException>>.Next { get { return (PromiseArgFromResult<TResult, TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<TResult> callback;

		internal PromiseArgFromResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = callback.Invoke();
			return null;
		}
	}



	internal sealed class PromiseArgFromArgResultResolve<TArg, TResult> : Promise<TResult>, ILinked<PromiseArgFromArgResultResolve<TArg, TResult>>
	{
		PromiseArgFromArgResultResolve<TArg, TResult> ILinked<PromiseArgFromArgResultResolve<TArg, TResult>>.Next { get { return (PromiseArgFromArgResultResolve<TArg, TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, TResult> callback;

		internal PromiseArgFromArgResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = callback.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}

	internal sealed class PromiseArgFromArgResult<TArg, TResult> : PromiseArgReject<TResult>, ILinked<PromiseArgFromArgResult<TArg, TResult>>
	{
		PromiseArgFromArgResult<TArg, TResult> ILinked<PromiseArgFromArgResult<TArg, TResult>>.Next { get { return (PromiseArgFromArgResult<TArg, TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, TResult> callback;

		internal PromiseArgFromArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = callback.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}

	internal sealed class PromiseArgFromArgResult<TArg, TResult, TException> : PromiseArgReject<TException, TResult>, ILinked<PromiseArgFromArgResult<TArg, TResult, TException>> where TException : Exception
	{
		PromiseArgFromArgResult<TArg, TResult, TException> ILinked<PromiseArgFromArgResult<TArg, TResult, TException>>.Next { get { return (PromiseArgFromArgResult<TArg, TResult, TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, TResult> callback;

		internal PromiseArgFromArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Value = callback.Invoke(((IValueContainer<TArg>) feed).Value);
			return null;
		}
	}



	internal sealed class PromiseVoidFromPromiseResultResolve : Promise, ILinked<PromiseVoidFromPromiseResultResolve>
	{
		PromiseVoidFromPromiseResultResolve ILinked<PromiseVoidFromPromiseResultResolve>.Next { get { return (PromiseVoidFromPromiseResultResolve) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> callback;

		internal PromiseVoidFromPromiseResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke();
			PromiseHelper(promise);
			return promise;
		}
	}

	internal sealed class PromiseVoidFromPromiseResult : PromiseVoidRejectPromise, ILinked<PromiseVoidFromPromiseResult>
	{
		PromiseVoidFromPromiseResult ILinked<PromiseVoidFromPromiseResult>.Next { get { return (PromiseVoidFromPromiseResult) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> callback;

		internal PromiseVoidFromPromiseResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke();
			PromiseHelper(promise);
			return promise;
		}
	}

	internal sealed class PromiseVoidFromPromiseResult<TException> : PromiseArgRejectPromise<TException>, ILinked<PromiseVoidFromPromiseResult> where TException : Exception
	{
		PromiseVoidFromPromiseResult ILinked<PromiseVoidFromPromiseResult>.Next { get { return (PromiseVoidFromPromiseResult) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> callback;

		internal PromiseVoidFromPromiseResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke();
			PromiseHelper(promise);
			return promise;
		}
	}



	internal sealed class PromiseVoidFromPromiseArgResultResolve<TArg> : Promise, ILinked<PromiseVoidFromPromiseArgResultResolve<TArg>>
	{
		PromiseVoidFromPromiseArgResultResolve<TArg> ILinked<PromiseVoidFromPromiseArgResultResolve<TArg>>.Next { get { return (PromiseVoidFromPromiseArgResultResolve<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, Promise> callback;

		internal PromiseVoidFromPromiseArgResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke(((IValueContainer<TArg>) feed).Value);
			PromiseHelper(promise);
			return promise;
		}
	}

	internal sealed class PromiseVoidFromPromiseArgResult<TArg> : PromiseVoidRejectPromise, ILinked<PromiseVoidFromPromiseArgResult<TArg>>
	{
		PromiseVoidFromPromiseArgResult<TArg> ILinked<PromiseVoidFromPromiseArgResult<TArg>>.Next { get { return (PromiseVoidFromPromiseArgResult<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, Promise> callback;

		internal PromiseVoidFromPromiseArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke(((IValueContainer<TArg>) feed).Value);
			PromiseHelper(promise);
			return promise;
		}
	}

	internal sealed class PromiseVoidFromPromiseArgResult<TArg, TException> : PromiseArgRejectPromise<TException>, ILinked<PromiseVoidFromPromiseArgResult<TArg, TException>> where TException : Exception
	{
		PromiseVoidFromPromiseArgResult<TArg, TException> ILinked<PromiseVoidFromPromiseArgResult<TArg, TException>>.Next { get { return (PromiseVoidFromPromiseArgResult<TArg, TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, Promise> callback;

		internal PromiseVoidFromPromiseArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke(((IValueContainer<TArg>) feed).Value);
			PromiseHelper(promise);
			return promise;
		}
	}



	internal sealed class PromiseArgFromPromiseResultResolve<TArg> : Promise<TArg>, ILinked<PromiseArgFromPromiseResultResolve<TArg>>
	{
		PromiseArgFromPromiseResultResolve<TArg> ILinked<PromiseArgFromPromiseResultResolve<TArg>>.Next { get { return (PromiseArgFromPromiseResultResolve<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> callback;

		internal PromiseArgFromPromiseResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke();
			PromiseHelper(promise);
			return promise;
		}
	}

	internal sealed class PromiseArgFromPromiseResult<TArg> : PromiseArgRejectPromiseT<TArg>, ILinked<PromiseArgFromPromiseResult<TArg>>
	{
		PromiseArgFromPromiseResult<TArg> ILinked<PromiseArgFromPromiseResult<TArg>>.Next { get { return (PromiseArgFromPromiseResult<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> callback;

		internal PromiseArgFromPromiseResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke();
			PromiseHelper(promise);
			return promise;
		}
	}

	internal sealed class PromiseArgFromPromiseResult<TArg, TException> : PromiseArgRejectPromiseT<TException, TArg>, ILinked<PromiseArgFromPromiseResult<TArg, TException>> where TException : Exception
	{
		PromiseArgFromPromiseResult<TArg, TException> ILinked<PromiseArgFromPromiseResult<TArg, TException>>.Next { get { return (PromiseArgFromPromiseResult<TArg, TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> callback;

		internal PromiseArgFromPromiseResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke();
			PromiseHelper(promise);
			return promise;
		}
	}



	internal sealed class PromiseArgFromPromiseArgResultResolve<TArg, PArg> : Promise<TArg>, ILinked<PromiseArgFromPromiseArgResultResolve<TArg, PArg>>
	{
		PromiseArgFromPromiseArgResultResolve<TArg, PArg> ILinked<PromiseArgFromPromiseArgResultResolve<TArg, PArg>>.Next { get { return (PromiseArgFromPromiseArgResultResolve<TArg, PArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<PArg, Promise<TArg>> callback;

		internal PromiseArgFromPromiseArgResultResolve(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke(((IValueContainer<PArg>) feed).Value);
			PromiseHelper(promise);
			return promise;
		}
	}

	internal sealed class PromiseArgFromPromiseArgResult<TArg, PArg> : PromiseArgRejectPromiseT<TArg>, ILinked<PromiseArgFromPromiseArgResult<TArg, PArg>>
	{
		PromiseArgFromPromiseArgResult<TArg, PArg> ILinked<PromiseArgFromPromiseArgResult<TArg, PArg>>.Next { get { return (PromiseArgFromPromiseArgResult<TArg, PArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<PArg, Promise<TArg>> callback;

		internal PromiseArgFromPromiseArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke(((IValueContainer<PArg>) feed).Value);
			PromiseHelper(promise);
			return promise;
		}
	}

	internal sealed class PromiseArgFromPromiseArgResult<TArg, PArg, TException> : PromiseArgRejectPromiseT<TException, TArg>, ILinked<PromiseArgFromPromiseArgResult<TArg, PArg, TException>> where TException : Exception
	{
		PromiseArgFromPromiseArgResult<TArg, PArg, TException> ILinked<PromiseArgFromPromiseArgResult<TArg, PArg, TException>>.Next { get { return (PromiseArgFromPromiseArgResult<TArg, PArg, TException>) NextInternal; } set { NextInternal = value; } }

		internal Func<PArg, Promise<TArg>> callback;

		internal PromiseArgFromPromiseArgResult(ADeferred deferred) : base(deferred) { }

		internal override Promise ResolveProtected(IValueContainer feed)
		{
			Promise promise = callback.Invoke(((IValueContainer<PArg>) feed).Value);
			PromiseHelper(promise);
			return promise;
		}
	}
}