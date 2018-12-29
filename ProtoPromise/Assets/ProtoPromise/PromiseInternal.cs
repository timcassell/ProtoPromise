using System;

namespace ProtoPromise
{
	internal interface IDelegate
	{
		void Invoke(IDelegate feed);
		bool TryInvoke<U>(U arg);
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
		readonly T _value;

		public ValueContainer(T value)
		{
			_value = value;
		}

		public T Value
		{
			get
			{
				return _value;
			}
		}

		public override void RejectOther(ADeferred other)
		{
			other.Reject(_value);
		}

		public override bool TryHandleRejection(ADeferred other)
		{
			return other.TryHandleRejectionInternal(_value);
		}

		public override void TryInvoke<TArg>(Action<TArg> callback, ADeferred deferred)
		{
			//if (this is ValueContainer<TArg>) // This avoids boxing value types.
			//{
			//	deferred.ResolveUnhandledInternal(); // You never know what someone might do in a callback, so make sure deferred is in a clean state before invoking.
			//	callback.Invoke((this as ValueContainer<TArg>)._value);
			//}

			object val = _value;
			if (typeof(TArg).IsAssignableFrom(typeof(T)) || (val != null && _value is TArg))
			{
				deferred.ResolveUnhandledInternal(); // You never know what someone might do in a callback, so make sure deferred is in a clean state before invoking.
				callback.Invoke((TArg)val);
			}
		}

		public override TResult TryInvoke<TArg, TResult>(Func<TArg, TResult> callback, ADeferred deferred)
		{
			//if (this is ValueContainer<TArg>) // This avoids boxing value types.
			//{
			//	deferred.ResolveUnhandledInternal(); // You never know what someone might do in a callback, so make sure deferred is in a clean state before invoking.
			//	return callback.Invoke((this as ValueContainer<TArg>)._value);
			//}

			object val = _value;
			if (typeof(TArg).IsAssignableFrom(typeof(T)) || (val != null && _value is TArg))
			{
				deferred.ResolveUnhandledInternal(); // You never know what someone might do in a callback, so make sure deferred is in a clean state before invoking.
				return callback.Invoke((TArg)val);
			}
			return default(TResult);
		}
	}

	internal sealed class DelegateVoid : IDelegateVoid
	{
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

		public bool TryInvoke<U>(U arg)
		{
			Invoke();
			return true;
		}
	}

	internal sealed class DelegateArg<TArg> : IDelegateArg<TArg>
	{
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
			Invoke(((IValueContainer<TArg>)feed).Value);
		}

		public void Invoke(TArg arg)
		{
			callback.Invoke(arg);
		}

