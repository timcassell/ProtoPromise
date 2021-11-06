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

            internal interface IDelegate<TArg, TResult>
            {
                TResult Invoke(TArg arg);
                bool IsNull { get; }
            }

            internal interface IDelegateContinue
            {
                void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }
        }
    }
}