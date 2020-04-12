#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
#pragma warning disable RECS0085 // When initializing explicitly typed local variable or array type, array creation expression can be replaced with array initializer.
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
    partial class Promise
    {
        // Calls to these get compiled away in RELEASE mode
        static partial void ValidateOperation(Promise promise, int skipFrames);
        static partial void ValidateProgress(float progress, int skipFrames);
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
        partial void ValidateReturn(Promise other);
        static partial void ValidateReturn(Delegate other);
        static partial void ValidateYieldInstructionOperation(object valueContainer, int skipFrames);
        static partial void ValidateElementNotNull(Promise promise, string argName, string message, int skipFrames);

        static partial void SetCreatedStacktrace(Internal.ITraceable stacktraceable, int skipFrames);
        partial void SetCreatedAndRejectedStacktrace(Internal.IRejectionContainer unhandledException, bool generateStacktrace);
        partial void SetNotDisposed();
        static partial void SetCurrentInvoker(Internal.ITraceable current);
        static partial void ClearCurrentInvoker();
#if PROMISE_DEBUG
        protected static ulong _invokeId;

        private static int idCounter;
        protected readonly int _id;

        private ushort _userRetainCounter;

        private static Internal.CausalityTrace _currentTrace;
        Internal.CausalityTrace Internal.ITraceable.Trace { get; set; }

        static partial void SetCurrentInvoker(Internal.ITraceable current)
        {
            _currentTrace = current.Trace;
        }

        static partial void ClearCurrentInvoker()
        {
            _currentTrace = null;
            ++_invokeId;
        }

        private static object DisposedObject
        {
            get
            {
                return Internal.DisposedChecker.instance;
            }
        }

        private static string GetFormattedStacktrace(Internal.ITraceable traceable)
        {
            return traceable.Trace.ToString();
        }

        partial void SetNotDisposed()
        {
            _valueOrPrevious = null;
        }

        partial class Internal
        {
            [DebuggerNonUserCode]
            public class CausalityTrace
            {
                private readonly StackTrace _stacktrace;
                private readonly CausalityTrace _next;

                public CausalityTrace(StackTrace stacktrace, CausalityTrace higherStacktrace)
                {
                    _stacktrace = stacktrace;
                    _next = higherStacktrace;
                }

                public override string ToString()
                {
                    if (_stacktrace == null)
                    {
                        return null;
                    }
                    List<StackTrace> stacktraces = new List<StackTrace>();
                    for (CausalityTrace current = _next; current != null; current = current._next)
                    {
                        stacktraces.Add(current._stacktrace);
                    }
                    return FormatStackTrace(stacktraces);
                }
            }

            // This allows us to re-use the reference field without having to add another bool field.
            [DebuggerNonUserCode]
            public sealed class DisposedChecker
            {
                public static readonly DisposedChecker instance = new DisposedChecker();

                private DisposedChecker() { }
            }
        }

        static partial void SetCreatedStacktrace(Internal.ITraceable stacktraceable, int skipFrames)
        {
            stacktraceable.Trace = Config.DebugCausalityTracer == TraceLevel.All
                ? new Internal.CausalityTrace(GetStackTrace(skipFrames + 1), _currentTrace)
                : null;
        }

        partial void SetCreatedAndRejectedStacktrace(Internal.IRejectionContainer unhandledException, bool generateStacktrace)
        {
            StackTrace stacktrace = generateStacktrace & Config.DebugCausalityTracer != TraceLevel.None
                ? GetStackTrace(1)
                : null;
            unhandledException.SetCreatedAndRejectedStacktrace(stacktrace, ((Internal.ITraceable) this).Trace);
        }

        private static StackTrace GetStackTrace(int skipFrames)
        {
            return new StackTrace(skipFrames + 1, true);
        }

        protected static string GetFormattedStacktrace(int skipFrames)
        {
            return FormatStackTrace(new StackTrace[1] { GetStackTrace(skipFrames + 1) });
        }

        private static string FormatStackTrace(IEnumerable<StackTrace> stacktraces)
        {
#if !CSHARP_7_OR_LATER
            // Format stacktrace to match "throw exception" so that double-clicking log in Unity console will go to the proper line.
            List<string> _stacktraces = new List<string>();
            string[] separator = new string[1] { Environment.NewLine + " " };
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (StackTrace st in stacktraces)
            {
                string stacktrace = st.ToString().Substring(1);
                foreach (var trace in stacktrace.Split(separator, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!trace.Contains("Proto.Promises"))
                    {
                        sb.Append(trace)
                            .Replace(":line ", ":")
                            .Replace("(", " (")
                            .Replace(") in", ") [0x00000] in"); // Not sure what "[0x00000]" is, but it's necessary for Unity's parsing.
                        _stacktraces.Add(sb.ToString());
                        sb.Length = 0;
                    }
                }
            }
            foreach (var trace in _stacktraces)
            {
                sb.Append(trace).Append(" " + Environment.NewLine);
            }
            sb.Append(" ");
            return sb.ToString();
#else
            // StackTrace.ToString() format issue was fixed in the new runtime.
            List<StackFrame> stackFrames = new List<StackFrame>();
            foreach (StackTrace stacktrace in stacktraces)
            {
                stackFrames.AddRange(stacktrace.GetFrames());
            }

            var trace = stackFrames
                .Where(frame =>
                {
                    // Ignore DebuggerStepThrough and DebuggerHidden and DebuggerNonUserCode.
                    var methodType = frame.GetMethod();
                    return !methodType.IsDefined(typeof(DebuggerHiddenAttribute), false)
                        && !methodType.IsDefined(typeof(DebuggerNonUserCodeAttribute), false)
                        && !methodType.DeclaringType.IsDefined(typeof(DebuggerNonUserCodeAttribute), false)
                        && !methodType.IsDefined(typeof(DebuggerStepThroughAttribute), false)
                        && !methodType.DeclaringType.IsDefined(typeof(DebuggerStepThroughAttribute), false);
                })
                // Create a new StackTrace to get proper formatting.
                .Select(frame => new StackTrace(frame).ToString());

            return string.Join(Environment.NewLine, trace.ToArray());
#endif

        }

        partial void ValidateReturn(Promise other)
        {
            if (other == null)
            {
                // Returning a null from the callback is not allowed.
                throw new InvalidReturnException("A null promise was returned.");
            }

            // Validate returned promise as not disposed.
            if (IsDisposed(other._valueOrPrevious))
            {
                throw new InvalidReturnException("A disposed promise was returned.");
            }

            // A promise cannot wait on itself.

            // This allows us to check All/Race/First Promises iteratively.
            ValueLinkedStack<Internal.PromisePassThrough> passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>();
            var prev = other;
        Repeat:
            for (; prev != null; prev = prev._valueOrPrevious as Promise)
            {
                if (prev == this)
                {
                    throw new InvalidReturnException("Circular Promise chain detected.", ((Internal.ITraceable) other).Trace.ToString());
                }
                prev.BorrowPassthroughs(ref passThroughs);
            }

            if (passThroughs.IsNotEmpty)
            {
                // passThroughs are removed from their targets before adding to passThroughs. Add them back here.
                var passThrough = passThroughs.Pop();
                prev = passThrough.Owner;
                passThrough.Target.ReAdd(passThrough);
                goto Repeat;
            }
        }

        static partial void ValidateReturn(Delegate other)
        {
            if (other == null)
            {
                // Returning a null from the callback is not allowed.
                throw new InvalidReturnException("A null delegate was returned.");
            }
        }

        protected static void ValidateProgressValue(float value, int skipFrames)
        {
            const string argName = "progress";
            if (value < 0f || value > 1f || float.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(argName, "Must be between 0 and 1.");
            }
        }

        private static bool IsDisposed(object valueContainer)
        {
            return ReferenceEquals(valueContainer, Internal.DisposedChecker.instance);
        }

        static protected void ValidateNotDisposed(object valueContainer, int skipFrames)
        {
            if (IsDisposed(valueContainer))
            {
                throw new PromiseDisposedException("Always nullify your references when you are finished with them!" +
                    " Call Retain() if you want to perform operations after the object has finished. Remember to call Release() when you are finished with it!"
                    , GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateYieldInstructionOperation(object valueContainer, int skipFrames)
        {
            ValidateNotDisposed(valueContainer, skipFrames + 1);
        }

        static partial void ValidateOperation(Promise promise, int skipFrames)
        {
            ValidateNotDisposed(promise._valueOrPrevious, skipFrames + 1);
        }

        static partial void ValidateProgress(float progress, int skipFrames)
        {
            ValidateProgressValue(progress, skipFrames + 1);
        }

        static protected void ValidateArg(object del, string argName, int skipFrames)
        {
            if (del == null)
            {
                throw new ArgumentNullException(argName, null, GetFormattedStacktrace(skipFrames + 1));
            }
        }

        static partial void ValidateArgument(object arg, string argName, int skipFrames)
        {
            ValidateArg(arg, argName, skipFrames + 1);
        }

        static partial void ValidateElementNotNull(Promise promise, string argName, string message, int skipFrames)
        {
            if (promise == null)
            {
                throw new ElementNullException(argName, message, GetFormattedStacktrace(skipFrames + 1));
            }
        }

        public override string ToString()
        {
            return string.Format("Type: Promise, Id: {0}, State: {1}", _id, _state);
        }
#else
        protected static string GetFormattedStacktrace(int skipFrames)
        {
            return null;
        }

        private static string GetFormattedStacktrace(Internal.ITraceable traceable)
        {
            return null;
        }

        private static object DisposedObject
        {
            get
            {
                return null;
            }
        }

        public override string ToString()
        {
            return string.Format("Type: Promise, State: {0}", _state);
        }
#endif

        partial class Internal
        {
            public interface ITraceable
            {
#if PROMISE_DEBUG
                CausalityTrace Trace { get; set; }
#endif
            }

            partial class FinallyDelegate : ITraceable
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
            }

            partial class FinallyDelegateCapture<TCapture> : ITraceable
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
            }

            partial class CancelDelegate : ITraceable
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
            }

            partial class CancelDelegateCapture<TCapture> : ITraceable
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
            }
        }
    }

    partial class Promise<T>
    {
        // Calls to these get compiled away in RELEASE mode
        static partial void ValidateYieldInstructionOperation(object valueContainer, int skipFrames);
        static partial void ValidateOperation(Promise<T> promise, int skipFrames);
        static partial void ValidateArgument(object arg, string argName, int skipFrames);
        static partial void ValidateProgress(float progress, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateProgress(float progress, int skipFrames)
        {
            ValidateProgressValue(progress, skipFrames + 1);
        }

        static partial void ValidateYieldInstructionOperation(object valueContainer, int skipFrames)
        {
            ValidateNotDisposed(valueContainer, skipFrames + 1);
        }

        static partial void ValidateOperation(Promise<T> promise, int skipFrames)
        {
            ValidateNotDisposed(promise._valueOrPrevious, skipFrames + 1);
        }

        static partial void ValidateArgument(object arg, string argName, int skipFrames)
        {
            ValidateArg(arg, argName, skipFrames + 1);
        }

        public override string ToString()
        {
            return string.Format("Type: Promise<{0}>, Id: {1}, State: {2}", typeof(T), _id, _state);
        }
#else
        public override string ToString()
        {
            return string.Format("Type: Promise<{0}>, State: {1}", typeof(T), _state);
        }
#endif
    }
}