using System;
using System.Threading;

namespace Proto.Promises
{
    /// <summary>Represents a timer that can have its due time and period changed, and which may be pooled when it is disposed.</summary>
    /// <remarks>
    /// Users of this interface must ensure that <see cref="Change"/> is not called after <see cref="DisposeAsync"/>, and <see cref="DisposeAsync"/> is not called more than once.
    /// </remarks>
    public interface IPoolableTimer
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
        /// <returns><see langword="true"/> if the timer was successfully updated; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="dueTime"/> or <paramref name="period"/> parameter, in milliseconds, is less than -1 or greater than 4294967294.</exception>
        /// <remarks>
        /// This must not be called after <see cref="DisposeAsync"/>.
        /// </remarks>
        void Change(TimeSpan dueTime, TimeSpan period);

        /// <summary>
        /// Releases the instance such that it may be returned to an object pool.
        /// </summary>
        /// <returns>A <see cref="Promise"/> that will be resolved when all timer callbacks have completed.</returns>
        /// <remarks>
        /// <see cref="Change(TimeSpan, TimeSpan)"/> must not be called after this is called, and this must not be called more than once.
        /// </remarks>
        Promise DisposeAsync();
    }
}