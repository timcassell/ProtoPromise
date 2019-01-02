using System;

namespace ProtoPromise
{
	internal interface IDelegate : ILinked<IDelegate>
	{
		void Invoke(IDelegate feed);
	}
	internal interface ITryInvokable : ILinked<ITryInvokable>
	{
		bool TryInvoke<U>(U arg, out bool invoked);
		bool TryInvoke();
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
		public virtual void RejectOther(ADeferred other)
		{
			other.Reject();
		}

		public virtual bool TryHandleRejection(ADeferred other)
		{
			return other.TryHandleRejectionInternal();
		}

		public virtual void TryInvoke<TArg>(Action<TArg> callback, ADeferred deferred)
		{
			throw new NotImplementedException();
		}

		public virtual TResult TryInvoke<TArg, TResult>(Func<TArg, TResult> callback, ADeferred deferred)
		{
			throw new NotImplementedException();
		}
	}

	internal sealed class ValueContainer<T> : ValueContainer, IValueContainer<T>
	{
		public ValueContainer(T value)
		{
			Value = value;
		}

		public T Value { get; set; }

		public override void RejectOther(ADeferred other)
		{
			other.Reject(Value);
		}

		public override bool TryHandleRejection(ADeferred other)
		{
			return other.TryHandleRejectionInternal(Value);
		}

		public override void TryInvoke<TArg>(Action<TArg> callback, ADeferred deferred)
		{
			if (this is ValueContainer<TArg>) // This avoids boxing value types.
			{
				deferred.ResolveUnhandledInternal(); // You never know what someone might do in a callback, so make sure deferred is in a clean state before invoking.
				callback.Invoke((this as ValueContainer<TArg>).Value);
			}

			object val = Value;
			if (typeof(TArg).IsAssignableFrom(typeof(T)) || (val != null && Value is TArg))
			{
				deferred.ResolveUnhandledInternal(); // You never know what someone might do in a callback, so make sure deferred is in a clean state before invoking.
				callback.Invoke((TArg) val);
			}
		}

		public override TResult TryInvoke<TArg, TResult>(Func<TArg, TResult> callback, ADeferred deferred)
		{
			if (this is ValueContainer<TArg>) // This avoids boxing value types.
			{
				deferred.ResolveUnhandledInternal(); // You never know what someone might do in a callback, so make sure deferred is in a clean state before invoking.
				return callback.Invoke((this as ValueContainer<TArg>).Value);
			}

			object val = Value;
			if (typeof(TArg).IsAssignableFrom(typeof(T)) || (val != null && Value is TArg))
			{
				deferred.ResolveUnhandledInternal(); // You never know what someone might do in a callback, so make sure deferred is in a clean state before invoking.
				return callback.Invoke((TArg) val);
			}
			return default(TResult);
		}
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

		public void Invoke(IDelegate feed)
		{
			Invoke();
		}

		public void Invoke()
		{
			callback.Invoke();
		}

		public bool TryInvoke<U>(U arg, out bool invoked)
		{
			invoked = true;
			Invoke();
			return true;
		}

