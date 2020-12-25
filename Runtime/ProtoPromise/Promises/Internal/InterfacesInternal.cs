using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            internal partial interface IMultiTreeHandleable : ITreeHandleable
            {
                bool Handle(IValueContainer valueContainer, PromisePassThrough passThrough, int index);
                void ReAdd(PromisePassThrough passThrough);
            }

            internal interface ICancelableDelegate
            {
                void SetCancelationRegistration(CancelationRegistration cancelationRegistration);
            }

            internal interface IDelegateResolve
            {
                void InvokeResolver(IValueContainer valueContainer, PromiseRef owner);
                void MaybeUnregisterCancelation();
            }

            internal interface IDelegateResolvePromise
            {
                void InvokeResolver(IValueContainer valueContainer, PromiseRef owner);
                void MaybeUnregisterCancelation();
                bool IsNull { get; }
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner);
                bool IsNull { get; }
            }

            internal interface IDelegateContinue
            {
                void Invoke(IValueContainer valueContainer, PromiseRef owner);
                void CancelCallback();
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(IValueContainer valueContainer, PromiseRef owner);
                void CancelCallback();
                bool IsNull { get; }
            }
        }
    }
}