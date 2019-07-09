using System;

namespace ProtoPromise
{
	public interface ICancelable
	{
		void Cancel();
	}

	public interface ICancelable<T>
	{
		void Cancel(T reason);
	}

	public interface ICancelableAny : ICancelable
	{
		void Cancel<TCancel>(TCancel reason);
	}

	public interface IRetainable
	{
		void Retain();
		void Release();
	}

	partial class Promise
	{
#if DEBUG
		private static System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(128);

		private static string GetStackTrace(int skipFrames)
		{
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

		partial class Internal
		{
			public interface IValueContainer : IRetainable
			{
				bool TryGetValueAs<U>(out U value);
			}

			public interface IDelegate : ILinked<IDelegate>, IDisposable
			{
				bool TryInvoke(IValueContainer valueContainer);
				void Invoke(Promise feed);
			}
			public interface IDelegate<TResult> : IDisposable
			{
				bool TryInvoke(IValueContainer valueContainer, out TResult result);
				TResult Invoke(Promise feed);
			}
			public interface IFilter : IDisposable
			{
				bool RunThroughFilter(IValueContainer valueContainer);
			}

			public class DelegateVoid : IDelegate, ILinked<DelegateVoid>
			{
				public IDelegate Next { get; set; }
				DelegateVoid ILinked<DelegateVoid>.Next { get { return (DelegateVoid) Next; } set { Next = value; } }

				private Action _callback;

				protected static ValueLinkedStack<IDelegate> pool;

#pragma warning disable RECS0146 // Member hides static member from outer class
				public static DelegateVoid GetOrCreate(Action callback)
#pragma warning restore RECS0146 // Member hides static member from outer class
				{
					var del = pool.IsNotEmpty ? (DelegateVoid) pool.Pop() : new DelegateVoid();
					del._callback = callback;
					return del;
				}

				static DelegateVoid()
				{
					OnClearPool += () => pool.Clear();
				}

				private DelegateVoid() { }

				public void Invoke()
				{
					var temp = _callback;
					Dispose();
					temp.Invoke();
				}

				public void Dispose()
				{
					_callback = null;
					pool.Push(this);
				}

				public bool TryInvoke(IValueContainer valueContainer)
				{
					Invoke();
					return true;
				}

				public void Invoke(Promise feed)
				{
					Invoke();
				}
			}

			public sealed class DelegateArg<TArg> : IDelegate
			{
				public IDelegate Next { get; set; }

				private Action<TArg> _callback;

#pragma warning disable RECS0108 // Warns about static fields in generic types
				private static ValueLinkedStack<IDelegate> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

#pragma warning disable RECS0146 // Member hides static member from outer class
				public static DelegateArg<TArg> GetOrCreate(Action<TArg> callback)
#pragma warning restore RECS0146 // Member hides static member from outer class
				{
					var del = pool.IsNotEmpty ? (DelegateArg<TArg>) pool.Pop() : new DelegateArg<TArg>();
					del._callback = callback;
					return del;
				}

				static DelegateArg()
				{
					OnClearPool += () => pool.Clear();
				}

				private DelegateArg() { }

				public void Invoke(TArg arg)
				{
					var temp = _callback;
					Dispose();
					temp.Invoke(arg);
				}

				public void Dispose()
				{
					_callback = null;
					pool.Push(this);
				}

				public bool TryInvoke(IValueContainer valueContainer)
				{
					TArg arg;
					if (!valueContainer.TryGetValueAs(out arg))
					{
						return false;
					}
					Invoke(arg);
					return true;
				}

				public void Invoke(Promise feed)
				{
					Invoke(((IValueContainer<TArg>) feed).Value);
				}
			}

			public sealed class DelegateVoid<TResult> : IDelegate<TResult>, ILinked<DelegateVoid<TResult>>
			{
				DelegateVoid<TResult> ILinked<DelegateVoid<TResult>>.Next { get; set; }
				public IDelegate<TResult> Next { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

				private Func<TResult> _callback;

#pragma warning disable RECS0108 // Warns about static fields in generic types
				private static ValueLinkedStack<DelegateVoid<TResult>> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

				public static DelegateVoid<TResult> GetOrCreate(Func<TResult> callback)
				{
					var del = pool.IsNotEmpty ? pool.Pop() : new DelegateVoid<TResult>();
					del._callback = callback;
					return del;
				}

				static DelegateVoid()
				{
					OnClearPool += () => pool.Clear();
				}

				private DelegateVoid() { }

				public TResult Invoke()
				{
					var temp = _callback;
					Dispose();
					return temp.Invoke();
				}

				public void Dispose()
				{
					_callback = null;
					pool.Push(this);
				}

				public bool TryInvoke(IValueContainer valueContainer, out TResult result)
				{
					result = Invoke();
					return true;
				}

				public TResult Invoke(Promise feed)
				{
					return Invoke();
				}
			}

			public sealed class DelegateArg<TArg, TResult> : IDelegate<TResult>, ILinked<DelegateArg<TArg, TResult>>
			{
				DelegateArg<TArg, TResult> ILinked<DelegateArg<TArg, TResult>>.Next { get; set; }

				private Func<TArg, TResult> _callback;

#pragma warning disable RECS0108 // Warns about static fields in generic types
				private static ValueLinkedStack<DelegateArg<TArg, TResult>> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

				public static DelegateArg<TArg, TResult> GetOrCreate(Func<TArg, TResult> callback)
				{
					var del = pool.IsNotEmpty ? pool.Pop() : new DelegateArg<TArg, TResult>();
					del._callback = callback;
					return del;
				}

				static DelegateArg()
				{
					OnClearPool += () => pool.Clear();
				}

				private DelegateArg() { }

				public TResult Invoke(TArg arg)
				{
					var temp = _callback;
					Dispose();
					return temp.Invoke(arg);
				}

				public void Dispose()
				{
					_callback = null;
					pool.Push(this);
				}

				public bool TryInvoke(IValueContainer valueContainer, out TResult result)
				{
					TArg arg;
					if (!valueContainer.TryGetValueAs(out arg))
					{
						result = default(TResult);
						return false;
					}

					result = Invoke(arg);
					return true;
				}

				public TResult Invoke(Promise feed)
				{
					return Invoke(((IValueContainer<TArg>) feed).Value);
				}
			}

			public sealed class Filter : IFilter, ILinked<Filter>
			{
				Filter ILinked<Filter>.Next { get; set; }

				private Func<bool> _callback;

				private static ValueLinkedStack<Filter> pool;

				public static Filter GetOrCreate(Func<bool> callback)
				{
					var del = pool.IsNotEmpty ? pool.Pop() : new Filter();
					del._callback = callback;
					return del;
				}

				static Filter()
				{
					OnClearPool += () => pool.Clear();
				}

				private Filter() { }

				public bool RunThroughFilter(IValueContainer valueContainer)
				{
					try
					{
						var temp = _callback;
						_callback = null;
						return temp.Invoke();
					}
					catch (Exception e)
					{
						Logger.LogWarning("Caught an exception in a promise onRejectedFilter. Assuming filter returned false. Logging exception next...");
						Logger.LogException(e);
						return false;
					}
				}

				public void Dispose()
				{
					pool.Push(this);
				}
			}

			public sealed class Filter<TArg> : IFilter, ILinked<Filter<TArg>>
			{
				Filter<TArg> ILinked<Filter<TArg>>.Next { get; set; }

				private Func<TArg, bool> _callback;

#pragma warning disable RECS0108 // Warns about static fields in generic types
				private static ValueLinkedStack<Filter<TArg>> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

				public static Filter<TArg> GetOrCreate(Func<TArg, bool> callback)
				{
					var del = pool.IsNotEmpty ? pool.Pop() : new Filter<TArg>();
					del._callback = callback;
					return del;
				}

				static Filter()
				{
					OnClearPool += () => pool.Clear();
				}

				private Filter() { }

				public bool RunThroughFilter(IValueContainer valueContainer)
				{
					TArg arg;
					if (!valueContainer.TryGetValueAs(out arg))
					{
						return false;
					}

					try
					{
						var temp = _callback;
						_callback = null;
						return temp.Invoke(arg);
					}
					catch (Exception e)
					{
						Logger.LogWarning("Caught an exception in a promise onRejectedFilter. Assuming filter returned false. Logging exception next...");
						Logger.LogException(e);
						return false;
					}
				}

				public void Dispose()
				{
					pool.Push(this);
				}
			}


			public sealed class UnhandledExceptionVoid : UnhandledException, IValueContainer
			{
				// We can reuse the same object.
				private static readonly UnhandledExceptionVoid obj = new UnhandledExceptionVoid();

				private UnhandledExceptionVoid() { }

				public static UnhandledExceptionVoid GetOrCreate()
				{
					return obj;
				}

				public new UnhandledExceptionVoid SetStackTrace(string stackTrace)
				{
					base.SetStackTrace(stackTrace);
					return this;
				}

				public override string Message
				{
					get
					{
						return "A non-value rejection was not handled.";
					}
				}

				public bool TryGetValueAs<U>(out U value)
				{
					value = default(U);
					return false;
				}

				public void Retain() { }

				public void Release() { }
			}

			public sealed class UnhandledException<T> : UnhandledException, IValueContainer
			{
				public T Value { get; private set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
				private static ValueLinkedStack<UnhandledException> pool = new ValueLinkedStack<UnhandledException>();
#pragma warning restore RECS0108 // Warns about static fields in generic types

				private uint retainCounter;

				static UnhandledException()
				{
					OnClearPool += () => pool.Clear();
				}

				private UnhandledException() { }

				public static UnhandledException<T> GetOrCreate(T value)
				{
					UnhandledException<T> ex = pool.IsNotEmpty ? (UnhandledException<T>) pool.Pop() : new UnhandledException<T>();
					ex.Value = value;
					return ex;
				}

				public new UnhandledException<T> SetStackTrace(string stackTrace)
				{
					base.SetStackTrace(stackTrace);
					return this;
				}

				public bool TryGetValueAs<U>(out U value)
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
						if (typeof(U).IsAssignableFrom(typeof(T)) || Value is U)
						{
							value = (U) (object) Value;
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

				public void Retain()
				{
					++retainCounter;
				}

				public void Release()
				{
					if (--retainCounter == 0)
					{
						Value = default(T);
						pool.Push(this);
					}
				}
			}

			public sealed class UnhandledExceptionException : UnhandledException, IValueContainer
			{
				private UnhandledExceptionException(Exception innerException) : base(innerException) { }


				// Don't care about re-using this exception for 2 reasons:
				// exceptions create garbage themselves, creating a little more with this one is negligible,
				// and it's too difficult to try to replicate the formatting for Unity to pick it up by using a cached local variable like in UnhandledException<T>, and prefer not to use reflection to assign innerException
				public static UnhandledExceptionException GetOrCreate(Exception innerException)
				{
					return new UnhandledExceptionException(innerException);
				}

				public new UnhandledExceptionException SetStackTrace(string stackTrace)
				{
					base.SetStackTrace(stackTrace);
					return this;
				}

				public override string Message
				{
					get
					{
						return "An exception was encountered that was not handled.";
					}
				}

				public bool TryGetValueAs<U>(out U value)
				{
					if (InnerException is U)
					{
						value = (U) (object) InnerException;
						return true;
					}
					value = default(U);
					return false;
				}

				public void Retain() { }

				public void Release() { }
			}

			public sealed class CancelVoid : IValueContainer
			{
				// We can reuse the same object.
				private static readonly CancelVoid obj = new CancelVoid();

				private CancelVoid() { }

				public static CancelVoid GetOrCreate()
				{
					return obj;
				}

				public bool TryGetValueAs<U>(out U value)
				{
					value = default(U);
					return false;
				}

				public void Retain() { }

				public void Release() { }
			}

			public sealed class ValueContainer<T> : IValueContainer, ILinked<ValueContainer<T>>
			{
				public ValueContainer<T> Next { get; set; }

				public T Value { get; private set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
                private static ValueLinkedStack<ValueContainer<T>> pool = new ValueLinkedStack<ValueContainer<T>>();
#pragma warning restore RECS0108 // Warns about static fields in generic types

				private uint retainCounter;

				static ValueContainer()
				{
					OnClearPool += () => pool.Clear();
				}

				private ValueContainer() { }

				public static ValueContainer<T> GetOrCreate(T value)
				{
					ValueContainer<T> ex = pool.IsNotEmpty ? pool.Pop() : new ValueContainer<T>();
					ex.Value = value;
					return ex;
				}

				public bool TryGetValueAs<U>(out U value)
				{
					// This avoids boxing value types.
					var casted = this as ValueContainer<U>;
					if (casted != null)
					{
						value = casted.Value;
						return true;
					}
					if (!typeof(T).IsValueType)
					{
						if (typeof(U).IsAssignableFrom(typeof(T)) || Value is U)
						{
							value = (U) (object) Value;
							return true;
						}
					}
					value = default(U);
					return false;
				}

				public void Retain()
				{
					++retainCounter;
				}

				public void Release()
				{
					if (--retainCounter == 0)
					{
						Value = default(T);
						pool.Push(this);
					}
				}
			}
		}

		public class InvalidReturnException : InvalidOperationException
		{
			public InvalidReturnException(string message, string stackTrace = null) : base(message)
			{
				_stackTrace = stackTrace;
			}

			private readonly string _stackTrace;
			public override string StackTrace { get { return _stackTrace; } }
		}

		public abstract class UnhandledException : Exception, ILinked<UnhandledException>
		{
			UnhandledException ILinked<UnhandledException>.Next { get; set; }

			protected UnhandledException() { }
			protected UnhandledException(Exception innerException) : base(null, innerException) { }

			public bool unhandled = true;

			protected void SetStackTrace(string stackTrace)
			{
				_stackTrace = stackTrace;
			}

			private string _stackTrace;
			public override sealed string StackTrace
			{
				get
				{
					return _stackTrace;
				}
			}
		}
	}

	public interface ILinked<T> where T : class, ILinked<T>
	{
		T Next { get; set; }
	}

	/// <summary>
	/// This structure is unsuitable for general purpose.
	/// </summary>
	public struct ValueLinkedStack<T> where T : class, ILinked<T>
	{
		T first;

		public bool IsEmpty { get { return first == null; } }
		public bool IsNotEmpty { get { return first != null; } }

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
	/// This structure is unsuitable for general purpose.
	/// </summary>
	public struct ValueLinkedQueue<T> where T : class, ILinked<T>
	{
		T first;
		T last;

		public bool IsEmpty { get { return first == null; } }
		public bool IsNotEmpty { get { return first != null; } }

		public ValueLinkedQueue(T item)
		{
			item.Next = null;
			first = last = item;
		}

		public ValueLinkedQueue(ValueLinkedQueue<T> other)
		{
			first = other.first;
			last = other.last;
		}

		public void Clear()
		{
			first = last = null;
		}

		public void AddLast(T item)
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
		/// Only use this to add if you know this isn't empty.
		/// </summary>
		/// <param name="item">Item.</param>
		public void AddLastRisky(T item)
		{
			item.Next = null;
			last.Next = item;
			last = item;
		}

		public void AddFirst(T item)
		{
			item.Next = first;
			first = item;
		}

		/// <summary>
		/// <paramref name="index"/> must be greater than 0. If index is 0, use AddFirst instead.
		/// </summary>
		public void Insert(T item, int index)
		{
			T current = first;
			while (index > 0)
			{
				current = current.Next;
				--index;
			}
		}

		public T TakeFirst()
		{
			T temp = first;
			first = first.Next;
			return temp;
		}

		public T PeekFirst()
		{
			return first;
		}

		public T PeekLast()
		{
			return last;
		}

		public void Append(ValueLinkedQueue<T> other)
		{
			last.Next = other.first;
			last = other.last;
		}
	}

	public struct ValueLinkedStackZeroGC<T>
	{
		private class Node : ILinked<Node>
		{
#pragma warning disable RECS0108 // Warns about static fields in generic types
			private static ValueLinkedStack<Node> pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

			public static void ClearPool()
			{
				pool.Clear();
			}

			public Node Next { get; set; }

			public T item;

			public static Node GetOrCreate(T item)
			{
				Node node = pool.IsNotEmpty ? pool.Pop() : new Node();
				node.item = item;
				return node;
			}

			public T TakeItemAndDispose()
			{
				T temp = item;
				Dispose();
				return temp;
			}

			public void Dispose()
			{
				item = default(T);
				pool.Push(this);
			}
		}

		public static void ClearPooledNodes()
		{
			Node.ClearPool();
		}

		private ValueLinkedStack<Node> stack;

		public bool IsEmpty { get { return stack.IsEmpty; } }
		public bool IsNotEmpty { get { return stack.IsNotEmpty; } }

		public void Clear()
		{
			while (stack.IsNotEmpty)
			{
				stack.Pop().Dispose();
			}
		}

		public void ClearAndDontRepool()
		{
			stack.Clear();
		}

		public void Push(T item)
		{
			stack.Push(Node.GetOrCreate(item));
		}

		public T Pop()
		{
			return stack.Pop().TakeItemAndDispose();
		}

		public T Peek()
		{
			return stack.Peek().item;
		}
	}
}