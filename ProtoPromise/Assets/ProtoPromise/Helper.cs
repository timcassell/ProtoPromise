using System;
using System.Collections.Generic;

namespace ProtoPromise
{
	partial class Promise
	{
#if !DEBUG
		public static bool GenerateDebugStacktrace { get { return false; } set { } }
#else
		public static bool GenerateDebugStacktrace { get; set; }

		private static System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(128);

		// TODO: Only do this in debug mode.
		internal static string GetStackTrace(int skipFrames)
		{
			if (!GenerateDebugStacktrace)
			{
				return string.Empty;
			}

			string stackTrace = new System.Diagnostics.StackTrace(skipFrames, true).ToString();
			if (string.IsNullOrEmpty(stackTrace))
			{
				return stackTrace;
			}

			stringBuilder.Length = 0;
			stringBuilder.Append(stackTrace);

			// Format stacktrace to match "throw exception" so that double-clicking log in Unity console will go to the proper line.
			return stringBuilder.Remove(0, 1)
				.Replace(":line ", ":")
				.Replace("\n ", " \n")
				.Replace("(", " (")
				.Replace(") in", ") [0x00000] in") // Not sure what "[0x00000]" is, but it's necessary for Unity's parsing.
				.Append(" ")
				.ToString();
		}
#endif
	}

	public class NullPromiseException : Exception, ILinked<NullPromiseException>
	{
		NullPromiseException ILinked<NullPromiseException>.Next { get; set; }

		public NullPromiseException SetStackTrace(string stackTrace)
		{
			_stackTrace = stackTrace;
			return this;
		}

		private string _stackTrace;
		public override string StackTrace { get { return _stackTrace; } }

		public override string Message
		{
			get { return "A null promise was returned."; }
		}
	}

	public class UnhandledException : Exception, ILinked<UnhandledException>
	{
		internal UnhandledException nextInternal;
		UnhandledException ILinked<UnhandledException>.Next { get { return nextInternal; } set { nextInternal = value; } }

		public void SetStackTrace(string stackTrace)
		{
			_stackTrace = stackTrace;
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
				return "A non-value rejection was not handled.";
			}
		}

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

		public UnhandledException<T> SetValue(T value)
		{
			Value = value;
			return this;
		}

		public override sealed bool TryGetValueAs<U>(out U value)
		{
			// This avoids boxing value types.
			var casted = this as UnhandledException<U>;
			if (casted != null)
			{
				value = casted.Value;
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
			if (value == null)
			{
				value = new NullReferenceException();
			}
			base.SetValue(value);
			SetStackTrace(value.StackTrace);
			return this;
		}

		public override string Message
		{
			get
			{
				return "An exception was encountered that was not handled: " + Value;
			}
		}
	}

	internal interface IValueContainer { }
	internal interface IValueContainer<T> : IValueContainer
	{
		T Value { get; }
	}

	// Used IFilter and IDelegate(Result) to reduce the amount of classes I would have to generate to handle catches. I'm less concerned about performance for catches since exceptions are expensive anyway.
	internal interface IDelegate : ILinked<IDelegate>
	{
		bool TryInvoke(UnhandledException unhandledException);
	}
	internal interface IDelegateResult
	{
		bool TryInvoke<TResult>(UnhandledException unhandledException, out TResult result);
	}
	internal interface IDelegateArg
	{
		bool TryInvoke<TArg>(TArg arg);
	}
	internal interface IFilter
	{
		bool RunThroughFilter(UnhandledException valueToCatch);
	}

	// TODO: pool delegates.
	internal sealed class DelegateVoidVoid : IDelegate, ILinked<DelegateVoidVoid>
	{
		public IDelegate Next { get; set; }

		DelegateVoidVoid ILinked<DelegateVoidVoid>.Next { get { return (DelegateVoidVoid) Next; } set { Next = value; } }
		
		internal Action callback;

		public void Invoke()
		{
			var temp = callback;
			callback = null;
			temp.Invoke();
		}

