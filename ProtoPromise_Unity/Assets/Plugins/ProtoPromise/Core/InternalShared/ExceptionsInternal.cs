#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
            internal UnhandledExceptionInternal(object value, string message, string stackTrace, Exception innerException) :
                base(value, message, stackTrace, innerException)
            { }

            void ICantHandleException.ReportUnhandled(ITraceable traceable)
            {
                ReportUnhandledException(this);
            }

            void IRejectContainer.ReportUnhandled()
            {
                ReportUnhandledException(this);
            }

            ExceptionDispatchInfo IRejectContainer.GetExceptionDispatchInfo()
            {
                return ExceptionDispatchInfo.Capture(Value as Exception ?? this);
            }

            IRejectContainer IRejectionToContainer.ToContainer(ITraceable traceable)
            {
                return this;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class CanceledExceptionInternal : CanceledException
        {
#if !PROMISE_DEBUG
            private static readonly CanceledExceptionInternal s_instance = new CanceledExceptionInternal("Operation was canceled.");
#endif

            internal static CanceledExceptionInternal GetOrCreate()
            {
#if PROMISE_DEBUG
                return new CanceledExceptionInternal("Operation was canceled."); // Don't re-use instance in DEBUG mode so users can read its stacktrace on any thread.
#else
                return s_instance;
#endif
            }

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

            public override string StackTrace { get { return _stackTrace; } }
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
            {
                return CreateRejectContainer(_value, int.MinValue, this, traceable);
            }

            void ICantHandleException.ReportUnhandled(ITraceable traceable)
            {
                ReportRejection(_value, traceable);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class ForcedRethrowException : RethrowException
        {
#if !PROMISE_DEBUG
            private static readonly ForcedRethrowException s_instance = new ForcedRethrowException();
#endif

            private ForcedRethrowException() { }

            new internal static ForcedRethrowException GetOrCreate()
            {
#if PROMISE_DEBUG
                return new ForcedRethrowException(); // Don't re-use instance in DEBUG mode so that we can read its stacktrace on any thread.
#else
                return s_instance;
#endif
            }
        }
    }
}