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

    partial class Promise
    {
        partial class Internal
        {
            public interface ITreeHandleable : ILinked<ITreeHandleable>
            {
                void Handle();
                void Cancel();
            }

            public interface IValueContainerOrPrevious
            {
                bool TryGetValueAs<U>(out U value);
                bool ContainsType<U>();
                void Retain();
                void Release();
            }

            public interface IValueContainerContainer : IValueContainerOrPrevious
            {
                IValueContainerOrPrevious ValueContainerOrPrevious { get; }
            }

            public interface IDelegateResolve : IRetainable
            {
                void ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner);
            }

            public interface IDelegateResolve<T> : IRetainable
            {
                void ReleaseAndInvoke(Promise feed, PromiseResolveReject<T> owner);
            }

            public interface IDelegateResolvePromise : IRetainable
            {
                void ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner);
            }

            public interface IDelegateResolvePromise<T> : IRetainable
            {
                void ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<T> owner);
            }

            public interface IDelegateReject : IRetainable
            {
                void ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner);
            }

            public interface IDelegateReject<T> : IRetainable
            {
                void ReleaseAndInvoke(Promise feed, PromiseResolveReject<T> owner);
            }

            public interface IDelegateRejectPromise : IRetainable
            {
                void ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner);
            }

            public interface IDelegateRejectPromise<T> : IRetainable
            {
                void ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<T> owner);
            }

            public partial interface IMultiTreeHandleable : ITreeHandleable
            {
                void Handle(Promise feed, int index);
                void Cancel(IValueContainerOrPrevious cancelValue);
                void ReAdd(PromisePassThrough passThrough);
            }
        }
    }
}