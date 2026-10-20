#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

#pragma warning disable IDE0028 // Simplify collection initialization
#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0300 // Simplify collection initialization

namespace Proto.Diagnostics
{
    // Formats causality traces for ToString. Causality traces form a graph where previous traces are shared,
    // so the printer makes sure that each trace is printed at most once (and again in its own section if it's also an outer scope),
    // and folds repeated traces (e.g. from loops or recursion).
    partial class CausalityTrace
    {
        // Lazily created when the trace is printed.
        private Signature _signature;

        // Whether this trace has the same stack trace as the other trace, and the same number of previous traces.
        // The printer also requires that their previous traces are printed the same way, and that folding them doesn't hide any stack trace.
        private bool HasSameStackTrace(CausalityTrace other)
            => GetSignature().Equals(other.GetSignature()) && _previousTraces.Length == other._previousTraces.Length;

        private Signature GetSignature()
        {
            // This may be set from multiple threads, but every signature is equivalent.
            var signature = Volatile.Read(ref _signature);
            if (signature == null)
            {
                signature = new Signature(_stackTrace);
                Volatile.Write(ref _signature, signature);
            }
            return signature;
        }

        // Identifies traces that were captured from the same code path, by their visible frames' methods, offsets and line numbers.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class Signature : IEquatable<Signature>
        {
            private readonly System.Reflection.MethodBase[] _methods;
            private readonly int[] _offsets;
            private readonly int[] _lineNumbers;
            private readonly int _hashCode;

#if NET6_0_OR_GREATER
            [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026",
                Justification = "GetVisibleFrames excludes frames whose method metadata was trimmed.")]
#endif
            internal Signature(StackTrace stackTrace)
            {
                var frames = GetVisibleFrames(stackTrace);
                _methods = new System.Reflection.MethodBase[frames.Length];
                _offsets = new int[frames.Length];
                _lineNumbers = new int[frames.Length];
                int hashCode = frames.Length;
                for (int i = 0; i < frames.Length; i++)
                {
                    var frame = frames[i];
                    _methods[i] = frame.GetMethod();
                    // Some runtimes (like IL2CPP) don't provide accurate IL offsets, so we also compare the line numbers.
                    _offsets[i] = frame.GetILOffset();
                    _lineNumbers[i] = frame.GetFileLineNumber();
                    hashCode = unchecked(((hashCode * 31 + _methods[i].GetHashCode()) * 31 + _offsets[i]) * 31 + _lineNumbers[i]);
                }
                _hashCode = hashCode;
            }

            internal bool IsEmpty => _methods.Length == 0;

