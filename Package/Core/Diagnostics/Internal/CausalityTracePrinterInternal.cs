#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

#pragma warning disable IDE0028 // Simplify collection initialization
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0300 // Simplify collection initialization

namespace Proto.Diagnostics
{
    // Formats causality traces for ToString. Causality traces form a graph where previous traces are shared,
    // so the printer makes sure that each trace is printed at most once, and folds repeated traces (e.g. from loops or recursion).
    partial class CausalityTrace
    {
        // Lazily created when the trace is printed.
        private Signature _signature;

        // Whether this trace has the same stack trace as the other trace, and the same number of previous traces.
        // The printer also requires that their previous traces are printed the same way, and that folding them doesn't hide any stack trace.
        private bool HasSameStackTrace(CausalityTrace other)
            => GetSignature().Equals(other.GetSignature()) && _previousTraces.Length == other._previousTraces.Length;

        private Signature GetSignature()
            => _signature ?? (_signature = new Signature(_stackTrace));

        // Identifies traces that were captured from the same code path, by their visible frames' methods and offsets.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class Signature : IEquatable<Signature>
        {
            private readonly System.Reflection.MethodBase[] _methods;
            private readonly int[] _offsets;
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
                int hashCode = frames.Length;
                for (int i = 0; i < frames.Length; i++)
                {
                    var frame = frames[i];
                    _methods[i] = frame.GetMethod();
                    // Some runtimes (like IL2CPP) don't provide IL offsets, so we fall back to the line number.
                    int ilOffset = frame.GetILOffset();
                    _offsets[i] = ilOffset != StackFrame.OFFSET_UNKNOWN ? ilOffset : ~frame.GetFileLineNumber();
                    hashCode = unchecked(hashCode * 31 + _methods[i].GetHashCode() * 17 + _offsets[i]);
                }
                _hashCode = hashCode;
            }

            internal bool IsEmpty => _methods.Length == 0;

