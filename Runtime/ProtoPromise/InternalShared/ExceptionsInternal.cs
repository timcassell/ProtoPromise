#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
                checked
                {
                    ++_retainCounter;
                }
            }
            void IValueContainer.Release()
            {
                checked
                {
                    --_retainCounter;
                }
            }
            void IValueContainer.ReleaseAndMaybeAddToUnhandledStack()
            {
                checked
                {
                    if (--_retainCounter == 0)
                    {
                        AddUnhandledException(this);
                    }
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
        public sealed class CanceledExceptionInternal : CanceledException, ICancelValueContainer, ICancelationToContainer
        {
            public CanceledExceptionInternal(object value, Type valueType, string message) :
                base(value, valueType, message)
            { }

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
            // We can reuse the same object.
            private static readonly RejectExceptionInternal<T> _instance = new RejectExceptionInternal<T>();

            public T Value { get; private set; }

            public static RejectExceptionInternal<T> GetOrCreate(T value)
            {
                _instance.Value = value;
                return _instance;
            }

            private RejectExceptionInternal() { }

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
        public sealed class CancelExceptionVoidInternal : CancelException, ICancelationToContainer
        {
            // We can reuse the same object.
            private static readonly CancelExceptionVoidInternal _instance = new CancelExceptionVoidInternal();

            public static CancelExceptionVoidInternal GetOrCreate()
            {
                return _instance;
            }

            private CancelExceptionVoidInternal() { }

            public ICancelValueContainer ToContainer()
            {
                return CancelContainerVoid.GetOrCreate();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public sealed class CancelExceptionInternal<T> : CancelException, ICancelationToContainer
        {
            // We can reuse the same object.
            private static readonly CancelExceptionInternal<T> _instance = new CancelExceptionInternal<T>();

            public T Value { get; private set; }

            public static CancelExceptionInternal<T> GetOrCreate(T value)
            {
                _instance.Value = value;
                return _instance;
            }

            private CancelExceptionInternal() { }

            public ICancelValueContainer ToContainer()
            {
                T value = Value;
                return CreateCancelContainer(ref value);
            }
        }
    }
}