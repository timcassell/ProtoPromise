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
#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Diagnostics
{
    // Formats causality traces for ToString: the trace and its chain of previous traces, like an async stack trace,
    // where parts of the chain that repeat (e.g. a callback that registers itself again from different threads) are printed once.
    partial class CausalityTrace
    {
        // Lazily created when the trace is printed.
        private Signature _signature;

        private Signature GetSignature()
        {
            // This may be set from multiple threads, but every signature is equivalent.
            var signature = Volatile.Read(ref _signature);
            if (signature == null)
            {
                signature = new Signature(_stackTrace, _firstFrame);
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
            internal Signature(StackTrace stackTrace, int firstFrame)
            {
                var frames = GetVisibleFrames(stackTrace, firstFrame);
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

            public bool Equals(Signature other)
            {
                // Traces without visible frames have nothing to compare, so we don't treat them as repeats.
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

        // Internal so that tests can check descriptions of threads that their runtime may not have (e.g. unnamed thread pool threads, which newer runtimes name).
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class Printer
        {
            private const string PreviousLocationHeader = "--- End of stack trace from the previous location ---";
            // Every other message is indented like the stack frames, so that the previous location headers stand out.
            private const string MessageIndent = "   ";
            // The longest part of the chain that is checked for repeats, so that printing a long chain stays fast.
            private const int MaxRepeatedLength = 32;

            private readonly StringBuilder _sb = new StringBuilder();
            private readonly CausalityTrace _trace;
            private readonly List<CausalityTrace> _chain = new List<CausalityTrace>();
            private int _labelCount;

            private Printer(CausalityTrace trace)
            {
                _trace = trace;
            }

            internal static string Print(CausalityTrace trace)
                => new Printer(trace).Print();

            private string Print()
            {
                for (var trace = _trace; trace != null; trace = trace._previous)
                {
                    _chain.Add(trace);
                }

                int index = 0;
                while (index < _chain.Count)
                {
                    if (index > 0)
                    {
                        _sb.AppendLine(PreviousLocationHeader);
                    }
                    int length = GetRepeatedLength(index, out int repeatCount);
                    if (length <= 1)
                    {
                        // A single repeated trace is printed with the others as its repeats.
                        AppendTrace(_chain[index], GetRepeats(index, 1, repeatCount));
                        index += 1 + repeatCount;
                        continue;
                    }

                    int label = ++_labelCount;
                    _sb.Append(MessageIndent).Append("--- #").Append(label).AppendLine(" Begin ---");
                    for (int i = 0; i < length; i++)
                    {
                        if (i > 0)
                        {
                            _sb.AppendLine(PreviousLocationHeader);
                        }
                        AppendTrace(_chain[index + i], GetRepeats(index + i, length, repeatCount));
                    }
                    _sb.Append(MessageIndent).Append("--- #").Append(label).AppendLine(" End ---");
                    // The threads and times of the repeated parts are the threads and times of their first traces.
                    var repeats = Array.Empty<RepeatGroup>();
                    for (int i = repeatCount; i > 0; i--)
                    {
                        repeats = RepeatGroup.Add(repeats, _chain[index + length * i]);
                    }
                    _sb.Append(MessageIndent).Append("--- #").Append(label).Append(" repeated ").Append(repeatCount).Append(repeatCount == 1 ? " time: " : " times: ");
                    AppendRepeats(repeats);
                    _sb.AppendLine(" ---");
                    index += length * (1 + repeatCount);
                }

                if (_chain[_chain.Count - 1]._isTruncated)
                {
                    _sb.AppendLine(PreviousLocationHeader);
                    _sb.Append(MessageIndent).AppendLine($"--- Older traces truncated, increase {nameof(CausalityTrace)}.{nameof(MaxDepth)} to see more ---");
                }
                return _sb.ToString();
            }

            // Returns the length of the shortest part of the chain starting at the index that is repeated right after it, and how many times it's repeated,
            // or 0 if it's not repeated.
            private int GetRepeatedLength(int index, out int repeatCount)
            {
                int maxLength = Math.Min(MaxRepeatedLength, (_chain.Count - index) / 2);
                for (int length = 1; length <= maxLength; length++)
                {
                    repeatCount = 0;
                    while (index + length * (repeatCount + 2) <= _chain.Count && IsRepeat(index, index + length * (repeatCount + 1), length))
                    {
                        ++repeatCount;
                    }
                    if (repeatCount > 0)
                    {
                        return length;
                    }
                }
                repeatCount = 0;
                return 0;
            }

            // Whether the part of the chain at the other index is a repeat of the part at the index.
            // Their outer traces must also be the same, so that folding the repeats doesn't hide any stack trace.
            private bool IsRepeat(int index, int otherIndex, int length)
            {
                for (int i = 0; i < length; i++)
                {
                    var trace = _chain[index + i];
                    var other = _chain[otherIndex + i];
                    if (!trace.GetSignature().Equals(other.GetSignature()) || trace._outerTraces.Length != other._outerTraces.Length
                        || trace._areOuterTracesTruncated != other._areOuterTracesTruncated)
                    {
                        return false;
                    }
                    for (int j = 0; j < trace._outerTraces.Length; j++)
                    {
                        if (!trace._outerTraces[j].GetSignature().Equals(other._outerTraces[j].GetSignature()))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            // Gets the repeats of the trace at the index, with the repeats of its copies in the repeated parts of the chain (every length traces after it).
            // If the part is only that trace, the copies are also its repeats. Otherwise they're printed as the repeats of the part.
            private RepeatGroup[] GetRepeats(int index, int length, int repeatCount)
            {
                var repeats = _chain[index]._repeats;
                for (int i = 1; i <= repeatCount; i++)
                {
                    var copy = _chain[index + length * i];
                    foreach (var repeat in copy._repeats)
                    {
                        repeats = RepeatGroup.Add(repeats, repeat);
                    }
                    if (length == 1)
                    {
                        repeats = RepeatGroup.Add(repeats, copy);
                    }
                }
                return repeats;
            }

            // Prints the trace, with the repeats, and its outer traces.
            private void AppendTrace(CausalityTrace trace, RepeatGroup[] repeats)
            {
                AppendHeader(trace, "");
                AppendStackTrace(trace, "");

                if (repeats.Length > 0)
                {
                    int count = 0;
                    foreach (var repeat in repeats)
                    {
                        count += repeat.Count;
                    }
                    _sb.Append(MessageIndent).Append("--- Repeated ").Append(count).Append(count == 1 ? " time: " : " times: ");
                    AppendRepeats(repeats);
                    _sb.AppendLine(" ---");
                }

                // Each outer trace is the trace of a scope outside of the one before it, so it's indented one more level.
                string indent = "";
                foreach (var outerTrace in trace._outerTraces)
                {
                    indent += MessageIndent;
                    AppendHeader(outerTrace, indent);
                    AppendStackTrace(outerTrace, indent);
                }
                if (trace._areOuterTracesTruncated)
                {
                    indent += MessageIndent;
                    _sb.Append(indent).Append(MessageIndent).AppendLine($"--- Outer traces truncated, increase {nameof(CausalityTrace)}.{nameof(MaxOuterTraces)} to see more ---");
                }
            }

            // E.g. --- Thread 5 (thread pool), 12.345 ms earlier ---
            private void AppendHeader(CausalityTrace trace, string indent)
            {
                _sb.Append(indent).Append(MessageIndent).Append("--- ");
                AppendThreadDescription(_sb, trace._threadId, trace._threadName, trace._isThreadPoolThread, trace._isBackgroundThread);
                if (trace != _trace)
                {
                    _sb.Append(", ");
                    AppendMilliseconds(trace._timestamp);
                    _sb.Append(" ms earlier");
                }
                _sb.AppendLine(" ---");
            }

            private void AppendStackTrace(CausalityTrace trace, string indent)
            {
                var frames = trace.GetFrames();
                if (frames.Length == 0)
                {
                    // Every frame is hidden (e.g. ProtoPromise completed the operation from its own code), so we show that the trace is there.
                    _sb.Append(indent).Append(MessageIndent).AppendLine("--- All frames are hidden ---");
                    return;
                }
                foreach (var frame in frames)
                {
                    _sb.Append(indent);
                    AppendFrame(_sb, frame);
                }
            }

            // Prints the repeats from the most recent, e.g. "Thread 1 "Main Thread" 2 times at 3.456 to 7.890 ms earlier; thread pool 1 time at 9.876 ms earlier".
            private void AppendRepeats(RepeatGroup[] repeats)
            {
                for (int i = 0; i < repeats.Length; i++)
                {
                    var repeat = repeats[i];
                    if (i > 0)
                    {
                        _sb.Append("; ");
                    }
                    if (repeat.IsThreadPoolThread)
                    {
                        // Repeats on any thread pool thread are grouped.
                        _sb.Append("thread pool");
                    }
                    else
                    {
                        AppendThreadDescription(_sb, repeat.ThreadId, repeat.ThreadName, false, repeat.IsBackgroundThread);
                    }
                    _sb.Append(' ').Append(repeat.Count).Append(repeat.Count == 1 ? " time at " : " times at ");
                    AppendMilliseconds(repeat.LastTimestamp);
                    if (repeat.Count > 1)
                    {
                        _sb.Append(" to ");
                        AppendMilliseconds(repeat.FirstTimestamp);
                    }
                    _sb.Append(" ms earlier");
                }
            }

            // Thread 5 "Name", or a description of an unnamed thread.
            internal static void AppendThreadDescription(StringBuilder sb, int threadId, string threadName, bool isThreadPoolThread, bool isBackgroundThread)
            {
                sb.Append("Thread ").Append(threadId).Append(' ');
                if (threadName != null)
                {
                    // Line breaks are replaced, so that the name can't break the line.
                    sb.Append('"').Append(threadName.Replace('\r', ' ').Replace('\n', ' ')).Append('"');
                }
                else if (isThreadPoolThread)
                {
                    sb.Append("(thread pool)");
                }
                else
                {
                    sb.Append(isBackgroundThread ? "(background)" : "(foreground)");
                }
            }

            // How long before the printed trace the timestamp is.
            private void AppendMilliseconds(long timestamp)
            {
                // The clock may not be monotonic on some platforms, so we don't print negative times.
                double milliseconds = Math.Max(0d, TimeProvider.System.GetElapsedTime(timestamp, _trace._timestamp).TotalMilliseconds);
                _sb.Append(milliseconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    } // class CausalityTrace
} // namespace Proto.Diagnostics