		public bool TryInvoke()
		{
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

		public void Invoke(IDelegate feed)
		{
			Invoke(((IValueContainer<TArg>) feed).Value);
		}

		public void Invoke(TArg arg)
		{
			callback.Invoke(arg);
		}

		public bool TryInvoke<U>(U arg, out bool invoked)
		{
			if (this is DelegateArg<U>) // This avoids boxing value types.
			{
				invoked = true;
				(this as DelegateArg<U>).Invoke(arg);
				return true;
			}

			object val = arg;
			if (typeof(TArg).IsAssignableFrom(typeof(U)) || (val != null && arg is TArg))
			{
				invoked = true;
				Invoke((TArg) val);
				return true;
			}
			return invoked = false;
		}

		public bool TryInvoke()
		{
			return false;
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

		public virtual void Invoke(IDelegate feed)
		{
			Invoke();
		}

		public void Invoke()
		{
			Value = callback.Invoke();
		}

		public bool TryInvoke<U>(U arg, out bool invoked)
		{
			invoked = true;
			Invoke();
			return true;
		}

		public bool TryInvoke()
		{
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

		public void Invoke(IDelegate feed)
		{
			Invoke(((IValueContainer<TArg>) feed).Value);
		}

		public void Invoke(TArg arg)
		{
			Value = callback.Invoke(arg);
		}

		public bool TryInvoke<U>(U arg, out bool invoked)
		{
			if (this is DelegateArg<U>) // This avoids boxing value types.
			{
				invoked = true;
				(this as DelegateArg<U>).Invoke(arg);
				return true;
			}

			object val = arg;
			if (typeof(TArg).IsAssignableFrom(typeof(U)) || (val != null && arg is TArg))
			{
				invoked = true;
				Invoke((TArg) val);
				return true;
			}
			return invoked = false;
		}

		public bool TryInvoke()
		{
			return false;
		}
	}


	internal sealed class PromiseVoidFromVoid : Promise, IDelegateVoid, ILinked<PromiseVoidFromVoid>
	{
		public PromiseVoidFromVoid Next { get { return (PromiseVoidFromVoid) NextInternal; } set { NextInternal = value; } }

		internal Action callback;

		internal PromiseVoidFromVoid(ADeferred deferred) : base(deferred) { }

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke();
		}

		public void Invoke()
		{
			callback.Invoke();
		}
	}

	internal sealed class PromiseVoidFromArg<TArg> : Promise, IDelegateArg<TArg>, ILinked<PromiseVoidFromArg<TArg>>
	{
		public PromiseVoidFromArg<TArg> Next { get { return (PromiseVoidFromArg<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Action<TArg> callback;

		internal PromiseVoidFromArg(ADeferred deferred) : base(deferred) { }

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke(((IValueContainer<TArg>) feed).Value);
		}

		public void Invoke(TArg arg)
		{
			callback.Invoke(arg);
		}
	}

	internal sealed class PromiseArgFromResult<TResult> : Promise<TResult>, IDelegateVoidResult<TResult>, ILinked<PromiseArgFromResult<TResult>>
	{
		public PromiseArgFromResult<TResult> Next { get { return (PromiseArgFromResult<TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TResult> callback;

		internal PromiseArgFromResult(ADeferred deferred) : base(deferred) { }

		public void Invoke()
		{
			Value = callback.Invoke();
		}

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke();
		}
	}

	internal sealed class PromiseArgFromArgResult<TArg, TResult> : Promise<TResult>, IDelegateArgResult<TArg, TResult>, ILinked<PromiseArgFromArgResult<TArg, TResult>>
	{
		public PromiseArgFromArgResult<TArg, TResult> Next { get { return (PromiseArgFromArgResult<TArg, TResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, TResult> callback;

		internal PromiseArgFromArgResult(ADeferred deferred) : base(deferred) { }

		public void Invoke(TArg arg)
		{
			Value = callback.Invoke(arg);
		}

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke(((IValueContainer<TArg>) feed).Value);
		}
	}

	internal sealed class PromiseVoidFromResultPromise : Promise, IDelegateVoidResult<Promise>, ILinked<PromiseVoidFromResultPromise>
	{
		public PromiseVoidFromResultPromise Next { get { return (PromiseVoidFromResultPromise) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise> callback;
		internal Promise result;

		internal PromiseVoidFromResultPromise(ADeferred deferred) : base(deferred) { }

		public Promise Value
		{
			get
			{
				return result;
			}
		}

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke();
		}

		public void Invoke()
		{
			result = callback.Invoke();
		}

		internal override bool TryGetPromiseResultInternal(out Promise promise)
		{
			promise = result;
			return true;
		}
	}

	internal sealed class PromiseVoidFromArgResultPromise<TArg> : Promise, IDelegateArgResult<TArg, Promise>, ILinked<PromiseVoidFromArgResultPromise<TArg>>
	{
		public PromiseVoidFromArgResultPromise<TArg> Next { get { return (PromiseVoidFromArgResultPromise<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<TArg, Promise> callback;
		internal Promise result;

		internal PromiseVoidFromArgResultPromise(ADeferred deferred) : base(deferred) { }

		public Promise Value
		{
			get
			{
				return result;
			}
		}

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke(((IValueContainer<TArg>) feed).Value);
		}

		public void Invoke(TArg arg)
		{
			result = callback.Invoke(arg);
		}

		internal override bool TryGetPromiseResultInternal(out Promise promise)
		{
			promise = result;
			return true;
		}
	}

	internal sealed class PromiseArgFromResultPromise<TArg> : Promise<TArg>, IDelegateVoidResult<Promise<TArg>>, ILinked<PromiseArgFromResultPromise<TArg>>
	{
		public PromiseArgFromResultPromise<TArg> Next { get { return (PromiseArgFromResultPromise<TArg>) NextInternal; } set { NextInternal = value; } }

		internal Func<Promise<TArg>> callback;
		internal Promise<TArg> result;

		internal PromiseArgFromResultPromise(ADeferred deferred) : base(deferred) { }

		Promise<TArg> IValueContainer<Promise<TArg>>.Value
		{
			get { return result; }
		}

		public void Invoke()
		{
			result = callback.Invoke()
				.Complete(() =>
				{
					Value = result.Value;
				});
		}

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke();
		}

		internal override bool TryGetPromiseResultInternal(out Promise promise)
		{
			promise = result;
			return true;
		}
	}

	internal sealed class PromiseArgFromArgResultPromise<TArg, PResult> : Promise<TArg>, IDelegateArgResult<PResult, Promise<TArg>>, ILinked<PromiseArgFromArgResultPromise<TArg, PResult>>
	{
		public PromiseArgFromArgResultPromise<TArg, PResult> Next { get { return (PromiseArgFromArgResultPromise<TArg, PResult>) NextInternal; } set { NextInternal = value; } }

		internal Func<PResult, Promise<TArg>> callback;
		internal Promise<TArg> result;

		internal PromiseArgFromArgResultPromise(ADeferred deferred) : base(deferred) { }

		Promise<TArg> IValueContainer<Promise<TArg>>.Value
		{
			get { return result; }
		}

		public void Invoke(PResult arg)
		{
			result = callback.Invoke(arg)
				.Complete(() =>
				{
					Value = result.Value;
				});
		}

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke(((IValueContainer<PResult>) feed).Value);
		}

		internal override bool TryGetPromiseResultInternal(out Promise promise)
		{
			promise = result;
			return true;
		}
	}
}