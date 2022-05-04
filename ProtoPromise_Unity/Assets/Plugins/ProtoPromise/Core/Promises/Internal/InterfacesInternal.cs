#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System.Runtime.CompilerServices;

namespace Proto.Promises
{
#if NET_LEGACY // IProgress<T> is included in .Net 4.5 and later.
    internal interface IProgress<T>
    {
        void Report(T value);
    }
#endif

    partial class Internal
    {
        // Abstract classes are used instead of interfaces, because virtual calls on interfaces are twice as slow as virtual calls on classes.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal abstract partial class HandleablePromiseBase : ILinked<HandleablePromiseBase>
        {
            HandleablePromiseBase ILinked<HandleablePromiseBase>.Next
            {
                [MethodImpl(InlineOption)]
                get { return _next; }
                [MethodImpl(InlineOption)]
                set { _next = value; }
            }

            internal abstract void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler);
            // This is overridden in PromiseMultiAwait and PromiseProgress and PromiseConfigured.
            internal virtual void Handle(ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
#if PROMISE_PROGRESS
            internal abstract PromiseRef.PromiseSingleAwait SetProgress(ref PromiseRef.Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler);
#endif
        }

        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class MultiHandleablePromiseBase : PromiseSingleAwait
            {
                internal abstract void Handle(PromisePassThrough passThrough, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler);
                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
            }

            internal interface IDelegateResolveOrCancel
            {
                void InvokeResolver(ref PromiseRef handler, out HandleablePromiseBase nextHandler, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateResolveOrCancelPromise
            {
                void InvokeResolver(ref PromiseRef handler, out HandleablePromiseBase nextHandler, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(ref PromiseRef handler, out HandleablePromiseBase nextHandler, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(ref PromiseRef handler, out HandleablePromiseBase nextHandler, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinue
            {
                void Invoke(ref PromiseRef handler, out HandleablePromiseBase nextHandler, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler);
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(ref PromiseRef handler, out HandleablePromiseBase nextHandler, PromiseWaitPromise owner, ref ExecutionScheduler executionScheduler);
                bool IsNull { get; }
            }
        }
    }
}