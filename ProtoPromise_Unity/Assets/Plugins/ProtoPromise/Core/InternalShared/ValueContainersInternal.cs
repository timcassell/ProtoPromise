#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    internal static partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal abstract class ValueContainer<T> : ValueContainer, ITraceable
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif
            // This should be nint to be more efficient in 32-bit runtimes, but it's only available in C# 9 and later, and Interlocked does not have an overload for nint.
            private long _retainCounter;
            public T value;

            internal override sealed object Value
            {
                get
                {
                    // Don't throw if in pool, just return either way.
                    // If it's in the pool, it means the promise was canceled, and it will check the cancelation before using the value.
                    //ThrowIfInPool(this);
                    return value;
                }
            }

            internal override sealed Type ValueType
            {
                get
                {
                    // Don't throw if in pool, just return either way.
                    // If it's in the pool, it means the promise was canceled, and it will check the cancelation before using the value.
                    //ThrowIfInPool(this);
                    Type type = typeof(T);
                    if (type.IsValueType)
                    {
                        return type;
                    }
                    object temp = value;
                    return temp == null ? type : temp.GetType();
                }
            }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            ~ValueContainer()
            {
                try
                {
                    if (_retainCounter != 0)
                    {
                        // For debugging. This should never happen.
                        string message = "A " + GetType() + " was garbage collected without it being released. _retainCounter: " + _retainCounter + ", value: " + value;
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                    }
                }
                catch (Exception e)
                {
                    // This should never happen.
                    AddRejectionToUnhandledStack(e, this);
                }
            }
#endif

            internal override void Retain()
            {
                ThrowIfInPool(this);
                // Generally it is impossible to overflow the long, but it may be possible if the user is abusing promises.
                InterlockedAddWithOverflowCheck(ref _retainCounter, 1, -1);
            }

            protected bool TryReleaseComplete()
            {
                ThrowIfInPool(this);
                return InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0) == 0;
            }

            protected void Reset()
            {
                _retainCounter = 1;
                SetCreatedStacktrace(this, 2);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class RejectionContainer<T> : ValueContainer<T>, ILinked<RejectionContainer<T>>, IRejectValueContainer, IRejectionToContainer, ICantHandleException
        {
            RejectionContainer<T> ILinked<RejectionContainer<T>>.Next { get; set; }

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

            internal static RejectionContainer<T> GetOrCreate(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                T value)
            {
                var container = ObjectPool<RejectionContainer<T>>.TryTake<RejectionContainer<T>>()
                    ?? new RejectionContainer<T>();
                container.value = value;
                container.Reset();
                return container;
            }

            internal override Promise.State GetState()
            {
                return Promise.State.Rejected;
            }

            internal override void Release()
            {
                ThrowIfInPool(this);
                if (TryReleaseComplete())
                {
                    Dispose();
                }
            }

            internal override void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                if (shouldAdd)
                {
                    AddToUnhandledStack();
                }
                Release();
            }

            internal override void AddToUnhandledStack()
            {
                AddUnhandledException(ToException());
            }

            private void Dispose()
            {
#if PROMISE_DEBUG
                _rejectedStackTrace = null;
                _stackTraces = null;
#endif
                value = default(T);
                ObjectPool<RejectionContainer<T>>.MaybeRepool(this);
            }

            private UnhandledException ToException()
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG
                string innerStacktrace = _rejectedStackTrace == null ? null : FormatStackTrace(new StackTrace[1] { _rejectedStackTrace });
#else
                string innerStacktrace = null;
#endif
                string message = "A rejected value was not handled, type: " + value.GetType() + ", value: " + value.ToString();
                Exception innerException = new RejectionException(message, innerStacktrace, null);
#if PROMISE_DEBUG
                string outerStacktrace = _stackTraces.ToString();
#else
                string outerStacktrace = null;
#endif
                return new UnhandledExceptionInternal(value, message + CausalityTraceMessage, outerStacktrace, innerException);
            }

