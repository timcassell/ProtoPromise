namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            // Abstract classes is used instead of interface, because virtual calls on interfaces are twice as slow as virtual calls on classes.
            internal abstract partial class MultiHandleablePromiseBase : PromiseSingleAwaitWithProgress
            {
                internal abstract void Handle(PromiseRef owner, ValueContainer valueContainer, PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler);
            }

            partial class PromiseSingleAwait
            {
                internal interface IDelegateHandle
                {
                    void InvokeAndHandle(ValueContainer valueContainer, ref ExecutionScheduler executionScheduler);
                    void InvokeAndHandle(ValueContainer valueContainer, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
                }
            }

            internal interface IDelegateResolveOrCancel
            {
                void InvokeResolver(ValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void InvokeResolver(ValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateResolveOrCancelPromise
            {
                void InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void InvokeResolver(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(ValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void InvokeRejecter(ValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void InvokeRejecter(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinue
            {
                void Invoke(ValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void Invoke(ValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(ValueContainer valueContainer, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void Invoke(ValueContainer valueContainer, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }
        }
    }
}