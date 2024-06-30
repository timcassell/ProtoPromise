#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0054 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Proto.Promises
{
    partial class Internal
    {
        // Extension method instead of including on the interface, since old IL2CPP compiler does not support virtual generics with structs.
        internal static bool TryGetValue<TValue>(this IRejectContainer rejectContainer, out TValue converted)
        {
            // These checks are optimized away by the JIT.
            // Null check is necessary for older runtimes to prevent the type check when TValue is a reference type.
            if (null != default(TValue) && typeof(TValue) == typeof(VoidResult))
            {
                converted = default;
                return true;
            }

            if (rejectContainer.Value is TValue value)
            {
                converted = value;
                return true;
            }
            converted = default;
            return false;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class RejectContainer : IRejectContainer, ITraceable
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            public abstract object Value { get; }
            public abstract Exception GetValueAsException();
            public abstract void ReportUnhandled();
            public abstract ExceptionDispatchInfo GetExceptionDispatchInfo();

            internal static IRejectContainer Create(object reason, int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable)
            {
                if (reason is IRejectionToContainer internalRejection)
                {
                    // reason is an internal rejection object, get its container instead of wrapping it.
                    return internalRejection.ToContainer(traceable);
                }

                // If reason is null, behave the same way .Net behaves if you throw null.
                reason = reason ?? new NullReferenceException();
                return reason is Exception e
                    ? RejectionContainerException.Create(e, rejectSkipFrames + 1, exceptionWithStacktrace, traceable)
                    : (IRejectContainer) RejectionContainer.Create(reason, rejectSkipFrames + 1, exceptionWithStacktrace, traceable);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class RejectionContainer : RejectContainer, IRejectionToContainer, ICantHandleException
        {
            private object _value;
            public override object Value => _value;

            partial void SetCreatedAndRejectedStacktrace(int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable);
#if PROMISE_DEBUG
            private StackTrace _rejectedStackTrace;
            // Stack traces of recursive callbacks.
            private CausalityTrace _stackTraces;

            partial void SetCreatedAndRejectedStacktrace(int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable)
            {
                _stackTraces = traceable.Trace;
                _rejectedStackTrace = exceptionWithStacktrace != null ? new StackTrace(exceptionWithStacktrace, true)
                    : rejectSkipFrames > 0 & Promise.Config.DebugCausalityTracer != Promise.TraceLevel.None ? GetStackTrace(rejectSkipFrames + 1)
                    : null;
            }
#endif

            private RejectionContainer() { }

            public override Exception GetValueAsException()
                => ToException();

            new internal static RejectionContainer Create(object value, int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable)
            {
                var container = new RejectionContainer
                {
                    _value = value
                };
                SetCreatedStacktrace(container, 2);
                container.SetCreatedAndRejectedStacktrace(rejectSkipFrames + 1, exceptionWithStacktrace, traceable);
                return container;
            }

            public override void ReportUnhandled()
                => ReportUnhandledException(ToException());

            private UnhandledException ToException()
            {
#if PROMISE_DEBUG
                string innerStacktrace = _rejectedStackTrace == null ? null : FormatStackTrace(new StackTrace[1] { _rejectedStackTrace });
                string outerStacktrace = _stackTraces.ToString();
#else
                string innerStacktrace = null;
                string outerStacktrace = null;
#endif
                string message = "A rejected value was not handled, type: " + Value.GetType() + ", Value: " + Value.ToString();
                Exception innerException = new RejectionException(message, innerStacktrace, null);
                return new UnhandledExceptionInternal(Value, message + CausalityTraceMessage, outerStacktrace, innerException);
            }

            public override ExceptionDispatchInfo GetExceptionDispatchInfo()
                => ExceptionDispatchInfo.Capture(ToException());

            IRejectContainer IRejectionToContainer.ToContainer(ITraceable traceable)
                => this;

            void ICantHandleException.ReportUnhandled(ITraceable traceable)
                => ReportUnhandledException(ToException());
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class RejectionContainerException : RejectContainer, IRejectionToContainer, ICantHandleException
        {
#if PROMISE_DEBUG
            // This is used to reconstruct the rejection causality trace when the original exception is rethrown from await in an async function, and this container is lost.
            private static readonly ConditionalWeakTable<object, RejectionException> s_rejectExceptionsForTrace = new ConditionalWeakTable<object, RejectionException>();
#endif

            ExceptionDispatchInfo _capturedInfo;
            partial void SetCreatedAndRejectedStacktrace(int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable);
#if PROMISE_DEBUG
            private RejectionException _rejectException;
            // Stack traces of recursive callbacks.
            private CausalityTrace _stackTraces;

            partial void SetCreatedAndRejectedStacktrace(int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable)
            {
                _stackTraces = traceable.Trace;
                StackTrace rejectedStacktrace = exceptionWithStacktrace != null ? new StackTrace(exceptionWithStacktrace, true)
                    : rejectSkipFrames > 0 & Promise.Config.DebugCausalityTracer != Promise.TraceLevel.None ? GetStackTrace(rejectSkipFrames + 1)
                    : null;
                if (rejectedStacktrace != null)
                {
                    const string message = "This exception contains the stacktrace of where Deferred.Reject was called, or Promise.RejectException() was thrown.";
                    lock (s_rejectExceptionsForTrace)
                    {
                        if (!s_rejectExceptionsForTrace.TryGetValue(Value, out _rejectException))
                        {
                            _rejectException = new RejectionException(message, FormatStackTrace(new StackTrace[1] { rejectedStacktrace }), (Exception) Value);
                            s_rejectExceptionsForTrace.Add(Value, _rejectException);
                        }
                    }
                }
                else
                {
                    s_rejectExceptionsForTrace.TryGetValue(Value, out _rejectException);
                }
            }
#endif

            public override object Value => _capturedInfo.SourceException;

            private RejectionContainerException() { }

            internal static RejectionContainerException Create(Exception value, int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable)
            {
                var container = new RejectionContainerException
                {
                    _capturedInfo = ExceptionDispatchInfo.Capture(value)
                };
                SetCreatedStacktrace(container, 2);
                container.SetCreatedAndRejectedStacktrace(rejectSkipFrames + 1, exceptionWithStacktrace, traceable);
                return container;
            }

            public override void ReportUnhandled()
                => ReportUnhandledException(ToException());

            private UnhandledException ToException()
            {
#if PROMISE_DEBUG
                return new UnhandledExceptionInternal(Value, "An exception was not handled." + CausalityTraceMessage, _stackTraces.ToString(), _rejectException ?? _capturedInfo.SourceException);
#else
                return new UnhandledExceptionInternal(Value, "An exception was not handled." + CausalityTraceMessage, null, _capturedInfo.SourceException);
#endif
            }

            public override ExceptionDispatchInfo GetExceptionDispatchInfo()
                => _capturedInfo;

            IRejectContainer IRejectionToContainer.ToContainer(ITraceable traceable)
                => this;

            void ICantHandleException.ReportUnhandled(ITraceable traceable)
                => ReportUnhandledException(ToException());

            public override Exception GetValueAsException()
                => _capturedInfo.SourceException;
        }
    }
}