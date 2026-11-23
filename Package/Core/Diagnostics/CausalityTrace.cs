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
using System.Threading;

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
    public sealed partial class CausalityTrace
    {
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
                    VisibilityCache.s_isVisible.Clear();
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
        /// because its frames are already in <see cref="StackTrace"/>, unless it has <see cref="Repeats"/>.
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
        /// An outer trace whose outer frames are the frames of the <see cref="Previous"/> trace, captured on the same thread (e.g. where an async function first awaited,
        /// whose outer frames are where it was called), is kept, but <see cref="ToString"/> only prints its other frames with it, because the <see cref="Previous"/> trace prints them.
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

        /// <summary>
        /// A group of earlier traces that were folded into a <see cref="CausalityTrace"/> (see <see cref="Repeats"/>), which were captured on the same thread,
        /// on any thread pool thread, or on threads with the same name.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly partial struct RepeatGroup
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
            // In this part with the other properties, because a partial struct's fields have no defined order if they're in more than one part (CS0282).
            internal bool HasMultipleThreads { get; }
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
        /// each indented one more level than the one before it, and labeled with its level, because some consoles strip the indentation
        /// (e.g. <c>--- Outer trace 1: Thread 5 (thread pool), 12.345 ms earlier ---</c>), followed by <c>--- Outer traces truncated, increase CausalityTrace.MaxOuterTraces to see more ---</c>
        /// if some were dropped to stay within <see cref="MaxOuterTraces"/>. Only the frames of an outer trace that it doesn't share with the <see cref="Previous"/> trace are printed with it.
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
        public readonly ref partial struct Scope
#if NET9_0_OR_GREATER
            : IDisposable
#endif
        {
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
                var nestedScopes = _disposedChecker.Dispose();
                Exit();
                if (nestedScopes != null)
                {
                    // Reported after the nested scopes were exited (see Exit), instead of when they're collected (see DisposedChecker.Dispose).
                    // Where each nested scope that was not disposed was entered is more useful than where this scope was entered,
                    // which is more useful than the trace that was passed (see ReportNestedScopesNotDisposed).
                    DisposedChecker.ReportNotDisposed(nestedScopes, (_disposedChecker._isPublic ? Promises.Internal.GetTrace(_disposedChecker) : null) ?? _trace);
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
        } // ref struct Scope
    } // class CausalityTrace
} // namespace Proto.Diagnostics
