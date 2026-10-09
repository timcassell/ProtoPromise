#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Proto.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

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
        public override string StackTrace => _stackTrace ?? base.StackTrace;
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
        public override string StackTrace => _stackTrace ?? base.StackTrace;
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
        public override string StackTrace => _stackTrace ?? base.StackTrace;
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
        public override string StackTrace => _stackTrace ?? base.StackTrace;
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
        public override string StackTrace => _stackTrace ?? base.StackTrace;
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
        public override string StackTrace => _stackTrace ?? base.StackTrace;
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
        public override string StackTrace => _stackTrace ?? base.StackTrace;
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
        public UnreleasedObjectException(string message, string stackTrace = null, Exception innerException = null) : base(message, innerException)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace => _stackTrace ?? base.StackTrace;
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
    public class UnhandledException : Exception, Internal.IRejectContainer, Internal.IRejectionToContainer, Internal.ICantHandleException
    {
        private readonly object _value;
        private readonly CausalityTrace _causalityTrace;

        internal UnhandledException(object value, string message, CausalityTrace causalityTrace, Exception innerException)
            : base(GetMessage(message, causalityTrace), innerException)
        {
            _value = value;
            _causalityTrace = causalityTrace;
        }

        private static string GetMessage(string message, CausalityTrace causalityTrace)
        {
            if (causalityTrace != null)
            {
                return $"{message} -- This exception's Stacktrace contains the causality trace of all async continuations.";
            }
            if (!CausalityTrace.IsEnabled)
            {
                return $"{message} -- Set Proto.Diagnostics.CausalityTrace.IsEnabled = true to get a causality trace.";
            }
            // Causality traces are enabled, but this rejection has no trace.
#if PROMISE_DEBUG
            return $"{message} -- No causality trace was captured. CausalityTrace.IsEnabled may have been set after the operation was started.";
#else
            return $"{message} -- No causality trace was found. Causality traces are not always available in RELEASE mode (for example, objects reported from finalizers on some runtimes). Enable DEBUG mode for reliable causality traces.";
#endif
        }


        public override string StackTrace => _causalityTrace?.ToString();

        public object Value => _value;

        public CausalityTrace CausalityTrace => _causalityTrace;

        void Internal.ICantHandleException.ReportUnhandled(object traceable)
            => Internal.ReportUnhandledException(this);

        void Internal.IRejectContainer.ReportUnhandled()
            => Internal.ReportUnhandledException(this);

        ExceptionDispatchInfo Internal.IRejectContainer.GetExceptionDispatchInfo()
            => ExceptionDispatchInfo.Capture(Value as Exception ?? this);

        Internal.IRejectContainer Internal.IRejectionToContainer.ToContainer(object traceable)
            => this;

        Exception Internal.IRejectContainer.GetValueAsException()
            => Value as Exception ?? this;
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
    }


    /// <summary>
    /// Special Exception that is used to rethrow a rejection from a Promise onRejected callback.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class RethrowException : Exception, Internal.IRejectionToContainer
    {
        // Unity runtimes have a bug where stack traces are continually appended to the exception, causing a memory leak and runtime slowdowns.
        // To avoid the issue, we only use a singleton in runtimes where the bug is not present.
#if PROMISE_DEBUG || !NETCOREAPP || UNITY_2018_3_OR_NEWER
        // Don't re-use instance in DEBUG mode so that we can read its stacktrace on any thread.
        internal static RethrowException GetOrCreate() => new RethrowException();
#else
        private static readonly RethrowException s_instance = new RethrowException();

        internal static RethrowException GetOrCreate() => s_instance;
#endif

        protected RethrowException() { }

        Internal.IRejectContainer Internal.IRejectionToContainer.ToContainer(object traceable)
        {
#if PROMISE_DEBUG
            string stacktrace = CausalityTrace.FormatStackTrace(new StackTrace(this, true));
#else
            string stacktrace = new StackTrace(this, true).ToString();
#endif
            Exception exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
            return Internal.ExceptionRejectionContainer.Create(exception, false, traceable);
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

        public override string Message => "This is used to reject a Promise from an onResolved or onRejected handler.";
    }
}