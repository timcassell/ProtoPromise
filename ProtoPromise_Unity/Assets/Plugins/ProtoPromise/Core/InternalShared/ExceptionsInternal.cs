#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Diagnostics;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class UnhandledExceptionInternal : UnhandledException, IRejectionToContainer, IRejectValueContainer, ICantHandleException
        {
            internal UnhandledExceptionInternal(object value, Type valueType, string message, string stackTrace, Exception innerException) :
                base(value, valueType, message, stackTrace, innerException)
            { }

            Promise.State IValueContainer.GetState()
            {
                return Promise.State.Rejected;
            }

            void IValueContainer.Retain() { }

            void IValueContainer.Release() { }

            void IValueContainer.ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                if (shouldAdd)
                {
                    AddUnhandledException(this);
                }
            }

            Exception IThrowable.GetException()
            {
                return this;
            }

            void ICantHandleException.AddToUnhandledStack(ITraceable traceable)
            {
                AddUnhandledException(this);
            }

            IRejectValueContainer IRejectionToContainer.ToContainer(ITraceable traceable)
            {
                return this;
            }

#if PROMISE_DEBUG
            void IRejectValueContainer.SetCreatedAndRejectedStacktrace(StackTrace rejectedStacktrace, CausalityTrace createdStacktraces) { }
#endif
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class CanceledExceptionInternal : CanceledException
        {
#if !PROMISE_DEBUG
            private static readonly CanceledExceptionInternal _instance = new CanceledExceptionInternal("Operation was canceled.");
#endif

            internal static CanceledExceptionInternal GetOrCreate()
            {
#if PROMISE_DEBUG
                return new CanceledExceptionInternal("Operation was canceled."); // Don't re-use instance in DEBUG mode so users can read its stacktrace on any thread.
#else
                return _instance;
#endif
            }

            private CanceledExceptionInternal(string message) : base(message) { }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
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
        [DebuggerNonUserCode]
#endif
        internal sealed class RejectExceptionInternal<T> : RejectException, IRejectionToContainer, ICantHandleException
        {
            public T Value { get; private set; }

            internal RejectExceptionInternal(T value)
            {
                Value = value;
            }

            public IRejectValueContainer ToContainer(ITraceable traceable)
            {
                var rejection = CreateRejectContainer(Value, int.MinValue, traceable);
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

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class ForcedRethrowException : RethrowException
        {
#if !PROMISE_DEBUG
            private static readonly ForcedRethrowException _instance = new ForcedRethrowException();
#endif

            private ForcedRethrowException() { }

            new internal static ForcedRethrowException GetOrCreate()
            {
#if PROMISE_DEBUG
                return new ForcedRethrowException(); // Don't re-use instance in DEBUG mode so that we can read its stacktrace on any thread.
#else
                return _instance;
#endif
            }
        }
    }
}