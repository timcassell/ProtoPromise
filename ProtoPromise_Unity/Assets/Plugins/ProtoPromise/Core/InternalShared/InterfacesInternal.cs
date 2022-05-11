#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

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

        internal interface IProgressInvokable : ILinked<IProgressInvokable>
        {
            void Invoke(ref ExecutionScheduler executionScheduler);
        }

        internal interface IRejectionToContainer
        {
            RejectContainer ToContainer(ITraceable traceable);
        }

        internal interface ICantHandleException
        {
            void AddToUnhandledStack(ITraceable traceable);
        }

        internal partial interface IRejectValueContainer
        {
#if !NET_LEGACY
            System.Runtime.ExceptionServices.ExceptionDispatchInfo GetExceptionDispatchInfo();
#else
            Exception GetException();
#endif
        }

        internal interface IDelegateSimple
        {
            void Invoke();
        }
    }
}