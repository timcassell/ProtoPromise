#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System;
using System.Diagnostics;

namespace Proto.Promises
{
    internal static partial class Internal
    {
        // Idea from https://www.meziantou.net/how-to-measure-elapsed-time-without-allocating-a-stopwatch.htm
#pragma warning disable IDE0250 // Make struct 'readonly'
        internal struct ValueStopwatch
#pragma warning restore IDE0250 // Make struct 'readonly'
        {
            private static readonly double s_timestampToTicks = TimeSpan.TicksPerSecond / (double) Stopwatch.Frequency;

            private readonly long _startTimestamp;

            private ValueStopwatch(long startTimestamp)
            {
                _startTimestamp = startTimestamp;
            }

            internal static ValueStopwatch StartNew()
            {
                return new ValueStopwatch(GetTimestamp());
            }

            internal static long GetTimestamp()
            {
                return Stopwatch.GetTimestamp();
            }

            internal static TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
            {
                var timestampDelta = endTimestamp - startTimestamp;
                var ticks = (long) (s_timestampToTicks * timestampDelta);
                return new TimeSpan(ticks);
            }

            internal TimeSpan GetElapsedTime()
            {
                return GetElapsedTime(_startTimestamp, GetTimestamp());
            }
        }
    }
}