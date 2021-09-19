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

            void ReleaseAndAddToUnhandledStack();
            void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd);
        }

        internal interface ITreeHandleable : ILinked<ITreeHandleable>
        {
            void Handle(ref ValueLinkedStack<ITreeHandleable> executionStack);
            void MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack);
            void MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack);
        }

        internal interface IRejectionToContainer
        {
            IRejectValueContainer ToContainer(ITraceable traceable);
        }

        internal interface ICancelationToContainer
        {
            ICancelValueContainer ToContainer();
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

        internal interface ICancelDelegate
        {
            void Invoke(ICancelValueContainer valueContainer);
            void Dispose();
        }

        internal interface IDelegateSimple
        {
            void Invoke(IValueContainer valueContainer);
        }
    }
}