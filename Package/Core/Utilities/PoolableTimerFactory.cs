using Proto.Promises.Threading;
using System;
using System.Diagnostics;
using System.Threading;

namespace Proto.Promises
{
    /// <summary>Provides an abstraction for a poolable timer factory.</summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public abstract partial class PoolableTimerFactory
    {
        private protected TimeProvider _timeProvider;

        /// <summary>
        /// Gets a <see cref="PoolableTimerFactory"/> that creates poolable timers based on <see cref="TimeProvider.System"/>.
        /// </summary>
        public static PoolableTimerFactory System { get; } = new PoolableSystemTimerFactory();

        /// <summary>
        /// Initializes the <see cref="PoolableTimerFactory"/>.
        /// </summary>
        protected PoolableTimerFactory() { }

        /// <summary>Creates a new <see cref="IPoolableTimer"/> instance, using <see cref="TimeSpan"/> values to measure time intervals.</summary>
        /// <returns>
        /// The newly created <see cref="IPoolableTimer"/> instance.
        /// </returns>
        /// <remarks>
        /// <para/>
        /// The delegate specified by the callback parameter is invoked once after <paramref name="dueTime"/> elapses, and thereafter each time the <paramref name="period"/> time interval elapses.
        /// <para/>
        /// If <paramref name="dueTime"/> is zero, the callback is invoked immediately. If <paramref name="dueTime"/> is -1 milliseconds, <paramref name="callback"/> is not invoked; the timer is disabled,
        /// but can be re-enabled by calling the <see cref="IPoolableTimer.Change"/> method.
        /// <para/>
        /// If <paramref name="period"/> is 0 or -1 milliseconds and <paramref name="dueTime"/> is positive, <paramref name="callback"/> is invoked once; the periodic behavior of the timer is disabled,
        /// but can be re-enabled using the <see cref="IPoolableTimer.Change"/> method.
        /// <para/>
        /// <see cref="CreateTimer"/> captures the <see cref="ExecutionContext"/> and stores that with the <see cref="IPoolableTimer"/> for use in invoking <paramref name="callback"/>
        /// each time it's called. That capture can be suppressed with <see cref="ExecutionContext.SuppressFlow"/>.
        /// </remarks>
        /// <inheritdoc cref="TimeProvider.CreateTimer(TimerCallback, object, TimeSpan, TimeSpan)"/>
        public abstract IPoolableTimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period);

        /// <summary>
        /// Gets a <see cref="TimeProvider"/> that creates timers based on this <see cref="PoolableTimerFactory"/>.
        /// </summary>
        /// <returns>A <see cref="TimeProvider"/> based on this <see cref="PoolableTimerFactory"/>.</returns>
        /// <remarks>
        /// If the implemented <see cref="PoolableTimerFactory"/> does not override this method, the returned <see cref="TimeProvider"/>'s
        /// <see cref="TimeProvider.CreateTimer(TimerCallback, object, TimeSpan, TimeSpan)"/> method will create timers that wrap the timers
        /// created from <see cref="CreateTimer(TimerCallback, object, TimeSpan, TimeSpan)"/>, and other methods will use the default implementation.
        /// </remarks>
        public virtual TimeProvider ToTimeProvider()
        {
            var timeProvider = _timeProvider;
            if (timeProvider is null)
            {
                _timeProvider = timeProvider = new PoolableTimerFactoryTimeProvider(this);
            }
            return timeProvider;
        }

        /// <summary>
        /// Gets a <see cref="PoolableTimerFactory"/> that will create <see cref="IPoolableTimer"/> timers based on the <see cref="ITimer"/> timers created from the provided <paramref name="timeProvider"/>.
        /// </summary>
        /// <param name="timeProvider">The <see cref="TimeProvider"/> that is used to create the backing timers for the pooled timers.</param>
        /// <returns>A <see cref="PoolableTimerFactory"/> based on the provided <paramref name="timeProvider"/>.</returns>
        public static PoolableTimerFactory FromTimeProvider(TimeProvider timeProvider)
        {
            if (timeProvider is null)
            {
                throw new ArgumentNullException(nameof(timeProvider), $"The provided {nameof(timeProvider)} may not be null", Internal.GetFormattedStacktrace(1));
            }

            return timeProvider == TimeProvider.System ? System
                : timeProvider is PoolableTimerFactoryTimeProvider tp ? tp._poolableTimerFactory
                : new PoolableTimeProviderTimerFactory(timeProvider);
        }
    } // class PoolableTimerFactory
} // namespace Proto.Promises