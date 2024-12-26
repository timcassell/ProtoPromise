#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises;
using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable IDE0290 // Use primary constructor

namespace Proto.Timers
{
    /// <summary>
    /// Represents a timer that can have its due time and period changed.
    /// </summary>
    public readonly partial struct Timer : IEquatable<Timer>
    {
        internal readonly ITimerSource _timerSource;
        internal readonly int _token;

        /// <summary>
        /// Initializes a new instance of <see cref="Timer"/> using the supplied <see cref="ITimerSource"/> object that represents the timer.
        /// </summary>
        /// <param name="timerSource">An object that represents the timer.</param>
        /// <param name="token">An opaque value that is passed through to the <see cref="ITimerSource"/>.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="timerSource"/> is <see langword="null"/>.</exception>
        public Timer(ITimerSource timerSource, int token)
        {
            if (timerSource is null)
            {
                throw new System.ArgumentNullException(nameof(timerSource));
            }
            _timerSource = timerSource;
            _token = token;
        }

        /// <summary>Changes the start time and the interval between method invocations for a timer, using <see cref="TimeSpan"/> values to measure time intervals.</summary>
        /// <param name="dueTime">
        /// A <see cref="TimeSpan"/> representing the amount of time to delay before invoking the callback method specified when the <see cref="ITimer"/> was constructed.
        /// Specify <see cref="Timeout.InfiniteTimeSpan"/> to prevent the timer from restarting. Specify <see cref="TimeSpan.Zero"/> to restart the timer immediately.
        /// </param>
        /// <param name="period">
        /// The time interval between invocations of the callback method specified when the Timer was constructed.
        /// Specify <see cref="Timeout.InfiniteTimeSpan"/> to disable periodic signaling.
        /// </param>
        /// <returns><see langword="true"/> if the timer was successfully updated; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="dueTime"/> or <paramref name="period"/> parameter, in milliseconds, is less than -1 or greater than 4294967294.</exception>
        /// <exception cref="ObjectDisposedException">This was disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public void Change(TimeSpan dueTime, TimeSpan period)
            => _timerSource.Change(dueTime, period, _token);

        /// <summary>
        /// Releases resources used by this timer.
        /// </summary>
        /// <returns>A <see cref="Promise"/> that will be resolved when all timer callbacks have completed.</returns>
        /// <exception cref="ObjectDisposedException">This was already disposed.</exception>
        /// <exception cref="NullReferenceException">This is a default value.</exception>
        public Promise DisposeAsync()
            => _timerSource.DisposeAsync(_token);

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="Timer"/>.</summary>
        public bool Equals(Timer other)
            => this == other;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj)
            => obj is Timer timer && Equals(timer);

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode()
            => HashCode.Combine(_timerSource, _token);

        /// <summary>Returns a value indicating whether two <see cref="Timer"/> values are equal.</summary>
        public static bool operator ==(Timer lhs, Timer rhs)
            => lhs._timerSource == rhs._timerSource
            & lhs._token == rhs._token;

        /// <summary>Returns a value indicating whether two <see cref="Timer"/> values are not equal.</summary>
        public static bool operator !=(Timer lhs, Timer rhs)
            => !(lhs == rhs);
    }

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
    partial struct Timer : IAsyncDisposable
    {
        ValueTask IAsyncDisposable.DisposeAsync()
            => DisposeAsync();
    }
#endif
} // namespace Proto.Timers