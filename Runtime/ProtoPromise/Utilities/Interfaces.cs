namespace Proto.Promises
{
    public interface ICancelable
    {
        /// <summary>
        /// Cancel this instance without a reason.
        /// </summary>
        void Cancel();
    }

    public interface ICancelableAny : ICancelable
    {
        /// <summary>
        /// Cancel this instance with <paramref name="reason"/>.
        /// </summary>
        void Cancel<TCancel>(TCancel reason);
    }

    public interface IRetainable
    {
        /// <summary>
        /// Retain this instance.
        /// <para/>This should always be paired with a call to <see cref="Release"/>
        /// </summary>
        void Retain();
        /// <summary>
        /// Release this instance.
        /// <para/>This should always be paired with a call to <see cref="Retain"/>
        /// </summary>
        void Release();
    }
}