#if CSHARP_7_3_OR_NEWER
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

            ValueContainer IRejectionToContainer.ToContainer(ITraceable traceable)
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
        internal sealed class RejectionContainerException : ValueContainer<Exception>, ILinked<RejectionContainerException>, IRejectValueContainer, IRejectionToContainer, ICantHandleException
        {
#if PROMISE_DEBUG && CSHARP_7_3_OR_NEWER
            // This is used to reconstruct the rejection causality trace when the original exception is rethrown from await in an async function, and this container is lost.
            private static readonly ConditionalWeakTable<Exception, RejectionException> _rejectExceptionsForTrace = new ConditionalWeakTable<Exception, RejectionException>();
#endif

            RejectionContainerException ILinked<RejectionContainerException>.Next { get; set; }

#if CSHARP_7_3_OR_NEWER
            System.Runtime.ExceptionServices.ExceptionDispatchInfo _capturedInfo;
#endif
#if PROMISE_DEBUG
            RejectionException _rejectException;
            // Stack traces of recursive callbacks.
            private CausalityTrace _stackTraces;

            public void SetCreatedAndRejectedStacktrace(StackTrace rejectedStacktrace, CausalityTrace createdStacktraces)
            {
                ThrowIfInPool(this);
                _stackTraces = createdStacktraces;
                // rejectedStacktrace will only be non-null when this is created from Deferred.Reject and causality traces are enabled.
                // Otherwise, _rejectException will have been gotten from _rejectExceptionsForTrace in GetOrCreate.
                if (rejectedStacktrace != null)
                {
                    _rejectException = new RejectionException("This exception contains the stacktrace of the Deferred.Reject for the uncaught exception.", FormatStackTrace(new StackTrace[1] { rejectedStacktrace }), value);
#if CSHARP_7_3_OR_NEWER
                    _rejectExceptionsForTrace.Add(value, _rejectException);
#endif
                }
            }
#endif

            private RejectionContainerException() { }

            internal static RejectionContainerException GetOrCreate(Exception value)
            {
                var container = ObjectPool<RejectionContainerException>.TryTake<RejectionContainerException>()
                    ?? new RejectionContainerException();
                container.value = value;
#if CSHARP_7_3_OR_NEWER
                container._capturedInfo = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(value);
#if PROMISE_DEBUG
                _rejectExceptionsForTrace.TryGetValue(value, out container._rejectException);
#endif
#endif
                container.Reset();
                return container;
            }

            internal override Promise.State GetState()
            {
                return Promise.State.Rejected;
            }

            internal override void Release()
            {
                ThrowIfInPool(this);
                if (TryReleaseComplete())
                {
                    Dispose();
                }
            }

            internal override void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                if (shouldAdd)
                {
                    AddToUnhandledStack();
                }
                Release();
            }

            internal override void AddToUnhandledStack()
            {
                AddUnhandledException(ToException());
            }

            private void Dispose()
            {
#if PROMISE_DEBUG
                _rejectException = null;
                _stackTraces = null;
#if CSHARP_7_3_OR_NEWER
                _rejectExceptionsForTrace.Remove(value);
#endif
#endif
                value = null;
                ObjectPool<RejectionContainerException>.MaybeRepool(this);
            }

            private UnhandledException ToException()
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG
                return new UnhandledExceptionInternal(Value, "An exception was not handled." + CausalityTraceMessage, _stackTraces.ToString(), _rejectException ?? value);
#else
                return new UnhandledExceptionInternal(Value, "An exception was not handled." + CausalityTraceMessage, null, value);
#endif
            }

#if CSHARP_7_3_OR_NEWER
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

            ValueContainer IRejectionToContainer.ToContainer(ITraceable traceable)
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
        internal sealed class RethrownRejectionContainer : ValueContainer<object>, ILinked<RethrownRejectionContainer>, IRejectValueContainer, IRejectionToContainer, ICantHandleException
        {
            RethrownRejectionContainer ILinked<RethrownRejectionContainer>.Next { get; set; }

            private UnhandledExceptionInternal _exception;

#if PROMISE_DEBUG
            public void SetCreatedAndRejectedStacktrace(StackTrace rejectedStacktrace, CausalityTrace createdStacktraces)
            {
                ThrowIfInPool(this);
            }
#endif

            private RethrownRejectionContainer() { }

            internal static RethrownRejectionContainer GetOrCreate(UnhandledExceptionInternal exception)
            {
                var container = ObjectPool<RethrownRejectionContainer>.TryTake<RethrownRejectionContainer>()
                    ?? new RethrownRejectionContainer();
                container.value = exception.Value;
                container._exception = exception;
                container.Reset();
                return container;
            }

            internal override Promise.State GetState()
            {
                return Promise.State.Rejected;
            }

            internal override void Release()
            {
                ThrowIfInPool(this);
                if (TryReleaseComplete())
                {
                    Dispose();
                }
            }

            internal override void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                if (shouldAdd)
                {
                    AddToUnhandledStack();
                }
                Release();
            }

            internal override void AddToUnhandledStack()
            {
                AddUnhandledException(ToException());
            }

            private void Dispose()
            {
                value = null;
                _exception = null;
                ObjectPool<RethrownRejectionContainer>.MaybeRepool(this);
            }

            private UnhandledException ToException()
            {
                ThrowIfInPool(this);
                return _exception;
            }

