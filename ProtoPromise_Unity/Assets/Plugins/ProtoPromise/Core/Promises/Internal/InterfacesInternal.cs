namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            internal partial interface IMultiTreeHandleable : ITreeHandleable
            {
                void Handle(PromiseRef owner, IValueContainer valueContainer, PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateResolve
            {
                void InvokeResolver(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void InvokeResolver(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateResolvePromise
            {
                void InvokeResolver(IValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void InvokeResolver(IValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void InvokeRejecter(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(IValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void InvokeRejecter(IValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinue
            {
                void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(IValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void Invoke(IValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }
        }
    }
}