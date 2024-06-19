#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class UnhandledExceptionInternal : UnhandledException, IRejectContainer, IRejectionToContainer, ICantHandleException
        {
            internal UnhandledExceptionInternal(object value, string message, string stackTrace, Exception innerException)
                : base(value, message, stackTrace, innerException)
            { }

            void ICantHandleException.ReportUnhandled(ITraceable traceable)
                => ReportUnhandledException(this);

            void IRejectContainer.ReportUnhandled()
                => ReportUnhandledException(this);

            ExceptionDispatchInfo IRejectContainer.GetExceptionDispatchInfo()
                => ExceptionDispatchInfo.Capture(Value as Exception ?? this);

            IRejectContainer IRejectionToContainer.ToContainer(ITraceable traceable)
                => this;

            Exception IRejectContainer.GetValueAsException()
                => Value as Exception ?? this;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class CanceledExceptionInternal : CanceledException
        {
            // Old Unity runtime has a bug where stack traces are continually appended to the exception, causing a memory leak and runtime slowdowns.
            // To avoid the issue, we only use a singleton in runtimes where the bug is not present.
#if PROMISE_DEBUG || NETSTANDARD2_0 || (UNITY_2018_3_OR_NEWER && !UNITY_2021_2_OR_NEWER)
            // Don't re-use instance in DEBUG mode so users can read its stacktrace on any thread.
            internal static CanceledExceptionInternal GetOrCreate()
                => new CanceledExceptionInternal("Operation was canceled.");
#else
            private static readonly CanceledExceptionInternal s_instance = new CanceledExceptionInternal("Operation was canceled.");
            
            internal static CanceledExceptionInternal GetOrCreate() => s_instance;
#endif


            private CanceledExceptionInternal(string message) : base(message) { }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class RejectionException : Exception
        {
            private readonly string _stackTrace;

            internal RejectionException(string message, string stackTrace, Exception innerException) : base(message, innerException)
            {
                _stackTrace = stackTrace;
            }

            public override string StackTrace => _stackTrace;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class RejectExceptionInternal : RejectException, IRejectionToContainer, ICantHandleException
        {
            private readonly object _value;

            internal RejectExceptionInternal(object value)
            {
                _value = value;
            }

            IRejectContainer IRejectionToContainer.ToContainer(ITraceable traceable)
                => CreateRejectContainer(_value, int.MinValue, this, traceable);

            void ICantHandleException.ReportUnhandled(ITraceable traceable)
                => ReportRejection(_value, traceable);
        }
    }
}