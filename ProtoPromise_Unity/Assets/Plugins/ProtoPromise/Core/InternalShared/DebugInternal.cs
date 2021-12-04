#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0031 // Use null propagation

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if PROMISE_DEBUG
using System.Linq;
#endif

namespace Proto.Promises
{
    partial class Internal
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

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        internal const MethodImplOptions InlineOption = MethodImplOptions.NoInlining;
#else
        internal const MethodImplOptions InlineOption = (MethodImplOptions) 256; // AggressiveInlining
#endif

        // Calls to these get compiled away in RELEASE mode
        partial class PromiseRef
        {
            static partial void ValidateArgument(object arg, string argName, int skipFrames);
            partial void ValidateReturn(Promise other);

            partial class DeferredPromiseBase
            {
                static partial void ValidateProgress(float progress, int skipFrames);
            }
        }

        static partial void SetCreatedStacktrace(ITraceable traceable, int skipFrames);
        static partial void SetCreatedAndRejectedStacktrace(IRejectValueContainer unhandledException, int rejectSkipFrames, ITraceable traceable);
        static partial void SetCurrentInvoker(ITraceable current);
        static partial void ClearCurrentInvoker();
        static partial void IncrementInvokeId();
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
        private static long _invokeId;
        internal static long InvokeId { get { return _invokeId; } }

        static partial void IncrementInvokeId()
        {
            ++_invokeId;
        }
#else
        internal static long InvokeId { get { return ValidIdFromApi; } }
#endif // !CSHARP_7_3_OR_NEWER

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
            IncrementInvokeId();
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
                if (st == null)
                {
                    continue;
                }
                string stackTrace = st.ToString();
                if (string.IsNullOrEmpty(stackTrace))
                {
                    continue;
                }
                foreach (var trace in stackTrace.Substring(1).Split(separator, StringSplitOptions.RemoveEmptyEntries))
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
#else // !CSHARP_7_3_OR_NEWER
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
#endif // !CSHARP_7_3_OR_NEWER
        }

        partial interface ITraceable
        {
            CausalityTrace Trace { get; set; }
        }

        partial interface IRejectValueContainer
        {
            void SetCreatedAndRejectedStacktrace(StackTrace rejectedStacktrace, CausalityTrace createdStacktraces);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal class CausalityTrace
        {
            private readonly StackTrace _stackTrace;
            private readonly CausalityTrace _next;

            public CausalityTrace(StackTrace stackTrace, CausalityTrace higherStacktrace)
            {
                _stackTrace = stackTrace;
                _next = higherStacktrace;
            }

            public override string ToString()
            {
                if (_stackTrace == null)
                {
                    return null;
                }
                List<StackTrace> stackTraces = new List<StackTrace>();
                for (CausalityTrace current = this; current != null; current = current._next)
                {
                    if (current._stackTrace == null)
                    {
                        break;
                    }
                    stackTraces.Add(current._stackTrace);
                }
                return FormatStackTrace(stackTraces);
            }
        }

        internal static void ValidateProgressValue(float value, int skipFrames)
        {
            const string argName = "progress";
            bool isBetween01 = value >= 0f && value <= 1f;
            if (!isBetween01)
            {
                throw new ArgumentOutOfRangeException(argName, "Must be between 0 and 1.", GetFormattedStacktrace(skipFrames + 1));
            }
        }

        internal static void ValidateOperation(Promise promise, int skipFrames)
        {
            if (!promise.IsValid)
            {
                throw new InvalidOperationException("Promise is invalid." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!",
                    GetFormattedStacktrace(skipFrames + 1));
            }
        }

        partial class PromiseRef
        {
            static partial void ValidateArgument(object arg, string argName, int skipFrames)
            {
                Internal.ValidateArgument(arg, argName, skipFrames + 1);
            }

            partial void ValidateReturn(Promise other)
            {
                if (!other.IsValid)
                {
                    // Returning an invalid from the callback is not allowed.
                    throw new InvalidReturnException("An invalid promise was returned.", string.Empty);
                }

                PromiseRef _ref = other._target._ref;

                // A promise cannot wait on itself.
                if (_ref == this)
                {
                    throw new InvalidReturnException("A Promise cannot wait on itself.", string.Empty);
                }
                if (_ref == null)
                {
                    return;
                }
                // This allows us to check All/Race/First Promises iteratively.
                Stack<PromisePassThrough> passThroughs = PassthroughsForIterativeAlgorithm;
                PromiseRef prev = _ref._valueOrPrevious as PromiseRef;
            Repeat:
                for (; prev != null; prev = prev._valueOrPrevious as PromiseRef)
                {
                    if (prev == this)
                    {
                        _ref.MarkAwaitedAndMaybeDispose(other._target.Id, true);
                        while (passThroughs.Count > 0)
                        {
                            passThroughs.Pop().Release();
                        }
                        throw new InvalidReturnException("Circular Promise chain detected.", GetFormattedStacktrace(_ref));
                    }
                    prev.BorrowPassthroughs(passThroughs);
                }

                if (passThroughs.Count > 0)
                {
                    var passThrough = passThroughs.Pop();
                    prev = passThrough.Owner;
                    passThrough.Release();
                    goto Repeat;
                }
            }

            [ThreadStatic]
            private static Stack<PromisePassThrough> _passthroughsForIterativeAlgorithm;
            private static Stack<PromisePassThrough> PassthroughsForIterativeAlgorithm
            {
                get
                {
                    if (_passthroughsForIterativeAlgorithm == null)
                    {
                        _passthroughsForIterativeAlgorithm = new Stack<PromisePassThrough>();
                    }
                    return _passthroughsForIterativeAlgorithm;
                }
            }

            protected virtual void BorrowPassthroughs(Stack<PromisePassThrough> borrower) { }

            private static void ExchangePassthroughs(ref ValueLinkedStack<PromisePassThrough> from, Stack<PromisePassThrough> to, object locker)
            {
                lock (locker)
                {
                    foreach (var passthrough in from)
                    {
                        passthrough.Retain();
                        to.Push(passthrough);
                    }
                }
            }

            partial class MergePromise
            {
                protected override void BorrowPassthroughs(Stack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, borrower, _locker);
                }
            }

            partial class RacePromise
            {
                protected override void BorrowPassthroughs(Stack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, borrower, _locker);
                }
            }

            partial class FirstPromise
            {
                protected override void BorrowPassthroughs(Stack<PromisePassThrough> borrower)
                {
                    ExchangePassthroughs(ref _passThroughs, borrower, _locker);
                }
            }

            partial class DeferredPromiseBase
            {
                static partial void ValidateProgress(float progress, int skipFrames)
                {
                    ValidateProgressValue(progress, skipFrames + 1);
                }
            }
        }
