#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable IDE0028 // Simplify collection initialization
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0300 // Simplify collection initialization
#pragma warning disable IDE0301 // Simplify collection initialization

namespace Proto.Diagnostics
{
    /// <summary>
    /// Represents a causality trace: the <see cref="System.Diagnostics.StackTrace"/> of where an operation was started,
    /// and the causality traces of the operations that were running at that time (see <see cref="PreviousTraces"/>).
    /// </summary>
    /// <remarks>
    /// Previous traces may be shared by multiple causality traces, so the causality traces form a graph rather than a tree.
    /// </remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public sealed partial class CausalityTrace
    {
        [ThreadStatic]
        internal static Stack<CausalityTrace> ts_traces;

        private static bool s_isEnabled = false;
        private static int s_maxDepth = 128;

        /// <summary>
        /// Should causality traces be captured when async objects are created and continued.
        /// When this is <see langword="false"/>, <see cref="Capture()"/> returns <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// NOTE: This can be extremely expensive, so you should only enable this if you got an error and you are not sure where it came from.
        /// Objects are not pooled while this is enabled.
        /// <para/>
        /// Causality traces are most reliable in DEBUG mode. In RELEASE mode, traces are not always available
        /// (for example, for objects that are reported from finalizers on some runtimes).
        /// </remarks>
        public static bool IsEnabled
        {
            [MethodImpl(Promises.Internal.InlineOption)]
            get => s_isEnabled;
            set
            {
                s_isEnabled = value;
                Promises.Internal.UpdateIsObjectPoolingEnabled();
                if (!value)
                {
                    // Objects that were traced may be pooled again, so we release their traces.
                    Promises.Internal.ClearTraces();
                }
            }
        }

        /// <summary>
        /// The maximum number of levels a causality trace keeps, where each level is a callback that was running when a trace was captured
        /// (e.g. each async hop in a chain of continuations). When a newly captured trace would exceed this depth,
        /// its oldest levels are dropped, keeping the most recent half of this depth.
        /// </summary>
        /// <remarks>
        /// This prevents causality traces from growing without bound when callbacks are recursively scheduled
        /// (e.g. a callback that registers another callback that registers another callback...).
        /// </remarks>
        public static int MaxDepth
        {
            [MethodImpl(Promises.Internal.InlineOption)]
            get => s_maxDepth;
            set
            {
                if (value < 1)
                {
                    throw new Promises.ArgumentOutOfRangeException(nameof(value), $"{nameof(MaxDepth)} must be greater than 0.", Promises.Internal.GetFormattedStacktrace(1));
                }
                s_maxDepth = value;
            }
        }

        private readonly StackTrace _stackTrace;
        private readonly CausalityTrace[] _previousTraces;
        // The number of levels in this trace, including itself.
        private readonly int _depth;
        // Whether older previous traces were dropped to stay within MaxDepth.
        private readonly bool _isTruncated;
        // Lazily created.
        private ReadOnlyCollection<CausalityTrace> _readOnlyPreviousTraces;
        private string _string;

        /// <summary>
        /// The raw <see cref="System.Diagnostics.StackTrace"/> of where this <see cref="CausalityTrace"/> was captured.
        /// </summary>
        /// <remarks>
        /// This includes frames that are normally hidden from stack traces, such as ProtoPromise's internal frames. Use <see cref="GetFrames"/> to exclude them.
        /// </remarks>
        public StackTrace StackTrace => _stackTrace;

        /// <summary>
        /// The <see cref="CausalityTrace"/>s of the operations that were running when this trace was captured, from innermost to outermost.
        /// An operation is anything that entered a <see cref="Scope"/>, such as a callback being invoked or a deferred being completed.
        /// </summary>
        /// <remarks>
        /// The first element is the trace of where the innermost running operation was started (e.g. where the callback was registered), which is the async cause of this trace.
        /// Each following element is the trace of where an outer operation was started, which was still running when the operation before it ran synchronously.
        /// Elements may be <see langword="null"/> if a <see cref="Scope"/> was entered with a <see langword="null"/> trace
        /// (e.g. the operation was started while <see cref="IsEnabled"/> was <see langword="false"/>).
        /// </remarks>
        public IReadOnlyList<CausalityTrace> PreviousTraces
            => _readOnlyPreviousTraces ?? (_readOnlyPreviousTraces = new ReadOnlyCollection<CausalityTrace>(_previousTraces));

        /// <summary>
        /// Gets the frames of <see cref="StackTrace"/>, excluding frames that are hidden from stack traces, such as ProtoPromise's internal frames.
        /// </summary>
        public StackFrame[] GetFrames()
            => GetVisibleFrames(_stackTrace);

        internal static StackFrame[] GetVisibleFrames(StackTrace stackTrace)
        {
            var frames = stackTrace.GetFrames();
            if (frames == null)
            {
                return Array.Empty<StackFrame>();
            }

            var visibleFrames = new List<StackFrame>(frames.Length);
            foreach (var frame in frames)
            {
                if (IsVisible(frame))
                {
                    visibleFrames.Add(frame);
                }
            }
            return visibleFrames.ToArray();
        }

#if NET6_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "If the method metadata was trimmed, GetMethod() returns null and the frame is excluded, the same as StackTrace.ToString().")]
#endif
        private static bool IsVisible(StackFrame frame)
        {
            var method = frame?.GetMethod();
            if (method == null)
            {
                return false;
            }
            // Match the .Net 6+ runtime's StackTrace.ToString() filtering, so that frames are filtered the same way on all runtimes.
            if ((method.MethodImplementationFlags & System.Reflection.MethodImplAttributes.AggressiveInlining) != 0
                || IsStackTraceHidden(method))
            {
                return false;
            }
            var declaringType = method.DeclaringType;
            return declaringType == null || !IsStackTraceHidden(declaringType);
        }

        private static bool IsStackTraceHidden(System.Reflection.MemberInfo member)
        {
#if NET6_0_OR_GREATER
            // Match the runtime, which only hides frames with its own StackTraceHiddenAttribute.
            return member.IsDefined(typeof(StackTraceHiddenAttribute), false);
#else
            // Before .Net 6, StackTraceHiddenAttribute doesn't exist, so libraries define their own (like we do, see ForOldRuntime/StackTraceHiddenAttribute.cs).
            // We match it by name instead of by type, so that it works with any of them.
            foreach (var attribute in member.GetCustomAttributes(false))
            {
                if (attribute.GetType().FullName == "System.Diagnostics.StackTraceHiddenAttribute")
                {
                    return true;
                }
            }
            return false;
#endif
        }

        /// <summary>
        /// Captures a new <see cref="CausalityTrace"/> from the current stack and the current traces, if <see cref="IsEnabled"/> is <see langword="true"/>.
        /// </summary>
        /// <param name="skipFrames">The number of frames up the stack from which to start the trace.</param>
        /// <returns>The captured <see cref="CausalityTrace"/>, or <see langword="null"/> if <see cref="IsEnabled"/> is <see langword="false"/>.</returns>
        // NoInlining so that skipFrames is accurate.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static CausalityTrace Capture(int skipFrames)
            => s_isEnabled ? new CausalityTrace(skipFrames + 1) : null;

        /// <summary>
        /// Captures a new <see cref="CausalityTrace"/> from the current stack and the current traces, if <see cref="IsEnabled"/> is <see langword="true"/>.
        /// </summary>
        /// <returns>The captured <see cref="CausalityTrace"/>, or <see langword="null"/> if <see cref="IsEnabled"/> is <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static CausalityTrace Capture()
            => s_isEnabled ? new CausalityTrace(1) : null;

        // Internal so that it can be captured regardless of IsEnabled.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal CausalityTrace(int skipFrames)
        {
#if PROTO_PROMISE_DEVELOPER_MODE // Don't skip frames in developer mode.
            _stackTrace = new StackTrace(1, true);
#else
            _stackTrace = new StackTrace(skipFrames + 1, true);
#endif
            _previousTraces = ts_traces?.ToArray() ?? Array.Empty<CausalityTrace>();
            _depth = 1 + GetMaxDepth(_previousTraces);

            int maxDepth = s_maxDepth;
            if (_depth > maxDepth)
            {
                // Keep the most recent half instead of exactly maxDepth, so that the cost of copying is amortized over many captures.
                int keepDepth = Math.Max(maxDepth / 2, 1);
                _previousTraces = TruncatePreviousTraces(_previousTraces, keepDepth - 1, new Dictionary<(CausalityTrace, int), CausalityTrace>(), out _isTruncated);
                _depth = 1 + GetMaxDepth(_previousTraces);
            }
        }

        private CausalityTrace(StackTrace stackTrace, CausalityTrace[] previousTraces, bool isTruncated)
        {
            _stackTrace = stackTrace;
            _previousTraces = previousTraces;
            _depth = 1 + GetMaxDepth(previousTraces);
            _isTruncated = isTruncated;
        }

        private static int GetMaxDepth(CausalityTrace[] traces)
        {
            int maxDepth = 0;
            foreach (var trace in traces)
            {
                if (trace != null && trace._depth > maxDepth)
                {
                    maxDepth = trace._depth;
                }
            }
            return maxDepth;
        }

        // Returns the previous traces limited to maxDepth levels. Traces that are already shallow enough are shared rather than copied.
        private static CausalityTrace[] TruncatePreviousTraces(CausalityTrace[] previousTraces, int maxDepth,
            Dictionary<(CausalityTrace, int), CausalityTrace> copies, out bool isTruncated)
        {
            if (maxDepth == 0)
            {
                isTruncated = GetMaxDepth(previousTraces) > 0;
                return Array.Empty<CausalityTrace>();
            }

            isTruncated = false;
            CausalityTrace[] truncatedTraces = null;
            for (int i = 0; i < previousTraces.Length; i++)
            {
                var trace = previousTraces[i];
                var truncatedTrace = Truncate(trace, maxDepth, copies);
                if (truncatedTrace != trace)
                {
                    if (truncatedTraces == null)
                    {
                        truncatedTraces = (CausalityTrace[]) previousTraces.Clone();
                    }
                    truncatedTraces[i] = truncatedTrace;
                }
            }
            return truncatedTraces ?? previousTraces;
        }

        private static CausalityTrace Truncate(CausalityTrace trace, int maxDepth, Dictionary<(CausalityTrace, int), CausalityTrace> copies)
        {
            if (trace == null || trace._depth <= maxDepth)
            {
                return trace;
            }

            // The same trace can be reached through multiple paths, so we cache the copies to preserve sharing.
            if (!copies.TryGetValue((trace, maxDepth), out var copy))
            {
                var previousTraces = TruncatePreviousTraces(trace._previousTraces, maxDepth - 1, copies, out bool isTruncated);
                copy = new CausalityTrace(trace._stackTrace, previousTraces, isTruncated);
                copies.Add((trace, maxDepth), copy);
            }
            return copy;
        }

        /// <summary>
        /// Returns a readable representation of the <see cref="CausalityTrace"/>.
        /// </summary>
        /// <remarks>
        /// The stack trace and its async causes (the first of each <see cref="PreviousTraces"/>) are printed first, like an async stack trace.
        /// The other previous traces (the outer causality scopes) are referred to by letter, and printed in their own sections below,
        /// where an outer scope's outer scopes extend its letter (A, A/A, A/B, B, ...).
        /// <para/>
        /// Every stack trace in the history is printed, but traces that repeat (e.g. from loops or recursion) are only printed once:
        /// the repeated part is printed between <c>[#n Begin]</c> and <c>[#n End]</c> markers, and other places where it occurs refer to it with <c>[#n repeated N times]</c>.
        /// </remarks>
        public override string ToString()
        {
            if (_string == null)
            {
                _string = new Printer(this).Print();
            }
            return _string;
        }

        private string FormatStackTrace()
            => FormatStackTrace(_stackTrace);

        // Formats the stack trace without the frames that are hidden from stack traces, such as ProtoPromise's internal frames.
        internal static string FormatStackTrace(StackTrace stackTrace)
        {
            // .Net 6 has built-in [StackTraceHidden], so we don't need to manually filter the stack frames.
#if NET6_0_OR_GREATER
            return stackTrace.ToString();
#else
            var sb = new StringBuilder();
            foreach (var frame in GetVisibleFrames(stackTrace))
            {
                // Create a new StackTrace to get proper formatting.
                // Some runtimes include a trailing new line and some don't, so we normalize it to match the .Net 6 format (one line per frame, with a trailing new line).
                sb.Append(new StackTrace(frame).ToString().TrimEnd('\r', '\n')).AppendLine();
            }
            return sb.ToString();
#endif
        }

        /// <summary>
        /// Enters a scope in which <paramref name="trace"/> is the current causality trace, so that traces captured within the scope include it in their <see cref="PreviousTraces"/>.
        /// </summary>
        /// <param name="trace">
        /// The trace to make current, typically captured with <see cref="Capture()"/> where the callback that will run in this scope was registered.
        /// May be <see langword="null"/>.
        /// </param>
        /// <returns>A <see cref="Scope"/> that restores the previous causality trace when it is disposed.</returns>
        /// <remarks>
        /// Dispose the returned <see cref="Scope"/> with a <see langword="using"/> statement (C# 8 or later) or a <see langword="try"/>/<see langword="finally"/> block.
        /// Nested scopes must be disposed before outer scopes.
        /// </remarks>
        public static Scope EnterScope(CausalityTrace trace)
            => new Scope(trace);

        // For types that store their own trace in a field: the common case (not traced) is only a null check, and a scope that does nothing when it's disposed.
        [MethodImpl(Promises.Internal.InlineOption)]
        internal static Scope EnterScopeIfNotNull(CausalityTrace trace)
            => trace == null ? default : new Scope(trace);

        // Enters a scope for trace only if it would add anything (see ShouldEnterScope), otherwise returns a scope that does nothing when it's disposed.
        [MethodImpl(Promises.Internal.InlineOption)]
        internal static Scope EnterScopeIfNeeded(CausalityTrace trace)
            => ShouldEnterScope(trace) ? new Scope(trace) : default;

        // Whether entering a scope for trace would add anything: it's non-null and not already the current scope
        // (e.g. Promise.New's callback already entered the deferred's scope before the deferred is resolved).
        [MethodImpl(Promises.Internal.InlineOption)]
        private static bool ShouldEnterScope(CausalityTrace trace)
        {
            if (trace == null)
            {
                return false;
            }
            var traces = ts_traces;
            return traces == null || traces.Count == 0 || traces.Peek() != trace;
        }

        /// <summary>
        /// A scope in which a <see cref="CausalityTrace"/> is current, used while running an async callback or continuation. Created by <see cref="EnterScope(CausalityTrace)"/>.
        /// </summary>
        /// <remarks>
        /// Dispose it with a <see langword="using"/> statement (C# 8 or later) or a <see langword="try"/>/<see langword="finally"/> block.
        /// </remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly ref struct Scope
#if NET9_0_OR_GREATER
            : IDisposable
#endif
        {
            private readonly Stack<CausalityTrace> _traces;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            private readonly CausalityTrace _trace;
#endif

            internal Scope(CausalityTrace trace)
            {
                if (trace == null & !s_isEnabled)
                {
                    // Nothing to record, so this scope is a no-op.
                    _traces = null;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _trace = null;
#endif
                    return;
                }
                _traces = ts_traces;
                if (_traces == null)
                {
                    _traces = new Stack<CausalityTrace>();
                    ts_traces = _traces;
                }
                _traces.Push(trace);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _trace = trace;
#endif
            }

            /// <summary>
            /// Disposes the scope, restoring the previous <see cref="CausalityTrace"/>.
            /// </summary>
            public void Dispose()
            {
                if (_traces == null)
                {
                    // This scope was a no-op.
                    return;
                }
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (_traces.Count == 0)
                {
                    throw new System.InvalidOperationException($"No {nameof(Scope)} to dispose.");
                }
                if (_traces.Peek() != _trace)
                {
                    throw new System.InvalidOperationException($"Nested {nameof(Scope)}s must be disposed before outer scopes.");
                }
#endif
                _traces.Pop();
            }
        } // ref struct Scope
    } // class CausalityTrace
} // namespace Proto.Diagnostics
