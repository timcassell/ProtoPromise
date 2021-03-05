namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            internal partial interface IMultiTreeHandleable : ITreeHandleable
            {
                bool Handle(IValueContainer valueContainer, PromisePassThrough passThrough, int index);
            }

            internal interface IDelegateResolve
            {
                void InvokeResolver(IValueContainer valueContainer, PromiseBranch owner);
                void InvokeResolver(IValueContainer valueContainer, PromiseBranch owner, ref CancelationHelper cancelationHelper);
            }

            internal interface IDelegateResolvePromise
            {
                void InvokeResolver(IValueContainer valueContainer, PromiseBranch owner);
                void InvokeResolver(IValueContainer valueContainer, PromiseBranch owner, ref CancelationHelper cancelationHelper);
                bool IsNull { get; }
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(IValueContainer valueContainer, PromiseBranch owner);
                void InvokeRejecter(IValueContainer valueContainer, PromiseBranch owner, ref CancelationHelper cancelationHelper);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(IValueContainer valueContainer, PromiseBranch owner);
                void InvokeRejecter(IValueContainer valueContainer, PromiseBranch owner, ref CancelationHelper cancelationHelper);
            }

            internal interface IDelegateContinue
            {
                void Invoke(IValueContainer valueContainer, PromiseBranch owner);
                void Invoke(IValueContainer valueContainer, PromiseBranch owner, ref CancelationHelper cancelationHelper);
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(IValueContainer valueContainer, PromiseBranch owner);
                void Invoke(IValueContainer valueContainer, PromiseBranch owner, ref CancelationHelper cancelationHelper);
                bool IsNull { get; }
            }
        }
    }
}