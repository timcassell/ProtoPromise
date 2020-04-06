#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable RECS0108 // Warns about static fields in generic types

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace Proto.Promises
{
    public class InvalidOperationException : System.InvalidOperationException
    {
        public InvalidOperationException(string message, string stackTrace = null) : base(message)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

    public class EmptyArgumentException : ArgumentException
    {
        public EmptyArgumentException(string paramName, string message, string stackTrace = null) : base(message, paramName)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

    public class ArgumentNullException : System.ArgumentNullException
    {
        public ArgumentNullException(string paramName, string message, string stackTrace = null) : base(paramName, message)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

    public class ElementNullException : System.ArgumentNullException
    {
        public ElementNullException(string paramName, string message, string stackTrace = null) : base(paramName, message)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

    public class PromiseDisposedException : ObjectDisposedException
    {
        public PromiseDisposedException(string message, string stackTrace = null) : base(message, default(Exception))
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

    public class InvalidReturnException : System.InvalidOperationException
    {
        public InvalidReturnException(string message, string stackTrace = null, Exception innerException = null) : base(message, innerException)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

    public class UnhandledDeferredException : Exception
    {
        public static readonly UnhandledDeferredException instance =
            new UnhandledDeferredException("A Deferred object was garbage collected that was not handled. You must Resolve, Reject, or Cancel all Deferred objects.");

        private UnhandledDeferredException(string message) : base(message) { }
    }

    public class UnreleasedObjectException : Exception
    {
        public static readonly UnreleasedObjectException instance =
            new UnreleasedObjectException("An IRetainable object was garbage collected that was not released. You must release all IRetainable objects that you have retained.");

        private UnreleasedObjectException(string message) : base(message) { }
    }


    /// <summary>
    /// Exception that is thrown if a promise is rejected and that rejection is never handled.
    /// </summary>
    public abstract class UnhandledException : Exception
    {
        private readonly object _value;
        private readonly Type _type;
        private readonly string _stackTrace;

        protected UnhandledException(object value, Type valueType, string message, string stacktrace, Exception innerException) : base(message, innerException)
        {
            _value = value;
            _type = valueType;
            _stackTrace = stacktrace;
        }

        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }

        public Type ValueType { get { return _type; } }

        public object Value { get { return _value; } }

        public bool TryGetValueAs<T>(out T value)
        {
            if (typeof(T).IsAssignableFrom(_type))
            {
                value = (T) _value;
                return true;
            }
            value = default(T);
            return false;
        }
    }

    /// <summary>
    /// Exception that is thrown if an awaited promise is canceled.
    /// </summary>
    public abstract class CanceledException : OperationCanceledException
    {
        private readonly object _value;
        private readonly Type _type;

        protected CanceledException(object value, Type valueType, string message) : base(message)
        {
            _value = value;
            _type = valueType;
        }

        public Type ValueType { get { return _type; } }

        public object Value { get { return _value; } }

        public bool TryGetValueAs<T>(out T value)
        {
            if (typeof(T).IsAssignableFrom(_type))
            {
                value = (T) _value;
                return true;
            }
            value = default(T);
            return false;
        }
    }


    /// <summary>
    /// Special Exception that is used to rethrow a rejection from a Promise onRejected callback.
    /// </summary>
    public sealed class RethrowException : Exception
    {
        public static readonly RethrowException instance = new RethrowException();

        private RethrowException() { }
    }

    /// <summary>
    /// Special Exception that is used to reject a Promise from an onResolved or onRejected callback.
    /// </summary>
    public abstract class RejectException : Exception
    {
        protected RejectException() { }

        public override string Message
        {
            get
            {
                return "This is used to reject a Promise from an onResolved or onRejected handler.";
            }
        }
    }

    /// <summary>
    /// Special Exception that is used to cancel a Promise from an onResolved or onRejected callback.
    /// </summary>
    public abstract class CancelException : OperationCanceledException
    {
        protected CancelException() { }

        public override string Message
        {
            get
            {
                return "This is used to cancel a Promise from an onResolved or onRejected handler.";
            }
        }
    }


    partial class Promise
    {
        partial class Internal
        {
            public sealed class UnhandledExceptionInternal : UnhandledException, IValueContainer, IRejectionContainer, IThrowable
            {
                public UnhandledExceptionInternal(object value, Type valueType, string message, string stacktrace, Exception innerException) :
                    base(value, valueType, message, stacktrace, innerException)
                { }

                State IValueContainer.GetState()
                {
                    return State.Rejected;
                }

                void IValueContainer.Retain() { }
                void IValueContainer.Release() { }
                void IValueContainer.ReleaseAndMaybeAddToUnhandledStack()
                {
                    AddUnhandledException(this);
                }

                Exception IThrowable.GetException()
                {
                    return this;
                }

                void IRejectionContainer.SetCreatedAndRejectedStacktrace(StackTrace rejectedStacktrace, CausalityTrace createdStacktraces) { }
            }

            public sealed class CanceledExceptionInternal : CanceledException, IValueContainer, IThrowable
            {
                public CanceledExceptionInternal(object value, Type valueType, string message) :
                    base(value, valueType, message)
                { }

                State IValueContainer.GetState()
                {
                    return State.Canceled;
                }

                void IValueContainer.Retain() { }
                void IValueContainer.Release() { }
                void IValueContainer.ReleaseAndMaybeAddToUnhandledStack() { }

                Exception IThrowable.GetException()
                {
                    return this;
                }
            }

            public sealed class RejectionException : Exception
            {
                private readonly string _stackTrace;

                public RejectionException(string message, string stacktrace, Exception innerException) : base(message, innerException)
                {
                    _stackTrace = stacktrace;
                }

                public override string StackTrace { get { return _stackTrace; } }
            }

            public sealed class RejectExceptionInternal<T> : RejectException, IExceptionToContainer, ICantHandleException
            {
                // We can reuse the same object.
                private static readonly RejectExceptionInternal<T> _instance = new RejectExceptionInternal<T>();

                public T Value { get; private set; }

                public static RejectExceptionInternal<T> GetOrCreate(T value)
                {
                    _instance.Value = value;
                    return _instance;
                }

                private RejectExceptionInternal() { }

                public IValueContainer ToContainer(ITraceable traceable)
                {
                    var rejection = CreateRejection(Value);
#if PROMISE_DEBUG
                    rejection.SetCreatedAndRejectedStacktrace(new StackTrace(this, true), traceable.Trace);
#endif
                    return rejection;
                }

                public void AddToUnhandledStack(ITraceable traceable)
                {
                    AddRejectionToUnhandledStack(Value, traceable);
                }
            }

            public sealed class CancelExceptionVoidInternal : CancelException, IExceptionToContainer
            {
                // We can reuse the same object.
                private static readonly CancelExceptionVoidInternal _instance = new CancelExceptionVoidInternal();

                public static CancelExceptionVoidInternal GetOrCreate()
                {
                    return _instance;
                }

                private CancelExceptionVoidInternal() { }

                public IValueContainer ToContainer(ITraceable traceable)
                {
                    return CancelContainerVoid.GetOrCreate();
                }
            }

            public sealed class CancelExceptionInternal<T> : CancelException, IExceptionToContainer
            {
                // We can reuse the same object.
                private static readonly CancelExceptionInternal<T> _instance = new CancelExceptionInternal<T>();

                public T Value { get; private set; }

                public static CancelExceptionInternal<T> GetOrCreate(T value)
                {
                    _instance.Value = value;
                    return _instance;
                }

                private CancelExceptionInternal() { }

                public IValueContainer ToContainer(ITraceable traceable)
                {
#if CSHARP_7_OR_LATER
                    if (((object) Value) is CanceledExceptionInternal e)
#else
                    CanceledExceptionInternal e = Value as CanceledExceptionInternal;
                    if (e != null)
#endif
                    {
                        return e;
                    }
                    return CancelContainer<T>.GetOrCreate(Value);
                }
            }
        }
    }
}

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