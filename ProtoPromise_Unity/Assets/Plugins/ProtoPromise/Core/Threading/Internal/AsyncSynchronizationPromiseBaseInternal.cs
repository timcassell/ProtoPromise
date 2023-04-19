#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using System.Diagnostics;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract class AsyncSynchronizationPromiseBase<TResult> : PromiseSingleAwait<TResult>
            {
                // We post continuations to the caller's context to prevent blocking the thread that released the lock (and to avoid StackOverflowException).
                protected SynchronizationContext _callerContext;
                protected CancelationRegistration _cancelationRegistration;
                // We have to store the state in a separate field until the next awaiter is ready to be invoked on the proper context.
                private Promise.State _tempState;

                new protected void Dispose()
                {
                    base.Dispose();
                    _callerContext = null;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                protected void Continue(Promise.State state)
                {
                    if (_callerContext == null)
                    {
                        // It was a synchronous lock or wait, handle next continuation synchronously so that the PromiseSynchronousWaiter will be pulsed to wake the waiting thread.
                        HandleNextInternal(null, state);
                        return;
                    }
                    // Post the continuation to the caller's context. This prevents blocking the current thread and avoids StackOverflowException.
                    _tempState = state;
                    ScheduleForHandle(this, _callerContext);
                }

                internal override sealed void HandleFromContext()
                {
                    HandleNextInternal(null, _tempState);
                }

                internal override sealed void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state) { throw new System.InvalidOperationException(); }

#if PROMISE_DEBUG
                internal void Reject(IRejectContainer rejectContainer)
                {
                    Promise.Run(() =>
                    {
                        _cancelationRegistration.Dispose();
                        HandleNextInternal(rejectContainer, Promise.State.Rejected);
                    }, _callerContext, forceAsync: true)
                        .Forget();
                }
#endif
            }
        }
    }
}