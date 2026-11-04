#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0290 // Use primary constructor

using Proto.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static partial class Internal
    {
#if NET8_0_OR_GREATER
        // The thread's ExecutionContext field and the internals of ExecutionContext that its internal CaptureForRestore and RestoreInternal use,
        // which capture and restore it even while its flow is suppressed, like the runtime's AsyncTaskMethodBuilder.Start does
        // (but unlike it, we don't restore the SynchronizationContext, see ExecutionContextScope).
        // They're used directly, like AsyncTaskMethodBuilder.Start does, so that the thread is only looked up once,
        // and because Mono on .Net 8 crashes calling an accessor of a static method that has no arguments and returns a reference type (CaptureForRestore).
        // The default ExecutionContext is null on the thread.
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_executionContext")]
        private static extern ref ExecutionContext ThreadExecutionContext(Thread thread);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "m_isDefault")]
        private static extern ref bool IsDefault(ExecutionContext executionContext);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "RestoreChangedContextToThread")]
        private static extern void RestoreChangedContextToThread(ExecutionContext _, Thread currentThread, ExecutionContext contextToRestore, ExecutionContext currentContext);

        // Sets the ExecutionContext on the thread, and restores the previous one when it's disposed, after the caller runs its code.
        // It's used instead of ExecutionContext.Run, so that the code is called directly instead of through a delegate,
        // and so that ExecutionContext.Run's frames aren't in causality traces that are captured in it.
        // The code catches its exceptions, so the previous ExecutionContext is restored without a finally. Unlike ExecutionContext.Run,
        // the SynchronizationContext isn't restored, the same as when the code runs without an ExecutionContext.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly ref struct ExecutionContextScope
        {
            private readonly Thread _thread;
            private readonly ExecutionContext _previousExecutionContext;

            [MethodImpl(InlineOption)]
            internal ExecutionContextScope(ExecutionContext executionContext)
            {
                Debug.Assert(executionContext != null);
                var thread = Thread.CurrentThread;
                var currentExecutionContext = ThreadExecutionContext(thread);
                _thread = thread;
                _previousExecutionContext = currentExecutionContext;
                // ExecutionContext.Capture() returns the default ExecutionContext instead of null.
                if (IsDefault(executionContext))
                {
                    executionContext = null;
                }
                if (executionContext != currentExecutionContext)
                {
                    RestoreChangedContextToThread(null, thread, executionContext, currentExecutionContext);
                }
            }

            [MethodImpl(InlineOption)]
            internal void Dispose()
                => RestoreExecutionContext(_thread, _previousExecutionContext);
        }

        [MethodImpl(InlineOption)]
        private static void RestoreExecutionContext(Thread thread, ExecutionContext previousExecutionContext)
        {
            var currentExecutionContext = ThreadExecutionContext(thread);
            if (previousExecutionContext != currentExecutionContext)
            {
                RestoreChangedContextToThread(null, thread, previousExecutionContext, currentExecutionContext);
            }
        }

        // Starts the async function, and then restores the ExecutionContext, so that changes it made (e.g. setting an AsyncLocal<T>) don't flow to its caller.
        // It's used instead of AsyncTaskMethodBuilder.Start for the same reasons as ExecutionContextScope.
        internal static void StartAndRestoreExecutionContext<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            var thread = Thread.CurrentThread;
            var previousExecutionContext = ThreadExecutionContext(thread);
            stateMachine.MoveNext();
            RestoreExecutionContext(thread, previousExecutionContext);
        }
