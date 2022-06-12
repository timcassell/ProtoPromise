namespace Proto.Promises
{
    /// <summary>
    /// Cancelable interface
    /// </summary>
    public interface ICancelable
    {
        /// <summary>
        /// Cancel this instance.
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// Retainable interface
    /// </summary>
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