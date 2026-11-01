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
    /// and the causality traces of the operations that were running at that time (see <see cref="Previous"/> and <see cref="OuterTraces"/>).
    /// </summary>
    /// <remarks>
    /// Previous traces may be shared by multiple causality traces.
    /// </remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
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
        private static int s_maxOuterTracesOffset;
        private const int DefaultMaxOuterTraces = 8;

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
        /// <para/>
        /// Only operations that are started after this is enabled are traced. When this is toggled, the traces that ProtoPromise captured for operations that are still running
        /// are not guaranteed to remain, or to be released, so those operations may or may not have their traces when they complete (e.g. in an <see cref="Promises.UnhandledException"/>).
        /// Traces that you captured with <see cref="Capture()"/> are not affected.
        /// </remarks>
        public static bool IsEnabled
        {
            [MethodImpl(Promises.Internal.InlineOption)]
            get => s_isEnabled;
            set
            {
                // Locked so that concurrent sets can't leave this enabled without a trace table, or replace a table that was already in use,
                // and so that the settings that are combined with this are updated from its latest value (see Promise.Config.UpdateCombinedSettings).
                Promises.Promise.Config.s_lock.Enter();
                try
                {
                    if (value == s_isEnabled)
                    {
                        return;
                    }
                    if (value)
                    {
                        // The table is created before this is enabled, so that operations that see this enabled can store their traces.
                        Promises.Internal.CreateTraceTable();
                        // Pooling is disabled before this is enabled, so that objects that are traced can't be pooled with their traces.
                        Promises.Internal.s_isObjectPoolingDisabled = true;
                        s_isEnabled = true;
                        Promises.Promise.Config.UpdateCombinedSettings();
                        return;
                    }
                    s_isEnabled = false;
                    Promises.Promise.Config.UpdateCombinedSettings();
                    // Objects that were traced may be pooled again, and exceptions may be rejected again, so we release their traces.
                    Promises.Internal.ReleaseTraceTable();
                    // The cached methods of the frames are released too, so that they don't keep their assemblies from being unloaded
                    // (unless frames are printed again while traces are disabled, e.g. by a DEBUG validation exception).
                    MethodCache.s_isVisible.Clear();
                    MethodCache.s_asyncStateMachineTypes.Clear();
                }
                finally
                {
                    Promises.Promise.Config.s_lock.Exit();
                }
            }
        }

        /// <summary>
        /// The maximum number of traces in the chain of a causality trace and its causes (each one's <see cref="Previous"/> trace), including itself. The default is 128.
        /// When a newly captured trace would exceed this depth, its oldest traces are dropped, keeping the most recent half of this depth.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">The value is less than 1.</exception>
        /// <remarks>
        /// This prevents causality traces from growing without bound when callbacks are recursively scheduled
        /// (e.g. a callback that registers another callback that registers another callback...).
        /// A trace that is captured at the same location as its previous trace, on the same thread, on any thread pool thread, or on another thread with the same name
        /// (e.g. a callback that registers itself again),
        /// replaces the previous trace instead of growing the chain (see <see cref="Repeats"/>).
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

        /// <summary>
        /// The maximum number of <see cref="OuterTraces"/> that a causality trace keeps. The default is 8.
        /// When more outer traces would be kept, the outermost ones are dropped.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">The value is less than 0.</exception>
        /// <remarks>
        /// This prevents causality traces from growing without bound when operations are nested deeply
        /// (e.g. callbacks that complete other promises synchronously, or scopes that are entered recursively).
        /// </remarks>
        public static int MaxOuterTraces
        {
            [MethodImpl(Promises.Internal.InlineOption)]
            get => s_maxOuterTracesOffset + DefaultMaxOuterTraces;
            set
            {
                if (value < 0)
                {
                    throw new Promises.ArgumentOutOfRangeException(nameof(value), $"{nameof(MaxOuterTraces)} must not be negative.", Promises.Internal.GetFormattedStacktrace());
                }
                s_maxOuterTracesOffset = value - DefaultMaxOuterTraces;
            }
        }

        private readonly StackTrace _stackTrace;
        // The index of the first frame of the stack trace that is part of this trace (see CaptureWhereAsyncFunctionAwaited and CaptureWhereAsyncFunctionWasCalled).
        private readonly int _firstFrame;
        // The cause (see Previous).
        private readonly CausalityTrace _previous;
        // Copies without their histories (see OuterTraces and GetWithoutHistory).
        private readonly CausalityTrace[] _outerTraces;
        private readonly RepeatGroup[] _repeats;
        // The number of traces in the chain of this trace and its previous traces, including itself.
        private readonly int _depth;
        // Whether older traces were dropped from the chain to stay within MaxDepth.
        private readonly bool _isTruncated;
        // Whether outer traces were dropped to stay within MaxOuterTraces.
        private readonly bool _areOuterTracesTruncated;
        private readonly int _threadId;
        // Read when the trace is captured, because the thread's name may change later.
        private readonly string _threadName;
        private readonly bool _isThreadPoolThread;
        private readonly bool _isBackgroundThread;
        private readonly long _timestamp;
        // The most recent truncated copy of this trace, so that captures in the same scope share it instead of copying it again.
        private TruncatedCopy _truncatedCopy;
        // A copy of this trace without its history, for the traces whose outer traces include it (see OuterTraces). Lazily created.
        private CausalityTrace _withoutHistory;
        // Lazily created.
        private ReadOnlyCollection<CausalityTrace> _readOnlyOuterTraces;
        // Lazily created.
        private ReadOnlyCollection<RepeatGroup> _readOnlyRepeats;
        private string _string;

        // Internal so that tests can check that the truncations are printed.
        internal bool IsTruncated => _isTruncated;
        internal bool AreOuterTracesTruncated => _areOuterTracesTruncated;

        /// <summary>
        /// The raw <see cref="System.Diagnostics.StackTrace"/> of where this <see cref="CausalityTrace"/> was captured.
        /// </summary>
        /// <remarks>
        /// This includes frames that are normally hidden from stack traces, such as ProtoPromise's internal frames,
        /// and, for the trace of an <c>async Promise</c> function, the function's own frame if it was captured where the function first awaited. Use <see cref="GetFrames"/> to exclude them.
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
        /// The <see cref="CausalityTrace"/> of the operation that was running when this trace was captured, which is the async cause of this trace
        /// (e.g. where the callback that was running was registered), with its own <see cref="Previous"/> trace,
        /// or <see langword="null"/> if no operation was running, or if it was dropped to stay within <see cref="MaxDepth"/>.
        /// </summary>
        /// <remarks>
        /// An operation is anything that entered a <see cref="Scope"/>, such as a callback being invoked, an async function running,
        /// or a callback that was scheduled to run on a <see cref="SynchronizationContext"/> or the thread pool (whose trace is where it was scheduled).
        /// If operations were nested, this is the innermost operation, and the others are in <see cref="OuterTraces"/>.
        /// <para/>
        /// An operation whose trace was captured on the same thread by a call that was still running (e.g. <c>Promise.New</c>, which invokes its callback) is skipped,
        /// because its frames are already in <see cref="StackTrace"/> (or in one of the <see cref="OuterTraces"/>), unless it has <see cref="Repeats"/>.
        /// Its cause is this trace's cause instead, and its outer traces are kept. This compares what is printed (the methods and lines of the frames),
        /// so a trace that was captured by an earlier call on the same line (or in the same method, if line numbers are not available) may also be skipped.
        /// An operation whose trace was captured at the same location as this trace is folded into this trace (see <see cref="Repeats"/>).
        /// So this may be <see langword="null"/> while <see cref="OuterTraces"/> is not empty.
        /// <para/>
        /// Which traces are skipped, folded or dropped is decided on a best-effort basis, and depends on the frames that the runtime provides,
        /// so this (like <see cref="OuterTraces"/> and <see cref="Repeats"/>) is meant for diagnostics, not for program logic.
        /// </remarks>
        public CausalityTrace Previous => _previous;

        /// <summary>
        /// The <see cref="CausalityTrace"/>s of the other operations that were running when this trace was captured, outside of the <see cref="Previous"/> trace's operation,
        /// from the innermost (e.g. where the code that invoked the callback that was running was scheduled from, which is not in <see cref="StackTrace"/> if the callback was scheduled).
        /// </summary>
        /// <remarks>
        /// They are copies of the operations' traces without their own <see cref="Previous"/> traces, <see cref="OuterTraces"/> or <see cref="Repeats"/>,
        /// so that a chain of causes is only printed once.
        /// <para/>
        /// An outer trace is skipped if it adds nothing: its frames are already in <see cref="StackTrace"/> (e.g. a callback that a context invoked synchronously),
        /// it's already in the chain of <see cref="Previous"/> traces or their outer traces, it was captured at the same location as this trace, the <see cref="Previous"/> trace,
        /// or another outer trace, or its frames are the outer frames of another outer trace that was captured on the same thread, which replaces it
        /// (their innermost frames are compared by method and line, because the call may have moved on since the outer trace was captured). If IL offsets are not available (e.g. IL2CPP), an outer trace that was captured by an earlier call on the same line of a method that is still running
        /// (or in the same method, if line numbers are not available either) may also be skipped, because it's printed like the outer frames of <see cref="StackTrace"/>.
        /// The outer traces of an operation that was skipped or folded into this trace (see <see cref="Previous"/>) are kept after this trace's own.
        /// At most <see cref="MaxOuterTraces"/> are kept.
        /// <para/>
        /// Like <see cref="Previous"/>, this is decided on a best-effort basis, and is meant for diagnostics, not for program logic.
        /// </remarks>
        public IReadOnlyList<CausalityTrace> OuterTraces
        {
            get
            {
                if (_outerTraces.Length == 0)
                {
                    // Most traces have no outer traces. The empty array is shared, and it can't be modified.
                    return _outerTraces;
                }
                // This may be set from multiple threads, but every collection is equivalent.
                var outerTraces = Volatile.Read(ref _readOnlyOuterTraces);
                if (outerTraces == null)
                {
                    outerTraces = new ReadOnlyCollection<CausalityTrace>(_outerTraces);
                    Volatile.Write(ref _readOnlyOuterTraces, outerTraces);
                }
                return outerTraces;
            }
        }

        /// <summary>
        /// Earlier traces that were captured at the same location as this trace (with the same frames, see <see cref="GetFrames"/>),
        /// on the same thread, on any thread pool thread, or on another thread with the same name (e.g. the threads of a custom thread pool), each the cause of the next one
        /// (e.g. a callback that registers itself again).
        /// They were folded into this trace instead of growing the chain of causes (see <see cref="Previous"/>), so this trace's cause is the oldest one's cause (unless it was dropped to stay within <see cref="MaxDepth"/>).
        /// They are grouped by thread, with all thread pool threads in one group, and other named threads grouped by name, ordered from the most recent.
        /// </summary>
        /// <remarks>
        /// Like <see cref="Previous"/>, this is decided on a best-effort basis, and is meant for diagnostics, not for program logic.
        /// </remarks>
        public IReadOnlyList<RepeatGroup> Repeats
        {
            get
            {
                if (_repeats.Length == 0)
                {
                    // Most traces have no repeats. The empty array is shared, and it can't be modified.
                    return _repeats;
                }
                // This may be set from multiple threads, but every collection is equivalent.
                var repeats = Volatile.Read(ref _readOnlyRepeats);
                if (repeats == null)
                {
                    repeats = new ReadOnlyCollection<RepeatGroup>(_repeats);
                    Volatile.Write(ref _readOnlyRepeats, repeats);
                }
                return repeats;
            }
        }

        /// <summary>
        /// Gets the frames of <see cref="StackTrace"/>, excluding frames that are hidden from stack traces, such as ProtoPromise's internal frames,
        /// and, for the trace of an <c>async Promise</c> function, the function's own frame if it was captured where the function first awaited.
        /// </summary>
        public StackFrame[] GetFrames()
            => GetVisibleFrames(_stackTrace, _firstFrame);

        internal static StackFrame[] GetVisibleFrames(StackTrace stackTrace, int firstFrame = 0)
        {
            int frameCount = stackTrace.FrameCount;
            if (frameCount <= firstFrame)
            {
                return Array.Empty<StackFrame>();
            }

            var visibleFrames = new List<StackFrame>(frameCount - firstFrame);
            for (int i = firstFrame; i < frameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
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
            // Reading the attributes is expensive, and every capture compares frames (see IsRepeatOf), so the results are cached.
            var cache = MethodCache.s_isVisible;
            if (!cache.TryGetValue(method, out bool isVisible))
            {
                isVisible = IsVisible(method);
                cache.TryAdd(method, isVisible);
            }
            return isVisible;
        }

        private static bool IsVisible(System.Reflection.MethodBase method)
        {
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

        // In its own class, so that CausalityTrace doesn't need a static constructor (see s_isEnabled).
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private static class MethodCache
        {
            internal static readonly System.Collections.Concurrent.ConcurrentDictionary<System.Reflection.MethodBase, bool> s_isVisible
                = new System.Collections.Concurrent.ConcurrentDictionary<System.Reflection.MethodBase, bool>();
            internal static readonly System.Collections.Concurrent.ConcurrentDictionary<System.Reflection.MethodBase, Type> s_asyncStateMachineTypes
                = new System.Collections.Concurrent.ConcurrentDictionary<System.Reflection.MethodBase, Type>();
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
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="skipFrames"/> is less than 0.</exception>
        // NoInlining so that skipFrames is accurate.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static CausalityTrace Capture(int skipFrames)
        {
            if (skipFrames < 0)
            {
                throw new Promises.ArgumentOutOfRangeException(nameof(skipFrames), $"{nameof(skipFrames)} must be greater than or equal to 0.", Promises.Internal.GetFormattedStacktrace());
            }
            // Clamped so that adding this frame (here and in the constructor) doesn't overflow, nor do the frames that Mono's StackTrace adds to it (Mono crashes if it overflows).
            // A count that large skips every frame either way.
            return s_isEnabled ? new CausalityTrace(Math.Min(skipFrames, int.MaxValue / 2) + 1) : null;
        }

        /// <summary>
        /// Captures a new <see cref="CausalityTrace"/> from the current stack and the current traces, if <see cref="IsEnabled"/> is <see langword="true"/>.
        /// </summary>
        /// <returns>The captured <see cref="CausalityTrace"/>, or <see langword="null"/> if <see cref="IsEnabled"/> is <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static CausalityTrace Capture()
            => s_isEnabled ? new CausalityTrace(1) : null;

        // For an async function's trace, which is captured before its body runs in the non-optimized async mode (see AsyncPromiseRef.TracedAsyncPromiseRef),
        // or where it first awaits in the optimized async mode (see AsyncPromiseRef.TracedAsyncPromiseRefMachine).
        // The frames before the method that the compiler generated for it are dropped (see GetFrameAfterAsyncFunction),
        // so that the trace starts at that method, where it was called.
        // asyncStateMachineType is null if it's not known (the non-optimized async mode creates the async promise before the async function is started),
        // in which case it's the innermost method that the compiler generated for any async function.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static CausalityTrace CaptureWhereAsyncFunctionWasCalled(Type asyncStateMachineType)
            => s_isEnabled ? new CausalityTrace(1, TraceStart.WhereAsyncFunctionWasCalled, asyncStateMachineType) : null;

        // For where an async function awaits (see AsyncPromiseRef.TracedAsyncPromiseRef and AsyncPromiseRef.TracedAsyncPromiseRefMachine).
        // The frames before the async function's frame are dropped (see GetAsyncFunctionFrame), so that it starts where it awaited.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static CausalityTrace CaptureWhereAsyncFunctionAwaited(Type asyncStateMachineType)
            => s_isEnabled ? new CausalityTrace(1, TraceStart.WhereAsyncFunctionAwaited, asyncStateMachineType) : null;

        // Where a trace starts in its stack trace (see the constructor).
        internal enum TraceStart : byte
        {
            // The innermost frame.
            Innermost,
            // Where the async function awaited (see CaptureWhereAsyncFunctionAwaited).
            WhereAsyncFunctionAwaited,
            // Where the async function was called (see CaptureWhereAsyncFunctionWasCalled).
            WhereAsyncFunctionWasCalled
        }

        // Internal so that it can be captured regardless of IsEnabled.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal CausalityTrace(int skipFrames, TraceStart start = TraceStart.Innermost, Type asyncStateMachineType = null)
        {
            _timestamp = TimeProvider.System.GetTimestamp();
            var thread = Thread.CurrentThread;
            _threadId = thread.ManagedThreadId;
            _threadName = thread.Name;
            _isThreadPoolThread = thread.IsThreadPoolThread;
            _isBackgroundThread = thread.IsBackground;
#if PROTO_PROMISE_DEVELOPER_MODE // Don't skip frames in developer mode, which doesn't hide internal frames, so the innermost visible frame isn't dropped either.
            _stackTrace = new StackTrace(1, true);
#else
            _stackTrace = new StackTrace(skipFrames + 1, true);
            if (start != TraceStart.Innermost)
            {
                _firstFrame = start == TraceStart.WhereAsyncFunctionAwaited
                    ? GetAsyncFunctionFrame(_stackTrace, asyncStateMachineType)
                    : GetFrameAfterAsyncFunction(_stackTrace, asyncStateMachineType);
            }
#endif
            _repeats = Array.Empty<RepeatGroup>();

            var traces = ts_traces;
            CausalityTrace previous = null;
            // Most traces are captured in at most one scope, so they have no outer traces, which doesn't allocate.
            var outerTraces = Array.Empty<CausalityTrace>();
            int maxOuterTraces = MaxOuterTraces;
            bool areOuterTracesTruncated = false;
            if (traces != null && traces.Count > 0)
            {
                previous = traces.Peek();
                if (traces.Count > 1)
                {
                    outerTraces = GetOuterTraces(traces, previous, maxOuterTraces, ref areOuterTracesTruncated);
                }
            }

            // The previous trace may have been captured by a call that is still running (e.g. Promise.New, which invokes its callback),
            // in which case its frames are already in this trace, so it's skipped. Its outer traces are kept, so that they're not lost.
            if (previous != null && previous.IsContainedBy(this, outerTraces))
            {
                outerTraces = AddOuterTraces(outerTraces, previous, maxOuterTraces, ref areOuterTracesTruncated);
                _isTruncated = previous._isTruncated;
                previous = previous._previous;
            }
            // The previous trace may have been captured at the same location (e.g. a callback that registers itself again,
            // or a loop through Promise.New whose trace was skipped), in which case it's folded into this trace, so that the chain doesn't grow.
            if (previous != null && previous.IsRepeatOf(this))
            {
                _repeats = RepeatGroup.Add(previous._repeats, previous);
                outerTraces = AddOuterTraces(outerTraces, previous, maxOuterTraces, ref areOuterTracesTruncated);
                _isTruncated = previous._isTruncated;
                previous = previous._previous;
            }
            _previous = previous;
            _depth = previous == null ? 1 : previous._depth + 1;

            int maxDepth = MaxDepth;
            if (_depth > maxDepth)
            {
                // Keep the most recent half instead of exactly maxDepth, so that the cost of copying is amortized over many captures.
                // The oldest trace that is kept is marked as truncated.
                int keepDepth = Math.Max(maxDepth / 2, 1);
                if (keepDepth == 1)
                {
                    _previous = null;
                    _isTruncated = true;
                }
                else
                {
                    _previous = Truncate(previous, keepDepth - 1);
                }
                _depth = keepDepth;
            }
            // This is checked after the chain is truncated, so that an outer trace is not lost with the traces that were dropped.
            _outerTraces = RemovePrintedOuterTraces(outerTraces, _previous);
            _areOuterTracesTruncated = areOuterTracesTruncated;
        }

        // Gets the outer traces that add anything (see GetOuterTraceIndex), from the scopes outside of the innermost scope (which is the previous trace).
        // They're copies without their histories, so that a trace only keeps the chain of its causes, which is limited by MaxDepth.
        // Without the copies, each trace would keep the outer traces' histories, which may keep each other without bound
        // (e.g. a loop that awaits an operation that was started while the previous one was completing).
        [MethodImpl(MethodImplOptions.NoInlining)]
        private CausalityTrace[] GetOuterTraces(Stack<CausalityTrace> traces, CausalityTrace previous, int maxOuterTraces, ref bool areOuterTracesTruncated)
        {
            var outerTraces = new CausalityTrace[Math.Min(traces.Count - 1, maxOuterTraces)];
            int count = 0;
            bool isInnermost = true;
            // The stack enumerates from the top, which is the innermost scope.
            foreach (var trace in traces)
            {
                if (isInnermost)
                {
                    isInnermost = false;
                    continue;
                }
                // The previous trace may be skipped (see IsContainedBy), but an outer trace that repeats it is still already printed in this trace.
                if (trace == previous || trace.IsRepeatOf(previous))
                {
                    continue;
                }
                int index = GetOuterTraceIndex(trace, outerTraces, ref count);
                if (index < 0)
                {
                    continue;
                }
                if (index == count)
                {
                    if (count == outerTraces.Length)
                    {
                        // The rest of the scopes are not compared, so that a deep stack of scopes stays fast.
                        areOuterTracesTruncated = true;
                        break;
                    }
                    ++count;
                }
                outerTraces[index] = trace.GetWithoutHistory();
            }
            if (count < outerTraces.Length)
            {
                if (count == 0)
                {
                    return Array.Empty<CausalityTrace>();
                }
                Array.Resize(ref outerTraces, count);
            }
            return outerTraces;
        }

        // Adds the outer traces of a previous trace that was skipped or folded into this trace, after this trace's own outer traces.
        private CausalityTrace[] AddOuterTraces(CausalityTrace[] outerTraces, CausalityTrace previous, int maxOuterTraces, ref bool areOuterTracesTruncated)
        {
            areOuterTracesTruncated |= previous._areOuterTracesTruncated;
            var otherOuterTraces = previous._outerTraces;
            if (otherOuterTraces.Length == 0)
            {
                return outerTraces;
            }
            var result = new CausalityTrace[Math.Min(outerTraces.Length + otherOuterTraces.Length, Math.Max(outerTraces.Length, maxOuterTraces))];
            outerTraces.CopyTo(result, 0);
            int count = outerTraces.Length;
            bool isReplaced = false;
            foreach (var trace in otherOuterTraces)
            {
                int index = GetOuterTraceIndex(trace, result, ref count);
                if (index < 0)
                {
                    continue;
                }
                if (index < count)
                {
                    isReplaced = true;
                }
                else if (count == result.Length)
                {
                    areOuterTracesTruncated = true;
                    break;
                }
                else
                {
                    ++count;
                }
                result[index] = trace;
            }
            if (count == outerTraces.Length && !isReplaced)
            {
                return outerTraces;
            }
            if (count < result.Length)
            {
                Array.Resize(ref result, count);
            }
            return result;
        }

        // Gets the index to put an outer trace at, or -1 if it doesn't add anything: its frames are already in this trace (e.g. Promise.New, which invokes its callback,
        // or a context that ran a scheduled callback synchronously, which are still in the same call, so their innermost frames are compared exactly),
        // or it was captured at the same location as this trace or another outer trace (e.g. a callback that schedules itself again),
        // or its frames are already in another outer trace. It replaces the other outer traces whose frames are already in it
        // (e.g. an async function's own trace, whose frames are in the trace of where it first awaited, whose scope is outside of the async function's scope):
        // it's put at the first one's index, and the rest of them are removed.
        // The outer traces are compared with each other by their signatures, which are cached, because the same scopes' traces are compared on every capture in them.
        private int GetOuterTraceIndex(CausalityTrace trace, CausalityTrace[] outerTraces, ref int count)
        {
            if (trace.IsRepeatOf(this) || trace.IsOuterPartOf(this, true))
            {
                return -1;
            }
            int index = -1;
            int keptCount = 0;
            for (int i = 0; i < count; i++)
            {
                var outerTrace = outerTraces[i];
                if (index < 0)
                {
                    // Compared by stack trace first, because the traces may be different copies of the same trace (see GetWithoutHistory).
                    if (trace.IsPrintedBy(outerTrace) || trace.IsRepeatOfBySignature(outerTrace))
                    {
                        return -1;
                    }
                    if (outerTrace.IsPrintedBy(trace))
                    {
                        index = i;
                    }
                    else
                    {
                        var outerPart = GetOuterPartBySignature(trace, outerTrace);
                        if (outerPart == trace)
                        {
                            return -1;
                        }
                        if (outerPart == outerTrace)
                        {
                            index = i;
                        }
                    }
                    ++keptCount;
                }
                // After it replaces an outer trace, the rest are only compared for whether it replaces them too. One that was captured at the same location
                // (e.g. on another thread pool thread) has the same frames, so it's replaced too. Any other outer trace that has its frames is on the same thread
                // or shares its capture, so it would also have the frames of the outer trace that it replaced, and they wouldn't both be outer traces.
                else if (!outerTrace.IsPrintedBy(trace) && !trace.IsRepeatOfBySignature(outerTrace) && GetOuterPartBySignature(trace, outerTrace) != outerTrace)
                {
                    outerTraces[keptCount++] = outerTrace;
                }
            }
            if (index < 0)
            {
                return count;
            }
            // The removed outer traces are left past the count, which are overwritten or dropped when the array is resized.
            count = keptCount;
            return index;
        }

        // Removes the outer traces that are already printed: the previous trace, or a trace in the chain of previous traces or their outer traces
        // (e.g. a callback that registered a callback, which captured both traces). Only the previous trace is compared by frames,
        // so that the chain (which may be MaxDepth long) is only compared by stack trace.
        private static CausalityTrace[] RemovePrintedOuterTraces(CausalityTrace[] outerTraces, CausalityTrace previous)
        {
            if (outerTraces.Length == 0 || previous == null)
            {
                return outerTraces;
            }
            CausalityTrace[] result = null;
            int count = 0;
            for (int i = 0; i < outerTraces.Length; i++)
            {
                var trace = outerTraces[i];
                bool isPrinted = trace.IsRepeatOf(previous) || IsInChain(trace, previous);
                if (isPrinted && result == null)
                {
                    result = new CausalityTrace[outerTraces.Length - 1];
                    Array.Copy(outerTraces, result, i);
                    count = i;
                }
                else if (!isPrinted && result != null)
                {
                    result[count++] = trace;
                }
            }
            if (result == null)
            {
                return outerTraces;
            }
            if (count == 0)
            {
                return Array.Empty<CausalityTrace>();
            }
            if (count < result.Length)
            {
                Array.Resize(ref result, count);
            }
            return result;
        }

        private static bool IsInChain(CausalityTrace trace, CausalityTrace chain)
        {
            for (var current = chain; current != null; current = current._previous)
            {
                if (trace.IsPrintedBy(current))
                {
                    return true;
                }
                foreach (var outerTrace in current._outerTraces)
                {
                    if (trace.IsPrintedBy(outerTrace))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // A copy of a trace with another previous trace, which shares its stack trace, where it was captured, its signature (if it was already created),
        // its outer traces, and its repeats. Internal so that tests can build traces with the same stack trace.
        internal CausalityTrace(CausalityTrace trace, CausalityTrace previous, bool isTruncated)
        {
            _stackTrace = trace._stackTrace;
            _firstFrame = trace._firstFrame;
            _threadId = trace._threadId;
            _threadName = trace._threadName;
            _isThreadPoolThread = trace._isThreadPoolThread;
            _isBackgroundThread = trace._isBackgroundThread;
            _timestamp = trace._timestamp;
            _signature = Volatile.Read(ref trace._signature);
            _outerTraces = trace._outerTraces;
            _areOuterTracesTruncated = trace._areOuterTracesTruncated;
            _repeats = trace._repeats;
            _previous = previous;
            _depth = previous == null ? 1 : previous._depth + 1;
            _isTruncated = isTruncated;
        }

        // A copy without its previous traces or its repeats.
        private CausalityTrace(CausalityTrace trace)
        {
            _stackTrace = trace._stackTrace;
            _firstFrame = trace._firstFrame;
            _threadId = trace._threadId;
            _threadName = trace._threadName;
            _isThreadPoolThread = trace._isThreadPoolThread;
            _isBackgroundThread = trace._isBackgroundThread;
            _timestamp = trace._timestamp;
            _signature = Volatile.Read(ref trace._signature);
            _outerTraces = Array.Empty<CausalityTrace>();
            _repeats = Array.Empty<RepeatGroup>();
            _depth = 1;
        }

        // Whether the other trace prints all of this trace's frames, because they're copies of the same capture, and it doesn't drop more of its frames
        // (see GetWhereAsyncFunctionFirstAwaited).
        private bool IsPrintedBy(CausalityTrace other)
            => _stackTrace == other._stackTrace && other._firstFrame <= _firstFrame;

        // For where an async function first awaited in the optimized async mode, which is where its trace was captured without the async function's frame
        // (see CaptureWhereAsyncFunctionWasCalled), so it shares that capture, starting at the async function's frame (see CaptureWhereAsyncFunctionAwaited),
        // instead of capturing the same stack again.
        internal CausalityTrace GetWhereAsyncFunctionFirstAwaited(Type asyncStateMachineType)
        {
#if PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't drop frames (see the constructor).
            return this;
#else
            int firstFrame = GetAsyncFunctionFrame(_stackTrace, asyncStateMachineType);
            return firstFrame == _firstFrame ? this : new CausalityTrace(this, firstFrame);
#endif
        }

        // A copy with another first frame, which isn't printed the same, so its signature isn't shared.
        private CausalityTrace(CausalityTrace trace, int firstFrame)
        {
            _stackTrace = trace._stackTrace;
            _firstFrame = firstFrame;
            _threadId = trace._threadId;
            _threadName = trace._threadName;
            _isThreadPoolThread = trace._isThreadPoolThread;
            _isBackgroundThread = trace._isBackgroundThread;
            _timestamp = trace._timestamp;
            _outerTraces = trace._outerTraces;
            _areOuterTracesTruncated = trace._areOuterTracesTruncated;
            _repeats = trace._repeats;
            _previous = trace._previous;
            _depth = trace._depth;
            _isTruncated = trace._isTruncated;
        }

        private CausalityTrace GetWithoutHistory()
        {
            if (_previous == null && _outerTraces.Length == 0 && _repeats.Length == 0 && !_isTruncated && !_areOuterTracesTruncated)
            {
                return this;
            }
            // This may be set from multiple threads, but every copy is equivalent.
            var copy = Volatile.Read(ref _withoutHistory);
            if (copy == null)
            {
                copy = new CausalityTrace(this);
                Volatile.Write(ref _withoutHistory, copy);
            }
            return copy;
        }

        // Whether this trace was captured at the same location as the other trace: with the same visible frames,
        // on the same thread, on any thread pool thread, or on another thread with the same name (a loop that awaits on the thread pool,
        // or a custom thread pool whose threads share a name, may continue on a different thread each time).
        private bool IsRepeatOf(CausalityTrace other)
            => CanBeRepeatOf(other) && HasSameVisibleFrames(this, other);

        // The same as IsRepeatOf, but compares the traces' signatures, which are cached, for traces that are compared many times (see GetOuterTraceIndex).
        private bool IsRepeatOfBySignature(CausalityTrace other)
            => CanBeRepeatOf(other) && GetSignature().Equals(other.GetSignature());

        private bool CanBeRepeatOf(CausalityTrace other)
            // Unlike the repeat groups (see IsSameThreadGroup), this compares a pair of traces, so it also matches the same thread after its name changed.
            => _isThreadPoolThread ? other._isThreadPoolThread
                : !other._isThreadPoolThread && (_threadId == other._threadId || (_threadName != null && _threadName == other._threadName));

        // Named threads are grouped by name (e.g. the threads of a custom thread pool), and unnamed threads by id,
        // so the threads of a group always share a name, or it's one unnamed thread.
        private static bool IsSameThreadGroup(int threadId, string threadName, int otherThreadId, string otherThreadName)
            => threadName == null ? otherThreadName == null && threadId == otherThreadId : threadName == otherThreadName;

        // Whether this trace's frames are already in the next trace that is captured on the same thread, so it adds nothing to it: its visible frames are
        // the outermost visible frames of that trace, or of one of its outer traces (e.g. the trace of Promise.New, which invokes its callback,
        // and the trace of an operation started in the callback, or an async function's trace, and where it scheduled its continuation before it returned).
        // It's kept if it has repeats, which would be lost. Its outer traces are added to the next trace's (see AddOuterTraces).
        private bool IsContainedBy(CausalityTrace next, CausalityTrace[] nextOuterTraces)
        {
            if (_repeats.Length != 0)
            {
                return false;
            }
            if (IsOuterPartOf(next, false))
            {
                return true;
            }
            foreach (var outerTrace in nextOuterTraces)
            {
                if (IsOuterPartOf(outerTrace, false))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsOuterPartOf(CausalityTrace other, bool isSameCall)
            => GetOuterPart(this, other, isSameCall) == this;

        // Compares the frames from the innermost frame, so that traces captured at different locations usually differ at the first visible frame.
        // Traces without visible frames have nothing to compare, so they're never the same.
        private static bool HasSameVisibleFrames(CausalityTrace trace, CausalityTrace other)
        {
            int index = trace._firstFrame, otherIndex = other._firstFrame;
            bool hasFrames = false;
            while (true)
            {
                var frame = GetNextVisibleFrame(trace._stackTrace, ref index);
                var otherFrame = GetNextVisibleFrame(other._stackTrace, ref otherIndex);
                if (frame == null || otherFrame == null)
                {
                    return hasFrames && frame == otherFrame;
                }
                if (!IsSameFrame(frame, otherFrame))
                {
                    return false;
                }
                hasFrames = true;
            }
        }

        // Gets the trace whose visible frames are the outermost visible frames of the other trace, which has more visible frames, or null if neither of them is.
        // Both are compared in one pass, from the outermost frame, where traces captured on the same thread share the frames of the calls that are still running.
        // A call that is still running is only on the stack of the thread that is running it, so the thread must be the same.
        // Traces without visible frames have nothing to compare, so they're never contained.
        // isSameCall is whether the innermost frame of the outer part is known to still be in the same call.
        private static CausalityTrace GetOuterPart(CausalityTrace trace, CausalityTrace other, bool isSameCall)
        {
            if (trace._threadId != other._threadId)
            {
                return null;
            }
            var stackTrace = trace._stackTrace;
            var otherStackTrace = other._stackTrace;
            int index = stackTrace.FrameCount, otherIndex = otherStackTrace.FrameCount;
            var frame = GetPreviousVisibleFrame(stackTrace, ref index, trace._firstFrame);
            var otherFrame = GetPreviousVisibleFrame(otherStackTrace, ref otherIndex, other._firstFrame);
            if (frame == null || otherFrame == null)
            {
                return null;
            }
            while (true)
            {
                var nextFrame = GetPreviousVisibleFrame(stackTrace, ref index, trace._firstFrame);
                var nextOtherFrame = GetPreviousVisibleFrame(otherStackTrace, ref otherIndex, other._firstFrame);
                if (nextFrame == null || nextOtherFrame == null)
                {
                    // If they have the same number of visible frames, neither of them has more.
                    if (nextFrame == nextOtherFrame)
                    {
                        return null;
                    }
                    // The innermost frame may have moved on to another call since the outer part was captured (e.g. an async method that called
                    // its builder's Create and then its Start), so its IL offset is not compared, only what is printed: its method and its line.
                    if (!(isSameCall ? IsSameFrame(frame, otherFrame) : IsSameMethodAndLine(frame, otherFrame)))
                    {
                        return null;
                    }
                    return nextFrame == null ? trace : other;
                }
                if (!IsSameFrame(frame, otherFrame))
                {
                    return null;
                }
                frame = nextFrame;
                otherFrame = nextOtherFrame;
            }
        }

        // The same as GetOuterPart (when the innermost frame of the outer part may not be in the same call), but compares the traces' signatures, which are cached,
        // for traces that are compared many times (see GetOuterTraceIndex).
        private static CausalityTrace GetOuterPartBySignature(CausalityTrace trace, CausalityTrace other)
        {
            if (trace._threadId != other._threadId)
            {
                return null;
            }
            var signature = trace.GetSignature();
            var outerPart = Signature.GetOuterPart(signature, other.GetSignature());
            if (outerPart == null)
            {
                return null;
            }
            return outerPart == signature ? trace : other;
        }

        // Some runtimes (like IL2CPP) don't provide accurate IL offsets, so we also compare the line numbers.
        private static bool IsSameFrame(StackFrame frame, StackFrame otherFrame)
            => frame.GetILOffset() == otherFrame.GetILOffset() && IsSameMethodAndLine(frame, otherFrame);

#if NET6_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "The frames are visible frames, which excludes frames whose method metadata was trimmed.")]
#endif
        private static bool IsSameMethodAndLine(StackFrame frame, StackFrame otherFrame)
            => frame.GetFileLineNumber() == otherFrame.GetFileLineNumber() && frame.GetMethod().Equals(otherFrame.GetMethod());

        private static StackFrame GetNextVisibleFrame(StackTrace stackTrace, ref int index)
        {
            while (index < stackTrace.FrameCount)
            {
                var frame = stackTrace.GetFrame(index++);
                if (IsVisible(frame))
                {
                    return frame;
                }
            }
            return null;
        }

        // Gets the index of the async function's frame, which is its state machine's MoveNext, where it awaited (see CaptureWhereAsyncFunctionAwaited).
        // The frames before it are dropped: the code that captured where it awaited, which is hidden, even if it's printed with another method's name
        // (e.g. in IL2CPP release builds). Its state machine type is compared by its generic type definition,
        // because the frame's method may not be declared by the same instantiation (e.g. if its code is shared with other instantiations).
        // It's the innermost frame of that type, which is where it awaited, even if the async function called itself.
        // Nothing is dropped if it's not found.
#if NET6_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "If the method metadata was trimmed, GetMethod() returns null and the frame is not the async function's frame.")]
#endif
        private static int GetAsyncFunctionFrame(StackTrace stackTrace, Type asyncStateMachineType)
        {
            var stateMachineType = GetTypeDefinition(asyncStateMachineType);
            for (int i = 0, frameCount = stackTrace.FrameCount; i < frameCount; i++)
            {
                if (GetTypeDefinition(stackTrace.GetFrame(i)?.GetMethod()?.DeclaringType) == stateMachineType)
                {
                    return i;
                }
            }
            return 0;
        }

        // Gets the index of the frame of the method that the compiler generated for the async function, where it was called (see CaptureWhereAsyncFunctionWasCalled).
        // The frames before it are dropped: the async function's frame where it first awaits, the runtime code that started it
        // (e.g. AsyncTaskMethodBuilder.Start, which runs it on its ExecutionContext on runtimes older than .Net 6, see Internal.StartAndRestoreExecutionContext),
        // and hidden frames, even if they're printed with another method's name (e.g. in IL2CPP release builds).
        // The method is identified by its AsyncStateMachineAttribute, whose state machine type is compared by its generic type definition,
        // because the attribute has the generic type definition, so a call of the same async function (e.g. a recursive call) can't be told apart this way.
        // If asyncStateMachineType is null, it's the innermost method with an AsyncStateMachineAttribute, which is the async function's
        // when it's captured before the async function's body runs, because only hidden frames were called from it.
        // Nothing is dropped if it's not found (e.g. if it was inlined into the method that called it).
        private static int GetFrameAfterAsyncFunction(StackTrace stackTrace, Type asyncStateMachineType)
        {
            var stateMachineType = GetTypeDefinition(asyncStateMachineType);
            for (int i = 0, frameCount = stackTrace.FrameCount; i < frameCount; i++)
            {
                var frameStateMachineType = GetAsyncStateMachineType(stackTrace.GetFrame(i));
                if (frameStateMachineType != null && (stateMachineType == null || frameStateMachineType == stateMachineType))
                {
                    return i;
                }
            }
            return 0;
        }

        // Gets the generic type definition of the state machine type of the frame's method's AsyncStateMachineAttribute, or null if it has none.
#if NET6_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "If the method metadata was trimmed, GetMethod() returns null and the frame has no state machine type.")]
#endif
        private static Type GetAsyncStateMachineType(StackFrame frame)
        {
            var method = frame?.GetMethod();
            if (method == null)
            {
                return null;
            }
            // Reading the attributes is expensive, and every async function's trace reads them, so the results are cached.
            var cache = MethodCache.s_asyncStateMachineTypes;
            if (!cache.TryGetValue(method, out var stateMachineType))
            {
                stateMachineType = GetAsyncStateMachineType(method);
                cache.TryAdd(method, stateMachineType);
            }
            return stateMachineType;
        }

        // Like IsStackTraceHidden, if the attributes can't be read, we behave as if it's not present.
        private static Type GetAsyncStateMachineType(System.Reflection.MethodBase method)
        {
            try
            {
                var attribute = (AsyncStateMachineAttribute) Attribute.GetCustomAttribute(method, typeof(AsyncStateMachineAttribute), false);
                return GetTypeDefinition(attribute?.StateMachineType);
            }
            catch
            {
                return null;
            }
        }

        private static Type GetTypeDefinition(Type type)
            => type != null && type.IsGenericType ? type.GetGenericTypeDefinition() : type;

        private static StackFrame GetPreviousVisibleFrame(StackTrace stackTrace, ref int index, int firstFrame)
        {
            while (index > firstFrame)
            {
                var frame = stackTrace.GetFrame(--index);
                if (IsVisible(frame))
                {
                    return frame;
                }
            }
            return null;
        }

        // Copies the most recent maxDepth traces of the chain, so that the oldest copy has no previous trace.
        private static CausalityTrace Truncate(CausalityTrace trace, int maxDepth)
        {
            // Every capture in a scope whose trace is too deep truncates the same traces, so the copy is cached on the trace.
            var truncatedCopy = Volatile.Read(ref trace._truncatedCopy);
            if (truncatedCopy != null && truncatedCopy._maxDepth == maxDepth)
            {
                return truncatedCopy._copy;
            }

            var chain = new CausalityTrace[maxDepth];
            var current = trace;
            for (int i = 0; i < maxDepth; i++)
            {
                chain[i] = current;
                current = current._previous;
            }
            CausalityTrace copy = null;
            for (int i = maxDepth - 1; i >= 0; i--)
            {
                copy = new CausalityTrace(chain[i], copy, copy == null);
            }
            // This may be set from multiple threads, but every copy for the same depth is equivalent.
            // Published with a release fence. CoreCLR already publishes new objects that way, but Mono and IL2CPP don't guarantee it on weakly ordered CPUs (e.g. ARM),
            // where other threads could otherwise see the TruncatedCopy before its constructor's writes.
            Volatile.Write(ref trace._truncatedCopy, new TruncatedCopy(copy, maxDepth));
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
        /// A group of earlier traces that were folded into a <see cref="CausalityTrace"/> (see <see cref="Repeats"/>), which were captured on the same thread,
        /// on any thread pool thread, or on threads with the same name.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly struct RepeatGroup
        {
            /// <summary>
            /// The managed thread id (see <see cref="Thread.ManagedThreadId"/>) of the thread that captured the most recent of these traces.
            /// The others may have been captured on other thread pool threads, or other threads with the same name (see <see cref="ThreadName"/>).
            /// </summary>
            public int ThreadId { get; }

            /// <summary>
            /// The name of the thread that captured the most recent of these traces when it was captured, or <see langword="null"/> if the thread had no name.
            /// </summary>
            public string ThreadName { get; }

            /// <summary>
            /// Whether these traces were captured on thread pool threads (see <see cref="Thread.IsThreadPoolThread"/>).
            /// </summary>
            public bool IsThreadPoolThread { get; }

            /// <summary>
            /// Whether the thread that captured the most recent of these traces was a background thread when it was captured (see <see cref="Thread.IsBackground"/>).
            /// </summary>
            public bool IsBackgroundThread { get; }

            /// <summary>
            /// The number of traces.
            /// </summary>
            public int Count { get; }

            /// <summary>
            /// When the earliest of these traces was captured, as a timestamp of <see cref="TimeProvider.System"/> (see <see cref="CausalityTrace.Timestamp"/>).
            /// </summary>
            public long FirstTimestamp { get; }

            /// <summary>
            /// When the most recent of these traces was captured, as a timestamp of <see cref="TimeProvider.System"/> (see <see cref="CausalityTrace.Timestamp"/>).
            /// </summary>
            public long LastTimestamp { get; }

            // Whether these traces were captured on more than one thread, so the printer describes them by the threads' name instead of the most recent thread.
            internal bool HasMultipleThreads { get; }

            private RepeatGroup(CausalityTrace trace, int count, long firstTimestamp)
            {
                ThreadId = trace._threadId;
                ThreadName = trace._threadName;
                IsThreadPoolThread = trace._isThreadPoolThread;
                IsBackgroundThread = trace._isBackgroundThread;
                Count = count;
                FirstTimestamp = firstTimestamp;
                LastTimestamp = trace._timestamp;
                HasMultipleThreads = false;
            }

            // Combines repeats on the same thread or threads with the same name, where the thread is described by the most recent one.
            private RepeatGroup(RepeatGroup repeat, RepeatGroup other)
            {
                var latest = repeat.LastTimestamp >= other.LastTimestamp ? repeat : other;
                ThreadId = latest.ThreadId;
                ThreadName = latest.ThreadName;
                IsThreadPoolThread = latest.IsThreadPoolThread;
                IsBackgroundThread = latest.IsBackgroundThread;
                Count = repeat.Count + other.Count;
                FirstTimestamp = Math.Min(repeat.FirstTimestamp, other.FirstTimestamp);
                LastTimestamp = latest.LastTimestamp;
                HasMultipleThreads = repeat.HasMultipleThreads | other.HasMultipleThreads | repeat.ThreadId != other.ThreadId;
            }

            private bool IsSameThread(RepeatGroup other)
                => IsThreadPoolThread ? other.IsThreadPoolThread : !other.IsThreadPoolThread && IsSameThreadGroup(ThreadId, ThreadName, other.ThreadId, other.ThreadName);

            // Returns new repeats with the trace added.
            internal static RepeatGroup[] Add(RepeatGroup[] repeats, CausalityTrace trace)
                => Add(repeats, new RepeatGroup(trace, 1, trace._timestamp));

            // Returns new repeats with the repeat combined with the repeats on the same thread, ordered from the most recent.
            internal static RepeatGroup[] Add(RepeatGroup[] repeats, RepeatGroup repeat)
            {
                var result = new List<RepeatGroup>(repeats.Length + 1);
                foreach (var other in repeats)
                {
                    if (repeat.IsSameThread(other))
                    {
                        repeat = new RepeatGroup(repeat, other);
                    }
                    else
                    {
                        result.Add(other);
                    }
                }
                int index = 0;
                while (index < result.Count && result[index].LastTimestamp >= repeat.LastTimestamp)
                {
                    ++index;
                }
                result.Insert(index, repeat);
                return result.ToArray();
            }
        }

        /// <summary>
        /// Returns a readable representation of the <see cref="CausalityTrace"/>.
        /// </summary>
        /// <remarks>
        /// The stack trace and its chain of causes (each one's <see cref="Previous"/> trace) are printed in order, like an async stack trace.
        /// Each stack trace is preceded by the thread that captured it, and for the earlier traces, how long before this trace they were captured
        /// (e.g. <c>--- Thread 5 (thread pool), 12.345 ms earlier ---</c>). Threads without a name are printed as <c>(thread pool)</c>, <c>(background)</c> or <c>(foreground)</c>.
        /// A stack trace whose frames are all hidden from stack traces is printed as <c>--- All frames are hidden ---</c>.
        /// <para/>
        /// A trace's <see cref="Repeats"/> are printed after its stack trace with their threads and times, and its <see cref="OuterTraces"/> are printed after that,
        /// each indented one more level than the one before it (followed by <c>--- Outer traces truncated, increase CausalityTrace.MaxOuterTraces to see more ---</c>
        /// if some were dropped to stay within <see cref="MaxOuterTraces"/>).
        /// Parts of the chain that repeat (e.g. a callback that registers itself again from another thread, or callbacks that register each other) are only printed once:
        /// a single repeated trace is printed with the others as its repeats, and a longer repeated part is printed between <c>--- #n Begin ---</c> and <c>--- #n End ---</c> markers,
        /// followed by <c>--- #n repeated N times: ... ---</c> with the threads and times of the repeats.
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

        // Debuggers show this instead of ToString(), which prints the whole chain. E.g. Thread 1 "Main Thread" at Program.Main
        // Internal so that tests can check it.
        internal string DebuggerDisplay
        {
#if NET6_0_OR_GREATER
            [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026",
                Justification = "The frames are visible frames, whose methods were already read (see IsVisible).")]
#endif
            get
            {
                var sb = new StringBuilder();
                Printer.AppendThreadDescription(sb, _threadId, _threadName, _isThreadPoolThread, _isBackgroundThread);
                var frames = GetFrames();
                if (frames.Length > 0)
                {
                    // Visible frames always have a method (see IsVisible).
                    var method = frames[0].GetMethod();
                    sb.Append(" at ");
                    if (method.DeclaringType != null)
                    {
                        sb.Append(method.DeclaringType.FullName).Append('.');
                    }
                    sb.Append(method.Name);
                }
                return sb.ToString();
            }
        }

        // Formats the stack trace without the frames that are hidden from stack traces, such as ProtoPromise's internal frames.
        // We filter the frames even on .Net 6+, which has built-in [StackTraceHidden], because its StackTrace.ToString() never hides the outermost frame,
        // even if it's annotated (e.g. when the engine calls into ProtoPromise directly), and so that the frames are the same as GetFrames() on all runtimes.
        internal static string FormatStackTrace(StackTrace stackTrace)
        {
            var sb = new StringBuilder();
            foreach (var frame in GetVisibleFrames(stackTrace))
            {
                AppendFrame(sb, frame);
            }
            return sb.ToString();
        }

        private static void AppendFrame(StringBuilder sb, StackFrame frame)
            // Create a new StackTrace to get proper formatting.
            // Some runtimes include a trailing new line and some don't, so we normalize it to match the .Net 6 format (one line per frame, with a trailing new line).
            => sb.Append(new StackTrace(frame).ToString().TrimEnd('\r', '\n')).AppendLine();

        /// <summary>
        /// Enters a scope in which <paramref name="trace"/> is the current causality trace, so that it's the <see cref="Previous"/> trace of traces captured within the scope,
        /// or a copy of it without its history is one of their <see cref="OuterTraces"/> if a nested scope is entered (unless they skip it or fold it, see <see cref="Previous"/> and <see cref="OuterTraces"/>).
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
        /// Disposing a <see cref="Scope"/> more than once, or after its outer scope, is detected on a best-effort basis, and throws an <see cref="InvalidOperationException"/>
        /// (if <paramref name="trace"/> is <see langword="null"/>, only in DEBUG mode while causality traces are enabled, and in RELEASE mode, not if the same trace was entered again at the same depth).
        /// Disposing a <see cref="Scope"/> before its nested scopes (e.g. a callback entered a scope without disposing it) is reported to <see cref="Promises.Promise.Config.UncaughtRejectionHandler"/>,
        /// and the nested scopes are also exited, so that the current causality trace is restored (if <paramref name="trace"/> is <see langword="null"/>, only in DEBUG mode while causality traces are enabled).
        /// </remarks>
        // In RELEASE mode, a null trace is only a null check. In DEBUG mode, every scope is checked for being disposed correctly,
        // and while causality traces are enabled, remembers where it was entered to report it if it's never disposed (see DisposedChecker).
        // While they're disabled, a null trace is only a null check in DEBUG mode too, because the checker allocates, and a scope may be entered for every callback.
        [MethodImpl(Promises.Internal.InlineOption)]
        public static Scope EnterScope(CausalityTrace trace)
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            => trace != null || s_isEnabled ? new Scope(trace, true) : default;
#else
            => trace != null ? new Scope(trace, true) : default;
#endif

        // For the library's own scopes, which are always disposed in finally blocks.
        // The common case (not traced) is only a null check, so that types that store their own trace can call this on their fast paths.
        [MethodImpl(Promises.Internal.InlineOption)]
        internal static Scope EnterScopeInternal(CausalityTrace trace)
            => trace != null ? new Scope(trace, false) : default;

        // Whether trace is the scope that is entered on this thread.
        internal static bool IsCurrentScope(CausalityTrace trace)
        {
            var traces = ts_traces;
            return traces != null && traces.Count > 0 && traces.Peek() == trace;
        }

        // A scope that was exited without disposing its Scope, until it's restored by disposing this (see ExitScopeIfCurrent).
        // Disposing a default value does nothing.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly ref struct ExitedScope
        {
            internal readonly CausalityTrace _trace;
            // The scope that was exited with it, which was entered just outside of it (see PromiseRefBase.EnteredAwaitTrace), or null.
            private readonly CausalityTrace _outerTrace;
            // The number of traces on the stack after it was exited, to find the scopes that were entered since then and not disposed (see Dispose).
            internal readonly int _depth;

            internal ExitedScope(CausalityTrace trace, CausalityTrace outerTrace, int depth)
            {
                _trace = trace;
                _outerTrace = outerTrace;
                _depth = depth;
            }

            internal void Dispose()
            {
                if (_trace == null)
                {
                    // Nothing was exited (see ExitScopeIfCurrent).
                    return;
                }
                var traces = ts_traces;
                if (traces.Count == _depth)
                {
                    Restore(traces);
                    return;
                }
                if (traces.Count < _depth)
                {
                    // An outer scope was disposed first, which also exited this scope, so it's not restored (its Scope throws when it's disposed, the same as if it wasn't exited).
                    // This only happens when scopes are misused (e.g. a callback disposed a scope that was entered outside of it), so it's not tested.
                    return;
                }
                // Nested scopes were entered since it was exited and not disposed (e.g. by a continuation that doesn't enter a scope of its own),
                // which is not its Scope's fault. We exit them, the same as when a Scope is disposed before its nested scopes (see Scope.Exit),
                // so that its Scope is still disposed correctly, and traces that are captured later on this thread are not corrupted.
                while (traces.Count > _depth)
                {
                    traces.Pop();
                }
                Restore(traces);
#if !(PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE)
                // In DEBUG mode, its Scope reports it when it's disposed (see Scope.Dispose).
                Scope.ReportNestedScopesNotDisposed(_trace);
#endif
            }

            private void Restore(Stack<CausalityTrace> traces)
            {
                if (_outerTrace != null)
                {
                    traces.Push(_outerTrace);
                }
                traces.Push(_trace);
            }
        }

        // Exits the scope of the promise's trace if it's the current scope, until the result is disposed, without disposing its Scope
        // (see PromiseRefBase.ScopeExitingWaiter). The result's trace is null if it's not the current scope.
        // The scope of where an async promise last awaited is exited with it, if it's entered just outside of it (see PromiseRefBase.EnteredAwaitTrace).
        internal static ExitedScope ExitScopeIfCurrent(Promises.Internal.PromiseRefBase promise)
        {
            var traces = ts_traces;
            // The trace is looked up after the stack is checked, so that it's not looked up when no scope is current (e.g. the promise was completed from outside any scope).
            if (traces == null || traces.Count == 0)
            {
                return default;
            }
            var trace = promise.Trace;
            if (trace == null || traces.Peek() != trace)
            {
                return default;
            }
            traces.Pop();
            var awaitTrace = promise.EnteredAwaitTrace;
            if (awaitTrace != null && traces.Count > 0 && traces.Peek() == awaitTrace)
            {
                traces.Pop();
            }
            else
            {
                awaitTrace = null;
            }
            return new ExitedScope(trace, awaitTrace, traces.Count);
        }

        /// <summary>
        /// A scope in which a <see cref="CausalityTrace"/> is current, used while running an async callback or continuation. Created by <see cref="EnterScope(CausalityTrace)"/>.
        /// </summary>
        /// <remarks>
        /// Dispose it with a <see langword="using"/> statement (C# 8 or later) or a <see langword="try"/>/<see langword="finally"/> block.
        /// Disposing a default <see cref="Scope"/> does nothing.
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

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            private Scope(Stack<CausalityTrace> traces, CausalityTrace trace, int depth, DisposedChecker disposedChecker)
            {
                _disposedChecker = disposedChecker;
#else
            private Scope(Stack<CausalityTrace> traces, CausalityTrace trace, int depth)
            {
#endif
                _traces = traces;
                _trace = trace;
                _depth = depth;
            }

            // A scope that is kept in an object, for the library's callback invokes. A Scope local (unlike a reference local) makes their untraced path slower,
            // even when it's only the default value (see PromiseRefBase.EnterTracedScopeAndClearSentinel).
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed class Box
            {
                private readonly Stack<CausalityTrace> _traces;
                private readonly CausalityTrace _trace;
                private readonly int _depth;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                private readonly DisposedChecker _disposedChecker;
#endif

                internal Box(in Scope scope)
                {
                    _traces = scope._traces;
                    _trace = scope._trace;
                    _depth = scope._depth;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposedChecker = scope._disposedChecker;
#endif
                }

                internal void Dispose()
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    => new Scope(_traces, _trace, _depth, _disposedChecker).Dispose();
#else
                    => new Scope(_traces, _trace, _depth).Dispose();
#endif
            }

            /// <summary>
            /// Disposes the scope, restoring the previous <see cref="CausalityTrace"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException">The scope was already disposed, or its outer scope was disposed first (detected on a best-effort basis, see <see cref="EnterScope(CausalityTrace)"/>).</exception>
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
            internal static void ReportNestedScopesNotDisposed(CausalityTrace trace)
                => Promises.Internal.ReportUnhandledValue(new Promises.InvalidOperationException(
                    $"A {nameof(CausalityTrace)}.{nameof(Scope)} was disposed before its nested {nameof(Scope)}s. Nested {nameof(Scope)}s must be disposed before outer {nameof(Scope)}s.",
                    FormatStackTrace(new StackTrace(true))), IsEnabled ? trace : null);
        } // ref struct Scope
    } // class CausalityTrace
} // namespace Proto.Diagnostics
