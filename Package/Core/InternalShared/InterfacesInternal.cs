#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

using System.Runtime.ExceptionServices;

namespace Proto.Promises
{
    internal static partial class Internal
    {
        internal partial interface ITraceable { }

        internal interface ILinked<T> where T : class, ILinked<T>
        {
            T Next { get; set; }
        }

        internal interface IRejectionToContainer
        {
            IRejectContainer ToContainer(ITraceable traceable);
        }

        internal interface ICantHandleException
        {
            void ReportUnhandled(ITraceable traceable);
        }

        internal interface IRejectContainer
        {
            void ReportUnhandled();
            ExceptionDispatchInfo GetExceptionDispatchInfo();
            object Value { get; }
        }

        internal interface IAction
        {
            void Invoke();
        }

        internal interface IAction<TArg>
        {
            void Invoke(TArg arg);
        }

        internal interface IFunc<TResult>
        {
            TResult Invoke();
        }

        internal interface IFunc<TArg, TResult>
        {
            TResult Invoke(TArg arg);
        }

        internal interface IAction<TArg1, TArg2>
        {
            void Invoke(TArg1 arg1, TArg2 arg2);
        }

        internal interface IFunc<TArg1, TArg2, TResult>
        {
            TResult Invoke(TArg1 arg1, TArg2 arg2);
        }
    }
}