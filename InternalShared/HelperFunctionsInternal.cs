#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable RECS0085 // When initializing explicitly typed local variable or array type, array creation expression can be replaced with array initializer.
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0041 // Use 'is null' check

using System;
using System.Collections.Generic;
using Proto.Utils;

#if PROMISE_DEBUG
using System.Diagnostics;
using System.Linq;
#endif

namespace Proto.Promises
{
    /// <summary>
    /// Members of this type are meant for INTERNAL USE ONLY! Do not use in user code! Use the documented public APIs.
    /// </summary>
    internal static partial class Internal
    {
        public static bool invokingRejected;

        public static event Action OnClearPool;

        public static void ClearPool()
        {
            Action temp = OnClearPool;
            if (temp != null)
            {
                temp.Invoke();
            }
        }

        static partial void _SetCreatedStacktrace(ITraceable traceable, int skipFrames);
        static partial void _SetCreatedAndRejectedStacktrace(IRejectValueContainer unhandledException, int rejectSkipFrames, ITraceable traceable);
        static partial void _SetCurrentInvoker(ITraceable current);
        static partial void _ClearCurrentInvoker();
#if PROMISE_DEBUG
        static partial void _SetCreatedStacktrace(ITraceable traceable, int skipFrames)
        {
            SetCreatedStacktrace(traceable, skipFrames + 1);
        }
        static partial void _SetCreatedAndRejectedStacktrace(IRejectValueContainer unhandledException, int rejectSkipFrames, ITraceable traceable)
        {
            SetCreatedAndRejectedStacktrace(unhandledException, rejectSkipFrames + 1, traceable);
        }
        static partial void _SetCurrentInvoker(ITraceable current)
        {
            SetCurrentInvoker(current);
        }
        static partial void _ClearCurrentInvoker()
        {
            ClearCurrentInvoker();
        }

        public static ulong InvokeId { get; private set; }
        public static CausalityTrace CurrentTrace { get; private set; }

        public static void SetCurrentInvoker(ITraceable current)
        {
            CurrentTrace = current.Trace;
        }

        public static void ClearCurrentInvoker()
        {
            CurrentTrace = null;
            ++InvokeId;
        }

        private static StackTrace GetStackTrace(int skipFrames)
        {
            return new StackTrace(skipFrames + 1, true);
        }

        public static string GetFormattedStacktrace(ITraceable traceable)
        {
            return traceable.Trace.ToString();
        }

        public static string GetFormattedStacktrace(int skipFrames)
        {
            return FormatStackTrace(new StackTrace[1] { GetStackTrace(skipFrames + 1) });
        }

        public static void SetCreatedStacktrace(ITraceable traceable, int skipFrames)
        {
            StackTrace stackTrace = Promise.Config.DebugCausalityTracer == Promise.TraceLevel.All
                ? GetStackTrace(skipFrames + 1)
                : null;
            traceable.Trace = new CausalityTrace(stackTrace, CurrentTrace);
        }

        public static void SetCreatedAndRejectedStacktrace(IRejectValueContainer unhandledException, int rejectSkipFrames, ITraceable traceable)
        {
            StackTrace stackTrace = rejectSkipFrames > 0 & Promise.Config.DebugCausalityTracer != Promise.TraceLevel.None
                ? GetStackTrace(rejectSkipFrames + 1)
                : null;
            unhandledException.SetCreatedAndRejectedStacktrace(stackTrace, traceable.Trace);
        }

        public static void ValidateArgument(object del, string argName, int skipFrames)
        {
            if (del == null)
            {
                throw new ArgumentNullException(argName, null, GetFormattedStacktrace(skipFrames + 1));
            }
        }