            public bool Equals(Signature other)
            {
                // Traces without visible frames have nothing to compare, so we don't treat them as repeats.
                // That includes comparing an empty signature to itself, so empty signatures are never used as keys (see IndexSignatures).
                if (other == null || _methods.Length == 0 || _hashCode != other._hashCode || _methods.Length != other._methods.Length)
                {
                    return false;
                }
                for (int i = 0; i < _methods.Length; i++)
                {
                    if (_offsets[i] != other._offsets[i] || _lineNumbers[i] != other._lineNumbers[i] || !_methods[i].Equals(other._methods[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public override bool Equals(object obj)
                => Equals(obj as Signature);

            public override int GetHashCode()
                => _hashCode;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class Printer
        {
            private const string PreviousLocationHeader = "--- End of stack trace from the previous location ---";
            // Every other message is indented like the stack frames, so that the previous location headers and outer scope headers stand out.
            private const string MessageIndent = "   ";

            // A block of the output can be the repeated stack trace of a trace, or the history of a trace (to the end of its chain).
            private enum LabelKind
            {
                Repeated,
                History
            }

            private enum InsertionKind
            {
                Begin,
                End,
                Reference
            }

            // The labels of the blocks and the references to them are inserted into the text after everything is printed,
            // because the blocks are numbered in order of appearance, and only the blocks that something refers to are labeled.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct Insertion
            {
                internal readonly int _position;
                internal readonly int _labelId;
                internal readonly InsertionKind _kind;
                // The number of times the block is repeated, for references.
                internal readonly int _count;
                // The threads and times of the repeats, for references to repeated blocks (only where the repeats are first referred to).
                internal readonly string _details;

                internal Insertion(int position, int labelId, InsertionKind kind, int count, string details)
                {
                    _position = position;
                    _labelId = labelId;
                    _kind = kind;
                    _count = count;
                    _details = details;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct FoldedRepeat
            {
                internal readonly CausalityTrace _repeated;
                internal readonly CausalityTrace _lastRepeat;
                // Including this repeat.
                internal readonly int _remainingCount;
                // The index of the previous traces that the repeats continue through (0 for the main chain, or the index of an outer scope).
                internal readonly int _index;

                internal FoldedRepeat(CausalityTrace repeated, CausalityTrace lastRepeat, int remainingCount, int index)
                {
                    _repeated = repeated;
                    _lastRepeat = lastRepeat;
                    _remainingCount = remainingCount;
                    _index = index;
                }
            }

            private readonly StringBuilder _sb = new StringBuilder();
            private readonly CausalityTrace _trace;
            // Every trace is printed at most once in a chain (and again in its own section if it's also an outer scope),
            // which guarantees that the output can't grow faster than the number of traces.
            private readonly HashSet<CausalityTrace> _printed = new HashSet<CausalityTrace>();
            // Repeats that were not printed.
            private readonly Dictionary<CausalityTrace, FoldedRepeat> _foldedRepeats = new Dictionary<CausalityTrace, FoldedRepeat>();
            private readonly List<CausalityTrace> _outerScopes = new List<CausalityTrace>();
            private readonly HashSet<CausalityTrace> _outerScopeSet = new HashSet<CausalityTrace>();
            private readonly Dictionary<CausalityTrace, int> _traceIds = new Dictionary<CausalityTrace, int>();
            private readonly List<Insertion> _insertions = new List<Insertion>();
            // Outer scopes are labeled with paths of letters, where each outer scope's outer scopes extend its path (A, A/A, A/B, B, ...).
            private readonly Dictionary<CausalityTrace, string> _outerScopePaths = new Dictionary<CausalityTrace, string>();
            // The outer scopes whose sections were printed, so that references to them can say whether they're above or below.
            private readonly HashSet<CausalityTrace> _printedOuterScopes = new HashSet<CausalityTrace>();
            private readonly Dictionary<string, int> _outerScopeChildCounts = new Dictionary<string, int>();
            private string _currentPath = string.Empty;
            // The outer scope that is being printed, and the outer scopes that contain it.
            private readonly List<CausalityTrace> _currentScopes = new List<CausalityTrace>();
            // The stack traces in each trace's history, used to make sure that folding never hides a stack trace.
            private readonly Dictionary<Signature, int> _signatureIndices = new Dictionary<Signature, int>();
            private readonly Dictionary<CausalityTrace, ulong[]> _historySignatures = new Dictionary<CausalityTrace, ulong[]>();
            private int _signatureWordCount;
            // The truncation marker and the hidden frames marker are treated like stack traces, so that folding never hides them.
            private int _truncatedIndex;
            private int _hiddenIndex;
            // The stack traces that were printed, to verify that every stack trace in the history was printed.
            private ulong[] _printedSignatures;
            // Traces whose histories contain these stack traces are not folded, because folding hid them in a previous attempt to print (see Print).
            private readonly ulong[] _unfoldableSignatures;
            // Traces with the same stack trace and the same history (compared by content, not by reference) are printed as one trace.
            private readonly Dictionary<CausalityTrace, CausalityTrace> _canonicalTraces = new Dictionary<CausalityTrace, CausalityTrace>();
            // The new outer scopes that were referenced by the chain that is being printed.
            private List<CausalityTrace> _currentOuterScopes = new List<CausalityTrace>();

            private Printer(CausalityTrace trace, ulong[] unfoldableSignatures)
            {
                _trace = trace;
                _unfoldableSignatures = unfoldableSignatures;
            }

            internal static string Print(CausalityTrace trace)
            {
                // Each check that allows a trace to be folded into another is local, so folds can depend on each other in a circle, which can hide a stack trace
                // (e.g. an outer scope that is printed as another outer scope, whose history only reaches the first outer scope's stack traces through itself).
                // So we verify that every stack trace was printed, and if any were hidden, we print it again without folding the traces whose histories contain them.
                ulong[] unfoldableSignatures = null;
                bool isLastAttempt = false;
                while (true)
                {
                    var printer = new Printer(trace, unfoldableSignatures);
                    var text = printer.Print();
                    var missingSignatures = printer.GetMissingSignatures();
                    if (missingSignatures == null || isLastAttempt)
                    {
                        return text;
                    }
                    if (unfoldableSignatures != null && IsSubset(missingSignatures, unfoldableSignatures))
                    {
                        // This should never happen, but if folding still hid a stack trace, we print it one last time without folding any trace
                        // that has a stack trace in its history, which can't hide any stack trace.
                        for (int i = 0; i < unfoldableSignatures.Length; i++)
                        {
                            unfoldableSignatures[i] = ulong.MaxValue;
                        }
                        isLastAttempt = true;
                        continue;
                    }
                    if (unfoldableSignatures != null)
                    {
                        Union(missingSignatures, unfoldableSignatures);
                    }
                    unfoldableSignatures = missingSignatures;
                }
            }

            private string Print()
            {
                IndexSignatures();
                CanonicalizeTraces();
                AppendChain(_trace);
                AppendOuterScopes(_currentOuterScopes);
                return InsertLabels();
            }

            // Returns the stack traces in the history that were not printed, or null if every stack trace was printed.
            private ulong[] GetMissingSignatures()
            {
                var historySignatures = GetHistorySignatures(_trace);
                ulong[] missingSignatures = null;
                for (int i = 0; i < historySignatures.Length; i++)
                {
                    ulong missing = historySignatures[i] & ~_printedSignatures[i];
                    if (missing != 0)
                    {
                        if (missingSignatures == null)
                        {
                            missingSignatures = new ulong[historySignatures.Length];
                        }
                        missingSignatures[i] = missing;
                    }
                }
                return missingSignatures;
            }

            // Whether the outer scope may be printed as another outer scope, which hides its entire history (see Print).
            private bool CanFold(CausalityTrace outerScope)
                => _unfoldableSignatures == null || !Intersects(GetHistorySignatures(outerScope), _unfoldableSignatures);

            // Whether the repeat may be folded into the repeated trace (see Print). Folding it only hides its stack trace, which is printed with the repeated trace,
            // and its other previous traces. The previous trace that the repeats continue through is still printed after the last repeat.
            private bool CanFoldRepeat(CausalityTrace repeat, int index)
            {
                if (_unfoldableSignatures == null)
                {
                    return true;
                }
                for (int i = 0; i < repeat._previousTraces.Length; i++)
                {
                    if (i != index && Intersects(GetHistorySignatures(Previous(repeat, i)), _unfoldableSignatures))
                    {
                        return false;
                    }
                }
                return true;
            }

            private void AppendStackTrace(CausalityTrace trace)
            {
                AppendThread(trace);
                var stackTrace = trace.FormatStackTrace();
                if (stackTrace.Length == 0)
                {
                    // Every frame is hidden (e.g. ProtoPromise completed the operation from its own code), so we show that the trace is there.
                    _sb.Append(MessageIndent).AppendLine("--- [All frames are hidden] ---");
                    SetBit(_printedSignatures, _hiddenIndex);
                }
                else
                {
                    _sb.Append(stackTrace);
                }
                _printed.Add(trace);
                if (_signatureIndices.TryGetValue(trace.GetSignature(), out int index))
                {
                    SetBit(_printedSignatures, index);
                }
            }

            // Prints the thread that captured the trace, and how long before the printed trace it was captured.
            // Traces that are folded into this trace print their threads and times where they're referred to (see DescribeRepeats and AppendOuterScopeReference).
            private void AppendThread(CausalityTrace trace)
            {
                _sb.Append(MessageIndent).Append("--- [");
                AppendThreadDescription(_sb, trace);
                if (trace != _trace)
                {
                    _sb.Append(", ");
                    AppendTime(_sb, trace);
                }
                _sb.AppendLine("] ---");
            }

            // Thread 5 "Name", or a description of an unnamed thread.
            private static void AppendThreadDescription(StringBuilder sb, CausalityTrace trace)
            {
                sb.Append("Thread ").Append(trace._threadId).Append(' ');
                if (trace._threadName != null)
                {
                    // Line breaks are replaced, so that the name can't break the line.
                    sb.Append('"').Append(trace._threadName.Replace('\r', ' ').Replace('\n', ' ')).Append('"');
                }
                else if (trace._isThreadPoolThread)
                {
                    sb.Append("(thread pool)");
                }
                else
                {
                    sb.Append(trace._isBackgroundThread ? "(background)" : "(foreground)");
                }
            }

            // How long before the printed trace the trace was captured.
            private void AppendTime(StringBuilder sb, CausalityTrace trace)
            {
                AppendMilliseconds(sb, trace);
                sb.Append(" ms earlier");
            }

            private void AppendMilliseconds(StringBuilder sb, CausalityTrace trace)
            {
                // The clock may not be monotonic on some platforms, so we don't print negative times.
                double milliseconds = Math.Max(0d, TimeProvider.System.GetElapsedTime(trace._timestamp, _trace._timestamp).TotalMilliseconds);
                sb.Append(milliseconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
            }

            private static bool IsSameThread(CausalityTrace trace, CausalityTrace other)
                => trace._threadId == other._threadId && trace._threadName == other._threadName
                && trace._isThreadPoolThread == other._isThreadPoolThread && trace._isBackgroundThread == other._isBackgroundThread;

            // Whether the traces were captured at the same time on the same thread, which means one is a copy of the other (see Truncate).
            private static bool IsSameCapture(CausalityTrace trace, CausalityTrace other)
                => trace._timestamp == other._timestamp && trace._threadId == other._threadId;

            // The threads and times of the repeats that are folded into a repeated trace, starting with the first repeat.
            // They're only printed where the repeats are first referred to (right after the repeated trace), not where the rest of them are referred to again,
            // so that the output can't grow faster than the number of traces.
            private string DescribeRepeats(CausalityTrace firstRepeat, int index, int count)
            {
                var repeats = new List<CausalityTrace>(count);
                for (var repeat = firstRepeat; ; repeat = Previous(repeat, index))
                {
                    repeats.Add(repeat);
                    if (repeats.Count == count)
                    {
                        break;
                    }
                }
                var sb = new StringBuilder(": ");
                AppendThreadsAndTimes(sb, repeats);
                return sb.ToString();
            }

            // Prints the threads and times of the traces in order, with consecutive traces on the same thread grouped
            // (e.g. "Thread 1 (foreground) at 2.110 ms earlier; Thread 5 (thread pool) at 7.004, 9.812 ms earlier").
            private void AppendThreadsAndTimes(StringBuilder sb, List<CausalityTrace> traces)
            {
                CausalityTrace groupThread = null;
                foreach (var trace in traces)
                {
                    if (groupThread != null && IsSameThread(groupThread, trace))
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        if (groupThread != null)
                        {
                            sb.Append(" ms earlier; ");
                        }
                        AppendThreadDescription(sb, trace);
                        sb.Append(" at ");
                        groupThread = trace;
                    }
                    AppendMilliseconds(sb, trace);
                }
                sb.Append(" ms earlier");
            }

            // Prints the outer scopes depth-first, so that each outer scope's outer scopes are printed right after it.
            // This is iterative, because outer scopes can be nested deeply.
            private void AppendOuterScopes(List<CausalityTrace> outerScopes)
            {
                // Each entry is a list of outer scopes, the index of the next one to print, and whether they're the outer scopes of the current outer scope.
                var stack = new Stack<(List<CausalityTrace> outerScopes, int index, bool isNested)>();
                stack.Push((outerScopes, 0, false));
                while (stack.Count > 0)
                {
                    var (scopes, index, isNested) = stack.Pop();
                    if (index == scopes.Count)
                    {
                        if (isNested)
                        {
                            // The current outer scope and its outer scopes are done, so we return to its parent.
                            _currentScopes.RemoveAt(_currentScopes.Count - 1);
                            _currentPath = _currentScopes.Count == 0 ? string.Empty : _outerScopePaths[_currentScopes[_currentScopes.Count - 1]];
                        }
                        continue;
                    }
                    stack.Push((scopes, index + 1, isNested));

                    var outerScope = scopes[index];
                    var path = _outerScopePaths[outerScope];
                    _sb.AppendLine()
                        .Append("--- [Outer causality scope ").Append(path).AppendLine("] ---");
                    _printedOuterScopes.Add(outerScope);
                    _currentPath = path;
                    _currentScopes.Add(outerScope);
                    _currentOuterScopes = new List<CausalityTrace>();
                    AppendChain(outerScope);
                    stack.Push((_currentOuterScopes, 0, true));
                }
            }

            // Prints the trace and its main chain (following the first previous trace), with references to the outer scopes.
            private void AppendChain(CausalityTrace trace)
            {
                var chain = new List<CausalityTrace>();
                while (true)
                {
                    // An outer scope's trace may have already been printed as part of another chain.
                    // Its stack trace is printed again in its own section, and its history refers to what was already printed.
                    bool isPrinted = _printed.Contains(trace);
                    int repeatIndex = -1;
                    int repeatCount = 0;
                    CausalityTrace lastRepeat = null;
                    if (!isPrinted)
                    {
                        // The history of this trace is labeled in case another chain reaches it.
                        AppendLabel(InsertionKind.Begin, trace, LabelKind.History);
                        chain.Add(trace);
                        if (_foldedRepeats.TryGetValue(trace, out var foldedRepeat))
                        {
                            // This is one of the repeats of a trace that was already printed (e.g. an outer scope that is a loop iteration).
                            // Its stack trace is printed, then the rest of its repeats refer to the repeated trace.
                            AppendFoldedRepeat(trace, foldedRepeat);
                            if (!AppendPreviousLocation(trace, out var next))
                            {
                                break;
                            }
                            trace = next;
                            continue;
                        }
                        repeatIndex = GetRepeats(trace, out repeatCount, out lastRepeat);
                    }
                    if (repeatCount > 0)
                    {
                        AppendLabel(InsertionKind.Begin, trace, LabelKind.Repeated);
                    }
                    AppendStackTrace(trace);
                    AppendOuterScopeReferences(trace, repeatIndex);

                    var previousSource = trace;
                    if (repeatCount > 0)
                    {
                        // Remember the repeats, so that a trace that refers to one of them can refer to the repeated trace.
                        int remainingCount = repeatCount;
                        for (var repeat = Previous(trace, repeatIndex); ; repeat = Previous(repeat, repeatIndex))
                        {
                            _foldedRepeats.Add(repeat, new FoldedRepeat(trace, lastRepeat, remainingCount--, repeatIndex));
                            if (repeat == lastRepeat)
                            {
                                break;
                            }
                        }
                        AppendLabel(InsertionKind.End, trace, LabelKind.Repeated);
                        AppendRepeated(trace, LabelKind.Repeated, repeatCount, DescribeRepeats(Previous(trace, repeatIndex), repeatIndex, repeatCount));
                        if (repeatIndex == 0)
                        {
                            // Repeats in the main chain (e.g. recursion). Their histories continue after the last repeat.
                            previousSource = lastRepeat;
                        }
                        else
                        {
                            // Repeats in an outer scope (e.g. each loop iteration ran inside the previous iteration's scope).
                            // The outer scopes continue with the last repeat's outer scope.
                            if (repeatIndex < lastRepeat._previousTraces.Length)
                            {
                                AppendOuterScopeReference(Previous(lastRepeat, repeatIndex));
                            }
                        }
                    }

                    if (!AppendPreviousLocation(previousSource, out var previous))
                    {
                        break;
                    }
                    trace = previous;
                }

                // Close the histories of the traces in this chain, innermost first.
                for (int i = chain.Count - 1; i >= 0; --i)
                {
                    AppendLabel(InsertionKind.End, chain[i], LabelKind.History);
                }
            }

            // Prints a repeat that was folded into a repeated trace: its stack trace and outer scopes, with the rest of the repeats as a reference to the repeated trace.
            // For repeats in a main chain, the rest of the repeats are the previous location, which AppendPreviousLocation refers to.
            private void AppendFoldedRepeat(CausalityTrace trace, FoldedRepeat foldedRepeat)
            {
                AppendStackTrace(trace);
                int index = foldedRepeat._index;
                if (index == 0)
                {
                    AppendOuterScopeReferences(trace, -1);
                    return;
                }
                AppendOuterScopeReferences(trace, index);
                int remainingCount = foldedRepeat._remainingCount - 1;
                if (remainingCount > 0)
                {
                    AppendRepeated(foldedRepeat._repeated, LabelKind.Repeated, remainingCount);
                }
                // The outer scopes continue with the last repeat's outer scope.
                var lastRepeat = foldedRepeat._lastRepeat;
                if (index < lastRepeat._previousTraces.Length)
                {
                    AppendOuterScopeReference(Previous(lastRepeat, index));
                }
            }

            // Returns true if the previous location needs to be printed in the chain.
            private bool AppendPreviousLocation(CausalityTrace trace, out CausalityTrace previous)
            {
                while (true)
                {
                    if (trace._previousTraces.Length == 0)
                    {
                        if (trace._isTruncated)
                        {
                            _sb.AppendLine(PreviousLocationHeader);
                            _sb.Append(MessageIndent).AppendLine($"--- [Older traces truncated, increase {nameof(CausalityTrace)}.{nameof(MaxDepth)} to see more] ---");
                            SetBit(_printedSignatures, _truncatedIndex);
                        }
                        previous = null;
                        return false;
                    }

                    _sb.AppendLine(PreviousLocationHeader);
                    previous = Previous(trace, 0);
                    if (_printed.Contains(previous))
                    {
                        // The rest of this trace is the same as the history of a trace that was already printed.
                        AppendRepeated(previous, LabelKind.History, 1);
                        return false;
                    }
                    if (!_foldedRepeats.TryGetValue(previous, out var foldedRepeat) || foldedRepeat._index != 0)
                    {
                        return true;
                    }
                    // The rest of this trace starts with repeats that were already printed, then continues after the last repeat.
                    AppendRepeated(foldedRepeat._repeated, LabelKind.Repeated, foldedRepeat._remainingCount);
                    trace = foldedRepeat._lastRepeat;
                }
            }

            // Returns the index of the previous traces that repeat this trace, and how many times they repeat in sequence.
            // A repeat must have the same stack trace and the same outer scopes (other than the repeats),
            // and every stack trace in its history must be printed elsewhere (see GetCoveredRepeatCount).
            private int GetRepeats(CausalityTrace trace, out int repeatCount, out CausalityTrace lastRepeat)
            {
                for (int index = 0; index < trace._previousTraces.Length; index++)
                {
                    repeatCount = 0;
                    lastRepeat = trace;
                    while (index < lastRepeat._previousTraces.Length)
                    {
                        var next = Previous(lastRepeat, index);
                        if (_printed.Contains(next) || _foldedRepeats.ContainsKey(next) || !CanFoldRepeat(next, index)
                            || !next.HasSameStackTrace(trace) || !HasSameOuterScopes(next, trace, index))
                        {
                            break;
                        }
                        ++repeatCount;
                        lastRepeat = next;
                    }
                    // The repeats are not printed, so every stack trace in their histories must be printed elsewhere, otherwise fewer repeats are folded.
                    while (repeatCount > 0)
                    {
                        int coveredCount = GetCoveredRepeatCount(trace, index, repeatCount, lastRepeat);
                        if (coveredCount == repeatCount)
                        {
                            return index;
                        }
                        repeatCount = coveredCount;
                        lastRepeat = trace;
                        for (int i = 0; i < repeatCount; ++i)
                        {
                            lastRepeat = Previous(lastRepeat, index);
                        }
                    }
                }
                repeatCount = 0;
                lastRepeat = null;
                return -1;
            }

            // Returns how many of the repeats have only stack traces in their histories that are printed with the repeated trace or after the repeats.
            private int GetCoveredRepeatCount(CausalityTrace trace, int index, int repeatCount, CausalityTrace lastRepeat)
            {
                var printedSignatures = new ulong[_signatureWordCount];
                AddSignature(printedSignatures, trace);
                for (int i = 0; i < trace._previousTraces.Length; i++)
                {
                    if (i != index)
                    {
                        AddHistorySignatures(printedSignatures, Previous(trace, i));
                    }
                }
                if (index < lastRepeat._previousTraces.Length)
                {
                    AddHistorySignatures(printedSignatures, Previous(lastRepeat, index));
                }

                var repeat = trace;
                for (int count = 0; count < repeatCount; ++count)
                {
                    repeat = Previous(repeat, index);
                    for (int i = 0; i < repeat._previousTraces.Length; i++)
                    {
                        var previous = Previous(repeat, i);
                        if (i != index && !IsSubset(GetHistorySignatures(previous), printedSignatures))
                        {
                            return count;
                        }
                    }
                }
                return repeatCount;
            }

            // Assigns an index to each distinct stack trace in the history, so that the stack traces in each trace's history can be stored as bits.
            private void IndexSignatures()
            {
                var visited = new HashSet<CausalityTrace>();
                var stack = new Stack<CausalityTrace>();
                stack.Push(_trace);
                while (stack.Count > 0)
                {
                    var trace = stack.Pop();
                    if (!visited.Add(trace))
                    {
                        continue;
                    }
                    var signature = trace.GetSignature();
                    // Traces without visible frames all print the same marker, which has its own index.
                    if (!signature.IsEmpty && !_signatureIndices.ContainsKey(signature))
                    {
                        _signatureIndices.Add(signature, _signatureIndices.Count);
                    }
                    foreach (var previous in trace._previousTraces)
                    {
                        stack.Push(previous);
                    }
                }
                _truncatedIndex = _signatureIndices.Count;
                _hiddenIndex = _signatureIndices.Count + 1;
                _signatureWordCount = (_signatureIndices.Count + 2 + 63) / 64;
                _printedSignatures = new ulong[_signatureWordCount];
            }

            // Maps each trace to the first trace with the same stack trace and the same history, so that identical traces are printed once.
            // This is iterative, because histories can be deep.
            private void CanonicalizeTraces()
            {
                var keys = new Dictionary<string, int>();
                var ids = new Dictionary<CausalityTrace, int>();
                var canonicalTraces = new List<CausalityTrace>();
                var keyBuilder = new StringBuilder();
                var stack = new Stack<CausalityTrace>();
                stack.Push(_trace);
                while (stack.Count > 0)
                {
                    var current = stack.Peek();
                    if (ids.ContainsKey(current))
                    {
                        stack.Pop();
                        continue;
                    }
                    bool isReady = true;
                    foreach (var previous in current._previousTraces)
                    {
                        if (!ids.ContainsKey(previous))
                        {
                            stack.Push(previous);
                            isReady = false;
                        }
                    }
                    if (!isReady)
                    {
                        continue;
                    }
                    stack.Pop();

                    // The key is the stack trace, the capture (so that only copies of the same capture are merged, see IsSameCapture,
                    // and other traces with the same stack trace are folded where their threads and times are printed), whether the history was truncated,
                    // and the ids of the previous traces.
                    keyBuilder.Clear();
                    keyBuilder.Append(_signatureIndices.TryGetValue(current.GetSignature(), out int signatureIndex) ? signatureIndex : -1)
                        .Append('#').Append(current._threadId).Append('@').Append(current._timestamp)
                        .Append(current._isTruncated ? 't' : ':');
                    foreach (var previous in current._previousTraces)
                    {
                        keyBuilder.Append(ids[previous]).Append(',');
                    }
                    var key = keyBuilder.ToString();
                    if (!keys.TryGetValue(key, out int id))
                    {
                        id = canonicalTraces.Count;
                        keys.Add(key, id);
                        canonicalTraces.Add(current);
                    }
                    ids.Add(current, id);
                    _canonicalTraces.Add(current, canonicalTraces[id]);
                }
            }

            // Gets the previous trace as the trace that it's printed as.
            private CausalityTrace Previous(CausalityTrace trace, int index)
                => _canonicalTraces[trace._previousTraces[index]];

            // Gets the stack traces in the trace's history (including itself) as bits. This is iterative, because histories can be deep.
            private ulong[] GetHistorySignatures(CausalityTrace trace)
            {
                if (_historySignatures.TryGetValue(trace, out var result))
                {
                    return result;
                }
                var stack = new Stack<CausalityTrace>();
                stack.Push(trace);
                while (stack.Count > 0)
                {
                    var current = stack.Peek();
                    if (_historySignatures.ContainsKey(current))
                    {
                        stack.Pop();
                        continue;
                    }
                    bool isReady = true;
                    foreach (var previous in current._previousTraces)
                    {
                        if (!_historySignatures.ContainsKey(previous))
                        {
                            stack.Push(previous);
                            isReady = false;
                        }
                    }
                    if (!isReady)
                    {
                        continue;
                    }
                    stack.Pop();
                    var signatures = new ulong[_signatureWordCount];
                    AddSignature(signatures, current);
                    foreach (var previous in current._previousTraces)
                    {
                        Union(signatures, _historySignatures[previous]);
                    }
                    _historySignatures.Add(current, signatures);
                }
                return _historySignatures[trace];
            }

            private void AddSignature(ulong[] signatures, CausalityTrace trace)
            {
                if (_signatureIndices.TryGetValue(trace.GetSignature(), out int index))
                {
                    SetBit(signatures, index);
                }
                if (trace._isTruncated)
                {
                    SetBit(signatures, _truncatedIndex);
                }
                if (trace.GetSignature().IsEmpty)
                {
                    SetBit(signatures, _hiddenIndex);
                }
            }

            private static void SetBit(ulong[] signatures, int index)
                => signatures[index >> 6] |= 1UL << (index & 63);

            private void AddHistorySignatures(ulong[] signatures, CausalityTrace trace)
                => Union(signatures, GetHistorySignatures(trace));

            private static void Union(ulong[] signatures, ulong[] otherSignatures)
            {
                for (int i = 0; i < signatures.Length; i++)
                {
                    signatures[i] |= otherSignatures[i];
                }
            }

            private static bool Intersects(ulong[] signatures, ulong[] otherSignatures)
            {
                for (int i = 0; i < signatures.Length; i++)
                {
                    if ((signatures[i] & otherSignatures[i]) != 0)
                    {
                        return true;
                    }
                }
                return false;
            }

            private static bool IsSubset(ulong[] signatures, ulong[] otherSignatures)
            {
                for (int i = 0; i < signatures.Length; i++)
                {
                    if ((signatures[i] & ~otherSignatures[i]) != 0)
                    {
                        return false;
                    }
                }
                return true;
            }

            // A trace may be printed as another trace only if every stack trace in its history is also in the other trace's history.
            private bool IsCoveredBy(CausalityTrace trace, CausalityTrace other)
                => IsSubset(GetHistorySignatures(trace), GetHistorySignatures(other));

            private bool HasSameOuterScopes(CausalityTrace trace, CausalityTrace other, int skipIndex)
            {
                // HasSameStackTrace already verified that they have the same number of previous traces.
                for (int i = 1; i < trace._previousTraces.Length; i++)
                {
                    if (i != skipIndex && !IsSameOuterScope(Previous(trace, i), Previous(other, i)))
                    {
                        return false;
                    }
                }
                return true;
            }

            // Whether the outer scopes are (or will be) printed as the same outer scope.
            private bool IsSameOuterScope(CausalityTrace outerScope, CausalityTrace otherOuterScope)
            {
                if (outerScope == otherOuterScope)
                {
                    return true;
                }
                var printedAs = FindOuterScope(outerScope);
                var otherPrintedAs = FindOuterScope(otherOuterScope);
                return printedAs != null || otherPrintedAs != null
                    ? printedAs == otherPrintedAs
                    // Neither is printed yet. The other will be printed first, then this will be printed as the other (see FindOuterScope).
                    : CanFold(outerScope) && outerScope.HasSameStackTrace(otherOuterScope) && IsCoveredBy(outerScope, otherOuterScope);
            }

            private void AppendOuterScopeReferences(CausalityTrace trace, int skipIndex)
            {
                // Different outer scopes may be printed as the same outer scope, which is only referred to once, with the threads and times of all of them.
                var references = new List<(CausalityTrace printedAs, List<CausalityTrace> outerScopes)>();
                for (int i = 1; i < trace._previousTraces.Length; i++)
                {
                    if (i == skipIndex)
                    {
                        continue;
                    }
                    var outerScope = Previous(trace, i);
                    var printedAs = AddOuterScope(outerScope);
                    int referenceIndex = references.FindIndex(reference => reference.printedAs == printedAs);
                    if (referenceIndex < 0)
                    {
                        references.Add((printedAs, new List<CausalityTrace>() { outerScope }));
                    }
                    else
                    {
                        references[referenceIndex].outerScopes.Add(outerScope);
                    }
                }
                foreach (var (printedAs, outerScopes) in references)
                {
                    AppendOuterScopeReference(printedAs, outerScopes);
                }
            }

            private void AppendOuterScopeReference(CausalityTrace outerScope)
                => AppendOuterScopeReference(AddOuterScope(outerScope), new List<CausalityTrace>() { outerScope });

            // Returns the trace that the outer scope is printed as, which is printed in its own section.
            private CausalityTrace AddOuterScope(CausalityTrace outerScope)
            {
                var printedAs = FindOuterScope(outerScope) ?? outerScope;
                // Every outer scope is printed in its own section, even if its trace was already printed as part of a chain.
                if (_outerScopeSet.Add(printedAs))
                {
                    _outerScopes.Add(printedAs);
                    _currentOuterScopes.Add(printedAs);
                }
                return printedAs;
            }

            // Refers to the section of the trace that the outer scopes are printed as.
            private void AppendOuterScopeReference(CausalityTrace printedAs, List<CausalityTrace> outerScopes)
            {
                // The path is assigned where the outer scope is first referenced.
                if (!_outerScopePaths.TryGetValue(printedAs, out var path))
                {
                    _outerScopeChildCounts.TryGetValue(_currentPath, out int childCount);
                    _outerScopeChildCounts[_currentPath] = childCount + 1;
                    var letters = GetLetters(childCount);
                    path = _currentPath.Length == 0 ? letters : _currentPath + "/" + letters;
                    _outerScopePaths.Add(printedAs, path);
                }
                _sb.Append(MessageIndent).Append("--- [Outer causality scope ").Append(path)
                    .Append(_printedOuterScopes.Contains(printedAs) ? ", see above" : ", see below");
                // Outer scopes that are printed as another trace with the same stack trace (e.g. loop iterations) are followed by their own threads and times.
                // Each capture is only listed once, because the same trace may be an outer scope more than once (e.g. a scope that was entered twice).
                var others = new List<CausalityTrace>();
                foreach (var outerScope in outerScopes)
                {
                    if (!IsSameCapture(outerScope, printedAs) && !others.Exists(other => IsSameCapture(other, outerScope)))
                    {
                        others.Add(outerScope);
                    }
                }
                if (others.Count > 0)
                {
                    _sb.Append("; ");
                    AppendThreadsAndTimes(_sb, others);
                }
                _sb.AppendLine("] ---");
            }

            // Gets the trace that the outer scope is printed as, or null if it's new.
            private CausalityTrace FindOuterScope(CausalityTrace outerScope)
            {
                if (_printed.Contains(outerScope))
                {
                    return outerScope;
                }
                // Referring to the outer scope that is being printed (or one that contains it) would be circular, so it's printed as a new outer scope instead.
                if (!CanFold(outerScope))
                {
                    // It can only be printed as itself.
                    return _outerScopeSet.Contains(outerScope) && !_currentScopes.Contains(outerScope) ? outerScope : null;
                }
                foreach (var existing in _outerScopes)
                {
                    if (_currentScopes.Contains(existing))
                    {
                        continue;
                    }
                    // An outer scope with the same stack trace as an existing one (e.g. loop iterations) is printed as that one,
                    // as long as every stack trace in its history is also in the existing one's history.
                    if (outerScope == existing || (outerScope.HasSameStackTrace(existing) && IsCoveredBy(outerScope, existing)))
                    {
                        return existing;
                    }
                }
                return null;
            }

            private void AppendRepeated(CausalityTrace trace, LabelKind labelKind, int count, string details = null)
                => _insertions.Add(new Insertion(_sb.Length, GetLabelId(trace, labelKind), InsertionKind.Reference, count, details));

            private void AppendLabel(InsertionKind kind, CausalityTrace trace, LabelKind labelKind)
                => _insertions.Add(new Insertion(_sb.Length, GetLabelId(trace, labelKind), kind, 0, null));

            // Label ids combine the trace and the label kind.
            private int GetLabelId(CausalityTrace trace, LabelKind labelKind)
            {
                if (!_traceIds.TryGetValue(trace, out int traceId))
                {
                    traceId = _traceIds.Count;
                    _traceIds.Add(trace, traceId);
                }
                return traceId * 2 + (int) labelKind;
            }

            // Numbers the blocks that something refers to in order of appearance, and inserts their labels and the references to them into the text.
            private string InsertLabels()
            {
                var referencedIds = new HashSet<int>();
                foreach (var insertion in _insertions)
                {
                    if (insertion._kind == InsertionKind.Reference)
                    {
                        referencedIds.Add(insertion._labelId);
                    }
                }
                // The insertions are in order of their positions, and a block is always printed before it is referred to.
                var numbers = new Dictionary<int, int>();
                foreach (var insertion in _insertions)
                {
                    if (insertion._kind == InsertionKind.Begin && referencedIds.Contains(insertion._labelId))
                    {
                        numbers.Add(insertion._labelId, numbers.Count + 1);
                    }
                }

                var text = _sb.ToString();
                var sb = new StringBuilder(text.Length + numbers.Count * 64);
                int position = 0;
                foreach (var insertion in _insertions)
                {
                    sb.Append(text, position, insertion._position - position);
                    position = insertion._position;
                    if (!numbers.TryGetValue(insertion._labelId, out int number))
                    {
                        // Nothing refers to this block.
                        continue;
                    }
                    sb.Append(MessageIndent).Append("--- [#").Append(number);
                    switch (insertion._kind)
                    {
                        case InsertionKind.Begin:
                            sb.Append(" Begin");
                            break;
                        case InsertionKind.End:
                            sb.Append(" End");
                            break;
                        default:
                            sb.Append(" repeated ").Append(insertion._count).Append(insertion._count == 1 ? " time" : " times").Append(insertion._details);
                            break;
                    }
                    sb.AppendLine("] ---");
                }
                sb.Append(text, position, text.Length - position);
                return sb.ToString();
            }

            // A, B, ..., Z, AA, AB, ...
            private static string GetLetters(int index)
            {
                var letters = string.Empty;
                for (++index; index > 0; index = (index - 1) / 26)
                {
                    letters = (char) ('A' + (index - 1) % 26) + letters;
                }
                return letters;
            }
        }
    } // class CausalityTrace
} // namespace Proto.Diagnostics