#else // PROMISE_DEBUG
        internal static long InvokeId
        {
            [MethodImpl(InlineOption)]
            get { return ValidIdFromApi; }
        }

        internal static string GetFormattedStacktrace(int skipFrames)
        {
            return null;
        }

        internal static string GetFormattedStacktrace(ITraceable traceable)
        {
            return null;
        }
#endif // PROMISE_DEBUG
    } // class Internal

    partial struct Promise
    {
        // Calls to these get compiled away in RELEASE mode
        partial void ValidateOperation(int skipFrames);
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
        static partial void ValidateArgument(Promise arg, string argName, int skipFrames);
        static partial void ValidateElement(Promise promise, string argName, int skipFrames);

#if PROMISE_DEBUG
        partial void ValidateOperation(int skipFrames)
        {
            Internal.ValidateOperation(this, skipFrames + 1);
        }

        static partial void ValidateArgument(object arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }

        static partial void ValidateArgument(Promise arg, string argName, int skipFrames)
        {
            if (!arg.IsValid)
            {
                throw new InvalidArgumentException(argName,
                    "Promise is invalid." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!",
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateElement(Promise promise, string argName, int skipFrames)
        {
            if (!promise.IsValid)
            {
                throw new InvalidElementException(argName,
                    string.Format("A promise is invalid in {0}." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!", argName),
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }
#endif
    }

    partial struct Promise<T>
    {
        // Calls to these get compiled away in RELEASE mode
        partial void ValidateOperation(int skipFrames);
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
        static partial void ValidateArgument(Promise<T> arg, string argName, int skipFrames);
        static partial void ValidateElement(Promise<T> promise, string argName, int skipFrames);
#if PROMISE_DEBUG
        partial void ValidateOperation(int skipFrames)
        {
            Internal.ValidateOperation(this, skipFrames + 1);
        }

        static partial void ValidateArgument(object arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }

        static partial void ValidateArgument(Promise<T> arg, string argName, int skipFrames)
        {
            if (!arg.IsValid)
            {
                throw new InvalidArgumentException(argName,
                    "Promise is invalid." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!",
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateElement(Promise<T> promise, string argName, int skipFrames)
        {
            if (!promise.IsValid)
            {
                throw new InvalidElementException(argName,
                    string.Format("A promise is invalid in {0}." +
                    " Call `Preserve()` if you intend to add multiple callbacks or await multiple times on a single promise instance." +
                    " Remember to call `Forget()` when you are finished with it!", argName),
                    Internal.GetFormattedStacktrace(skipFrames + 1));
            }
        }
#endif
    }
}