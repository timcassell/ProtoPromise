namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            internal partial interface IMultiTreeHandleable : ITreeHandleable
            {
                bool Handle(PromiseRef owner, IValueContainer valueContainer, PromisePassThrough passThrough, int index);
            }

            internal interface IDelegate<TArg, TResult>
            {
                TResult Invoke(TArg arg);
                bool IsNull { get; }
            }

            internal interface IDelegateContinue
            {
                void Invoke(IValueContainer valueContainer, PromiseBranch owner, ref ValueLinkedStack<ITreeHandleable> executionStack);
                void Invoke(IValueContainer valueContainer, PromiseBranch owner, ref CancelationHelper cancelationHelper, ref ValueLinkedStack<ITreeHandleable> executionStack);
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(IValueContainer valueContainer, PromiseBranch owner, ref ValueLinkedStack<ITreeHandleable> executionStack);
                void Invoke(IValueContainer valueContainer, PromiseBranch owner, ref CancelationHelper cancelationHelper, ref ValueLinkedStack<ITreeHandleable> executionStack);
                bool IsNull { get; }
            }
        }
    }
}