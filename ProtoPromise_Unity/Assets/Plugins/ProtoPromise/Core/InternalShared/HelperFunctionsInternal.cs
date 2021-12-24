#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0044 // Add readonly modifier

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    /// <summary>
    /// Members of this type are meant for INTERNAL USE ONLY! Do not use in user code! Use the documented public APIs.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    internal static partial class Internal
    {
        // This is used to optimize foreground synchronization so that multiple promises can use a single SynchronizationContext.Post call.
        volatile internal static SynchronizationHandler _foregroundSynchronizationHandler;

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed partial class SynchronizationHandler : ILinked<ITreeHandleable>
        {
            private static readonly SendOrPostCallback _synchronizationContextCallback = ExecuteFromContext;

            ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

            internal readonly SynchronizationContext _context;
            // These must not be readonly.
            private ValueWriteOnlyLinkedQueue<ITreeHandleable> _handleQueue;
            private SpinLocker _locker;
            volatile private bool _isScheduled = false;

            internal SynchronizationHandler(SynchronizationContext synchronizationContext)
            {
                _context = synchronizationContext;
                _handleQueue = new ValueWriteOnlyLinkedQueue<ITreeHandleable>(this);
                InitProgress();
            }

            private static void ExecuteFromContext(object state)
            {
                ((SynchronizationHandler) state).Execute();
            }

            internal void PostHandleable(ITreeHandleable handleable)
            {
                _locker.Enter();
                bool wasScheduled = _isScheduled;
                _isScheduled = true;
                _handleQueue.Enqueue(handleable);
                _locker.Exit();

                if (!wasScheduled)
                {
                    _context.Post(_synchronizationContextCallback, this);
                }
            }

            internal void Execute()
            {
                ValueLinkedQueue<IProgressInvokable> progressStack = new ValueLinkedQueue<IProgressInvokable>();
                _locker.Enter();
                ValueLinkedStack<ITreeHandleable> handleStack = _handleQueue.MoveElementsToStack();
                TakeProgress(ref progressStack);
                _isScheduled = false;
                _locker.Exit();

                new ExecutionScheduler(this, handleStack, progressStack).Execute();
            }

            partial void InitProgress();
            partial void TakeProgress(ref ValueLinkedQueue<IProgressInvokable> progressStack);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal
#if CSHARP_7_3_OR_NEWER
            ref // Force to use only on the CPU stack.
#endif
            partial struct ExecutionScheduler
        {
            private static readonly WaitCallback _threadPoolCallback = ExecuteFromContext;
            private static readonly SendOrPostCallback _synchronizationContextCallback = ExecuteFromContext;

            internal ValueLinkedStack<ITreeHandleable> _handleStack;
            private readonly SynchronizationHandler _synchronizationHandler;
#if PROMISE_PROGRESS
            private ValueLinkedQueue<IProgressInvokable> _progressQueue;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            private bool _isExecutingProgress;
#endif
#endif

            [MethodImpl(InlineOption)]
            internal ExecutionScheduler(SynchronizationHandler synchronizationHandler, ValueLinkedStack<ITreeHandleable> handleStack, ValueLinkedQueue<IProgressInvokable> progressQueue)
            {
                _handleStack = handleStack;
                _synchronizationHandler = synchronizationHandler;
#if PROMISE_PROGRESS
                _progressQueue = progressQueue;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _isExecutingProgress = false;
#endif
#endif
            }

            [MethodImpl(InlineOption)]
            internal ExecutionScheduler(bool forHandleables) : this(null, !forHandleables) { }

            [MethodImpl(InlineOption)]
            private ExecutionScheduler(SynchronizationHandler synchronizationHandler, bool isExecutingProgress)
            {
                _handleStack = new ValueLinkedStack<ITreeHandleable>();
                _synchronizationHandler = null;
#if PROMISE_PROGRESS
                _progressQueue = new ValueLinkedQueue<IProgressInvokable>();
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _isExecutingProgress = isExecutingProgress;
#endif
#endif
            }

            [MethodImpl(InlineOption)]
            internal ExecutionScheduler GetEmptyCopy()
            {
                bool isExecutingProgress =
#if PROMISE_PROGRESS && (PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE)
                    _isExecutingProgress;
#else
                    false;
#endif
                return new ExecutionScheduler(_synchronizationHandler, isExecutingProgress);
            }

            internal void Execute()
            {
                // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
                ITreeHandleable lastExecuted = null;
                try
                {
                    while (_handleStack.IsNotEmpty)
                    {
                        lastExecuted = _handleStack.Pop();
                        lastExecuted.Handle(ref this);
                    }
                }
                catch (Exception e)
                {
                    // This should never happen.
                    AddRejectionToUnhandledStack(e, lastExecuted as ITraceable);
                }
                ExecuteProgressPartial();
                MaybeReportUnhandledRejections();
            }

            partial void ExecuteProgressPartial();
            partial void AssertNotExecutingProgress();

            [MethodImpl(InlineOption)]
            internal void ScheduleSynchronous(ITreeHandleable handleable)
            {
                AssertNotExecutingProgress();
#if PROTO_PROMISE_DEVELOPER_MODE // Helps to see full causality trace with internal stacktraces in exceptions (may cause StackOverflowException if the chain is very long).
                handleable.Handle(ref this);
#else
                _handleStack.Push(handleable);
#endif
            }

            internal void ScheduleOnContext(SynchronizationContext synchronizationContext, ITreeHandleable handleable)
            {
                AssertNotExecutingProgress();
                if (_synchronizationHandler != null && _synchronizationHandler._context == synchronizationContext)
                {
                    // We're scheduling to the context that is currently executing, just place it on the stack instead of going through the context.
                    ScheduleSynchronous(handleable);
                    return;
                }
                ScheduleOnContextStatic(synchronizationContext, handleable);
            }

            internal static void ScheduleOnContextStatic(SynchronizationContext synchronizationContext, ITreeHandleable handleable)
            {
                if (synchronizationContext == null)
                {
                    // If there is no context, send it to the ThreadPool.
                    ThreadPool.QueueUserWorkItem(_threadPoolCallback, handleable);
                    return;
                }
                SynchronizationHandler foregroundHandler = _foregroundSynchronizationHandler;
                if (foregroundHandler != null && foregroundHandler._context == synchronizationContext)
                {
                    // Schedule on the optimized foregroundHandler instead of going through the context.
                    foregroundHandler.PostHandleable(handleable);
                    return;
                }
                synchronizationContext.Post(_synchronizationContextCallback, handleable);
            }

            private static void ExecuteFromContext(object state)
            {
                // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
                try
                {
                    ExecutionScheduler executionScheduler = new ExecutionScheduler(true);
                    ((ITreeHandleable) state).Handle(ref executionScheduler);
                    executionScheduler.Execute();
                }
                catch (Exception e)
                {
                    // This should never happen.
                    AddRejectionToUnhandledStack(e, state as ITraceable);
                }
            }
        }

        [MethodImpl(InlineOption)]
        internal static IValueContainer CreateResolveContainer<TValue>(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TValue value, int retainCount)
        {
            // null check is same as typeof(TValue).IsValueType, but is actually optimized away by the JIT. This prevents the type check when TValue is a reference type.
            if (null != default(TValue) && typeof(TValue) == typeof(VoidResult))
            {
                return ResolveContainerVoid.GetOrCreate(retainCount);
            }
            return ResolveContainer<TValue>.GetOrCreate(value, retainCount);
        }

        // IValueContainer.(Try)GetValue<TValue>() must be implemented as extensions instead of interface members, because AOT might not compile the virtual methods when TValue is a value-type.
        [MethodImpl(InlineOption)]
        internal static TValue GetValue<TValue>(this IValueContainer valueContainer)
        {
            // null check is same as typeof(TValue).IsValueType, but is actually optimized away by the JIT. This prevents the type check when TValue is a reference type.
            if (null != default(TValue) && typeof(TValue) == typeof(VoidResult))
            {
                return default(TValue);
            }
            // TODO: check typeof(TValue).IsValueType == false and use the PromiseRef as the value container for reference types.
            return ((ResolveContainer<TValue>) valueContainer).value;
        }

        internal static bool TryGetValue<TValue>(this IValueContainer valueContainer, out TValue converted)
        {
            // null check is same as typeof(TValue).IsValueType, but is actually optimized away by the JIT. This prevents the type check when TValue is a reference type.
            if (null != default(TValue) && typeof(TValue) == typeof(VoidResult))
            {
                converted = default(TValue);
                return true;
            }

            // Try to avoid boxing value types.
#if CSHARP_7_3_OR_NEWER
            if (valueContainer is IValueContainer<TValue> directContainer)
#else
            var directContainer = valueContainer as IValueContainer<TValue>;
            if (directContainer != null)
#endif
            {
                converted = directContainer.Value;
                return true;
            }

            if (typeof(TValue).IsAssignableFrom(valueContainer.ValueType))
            {
                // Unfortunately, this will box if converting from a non-nullable value type to nullable.
                // I couldn't find any way around that without resorting to Expressions (which won't work for this purpose with the IL2CPP AOT compiler).
                // Also, this will only occur when catching rejections, so the performance concern is negated.
                converted = (TValue) valueContainer.Value;
                return true;
            }

            converted = default(TValue);
            return false;
        }

        internal static IRejectValueContainer CreateRejectContainer<TReject>(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TReject reason, int rejectSkipFrames, ITraceable traceable)
        {
            IRejectValueContainer valueContainer;

            // Avoid boxing value types.
            Type type = typeof(TReject);
            if (type.IsValueType)
            {
                valueContainer = RejectionContainer<TReject>.GetOrCreate(reason, 0);
            }
            else
            {
#if CSHARP_7_3_OR_NEWER
                if (reason is IRejectionToContainer internalRejection)
#else
                IRejectionToContainer internalRejection = reason as IRejectionToContainer;
                if (internalRejection != null)
#endif
                {
                    // reason is an internal rejection object, get its container instead of wrapping it.
                    return internalRejection.ToContainer(traceable);
                }

                // If reason is null, behave the same way .Net behaves if you throw null.
                object o = reason == null ? new NullReferenceException() : (object) reason;
                // Only need to create one object pool for reference types.
                valueContainer = RejectionContainer<object>.GetOrCreate(o, 0);
            }
            SetCreatedAndRejectedStacktrace(valueContainer, rejectSkipFrames + 1, traceable);
            return valueContainer;
        }

        internal static ICancelValueContainer CreateCancelContainer<TCancel>(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TCancel reason)
        {
            ICancelValueContainer cancelValue;
            if (typeof(TCancel).IsValueType)
            {
                cancelValue = CancelContainer<TCancel>.GetOrCreate(reason, 0);
            }
            else
            {
#if CSHARP_7_3_OR_NEWER
                if (reason is ICancelationToContainer internalCancelation)
#else
                ICancelationToContainer internalCancelation = reason as ICancelationToContainer;
                if (internalCancelation != null)
#endif
                {
                    // reason is an internal cancelation object, get its container instead of wrapping it.
                    cancelValue = internalCancelation.ToContainer();
                }
                else if (reason == null || reason is OperationCanceledException)
                {
                    // Use void container instead of wrapping OperationCanceledException, or if reason is null.
                    cancelValue = CancelContainerVoid.GetOrCreate(0);
                }
                else
                {
                    // Only need to create one object pool for reference types.
                    object o = reason;
                    cancelValue = CancelContainer<object>.GetOrCreate(o, 0);
                }
            }
            return cancelValue;
        }

        // Handle uncaught errors. These must not be readonly.
        private static ValueLinkedStack<UnhandledException> _unhandledExceptions = new ValueLinkedStack<UnhandledException>();
        private static SpinLocker _unhandledExceptionsLocker;

        internal static void AddUnhandledException(UnhandledException exception)
        {
            _unhandledExceptionsLocker.Enter();
            _unhandledExceptions.Push(exception);
            _unhandledExceptionsLocker.Exit();
        }

        internal static void AddRejectionToUnhandledStack(object unhandledValue, ITraceable traceable)
        {
#if CSHARP_7_3_OR_NEWER
            if (unhandledValue is ICantHandleException ex)
#else
            ICantHandleException ex = unhandledValue as ICantHandleException;
            if (ex != null)
#endif
            {
                ex.AddToUnhandledStack(traceable);
                return;
            }

            if (unhandledValue == null)
            {
                // unhandledValue is null, behave the same way .Net behaves if you throw null.
                unhandledValue = new NullReferenceException();
            }

            Type type = unhandledValue.GetType();
            Exception innerException = unhandledValue as Exception;
            string message = innerException != null ? "An exception was not handled." : "A rejected value was not handled, type: " + type + ", value: " + unhandledValue.ToString();

            AddUnhandledException(new UnhandledExceptionInternal(unhandledValue, type, message + CausalityTraceMessage, GetFormattedStacktrace(traceable), innerException));
        }

        [MethodImpl(InlineOption)]
        internal static void MaybeReportUnhandledRejections()
        {
            // If Promise.Config.UncaughtRejectionHandler is not set, unhandled rejections will continue to pile up until it is set.
            MaybeReportUnhandledRejections(Promise.Config.UncaughtRejectionHandler);
        }

        internal static void MaybeReportUnhandledRejections(Action<UnhandledException> handler)
        {
            if (handler == null)
            {
                // TODO: throw in background thread instead of letting them pile up.
                return;
            }

            _unhandledExceptionsLocker.Enter();
            var unhandledExceptions = _unhandledExceptions;
            _unhandledExceptions = new ValueLinkedStack<UnhandledException>();
            _unhandledExceptionsLocker.Exit();

            while (unhandledExceptions.IsNotEmpty)
            {
                handler.Invoke(unhandledExceptions.Pop());
            }
        }

        internal static bool InterlockedAddIfNotEqual(ref int location, int value, int comparand, out int newValue)
        {
            Thread.MemoryBarrier();
            int initialValue;
            do
            {
                initialValue = location;
                newValue = initialValue + value;
                if (initialValue == comparand) return false;
            }
            while (Interlocked.CompareExchange(ref location, newValue, initialValue) != initialValue);
            return true;
        }

        [MethodImpl(InlineOption)]
        internal static bool TryUnregisterAndIsNotCanceling(ref CancelationRegistration cancelationRegistration)
        {
            bool isCanceling;
            bool unregistered = cancelationRegistration.TryUnregister(out isCanceling);
            return unregistered | !isCanceling;
        }

        internal static int BuildHashCode(object _ref, int hashcode1, int hashcode2)
        {
            if (_ref == null)
            {
                return 0;
            }
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _ref.GetHashCode();
                hash = hash * 31 + hashcode1;
                hash = hash * 31 + hashcode2;
                return hash;
            }
        }
    } // class Internal
} // namespace Proto.Promises