            public bool Equals(Signature other)
            {
                // Traces without visible frames have nothing to compare, so we don't treat them as repeats.
                if (other == null || _methods.Length == 0 || _hashCode != other._hashCode || _methods.Length != other._methods.Length)
                {
                    return false;
                }
                for (int i = 0; i < _methods.Length; i++)
                {
                    if (_offsets[i] != other._offsets[i] || !_methods[i].Equals(other._methods[i]))
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

                internal Insertion(int position, int labelId, InsertionKind kind, int count)
                {
                    _position = position;
                    _labelId = labelId;
                    _kind = kind;
                    _count = count;
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
            // Every trace is printed at most once, which guarantees that the output can't grow faster than the number of traces.
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
            // Traces with the same stack trace and the same history (compared by content, not by reference) are printed as one trace.
            private readonly Dictionary<CausalityTrace, CausalityTrace> _canonicalTraces = new Dictionary<CausalityTrace, CausalityTrace>();
            // The new outer scopes that were referenced by the chain that is being printed.
            private List<CausalityTrace> _currentOuterScopes = new List<CausalityTrace>();

            internal Printer(CausalityTrace trace)
            {
                _trace = trace;
            }

            internal string Print()
            {
                IndexSignatures();
                CanonicalizeTraces();
                AppendChain(_trace);
                AppendOuterScopes(_currentOuterScopes);
                return InsertLabels();
            }

            // Prints the outer scopes depth-first, so that each outer scope's outer scopes are printed right after it.
            private void AppendOuterScopes(List<CausalityTrace> outerScopes)
            {
                foreach (var outerScope in outerScopes)
                {
                    var path = _outerScopePaths[outerScope];
                    _sb.AppendLine()
                        .Append("--- [Outer causality scope ").Append(path).AppendLine("] ---");
                    _printedOuterScopes.Add(outerScope);
                    var parentPath = _currentPath;
                    _currentPath = path;
                    _currentScopes.Add(outerScope);
                    _currentOuterScopes = new List<CausalityTrace>();
                    AppendChain(outerScope);
                    var childOuterScopes = _currentOuterScopes;
                    AppendOuterScopes(childOuterScopes);
                    _currentScopes.RemoveAt(_currentScopes.Count - 1);
                    _currentPath = parentPath;
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
                    _sb.Append(trace.FormatStackTrace());
                    _printed.Add(trace);
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
                        AppendRepeated(trace, LabelKind.Repeated, repeatCount);
                        if (repeatIndex == 0)
                        {
                            // Repeats in the main chain (e.g. recursion). Their histories continue after the last repeat.
                            previousSource = lastRepeat;
                        }
                        else
                        {
                            // Repeats in an outer scope (e.g. each loop iteration ran inside the previous iteration's scope).
                            // The outer scopes continue with the last repeat's outer scope.
                            AppendOuterScopeReference(repeatIndex < lastRepeat._previousTraces.Length ? Previous(lastRepeat, repeatIndex) : null, true);
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
                _sb.Append(trace.FormatStackTrace());
                _printed.Add(trace);
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
                AppendOuterScopeReference(index < lastRepeat._previousTraces.Length ? Previous(lastRepeat, index) : null, true);
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
                        }
                        previous = null;
                        return false;
                    }

                    _sb.AppendLine(PreviousLocationHeader);
                    previous = Previous(trace, 0);
                    if (previous == null)
                    {
                        // A null CausalityTrace was passed to EnterScope.
                        _sb.Append(MessageIndent).AppendLine("--- [Missing Trace] ---");
                        return false;
                    }
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
                        if (next == null || _printed.Contains(next) || _foldedRepeats.ContainsKey(next)
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
                        if (i != index && previous != null && !IsSubset(GetHistorySignatures(previous), printedSignatures))
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
                    // Traces without visible frames have no stack trace to lose.
                    if (!signature.IsEmpty && !_signatureIndices.ContainsKey(signature))
                    {
                        _signatureIndices.Add(signature, _signatureIndices.Count);
                    }
                    foreach (var previous in trace._previousTraces)
                    {
                        if (previous != null)
                        {
                            stack.Push(previous);
                        }
                    }
                }
                _signatureWordCount = (_signatureIndices.Count + 63) / 64;
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
                        if (previous != null && !ids.ContainsKey(previous))
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

                    // The key is the stack trace, whether the history was truncated, and the ids of the previous traces.
                    keyBuilder.Clear();
                    keyBuilder.Append(_signatureIndices.TryGetValue(current.GetSignature(), out int signatureIndex) ? signatureIndex : -1)
                        .Append(current._isTruncated ? 't' : ':');
                    foreach (var previous in current._previousTraces)
                    {
                        keyBuilder.Append(previous == null ? -1 : ids[previous]).Append(',');
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
            {
                var previous = trace._previousTraces[index];
                return previous == null ? null : _canonicalTraces[previous];
            }

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
                        if (previous != null && !_historySignatures.ContainsKey(previous))
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
                        if (previous != null)
                        {
                            Union(signatures, _historySignatures[previous]);
                        }
                    }
                    _historySignatures.Add(current, signatures);
                }
                return _historySignatures[trace];
            }

            private void AddSignature(ulong[] signatures, CausalityTrace trace)
            {
                if (_signatureIndices.TryGetValue(trace.GetSignature(), out int index))
                {
                    signatures[index >> 6] |= 1UL << (index & 63);
                }
            }

            private void AddHistorySignatures(ulong[] signatures, CausalityTrace trace)
            {
                if (trace != null)
                {
                    Union(signatures, GetHistorySignatures(trace));
                }
            }

            private static void Union(ulong[] signatures, ulong[] otherSignatures)
            {
                for (int i = 0; i < signatures.Length; i++)
                {
                    signatures[i] |= otherSignatures[i];
                }
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
                if (outerScope == null || otherOuterScope == null)
                {
                    return false;
                }
                var printedAs = FindOuterScope(outerScope);
                var otherPrintedAs = FindOuterScope(otherOuterScope);
                return printedAs != null || otherPrintedAs != null
                    ? printedAs == otherPrintedAs
                    // Neither is printed yet. The other will be printed first, then this will be printed as the other (see FindOuterScope).
                    : outerScope.HasSameStackTrace(otherOuterScope) && IsCoveredBy(outerScope, otherOuterScope);
            }

            private void AppendOuterScopeReferences(CausalityTrace trace, int skipIndex)
            {
                // Different outer scopes may be printed as the same outer scope, which is only referred to once.
                var referencedScopes = new HashSet<CausalityTrace>();
                for (int i = 1; i < trace._previousTraces.Length; i++)
                {
                    if (i != skipIndex)
                    {
                        AppendOuterScopeReference(Previous(trace, i), false, referencedScopes);
                    }
                }
            }

            private void AppendOuterScopeReference(CausalityTrace outerScope, bool isNullReported, HashSet<CausalityTrace> referencedScopes = null)
            {
                if (outerScope == null)
                {
                    if (!isNullReported && (referencedScopes == null || referencedScopes.Add(null)))
                    {
                        // A null CausalityTrace was passed to EnterScope.
                        _sb.Append(MessageIndent).AppendLine("--- [Outer causality scope without a trace] ---");
                    }
                    return;
                }
                var printedAs = FindOuterScope(outerScope) ?? outerScope;
                // Every outer scope is printed in its own section, even if its trace was already printed as part of a chain.
                if (_outerScopeSet.Add(printedAs))
                {
                    _outerScopes.Add(printedAs);
                    _currentOuterScopes.Add(printedAs);
                }
                if (referencedScopes != null && !referencedScopes.Add(printedAs))
                {
                    return;
                }
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
                    .Append(_printedOuterScopes.Contains(printedAs) ? ", see above" : ", see below")
                    .AppendLine("] ---");
            }

            // Gets the trace that the outer scope is printed as, or null if it's new.
            private CausalityTrace FindOuterScope(CausalityTrace outerScope)
            {
                if (outerScope == null || _printed.Contains(outerScope))
                {
                    return outerScope;
                }
                foreach (var existing in _outerScopes)
                {
                    // Referring to the outer scope that is being printed (or one that contains it) would be circular, so it's printed as a new outer scope instead.
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

            private void AppendRepeated(CausalityTrace trace, LabelKind labelKind, int count)
                => _insertions.Add(new Insertion(_sb.Length, GetLabelId(trace, labelKind), InsertionKind.Reference, count));

            private void AppendLabel(InsertionKind kind, CausalityTrace trace, LabelKind labelKind)
                => _insertions.Add(new Insertion(_sb.Length, GetLabelId(trace, labelKind), kind, 0));

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
                            sb.Append(" repeated ").Append(insertion._count).Append(insertion._count == 1 ? " time" : " times");
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
