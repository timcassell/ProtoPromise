#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    internal static partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal abstract class ValueContainer
        {
            internal abstract Promise.State GetState();
            internal abstract Type ValueType { get; }
            internal abstract object Value { get; }

            internal abstract void DisposeAndMaybeAddToUnhandledStack(bool shouldAdd);
            internal abstract void AddToUnhandledStack();

            internal abstract ValueContainer Clone();

            // TODO: when types implement IValueTaskSource<TResult>, they will have access to their <TResult> to call this direct function which is more efficient than the virtual call.

            //[MethodImpl(InlineOption)]
            //internal ResolveContainer<T> CloneResolve<T>()
            //{
            //    return ResolveContainer<T>.GetOrCreate(GetValue<T>());
            //}

            [MethodImpl(InlineOption)]
            internal static ValueContainer CreateResolve<TValue>(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TValue value)
            {
                // Null check helps eliminate the IsValueType check for reference types. IsValueType is necessary to detect nullable value types.
                if (null == default(TValue) && !typeof(TValue).IsValueType)
                {
                    // Only need to create one object pool for reference types.
                    return ResolveContainer<object>.GetOrCreate(value);
                }
                if (typeof(TValue) == typeof(VoidResult))
                {
                    return ResolveContainerVoid.GetOrCreate();
                }
                return ResolveContainer<TValue>.GetOrCreate(value);
            }

            [MethodImpl(InlineOption)]
            internal TValue GetValue<TValue>()
            {
                // Null check helps eliminate the IsValueType check for reference types. IsValueType is necessary to detect nullable value types.
                if (null == default(TValue) && !typeof(TValue).IsValueType)
                {
                    return (TValue) ((ResolveContainer<object>) this).value;
                }
                if (typeof(TValue) == typeof(VoidResult))
                {
                    return default(TValue);
                }
                return ((ResolveContainer<TValue>) this).value;
            }

            internal static ValueContainer CreateReject<TReject>(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TReject reason, int rejectSkipFrames, ITraceable traceable)
            {
                ValueContainer valueContainer;

                // Avoid boxing value types.
                Type type = typeof(TReject);
                if (type.IsValueType)
                {
                    valueContainer = RejectionContainer<TReject>.GetOrCreate(reason);
                }
                else
                {
                    IRejectionToContainer internalRejection = reason as IRejectionToContainer;
                    if (internalRejection != null)
                    {
                        // reason is an internal rejection object, get its container instead of wrapping it.
                        return internalRejection.ToContainer(traceable);
                    }

                    // If reason is null, behave the same way .Net behaves if you throw null.
                    object o = (object) reason ?? new NullReferenceException();
                    Exception e = o as Exception;
                    if (e != null)
                    {
                        valueContainer = RejectionContainerException.GetOrCreate(e);
                    }
                    else
                    {
                        // Only need to create one object pool for reference types.
                        valueContainer = RejectionContainer<object>.GetOrCreate(o);
                    }
                }
                SetCreatedAndRejectedStacktrace((IRejectValueContainer) valueContainer, rejectSkipFrames + 1, traceable);
                return valueContainer;
            }

            internal bool TryGetValue<TValue>(out TValue converted)
            {
                // null check is same as typeof(TValue).IsValueType, but is actually optimized away by the JIT. This prevents the type check when TValue is a reference type.
                if (null != default(TValue) && typeof(TValue) == typeof(VoidResult))
                {
                    converted = default(TValue);
                    return true;
                }

                // Try to avoid boxing value types.
                var directContainer = this as ValueContainer<TValue>;
                if (directContainer != null)
                {
                    converted = directContainer.value;
                    return true;
                }

                if (typeof(TValue).IsAssignableFrom(ValueType))
                {
                    // Unfortunately, this will box if converting from a non-nullable value type to nullable.
                    // I couldn't find any way around that without resorting to Expressions (which won't work for this purpose with the IL2CPP AOT compiler).
                    // Also, this will only occur when catching rejections, so the performance concern is negated.
                    converted = (TValue) Value;
                    return true;
                }

                converted = default(TValue);
                return false;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal abstract class ValueContainer<T> : ValueContainer, ITraceable
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif
            public T value;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            volatile private bool _disposed;

            ~ValueContainer()
            {
                try
                {
                    if (!_disposed)
                    {
                        // For debugging. This should never happen.
                        string message = "A " + GetType() + " was garbage collected without it being released. value: " + value;
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

            [MethodImpl(InlineOption)]
            internal override void DisposeAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _disposed = true;
#endif
            }

            [MethodImpl(InlineOption)]
            protected void Reset()
            {
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

            internal override ValueContainer Clone()
            {
                var clone = GetOrCreate(value);
#if PROMISE_DEBUG
                clone.SetCreatedAndRejectedStacktrace(_rejectedStackTrace, _stackTraces);
#endif
                return clone;
            }

            internal override void DisposeAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                ThrowIfInPool(this);
                if (shouldAdd)
                {
                    AddToUnhandledStack();
                }
                base.DisposeAndMaybeAddToUnhandledStack(shouldAdd);
                Dispose();
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

#if !NET_LEGACY
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
#if PROMISE_DEBUG && !NET_LEGACY
            // This is used to reconstruct the rejection causality trace when the original exception is rethrown from await in an async function, and this container is lost.
            private static readonly ConditionalWeakTable<Exception, RejectionException> _rejectExceptionsForTrace = new ConditionalWeakTable<Exception, RejectionException>();
#endif

            RejectionContainerException ILinked<RejectionContainerException>.Next { get; set; }

#if !NET_LEGACY
            System.Runtime.ExceptionServices.ExceptionDispatchInfo _capturedInfo;
#endif
#if PROMISE_DEBUG
            private RejectionException _rejectException;
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
#if !NET_LEGACY
                    _rejectExceptionsForTrace.Add(value, _rejectException);
#endif
                }
            }
#endif

            private RejectionContainerException() { }

            private static RejectionContainerException GetOrCreate()
            {
                var container = ObjectPool<RejectionContainerException>.TryTake<RejectionContainerException>()
                    ?? new RejectionContainerException();
                container.Reset();
                return container;
            }

            internal static RejectionContainerException GetOrCreate(Exception value)
            {
                var container = GetOrCreate();
                container.value = value;
#if !NET_LEGACY
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

            internal override ValueContainer Clone()
            {
                var clone = GetOrCreate();
                clone.value = value;
#if !NET_LEGACY
                clone._capturedInfo = _capturedInfo;
#endif
#if PROMISE_DEBUG
                clone._rejectException = _rejectException;
                clone._stackTraces = _stackTraces;
#endif
                return clone;
            }

            internal override void DisposeAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                ThrowIfInPool(this);
                if (shouldAdd)
                {
                    AddToUnhandledStack();
                }
                base.DisposeAndMaybeAddToUnhandledStack(shouldAdd);
                Dispose();
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
#if !NET_LEGACY
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

#if !NET_LEGACY
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

            internal override ValueContainer Clone()
            {
                return GetOrCreate(_exception);
            }

            internal override void DisposeAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                ThrowIfInPool(this);
                if (shouldAdd)
                {
                    AddToUnhandledStack();
                }
                base.DisposeAndMaybeAddToUnhandledStack(shouldAdd);
                Dispose();
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

#if !NET_LEGACY
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
            internal override ValueContainer Clone()
            {
                var clone = GetOrCreateBase();
                clone.value = value;
                return clone;
            }

            internal override void DisposeAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                ThrowIfInPool(this);
                base.DisposeAndMaybeAddToUnhandledStack(shouldAdd);
                ObjectPool<TValueContainer>.MaybeRepool((TValueContainer) this);
            }
#else
            internal override ValueContainer Clone()
            {
                return this;
            }

            internal override void DisposeAndMaybeAddToUnhandledStack(bool shouldAdd) { }
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

            internal override ValueContainer Clone()
            {
                return GetOrCreate(value);
            }

            internal override void DisposeAndMaybeAddToUnhandledStack(bool shouldAdd)
            {
                base.DisposeAndMaybeAddToUnhandledStack(shouldAdd);
                Dispose();
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