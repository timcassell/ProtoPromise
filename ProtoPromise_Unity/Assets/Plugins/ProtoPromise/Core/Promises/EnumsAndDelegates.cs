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
            /// The promise failed to complete due to a reason.
            /// </summary>
            Rejected,
            /// <summary>
            /// The promise was canceled before the operation was able to complete.
            /// </summary>
            Canceled
        }

        // These are necesary to pass ResultContainer to ContinueWith callbacks (because it is a ref struct, it cannot be used as a generic argument in System.Action<> and System.Func<>).

        /// <summary>
        /// The delegate type used for <see cref="ContinueWith(ContinueAction, CancelationToken)"/>.
        /// </summary>
        /// <param name="resultContainer">The container from which the promise's state and result or reason can be extracted.</param>
        public delegate void ContinueAction(ResultContainer resultContainer);
        /// <summary>
        /// The delegate type used for <see cref="ContinueWith{TCapture}(TCapture, ContinueAction{TCapture}, CancelationToken)"/>.
        /// </summary>
        /// <param name="capturedValue">The value that was passed to <see cref="ContinueWith{TCapture}(TCapture, ContinueAction{TCapture}, CancelationToken)"/>.</param>
        /// <param name="resultContainer">The container from which the promise's state and result or reason can be extracted.</param>
        public delegate void ContinueAction<TCapture>(TCapture capturedValue, ResultContainer resultContainer);
        /// <summary>
        /// The delegate type used for <see cref="ContinueWith{TResult}(ContinueFunc{TResult}, CancelationToken)"/>.
        /// </summary>
        /// <param name="resultContainer">The container from which the promise's state and result or reason can be extracted.</param>
        public delegate TResult ContinueFunc<TResult>(ResultContainer resultContainer);
        /// <summary>
        /// The delegate type used for <see cref="ContinueWith{TCapture, TResult}(TCapture, ContinueFunc{TCapture, TResult}, CancelationToken)"/>.
        /// </summary>
        /// <param name="capturedValue">The value that was passed to <see cref="ContinueWith{TCapture, TResult}(TCapture, ContinueFunc{TCapture, TResult}, CancelationToken)"/>.</param>
        /// <param name="resultContainer">The container from which the promise's state and result or reason can be extracted.</param>
        public delegate TResult ContinueFunc<TCapture, TResult>(TCapture capturedValue, ResultContainer resultContainer);
    }

    partial struct Promise<T>
    {
        /// <summary>
        /// The delegate type used for <see cref="ContinueWith(ContinueAction, CancelationToken)"/>.
        /// </summary>
        /// <param name="resultContainer">The container from which the promise's state and result or reason can be extracted.</param>
        public delegate void ContinueAction(ResultContainer resultContainer);
        /// <summary>
        /// The delegate type used for <see cref="ContinueWith{TCapture}(TCapture, ContinueAction{TCapture}, CancelationToken)"/>.
        /// </summary>
        /// <param name="capturedValue">The value that was passed to <see cref="ContinueWith{TCapture}(TCapture, ContinueAction{TCapture}, CancelationToken)"/>.</param>
        /// <param name="resultContainer">The container from which the promise's state and result or reason can be extracted.</param>
        public delegate void ContinueAction<TCapture>(TCapture capturedValue, ResultContainer resultContainer);
        /// <summary>
        /// The delegate type used for <see cref="ContinueWith{TResult}(ContinueFunc{TResult}, CancelationToken)"/>.
        /// </summary>
        /// <param name="resultContainer">The container from which the promise's state and result or reason can be extracted.</param>
        public delegate TResult ContinueFunc<TResult>(ResultContainer resultContainer);
        /// <summary>
        /// The delegate type used for <see cref="ContinueWith{TCapture, TResult}(TCapture, ContinueFunc{TCapture, TResult}, CancelationToken)"/>.
        /// </summary>
        /// <param name="capturedValue">The value that was passed to <see cref="ContinueWith{TCapture, TResult}(TCapture, ContinueFunc{TCapture, TResult}, CancelationToken)"/>.</param>
        /// <param name="resultContainer">The container from which the promise's state and result or reason can be extracted.</param>
        public delegate TResult ContinueFunc<TCapture, TResult>(TCapture capturedValue, ResultContainer resultContainer);
    }
}