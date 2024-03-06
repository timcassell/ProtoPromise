namespace Proto.Promises
{
    /// <summary>
    /// How the next continuation should be scheduled.
    /// </summary>
    public enum SynchronizationOption : byte
    {
        /// <summary>
        /// Schedule the next continuation to execute synchronously.
        /// </summary>
        Synchronous,
        /// <summary>
        /// Schedule the next continuation to execute on the <see cref="Promise.Config.ForegroundContext"/>.
        /// </summary>
        Foreground,
        /// <summary>
        /// Schedule the next continuation to execute on the <see cref="Promise.Config.BackgroundContext"/>.
        /// </summary>
        Background
    }

    partial struct Promise
    {
        /// <summary>
        /// The state of the promise.
        /// </summary>
        public enum State : byte
        {
            /// <summary>
            /// The promise has not yet completed. (The operation is in progress.)
            /// </summary>
            Pending,
            /// <summary>
            /// The promise completed successfully. (The operation ran to completion with no errors.)
            /// </summary>
            Resolved,
            /// <summary>
            /// The promise completed unsuccessfully. (The operation failed to complete due to a reason).
            /// </summary>
            Rejected,
            /// <summary>
            /// The promise completed unsuccessfully. (The operation was canceled before it was able to complete).
            /// </summary>
            Canceled
        }
    }
}