        public static string FormatStackTrace(IEnumerable<StackTrace> stackTraces)
        {
#if !CSHARP_7_OR_LATER
            // Format stack trace to match "throw exception" so that double-clicking log in Unity console will go to the proper line.
            List<string> _stackTraces = new List<string>();
            string[] separator = new string[1] { Environment.NewLine + " " };
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (StackTrace st in stackTraces)
            {
                string stackTrace = st.ToString().Substring(1);
                foreach (var trace in stackTrace.Split(separator, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!trace.Contains("Proto.Promises"))
                    {
                        sb.Append(trace)
                            .Replace(":line ", ":")
                            .Replace("(", " (")
                            .Replace(") in", ") [0x00000] in"); // Not sure what "[0x00000]" is, but it's necessary for Unity's parsing.
                        _stackTraces.Add(sb.ToString());
                        sb.Length = 0;
                    }
                }
            }
            foreach (var trace in _stackTraces)
            {
                sb.Append(trace).Append(" " + Environment.NewLine);
            }
            sb.Append(" ");
            return sb.ToString();
#else
            // StackTrace.ToString() format issue was fixed in the new runtime.
            List<StackFrame> stackFrames = new List<StackFrame>();
            foreach (StackTrace stackTrace in stackTraces)
            {
                stackFrames.AddRange(stackTrace.GetFrames());
            }

            var trace = stackFrames
                .Where(frame =>
                {
                    // Ignore DebuggerStepThrough and DebuggerHidden and DebuggerNonUserCode.
                    var methodType = frame.GetMethod();
                    return !methodType.IsDefined(typeof(DebuggerNonUserCodeAttribute), false)
                        && !methodType.DeclaringType.IsDefined(typeof(DebuggerNonUserCodeAttribute), false)
                        && !methodType.IsDefined(typeof(DebuggerHiddenAttribute), false);
                })
                // Create a new StackTrace to get proper formatting.
                .Select(frame => new StackTrace(frame).ToString());

            return string.Join(Environment.NewLine, trace.ToArray());
#endif
        }
#else
        public static string GetFormattedStacktrace(int skipFrames)
        {
            return null;
        }

        public static string GetFormattedStacktrace(Internal.ITraceable traceable)
        {
            return null;
        }
#endif

        public static bool TryConvert<TConvert>(IValueContainer valueContainer, out TConvert converted)
        {
            // Try to avoid boxing value types.
#if CSHARP_7_OR_LATER
            if (((object) valueContainer) is IValueContainer<TConvert> directContainer)
#else
            var directContainer = valueContainer as IValueContainer<TConvert>;
            if (directContainer != null)
#endif
            {
                converted = directContainer.Value;
                return true;
            }

            if (typeof(TConvert).IsAssignableFrom(valueContainer.ValueType))
            {
                // Unfortunately, this will box if converting from a non-nullable value type to nullable.
                // I couldn't find any way around that without resorting to Expressions (which won't work for this purpose with the IL2CPP AOT compiler).
                converted = (TConvert) valueContainer.Value;
                return true;
            }

            converted = default(TConvert);
            return false;
        }

        public static IRejectValueContainer CreateRejectContainer<TReject>(ref TReject reason, int rejectSkipFrames, ITraceable traceable)
        {
            IRejectValueContainer valueContainer;

            // Avoid boxing value types.
            Type type = typeof(TReject);
            if (type.IsValueType)
            {
                valueContainer = RejectionContainer<TReject>.GetOrCreate(ref reason);
            }
            else
            {
#if CSHARP_7_OR_LATER
                if (((object) reason) is IRejectionToContainer internalRejection)
#else
                IRejectionToContainer internalRejection = reason as IRejectionToContainer;
                if (internalRejection != null)
#endif
                {
                    // reason is an internal rejection object, get its container instead of wrapping it.
                    return internalRejection.ToContainer(traceable);
                }

                object o = reason;
                if (ReferenceEquals(o, null))
                {
                    // reason is null, behave the same way .Net behaves if you throw null.
                    o = new NullReferenceException();
                }
                // Only need to create one object pool for reference types.
                valueContainer = RejectionContainer<object>.GetOrCreate(ref o);
            }
            _SetCreatedAndRejectedStacktrace(valueContainer, rejectSkipFrames + 1, traceable);
            return valueContainer;
        }

        public static ICancelValueContainer CreateCancelContainer<TCancel>(ref TCancel reason)
        {
            ICancelValueContainer cancelValue;
            if (typeof(TCancel).IsValueType)
            {
                cancelValue = CancelContainer<TCancel>.GetOrCreate(ref reason);
            }
            else
            {
#if CSHARP_7_OR_LATER
                if (((object) reason) is ICancelationToContainer internalCancelation)
#else
                ICancelationToContainer internalCancelation = reason as ICancelationToContainer;
                if (internalCancelation != null)
#endif
                {
                    // reason is an internal cancelation object, get its container instead of wrapping it.
                    cancelValue = internalCancelation.ToContainer();
                }
                else if (ReferenceEquals(reason, null) || reason is OperationCanceledException)
                {
                    // Use void container instead of wrapping OperationCanceledException, or if reason is null.
                    cancelValue = CancelContainerVoid.GetOrCreate();
                }
                else
                {
                    // Only need to create one object pool for reference types.
                    object o = reason;
                    cancelValue = CancelContainer<object>.GetOrCreate(ref o);
                }
            }
            return cancelValue;
        }

