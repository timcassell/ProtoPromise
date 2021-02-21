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

            internal interface IDelegateResolve
            {
                void InvokeResolver(IValueContainer valueContainer, PromiseRef owner);
                void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper);
            }

            internal interface IDelegateResolvePromise
            {
                void InvokeResolver(IValueContainer valueContainer, PromiseRef owner);
                void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper);
                bool IsNull { get; }
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner);
                void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner);
                void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper);
            }

            internal interface IDelegateContinue
            {
                void Invoke(IValueContainer valueContainer, PromiseRef owner);
                void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper);
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(IValueContainer valueContainer, PromiseRef owner);
                void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper);
                bool IsNull { get; }
            }

            internal interface IDelegateFinally
            {
                void Invoke(IValueContainer valueContainer, PromiseRef owner);
            }
        }
    }
}