#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

using System;
using Proto.Utils;

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

    partial class Promise
    {
        partial class InternalProtected
        {
            internal partial interface IMultiTreeHandleable : Internal.ITreeHandleable
            {
                bool Handle(Internal.IValueContainer valueContainer, Promise owner, int index);
                void ReAdd(PromisePassThrough passThrough);
            }

            internal interface IDelegateResolve
            {
                void InvokeResolver(Internal.IValueContainer valueContainer, Promise owner);
                void MaybeUnregisterCancelation();
                bool IsNull { get; }
            }

            internal interface IDelegateResolvePromise
            {
                void InvokeResolver(Internal.IValueContainer valueContainer, Promise owner);
                void MaybeUnregisterCancelation();
                bool IsNull { get; }
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(Internal.IValueContainer valueContainer, Promise owner);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(Internal.IValueContainer valueContainer, Promise owner);
            }

            internal interface IDelegateContinue
            {
                void Invoke(Internal.IValueContainer valueContainer);
                bool IsNull { get; }
            }

            internal interface IDelegateContinue<T>
            {
                T Invoke(Internal.IValueContainer valueContainer);
                bool IsNull { get; }
            }
        }
    }
}