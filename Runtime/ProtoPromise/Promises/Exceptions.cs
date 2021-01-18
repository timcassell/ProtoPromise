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

        public ArgumentOutOfRangeException(string paramName, object actualValue, string message, string stackTrace = null) : base(paramName, actualValue, message)
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


    /// <summary>
    /// Exception that is thrown if a promise is rejected and that rejection is never handled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public abstract class UnhandledException : Exception
    {
        private readonly object _value;
        private readonly Type _type;
        private readonly string _stackTrace;

        internal UnhandledException(object value, Type valueType, string message, string stackTrace, Exception innerException) : base(message, innerException)
        {
            _value = value;
            _type = valueType;
            _stackTrace = stackTrace;
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
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public abstract class CanceledException : OperationCanceledException
    {
        private readonly object _value;
        private readonly Type _type;

        internal CanceledException(object value, Type valueType, string message) : base(message)
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
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class RethrowException : Exception
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

    /// <summary>
    /// Special Exception that is used to cancel a Promise from an onResolved or onRejected callback.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public abstract class CancelException : OperationCanceledException
    {
        internal CancelException() { }

        public override string Message
        {
            get
            {
                return "This is used to cancel a Promise from an onResolved or onRejected handler.";
            }
        }
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class ForcedRethrowException : RethrowException
        {
            private static readonly ForcedRethrowException _instance = new ForcedRethrowException();

            private ForcedRethrowException() { }

            new internal static ForcedRethrowException GetOrCreate()
            {
                return _instance;
            }
        }
    }
}