		public bool TryInvoke(UnhandledException unhandledException)
		{
			Invoke();
			return true;
		}
	}

	internal sealed class DelegateArgVoid<TArg> : IDelegate, IDelegateArg, ILinked<DelegateArgVoid<TArg>>
	{
		public IDelegate Next { get; set; }

		DelegateArgVoid<TArg> ILinked<DelegateArgVoid<TArg>>.Next { get { return (DelegateArgVoid<TArg>) Next; } set { Next = value; } }

		internal Action<TArg> callback;

		public void Invoke(TArg arg)
		{
			var temp = callback;
			callback = null;
			temp.Invoke(arg);
		}

		public bool TryInvoke<TArg1>(TArg1 arg)
		{
			// This avoids boxing value types.
			var casted = this as DelegateArgVoid<TArg1>;
			if (casted != null)
			{
				casted.Invoke(arg);
				return true;
			}
			if (!typeof(TArg).IsValueType)
			{
				object val = arg;
				if (typeof(TArg).IsAssignableFrom(typeof(TArg1)) || (val != null && arg is TArg))
				{
					Invoke((TArg) val);
					return true;
				}
			}
			return false;
		}

		public bool TryInvoke(UnhandledException unhandledException)
		{
			TArg arg;
			if (!unhandledException.TryGetValueAs(out arg))
			{
				return false;
			}
			Invoke(arg);
			return true;
		}
	}

	internal class DelegateVoidResult<TResult> : IDelegateResult, ILinked<DelegateVoidResult<TResult>>
	{
		DelegateVoidResult<TResult> ILinked<DelegateVoidResult<TResult>>.Next { get; set; }

		internal Func<TResult> callback;

		public TResult Invoke()
		{
			var temp = callback;
			callback = null;
			return temp.Invoke();
		}

		public bool TryInvoke<TResult1>(UnhandledException unhandledException, out TResult1 result)
		{
			// This avoids boxing value types.
			var casted = this as DelegateVoidResult<TResult1>;
			if (casted != null)
			{
				result = casted.Invoke();
				return true;
			}
			if (typeof(TResult1).IsAssignableFrom(typeof(TResult)))
			{
				result = (TResult1) (object) Invoke();
				return true;
			}
			result = default(TResult1);
			return false;
		}
	}

	internal class DelegateArgResult<TArg, TResult> : IDelegateResult, ILinked<DelegateArgResult<TArg, TResult>>
	{
		DelegateArgResult<TArg, TResult> ILinked<DelegateArgResult<TArg, TResult>>.Next { get; set; }

		internal Func<TArg, TResult> callback;

		public TResult Invoke(TArg arg)
		{
			var temp = callback;
			callback = null;
			return temp.Invoke(arg);
		}

		public bool TryInvoke<TResult1>(UnhandledException unhandledException, out TResult1 result)
		{
			TArg arg;
			if (!unhandledException.TryGetValueAs(out arg))
			{
				result = default(TResult1);
				return false;
			}

			// This avoids boxing value types.
			var casted = this as DelegateArgResult<TArg, TResult1>;
			if (casted != null)
			{
				result = casted.Invoke(arg);
				return true;
			}
			if (typeof(TResult1).IsAssignableFrom(typeof(TResult)))
			{
				result = (TResult1) (object) Invoke(arg);
				return true;
			}
			result = default(TResult1);
			return false;
		}
	}

	internal sealed class Filter : DelegateVoidResult<bool>, IFilter
	{
		public bool RunThroughFilter(UnhandledException valueToCatch)
		{
			try
			{
				return Invoke();
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogWarning("Caught an exception in a promise onRejectedFilter. Assuming filter returned false. Logging exception next...");
				UnityEngine.Debug.LogException(e);
				return false;
			}
		}
	}

