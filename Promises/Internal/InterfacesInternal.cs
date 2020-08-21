namespace Proto.Promises
{
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
                bool IsNull { get; }
            }

            internal interface IDelegateContinue
            {
                void Invoke(Internal.IValueContainer valueContainer, Promise owner);
                void CancelCallback();
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(Internal.IValueContainer valueContainer, Promise owner);
                void CancelCallback();
                bool IsNull { get; }
            }
        }
    }
}