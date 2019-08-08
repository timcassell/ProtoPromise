using System;
using System.Collections;
using System.Collections.Generic;

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
        bool IsRetained { get; }
    }


    partial class Promise
	{
        partial void SetCreatedStackTrace(int skipFrames);
        partial void SetStackTraceFromCreated(UnhandledException unhandledException);
        static partial void SetFormattedStackTrace(UnhandledException unhandledException, int skipFrames);
#if DEBUG
        partial void SetCreatedStackTrace(int skipFrames)
        {
            _createdStackTrace = GetStackTrace(skipFrames + 1);
        }

        partial void SetStackTraceFromCreated(UnhandledException unhandledException)
        {
            unhandledException.SetStackTrace(FormatStackTrace(_createdStackTrace));
        }

        static partial void SetFormattedStackTrace(UnhandledException unhandledException, int skipFrames)
        {
            if (Manager.DebugStacktraceGenerator != GeneratedStacktrace.None)
            {
                unhandledException.SetStackTrace(FormatStackTrace(GetStackTrace(skipFrames + 1)));
            }
        }

        private static System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(128);

        private static string GetStackTrace(int skipFrames)
        {
            return new System.Diagnostics.StackTrace(skipFrames + 1, true).ToString();
        }

        private static string FormatStackTrace(string stackTrace)
        { 
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
            public partial interface ITreeHandleAble : ILinked<ITreeHandleAble>
            {
                void Handle(Promise feed);
                void Cancel();
                void Repool();
            }
            public interface IValueContainer : IRetainable
			{
				bool TryGetValueAs<U>(out U value);
			}

			public interface IDelegate
			{
				bool DisposeAndTryInvoke(IValueContainer valueContainer);
				void DisposeAndInvoke(Promise feed);
                void Dispose();
			}
			public interface IDelegate<TResult>
			{
				bool DisposeAndTryInvoke(IValueContainer valueContainer, out TResult result);
				TResult DisposeAndInvoke(Promise feed);
                void Dispose();
			}
			public interface IFilter : IDisposable
			{
				bool RunThroughFilter(IValueContainer valueContainer);
			}

			public class DelegateVoid : IDelegate, ILinked<DelegateVoid>
			{
				DelegateVoid ILinked<DelegateVoid>.Next { get; set; }

				private Action _callback;

				protected static ValueLinkedStack<DelegateVoid> _pool;

				public static DelegateVoid GetOrCreate(Action callback)
				{
					var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoid();
					del._callback = callback;
					return del;
				}

				static DelegateVoid()
				{
					OnClearPool += () => _pool.Clear();
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
					_pool.Push(this);
				}

				public bool DisposeAndTryInvoke(IValueContainer valueContainer)
				{
					Invoke();
					return true;
				}

				public void DisposeAndInvoke(Promise feed)
				{
					Invoke();
				}
			}

			public sealed class DelegateArg<TArg> : IDelegate, ILinked<DelegateArg<TArg>>
			{
                DelegateArg<TArg> ILinked<DelegateArg<TArg>>.Next { get; set; }

                private Action<TArg> _callback;

#pragma warning disable RECS0108 // Warns about static fields in generic types
				private static ValueLinkedStack<DelegateArg<TArg>> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

				public static DelegateArg<TArg> GetOrCreate(Action<TArg> callback)
				{
					var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArg<TArg>();
					del._callback = callback;
					return del;
				}

				static DelegateArg()
				{
					OnClearPool += () => _pool.Clear();
				}

				private DelegateArg() { }

				public void DisposeAndInvoke(TArg arg)
				{
					var temp = _callback;
					Dispose();
					temp.Invoke(arg);
				}

				public void Dispose()
				{
					_callback = null;
					_pool.Push(this);
				}

				public bool DisposeAndTryInvoke(IValueContainer valueContainer)
				{
					TArg arg;
					if (valueContainer.TryGetValueAs(out arg))
					{
                        DisposeAndInvoke(arg);
                        return true;
                    }
                    Dispose();
                    return false;
				}

				public void DisposeAndInvoke(Promise feed)
				{
					DisposeAndInvoke(feed.GetValue<TArg>());
				}
			}

			public sealed class DelegateVoid<TResult> : IDelegate<TResult>, ILinked<DelegateVoid<TResult>>
			{
				DelegateVoid<TResult> ILinked<DelegateVoid<TResult>>.Next { get; set; }

				private Func<TResult> _callback;

#pragma warning disable RECS0108 // Warns about static fields in generic types
				private static ValueLinkedStack<DelegateVoid<TResult>> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

				public static DelegateVoid<TResult> GetOrCreate(Func<TResult> callback)
				{
					var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoid<TResult>();
					del._callback = callback;
					return del;
				}

				static DelegateVoid()
				{
					OnClearPool += () => _pool.Clear();
				}

				private DelegateVoid() { }

				public TResult DisposeAndInvoke()
				{
					var temp = _callback;
					Dispose();
					return temp.Invoke();
				}

				public void Dispose()
				{
					_callback = null;
					_pool.Push(this);
				}

				public bool DisposeAndTryInvoke(IValueContainer valueContainer, out TResult result)
				{
					result = DisposeAndInvoke();
					return true;
				}

				public TResult DisposeAndInvoke(Promise feed)
				{
					return DisposeAndInvoke();
				}
			}

			public sealed class DelegateArg<TArg, TResult> : IDelegate<TResult>, ILinked<DelegateArg<TArg, TResult>>
			{
				DelegateArg<TArg, TResult> ILinked<DelegateArg<TArg, TResult>>.Next { get; set; }

				private Func<TArg, TResult> _callback;

#pragma warning disable RECS0108 // Warns about static fields in generic types
				private static ValueLinkedStack<DelegateArg<TArg, TResult>> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

				public static DelegateArg<TArg, TResult> GetOrCreate(Func<TArg, TResult> callback)
				{
					var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArg<TArg, TResult>();
					del._callback = callback;
					return del;
				}

				static DelegateArg()
				{
					OnClearPool += () => _pool.Clear();
				}

				private DelegateArg() { }

				public TResult DisposeAndInvoke(TArg arg)
				{
					var temp = _callback;
					Dispose();
					return temp.Invoke(arg);
				}

				public void Dispose()
				{
					_callback = null;
					_pool.Push(this);
				}

				public bool DisposeAndTryInvoke(IValueContainer valueContainer, out TResult result)
				{
					TArg arg;
					if (valueContainer.TryGetValueAs(out arg))
                    {
                        result = DisposeAndInvoke(arg);
                        return true;
                    }
                    Dispose();
                    result = default(TResult);
                    return false;
				}

				public TResult DisposeAndInvoke(Promise feed)
				{
					return DisposeAndInvoke(feed.GetValue<TArg>());
				}
			}

//			public sealed class Filter : IFilter, ILinked<Filter>
//			{
//				Filter ILinked<Filter>.Next { get; set; }

//				private Func<bool> _callback;

//				private static ValueLinkedStack<Filter> pool;

//				public static Filter GetOrCreate(Func<bool> callback)
//				{
//					var del = pool.IsNotEmpty ? pool.Pop() : new Filter();
//					del._callback = callback;
//					return del;
//				}

//				static Filter()
//				{
//					OnClearPool += () => pool.Clear();
//				}

//				private Filter() { }

//				public bool RunThroughFilter(IValueContainer valueContainer)
//				{
//					try
//					{
//						var temp = _callback;
//						_callback = null;
//						return temp.Invoke();
//					}
//					catch (Exception e)
//					{
//						Logger.LogWarning("Caught an exception in a promise onRejectedFilter. Assuming filter returned false. Logging exception next...");
//						Logger.LogException(e);
//						return false;
//					}
//				}

//				public void Dispose()
//				{
//					pool.Push(this);
//				}
//			}

//			public sealed class Filter<TArg> : IFilter, ILinked<Filter<TArg>>
//			{
//				Filter<TArg> ILinked<Filter<TArg>>.Next { get; set; }

//				private Func<TArg, bool> _callback;

//#pragma warning disable RECS0108 // Warns about static fields in generic types
//				private static ValueLinkedStack<Filter<TArg>> pool;
//#pragma warning restore RECS0108 // Warns about static fields in generic types

			//	public static Filter<TArg> GetOrCreate(Func<TArg, bool> callback)
			//	{
			//		var del = pool.IsNotEmpty ? pool.Pop() : new Filter<TArg>();
			//		del._callback = callback;
			//		return del;
			//	}

			//	static Filter()
			//	{
			//		OnClearPool += () => pool.Clear();
			//	}

			//	private Filter() { }

			//	public bool RunThroughFilter(IValueContainer valueContainer)
			//	{
			//		TArg arg;
			//		if (!valueContainer.TryGetValueAs(out arg))
			//		{
			//			return false;
			//		}

			//		try
			//		{
			//			var temp = _callback;
			//			_callback = null;
			//			return temp.Invoke(arg);
			//		}
			//		catch (Exception e)
			//		{
			//			Logger.LogWarning("Caught an exception in a promise onRejectedFilter. Assuming filter returned false. Logging exception next...");
			//			Logger.LogException(e);
			//			return false;
			//		}
			//	}

			//	public void Dispose()
			//	{
			//		pool.Push(this);
			//	}
			//}


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

                public bool IsRetained { get { return false; } }
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

				public bool TryGetValueAs<U>(out U value)
				{
					// This avoids boxing value types.
					var casted = this as UnhandledException<U>;
					if (casted != null)
					{
						value = casted.Value;
						return true;
					}
					if (typeof(U).IsAssignableFrom(typeof(T)) || Value is U)
					{
						value = (U) (object) Value;
						return true;
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

                public bool IsRetained { get { return retainCounter > 0; } }
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

                public bool IsRetained { get { return false; } }
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

                public bool IsRetained { get { return false; } }
            }

            public sealed class CancelValue<T> : IValueContainer, ILinked<CancelValue<T>>
            {
                CancelValue<T> ILinked<CancelValue<T>>.Next { get; set; }

                public T Value { get; private set; }

#pragma warning disable RECS0108 // Warns about static fields in generic types
                private static ValueLinkedStack<CancelValue<T>> pool = new ValueLinkedStack<CancelValue<T>>();
#pragma warning restore RECS0108 // Warns about static fields in generic types

                private uint retainCounter;

                static CancelValue()
                {
                    OnClearPool += () => pool.Clear();
                }

                private CancelValue() { }

                public static CancelValue<T> GetOrCreate(T value)
                {
                    CancelValue<T> ex = pool.IsNotEmpty ? pool.Pop() : new CancelValue<T>();
                    ex.Value = value;
                    return ex;
                }

                public bool TryGetValueAs<U>(out U value)
                {
                    // This avoids boxing value types.
                    var casted = this as CancelValue<U>;
                    if (casted != null)
                    {
                        value = casted.Value;
                        return true;
                    }
                    if (typeof(U).IsAssignableFrom(typeof(T)) || Value is U)
                    {
                        value = (U) (object) Value;
                        return true;
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

                public bool IsRetained { get { return retainCounter > 0; } }
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

			internal void SetStackTrace(string stackTrace)
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

    public struct Enumerator<T> : IEnumerator<T> where T : class, ILinked<T>
    {
        private T _current;

        public Enumerator(T first)
        {
            _current = first;
        }

        public bool MoveNext()
        {
            return _current != null;
        }

        public T Current
        {
            get
            {
                T temp = _current;
                _current = _current.Next;
                return temp;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        void IEnumerator.Reset() { }

        void IDisposable.Dispose() { }
    }

    /// <summary>
    /// This structure is unsuitable for general purpose.
    /// </summary>
    public struct ValueLinkedStack<T> : IEnumerable<T> where T : class, ILinked<T>
	{
		T _first;

		public bool IsEmpty { get { return _first == null; } }
		public bool IsNotEmpty { get { return _first != null; } }

		public void Clear()
		{
			_first = null;
		}

		public void Push(T item)
		{
			item.Next = _first;
			_first = item;
		}

		public T Pop()
		{
			T temp = _first;
			_first = _first.Next;
			return temp;
		}

		public T Peek()
		{
			return _first;
        }

        public Enumerator<T> GetEnumerator()
        {
            return new Enumerator<T>(_first);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// This structure is unsuitable for general purpose.
    /// </summary>
    public struct ValueLinkedQueue<T> : IEnumerable<T> where T : class, ILinked<T>
	{
		T _first;
		T _last;

		public bool IsEmpty { get { return _first == null; } }
		public bool IsNotEmpty { get { return _first != null; } }

		public ValueLinkedQueue(T item)
		{
			item.Next = null;
			_first = _last = item;
		}

		public void Clear()
		{
            _first = null; 
            _last = null;
		}

		public void AddLast(T item)
		{
			item.Next = null;
			if (_first == null)
			{
				_first = _last = item;
			}
			else
			{
				_last.Next = item;
				_last = item;
			}
		}

		public void AddFirst(T item)
		{
			item.Next = _first;
			_first = item;
		}

        // Note: this doesn't clear _last when the last item is taken.
        public T TakeFirst()
        {
			T temp = _first;
			_first = _first.Next;
			return temp;
		}

		public T PeekFirst()
		{
			return _first;
		}

		public T PeekLast()
		{
			return _last;
		}

		public void Append(ValueLinkedQueue<T> other)
		{
            if (other.IsEmpty)
            {
                return;
            }
            if (IsEmpty)
            {
                _first = other._first;
                _last = other._last;
            }
            else
            {
                _last.Next = other._first;
                _last = other._last;
            }
		}

        public Enumerator<T> GetEnumerator()
        {
            return new Enumerator<T>(_first);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

	public struct ValueLinkedStackZeroGC<T> : IEnumerable<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            Enumerator<Node> enumerator;

            public Enumerator (ValueLinkedStackZeroGC<T> stack)
            {
                enumerator = new Enumerator<Node>(stack._stack.Peek());
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public T Current
            {
                get
                {
                    return enumerator.Current.item;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset() { }

            void IDisposable.Dispose() { }
        }

        private class Node : ILinked<Node>
		{
#pragma warning disable RECS0108 // Warns about static fields in generic types
			private static ValueLinkedStack<Node> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

			public static void ClearPool()
			{
				_pool.Clear();
			}

			public Node Next { get; set; }

			public T item;

			public static Node GetOrCreate(T item)
			{
				Node node = _pool.IsNotEmpty ? _pool.Pop() : new Node();
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
				_pool.Push(this);
			}
		}

		public static void ClearPooledNodes()
		{
			Node.ClearPool();
		}

		private ValueLinkedStack<Node> _stack;

		public bool IsEmpty { get { return _stack.IsEmpty; } }
		public bool IsNotEmpty { get { return _stack.IsNotEmpty; } }

		public void Clear()
		{
			while (_stack.IsNotEmpty)
			{
				_stack.Pop().Dispose();
			}
		}

		public void ClearAndDontRepool()
		{
			_stack.Clear();
		}

		public void Push(T item)
		{
			_stack.Push(Node.GetOrCreate(item));
		}

		public T Pop()
		{
			return _stack.Pop().TakeItemAndDispose();
		}

		public T Peek()
		{
			return _stack.Peek().item;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}