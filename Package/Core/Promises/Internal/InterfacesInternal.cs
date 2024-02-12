#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;
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
        [DebuggerNonUserCode, StackTraceHidden]
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

            internal virtual void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state) { throw new System.InvalidOperationException(); }
            // For Merge/Race promises
            internal virtual void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state, int index) { throw new System.InvalidOperationException(); }
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
                void InvokeRejecter(object rejectContainer, PromiseRefBase owner);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(PromiseRefBase handler, object rejectContainer, PromiseRefBase owner);
            }

            internal interface IDelegateContinue
            {
                void Invoke(PromiseRefBase handler, object rejectContainer, Promise.State state, PromiseRefBase owner);
            }

            internal interface IDelegateContinuePromise : INullable
            {
                void Invoke(PromiseRefBase handler, object rejectContainer, Promise.State state, PromiseRefBase owner);
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