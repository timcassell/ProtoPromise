using System;
using Proto.Utils;

namespace Proto.Promises
{
    public interface IValueConverter
    {
        /// <summary>
        /// Tries to convert valueContainer.Value to <typeparamref name="TConvert"/>. Returns true if successful, false otherwise.
        /// </summary>
        bool TryConvert<TOriginal, TConvert>(IValueContainer<TOriginal> valueContainer, out TConvert converted);
        /// <summary>
        /// Returns true if valueContainer.Value can be converted to <typeparamref name="TConvert"/>, false otherwise.
        /// </summary>
        bool CanConvert<TOriginal, TConvert>(IValueContainer<TOriginal> valueContainer);
    }

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
        /// Cancel this instance with the specified reason.
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

    /// <summary>
    /// Potential cancelation interface used to subscribe multiple cancelation callbacks accepting different types.
    /// </summary>
    public interface IPotentialCancelation : IRetainable
    {
        /// <summary>
        /// Add a cancel callback.
        /// <para/>If this instance is canceled for any or no reason, <paramref name="onCanceled"/> will be invoked.
        /// </summary>
        void CatchCancelation(Action onCanceled);
        /// <summary>
        /// Add a cancel callback. Returns an <see cref="IPotentialCancelation"/> object.
        /// <para/>If this is canceled with any reason that is convertible to <typeparamref name="TCancel"/>, <paramref name="onCanceled"/> will be invoked with that reason.
        /// <para/>If this is canceled for any other reason or no reason, the returned <see cref="IPotentialCancelation"/> will be canceled with the same reason.
        /// </summary>
        IPotentialCancelation CatchCancelation<TCancel>(Action<TCancel> onCanceled);
    }
}