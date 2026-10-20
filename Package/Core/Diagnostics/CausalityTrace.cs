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
using System.Threading;

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

        // The fields have no initializers, so that the class has no static constructor. Otherwise, IL2CPP checks that the class is initialized
        // where the fields are accessed, which includes every read of IsEnabled on hot paths.
        private static bool s_isEnabled;
        // Stored as an offset from the default, so that the default doesn't need an initializer.
        private static int s_maxDepthOffset;
        private const int DefaultMaxDepth = 128;

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
                    // Objects that were traced may be pooled again, and exceptions may be rejected again, so we release their traces.
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
            get => s_maxDepthOffset + DefaultMaxDepth;
            set
            {
                if (value < 1)
                {
                    throw new Promises.ArgumentOutOfRangeException(nameof(value), $"{nameof(MaxDepth)} must be greater than 0.", Promises.Internal.GetFormattedStacktrace());
                }
                s_maxDepthOffset = value - DefaultMaxDepth;
            }
        }

        private readonly StackTrace _stackTrace;
        private readonly CausalityTrace[] _previousTraces;
        // The number of levels in this trace, including itself.
        private readonly int _depth;
        // Whether older previous traces were dropped to stay within MaxDepth.
        private readonly bool _isTruncated;
        private readonly int _threadId;
        // Read when the trace is captured, because the thread's name may change later.
        private readonly string _threadName;
        private readonly bool _isThreadPoolThread;
        private readonly bool _isBackgroundThread;
        private readonly long _timestamp;
        // The most recent truncated copy of this trace, so that captures in the same scope share it instead of copying it again.
        private TruncatedCopy _truncatedCopy;
        // Lazily created.
        private ReadOnlyCollection<CausalityTrace> _readOnlyPreviousTraces;
        private string _string;

        // Internal so that tests can check that the truncation is printed.
        internal bool IsTruncated => _isTruncated;

        /// <summary>
        /// The raw <see cref="System.Diagnostics.StackTrace"/> of where this <see cref="CausalityTrace"/> was captured.
        /// </summary>
        /// <remarks>
        /// This includes frames that are normally hidden from stack traces, such as ProtoPromise's internal frames. Use <see cref="GetFrames"/> to exclude them.
        /// </remarks>
        public StackTrace StackTrace => _stackTrace;

        /// <summary>
        /// The managed thread id (see <see cref="Thread.ManagedThreadId"/>) of the thread that captured this <see cref="CausalityTrace"/>.
        /// </summary>
        public int ThreadId => _threadId;

        /// <summary>
        /// The name of the thread that captured this <see cref="CausalityTrace"/> when it was captured, or <see langword="null"/> if the thread had no name.
        /// </summary>
        public string ThreadName => _threadName;

        /// <summary>
        /// Whether the thread that captured this <see cref="CausalityTrace"/> is a thread pool thread (see <see cref="Thread.IsThreadPoolThread"/>).
        /// </summary>
        public bool IsThreadPoolThread => _isThreadPoolThread;

        /// <summary>
        /// Whether the thread that captured this <see cref="CausalityTrace"/> was a background thread when it was captured (see <see cref="Thread.IsBackground"/>).
        /// </summary>
        public bool IsBackgroundThread => _isBackgroundThread;

        /// <summary>
        /// When this <see cref="CausalityTrace"/> was captured, as a timestamp of <see cref="TimeProvider.System"/> (see <see cref="TimeProvider.GetTimestamp"/>).
        /// </summary>
        /// <remarks>
        /// Use <see cref="TimeProvider.GetElapsedTime(long, long)"/> to get the time between the timestamps of two traces.
        /// </remarks>
        public long Timestamp => _timestamp;

        /// <summary>
        /// The <see cref="CausalityTrace"/>s of the operations that were running when this trace was captured, from innermost to outermost.
        /// An operation is anything that entered a <see cref="Scope"/>, such as a callback being invoked or a deferred being completed.
        /// </summary>
        /// <remarks>
        /// The first element is the trace of where the innermost running operation was started (e.g. where the callback was registered), which is the async cause of this trace.
        /// Each following element is the trace of where an outer operation was started, which was still running when the operation before it ran synchronously.
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
            // Match the .Net 6+ runtime's StackTrace.ToString() filtering, so that frames are filtered the same way on all runtimes
            // (except that the runtime never hides the outermost frame, see FormatStackTrace).
            if ((method.MethodImplementationFlags & System.Reflection.MethodImplAttributes.AggressiveInlining) != 0
                || IsStackTraceHidden(method))
            {
                return false;
            }
            var declaringType = method.DeclaringType;
            return declaringType == null || !IsStackTraceHidden(declaringType);
        }

        // Like the runtime, if the attributes can't be read (e.g. an attribute's assembly is missing), we behave as if they are not present.
        private static bool IsStackTraceHidden(System.Reflection.MemberInfo member)
        {
            try
            {
#if NET6_0_OR_GREATER
                // Match the runtime, which only hides frames with its own StackTraceHiddenAttribute.
                return member.IsDefined(typeof(StackTraceHiddenAttribute), false);
#else
                // Before .Net 6, StackTraceHiddenAttribute doesn't exist, so libraries define their own (like we do, see ForOldRuntime/StackTraceHiddenAttribute.cs).
                // We match it by name instead of by type, so that it works with any of them.
                // Reading the attribute data doesn't construct the attributes.
                foreach (var attributeData in member.GetCustomAttributesData())
                {
                    if (attributeData.AttributeType.FullName == "System.Diagnostics.StackTraceHiddenAttribute")
                    {
                        return true;
                    }
                }
                return false;
#endif
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Captures a new <see cref="CausalityTrace"/> from the current stack and the current traces, if <see cref="IsEnabled"/> is <see langword="true"/>.
        /// </summary>
        /// <param name="skipFrames">The number of frames up the stack from which to start the trace.</param>
        /// <returns>The captured <see cref="CausalityTrace"/>, or <see langword="null"/> if <see cref="IsEnabled"/> is <see langword="false"/>.</returns>
        // NoInlining so that skipFrames is accurate.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static CausalityTrace Capture(int skipFrames)
        {
            if (skipFrames < 0)
            {
                throw new Promises.ArgumentOutOfRangeException(nameof(skipFrames), $"{nameof(skipFrames)} must be greater than or equal to 0.", Promises.Internal.GetFormattedStacktrace());
            }
            return s_isEnabled ? new CausalityTrace(skipFrames + 1) : null;
        }

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
            _timestamp = TimeProvider.System.GetTimestamp();
            var thread = Thread.CurrentThread;
            _threadId = thread.ManagedThreadId;
            _threadName = thread.Name;
            _isThreadPoolThread = thread.IsThreadPoolThread;
            _isBackgroundThread = thread.IsBackground;
#if PROTO_PROMISE_DEVELOPER_MODE // Don't skip frames in developer mode.
            _stackTrace = new StackTrace(1, true);
#else
            _stackTrace = new StackTrace(skipFrames + 1, true);
#endif
            _previousTraces = ts_traces?.ToArray() ?? Array.Empty<CausalityTrace>();
            _depth = 1 + GetMaxDepth(_previousTraces);

            int maxDepth = MaxDepth;
            if (_depth > maxDepth)
            {
                // Keep the most recent half instead of exactly maxDepth, so that the cost of copying is amortized over many captures.
                int keepDepth = Math.Max(maxDepth / 2, 1);
                _previousTraces = TruncatePreviousTraces(_previousTraces, keepDepth - 1, new Dictionary<(CausalityTrace, int), CausalityTrace>(), out _isTruncated);
                _depth = 1 + GetMaxDepth(_previousTraces);
            }
        }

        // A truncated copy of a trace, which shares its stack trace, where it was captured, and its signature (if it was already created).
        // Internal so that tests can build traces with the same stack trace.
        internal CausalityTrace(CausalityTrace trace, CausalityTrace[] previousTraces, bool isTruncated)
        {
            _stackTrace = trace._stackTrace;
            _threadId = trace._threadId;
            _threadName = trace._threadName;
            _isThreadPoolThread = trace._isThreadPoolThread;
            _isBackgroundThread = trace._isBackgroundThread;
            _timestamp = trace._timestamp;
            _signature = Volatile.Read(ref trace._signature);
            _previousTraces = previousTraces;
            _depth = 1 + GetMaxDepth(previousTraces);
            _isTruncated = isTruncated;
        }

        private static int GetMaxDepth(CausalityTrace[] traces)
        {
            int maxDepth = 0;
            foreach (var trace in traces)
            {
                if (trace._depth > maxDepth)
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
            if (trace._depth <= maxDepth)
            {
                return trace;
            }

            // Every capture in a scope whose trace is too deep truncates the same traces, so the copies are cached on the traces.
            var truncatedCopy = Volatile.Read(ref trace._truncatedCopy);
            if (truncatedCopy != null && truncatedCopy._maxDepth == maxDepth)
            {
                return truncatedCopy._copy;
            }

            // The same trace can be reached through multiple paths, so we also cache the copies of this truncation to preserve sharing.
            if (!copies.TryGetValue((trace, maxDepth), out var copy))
            {
                var previousTraces = TruncatePreviousTraces(trace._previousTraces, maxDepth - 1, copies, out bool isTruncated);
                copy = new CausalityTrace(trace, previousTraces, isTruncated);
                copies.Add((trace, maxDepth), copy);
                // This may be set from multiple threads, but every copy for the same depth is equivalent.
                // It's published with a release fence, so that other threads can't see the TruncatedCopy before its fields are written (e.g. on ARM).
                Volatile.Write(ref trace._truncatedCopy, new TruncatedCopy(copy, maxDepth));
            }
            return copy;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class TruncatedCopy
        {
            internal readonly CausalityTrace _copy;
            internal readonly int _maxDepth;

            internal TruncatedCopy(CausalityTrace copy, int maxDepth)
            {
                _copy = copy;
                _maxDepth = maxDepth;
            }
        }

        /// <summary>
        /// Returns a readable representation of the <see cref="CausalityTrace"/>.
        /// </summary>
        /// <remarks>
        /// The stack trace and its async causes (the first of each <see cref="PreviousTraces"/>) are printed first, like an async stack trace.
        /// The other previous traces (the outer causality scopes) are referred to by letter, and printed in their own sections below,
        /// where an outer scope's outer scopes extend its letter (A, A/A, A/B, B, ...).
        /// <para/>
        /// Each stack trace is preceded by the thread that captured it, and how long before this trace it was captured
        /// (e.g. <c>[Thread 5 (thread pool), 12.345 ms earlier]</c>). Threads without a name are printed as <c>(thread pool)</c>, <c>(background)</c> or <c>(foreground)</c>.
        /// <para/>
        /// Every stack trace in the history is printed, but traces that repeat (e.g. from loops or recursion) are only printed once:
        /// the repeated part is printed between <c>[#n Begin]</c> and <c>[#n End]</c> markers, and other places where it occurs refer to it with <c>[#n repeated N times]</c>.
        /// Where the repeats are first referred to, they are followed by their threads and times.
        /// Outer scopes that are printed as another outer scope with the same stack trace are also followed by their threads and times.
        /// The threads and times of other traces that are folded this way (e.g. the outer scopes of repeats) are not printed.
        /// A stack trace whose frames are all hidden from stack traces is printed as <c>[All frames are hidden]</c>.
        /// </remarks>
        public override string ToString()
        {
            // This may be set from multiple threads, but every string is equivalent.
            var str = Volatile.Read(ref _string);
            if (str == null)
            {
                str = Printer.Print(this);
                Volatile.Write(ref _string, str);
            }
            return str;
        }

        private string FormatStackTrace()
            => FormatStackTrace(_stackTrace);

        // Formats the stack trace without the frames that are hidden from stack traces, such as ProtoPromise's internal frames.
        // We filter the frames even on .Net 6+, which has built-in [StackTraceHidden], because its StackTrace.ToString() never hides the outermost frame,
        // even if it's annotated (e.g. when the engine calls into ProtoPromise directly), and so that the frames are the same as GetFrames() on all runtimes.
        internal static string FormatStackTrace(StackTrace stackTrace)
        {
            var sb = new StringBuilder();
            foreach (var frame in GetVisibleFrames(stackTrace))
            {
                // Create a new StackTrace to get proper formatting.
                // Some runtimes include a trailing new line and some don't, so we normalize it to match the .Net 6 format (one line per frame, with a trailing new line).
                sb.Append(new StackTrace(frame).ToString().TrimEnd('\r', '\n')).AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Enters a scope in which <paramref name="trace"/> is the current causality trace, so that traces captured within the scope include it in their <see cref="PreviousTraces"/>.
        /// </summary>
        /// <param name="trace">
        /// The trace to make current, typically captured with <see cref="Capture()"/> where the callback that will run in this scope was registered.
        /// If it is <see langword="null"/>, the current causality trace is not changed, but the returned <see cref="Scope"/> must still be disposed.
        /// </param>
        /// <returns>A <see cref="Scope"/> that restores the previous causality trace when it is disposed.</returns>
        /// <remarks>
        /// Dispose the returned <see cref="Scope"/> with a <see langword="using"/> statement (C# 8 or later) or a <see langword="try"/>/<see langword="finally"/> block.
        /// Nested scopes must be disposed before outer scopes.
        /// <para/>
        /// Disposing a <see cref="Scope"/> more than once, or after its outer scope, throws an <see cref="InvalidOperationException"/>
        /// (in RELEASE mode, only if <paramref name="trace"/> is not <see langword="null"/>).
        /// Disposing a <see cref="Scope"/> before its nested scopes (e.g. a callback entered a scope without disposing it) is reported to <see cref="Promises.Promise.Config.UncaughtRejectionHandler"/>,
        /// and the nested scopes are also exited, so that the current causality trace is restored (in RELEASE mode, only if <paramref name="trace"/> is not <see langword="null"/>).
        /// </remarks>
        // In RELEASE mode, a null trace is only a null check. In DEBUG mode, every scope is checked for being disposed correctly,
        // even with a null trace, and while causality traces are enabled, remembers where it was entered to report it if it's never disposed (see DisposedChecker).
        [MethodImpl(Promises.Internal.InlineOption)]
        public static Scope EnterScope(CausalityTrace trace)
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            => new Scope(trace, true);
#else
            => trace == null ? default : new Scope(trace, true);
#endif

        // For the library's own scopes, which are always disposed in finally blocks.
        // The common case (not traced) is only a null check, so that types that store their own trace can call this on their fast paths.
        [MethodImpl(Promises.Internal.InlineOption)]
        internal static Scope EnterScopeInternal(CausalityTrace trace)
            => trace == null ? default : new Scope(trace, false);

        // Enters a scope for trace only if it would add anything (see ShouldEnterScope), otherwise returns a scope that does nothing when it's disposed.
        [MethodImpl(Promises.Internal.InlineOption)]
        internal static Scope EnterScopeIfNeeded(CausalityTrace trace)
            => ShouldEnterScope(trace) ? new Scope(trace, false) : default;

        // Whether trace is the scope that is entered on this thread.
        internal static bool IsCurrentScope(CausalityTrace trace)
        {
            var traces = ts_traces;
            return traces != null && traces.Count > 0 && traces.Peek() == trace;
        }

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
            private readonly CausalityTrace _trace;
            // The number of traces on the stack after this scope was entered (including its own trace, if it has one), to find it if nested scopes were not disposed.
            private readonly int _depth;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            // Copies of the scope share the checker, so that disposing any copy marks them all as disposed.
            private readonly DisposedChecker _disposedChecker;

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class DisposedChecker
#if PROMISE_DEBUG
                : Promises.Internal.ITraceable
#endif
            {
#if PROMISE_DEBUG
                // In DEBUG mode, causality traces are stored in fields (see ITraceable).
                CausalityTrace Promises.Internal.ITraceable.Trace { get; set; }

#endif
                // The ids of the checked scopes that are entered on this thread, to check that they are disposed in order.
                // We keep their ids instead of the checkers, so that a checker whose scope is never disposed can still be collected and reported.
                [ThreadStatic]
                private static List<long> ts_enteredIds;
                [ThreadStatic]
                private static long ts_nextId;

                private readonly long _id;
                // The number of checked scopes that were entered on this thread when this scope was entered, including itself.
                private readonly int _depth;
                // Whether the scope was entered by EnterScope (see Scope.Dispose).
                internal readonly bool _isPublic;
                private bool _isDisposed;

                internal DisposedChecker(bool isPublic)
                {
                    _isPublic = isPublic;
#if !PROTO_PROMISE_DEVELOPER_MODE
                    // Capturing where the scope was entered is expensive, so we only capture it for scopes that users enter.
                    // The library always disposes its own scopes in finally blocks.
                    if (isPublic)
#endif
                    {
                        Promises.Internal.SetCreatedStacktrace(this);
                    }
                    var enteredIds = ts_enteredIds;
                    if (enteredIds == null)
                    {
                        enteredIds = new List<long>();
                        ts_enteredIds = enteredIds;
                    }
                    _id = ++ts_nextId;
                    enteredIds.Add(_id);
                    _depth = enteredIds.Count;
                }

                ~DisposedChecker()
                {
                    Promises.Internal.ReportRejection(new Promises.UnreleasedObjectException(
                        $"A {nameof(CausalityTrace)}.{nameof(Scope)} was garbage collected without being disposed."), this);
                }

                // Throws if this dispose is improper (the scope was already disposed, or its outer scope was disposed first).
                // Returns whether its nested scopes were not disposed, which is not this scope's fault (e.g. a callback that entered a scope without disposing it),
                // so they are exited along with this scope, then reported (see Scope.Dispose).
                internal bool Dispose()
                {
                    if (_isDisposed)
                    {
                        throw new System.ObjectDisposedException($"{nameof(CausalityTrace)}.{nameof(Scope)}");
                    }
                    _isDisposed = true;
                    GC.SuppressFinalize(this);

                    var enteredIds = ts_enteredIds;
                    if (enteredIds == null || enteredIds.Count < _depth || enteredIds[_depth - 1] != _id)
                    {
                        // Its outer scope was disposed first, which already exited this scope.
                        throw new System.InvalidOperationException($"A {nameof(CausalityTrace)}.{nameof(Scope)} was disposed after its outer {nameof(Scope)}. Nested {nameof(Scope)}s must be disposed before outer {nameof(Scope)}s.");
                    }
                    bool hasNestedScopes = enteredIds.Count > _depth;
                    enteredIds.RemoveRange(_depth - 1, enteredIds.Count - _depth + 1);
                    return hasNestedScopes;
                }
            }
#endif

            // isPublic is whether the scope was entered by EnterScope, which is only used in DEBUG mode (see DisposedChecker).
            // The trace is not null, except from EnterScope in DEBUG mode. A scope that does nothing is the default value.
            internal Scope(CausalityTrace trace, bool isPublic)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                // Created before the trace is pushed, so that its trace is where the scope was entered.
                _disposedChecker = new DisposedChecker(isPublic);
#endif
                _traces = ts_traces;
                if (_traces == null)
                {
                    _traces = new Stack<CausalityTrace>();
                    ts_traces = _traces;
                }
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (trace == null)
                {
                    // Nothing to record, but the scope is still checked for being disposed correctly,
                    // and it still exits its nested scopes if they were not disposed (see Exit).
                    _trace = null;
                    _depth = _traces.Count;
                    return;
                }
#endif
                _traces.Push(trace);
                _trace = trace;
                _depth = _traces.Count;
            }

            /// <summary>
            /// Disposes the scope, restoring the previous <see cref="CausalityTrace"/>.
            /// </summary>
            [MethodImpl(Promises.Internal.InlineOption)]
            public void Dispose()
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (_disposedChecker == null)
                {
                    // This scope does nothing.
                    return;
                }
                // The checker throws if this dispose is improper.
                bool hasNestedScopes = _disposedChecker.Dispose();
                Exit();
                if (hasNestedScopes)
                {
                    // Reported after the nested scopes were exited (see Exit).
                    // Where a scope that users entered was entered is more useful than the trace they passed (see ReportNestedScopesNotDisposed).
                    ReportNestedScopesNotDisposed((_disposedChecker._isPublic ? Promises.Internal.GetTrace(_disposedChecker) : null) ?? _trace);
                }
#else
                // The common case (not traced) is only a null check.
                if (_traces == null)
                {
                    return;
                }
                Exit();
#endif
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void Exit()
            {
                var traces = _traces;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (_trace == null)
                {
                    // A null trace was not pushed, so we only exit the nested scopes that were not disposed.
                    // The checker already verified that this scope is still entered.
                    while (traces.Count > _depth)
                    {
                        traces.Pop();
                    }
                    return;
                }
#endif
                if (traces.Count == _depth && traces.Peek() == _trace)
                {
                    traces.Pop();
                    return;
                }

                if (!IsEntered(traces))
                {
                    // This scope was already exited, by disposing it or its outer scope, so this dispose is improper.
                    // In DEBUG mode, the checker already threw.
                    // We don't pop another scope's trace, so that traces captured later on this thread are not corrupted.
                    throw new System.InvalidOperationException($"A {nameof(CausalityTrace)}.{nameof(Scope)} was disposed more than once, or after its outer {nameof(Scope)}. Nested {nameof(Scope)}s must be disposed before outer {nameof(Scope)}s.");
                }

                // Nested scopes were not disposed, which is not this scope's fault. Instead of throwing, we recover and report it,
                // so that the operation that is running (e.g. a callback that entered a scope without disposing it) can complete,
                // and traces that are captured later on this thread are not corrupted.
                // We exit the nested scopes that were not disposed (then this scope) before reporting it, so that the rejection handler runs with the restored traces.
                // In DEBUG mode, Dispose reports it.
                while (traces.Count >= _depth)
                {
                    traces.Pop();
                }
#if !(PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE)
                ReportNestedScopesNotDisposed(_trace);
#endif
            }

            // Whether this scope's trace is still on the stack where it was pushed.
            private bool IsEntered(Stack<CausalityTrace> traces)
            {
                if (traces.Count < _depth)
                {
                    return false;
                }
                // The stack enumerates from the top.
                int index = traces.Count - _depth;
                foreach (var trace in traces)
                {
                    if (index-- == 0)
                    {
                        return trace == _trace;
                    }
                }
                return false;
            }

            // The exception is not thrown (see Exit), so we give it the stack trace of where the scope was disposed.
            // Its causality trace is where the scope was entered if users entered it and it was captured (in DEBUG mode, see DisposedChecker),
            // otherwise the scope's trace. For the library's scopes, that's where the callback that didn't dispose its nested scopes was registered.
            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ReportNestedScopesNotDisposed(CausalityTrace trace)
                => Promises.Internal.ReportUnhandledValue(new Promises.InvalidOperationException(
                    $"A {nameof(CausalityTrace)}.{nameof(Scope)} was disposed before its nested {nameof(Scope)}s. Nested {nameof(Scope)}s must be disposed before outer {nameof(Scope)}s.",
                    FormatStackTrace(new StackTrace(true))), IsEnabled ? trace : null);
        } // ref struct Scope
    } // class CausalityTrace
} // namespace Proto.Diagnostics
