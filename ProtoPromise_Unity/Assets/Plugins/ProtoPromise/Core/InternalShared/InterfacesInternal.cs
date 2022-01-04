using System;

namespace Proto.Promises
{
    internal static partial class Internal
    {
        internal partial interface ITraceable { }

        internal interface ILinked<T> where T : class, ILinked<T>
        {
            T Next { get; set; }
        }

        internal interface IValueContainer<T>
        {
            T Value { get; }
        }

        internal interface IValueContainer
        {
            void Retain();
            void Release();
            Promise.State GetState();
            Type ValueType { get; }
            object Value { get; }

            void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd);
        }

        internal interface ITreeHandleable : ILinked<ITreeHandleable>
        {
            void Handle(ref ExecutionScheduler executionScheduler);
            void MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler);
        }

        internal interface IProgressInvokable : ILinked<IProgressInvokable>
        {
            void Invoke(ref ExecutionScheduler executionScheduler);
        }

        internal interface IRejectionToContainer
        {
            IRejectValueContainer ToContainer(ITraceable traceable);
        }

        internal interface ICantHandleException
        {
            void AddToUnhandledStack(ITraceable traceable);
        }

        internal interface IThrowable
        {
            Exception GetException();
        }

        internal partial interface IRejectValueContainer : IValueContainer, IThrowable { }

        internal interface ICancelValueContainer : IValueContainer, IThrowable { }

        internal interface IDelegateSimple
        {
            void Invoke();
        }
    }
}