		public bool TryInvoke<U>(U arg)
		{
			object val = arg;
			if (typeof(TArg).IsAssignableFrom(typeof(U)) || (val != null && arg is TArg))
			{
				Invoke((TArg) val);
				return true;
			}
			return false;
		}
	}

	internal class DelegateVoidResult<TResult> : IDelegateVoidResult<TResult>
	{
		Func<TResult> callback;

		TResult _value;

		public DelegateVoidResult(Func<TResult> func)
		{
			SetCallback(func);
		}

		public TResult Value
		{
			get
			{
				return _value;
			}
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
			_value = callback.Invoke();
		}

		public bool TryInvoke<U>(U arg)
		{
			Invoke();
			return true;
		}
	}

	internal sealed class DelegateArgResult<TArg, TResult> : IDelegateArgResult<TArg, TResult>
	{
		Func<TArg, TResult> callback;

		TResult _value;

		public DelegateArgResult(Func<TArg, TResult> func)
		{
			SetCallback(func);
		}

		public TResult Value
		{
			get { return _value; }
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
			_value = callback.Invoke(arg);
		}

		public bool TryInvoke<U>(U arg)
		{
			object val = arg;
			if (typeof(TArg).IsAssignableFrom(typeof(U)) || (val != null && arg is TArg))
			{
				Invoke((TArg)val);
				return true;
			}
			return false;
		}
	}


	internal sealed class PromiseVoidFromVoid : Promise, IDelegateVoid
	{
		internal Action callback;

		internal PromiseVoidFromVoid(ADeferred deferred) : base(deferred) { }

		internal override void InvokeInternal(IDelegate feed)
		{
			State = PromiseState.Resolved;
			Invoke();
		}

		public void Invoke()
		{
			callback.Invoke();
		}

		public bool TryInvoke<U>(U arg)
		{
			Invoke();
			return true;
		}
	}

	internal sealed class PromiseVoidFromArg<TArg> : Promise, IDelegateArg<TArg>
	{
		internal Action<TArg> callback;

		internal PromiseVoidFromArg(ADeferred deferred) : base(deferred) { }

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke(((IValueContainer<TArg>)feed).Value);
		}

		public void Invoke(TArg arg)
		{
			State = PromiseState.Resolved;
			callback.Invoke(arg);
		}

		public bool TryInvoke<U>(U arg)
		{
			object val = arg;
			if (typeof(TArg).IsAssignableFrom(typeof(U)) || (val != null && arg is TArg))
			{
				Invoke((TArg)val);
				return true;
			}
			return false;
		}
	}

	internal sealed class PromiseArgFromResult<TResult> : Promise<TResult>, IDelegateVoidResult<TResult>
	{
		internal Func<TResult> callback;

		internal PromiseArgFromResult(ADeferred deferred) : base(deferred) { }

		public void Invoke()
		{
			State = PromiseState.Resolved;
			_value = callback.Invoke();
		}

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke();
		}

		public bool TryInvoke<U>(U arg)
		{
			Invoke();
			return true;
		}
	}

	internal sealed class PromiseArgFromArgResult<TArg, TResult> : Promise<TResult>, IDelegateArgResult<TArg, TResult>
	{
		internal Func<TArg, TResult> callback;

		internal PromiseArgFromArgResult(ADeferred deferred) : base(deferred) { }

		public void Invoke(TArg arg)
		{
			State = PromiseState.Resolved;
			_value = callback.Invoke(arg);
		}

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke(((IValueContainer<TArg>) feed).Value);
		}

		public bool TryInvoke<U>(U arg)
		{
			object val = arg;
			if (typeof(TArg).IsAssignableFrom(typeof(U)) || (val != null && arg is TArg))
			{
				Invoke((TArg)val);
				return true;
			}
			return false;
		}
	}

	internal sealed class PromiseVoidFromResultPromise : Promise, IDelegateVoidResult<Promise>
	{
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
			UnityEngine.Debug.LogError("Wait for other promise to complete.");
			result = callback.Invoke();
			result.Complete(() =>
			{
				UnityEngine.Debug.LogError("Done waiting.");
				State = PromiseState.Resolved;
			});
		}

		internal override bool TryGetPromiseResultInternal(out Promise promise)
		{
			promise = result;
			return true;
		}

		public bool TryInvoke<U>(U arg)
		{
			Invoke();
			return true;
		}
	}

	internal sealed class PromiseVoidFromArgResultPromise<TArg> : Promise, IDelegateArgResult<TArg, Promise>
	{
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
			Invoke(((IValueContainer<TArg>)feed).Value);
		}

		public void Invoke(TArg arg)
		{
			result = callback.Invoke(arg);
			result.Complete(() =>
			{
				State = PromiseState.Resolved;
			});
		}

		internal override bool TryGetPromiseResultInternal(out Promise promise)
		{
			promise = result;
			return true;
		}

		public bool TryInvoke<U>(U arg)
		{
			object val = arg;
			if (typeof(TArg).IsAssignableFrom(typeof(U)) || (val != null && arg is TArg))
			{
				Invoke((TArg) val);
				return true;
			}
			return false;
		}
	}

	internal sealed class PromiseArgFromResultPromise<TArg> : Promise<TArg>, IDelegateVoidResult<Promise<TArg>>
	{
		internal Func<Promise<TArg>> callback;
		internal Promise<TArg> result;

		internal PromiseArgFromResultPromise(ADeferred deferred) : base(deferred) { }

		Promise<TArg> IValueContainer<Promise<TArg>>.Value
		{
			get { return result; }
		}

		public void Invoke()
		{
			result = callback.Invoke();
			result.Complete(() =>
			{
				_value = result.Value;
				State = PromiseState.Resolved;
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

		public bool TryInvoke<U>(U arg)
		{
			Invoke();
			return true;
		}
	}

	internal sealed class PromiseArgFromArgResultPromise<TArg, PResult> : Promise<TArg>, IDelegateArgResult<PResult, Promise<TArg>>
	{
		internal Func<PResult, Promise<TArg>> callback;
		internal Promise<TArg> result;

		internal PromiseArgFromArgResultPromise(ADeferred deferred) : base(deferred) { }

		Promise<TArg> IValueContainer<Promise<TArg>>.Value
		{
			get { return result; }
		}

		public void Invoke(PResult arg)
		{
			result = callback.Invoke(arg);
			result.Complete(() =>
			{
				_value = result.Value;
				State = PromiseState.Resolved;
			});
		}

		internal override void InvokeInternal(IDelegate feed)
		{
			Invoke(((IValueContainer<PResult>)feed).Value);
		}

		internal override bool TryGetPromiseResultInternal(out Promise promise)
		{
			promise = result;
			return true;
		}

		public bool TryInvoke<U>(U arg)
		{
			object val = arg;
			if (typeof(PResult).IsAssignableFrom(typeof(U)) || (val != null && arg is PResult))
			{
				Invoke((PResult) val);
				return true;
			}
			return false;
		}
	}
}