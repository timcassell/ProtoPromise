#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

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
#if PROMISE_PROGRESS
            internal virtual bool TryReportProgress(PromiseRefBase reporter, double progress, int deferredId, ref PromiseRefBase.DeferredIdAndProgress idAndProgress)
            {
                // Do nothing, just return whether the id matches. This is overridden by actual progress listeners.
                return deferredId == idAndProgress._id;
            }
            internal virtual void MaybeReportProgress(ref ProgressReportValues progressReportValues)
            {
                // Just exit the lock and set to null to break the loop, do nothing else. This is overridden by actual progress listeners.
                Monitor.Exit(progressReportValues._lockedObject);
                progressReportValues._progressListener = null;
            }
            internal virtual void MaybeHookupProgressToAwaited(PromiseRefBase current, PromiseRefBase awaited,
                // We pass by reference so that we will know the values will be settled once the lock is entered on the progress listener.
                ref PromiseRefBase.ProgressRange userProgressRange, ref PromiseRefBase.ProgressRange listenerProgressRange)
            {
                // Do nothing. This is overridden by actual progress listeners.
            }
#endif
        }

        partial class PromiseRefBase
        {
            // These interfaces are only used in this manner because IDelegate<TArg, TResult> does not work with structs in old IL2CPP runtime.
            internal interface IDelegateResolveOrCancel
            {
                void InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner);
            }

            internal interface IDelegateResolveOrCancelPromise
            {
                void InvokeResolver(PromiseRefBase handler, Promise.State state, PromiseRefBase owner);
                bool IsNull { get; }
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

            internal interface IDelegateContinuePromise
            {
                void Invoke(PromiseRefBase handler, object rejectContainer, Promise.State state, PromiseRefBase owner);
                bool IsNull { get; }
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