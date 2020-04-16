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
    partial class Promise
    {
        partial class Internal
        {
            [System.Diagnostics.DebuggerNonUserCode]
            public abstract class PoolableObject<T> : ILinked<T> where T : PoolableObject<T>
            {
                T ILinked<T>.Next { get; set; }

                protected static ValueLinkedStack<T> _pool;

                static PoolableObject()
                {
                    OnClearPool += () => _pool.Clear();
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class RejectionContainer<T> : PoolableObject<RejectionContainer<T>>, IRejectionContainer, IValueContainer<T>, IThrowable
            {
                public T Value { get; private set; }

                object IValueContainer.Value { get { return Value; } }

                Type IValueContainer.ValueType
                {
                    get
                    {
                        Type type = typeof(T);
                        if (type.IsValueType || ReferenceEquals(Value, null))
                        {
                            return type;
                        }
                        return Value.GetType();
                    }
                }

                private int _retainCounter;

#if PROMISE_DEBUG
                System.Diagnostics.StackTrace _rejectedStacktrace;
                // Stack traces of recursive callbacks.
                private CausalityTrace _stacktraces;

                public void SetCreatedAndRejectedStacktrace(System.Diagnostics.StackTrace rejectedStacktrace, CausalityTrace createdStacktraces)
                {
                    _rejectedStacktrace = rejectedStacktrace;
                    _stacktraces = createdStacktraces;
                }
#endif

                public static RejectionContainer<T> GetOrCreate(T value)
                {
                    RejectionContainer<T> ex = _pool.IsNotEmpty ? _pool.Pop() : new RejectionContainer<T>();
                    ex.Value = value;
                    return ex;
                }

                public State GetState()
                {
                    return State.Rejected;
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
                    _rejectedStacktrace = null;
                    _stacktraces = null;
#endif
                    Value = default(T);
                    if (Config.ObjectPooling != PoolType.None)
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
                    string innerStacktrace = _rejectedStacktrace == null ? null : FormatStackTrace(new System.Diagnostics.StackTrace[1] { _rejectedStacktrace });
#else
                    string innerStacktrace = null;
#endif
                    if (typeof(Exception).IsAssignableFrom(type))
                    {
                        Exception e = Value as Exception;
#if PROMISE_DEBUG
                        if (_rejectedStacktrace == null)
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
                    string outerStacktrace = _stacktraces.ToString();
                    message += Config.DebugCausalityTracer == TraceLevel.All
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
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class CancelContainer<T> : PoolableObject<CancelContainer<T>>, IValueContainer, IValueContainer<T>, IThrowable
            {
                public T Value { get; private set; }
                private int _retainCounter;

                object IValueContainer.Value { get { return Value; } }

                Type IValueContainer.ValueType
                {
                    get
                    {
                        Type type = typeof(T);
                        if (type.IsValueType || ReferenceEquals(Value, null))
                        {
                            return type;
                        }
                        return Value.GetType();
                    }
                }

                public static CancelContainer<T> GetOrCreate(T value)
                {
                    CancelContainer<T> ex = _pool.IsNotEmpty ? _pool.Pop() : new CancelContainer<T>();
                    ex.Value = value;
                    return ex;
                }

                public State GetState()
                {
                    return State.Canceled;
                }

                public void SetNewOwner(Promise newOwner, bool appendStacktrace) { }

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
                    if (Config.ObjectPooling != PoolType.None)
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
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class CancelContainerVoid : IValueContainer, IThrowable
            {
                // We can reuse the same object.
                private static readonly CancelContainerVoid _instance = new CancelContainerVoid();

                object IValueContainer.Value { get { return null; } }

                Type IValueContainer.ValueType { get { return null; } }

                public static CancelContainerVoid GetOrCreate()
                {
                    return _instance;
                }

                public State GetState()
                {
                    return State.Canceled;
                }

                public void SetNewOwner(Promise newOwner, bool appendStacktrace) { }
                public void Retain() { }
                public void Release() { }
                public void ReleaseAndMaybeAddToUnhandledStack() { }
                public void ReleaseAndAddToUnhandledStack() { }

                Exception IThrowable.GetException()
                {
                    return new CanceledExceptionInternal(null, null, "Promise was canceled without a reason.");
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class ResolveContainer<T> : PoolableObject<ResolveContainer<T>>, IValueContainer, IValueContainer<T>
            {
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

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
                public static ResolveContainer<T> GetOrCreate(in T value)
#else
                public static ResolveContainer<T> GetOrCreate(T value)
#endif
                {
                    ResolveContainer<T> ex = _pool.IsNotEmpty ? _pool.Pop() : new ResolveContainer<T>();
                    ex.value = value;
                    return ex;
                }

                public State GetState()
                {
                    return State.Resolved;
                }

                public void SetNewOwner(Promise newOwner, bool appendStacktrace) { }

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
                    if (Config.ObjectPooling != PoolType.None)
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

                public State GetState()
                {
                    return State.Resolved;
                }

                public void SetNewOwner(Promise newOwner, bool appendStacktrace) { }
                public void Retain() { }
                public void Release() { }
                public void ReleaseAndMaybeAddToUnhandledStack() { }
                public void ReleaseAndAddToUnhandledStack() { }
            }
        }
    }
}