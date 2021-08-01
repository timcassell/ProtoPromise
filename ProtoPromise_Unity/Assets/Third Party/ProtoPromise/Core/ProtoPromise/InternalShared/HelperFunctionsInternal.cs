#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0031 // Use null propagation
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Proto.Utils;

#if PROMISE_DEBUG
using System.Linq;
#endif

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
        internal static string CausalityTraceMessage
        {
            get
            {
#if PROMISE_DEBUG
                return Promise.Config.DebugCausalityTracer == Promise.TraceLevel.All
                    ? " -- This exception's Stacktrace contains the causality trace of all async callbacks that ran."
                    : " -- Set Promise.Config.DebugCausalityTracer = Promise.TraceLevel.All to get a causality trace.";
#else
                return " -- Enable DEBUG mode and set Promise.Config.DebugCausalityTracer = Promise.TraceLevel.All to get a causality trace.";
#endif
            }
        }

        static partial void SetCreatedStacktrace(ITraceable traceable, int skipFrames);
        static partial void SetCreatedAndRejectedStacktrace(IRejectValueContainer unhandledException, int rejectSkipFrames, ITraceable traceable);
        static partial void SetCurrentInvoker(ITraceable current);
        static partial void ClearCurrentInvoker();
#if PROMISE_DEBUG
        static partial void SetCreatedStacktrace(ITraceable traceable, int skipFrames)
        {
            StackTrace stackTrace = Promise.Config.DebugCausalityTracer == Promise.TraceLevel.All
                ? GetStackTrace(skipFrames + 1)
                : null;
            traceable.Trace = new CausalityTrace(stackTrace, _currentTrace);
        }

        static partial void SetCreatedAndRejectedStacktrace(IRejectValueContainer unhandledException, int rejectSkipFrames, ITraceable traceable)
        {
            StackTrace stackTrace = rejectSkipFrames > 0 & Promise.Config.DebugCausalityTracer != Promise.TraceLevel.None
                ? GetStackTrace(rejectSkipFrames + 1)
                : null;
            unhandledException.SetCreatedAndRejectedStacktrace(stackTrace, traceable.Trace);
        }

#if !CSHARP_7_3_OR_NEWER
        // This is only needed in older language versions that don't support ref structs.
        [ThreadStatic]
        private static ulong _invokeId;
        internal static ulong InvokeId { get { return _invokeId; } }
#endif

        [ThreadStatic]
        private static CausalityTrace _currentTrace;
        [ThreadStatic]
        private static Stack<CausalityTrace> _traces;

        static partial void SetCurrentInvoker(ITraceable current)
        {
            if (_traces == null)
            {
                _traces = new Stack<CausalityTrace>();
            }
            _traces.Push(_currentTrace);
            _currentTrace = current.Trace;
        }

        static partial void ClearCurrentInvoker()
        {
            _currentTrace = _traces.Pop();
#if !CSHARP_7_3_OR_NEWER
            ++_invokeId;
#endif
        }

        private static StackTrace GetStackTrace(int skipFrames)
        {
            return new StackTrace(skipFrames + 1, true);
        }

        internal static string GetFormattedStacktrace(ITraceable traceable)
        {
            return traceable != null ? traceable.Trace.ToString() : null;
        }

        internal static string GetFormattedStacktrace(int skipFrames)
        {
            return FormatStackTrace(new StackTrace[1] { GetStackTrace(skipFrames + 1) });
        }

        internal static void ValidateArgument(object arg, string argName, int skipFrames)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(argName, null, GetFormattedStacktrace(skipFrames + 1));
            }
        }

        internal static string FormatStackTrace(IEnumerable<StackTrace> stackTraces)
        {
#if !CSHARP_7_3_OR_NEWER
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
                    // Ignore DebuggerNonUserCode and DebuggerHidden.
                    var methodType = frame?.GetMethod();
                    return methodType != null
                        && !methodType.IsDefined(typeof(DebuggerNonUserCodeAttribute), false)
                        && !methodType.DeclaringType.IsDefined(typeof(DebuggerNonUserCodeAttribute), false)
                        && !methodType.IsDefined(typeof(DebuggerHiddenAttribute), false);
                })
                // Create a new StackTrace to get proper formatting.
                .Select(frame => new StackTrace(frame).ToString())
                .ToArray();

            return string.Join(Environment.NewLine, trace);
