#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0074 // Use compound assignment

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
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static partial class Internal
    {
        private static void ScheduleContextCallback(SynchronizationContext context, object state, SendOrPostCallback contextCallback, WaitCallback threadpoolCallback)
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
        {
            return RejectContainer.Create(reason, rejectSkipFrames, exceptionWithStacktrace, traceable);
        }

        internal static void ReportRejection(object unhandledValue, ITraceable traceable)
        {
            ICantHandleException ex = unhandledValue as ICantHandleException;
            if (ex != null)
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
        private static int InterlockedAddWithUnsignedOverflowCheck(ref int location, int value)
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
                    Thread.MemoryBarrier();
                    initialValue = location;
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
                        newValue = (int) uValue;
                    }
                } while (Interlocked.CompareExchange(ref location, newValue, initialValue) != initialValue);
                return newValue;
            }
#else
            return Interlocked.Add(ref location, value);
#endif
        }

        [MethodImpl(InlineOption)]
        internal static bool TryUnregisterAndIsNotCanceling(ref CancelationRegistration cancelationRegistration)
        {
            // We check isCanceling in case the token is not cancelable (in which case TryUnregister returns false).
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
        {
            if (dict.TryGetValue(key, out value))
            {
                return dict.Remove(key);
            }
            return false;
        }
#endif

        internal static SynchronizationContext CaptureContext()
        {
            // We capture the current context to post the continuation. If it's null, we use the background context.
            return Promise.Manager.ThreadStaticSynchronizationContext
                // TODO: update compilation symbol when Unity adopts .Net Core.
#if !NETCOREAPP
                // Old .Net Framework/Mono includes `SynchronizationContext.Current` in the `ExecutionContext`, so it may not be null on a background thread.
                // We check for that case to not unnecessarily invoke continuations on a foreground thread when they can continue on a background thread.
                ?? (Thread.CurrentThread.IsBackground ? null : SynchronizationContext.Current)
#endif
                ?? Promise.Config.BackgroundContext
                ?? BackgroundSynchronizationContextSentinel.s_instance;
        }

        internal static CancelationSource MaybeJoinCancelationTokens(CancelationToken first, CancelationToken second, out CancelationToken maybeJoinedToken)
        {
            if (first == second | !first.CanBeCanceled)
            {
                maybeJoinedToken = second;
                return default(CancelationSource);
            }
            if (!second.CanBeCanceled)
            {
                maybeJoinedToken = first;
                return default(CancelationSource);
            }
            if (first.IsCancelationRequested | second.IsCancelationRequested)
            {
                maybeJoinedToken = CancelationToken.Canceled();
                return default(CancelationSource);
            }
            var source = CancelationSource.New(first, second);
            maybeJoinedToken = source.Token;
            return source;
        }
    } // class Internal
} // namespace Proto.Promises