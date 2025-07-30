#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;

namespace Proto.Promises
{
    partial class Internal
    {
        // Idea from https://www.meziantou.net/how-to-measure-elapsed-time-without-allocating-a-stopwatch.htm
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct ValueStopwatch
        {
            private static readonly double s_timestampToTicks = TimeSpan.TicksPerSecond / (double) Stopwatch.Frequency;

            private readonly long _startTimestamp;

            private ValueStopwatch(long startTimestamp)
            {
                _startTimestamp = startTimestamp;
            }

            internal static ValueStopwatch StartNew()
                => new ValueStopwatch(GetTimestamp());

            internal static long GetTimestamp()
                => Stopwatch.GetTimestamp();

            internal static TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
            {
                var timestampDelta = endTimestamp - startTimestamp;
                var ticks = (long) (s_timestampToTicks * timestampDelta);
                return new TimeSpan(ticks);
            }

            internal TimeSpan GetElapsedTime()
                => GetElapsedTime(_startTimestamp, GetTimestamp());
        }
    }
}