#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0038 // Use pattern matching
#pragma warning disable IDE0054 // Use compound assignment

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    internal static partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal abstract class RejectContainer : ITraceable
        {
            // This is used with Interlocked in Merge/Race/First to handle completion race conditions.
            // It's also used as a placeholder for cancelations.
            internal static readonly RejectContainer s_completionSentinel = new CompletionSentinel();

            private class CompletionSentinel : RejectContainer
            {
                internal override void AddToUnhandledStack() { }
            }

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            protected object _value;
            internal object Value
            {
                get { return _value; }
            }

            internal abstract void AddToUnhandledStack();

            internal static RejectContainer Create(object reason, int rejectSkipFrames, ITraceable traceable)
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
                RejectContainer valueContainer = e != null
                    ? RejectionContainerException.Create(e)
                    // Only need to create one object pool for reference types.
                    : (RejectContainer) RejectionContainer.Create(reason);
                SetCreatedAndRejectedStacktrace(valueContainer.UnsafeAs<IRejectValueContainer>(), rejectSkipFrames + 1, traceable);
                return valueContainer;
            }

            internal bool TryGetValue<TValue>(out TValue converted)
            {
                // null check is same as typeof(TValue).IsValueType, but is actually optimized away by the JIT. This prevents the type check when TValue is a reference type.
                if (null != default(TValue) && typeof(TValue) == typeof(VoidResult))
                {
                    converted = default(TValue);
                    return true;
                }

                if (Value is TValue)
                {
                    converted = (TValue) Value;
                    return true;
                }
                converted = default(TValue);
                return false;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class RejectionContainer : RejectContainer, IRejectValueContainer, IRejectionToContainer, ICantHandleException
        {
#if PROMISE_DEBUG
            private StackTrace _rejectedStackTrace;
            // Stack traces of recursive callbacks.
            private CausalityTrace _stackTraces;

            public void SetCreatedAndRejectedStacktrace(StackTrace rejectedStacktrace, CausalityTrace createdStacktraces)
            {
                ThrowIfInPool(this);
                _rejectedStackTrace = rejectedStacktrace;
                _stackTraces = createdStacktraces;
            }
#endif

            private RejectionContainer() { }

            internal static RejectionContainer Create(object value)
            {
                var container = new RejectionContainer();
                container._value = value;
                SetCreatedStacktrace(container, 2);
                return container;
            }

            internal override void AddToUnhandledStack()
            {
                AddUnhandledException(ToException());
            }

            private UnhandledException ToException()
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG
                string innerStacktrace = _rejectedStackTrace == null ? null : FormatStackTrace(new StackTrace[1] { _rejectedStackTrace });
#else
                string innerStacktrace = null;
#endif
                string message = "A rejected value was not handled, type: " + Value.GetType() + ", Value: " + Value.ToString();
                Exception innerException = new RejectionException(message, innerStacktrace, null);
#if PROMISE_DEBUG
                string outerStacktrace = _stackTraces.ToString();
#else
                string outerStacktrace = null;
#endif
                return new UnhandledExceptionInternal(Value, message + CausalityTraceMessage, outerStacktrace, innerException);
            }

#if !NET_LEGACY
            System.Runtime.ExceptionServices.ExceptionDispatchInfo IRejectValueContainer.GetExceptionDispatchInfo()
            {
                ThrowIfInPool(this);
                return System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ToException());
            }
#else
            Exception IRejectValueContainer.GetException()
            {
                ThrowIfInPool(this);
                return ToException();
            }
