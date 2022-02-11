namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            // Abstract class is used instead of interface, because virtual calls on interfaces are twice as slow as virtual calls on classes.
            internal abstract partial class MultiHandleablePromiseBase : PromiseSingleAwaitWithProgress
            {
                internal abstract void Handle(PromiseRef owner, ValueContainer valueContainer, PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler);
                internal override sealed void Handle(ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    throw new System.InvalidOperationException();
                }
            }

            internal interface IDelegateResolveOrCancel
            {
                void InvokeResolver(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void InvokeResolver(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateResolveOrCancelPromise
            {
                void InvokeResolver(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void InvokeResolver(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void InvokeRejecter(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void InvokeRejecter(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinue
            {
                void Invoke(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void Invoke(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void Invoke(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }
        }
    }
}