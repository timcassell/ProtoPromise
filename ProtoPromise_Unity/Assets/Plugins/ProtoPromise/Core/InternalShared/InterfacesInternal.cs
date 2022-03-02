using System;

namespace Proto.Promises
{
    internal static partial class Internal
    {
        // Abstract class is used instead of interface, because virtual calls on interfaces are twice as slow as virtual calls on classes.
        internal abstract class ValueContainer
        {
            internal abstract void Retain();
            internal abstract void Release();
            internal abstract Promise.State GetState();
            internal abstract Type ValueType { get; }
            internal abstract object Value { get; }

            internal abstract void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd);
            internal abstract void AddToUnhandledStack();
        }

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
            ValueContainer ToContainer(ITraceable traceable);
        }

        internal interface ICantHandleException
        {
            void AddToUnhandledStack(ITraceable traceable);
        }

        internal partial interface IRejectValueContainer
        {
#if CSHARP_7_3_OR_NEWER
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