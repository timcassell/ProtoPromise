#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0001 // Class is declared partial but has only one part

using System;
using System.Collections.Generic;
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

                string _rejectedStacktrace;
                // Stack traces of recursive promises.
                private readonly List<string> _stacktraces = new List<string>();
                private Promise _currentOwner; // This allows us to get a deep recursive stacktrace without wide waiters interfering.

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

                public void SetOwnerAndRejectedStacktrace(Promise owner, string rejectedStacktrace)
                {
                    _stacktraces.Add(owner._createdStackTrace);
                    _currentOwner = owner;
                    _rejectedStacktrace = rejectedStacktrace;
                }

                // TODO: call this when a wait promise receives this.
                public void SetNewOwner(Promise newOwner, bool appendStacktrace)
                {
                    if (!ReferenceEquals(newOwner._valueOrPrevious, _currentOwner))
                    {
                        return; // Ignore wide waiters.
                    }
                    _currentOwner = newOwner;
                    if (appendStacktrace)
                    {
                        _stacktraces.Add(newOwner._createdStackTrace);
                    }
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
                    Value = default(T);
                    _rejectedStacktrace = null;
                    _stacktraces.Clear();
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public UnhandledException ToException()
                {
                    string message = null;
                    Exception innerException;
#if CSHARP_7_OR_LATER
                    if (((object) Value) is Exception e)
#else
                    Exception e = Value as Exception;
                    if (e != null)
#endif
                    {
                        message = "An exception was not handled.";
                        innerException = new InnerException<T>(message, FormatStackTrace(_rejectedStacktrace), e);
                    }
                    else
                    {
                        Type type = typeof(T);
                        message = "A rejected value was not handled, type: " + type + ", value: " + (ReferenceEquals(Value, null) ? "NULL" : Value.ToString());
                        innerException = new InnerException<T>(message, FormatStackTrace(_rejectedStacktrace), null);
                    }
                    message += " This exception contains the created stacktraces of all recursive promises that were rejected.";
                    string stacktrace = FormatStackTrace(string.Join("\n", _stacktraces.ToArray()));
                    return new UnhandledException<T>(Value, message, stacktrace, innerException);
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