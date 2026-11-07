#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System.Diagnostics;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class CanceledExceptionInternal : CanceledException
        {
            // Unity runtimes have a bug where stack traces are continually appended to the exception, causing a memory leak and runtime slowdowns.
            // To avoid the issue, we only use a singleton in runtimes where the bug is not present.
#if PROMISE_DEBUG || !NETCOREAPP || UNITY_2018_3_OR_NEWER
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
        internal sealed class RejectExceptionInternal : RejectException, IRejectionToContainer, ICantHandleException
#if PROMISE_DEBUG
            , ITraceable
#endif
        {
#if PROMISE_DEBUG
            // In DEBUG mode, causality traces are stored in fields (see ITraceable).
            Diagnostics.CausalityTrace ITraceable.Trace { get; set; }

#endif
            private readonly object _value;

            internal RejectExceptionInternal(object value)
            {
                _value = value;
                // This is usually created where it's thrown, so we record it to use as the rejection's causality trace.
                SetCreatedStacktrace(this);
            }

            // Use this exception's trace if it was captured, otherwise use the trace of the promise that caught it.
            private object Traceable(object traceable)
                => GetTrace(this) != null ? this : traceable;

            IRejectContainer IRejectionToContainer.ToContainer(object traceable)
                => CreateRejectContainer(_value, false, Traceable(traceable));

            void ICantHandleException.ReportUnhandled(object traceable)
                => ReportRejection(_value, Traceable(traceable));
        }
    }
}