#endif
        }
#else
        internal static string GetFormattedStacktrace(int skipFrames)
        {
            return null;
        }

        internal static string GetFormattedStacktrace(ITraceable traceable)
        {
            return null;
        }
#endif

        internal static bool TryConvert<TConvert>(IValueContainer valueContainer, out TConvert converted)
        {
            // Try to avoid boxing value types.
#if CSHARP_7_3_OR_NEWER
            if (valueContainer is IValueContainer<TConvert> directContainer)
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

        internal static IRejectValueContainer CreateRejectContainer<TReject>(ref TReject reason, int rejectSkipFrames, ITraceable traceable)
        {
            IRejectValueContainer valueContainer;

            // Avoid boxing value types.
            Type type = typeof(TReject);
            if (type.IsValueType)
            {
                valueContainer = RejectionContainer<TReject>.GetOrCreate(ref reason, 0);
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
                valueContainer = RejectionContainer<object>.GetOrCreate(ref o, 0);
            }
            SetCreatedAndRejectedStacktrace(valueContainer, rejectSkipFrames + 1, traceable);
            return valueContainer;
        }

        internal static ICancelValueContainer CreateCancelContainer<TCancel>(ref TCancel reason)
        {
            ICancelValueContainer cancelValue;
            if (typeof(TCancel).IsValueType)
            {
                cancelValue = CancelContainer<TCancel>.GetOrCreate(ref reason, 0);
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
                    cancelValue = CancelContainerVoid.GetOrCreate();
                }
                else
                {
                    // Only need to create one object pool for reference types.
                    object o = reason;
                    cancelValue = CancelContainer<object>.GetOrCreate(ref o, 0);
                }
            }
            return cancelValue;
        }

        // Handle promises in a depth-first manner.
        private static ValueLinkedQueue<ITreeHandleable> _handleQueue;
        private static readonly object _handleLocker = new object();

        internal static void AddToHandleQueueFront(ITreeHandleable handleable)
        {
            lock (_handleLocker)
            {
                _handleQueue.Push(handleable);
            }
        }

        internal static void AddToHandleQueueBack(ITreeHandleable handleable)
        {
            lock (_handleLocker)
            {
                _handleQueue.Enqueue(handleable);
            }
        }

        internal static void AddToHandleQueueFront(ref ValueLinkedQueue<ITreeHandleable> handleables)
        {
            lock (_handleLocker)
            {
                _handleQueue.PushAndClear(ref handleables);
            }
        }

        internal static void HandleEvents()
        {
            while (true)
            {
                ValueLinkedQueue<ITreeHandleable> queue;
                lock (_handleLocker)
                {
                    queue = _handleQueue;
                    _handleQueue.Clear();
                }
                if (queue.IsEmpty)
                {
                    break;
                }

                do
                {
                    queue.DequeueRisky().Handle();
                } while (queue.IsNotEmpty);
            }
        }

        private static ValueLinkedStackZeroGC<UnhandledException> _unhandledExceptions;

        internal static void AddUnhandledException(UnhandledException exception)
        {
            _unhandledExceptions.Push(exception);
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

        internal static void ThrowUnhandledRejections()
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

#if CSHARP_7_3_OR_NEWER
            var ex = new AggregateException(unhandledExceptions);
            unhandledExceptions.Clear();
            throw ex;
#else
            // .Net 3.5 and earlier can't convert IEnumerable<UnhandledExceptionInternal> to IEnumerable<Exception>
            var exceptions = new List<Exception>();
            foreach (var ex in unhandledExceptions)
            {
                exceptions.Add(ex);
            }
            unhandledExceptions.Clear();
            throw new AggregateException(exceptions);
#endif
        }

        internal static bool InterlockedAddIfNotEqual(ref int location, int value, int comparand, out int newValue)
        {
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
    }
}