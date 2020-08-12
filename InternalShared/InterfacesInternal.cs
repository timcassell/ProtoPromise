#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using Proto.Utils;

namespace Proto.Promises
{
    internal static partial class Internal
    {
        public interface ITraceable
        {
#if PROMISE_DEBUG
            CausalityTrace Trace { get; set; }
#endif
        }

        public interface IValueContainer
        {
            void Retain();
            void Release();
            Promise.State GetState();
            Type ValueType { get; }
            object Value { get; }

            void ReleaseAndAddToUnhandledStack();
            void ReleaseAndMaybeAddToUnhandledStack();
        }

        public interface ITreeHandleable : ILinked<ITreeHandleable>
        {
            void Handle();
            void MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue);
            void MakeReadyFromSettled(IValueContainer valueContainer);
        }

        public interface ITreeHandleableCollection
        {
            void Remove(ITreeHandleable treeHandleable);
        }

        public interface IRejectionToContainer
        {
            IRejectValueContainer ToContainer(ITraceable traceable);
        }

        public interface ICancelationToContainer
        {
            ICancelValueContainer ToContainer();
        }

        public interface ICantHandleException
        {
            void AddToUnhandledStack(ITraceable traceable);
        }

        public interface IThrowable
        {
            Exception GetException();
        }

        public interface IRejectValueContainer : IValueContainer, IThrowable
        {
#if PROMISE_DEBUG
            void SetCreatedAndRejectedStacktrace(System.Diagnostics.StackTrace rejectedStacktrace, CausalityTrace createdStacktraces);
#endif
        }

        public interface ICancelValueContainer : IValueContainer, IThrowable { }

        public interface ICancelDelegate
        {
            void Invoke(ICancelValueContainer valueContainer);
            void Dispose();
        }

        internal interface IDelegateCancel
        {
            void InvokeFromToken(IValueContainer valueContainer, ITreeHandleable owner);
            void SetValue(IValueContainer valueContainer);
            void InvokeFromPromise(ITraceable owner);
            void MaybeUnregisterCancelation();
        }
    }
}