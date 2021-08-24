#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

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
            RejectionContainer<T> ILinked<RejectionContainer<T>>.Next { get; set; }

            public T Value
            {
                get
                {
                    return _value;
                }
            }

            object IValueContainer.Value
            {
                get
                {
                    return _value;
                }
            }

            Type IValueContainer.ValueType
            {
                get
                {
                    Type type = typeof(T);
                    if (type.IsValueType)
                    {
                        return type;
                    }
                    object temp = _value;
                    return temp == null ? type : temp.GetType();
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

            private RejectionContainer() { }

            ~RejectionContainer()
            {
                if (_retainCounter != 0)
                {
                    // For debugging. This should never happen.
                    string message = "A RejectionContainer was garbage collected without it being released. _retainCounter: " + _retainCounter + ", _value: " + _value;
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), null);
                }
            }

            public static RejectionContainer<T> GetOrCreate(ref T value, int retainCount)
            {
                var container = ObjectPool<RejectionContainer<T>>.TryTake<RejectionContainer<T>>()
                    ?? new RejectionContainer<T>();
                container._value = value;
                container._retainCounter = retainCount;
                return container;
            }

            public Promise.State GetState()
            {
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
                if (ReleaseInternal())
                {
                    Dispose();
                }
            }

            public void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                ThrowIfInPool(this);
                if (ReleaseInternal())
                {
                    if (shouldAdd)
                    {
                        AddUnhandledException(ToException());
                    }
                    Dispose();
                }
            }

            public void ReleaseAndAddToUnhandledStack()
            {
                ThrowIfInPool(this);
                AddUnhandledException(ToException());
                Release();
            }

            private bool ReleaseInternal()
            {
                int newValue;
                // Don't let counter go below 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, -1, 0, out newValue))
                {
                    throw new OverflowException(); // This should never happen, but checking just in case.
                }
                return newValue == 0;
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
            CancelContainer<T> ILinked<CancelContainer<T>>.Next { get; set; }

            private int _retainCounter;
            private T _value;
            public T Value
            {
                get
                {
                    return _value;
                }
            }

            object IValueContainer.Value
            {
                get
                {
                    return _value;
                }
            }

            Type IValueContainer.ValueType
            {
                get
                {
                    Type type = typeof(T);
                    if (type.IsValueType)
                    {
                        return type;
                    }
                    object temp = _value;
                    return temp == null ? type : temp.GetType();
                }
            }

            private CancelContainer() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            ~CancelContainer()
            {
                if (_retainCounter != 0)
                {
                    // For debugging. This should never happen.
                    string message = "A CancelContainer was garbage collected without it being released. _retainCounter: " + _retainCounter + ", _value: " + _value;
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), null);
                }
            }
#endif

            public static CancelContainer<T> GetOrCreate(ref T value, int retainCount)
            {
                var container = ObjectPool<CancelContainer<T>>.TryTake<CancelContainer<T>>()
                    ?? new CancelContainer<T>();
                container._value = value;
                container._retainCounter = retainCount;
                return container;
            }

            public Promise.State GetState()
            {
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
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                // Don't let counter go below 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, -1, 0, out newValue))
                {
                    throw new OverflowException(); // This should never happen, but checking just in case.
                }
#else
                newValue = System.Threading.Interlocked.Decrement(ref _retainCounter);
#endif
                if (newValue == 0)
                {
                    Dispose();
                }
            }

            public void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                Release();
            }

            public void ReleaseAndAddToUnhandledStack()
            {
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
                Type type = typeof(T).IsValueType ? typeof(T) : _value.GetType();
                string message = "Operation was canceled with a reason, type: " + type + ", value: " + _value.ToString();

                return new CanceledExceptionInternal<T>(_value, message);
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

            [MethodImpl(InlineOption)]
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
            public void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd) { }
            public void ReleaseAndAddToUnhandledStack() { }

            Exception IThrowable.GetException()
            {
                return CanceledExceptionInternalVoid.GetOrCreate();
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
            ResolveContainer<T> ILinked<ResolveContainer<T>>.Next { get; set; }

            private int _retainCounter;
            public T value;

            T IValueContainer<T>.Value
            {
                get
                {
                    return value;
                }
            }

            object IValueContainer.Value
            {
                get
                {
                    return value;
                }
            }

            Type IValueContainer.ValueType
            {
                get
                {
                    Type type = typeof(T);
                    if (type.IsValueType)
                    {
                        return type;
                    }
                    object temp = value;
                    return temp == null ? type : temp.GetType();
                }
            }

            private ResolveContainer() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            ~ResolveContainer()
            {
                if (_retainCounter != 0)
                {
                    // For debugging. This should never happen.
                    string message = "A ResolveContainer was garbage collected without it being released. _retainCounter: " + _retainCounter + ", value: " + value;
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), null);
                }
            }
#endif

            // TODO: check typeof(T).IsValueType == false and use the PromiseRef as the value container for reference types.
            public static ResolveContainer<T> GetOrCreate(ref T value, int retainCount)
            {
                var container = ObjectPool<ResolveContainer<T>>.TryTake<ResolveContainer<T>>()
                    ?? new ResolveContainer<T>();
                container.value = value;
                container._retainCounter = retainCount;
                return container;
            }

            [MethodImpl(InlineOption)]
            public static ResolveContainer<T> GetOrCreate(T value, int retainCount)
            {
                return GetOrCreate(ref value, retainCount);
            }

            public Promise.State GetState()
            {
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
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                // Don't let counter go below 0.
                if (!InterlockedAddIfNotEqual(ref _retainCounter, -1, 0, out newValue))
                {
                    throw new OverflowException(); // This should never happen, but checking just in case.
                }
#else
                newValue = System.Threading.Interlocked.Decrement(ref _retainCounter);
#endif
                if (newValue == 0)
                {
                    Dispose();
                }
            }

            public void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                Release();
            }

            public void ReleaseAndAddToUnhandledStack()
            {
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

            [MethodImpl(InlineOption)]
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
            public void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd) { }
            public void ReleaseAndAddToUnhandledStack() { }
        }
    }
}