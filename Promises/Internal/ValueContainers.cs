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

using System;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        partial class Internal
        {
            public abstract class PoolableObject<T> : ILinked<T> where T : PoolableObject<T>
            {
                T ILinked<T>.Next { get; set; }

                protected static ValueLinkedStack<T> _pool;

                static PoolableObject()
                {
                    OnClearPool += () => _pool.Clear();
                }
            }

            public sealed class RejectionContainer<T> : PoolableObject<RejectionContainer<T>>, IRejectionContainer, IValueContainer<T>
            {
                public T Value { get; private set; }
                private int _retainCounter;

#if PROMISE_DEBUG
                System.Diagnostics.StackTrace _rejectedStacktrace;
                // Stack traces of recursive promises.
                private DeepStacktrace _stacktraces;

                public void SetCreatedAndRejectedStacktrace(System.Diagnostics.StackTrace rejectedStacktrace, DeepStacktrace createdStacktraces)
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

                public State GetStateAndValueAs<U>(out U value)
                {
                    return TryGetValueAs(out value) ? State.Rejected : State.Pending;
                }

                public bool TryGetValueAs<U>(out U value)
                {
                    return Config.ValueConverter.TryConvert(this, out value);
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

                    if (typeof(T) == typeof(Exception))
                    {
                        Exception e = Value as Exception;
#if PROMISE_DEBUG
                        if (_rejectedStacktrace == null)
                        {
                            innerException = e;
                        }
                        else
                        {
                            string innerStacktrace = FormatStackTrace(new System.Diagnostics.StackTrace[1] { _rejectedStacktrace });
                            innerException = new InnerException<T>(message, innerStacktrace, e);
                        }
#else
                        innerException = e;
#endif
                        message = "An exception was not handled.";
                    }
                    else
                    {
                        Type type = typeof(T);
                        message = "A rejected value was not handled, type: " + type + ", value: " + (ReferenceEquals(Value, null) ? "NULL" : Value.ToString());
#if PROMISE_DEBUG
                        innerException = new InnerException<T>(message, FormatStackTrace(new System.Diagnostics.StackTrace[1] { _rejectedStacktrace }), null);
#else
                        innerException = new InnerException<T>(message, null, null);
#endif
                    }
#if PROMISE_DEBUG
                    string outerStacktrace = _stacktraces.ToString();
                    message += Config.DebugStacktraceGenerator == GeneratedStacktrace.All
                        ? " -- This exception contains the deep recursive stacktraces of all async callbacks that ran."
                        : " -- Set Proto.Promises.Promise.Config.DebugStacktraceGenerator to GeneratedStacktrace.All to get a deep recursive stacktrace.";
#else
                    string outerStacktrace = null;
#endif
                    return new UnhandledException<T>(Value, message, outerStacktrace, innerException);
                }
            }

            public sealed class CancelContainer<T> : PoolableObject<CancelContainer<T>>, IValueContainer, IValueContainer<T>
            {
                public T Value { get; private set; }
                private int _retainCounter;

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

                public State GetStateAndValueAs<U>(out U value)
                {
                    return TryGetValueAs(out value) ? State.Canceled : State.Pending;
                }

                public bool TryGetValueAs<U>(out U value)
                {
                    return Config.ValueConverter.TryConvert(this, out value);
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

                private void Dispose()
                {
                    Value = default(T);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            public sealed class CancelContainerVoid : IValueContainer
            {
                // We can reuse the same object.
                private static readonly CancelContainerVoid _instance = new CancelContainerVoid();

                public static CancelContainerVoid GetOrCreate()
                {
                    return _instance;
                }

                public State GetState()
                {
                    return State.Canceled;
                }

                public State GetStateAndValueAs<U>(out U value)
                {
                    value = default(U);
                    return State.Pending;
                }

                public bool TryGetValueAs<U>(out U value)
                {
                    value = default(U);
                    return false;
                }

                public void SetNewOwner(Promise newOwner, bool appendStacktrace) { }

                public void Retain() { }

                public void Release() { }

                public void ReleaseAndMaybeAddToUnhandledStack() { }
            }

            public sealed class ResolveContainer<T> : PoolableObject<ResolveContainer<T>>, IValueContainer
            {
                public T value;
                private int _retainCounter;

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

                public State GetStateAndValueAs<U>(out U value)
                {
                    value = (this as ResolveContainer<U>).value;
                    return State.Resolved;
                }

                public bool TryGetValueAs<U>(out U value)
                {
                    throw new System.InvalidOperationException();
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

                private void Dispose()
                {
                    value = default(T);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            public sealed class ResolveContainerVoid : IValueContainer
            {
                // We can reuse the same object.
                private static readonly ResolveContainerVoid _instance = new ResolveContainerVoid();

                public static ResolveContainerVoid GetOrCreate()
                {
                    return _instance;
                }

                public State GetState()
                {
                    return State.Resolved;
                }

                public State GetStateAndValueAs<U>(out U value)
                {
                    throw new System.InvalidOperationException();
                }

                public bool TryGetValueAs<U>(out U value)
                {
                    throw new System.InvalidOperationException();
                }

                public void SetNewOwner(Promise newOwner, bool appendStacktrace) { }

                public void Retain() { }

                public void Release() { }

                public void ReleaseAndMaybeAddToUnhandledStack() { }
            }
        }
    }
}