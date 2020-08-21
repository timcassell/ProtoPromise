#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0001 // Class is declared partial but has only one part
#pragma warning disable IDE0041 // Use 'is null' check
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable IDE0031 // Use null propagation

using System;
using Proto.Utils;

namespace Proto.Promises
{
    internal static partial class Internal
    {
        [System.Diagnostics.DebuggerNonUserCode]
        public sealed class RejectionContainer<T> : ILinked<RejectionContainer<T>>, IRejectValueContainer, IValueContainer<T>, IRejectionToContainer, ICantHandleException
        {
            RejectionContainer<T> ILinked<RejectionContainer<T>>.Next { get; set; }

            private static ValueLinkedStack<RejectionContainer<T>> _pool;

            static RejectionContainer()
            {
                OnClearPool += () => _pool.Clear();
            }

            public T Value { get; private set; }

            object IValueContainer.Value { get { return Value; } }

            Type IValueContainer.ValueType
            {
                get
                {
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

#if PROMISE_DEBUG
            System.Diagnostics.StackTrace _rejectedStackTrace;
            // Stack traces of recursive callbacks.
            private CausalityTrace _stackTraces;

            public void SetCreatedAndRejectedStacktrace(System.Diagnostics.StackTrace rejectedStacktrace, CausalityTrace createdStacktraces)
            {
                _rejectedStackTrace = rejectedStacktrace;
                _stackTraces = createdStacktraces;
            }
#endif

            public static RejectionContainer<T> GetOrCreate(ref T value)
            {
                RejectionContainer<T> ex = _pool.IsNotEmpty ? _pool.Pop() : new RejectionContainer<T>();
                ex.Value = value;
                return ex;
            }

            public Promise.State GetState()
            {
                return Promise.State.Rejected;
            }

            public void Retain()
            {
                ++_retainCounter;
            }

            public void Release()
            {
                if (--_retainCounter == 0)
                {
                    Dispose();
                }
            }

            public void ReleaseAndMaybeAddToUnhandledStack()
            {
                if (--_retainCounter == 0)
                {
                    AddUnhandledException(ToException());
                    Dispose();
                }
            }

            public void ReleaseAndAddToUnhandledStack()
            {
                AddUnhandledException(ToException());
                if (--_retainCounter == 0)
                {
                    Dispose();
                }
            }

            private void Dispose()
            {
#if PROMISE_DEBUG
                _rejectedStackTrace = null;
                _stackTraces = null;
#endif
                Value = default(T);
                if (Promise.Config.ObjectPooling != Promise.PoolType.None)
                {
                    _pool.Push(this);
                }
            }

            public UnhandledException ToException()
            {
                string message = null;
                Exception innerException;
                bool valueIsNull = ReferenceEquals(Value, null);
                Type type = valueIsNull ? typeof(T) : Value.GetType();

#if PROMISE_DEBUG
                string innerStacktrace = _rejectedStackTrace == null ? null : FormatStackTrace(new System.Diagnostics.StackTrace[1] { _rejectedStackTrace });
#else
                    string innerStacktrace = null;
#endif
                if (typeof(Exception).IsAssignableFrom(type))
                {
                    Exception e = Value as Exception;
#if PROMISE_DEBUG
                    if (_rejectedStackTrace == null)
                    {
                        innerException = e;
                    }
                    else
                    {
                        innerException = new RejectionException(message, innerStacktrace, e);
                    }
#else
                        innerException = e;
#endif
                    message = "An exception was not handled.";
                }
                else
                {
                    message = "A rejected value was not handled, type: " + type + ", value: " + (valueIsNull ? "NULL" : Value.ToString());
                    innerException = new RejectionException(message, innerStacktrace, null);
                }
#if PROMISE_DEBUG
                string outerStacktrace = _stackTraces.ToString();
                message += Promise.Config.DebugCausalityTracer == Promise.TraceLevel.All
                    ? " -- This exception's Stacktrace contains the causality trace of all async callbacks that ran."
                    : " -- Set Proto.Promises.Promise.Config.DebugCausalityTracer to Proto.Promises.Promise.TraceLevel.All to get a causality trace.";
#else
                    string outerStacktrace = null;
#endif
                return new UnhandledExceptionInternal(Value, type, message, outerStacktrace, innerException);
            }

            Exception IThrowable.GetException()
            {
                return ToException();
            }

            IRejectValueContainer IRejectionToContainer.ToContainer(ITraceable traceable)
            {
                return this;
            }

            void ICantHandleException.AddToUnhandledStack(ITraceable traceable)
            {
                AddUnhandledException(ToException());
            }
        }

        [System.Diagnostics.DebuggerNonUserCode]
        public sealed class CancelContainer<T> : ILinked<CancelContainer<T>>, ICancelValueContainer, IValueContainer<T>, ICancelationToContainer
        {
            CancelContainer<T> ILinked<CancelContainer<T>>.Next { get; set; }

            private static ValueLinkedStack<CancelContainer<T>> _pool;

            static CancelContainer()
            {
                OnClearPool += () => _pool.Clear();
            }

            public T Value { get; private set; }
            private int _retainCounter;

            object IValueContainer.Value { get { return Value; } }

            Type IValueContainer.ValueType
            {
                get
                {
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
                CancelContainer<T> ex = _pool.IsNotEmpty ? _pool.Pop() : new CancelContainer<T>();
                ex.Value = value;
                return ex;
            }

            public Promise.State GetState()
            {
                return Promise.State.Canceled;
            }

            public void Retain()
            {
                ++_retainCounter;
            }

            public void Release()
            {
                if (--_retainCounter == 0)
                {
                    Dispose();
                }
            }

            public void ReleaseAndMaybeAddToUnhandledStack()
            {
                Release();
            }

            public void ReleaseAndAddToUnhandledStack()
            {
                Release();
            }

            private void Dispose()
            {
                Value = default(T);
                if (Promise.Config.ObjectPooling != Promise.PoolType.None)
                {
                    _pool.Push(this);
                }
            }

            Exception IThrowable.GetException()
            {
                bool valueIsNull = ReferenceEquals(Value, null);
                Type type = valueIsNull ? typeof(T) : Value.GetType();
                string message = "Promise was canceled with a reason, type: " + type + ", value: " + (valueIsNull ? "NULL" : Value.ToString());

                return new CanceledExceptionInternal(Value, type, message);
            }

            ICancelValueContainer ICancelationToContainer.ToContainer()
            {
                return this;
            }
        }

        [System.Diagnostics.DebuggerNonUserCode]
        public sealed class CancelContainerVoid : ICancelValueContainer, ICancelationToContainer
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
                return new CanceledExceptionInternal(null, null, "Promise was canceled without a reason.");
            }

            ICancelValueContainer ICancelationToContainer.ToContainer()
            {
                return this;
            }
        }

