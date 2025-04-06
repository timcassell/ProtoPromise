using System;
using System.Runtime.ExceptionServices;

namespace Proto.Promises
{
    partial class Internal
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
            object Value { get; }
            Exception GetValueAsException();
            void ReportUnhandled();
            ExceptionDispatchInfo GetExceptionDispatchInfo();
        }
    }

    internal interface IAction
    {
        void Invoke();
    }

    internal interface IAction<TArg>
    {
        void Invoke(in TArg arg);
    }

    internal interface IFunc<TResult>
    {
        TResult Invoke();
    }

    internal interface IFunc<TArg, TResult>
    {
        TResult Invoke(in TArg arg);
    }

    internal interface IAction<TArg1, TArg2>
    {
        void Invoke(in TArg1 arg1, in TArg2 arg2);
    }

    internal interface IFunc<TArg1, TArg2, TResult>
    {
        TResult Invoke(in TArg1 arg1, in TArg2 arg2);
    }

    internal interface IFunc<TArg1, TArg2, TArg3, TResult>
    {
        TResult Invoke(in TArg1 arg1, in TArg2 arg2, in TArg3 arg3);
    }
}