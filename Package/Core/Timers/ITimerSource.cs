using Proto.Promises;
using System;
using System.Threading;

namespace Proto.Timers
{
    /// <summary>Represents an object that can be wrapped by a <see cref="Timer"/>.</summary>
    public interface ITimerSource
    {
        /// <summary>Changes the start time and the interval between method invocations for a timer, using <see cref="TimeSpan"/> values to measure time intervals.</summary>
        /// <param name="dueTime">
        /// A <see cref="TimeSpan"/> representing the amount of time to delay before invoking the callback method specified when the <see cref="ITimer"/> was constructed.
        /// Specify <see cref="Timeout.InfiniteTimeSpan"/> to prevent the timer from restarting. Specify <see cref="TimeSpan.Zero"/> to restart the timer immediately.
        /// </param>
        /// <param name="period">
        /// The time interval between invocations of the callback method specified when the Timer was constructed.
        /// Specify <see cref="Timeout.InfiniteTimeSpan"/> to disable periodic signaling.
        /// </param>
        /// <param name="token">An opaque value that was provided to the <see cref="Timer"/> constructor.</param>
        /// <returns><see langword="true"/> if the timer was successfully updated; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="dueTime"/> or <paramref name="period"/> parameter, in milliseconds, is less than -1 or greater than 4294967294.</exception>
        /// <exception cref="ObjectDisposedException">This was disposed.</exception>
        void Change(TimeSpan dueTime, TimeSpan period, int token);

        /// <summary>
        /// Releases resources used by the timer.
        /// </summary>
        /// <param name="token">An opaque value that was provided to the <see cref="Timer"/> constructor.</param>
        /// <returns>A <see cref="Promise"/> that will be resolved when all timer callbacks have completed.</returns>
        /// <exception cref="ObjectDisposedException">This was already disposed.</exception>
        Promise DisposeAsync(int token);
    }
}