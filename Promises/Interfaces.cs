#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

using System;
using Proto.Utils;

namespace Proto.Promises
{
    public interface ICancelable
    {
        /// <summary>
        /// Cancel this instance without a reason.
        /// </summary>
        void Cancel();
    }

    public interface ICancelableAny : ICancelable
    {
        /// <summary>
        /// Cancel this instance with <paramref name="reason"/>.
        /// </summary>
        void Cancel<TCancel>(TCancel reason);
    }

    public interface IRetainable
    {
        /// <summary>
        /// Retain this instance.
        /// <para/>This should always be paired with a call to <see cref="Release"/>
        /// </summary>
        void Retain();
        /// <summary>
        /// Release this instance.
        /// <para/>This should always be paired with a call to <see cref="Retain"/>
        /// </summary>
        void Release();
    }

    partial class Promise
    {
        partial class Internal
        {
            public interface ITreeHandleable : ILinked<ITreeHandleable>
            {
                void Handle();
                void Cancel();
                void MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue, ref ValueLinkedQueue<ITreeHandleable> cancelQueue);
                void MakeReadyFromSettled(IValueContainer valueContainer);
            }

            public interface IValueContainer
            {
                void Retain();
                void Release();
                State GetState();
                Type ValueType { get; }
                object Value { get; }

                void ReleaseAndAddToUnhandledStack();
                void ReleaseAndMaybeAddToUnhandledStack();
            }

            public interface IExceptionToContainer
            {
                IValueContainer ToContainer(ITraceable traceable);
            }

            public interface ICantHandleException
            {
                void AddToUnhandledStack(ITraceable traceable);
            }

            public interface IRejectionContainer : IValueContainer
            {
#if PROMISE_DEBUG
                void SetCreatedAndRejectedStacktrace(System.Diagnostics.StackTrace rejectedStacktrace, CausalityTrace createdStacktraces);
#endif
            }

            public interface IThrowable
            {
                Exception GetException();
            }

            public interface IDelegateResolve : IDisposable
            {
                void DisposeAndInvoke(IValueContainer valueContainer, Promise owner);
            }
            public interface IDelegateResolvePromise : IDisposable
            {
                void DisposeAndInvoke(IValueContainer valueContainer, Promise owner);
            }

            public interface IDelegateReject : IDisposable
            {
                void DisposeAndInvoke(IValueContainer valueContainer, Promise owner);
            }

            public interface IDelegateRejectPromise : IDisposable
            {
                void DisposeAndInvoke(IValueContainer valueContainer, Promise owner);
            }

            public interface IDelegateContinue
            {
                void DisposeAndInvoke(IValueContainer valueContainer);
            }

            public interface IDelegateContinue<T>
            {
                T DisposeAndInvoke(IValueContainer valueContainer);
            }

            public partial interface IMultiTreeHandleable : ITreeHandleable
            {
                bool Handle(IValueContainer valueContainer, Promise owner, int index);
                void ReAdd(PromisePassThrough passThrough);
            }
        }
    }
}