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
        public sealed class UnhandledExceptionInternal : UnhandledException, IRejectionToContainer, IRejectValueContainer, ICantHandleException
        {
            private int _retainCounter;

            public UnhandledExceptionInternal(object value, Type valueType, string message, string stackTrace, Exception innerException) :
                base(value, valueType, message, stackTrace, innerException)
            { }

            Promise.State IValueContainer.GetState()
            {
                return Promise.State.Rejected;
            }

            void IValueContainer.Retain()
            {
                int _;
                // Don't let counter wrap around past 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, 1, -1, out _))
                {
                    throw new OverflowException();
                }
            }

            void IValueContainer.Release()
            {
                int _;
                // Don't let counter go below 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, -1, 0, out _))
                {
                    throw new OverflowException(); // This should never happen, but checking just in case.
                }
            }

            void IValueContainer.ReleaseAndMaybeAddToUnhandledStack()
            {
                int newValue;
                // Don't let counter go below 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, -1, 0, out newValue))
                {
                    throw new OverflowException(); // This should never happen, but checking just in case.
                }
                if (newValue == 0)
                {
                    AddUnhandledException(this);
                }
            }

            void IValueContainer.ReleaseAndAddToUnhandledStack()
            {
                AddUnhandledException(this);
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
        public sealed class CanceledExceptionInternal<T> : CanceledException, ICancelValueContainer, ICancelationToContainer
        {
            private readonly T _value;

            public CanceledExceptionInternal(T value, string message) : base(message)
            {
                _value = value;
            }

            public override Type ValueType { get { return typeof(T).IsValueType ? typeof(T) : _value.GetType(); } }

            public override object Value { get { return _value; } }

            public override bool TryGetValueAs<TConvert>(out TConvert value)
            {
                CanceledExceptionInternal<TConvert> casted = this as CanceledExceptionInternal<TConvert>;
                if (casted != null)
                {
                    value = casted._value;
                    return true;
                }
                if (!typeof(T).IsValueType && typeof(TConvert).IsAssignableFrom(_value.GetType()))
                {
                    value = (TConvert) (object) _value;
                    return true;
                }
                value = default(TConvert);
                return false;
            }

            Promise.State IValueContainer.GetState()
            {
                return Promise.State.Canceled;
            }

            void IValueContainer.Retain() { }
            void IValueContainer.Release() { }
            void IValueContainer.ReleaseAndMaybeAddToUnhandledStack() { }
            void IValueContainer.ReleaseAndAddToUnhandledStack() { }

            Exception IThrowable.GetException()
            {
                return this;
            }

            ICancelValueContainer ICancelationToContainer.ToContainer()
            {
                return this;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public sealed class CanceledExceptionInternalVoid : CanceledException, ICancelValueContainer, ICancelationToContainer
        {
#if !PROMISE_DEBUG
            private static readonly CanceledExceptionInternalVoid _instance = new CanceledExceptionInternalVoid("Operation was canceled without a reason.");
#endif

            public static CanceledExceptionInternalVoid GetOrCreate()
            {
#if PROMISE_DEBUG
                return new CanceledExceptionInternalVoid("Operation was canceled without a reason."); // Don't re-use instance in DEBUG mode so users can read its stacktrace on any thread.
#else
                return _instance;
#endif
            }

            public CanceledExceptionInternalVoid(string message) : base(message) { }

            public override Type ValueType { get { return null; } }

            public override object Value { get { return null; } }

            public override bool TryGetValueAs<TConvert>(out TConvert value)
            {
                value = default(TConvert);
                return false;
            }

            Promise.State IValueContainer.GetState()
            {
                return Promise.State.Canceled;
            }

            void IValueContainer.Retain() { }
            void IValueContainer.Release() { }
            void IValueContainer.ReleaseAndMaybeAddToUnhandledStack() { }
            void IValueContainer.ReleaseAndAddToUnhandledStack() { }

            Exception IThrowable.GetException()
            {
                return this;
            }

            ICancelValueContainer ICancelationToContainer.ToContainer()
            {
                return this;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public sealed class RejectionException : Exception
        {
            private readonly string _stackTrace;

            public RejectionException(string message, string stackTrace, Exception innerException) : base(message, innerException)
            {
                _stackTrace = stackTrace;
            }

            public override string StackTrace { get { return _stackTrace; } }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public sealed class RejectExceptionInternal<T> : RejectException, IRejectionToContainer, ICantHandleException
        {
            public T Value { get; private set; }

            public RejectExceptionInternal(T value)
            {
                Value = value;
            }

            public IRejectValueContainer ToContainer(ITraceable traceable)
            {
                T value = Value;
                var rejection = CreateRejectContainer(ref value, int.MinValue, traceable);
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
            private static readonly ForcedRethrowException _instance = new ForcedRethrowException();

            private ForcedRethrowException() { }

            new internal static ForcedRethrowException GetOrCreate()
            {
                return _instance;
            }
        }
    }
}