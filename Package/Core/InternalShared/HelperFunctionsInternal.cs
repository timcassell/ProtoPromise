#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0074 // Use compound assignment

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
        internal static void ScheduleContextCallback(SynchronizationContext context, object state, SendOrPostCallback contextCallback, WaitCallback threadpoolCallback)
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            if (context == null)
            {
                throw new System.InvalidOperationException("context cannot be null");
            }
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
                        ReportRejection(e, state as ITraceable);
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
                        ReportRejection(e, state as ITraceable);
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

        internal static IRejectContainer CreateRejectContainer(object reason, int rejectSkipFrames, Exception exceptionWithStacktrace, ITraceable traceable)
            => RejectContainer.Create(reason, rejectSkipFrames + 1, exceptionWithStacktrace, traceable);

        internal static void ReportRejection(object unhandledValue, ITraceable traceable)
        {
            if (unhandledValue is ICantHandleException ex)
            {
                ex.ReportUnhandled(traceable);
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

#if !(NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER)
        internal static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, out TValue value)
            => dict.TryGetValue(key, out value) && dict.Remove(key);
#endif

        internal static CancelationSource MaybeJoinCancelationTokens(CancelationToken first, CancelationToken second, out CancelationToken maybeJoinedToken)
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
            return source;
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