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
                void MaybeUnregisterCancelation();
                bool IsNull { get; }
            }

            internal interface IDelegateContinue<T>
            {
                T Invoke(Internal.IValueContainer valueContainer);
                void MaybeUnregisterCancelation();
                bool IsNull { get; }
            }
        }
    }
}