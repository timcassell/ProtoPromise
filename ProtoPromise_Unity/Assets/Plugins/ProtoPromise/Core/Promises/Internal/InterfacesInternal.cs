#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System;
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

            internal virtual void Handle(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler) { throw new System.InvalidOperationException(); }
            // This is overridden in PromiseMultiAwait and PromiseProgress and PromiseConfigured.
            internal virtual void HandleFromContext() { throw new System.InvalidOperationException(); }
#if PROMISE_PROGRESS
            internal virtual PromiseRefBase SetProgress(ref PromiseRefBase.Fixed32 progress, ref ushort depth) { throw new System.InvalidOperationException(); }
            internal virtual void InvokeProgressFromContext() { throw new System.InvalidOperationException(); }
#endif
        }

        partial class PromiseRefBase
        {
            // For Merge/Race/First promises
            protected virtual void Handle(PromisePassThrough passThrough, out HandleablePromiseBase nextHandler) { throw new System.InvalidOperationException(); }
#if PROMISE_PROGRESS
            protected virtual PromiseRefBase IncrementProgress(long increment, ref Fixed32 progress, ushort depth) { throw new System.InvalidOperationException(); }
#endif

            internal interface IDelegateResolveOrCancel
            {
                void InvokeResolver(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner);
            }

            internal interface IDelegateResolveOrCancelPromise
            {
                void InvokeResolver(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner);
                bool IsNull { get; }
            }

            internal interface IDelegateReject
            {
                void InvokeRejecter(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner);
            }

            internal interface IDelegateRejectPromise
            {
                void InvokeRejecter(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner);
            }

            internal interface IDelegateContinue
            {
                void Invoke(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner);
            }

            internal interface IDelegateContinuePromise
            {
                void Invoke(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, PromiseRefBase owner);
                bool IsNull { get; }
            }
        }
    }
}