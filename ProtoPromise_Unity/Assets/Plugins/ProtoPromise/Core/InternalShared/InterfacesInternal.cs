using System;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    internal static partial class Internal
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

            internal abstract void Handle(ref ExecutionScheduler executionScheduler);
            internal abstract void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler);
        }

        internal abstract class ValueContainer
        {
            internal abstract void Retain();
            internal abstract void Release();
            internal abstract Promise.State GetState();
            internal abstract Type ValueType { get; }
            internal abstract object Value { get; }

            internal abstract void ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd);
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

        internal interface IThrowable
        {
            Exception GetException();
        }

        internal partial interface IRejectValueContainer : IThrowable { }

        internal interface IDelegateSimple
        {
            void Invoke();
        }
    }
}