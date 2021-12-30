namespace Proto.Promises
{
    public enum SynchronizationOption : byte
    {
        Synchronous,
        Foreground,
        Background
    }

    partial struct Promise
    {
        public enum State : byte
        {
            Pending,
            Resolved,
            Rejected,
            Canceled
        }

        // These are necesary to pass ResultContainer to ContinueWith callbacks (because it is a ref struct, it cannot be used as a generic argument in System.Action<> and System.Func<>).
        public delegate void ContinueAction(ResultContainer resultContainer);
        public delegate void ContinueAction<TCapture>(TCapture capturedValue, ResultContainer resultContainer);
        public delegate TResult ContinueFunc<TResult>(ResultContainer resultContainer);
        public delegate TResult ContinueFunc<TCapture, TResult>(TCapture capturedValue, ResultContainer resultContainer);
        // Same for ReasonContainer.
        public delegate void CanceledAction(ReasonContainer resultContainer);
        public delegate void CanceledAction<TCapture>(TCapture capturedValue, ReasonContainer resultContainer);
    }

    partial struct Promise<T>
    {
        public delegate void ContinueAction(ResultContainer resultContainer);
        public delegate void ContinueAction<TCapture>(TCapture capturedValue, ResultContainer resultContainer);
        public delegate TResult ContinueFunc<TResult>(ResultContainer resultContainer);
        public delegate TResult ContinueFunc<TCapture, TResult>(TCapture capturedValue, ResultContainer resultContainer);
    }
}