#endif

        internal static void ScheduleContextCallback(SynchronizationContext context, object state, SendOrPostCallback contextCallback, WaitCallback threadpoolCallback)
        {
            if (!CausalityTrace.IsEnabled)
            {
                ScheduleContextCallbackDirect(context, state, contextCallback, threadpoolCallback);
                return;
            }
            ScheduleContextCallbackInScope(context, state, contextCallback, threadpoolCallback);
        }

        // The callback is invoked in the scope of a trace of where it was scheduled, because what scheduled it (e.g. the code that completed an awaited promise)
        // is not on the stack when the callback runs on the context. If the callback enters its own scope, its own trace is the Previous trace
        // of traces captured inside it, and the trace of where it was scheduled is one of their outer traces (see CausalityTrace.OuterTraces).
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ScheduleContextCallbackInScope(SynchronizationContext context, object state, SendOrPostCallback contextCallback, WaitCallback threadpoolCallback)
            => ScheduleContextCallbackDirect(context,
                new ScopedContextCallback(state, contextCallback, threadpoolCallback, CausalityTrace.Capture()),
                ScopedContextCallback.s_contextCallback, ScopedContextCallback.s_threadpoolCallback);

        // Schedules the callback without capturing where it was scheduled. Used for callbacks that are scheduled when their object is created
        // (e.g. Promise.Run), whose own trace already shows where they were scheduled, and for hand-offs inside the library (e.g. between parallel workers).
        internal static void ScheduleContextCallbackDirect(SynchronizationContext context, object state, SendOrPostCallback contextCallback, WaitCallback threadpoolCallback)
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            if (context == null)
            {
                throw new System.InvalidOperationException("context cannot be null");
            }
#endif
#if PROTO_PROMISE_DEVELOPER_MODE
            // Callbacks are wrapped to catch bugs in the library, so that they are reported instead of crashing the test process.
            if (context == BackgroundSynchronizationContextSentinel.s_instance)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
                    try
                    {
                        threadpoolCallback.Invoke(state);
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ScopedContextCallback.Report(e, state);
                    }
                }, null);
            }
            else
            {
                context.Post(_ =>
                {
                    // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
                    try
                    {
                        contextCallback.Invoke(state);
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ScopedContextCallback.Report(e, state);
                    }
                }, null);
            }
#else
            if (context == BackgroundSynchronizationContextSentinel.s_instance)
            {
                ThreadPool.QueueUserWorkItem(threadpoolCallback, state);
            }
            else
            {
                context.Post(contextCallback, state);
            }