	internal sealed class Filter<TArg> : DelegateArgResult<TArg, bool>, IFilter
	{
		public bool RunThroughFilter(UnhandledException valueToCatch)
		{
			TArg arg;
			if (!valueToCatch.TryGetValueAs(out arg))
			{
				return false;
			}

			try
			{
				return Invoke(arg);
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogWarning("Caught an exception in a promise onRejectedFilter. Assuming filter returned false. Logging exception next...");
				UnityEngine.Debug.LogException(e);
				return false;
			}
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

		public bool IsEmpty { get { return first == null; } }

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

		public bool IsEmpty { get { return first == null; } }

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

	//public static class PromiseExtensions
	//{
	//	/// <summary>
	//	/// Helper method for promise.Notification{<see cref="float"/>}(onProgress).
	//	/// </summary>
	//	public static Promise Progress(this Promise promise, Action<float> onProgress)
	//	{
	//		return promise.Notification(onProgress);
	//	}

	//	/// <summary>
	//	/// Helper method for promise.Notification{<see cref="float"/>}(onProgress).
	//	/// </summary>
	//	public static Promise<T> Progress<T>(this Promise<T> promise, Action<float> onProgress)
	//	{
	//		return promise.Notification(onProgress);
	//	}

	//	public static Promise Catch(this Promise promise, Action<object> onRejected)
	//	{
	//		return promise.Catch(onRejected);
	//	}

	//	public static Promise<T> Catch<T>(this Promise<T> promise, Func<object, T> onRejected)
	//	{
	//		return promise.Catch(onRejected);
	//	}

	//	public static Promise<T> Catch<T>(this Promise<T> promise, Func<object, Promise<T>> onRejected)
	//	{
	//		return promise.Catch(onRejected);
	//	}

	//	// TODO: Implement deferred catches.
	//	//public static Promise Catch(this Promise promise, Func<object, Action<Deferred>> onRejected)
	//	//{
	//	//	return promise.Catch(onRejected);
	//	//}

	//	public static Promise Then(this Promise promise, Action onResolved, Action<object> onRejected)
	//	{
	//		return promise.Then(onResolved, onRejected);
	//	}

	//	// TODO
	//	public static Promise Then(this Promise promise, Action onResolved, Func<object, Promise> onRejected)
	//	{
	//		return promise.Then(onResolved, onRejected);
	//	}

	//	public static Promise<T> Then<T>(this Promise promise, Func<T> onResolved, Func<object, T> onRejected)
	//	{
	//		return promise.Then(onResolved, onRejected);
	//	}

	//	// TODO
	//	public static Promise Then<T>(this Promise<T> promise, Action<T> onResolved, Func<object, Promise> onRejected)
	//	{
	//		return promise.Then(onResolved, onRejected);
	//	}

	//	public static Promise<T> Then<T, U>(this Promise<U> promise, Func<U, T> onResolved, Func<object, T> onRejected)
	//	{
	//		return promise.Then(onResolved, onRejected);
	//	}

	//	public static Promise<T> Then<T>(this Promise promise, Func<T> onResolved, Func<object, Promise<T>> onRejected)
	//	{
	//		return promise.Then(onResolved, onRejected);
	//	}

	//	public static Promise<T> Then<T>(this Promise promise, Func<Promise<T>> onResolved, Func<object, T> onRejected)
	//	{
	//		return promise.Then(onResolved, onRejected);
	//	}

	//	public static Promise<T> Then<T>(this Promise promise, Func<Promise<T>> onResolved, Func<object, Promise<T>> onRejected)
	//	{
	//		return promise.Then(onResolved, onRejected);
	//	}

	//	//public static Promise<T> Then<T, U>(this Promise<U> promise, Func<U, T> onResolved, Func<object, Promise<T>> onRejected)
	//	//{
	//	//	return promise.Then(onResolved, onRejected);
	//	//}

	//	public static Promise<T> Then<T, U>(this Promise<U> promise, Func<U, Promise<T>> onResolved, Func<object, T> onRejected)
	//	{
	//		return promise.Then(onResolved, onRejected);
	//	}

	//	public static Promise<T> Then<T, U>(this Promise<U> promise, Func<U, Promise<T>> onResolved, Func<object, Promise<T>> onRejected)
	//	{
	//		return promise.Then(onResolved, onRejected);
	//	}
	//}
}