#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        // Abstract classes are used instead of interfaces, because virtual calls on interfaces are twice as slow as virtual calls on classes.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract partial class HandleablePromiseBase : ILinked<HandleablePromiseBase>
        {
            HandleablePromiseBase ILinked<HandleablePromiseBase>.Next
            {
                [MethodImpl(InlineOption)]
                get => _next;
                [MethodImpl(InlineOption)]
                set => _next = value;
            }

            internal virtual void Handle(PromiseRefBase handler, Promise.State state) { throw new System.InvalidOperationException(); }
            // For PromisePassThrough
            internal virtual void Handle(PromiseRefBase handler, Promise.State state, int index) { throw new System.InvalidOperationException(); }
            internal virtual void Handle(PromiseRefBase.PromisePassThroughForMergeGroup passthrough, PromiseRefBase handler, Promise.State state) { throw new System.InvalidOperationException(); }
        }

        partial class PromiseRefBase
        {
            // These interfaces are only used in this manner because IDelegate<TArg, TResult> does not work with structs in old IL2CPP runtime.
            internal interface IDelegateResolveOrCancel
            {
                void InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner);
            }

            internal interface IDelegateResolveOrCancelPromise : INullable
            {
                void InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner);
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(IRejectContainer rejectContainer, PromiseRefBase owner);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(PromiseRefBase handler, IRejectContainer rejectContainer, PromiseRefBase owner);
            }

            internal interface IDelegateContinue
            {
                void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner);
            }

            internal interface IDelegateContinuePromise : INullable
            {
                void Invoke(PromiseRefBase handler, IRejectContainer rejectContainer, Promise.State state, PromiseRefBase owner);
            }

            internal interface IDelegateNew<TResult>
            {
                void Invoke(DeferredPromise<TResult> owner);
            }

            internal interface IDelegateRun
            {
                void Invoke(PromiseRefBase owner);
            }

            internal interface IDelegateRunPromise
            {
                void Invoke(PromiseRefBase owner);
            }
        }
    }
}