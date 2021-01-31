#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0041 // Use 'is null' check

using System;
using System.Runtime.CompilerServices;
using Proto.Utils;

namespace Proto.Promises
{
    internal static partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal sealed class RejectionContainer<T> : ILinked<RejectionContainer<T>>, IRejectValueContainer, IValueContainer<T>, IRejectionToContainer, ICantHandleException
        {
            private struct Creator : ICreator<RejectionContainer<T>>
            {
                [MethodImpl(InlineOption)]
                public RejectionContainer<T> Create()
                {
                    return new RejectionContainer<T>();
                }
            }

            RejectionContainer<T> ILinked<RejectionContainer<T>>.Next { get; set; }

            public T Value
            {
                get
                {
                    ThrowIfInPool(this);
                    return _value;
                }
            }

            object IValueContainer.Value
            {
                get
                {
                    ThrowIfInPool(this);
                    return Value;
                }
            }

            Type IValueContainer.ValueType
            {
                get
                {
                    ThrowIfInPool(this);
                    Type type = typeof(T);
                    // Value is never null.
                    if (type.IsValueType)
                    {
                        return type;
                    }
                    return Value.GetType();
                }
            }

            private int _retainCounter;
            private T _value;

#if PROMISE_DEBUG
            System.Diagnostics.StackTrace _rejectedStackTrace;
            // Stack traces of recursive callbacks.
            private CausalityTrace _stackTraces;

            public void SetCreatedAndRejectedStacktrace(System.Diagnostics.StackTrace rejectedStacktrace, CausalityTrace createdStacktraces)
            {
                ThrowIfInPool(this);
                _rejectedStackTrace = rejectedStacktrace;
                _stackTraces = createdStacktraces;
            }
#endif

            public static RejectionContainer<T> GetOrCreate(ref T value)
            {
                var container = ObjectPool<RejectionContainer<T>>.GetOrCreate<RejectionContainer<T>, Creator>(new Creator());
                container._value = value;
                return container;
            }

            public Promise.State GetState()
            {
                ThrowIfInPool(this);
                return Promise.State.Rejected;
            }

            public void Retain()
            {
                ThrowIfInPool(this);
                int _;
                // Don't let counter wrap around past 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, 1, -1, out _))
                {
                    throw new OverflowException();
                }
            }

            public void Release()
            {
                ThrowIfInPool(this);
                int newValue;
                // Don't let counter go below 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, -1, 0, out newValue))
                {
                    throw new OverflowException(); // This should never happen, but checking just in case.
                }
                if (newValue == 0)
                {
                    Dispose();
                }
            }

            public void ReleaseAndMaybeAddToUnhandledStack()
            {
                ThrowIfInPool(this);
                if (ReleaseInternal())
                {
                    AddUnhandledException(ToException());
                    Dispose();
                }
            }

            public void ReleaseAndAddToUnhandledStack()
            {
                ThrowIfInPool(this);
                AddUnhandledException(ToException());
                if (ReleaseInternal())
                {
                    Dispose();
                }
            }

            private bool ReleaseInternal()
            {
#if PROMISE_DEBUG
                checked
#endif
                {
                    return --_retainCounter == 0;
                }
            }

            private void Dispose()
            {
#if PROMISE_DEBUG
                _rejectedStackTrace = null;
                _stackTraces = null;
#endif
                _value = default(T);
                ObjectPool<RejectionContainer<T>>.MaybeRepool(this);
            }

            private UnhandledException ToException()
            {
#if PROMISE_DEBUG
                string innerStacktrace = _rejectedStackTrace == null ? null : FormatStackTrace(new System.Diagnostics.StackTrace[1] { _rejectedStackTrace });
#else
                string innerStacktrace = null;
#endif
                string message = null;
                Type type = Value.GetType();
                Exception innerException = Value as Exception;
                if (innerException != null)
                {
#if PROMISE_DEBUG
                    if (_rejectedStackTrace != null)
                    {
                        innerException = new RejectionException(message, innerStacktrace, innerException);
                    }
#endif
                    message = "An exception was not handled.";
                }
                else
                {
                    message = "A rejected value was not handled, type: " + type + ", value: " + Value.ToString();
                    innerException = new RejectionException(message, innerStacktrace, null);
                }
#if PROMISE_DEBUG
                string outerStacktrace = _stackTraces.ToString();
#else
                string outerStacktrace = null;
#endif
                return new UnhandledExceptionInternal(Value, type, message + CausalityTraceMessage, outerStacktrace, innerException);
            }

            Exception IThrowable.GetException()
            {
                ThrowIfInPool(this);
                return ToException();
            }

            IRejectValueContainer IRejectionToContainer.ToContainer(ITraceable traceable)
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
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal sealed class CancelContainer<T> : ILinked<CancelContainer<T>>, ICancelValueContainer, IValueContainer<T>, ICancelationToContainer
        {
            private struct Creator : ICreator<CancelContainer<T>>
            {
                [MethodImpl(InlineOption)]
                public CancelContainer<T> Create()
                {
                    return new CancelContainer<T>();
                }
            }

            CancelContainer<T> ILinked<CancelContainer<T>>.Next { get; set; }

            private int _retainCounter;
            private T _value;
            public T Value
            {
                get
                {
                    ThrowIfInPool(this);
                    return _value;
                }
            }

            object IValueContainer.Value
            {
                get
                {
                    ThrowIfInPool(this);
                    return Value;
                }
            }

            Type IValueContainer.ValueType
            {
                get
                {
                    ThrowIfInPool(this);
                    Type type = typeof(T);
                    // Value is never null.
                    if (type.IsValueType)
                    {
                        return type;
                    }
                    return Value.GetType();
                }
            }

            public static CancelContainer<T> GetOrCreate(ref T value)
            {
                var container = ObjectPool<CancelContainer<T>>.GetOrCreate<CancelContainer<T>, Creator>(new Creator());
                container._value = value;
                return container;
            }

            public Promise.State GetState()
            {
                ThrowIfInPool(this);
                return Promise.State.Canceled;
            }

            public void Retain()
            {
                ThrowIfInPool(this);
                int _;
                // Don't let counter wrap around past 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, 1, -1, out _))
                {
                    throw new OverflowException();
                }
            }

            public void Release()
            {
                ThrowIfInPool(this);
                int newValue;
                // Don't let counter go below 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, -1, 0, out newValue))
                {
                    throw new OverflowException(); // This should never happen, but checking just in case.
                }
                if (newValue == 0)
                {
                    Dispose();
                }
            }

            public void ReleaseAndMaybeAddToUnhandledStack()
            {
                ThrowIfInPool(this);
                Release();
            }

            public void ReleaseAndAddToUnhandledStack()
            {
                ThrowIfInPool(this);
                Release();
            }

            private void Dispose()
            {
                _value = default(T);
                ObjectPool<CancelContainer<T>>.MaybeRepool(this);
            }

            Exception IThrowable.GetException()
            {
                ThrowIfInPool(this);
                bool valueIsNull = Value == null;
                Type type = valueIsNull ? typeof(T) : Value.GetType();
                string message = "Operation was canceled with a reason, type: " + type + ", value: " + (valueIsNull ? "NULL" : Value.ToString());

                return new CanceledExceptionInternal(Value, type, message);
            }

            ICancelValueContainer ICancelationToContainer.ToContainer()
            {
                ThrowIfInPool(this);
                return this;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal sealed class CancelContainerVoid : ICancelValueContainer, ICancelationToContainer
        {
            // We can reuse the same object.
            private static readonly CancelContainerVoid _instance = new CancelContainerVoid();

            object IValueContainer.Value { get { return null; } }

            Type IValueContainer.ValueType { get { return null; } }

            public static CancelContainerVoid GetOrCreate()
            {
                return _instance;
            }

            public Promise.State GetState()
            {
                return Promise.State.Canceled;
            }

            public void Retain() { }
            public void Release() { }
            public void ReleaseAndMaybeAddToUnhandledStack() { }
            public void ReleaseAndAddToUnhandledStack() { }

            Exception IThrowable.GetException()
            {
                return new CanceledExceptionInternal(null, null, "Operation was canceled without a reason.");
            }

            ICancelValueContainer ICancelationToContainer.ToContainer()
            {
                return this;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal sealed class ResolveContainer<T> : ILinked<ResolveContainer<T>>, IValueContainer, IValueContainer<T>
        {
            private struct Creator : ICreator<ResolveContainer<T>>
            {
                [MethodImpl(InlineOption)]
                public ResolveContainer<T> Create()
                {
                    return new ResolveContainer<T>();
                }
            }

            ResolveContainer<T> ILinked<ResolveContainer<T>>.Next { get; set; }

            private int _retainCounter;
            public T value;

            T IValueContainer<T>.Value
            {
                get
                {
                    ThrowIfInPool(this);
                    return value;
                }
            }

            object IValueContainer.Value
            {
                get
                {
                    ThrowIfInPool(this);
                    return value;
                }
            }

            Type IValueContainer.ValueType
            {
                get
                {
                    ThrowIfInPool(this);
                    Type type = typeof(T);
                    if (type.IsValueType || value == null)
                    {
                        return type;
                    }
                    return value.GetType();
                }
            }

            public static ResolveContainer<T> GetOrCreate(ref T value)
            {
                var container = ObjectPool<ResolveContainer<T>>.GetOrCreate<ResolveContainer<T>, Creator>(new Creator());
                container.value = value;
                return container;
            }

            public Promise.State GetState()
            {
                ThrowIfInPool(this);
                return Promise.State.Resolved;
            }

            public void Retain()
            {
                ThrowIfInPool(this);
                int _;
                // Don't let counter wrap around past 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, 1, -1, out _))
                {
                    throw new OverflowException();
                }
            }

            public void Release()
            {
                ThrowIfInPool(this);
                int newValue;
                // Don't let counter go below 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, -1, 0, out newValue))
                {
                    throw new OverflowException(); // This should never happen, but checking just in case.
                }
                if (newValue == 0)
                {
                    Dispose();
                }
            }

            public void ReleaseAndMaybeAddToUnhandledStack()
            {
                ThrowIfInPool(this);
                Release();
            }

            public void ReleaseAndAddToUnhandledStack()
            {
                ThrowIfInPool(this);
                Release();
            }

            private void Dispose()
            {
                value = default(T);
                ObjectPool<ResolveContainer<T>>.MaybeRepool(this);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal sealed class ResolveContainerVoid : IValueContainer
        {
            // We can reuse the same object.
            private static readonly ResolveContainerVoid _instance = new ResolveContainerVoid();

            object IValueContainer.Value { get { return null; } }

            Type IValueContainer.ValueType { get { return null; } }

            public static ResolveContainerVoid GetOrCreate()
            {
                return _instance;
            }

            public Promise.State GetState()
            {
                return Promise.State.Resolved;
            }

            public void Retain() { }
            public void Release() { }
            public void ReleaseAndMaybeAddToUnhandledStack() { }
            public void ReleaseAndAddToUnhandledStack() { }
        }
    }
}