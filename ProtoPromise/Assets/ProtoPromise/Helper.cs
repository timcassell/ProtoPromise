#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

#if !UNITY_5_3_OR_NEWER
namespace UnityEngine
{
    /// <summary>
    /// Custom yield instruction. Use yield return StartCoroutine(customYieldInstruction)
    /// </summary>
    public abstract class CustomYieldInstruction : IEnumerator
    {
        public abstract bool keepWaiting { get; }

        public object Current { get { return null; } }

        public bool MoveNext()
        {
            return keepWaiting;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
#endif

#if !CSHARP_7_OR_LATER
namespace System
{
    /// <summary>Represents one or more errors that occur during application execution.</summary>
    /// <remarks>
    /// <see cref="AggregateException"/> is used to consolidate multiple failures into a single, throwable
    /// exception object.
    /// </remarks>
    [Serializable]
    [DebuggerDisplay("Count = {InnerExceptionCount}")]
    public class AggregateException : Exception
    {
        private ReadOnlyCollection<Exception> m_innerExceptions; // Complete set of exceptions.

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with
        /// references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(IEnumerable<Exception> innerExceptions) :
            this(null, innerExceptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateException"/> class with a specified error
        /// message and references to the inner exceptions that are the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        public AggregateException(string message, IEnumerable<Exception> innerExceptions)
            // If it's already an IList, pass that along (a defensive copy will be made in the delegated ctor).  If it's null, just pass along
            // null typed correctly.  Otherwise, create an IList from the enumerable and pass that along. 
            : this(message, innerExceptions as IList<Exception> ?? (innerExceptions == null ? (List<Exception>) null : new List<Exception>(innerExceptions)))
        {
        }

        /// <summary>
        /// Allocates a new aggregate exception with the specified message and list of inner exceptions.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerExceptions">The exceptions that are the cause of the current exception.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="innerExceptions"/> argument
        /// is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element of <paramref name="innerExceptions"/> is
        /// null.</exception>
        private AggregateException(string message, IList<Exception> innerExceptions)
            : base(message, innerExceptions != null && innerExceptions.Count > 0 ? innerExceptions[0] : null)
        {
            if (innerExceptions == null)
            {
                throw new ArgumentNullException("innerExceptions");
            }

            // Copy exceptions to our internal array and validate them. We must copy them,
            // because we're going to put them into a ReadOnlyCollection which simply reuses
            // the list passed in to it. We don't want callers subsequently mutating.
            Exception[] exceptionsCopy = new Exception[innerExceptions.Count];

            for (int i = 0; i < exceptionsCopy.Length; i++)
            {
                exceptionsCopy[i] = innerExceptions[i];

                if (exceptionsCopy[i] == null)
                {
                    throw new ArgumentException("An InnerException is Null");
                }
            }

            m_innerExceptions = new ReadOnlyCollection<Exception>(exceptionsCopy);
        }

        /// <summary>
        /// Returns the <see cref="System.AggregateException"/> that is the root cause of this exception.
        /// </summary>
        public override Exception GetBaseException()
        {
            // Returns the first inner AggregateException that contains more or less than one inner exception

            // Recursively traverse the inner exceptions as long as the inner exception of type AggregateException and has only one inner exception
            Exception back = this;
            AggregateException backAsAggregate = this;
            while (backAsAggregate != null && backAsAggregate.InnerExceptions.Count == 1)
            {
                back = back.InnerException;
                backAsAggregate = back as AggregateException;
            }
            return back;
        }

        /// <summary>
        /// Gets a read-only collection of the <see cref="T:System.Exception"/> instances that caused the
        /// current exception.
        /// </summary>
        public ReadOnlyCollection<Exception> InnerExceptions
        {
            get { return m_innerExceptions; }
        }


        /// <summary>
        /// Invokes a handler on each <see cref="T:System.Exception"/> contained by this <see cref="AggregateException"/>.
        /// </summary>
        /// <param name="predicate">The predicate to execute for each exception. The predicate accepts as an
        /// argument the <see cref="T:System.Exception"/> to be processed and returns a Boolean to indicate
        /// whether the exception was handled.</param>
        /// <remarks>
        /// Each invocation of the <paramref name="predicate"/> returns true or false to indicate whether the
        /// <see cref="T:System.Exception"/> was handled. After all invocations, if any exceptions went
        /// unhandled, all unhandled exceptions will be put into a new <see cref="AggregateException"/>
        /// which will be thrown. Otherwise, the <see cref="Handle"/> method simply returns. If any
        /// invocations of the <paramref name="predicate"/> throws an exception, it will halt the processing
        /// of any more exceptions and immediately propagate the thrown exception as-is.
        /// </remarks>
        /// <exception cref="AggregateException">An exception contained by this <see cref="AggregateException"/> was not handled.</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="predicate"/> argument is
        /// null.</exception>
        public void Handle(Func<Exception, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            List<Exception> unhandledExceptions = null;
            for (int i = 0; i < m_innerExceptions.Count; i++)
            {
                // If the exception was not handled, lazily allocate a list of unhandled
                // exceptions (to be rethrown later) and add it.
                if (!predicate(m_innerExceptions[i]))
                {
                    if (unhandledExceptions == null)
                    {
                        unhandledExceptions = new List<Exception>();
                    }

                    unhandledExceptions.Add(m_innerExceptions[i]);
                }
            }

            // If there are unhandled exceptions remaining, throw them.
            if (unhandledExceptions != null)
            {
                throw new AggregateException(Message, unhandledExceptions);
            }
        }


        /// <summary>
        /// Flattens an <see cref="AggregateException"/> instances into a single, new instance.
        /// </summary>
        /// <returns>A new, flattened <see cref="AggregateException"/>.</returns>
        /// <remarks>
        /// If any inner exceptions are themselves instances of
        /// <see cref="AggregateException"/>, this method will recursively flatten all of them. The
        /// inner exceptions returned in the new <see cref="AggregateException"/>
        /// will be the union of all of the the inner exceptions from exception tree rooted at the provided
        /// <see cref="AggregateException"/> instance.
        /// </remarks>
        public AggregateException Flatten()
        {
            // Initialize a collection to contain the flattened exceptions.
            List<Exception> flattenedExceptions = new List<Exception>();

            // Create a list to remember all aggregates to be flattened, this will be accessed like a FIFO queue
#pragma warning disable IDE0028 // Simplify collection initialization
            List<AggregateException> exceptionsToFlatten = new List<AggregateException>();
#pragma warning restore IDE0028 // Simplify collection initialization
            exceptionsToFlatten.Add(this);
            int nDequeueIndex = 0;

            // Continue removing and recursively flattening exceptions, until there are no more.
            while (exceptionsToFlatten.Count > nDequeueIndex)
            {
                // dequeue one from exceptionsToFlatten
                IList<Exception> currentInnerExceptions = exceptionsToFlatten[nDequeueIndex++].InnerExceptions;

                for (int i = 0; i < currentInnerExceptions.Count; i++)
                {
                    Exception currentInnerException = currentInnerExceptions[i];

                    if (currentInnerException == null)
                    {
                        continue;
                    }

                    AggregateException currentInnerAsAggregate = currentInnerException as AggregateException;

                    // If this exception is an aggregate, keep it around for later.  Otherwise,
                    // simply add it to the list of flattened exceptions to be returned.
                    if (currentInnerAsAggregate != null)
                    {
                        exceptionsToFlatten.Add(currentInnerAsAggregate);
                    }
                    else
                    {
                        flattenedExceptions.Add(currentInnerException);
                    }
                }
            }


            return new AggregateException(Message, flattenedExceptions);
        }

        /// <summary>
        /// Creates and returns a string representation of the current <see cref="AggregateException"/>.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString()
        {
            string text = base.ToString();

            for (int i = 0; i < m_innerExceptions.Count; i++)
            {
                text = String.Format(
                    CultureInfo.InvariantCulture,
                    "AggregateException",
                    text, Environment.NewLine, i, m_innerExceptions[i].ToString(), "<---", Environment.NewLine);
            }

            return text;
        }

        /// <summary>
        /// This helper property is used by the DebuggerDisplay.
        /// 
        /// Note that we don't want to remove this property and change the debugger display to {InnerExceptions.Count} 
        /// because DebuggerDisplay should be a single property access or parameterless method call, so that the debugger 
        /// can use a fast path without using the expression evaluator.
        /// 
        /// See http://msdn.microsoft.com/en-us/library/x810d419.aspx
        /// </summary>
        private int InnerExceptionCount
        {
            get
            {
                return InnerExceptions.Count;
            }
        }
    }

}
#endif

namespace Proto.Promises
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

    public interface IPotentialCancelation
    {
        IPotentialCancelation CatchCancelation(Action onCanceled);
        IPotentialCancelation CatchCancelation<TCancel>(Action<TCancel> onCanceled);
    }


    partial class Promise
	{
        public sealed class YieldInstruction : UnityEngine.CustomYieldInstruction, Internal.ITreeHandleable, IDisposable
        {
            Internal.ITreeHandleable ILinked<Internal.ITreeHandleable>.Next { get; set; }

            private static ValueLinkedStack<Internal.ITreeHandleable> _pool;
            
            static YieldInstruction()
            {
                Internal.OnClearPool += () => _pool.Clear();
            }

            public State State { get; private set; }

            public override bool keepWaiting
            {
                get
                {
                    return State == State.Pending;
                }
            }

            private YieldInstruction() { }

            public static YieldInstruction GetOrCreate()
            {
                return _pool.IsNotEmpty ? (YieldInstruction) _pool.Pop() : new YieldInstruction();
            }

            /// <summary>
            /// Adds this object back to the pool.
            /// Don't try to access it after disposing! Results are undefined.
            /// </summary>
            /// <remarks>Call <see cref="Dispose"/> when you are finished using the
            /// <see cref="T:ProtoPromise.Promise.YieldInstruction"/>. The <see cref="Dispose"/> method leaves the
            /// <see cref="T:ProtoPromise.Promise.YieldInstruction"/> in an unusable state. After calling
            /// <see cref="Dispose"/>, you must release all references to the
            /// <see cref="T:ProtoPromise.Promise.YieldInstruction"/> so the garbage collector can reclaim the memory
            /// that the <see cref="T:ProtoPromise.Promise.YieldInstruction"/> was occupying.</remarks>
            public void Dispose()
            {
                _pool.Push(this);
            }

            void Internal.ITreeHandleable.Cancel()
            {
#pragma warning disable CS0618 // Type or member is obsolete
                State = State.Canceled;
#pragma warning restore CS0618 // Type or member is obsolete
            }

            void Internal.ITreeHandleable.Handle(Promise feed)
            {
                State = feed._state;
            }

            void Internal.ITreeHandleable.AssignCancelValue(Internal.IValueContainer cancelValue) { }
            void Internal.ITreeHandleable.OnSubscribeToCanceled(Internal.IValueContainer cancelValue) { }
        }

        public class InvalidReturnException : InvalidOperationException
		{
			public InvalidReturnException(string message, string stackTrace = null, Exception innerException = null) : base(message, innerException)
			{
				_stackTrace = stackTrace;
			}

			private readonly string _stackTrace;
			public override string StackTrace { get { return _stackTrace; } }
		}

		public abstract class UnhandledException : Exception
        {
			protected UnhandledException() { }
			protected UnhandledException(Exception innerException) : base(null, innerException) { }

            public abstract object GetValue();

			protected string _stackTrace;
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

        /// <summary>
        /// Doesn't actually move next, just returns if Current is valid.
        /// This allows the function to be branch-less. Useful for foreach loops.
        /// </summary>
        public bool MoveNext()
        {
            return _current != null;
        }

        /// <summary>
        /// Actually moves next and returns current.
        /// </summary>
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

        public ValueLinkedStack(T item)
        {
            item.Next = null;
            _first = item;
        }

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
            temp.Next = null;
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

        public void Enqueue(T item)
		{
			if (_first == null)
			{
                item.Next = null;
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
        public void EnqueueRisky(T item)
        {
            item.Next = null;
            _last.Next = item;
            _last = item;
        }

        public void Push(T item)
        {
            if (_first == null)
            {
                item.Next = null;
                _first = _last = item;
            }
            else
            {
                item.Next = _first;
                _first = item;
            }
        }

        /// <summary>
        /// Only use this if you know the queue is not empty.
        /// </summary>
        public void PushRisky(T item)
        {
            item.Next = null;
            item.Next = _first;
            _first = item;
        }

        public T Dequeue()
        {
			T temp = _first;
			_first = _first.Next;
            temp.Next = null;
            if (_first == null)
            {
                _last = null;
            }
            return temp;
        }

        /// <summary>
        /// This doesn't clear _last when the last item is taken.
        /// Only use this if you know this has 2 or more items, or if you will call ClearLast when you know this is empty.
        /// </summary>
        public T DequeueRisky()
        {
            T temp = _first;
            _first = _first.Next;
            temp.Next = null;
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

    public sealed class ReusableValueContainer<T> : IDisposable, ILinked<ReusableValueContainer<T>>
    {
#pragma warning disable RECS0108 // Warns about static fields in generic types
        private static ValueLinkedStack<ReusableValueContainer<T>> _pool;
#pragma warning restore RECS0108 // Warns about static fields in generic types

        public static void ClearPool()
        {
            _pool.Clear();
        }

        ReusableValueContainer<T> ILinked<ReusableValueContainer<T>>.Next { get; set; }

        public T value;

        /// <summary>
        /// Returns a new reusable value container containing <paramref name="value"/>.
        /// It will try to take from the pool, otherwise it will create a new object.
        /// Call <see cref="Dispose"/> when you are finished with this to add it back to the pool.
        /// </summary>
        public static ReusableValueContainer<T> New(T value)
        {
            ReusableValueContainer<T> node = _pool.IsNotEmpty ? _pool.Pop() : new ReusableValueContainer<T>();
            node.value = value;
            return node;
        }

        /// <summary>
        /// Adds this object back to the pool.
        /// Don't try to access it after disposing! Results are undefined.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the
        /// <see cref="T:ProtoPromise.ReusableValueContainer`1"/>. The <see cref="Dispose"/> method leaves the
        /// <see cref="T:ProtoPromise.ReusableValueContainer`1"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the
        /// <see cref="T:ProtoPromise.ReusableValueContainer`1"/> so the garbage collector can reclaim the memory that
        /// the <see cref="T:ProtoPromise.ReusableValueContainer`1"/> was occupying.</remarks>
        public void Dispose()
        {
            value = default(T);
            _pool.Push(this);
        }
    }

    public struct ValueLinkedStackZeroGC<T> : IEnumerable<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            Enumerator<ReusableValueContainer<T>> enumerator;

            public Enumerator(ValueLinkedStackZeroGC<T> stack)
            {
                enumerator = stack._stack.GetEnumerator();
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public T Current
            {
                get
                {
                    return enumerator.Current.value;
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

        public static void ClearPooledNodes()
        {
            ReusableValueContainer<T>.ClearPool();
        }

        private ValueLinkedStack<ReusableValueContainer<T>> _stack;

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
            _stack.Push(ReusableValueContainer<T>.New(item));
        }

        public T Pop()
        {
            var node = _stack.Pop();
            T item = node.value;
            node.Dispose();
            return item;
        }

        public T Peek()
        {
            return _stack.Peek().value;
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

    public struct ValueLinkedQueueZeroGC<T> : IEnumerable<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            Enumerator<ReusableValueContainer<T>> enumerator;

            public Enumerator(ValueLinkedQueueZeroGC<T> queue)
            {
                enumerator = queue._queue.GetEnumerator();
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public T Current
            {
                get
                {
                    return enumerator.Current.value;
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

        public static void ClearPooledNodes()
        {
            ReusableValueContainer<T>.ClearPool();
        }

        private ValueLinkedQueue<ReusableValueContainer<T>> _queue;

        public bool IsEmpty { get { return _queue.IsEmpty; } }
        public bool IsNotEmpty { get { return _queue.IsNotEmpty; } }

        public void Clear()
        {
            while (_queue.IsNotEmpty)
            {
                _queue.DequeueRisky().Dispose();
            }
            _queue.ClearLast();
        }

        public void ClearLast()
        {
            _queue.ClearLast();
        }

        public void ClearAndDontRepool()
        {
            _queue.Clear();
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(ReusableValueContainer<T>.New(item));
        }

        /// <summary>
        /// Only use this if you know the queue is not empty.
        /// </summary>
        public void EnqueueRisky(T item)
        {
            _queue.EnqueueRisky(ReusableValueContainer<T>.New(item));
        }

        public void Push(T item)
        {
            _queue.Push(ReusableValueContainer<T>.New(item));
        }

        /// <summary>
        /// Only use this if you know the queue is not empty.
        /// </summary>
        public void PushRisky(T item)
        {
            _queue.PushRisky(ReusableValueContainer<T>.New(item));
        }

        public T Dequeue()
        {
            var node = _queue.Dequeue();
            T item = node.value;
            node.Dispose();
            return item;
        }

        /// <summary>
        /// This doesn't clear _last when the last item is taken.
        /// Only use this if you know this has 2 or more items, or if you will call ClearLast after a loop that takes all the items.
        /// </summary>
        public T DequeueRisky()
        {
            var node = _queue.DequeueRisky();
            T item = node.value;
            node.Dispose();
            return item;
        }

        public T Peek()
        {
            return _queue.Peek().value;
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

    public struct ArrayEnumerator<T> : IEnumerator<T>
    {
        private T[] collection;
        private int index;

        public ArrayEnumerator(T[] array)
        {
            index = -1;
            collection = array;
        }

        public T Current
        {
            get
            {
                return collection[index];
            }
        }

        object IEnumerator.Current { get { return Current; } }

        void IDisposable.Dispose() { }

        bool IEnumerator.MoveNext()
        {
            return ++index < collection.Length;
        }

        void IEnumerator.Reset()
        {
#pragma warning disable RECS0083 // Shows NotImplementedException throws in the quick task bar
            throw new NotImplementedException();
#pragma warning restore RECS0083 // Shows NotImplementedException throws in the quick task bar
        }
    }
}