#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
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
		public class InvalidReturnException : InvalidOperationException
		{
			public InvalidReturnException(string message, string stackTrace = null, Exception innerException = null) : base(message, innerException)
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

			internal bool handled;

            public abstract object GetValue();

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

        public void ClearLast()
        {
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

        /// <summary>
        /// Only use this if you know the queue is not empty.
        /// </summary>
        public void AddLastRisky(T item)
        {
            item.Next = null;
            _last.Next = item;
            _last = item;
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

			Node ILinked<Node>.Next { get; set; }

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
#pragma warning restore IDE0018 // Inline variable declaration
#pragma warning restore IDE0034 // Simplify 'default' expression