#if CSHARP_7_3_OR_NEWER
            System.Runtime.ExceptionServices.ExceptionDispatchInfo IRejectValueContainer.GetExceptionDispatchInfo()
            {
                ThrowIfInPool(this);
                return System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(value as Exception ?? ToException());
            }
#else
            Exception IRejectValueContainer.GetException()
            {
                ThrowIfInPool(this);
                return ToException();
            }
#endif

            ValueContainer IRejectionToContainer.ToContainer(ITraceable traceable)
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

        internal interface IConstructor<T>
        {
            T Construct();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal abstract class SingletonValueContainer<TValueContainer, TConstructor> : ValueContainer<VoidResult>
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            , ILinked<TValueContainer>
#endif
            where TValueContainer : SingletonValueContainer<TValueContainer, TConstructor>
            where TConstructor : struct, IConstructor<TValueContainer>
        {
            // This is to help with internal debugging. When not debugging, a single instance can be reused for efficiency.

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            TValueContainer ILinked<TValueContainer>.Next { get; set; }
#else
            // We can reuse the same object.
            private static readonly TValueContainer _instance = default(TConstructor).Construct();
#endif

            protected static TValueContainer GetOrCreateBase()
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                var container = ObjectPool<TValueContainer>.TryTake<TValueContainer>()
                    ?? default(TConstructor).Construct();
                container.Reset();
                return container;
#else
                return _instance;
#endif
            }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            internal override void Release()
            {
                if (TryReleaseComplete())
                {
                    ObjectPool<TValueContainer>.MaybeRepool((TValueContainer) this);
                }
            }

            internal override void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                Release();
            }
#else
            internal override void Retain() { }
            internal override void Release() { }
            internal override void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd) { }
#endif

            internal override void AddToUnhandledStack() { }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class CancelContainerVoid : SingletonValueContainer<CancelContainerVoid, CancelContainerVoid.Constructor>
        {
            internal struct Constructor : IConstructor<CancelContainerVoid>
            {
                [MethodImpl(InlineOption)]
                public CancelContainerVoid Construct()
                {
                    return new CancelContainerVoid();
                }
            }

            [MethodImpl(InlineOption)]
            internal static CancelContainerVoid GetOrCreate()
            {
                return GetOrCreateBase();
            }

            internal override Promise.State GetState()
            {
                return Promise.State.Canceled;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class ResolveContainer<T> : ValueContainer<T>, ILinked<ResolveContainer<T>>
        {
            ResolveContainer<T> ILinked<ResolveContainer<T>>.Next { get; set; }

            private ResolveContainer() { }

            internal static ResolveContainer<T> GetOrCreate(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                T value)
            {
                var container = ObjectPool<ResolveContainer<T>>.TryTake<ResolveContainer<T>>()
                    ?? new ResolveContainer<T>();
                container.value = value;
                container.Reset();
                return container;
            }

            internal override Promise.State GetState()
            {
                return Promise.State.Resolved;
            }

            internal override void Release()
            {
                ThrowIfInPool(this);
                if (TryReleaseComplete())
                {
                    Dispose();
                }
            }

            internal override void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                Release();
            }

            internal override void AddToUnhandledStack() { }

            private void Dispose()
            {
                value = default(T);
                ObjectPool<ResolveContainer<T>>.MaybeRepool(this);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class ResolveContainerVoid : SingletonValueContainer<ResolveContainerVoid, ResolveContainerVoid.Constructor>
        {
            internal struct Constructor : IConstructor<ResolveContainerVoid>
            {
                [MethodImpl(InlineOption)]
                public ResolveContainerVoid Construct()
                {
                    return new ResolveContainerVoid();
                }
            }

            [MethodImpl(InlineOption)]
            internal static ResolveContainerVoid GetOrCreate()
            {
                return GetOrCreateBase();
            }

            internal override Promise.State GetState()
            {
                return Promise.State.Resolved;
            }
        }
    }
}