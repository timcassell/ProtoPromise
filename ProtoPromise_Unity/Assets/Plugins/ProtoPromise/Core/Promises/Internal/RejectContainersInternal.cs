#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0017 // Simplify object initialization
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0038 // Use pattern matching
#pragma warning disable IDE0054 // Use compound assignment

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Proto.Promises
{
    internal static partial class Internal
    {
        // Extension method instead of including on the interface, since old IL2CPP compiler does not support virtual generics with structs.
        internal static bool TryGetValue<TValue>(this IRejectContainer rejectContainer, out TValue converted)
        {
            // null check is same as typeof(TValue).IsValueType, but is actually optimized away by the JIT. This prevents the type check when TValue is a reference type.
            if (null != default(TValue) && typeof(TValue) == typeof(VoidResult))
            {
                converted = default(TValue);
                return true;
            }

            object value = rejectContainer.Value;
            if (value is TValue)
            {
                converted = (TValue) value;
                return true;
            }
            converted = default(TValue);
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

            protected object _value;
            public object Value
            {
                get { return _value; }
            }

            public abstract void ReportUnhandled();
            public abstract ExceptionDispatchInfo GetExceptionDispatchInfo();

            internal static IRejectContainer Create(object reason, int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable)
            {
                IRejectionToContainer internalRejection = reason as IRejectionToContainer;
                if (internalRejection != null)
                {
                    // reason is an internal rejection object, get its container instead of wrapping it.
                    return internalRejection.ToContainer(traceable);
                }

                // If reason is null, behave the same way .Net behaves if you throw null.
                reason = reason ?? new NullReferenceException();
                Exception e = reason as Exception;
                return e != null
                    ? RejectionContainerException.Create(e, rejectSkipFrames + 1, exceptionWithStacktrace, traceable)
                    // Only need to create one object pool for reference types.
                    : (IRejectContainer) RejectionContainer.Create(reason, rejectSkipFrames + 1, exceptionWithStacktrace, traceable);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class RejectionContainer : RejectContainer, IRejectionToContainer, ICantHandleException
        {
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

            new internal static RejectionContainer Create(object value, int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable)
            {
                var container = new RejectionContainer();
                container._value = value;
                SetCreatedStacktrace(container, 2);
                container.SetCreatedAndRejectedStacktrace(rejectSkipFrames + 1, exceptionWithStacktrace, traceable);
                return container;
            }

            public override void ReportUnhandled()
            {
                ReportUnhandledException(ToException());
            }

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
            {
                return ExceptionDispatchInfo.Capture(ToException());
            }

            IRejectContainer IRejectionToContainer.ToContainer(ITraceable traceable)
            {
                return this;
            }

            void ICantHandleException.ReportUnhandled(ITraceable traceable)
            {
                ReportUnhandledException(ToException());
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class RejectionContainerException : RejectContainer, IRejectionToContainer, ICantHandleException
        {
#if PROMISE_DEBUG && !NET_LEGACY
            // This is used to reconstruct the rejection causality trace when the original exception is rethrown from await in an async function, and this container is lost.
            private static readonly ConditionalWeakTable<object, RejectionException> s_rejectExceptionsForTrace = new ConditionalWeakTable<object, RejectionException>();
#endif

#if !NET_LEGACY
            ExceptionDispatchInfo _capturedInfo;
#endif
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
#if !NET_LEGACY
                    lock (s_rejectExceptionsForTrace)
                    {
                        if (!s_rejectExceptionsForTrace.TryGetValue(Value, out _rejectException))
                        {
                            _rejectException = new RejectionException(message, FormatStackTrace(new StackTrace[1] { rejectedStacktrace }), (Exception) Value);
                            s_rejectExceptionsForTrace.Add(Value, _rejectException);
                        }
                    }
#else
                    _rejectException = new RejectionException(message, FormatStackTrace(new StackTrace[1] { rejectedStacktrace }), (Exception) Value);
#endif
                }
#if !NET_LEGACY
                else
                {
                    s_rejectExceptionsForTrace.TryGetValue(Value, out _rejectException);
                }
#endif
            }
#endif

            private RejectionContainerException() { }

            internal static RejectionContainerException Create(Exception value, int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable)
            {
                var container = new RejectionContainerException();
                container._value = value;
#if !NET_LEGACY
                container._capturedInfo = ExceptionDispatchInfo.Capture(value);
#endif
                SetCreatedStacktrace(container, 2);
                container.SetCreatedAndRejectedStacktrace(rejectSkipFrames + 1, exceptionWithStacktrace, traceable);
                return container;
            }

            public override void ReportUnhandled()
            {
                ReportUnhandledException(ToException());
            }

            private UnhandledException ToException()
            {
#if PROMISE_DEBUG
                return new UnhandledExceptionInternal(Value, "An exception was not handled." + CausalityTraceMessage, _stackTraces.ToString(), _rejectException ?? (Exception) Value);
#else
                return new UnhandledExceptionInternal(Value, "An exception was not handled." + CausalityTraceMessage, null, (Exception) Value);
#endif
            }

            public override ExceptionDispatchInfo GetExceptionDispatchInfo()
            {
#if NET_LEGACY
                // Old runtimes don't support preserving stacktrace, so we wrap it in UnhandledException.
                return ExceptionDispatchInfo.Capture(ToException());
#else
                return _capturedInfo;
#endif
            }

            IRejectContainer IRejectionToContainer.ToContainer(ITraceable traceable)
            {
                return this;
            }

            void ICantHandleException.ReportUnhandled(ITraceable traceable)
            {
                ReportUnhandledException(ToException());
            }
        }
    }
}