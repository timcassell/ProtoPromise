using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoPromise
{
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

	internal sealed class ValueContainer<T> : ValueContainer, IValueContainer<T>
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


	internal interface ILinked<T> where T : class, ILinked<T>
	{
		T Next { get; set; }
	}

	/// <summary>
	///  This structure is unsuitable for general purpose.
	/// </summary>
	internal class LinkedStackClass<T> where T : class, ILinked<T>
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
	internal struct LinkedStackStruct<T> where T : class, ILinked<T>
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
	internal class LinkedQueueClass<T> where T : class, ILinked<T>
	{
		T first;
		T last;

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
	internal struct LinkedQueueStruct<T>  where T : class, ILinked<T>
	{
		T first;
		T last;

		public LinkedQueueStruct(T item)
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