        // Handle promises in a depth-first manner.
        private static ValueLinkedQueue<ITreeHandleable> _handleQueue;
        private static bool _runningHandles;

        public static void AddToHandleQueueFront(ITreeHandleable handleable)
        {
            _handleQueue.Push(handleable);
        }

        public static void AddToHandleQueueBack(ITreeHandleable handleable)
        {
            _handleQueue.Enqueue(handleable);
        }

        public static void AddToHandleQueueFront(ref ValueLinkedQueue<ITreeHandleable> handleables)
        {
            _handleQueue.PushAndClear(ref handleables);
        }

        public static void AddToHandleQueueBack(ref ValueLinkedQueue<ITreeHandleable> handleables)
        {
            _handleQueue.EnqueueAndClear(ref handleables);
        }

        private static ValueLinkedStackZeroGC<UnhandledException> _unhandledExceptions;

        public static void AddUnhandledException(UnhandledException exception)
        {
            _unhandledExceptions.Push(exception);
        }

        // Generate stack trace if traceable is null.
        public static void AddRejectionToUnhandledStack<TReject>(TReject unhandledValue, ITraceable traceable)
        {
#if CSHARP_7_OR_LATER
            if (((object) unhandledValue) is ICantHandleException ex)
#else
            ICantHandleException ex = unhandledValue as ICantHandleException;
            if (ex != null)
#endif
            {
                ex.AddToUnhandledStack(traceable);
                return;
            }

#if PROMISE_DEBUG
            string stackTrace =
                traceable != null
                    ? GetFormattedStacktrace(traceable)
                : Promise.Config.DebugCausalityTracer != Promise.TraceLevel.None
                    ? GetFormattedStacktrace(1)
                    : null;
#else
            string stackTrace = null;
#endif
            string message;
            Exception innerException;

            if (unhandledValue is Exception)
            {
                message = "An exception was not handled.";
                innerException = unhandledValue as Exception;
            }
            else if (ReferenceEquals(unhandledValue, null))
            {
                // unhandledValue is null, behave the same way .Net behaves if you throw null.
                message = "An rejected null value was not handled.";
                NullReferenceException nullRefEx = new NullReferenceException();
                AddUnhandledException(new UnhandledExceptionInternal(nullRefEx, typeof(NullReferenceException), message, stackTrace, nullRefEx));
                return;
            }
            else
            {
                Type type = typeof(TReject);
                message = "A rejected value was not handled, type: " + type + ", value: " + unhandledValue.ToString();
                innerException = null;
            }
            AddUnhandledException(new UnhandledExceptionInternal(unhandledValue, unhandledValue.GetType(), message, stackTrace, innerException));
        }

        public static void HandleEvents()
        {
            if (_runningHandles)
            {
                // HandleEvents is running higher in the program stack, so just return.
                return;
            }

            _runningHandles = true;

            while (_handleQueue.IsNotEmpty)
            {
                _handleQueue.DequeueRisky().Handle();
            }

            _handleQueue.ClearLast();
            _runningHandles = false;
        }

        public static void ThrowUnhandledRejections()
        {
            if (_unhandledExceptions.IsEmpty)
            {
                return;
            }

            var unhandledExceptions = _unhandledExceptions;
            _unhandledExceptions.ClearAndDontRepool();
            Action<UnhandledException> handler = Promise.Config.UncaughtRejectionHandler;
            if (handler != null)
            {
                foreach (UnhandledException unhandled in unhandledExceptions)
                {
                    handler.Invoke(unhandled);
                }
                unhandledExceptions.Clear();
                return;
            }

#if CSHARP_7_OR_LATER
            unhandledExceptions.Clear();
            throw new AggregateException(unhandledExceptions);
#else
            // .Net 3.5 dumb compiler can't convert IEnumerable<UnhandledExceptionInternal> to IEnumerable<Exception>
            var exceptions = new List<Exception>();
            foreach (var ex in unhandledExceptions)
            {
                exceptions.Add(ex);
            }
            unhandledExceptions.Clear();
            throw new AggregateException(exceptions);
#endif
        }
    }
}