#endif
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class ScopedContextCallback
        {
            internal static readonly SendOrPostCallback s_contextCallback = InvokeContextCallback;
            internal static readonly WaitCallback s_threadpoolCallback = InvokeThreadpoolCallback;

            private readonly object _state;
            private readonly SendOrPostCallback _contextCallback;
            private readonly WaitCallback _threadpoolCallback;
            private readonly CausalityTrace _trace;

#if PROTO_PROMISE_DEVELOPER_MODE
            // Reports an exception that a scheduled callback threw (which should never happen) with the trace of what was scheduled,
            // or with the trace of where it was scheduled if it doesn't have one.
            internal static void Report(Exception exception, object state)
            {
                if (state is ScopedContextCallback callback)
                {
                    state = callback._state;
                    if (GetTrace(state) == null && !(exception is ICantHandleException))
                    {
                        ReportUnhandledValue(exception, callback._trace);
                        return;
                    }
                }
                ReportRejection(exception, state);
            }
#endif

            internal ScopedContextCallback(object state, SendOrPostCallback contextCallback, WaitCallback threadpoolCallback, CausalityTrace trace)
            {
                _state = state;
                _contextCallback = contextCallback;
                _threadpoolCallback = threadpoolCallback;
                _trace = trace;
            }

            private static void InvokeContextCallback(object obj)
            {
                var callback = obj.UnsafeAs<ScopedContextCallback>();
                var scope = CausalityTrace.EnterScopeInternal(callback._trace);
                try
                {
                    callback._contextCallback.Invoke(callback._state);
                }
                finally
                {
                    scope.Dispose();
                }
            }

            private static void InvokeThreadpoolCallback(object obj)
            {
                var callback = obj.UnsafeAs<ScopedContextCallback>();
                var scope = CausalityTrace.EnterScopeInternal(callback._trace);
                try
                {
                    callback._threadpoolCallback.Invoke(callback._state);
                }
                finally
                {
                    scope.Dispose();
                }
            }
        }

        // Reports an exception that user code threw, with a trace captured where it was caught, which shows what invoked the code.
        // The scope of the operation that invoked the code must still be entered, so that it's the trace's cause.
        internal static void ReportCaughtException(Exception exception, object traceable)
        {
            if (exception is ICantHandleException ex)
            {
                ex.ReportUnhandled(traceable);
                return;
            }
            // The exception may have been rethrown, so we use the trace of where it was originally rejected or thrown (see ExceptionRejectionContainer).
            if (!TryGetExceptionTrace(exception, out var trace))
            {
                trace = CausalityTrace.Capture();
            }
            ReportUnhandledValue(exception, trace);
        }

        internal static void ReportRejection(object unhandledValue, object traceable)
        {
            if (unhandledValue is ICantHandleException ex)
            {
                ex.ReportUnhandled(traceable);
                return;
            }

            ReportUnhandledValue(unhandledValue, GetTrace(traceable));
        }

        // For an unhandled value whose causality trace is not stored on a traceable object.
        internal static void ReportUnhandledValue(object unhandledValue, CausalityTrace causalityTrace)
        {
            if (unhandledValue == null)
            {
                // unhandledValue is null, behave the same way .Net behaves if you throw null.
                unhandledValue = new NullReferenceException();
            }

            Exception innerException = unhandledValue as Exception;
            string message = innerException != null
                ? "An exception was not handled."
                : $"A rejected value was not handled, type: {unhandledValue.GetType()}, value: {unhandledValue}";

            ReportUnhandledException(new UnhandledException(unhandledValue, message, causalityTrace, innerException));
        }

        internal static void ReportUnhandledException(UnhandledException exception)
        {
#if PROTO_PROMISE_DEVELOPER_MODE
            exception = new UnhandledException(exception.Value, "Unhandled Exception added at (stacktrace in this exception)", new Proto.Diagnostics.CausalityTrace(1), exception);
#endif
            // Send to the handler if it exists.
            Action<UnhandledException> handler = Promise.Config.UncaughtRejectionHandler;
            if (handler != null)
            {
                try
                {
                    handler.Invoke(exception);
                }
                catch (Exception e)
                {
                    // The handler is called from the library's code (e.g. while a promise is completing), so its exception must not escape into it.
                    // Instead, we report it the same way as if there was no handler.
                    PostThrow(state => ((System.Runtime.ExceptionServices.ExceptionDispatchInfo) state).Throw(),
                        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e));
                }
                return;
            }

            PostThrow(e => { throw (UnhandledException) e; }, exception);
        }

        // Throws the exception in the ForegroundContext if it exists, or background if it doesn't.
        private static void PostThrow(SendOrPostCallback throwCallback, object exception)
        {
            SynchronizationContext synchronizationContext = Promise.Config.ForegroundContext ?? Promise.Config.BackgroundContext;
            if (synchronizationContext != null)
            {
                synchronizationContext.Post(throwCallback, exception);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(throwCallback.Invoke, exception);
            }
        }

        [MethodImpl(InlineOption)]
        internal static int InterlockedAddWithUnsignedOverflowCheck(ref int location, int value)
        {
            // ints are treated as uints, we just use int because Interlocked does not support uint on old runtimes.
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            unchecked
            {
                // This is also used to subtract, so we have to convert it to a subtraction so the checked context won't throw for a uint add overflow.
                uint addOrSubtract = value == int.MinValue
                    ? int.MaxValue + 1u
                    : (uint) Math.Abs(value);
                int initialValue, newValue;
                do
                {
                    initialValue = Volatile.Read(ref location);
                    uint uValue = (uint) initialValue;
                    checked
                    {
                        if (value >= 0)
                        {
                            uValue += addOrSubtract;
                        }
                        else
                        {
                            uValue -= addOrSubtract;
                        }
                    }
                    newValue = (int) uValue;
                } while (Interlocked.CompareExchange(ref location, newValue, initialValue) != initialValue);
                return newValue;
            }
#else
            return Interlocked.Add(ref location, value);
#endif
        }

        [MethodImpl(InlineOption)]
        internal static T UnsafeAs<T>(this object o) where T : class
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            return (T) o;
#elif UNITY_2020_1_OR_NEWER
            return Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<object, T>(ref o);
