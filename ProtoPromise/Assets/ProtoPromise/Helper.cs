using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	public class UnhandledException : Exception, ILinked<UnhandledException>
	{
		internal UnhandledException nextInternal;
		UnhandledException ILinked<UnhandledException>.Next { get { return nextInternal; } set { nextInternal = value; } }

		public virtual bool TryGetValueAs<U>(out U value)
		{
			value = default(U);
			return false;
		}
	}

	public class UnhandledException<T> : UnhandledException, IValueContainer<T>, ILinked<UnhandledException<T>>
	{
		UnhandledException<T> ILinked<UnhandledException<T>>.Next { get { return (UnhandledException<T>) nextInternal; } set { nextInternal = value; } }

		public T Value { get; private set; }

		public UnhandledException<T> SetValue(T value, string stackTrace)
		{
			Value = value;
			_stackTrace = stackTrace;
			return this;
		}

		public override sealed bool TryGetValueAs<U>(out U value)
		{
			// This avoids boxing value types.
			if (this is UnhandledException<U>)
			{
				value = (this as UnhandledException<U>).Value;
				return true;
			}
			if (!typeof(T).IsValueType)
			{
				object val = Value;
				if (typeof(U).IsAssignableFrom(typeof(T)) || (val != null && val is U))
				{
					value = (U) val;
					return true;
				}
			}
			value = default(U);
			return false;
		}

		protected string _stackTrace;
		public override sealed string StackTrace
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
				return "A rejected value was not handled: " + (Value.ToString() ?? "null");
			}
		}
	}

	public sealed class UnhandledExceptionException : UnhandledException<Exception>, ILinked<UnhandledExceptionException>
	{
		UnhandledExceptionException ILinked<UnhandledExceptionException>.Next { get { return (UnhandledExceptionException) nextInternal; } set { nextInternal = value; } }

		// value is never null
		public UnhandledExceptionException SetValue(Exception value)
		{
			base.SetValue(value, value.StackTrace);
			return this;
		}

		public new UnhandledExceptionException SetValue(Exception value, string stackTrace)
		{
			if (value == null)
			{
				value = new NullReferenceException();
			}
			else
			{
				stackTrace = value.StackTrace + "\n" + stackTrace;
			}
			base.SetValue(value, stackTrace);
			return this;
		}

		public override string Message
		{
			get
			{
				return "An exception was encountered that was not handled.";
			}
		}
	}


	internal interface ITryInvokable
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

	internal class ValueContainer<T> : ValueContainer, IValueContainer<T>
	{
		public T Value { get; set; }
	}

	internal sealed class DelegateVoid : IDelegateVoid, ITryInvokable, ILinked<DelegateVoid>
	{
		DelegateVoid ILinked<DelegateVoid>.Next
		{
			get
			{
				return (DelegateVoid) Next;
			}
			set
			{
				Next = value;
			}
		}

		public IDelegate Next { get; set; }

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
		DelegateArg<TArg> ILinked<DelegateArg<TArg>>.Next
		{
			get
			{
				return (DelegateArg<TArg>) Next;
			}
			set
			{
				Next = value;
			}
		}

		public IDelegate Next { get; set; }

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
		DelegateVoidResult<TResult> ILinked<DelegateVoidResult<TResult>>.Next
		{
			get
			{
				return (DelegateVoidResult<TResult>) Next;
			}
			set
			{
				Next = value;
			}
		}

		public IDelegate Next { get; set; }

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
		DelegateArgResult<TArg, TResult> ILinked<DelegateArgResult<TArg, TResult>>.Next
		{
			get
			{
				return (DelegateArgResult<TArg, TResult>) Next;
			}
			set
			{
				Next = value;
			}
		}

		public IDelegate Next { get; set; }

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


	internal class ObjectPool
	{
		private Dictionary<Type, object> pool = new Dictionary<Type, object>();

		public bool TryTakeInternal<T>(out T item) where T : class, ILinked<T>
		{
			object obj;
			if (pool.TryGetValue(typeof(T), out obj))
			{
				LinkedStack<T> stack = (LinkedStack<T>) obj;
				if (!stack.IsEmpty)
				{
					item = stack.Pop();
					return true;
				}
			}
			item = default(T);
			return false;
		}

		public void AddInternal<T>(T item) where T : class, ILinked<T>
		{
			object obj;
			LinkedStack<T> stack;
			if (pool.TryGetValue(typeof(T), out obj))
			{
				stack = (LinkedStack<T>) obj;
			}
			else
			{
				pool.Add(typeof(T), stack = new LinkedStack<T>());
			}
			stack.Push(item);
		}
	}


	internal interface ILinked<T> where T : class, ILinked<T>
	{
		T Next { get; set; }
	}

	/// <summary>
	///  This structure is unsuitable for general purpose.
	/// </summary>
	internal sealed class LinkedStack<T> where T : class, ILinked<T>
	{
		T first;

		public bool IsEmpty { get { return first == null; } }

		public void Clear()
		{
			first = null;
		}

		public void Push(T item)
		{
			item.Next = first;
			first = item;
		}

		public T Pop()
		{
			T temp = first;
			first = first.Next;
			return temp;
		}

		public T Peek()
		{
			return first;
		}
	}

	/// <summary>
	///  This structure is unsuitable for general purpose.
	/// </summary>
	internal struct ValueLinkedStack<T> where T : class, ILinked<T>
	{
		T first;

		public bool IsEmpty { get { return first == null; } }

		public void Clear()
		{
			first = null;
		}

		public void Push(T item)
		{
			item.Next = first;
			first = item;
		}

		public T Pop()
		{
			T temp = first;
			first = first.Next;
			return temp;
		}

		public T Peek()
		{
			return first;
		}
	}

	/// <summary>
	///  This structure is unsuitable for general purpose.
	/// </summary>
	internal sealed class LinkedQueue<T> where T : class, ILinked<T>
	{
		T first;
		T last;

		public LinkedQueue() { }

		public LinkedQueue(T item)
		{
			item.Next = null;
			first = last = item;
		}

		public void Clear()
		{
			first = last = null;
		}

		public void Enqueue(T item)
		{
			item.Next = null;
			if (first == null)
			{
				first = last = item;
			}
			else
			{
				last.Next = item;
				last = item;
			}
		}

		public T Peek()
		{
			return first;
		}
	}

	/// <summary>
	///  This structure is unsuitable for general purpose.
	/// </summary>
	internal struct ValueLinkedQueue<T>  where T : class, ILinked<T>
	{
		T first;
		T last;

		public ValueLinkedQueue(T item)
		{
			item.Next = null;
			first = last = item;
		}

		public void Clear()
		{
			first = last = null;
		}

		public void Enqueue(T item)
		{
			item.Next = null;
			if (first == null)
			{
				first = last = item;
			}
			else
			{
				last.Next = item;
				last = item;
			}
		}

		/// <summary>
		/// Only use this to enqueue if you know this isn't empty.
		/// </summary>
		/// <param name="item">Item.</param>
		public void EnqueueRisky(T item)
		{
			item.Next = null;
			last.Next = item;
			last = item;
		}

		public T Peek()
		{
			return first;
		}
	}

	public static class PromiseExtensions
	{
		/// <summary>
		/// Helper method for promise.Notification{<see cref="float"/>}(onProgress).
		/// </summary>
		public static Promise Progress(this Promise promise, Action<float> onProgress)
		{
			return promise.Notification(onProgress);
		}

		/// <summary>
		/// Helper method for promise.Notification{<see cref="float"/>}(onProgress).
		/// </summary>
		public static Promise<T> Progress<T>(this Promise<T> promise, Action<float> onProgress)
		{
			return promise.Notification(onProgress);
		}

		public static Promise Catch(this Promise promise, Action<Exception> onRejected)
		{
			return promise.Catch(onRejected);
		}
	}
}