#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Diagnostics;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class InvalidOperationException : System.InvalidOperationException
    {
        public InvalidOperationException(string message, string stackTrace = null) : base(message)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class InvalidArgumentException : ArgumentException
    {
        public InvalidArgumentException(string paramName, string message, string stackTrace = null) : base(message, paramName)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class EmptyArgumentException : ArgumentException
    {
        public EmptyArgumentException(string paramName, string message, string stackTrace = null) : base(message, paramName)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class ArgumentNullException : System.ArgumentNullException
    {
        public ArgumentNullException(string paramName, string message, string stackTrace = null) : base(paramName, message)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class ArgumentOutOfRangeException : System.ArgumentOutOfRangeException
    {
        public ArgumentOutOfRangeException(string paramName, string message, string stackTrace = null) : base(paramName, message)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class InvalidElementException : InvalidArgumentException
    {
        public InvalidElementException(string paramName, string message, string stackTrace = null) : base(paramName, message)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class InvalidReturnException : System.InvalidOperationException
    {
        public InvalidReturnException(string message, string stackTrace = null, Exception innerException = null) : base(message, innerException)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace { get { return _stackTrace ?? base.StackTrace; } }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class UnhandledDeferredException : Exception
    {
        public static readonly UnhandledDeferredException instance =
            new UnhandledDeferredException("A Deferred object was garbage collected that was not handled. You must Resolve, Reject, or Cancel all Deferred objects.");

        private UnhandledDeferredException(string message) : base(message) { }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class UnreleasedObjectException : Exception
    {
        public UnreleasedObjectException(string message) : base(message) { }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class UnobservedPromiseException : Exception
    {
        public UnobservedPromiseException(string message) : base(message) { }
    }


    /// <summary>
    /// Exception that is thrown if a promise is rejected and that rejection is never handled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public abstract class UnhandledException : Exception, Internal.ILinked<UnhandledException>
    {
        UnhandledException Internal.ILinked<UnhandledException>.Next { get; set; }

        private readonly object _value;
        private readonly string _stackTrace;

        internal UnhandledException(object value, string message, string stackTrace, Exception innerException) : base(message, innerException)
        {
            _value = value;
            _stackTrace = stackTrace;
        }

        public override string StackTrace { get { return _stackTrace; } }

        public Type ValueType { get { return _value.GetType(); } }

        public object Value { get { return _value; } }

        public bool TryGetValueAs<T>(out T value)
        {
            if (typeof(T).IsAssignableFrom(ValueType))
            {
                value = (T) _value;
                return true;
            }
            value = default(T);
            return false;
        }
    }

    /// <summary>
    /// Exception that is used to propagate cancelation of an operation.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public abstract class CanceledException : OperationCanceledException
    {
        internal CanceledException(string message) : base(message) { }


        [Obsolete("Cancelation reasons are no longer supported.", true)]
        public Type ValueType
        {
            get
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported.", Internal.GetFormattedStacktrace(1));
            }
        }

        [Obsolete("Cancelation reasons are no longer supported.", true)]
        public object Value
        {
            get
            {
                throw new InvalidOperationException("Cancelation reasons are no longer supported.", Internal.GetFormattedStacktrace(1));
            }
        }

        [Obsolete("Cancelation reasons are no longer supported.", true)]
        public bool TryGetValueAs<T>(out T value)
        {
            throw new InvalidOperationException("Cancelation reasons are no longer supported.", Internal.GetFormattedStacktrace(1));
        }
    }


    /// <summary>
    /// Special Exception that is used to rethrow a rejection from a Promise onRejected callback.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class RethrowException : Exception, Internal.IRejectionToContainer
    {
#if !PROMISE_DEBUG
        private static readonly RethrowException _instance = new RethrowException();
#endif

        protected RethrowException() { }

        internal static RethrowException GetOrCreate()
        {
#if PROMISE_DEBUG
            return new RethrowException(); // Don't re-use instance in DEBUG mode so that we can read its stacktrace on any thread.
#else
            return _instance;
#endif
        }

        Internal.ValueContainer Internal.IRejectionToContainer.ToContainer(Internal.ITraceable traceable)
        {
#if PROMISE_DEBUG
            string stacktrace = Internal.FormatStackTrace(new StackTrace[1] { new StackTrace(this, true) });
#else
            string stacktrace = new StackTrace(this, true).ToString();
#endif
            object exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
            return Internal.RejectionContainer<object>.GetOrCreate(exception);
        }
    }

    /// <summary>
    /// Special Exception that is used to reject a Promise from an onResolved or onRejected callback.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public abstract class RejectException : Exception
    {
        internal RejectException() { }

        public override string Message
        {
            get
            {
                return "This is used to reject a Promise from an onResolved or onRejected handler.";
            }
        }
    }
}