#elif NET5_0_OR_GREATER
            return Unsafe.As<T>(o);
#else
            return (T) o;
#endif
        }

        internal static MaybeJoinedCancelationSource MaybeJoinCancelationTokens(CancelationToken first, CancelationToken second, out CancelationToken maybeJoinedToken)
        {
            if (first == second | !first.CanBeCanceled)
            {
                maybeJoinedToken = second;
                return default;
            }
            if (!second.CanBeCanceled)
            {
                maybeJoinedToken = first;
                return default;
            }
            if (first.IsCancelationRequested | second.IsCancelationRequested)
            {
                maybeJoinedToken = CancelationToken.Canceled();
                return default;
            }
            var source = CancelationSource.New(first, second);
            maybeJoinedToken = source.Token;
            return new MaybeJoinedCancelationSource(source);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct MaybeJoinedCancelationSource: IDisposable
        {
            private readonly CancelationSource _source;

            [MethodImpl(InlineOption)]
            public MaybeJoinedCancelationSource(CancelationSource source)
            {
                _source = source;
            }

            [MethodImpl(InlineOption)]
            public void Dispose()
            {
                if (_source != default)
                {
                    _source.Dispose();
                }
            }
        }

        internal static void SetOrAdd<T>(this IList<T> list, in T value, int index)
        {
            if (index < list.Count)
            {
                list[index] = value;
            }
            else
            {
                list.Add(value);
            }
        }

        internal static void MaybeShrink<T>(this IList<T> list, int expectedCount)
        {
            int listCount = list.Count;
            while (listCount > expectedCount)
            {
                list.RemoveAt(--listCount);
            }
        }

        // SpinWait.SpinOnce(int sleep1Threshold) API was added in netcoreapp3.0. We just route it to SpinOnce() for older runtimes.
#if !NETCOREAPP3_0_OR_GREATER
        [MethodImpl(InlineOption)]
        internal static void SpinOnce(this ref SpinWait spinner, int sleep1Threshold)
            => spinner.SpinOnce();
#endif

        [MethodImpl(InlineOption)]
        internal static void ClearReferences<T>(ref T location)
        {
#if NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER || NETCOREAPP2_0_OR_GREATER
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#endif
            {
                location = default;
            }
        }

        [MethodImpl(InlineOption)]
        internal static void ClearReferences<T>(T[] array, int index, int length)
        {
#if NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER || NETCOREAPP2_0_OR_GREATER
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#endif
            {
                Array.Clear(array, index, length);
            }
        }

#if NETCOREAPP
        [MethodImpl(InlineOption)]
        internal static AsyncFlowControl SuppressExecutionContextFlow()
            => ExecutionContext.SuppressFlow();
#else
        // .Net Framework throws if ExecutionContext.SuppressFlow() is called recursively, so we need to check if it's already suppressed.
        [MethodImpl(InlineOption)]
        internal static WrappedAsyncFlowControl SuppressExecutionContextFlow()
            => ExecutionContext.IsFlowSuppressed() ? default : new WrappedAsyncFlowControl(ExecutionContext.SuppressFlow());

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct WrappedAsyncFlowControl : IDisposable
        {
            private readonly AsyncFlowControl _asyncFlowControl;

            [MethodImpl(InlineOption)]
            public WrappedAsyncFlowControl(AsyncFlowControl asyncFlowControl)
            {
                _asyncFlowControl = asyncFlowControl;
            }

            [MethodImpl(InlineOption)]
            public void Dispose()
            {
                if (_asyncFlowControl != default)
                {
                    _asyncFlowControl.Dispose();
                }
            }
        }
#endif
    } // class Internal
} // namespace Proto.Promises