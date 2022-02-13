using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        // Abstract classes are used instead of interfaces, because virtual calls on interfaces are twice as slow as virtual calls on classes.
        internal abstract partial class HandleablePromiseBase : ILinked<HandleablePromiseBase>
        {
            HandleablePromiseBase ILinked<HandleablePromiseBase>.Next
            {
                [MethodImpl(InlineOption)]
                get { return _next; }
                [MethodImpl(InlineOption)]
                set { _next = value; }
            }

            internal abstract void Handle(ref PromiseRef handler, ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler);
            // This is overridden in PromiseMultiAwait and PromiseProgress and PromiseConfigured.
            internal virtual void Handle(ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
        }

        partial class PromiseRef
        {
            internal abstract partial class MultiHandleablePromiseBase : PromiseSingleAwaitWithProgress
            {
                internal abstract void Handle(PromiseRef owner, ValueContainer valueContainer, PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler);
                internal override void Handle(ref PromiseRef handler, ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    throw new System.InvalidOperationException();
                }
            }

            internal interface IDelegateResolveOrCancel
            {
                void InvokeResolver(ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void InvokeResolver(ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateResolveOrCancelPromise
            {
                void InvokeResolver(ref PromiseRef handler, ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void InvokeResolver(ref PromiseRef handler, ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void InvokeRejecter(ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(ref PromiseRef handler, ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void InvokeRejecter(ref PromiseRef handler, ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinue
            {
                void Invoke(ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
                void Invoke(ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(ref PromiseRef handler, ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                void Invoke(ref PromiseRef handler, ref ValueContainer valueContainer, ref Promise.State state, out HandleablePromiseBase nextHandler, PromiseWaitPromise owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }
        }
    }
}