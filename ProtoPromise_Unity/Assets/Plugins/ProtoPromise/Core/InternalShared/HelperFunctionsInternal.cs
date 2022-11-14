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
        // This is used to detect if we're currently executing on the context we're going to schedule to, so we can just invoke synchronously instead.
        [ThreadStatic]
        internal static SynchronizationContext ts_currentContext;

        private static readonly SendOrPostCallback s_synchronizationContextHandleCallback = HandleFromContext;
        private static readonly WaitCallback s_threadPoolHandleCallback = HandleFromContext;

        private static void ScheduleForHandle(HandleablePromiseBase handleable, SynchronizationContext context)
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            if (context == null)
            {
                throw new System.InvalidOperationException("context cannot be null");
            }
#endif
            if (context == BackgroundSynchronizationContextSentinel.s_instance)
            {
                ThreadPool.QueueUserWorkItem(s_threadPoolHandleCallback, handleable);
            }
            else
            {
                context.Post(s_synchronizationContextHandleCallback, handleable);
            }
        }

        private static void HandleFromContext(object state)
        {
            // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
            try
            {
                state.UnsafeAs<HandleablePromiseBase>().HandleFromContext();
            }
            catch (Exception e)
            {
                // This should never happen.
                ReportRejection(e, state as ITraceable);
            }
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
        internal static T InterlockedExchange<T>(ref T location, T value) where T : class
        {
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. So use CompareExchange loop instead.
            T current;
            do
            {
                Thread.MemoryBarrier(); // Force fresh read. Necessary since Volatile.Read isn't available on old runtimes.
                current = location;
            } while (Interlocked.CompareExchange(ref location, value, current) != current);
            return current;
#else
            return Interlocked.Exchange(ref location, value);
#endif
        }

        [MethodImpl(InlineOption)]
        internal static int InterlockedExchange(ref int location, int value)
        {
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. So use CompareExchange loop instead.
            int current;
            do
            {
                Thread.MemoryBarrier(); // Force fresh read. Necessary since Volatile.Read isn't available on old runtimes.
                current = location;
            } while (Interlocked.CompareExchange(ref location, value, current) != current);
            return current;
#else
            return Interlocked.Exchange(ref location, value);
#endif
        }

        [MethodImpl(InlineOption)]
        // TODO: remove this helper. We no longer need to care if the token is canceling
        // since the TryUnregister was changed to allow unregistration while the token is invoking callbacks.
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
    } // class Internal
} // namespace Proto.Promises