        [System.Diagnostics.DebuggerNonUserCode]
        public sealed class ResolveContainer<T> : ILinked<ResolveContainer<T>>, IValueContainer, IValueContainer<T>
        {
            ResolveContainer<T> ILinked<ResolveContainer<T>>.Next { get; set; }

            private static ValueLinkedStack<ResolveContainer<T>> _pool;

            static ResolveContainer()
            {
                OnClearPool += () => _pool.Clear();
            }

            public T value;
            private int _retainCounter;

            T IValueContainer<T>.Value { get { return value; } }

            object IValueContainer.Value { get { return value; } }

            Type IValueContainer.ValueType
            {
                get
                {
                    Type type = typeof(T);
                    if (type.IsValueType || ReferenceEquals(value, null))
                    {
                        return type;
                    }
                    return value.GetType();
                }
            }

            public static ResolveContainer<T> GetOrCreate(ref T value)
            {
                ResolveContainer<T> ex = _pool.IsNotEmpty ? _pool.Pop() : new ResolveContainer<T>();
                ex.value = value;
                return ex;
            }

            public Promise.State GetState()
            {
                return Promise.State.Resolved;
            }

            public void Retain()
            {
                ++_retainCounter;
            }

            public void Release()
            {
                if (--_retainCounter == 0)
                {
                    Dispose();
                }
            }

            public void ReleaseAndMaybeAddToUnhandledStack()
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
                if (Promise.Config.ObjectPooling != Promise.PoolType.None)
                {
                    _pool.Push(this);
                }
            }
        }

        [System.Diagnostics.DebuggerNonUserCode]
        public sealed class ResolveContainerVoid : IValueContainer
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