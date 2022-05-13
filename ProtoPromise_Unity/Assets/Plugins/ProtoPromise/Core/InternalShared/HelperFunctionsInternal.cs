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
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections.Generic;
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
        internal sealed partial class SynchronizationHandler : ILinked<HandleablePromiseBase>
        {
            private static readonly SendOrPostCallback _synchronizationContextCallback = ExecuteFromContext;

            HandleablePromiseBase ILinked<HandleablePromiseBase>.Next { get; set; }

            internal readonly SynchronizationContext _context;
            // These must not be readonly.
            private ValueWriteOnlyLinkedQueue<HandleablePromiseBase> _handleQueue;
            private SpinLocker _locker;
            volatile private bool _isScheduled = false;

            internal SynchronizationHandler(SynchronizationContext synchronizationContext)
            {
                _context = synchronizationContext;
                _handleQueue = new ValueWriteOnlyLinkedQueue<HandleablePromiseBase>(this);
                InitProgress();
            }

            private static void ExecuteFromContext(object state)
            {
                state.UnsafeAs<SynchronizationHandler>().Execute();
            }

            internal void PostHandleable(HandleablePromiseBase handleable)
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
                ValueLinkedStack<HandleablePromiseBase> handleStack = _handleQueue.MoveElementsToStack();
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

            internal ValueLinkedStack<HandleablePromiseBase> _handleStack;
            private readonly SynchronizationHandler _synchronizationHandler;
#if PROMISE_PROGRESS
            private ValueLinkedQueue<IProgressInvokable> _progressQueue;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            private bool _isExecutingProgress;
#endif
#endif

            [MethodImpl(InlineOption)]
            internal ExecutionScheduler(SynchronizationHandler synchronizationHandler, ValueLinkedStack<HandleablePromiseBase> handleStack, ValueLinkedQueue<IProgressInvokable> progressQueue)
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
                _handleStack = new ValueLinkedStack<HandleablePromiseBase>();
                _synchronizationHandler = null;
#if PROMISE_PROGRESS
                _progressQueue = new ValueLinkedQueue<IProgressInvokable>();
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _isExecutingProgress = isExecutingProgress;
#endif
#endif
            }

            internal void Execute()
            {
                // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
                HandleablePromiseBase lastExecuted = null;
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
                    ReportRejection(e, lastExecuted as ITraceable);
                }
                ExecuteProgressPartial();
            }

            partial void ExecuteProgressPartial();
            partial void AssertNotExecutingProgress();

            [MethodImpl(InlineOption)]
            internal void ScheduleSynchronous(HandleablePromiseBase handleable)
            {
                AssertNotExecutingProgress();
#if PROTO_PROMISE_STACK_UNWIND_DISABLE && PROTO_PROMISE_DEVELOPER_MODE
                handleable.Handle(ref this);
#else
                _handleStack.Push(handleable);
#endif
            }

            internal void ScheduleOnContext(SynchronizationContext synchronizationContext, HandleablePromiseBase handleable)
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

            internal static void ScheduleOnContextStatic(SynchronizationContext synchronizationContext, HandleablePromiseBase handleable)
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
                    var executionScheduler = new ExecutionScheduler(true);
                    state.UnsafeAs<HandleablePromiseBase>().Handle(ref executionScheduler);
                    executionScheduler.Execute();
                }
                catch (Exception e)
                {
                    // This should never happen.
                    ReportRejection(e, state as ITraceable);
                }
            }
        }

        internal static RejectContainer CreateRejectContainer<TReject>(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TReject reason, int rejectSkipFrames, ITraceable traceable)
        {
            return RejectContainer.Create(reason, rejectSkipFrames, traceable);
        }

        internal static void ReportRejection(object unhandledValue, ITraceable traceable)
        {
            ICantHandleException ex = unhandledValue as ICantHandleException;
            if (ex != null)
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

            ReportUnhandledException(new UnhandledExceptionInternal(unhandledValue, message + CausalityTraceMessage, GetFormattedStacktrace(traceable), innerException));
        }

        internal static void ReportUnhandledException(UnhandledException exception)
        {
#if PROTO_PROMISE_DEVELOPER_MODE
            exception = new UnhandledExceptionInternal(exception.Value, "Unhandled Exception added at (stacktrace in this exception)", new StackTrace(1, true).ToString(), exception);
#endif
            // Send to the handler if it exists.
            Action<UnhandledException> handler = Promise.Config.UncaughtRejectionHandler;
            if (handler != null)
            {
                handler.Invoke(exception);
                return;
            }

            // Otherwise, throw it in the ForegroundContext if it exists, or background if it doesn't.
            SynchronizationContext synchronizationContext = Promise.Config.ForegroundContext ?? Promise.Config.BackgroundContext;
            if (synchronizationContext != null)
            {
                synchronizationContext.Post(e => { throw (UnhandledException) e; }, exception);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(e => { throw (UnhandledException) e; }, exception);
            }
        }

        [MethodImpl(InlineOption)]
        private static long InterlockedAddWithOverflowCheck(ref long location, long value, long comparand)
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            long initialValue, newValue;
            do
            {
                initialValue = Interlocked.Read(ref location);
                if (initialValue == comparand)
                {
                    throw new OverflowException(); // This should never happen, but checking just in case.
                }
                newValue = initialValue + value;
            } while (Interlocked.CompareExchange(ref location, newValue, initialValue) != initialValue);
            return newValue;
#else
            return Interlocked.Add(ref location, value);
#endif
        }

        [MethodImpl(InlineOption)]
        private static int InterlockedAddWithOverflowCheck(ref int location, int value, int comparand)
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            Thread.MemoryBarrier();
            int initialValue, newValue;
            do
            {
                initialValue = location;
                if (initialValue == comparand)
                {
                    throw new OverflowException(); // This should never happen, but checking just in case.
                }
                newValue = initialValue + value;
            } while (Interlocked.CompareExchange(ref location, newValue, initialValue) != initialValue);
            return newValue;
#else
            return Interlocked.Add(ref location, value);
#endif
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
            int hashcode0 = _ref == null ? 0 : _ref.GetHashCode();
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + hashcode0;
                hash = hash * 31 + hashcode1;
                hash = hash * 31 + hashcode2;
                return hash;
            }
        }

        internal static int BuildHashCode(object _ref, int hashcode1, int hashcode2, int hashcode3)
        {
            unchecked
            {
                return BuildHashCode(_ref, hashcode1, hashcode2) * 31 + hashcode3;
            }
        }

        [MethodImpl(InlineOption)]
        internal static T UnsafeAs<T>(this object o) where T : class
        {
#if NET5_0_OR_GREATER && !PROMISE_DEBUG && !PROTO_PROMISE_DEVELOPER_MODE
            return Unsafe.As<T>(o);
#else
            return (T) o;
#endif
        }
    } // class Internal
} // namespace Proto.Promises