#endif

            RejectContainer IRejectionToContainer.ToContainer(ITraceable traceable)
            {
                ThrowIfInPool(this);
                return this;
            }

            void ICantHandleException.AddToUnhandledStack(ITraceable traceable)
            {
                ThrowIfInPool(this);
                AddUnhandledException(ToException());
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class RejectionContainerException : RejectContainer, IRejectValueContainer, IRejectionToContainer, ICantHandleException
        {
#if PROMISE_DEBUG && !NET_LEGACY
            // This is used to reconstruct the rejection causality trace when the original exception is rethrown from await in an async function, and this container is lost.
            private static readonly ConditionalWeakTable<object, RejectionException> _rejectExceptionsForTrace = new ConditionalWeakTable<object, RejectionException>();
#endif

#if !NET_LEGACY
            System.Runtime.ExceptionServices.ExceptionDispatchInfo _capturedInfo;
#endif
#if PROMISE_DEBUG
            private RejectionException _rejectException;
            // Stack traces of recursive callbacks.
            private CausalityTrace _stackTraces;

            public void SetCreatedAndRejectedStacktrace(StackTrace rejectedStacktrace, CausalityTrace createdStacktraces)
            {
                ThrowIfInPool(this);
                _stackTraces = createdStacktraces;
                // rejectedStacktrace will only be non-null when this is created from Deferred.Reject and causality traces are enabled.
                // Otherwise, _rejectException will have been gotten from _rejectExceptionsForTrace in Create.
                if (rejectedStacktrace != null)
                {
                    _rejectException = new RejectionException("This exception contains the stacktrace of the Deferred.Reject for the uncaught exception.", FormatStackTrace(new StackTrace[1] { rejectedStacktrace }), (Exception) Value);
#if !NET_LEGACY
                    _rejectExceptionsForTrace.Add(Value, _rejectException);
#endif
                }
            }
#endif

            private RejectionContainerException() { }

            internal static RejectionContainerException Create(Exception value)
            {
                var container = new RejectionContainerException();
                container._value = value;
#if !NET_LEGACY
                container._capturedInfo = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(value);
#if PROMISE_DEBUG
                _rejectExceptionsForTrace.TryGetValue(value, out container._rejectException);
#endif
#endif
                SetCreatedStacktrace(container, 2);
                return container;
            }

            internal override void AddToUnhandledStack()
            {
                AddUnhandledException(ToException());
            }

            private UnhandledException ToException()
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG
                return new UnhandledExceptionInternal(Value, "An exception was not handled." + CausalityTraceMessage, _stackTraces.ToString(), _rejectException ?? (Exception) Value);
#else
                return new UnhandledExceptionInternal(Value, "An exception was not handled." + CausalityTraceMessage, null, (Exception) Value);
#endif
            }

#if !NET_LEGACY
            System.Runtime.ExceptionServices.ExceptionDispatchInfo IRejectValueContainer.GetExceptionDispatchInfo()
            {
                ThrowIfInPool(this);
                return _capturedInfo;
            }
#else
            Exception IRejectValueContainer.GetException()
            {
                ThrowIfInPool(this);
                return ToException();
            }
#endif

            RejectContainer IRejectionToContainer.ToContainer(ITraceable traceable)
            {
                ThrowIfInPool(this);
                return this;
            }

            void ICantHandleException.AddToUnhandledStack(ITraceable traceable)
            {
                ThrowIfInPool(this);
                AddUnhandledException(ToException());
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class RethrownRejectionContainer : RejectContainer, IRejectValueContainer, IRejectionToContainer, ICantHandleException
        {
            private UnhandledExceptionInternal _exception;

#if PROMISE_DEBUG
            public void SetCreatedAndRejectedStacktrace(StackTrace rejectedStacktrace, CausalityTrace createdStacktraces)
            {
                ThrowIfInPool(this);
            }
#endif

            private RethrownRejectionContainer() { }

            internal static RethrownRejectionContainer Create(UnhandledExceptionInternal exception)
            {
                var container = new RethrownRejectionContainer();
                container._value = exception.Value;
                container._exception = exception;
                SetCreatedStacktrace(container, 2);
                return container;
            }

            internal override void AddToUnhandledStack()
            {
                AddUnhandledException(ToException());
            }

            private UnhandledException ToException()
            {
                ThrowIfInPool(this);
                return _exception;
            }

#if !NET_LEGACY
            System.Runtime.ExceptionServices.ExceptionDispatchInfo IRejectValueContainer.GetExceptionDispatchInfo()
            {
                ThrowIfInPool(this);
                return System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(Value as Exception ?? ToException());
            }
#else
            Exception IRejectValueContainer.GetException()
            {
                ThrowIfInPool(this);
                return ToException();
            }
#endif

            RejectContainer IRejectionToContainer.ToContainer(ITraceable traceable)
            {
                ThrowIfInPool(this);
                return this;
            }

            void ICantHandleException.AddToUnhandledStack(ITraceable traceable)
            {
                ThrowIfInPool(this);
                AddUnhandledException(ToException());
            }
        }
    }
}