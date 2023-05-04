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
#pragma warning disable IDE0090 // Use 'new(...)'

using System;
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
            internal abstract class AsyncSynchronizationPromiseBase<TResult> : PromiseSingleAwait<TResult>, ICancelable
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

                internal void MaybeHookupCancelation(CancelationToken cancelationToken)
                {
                    ThrowIfInPool(this);
                    cancelationToken.TryRegister(this, out _cancelationRegistration);
                }

                public abstract void Cancel();

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

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class AsyncEventPromiseBase : PromiseRefBase.AsyncSynchronizationPromiseBase<bool>, ILinked<AsyncEventPromiseBase>
        {
            protected AsyncEventPromiseBase _nextEventPromise;
            AsyncEventPromiseBase ILinked<AsyncEventPromiseBase>.Next
            {
                get { return _nextEventPromise; }
                set { _nextEventPromise = value; }
            }

            internal void Resolve()
            {
                ThrowIfInPool(this);

                // We don't need to check if the unregister was successful or not.
                // The fact that this was called means the cancelation was unable to unregister this from the owner.
                // We just dispose to wait for the callback to complete before we continue.
                _cancelationRegistration.Dispose();

                _result = true;
                Continue(Promise.State.Resolved);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class AsyncEventPromise<TOwner> : AsyncEventPromiseBase
            where TOwner : class
        {
#if PROMISE_DEBUG
            // We use a weak reference in DEBUG mode so the owner's finalizer can still run if it's dropped.
            private readonly WeakReference _ownerReference = new WeakReference(null, false);
#pragma warning disable IDE1006 // Naming Styles
            protected TOwner _owner
#pragma warning restore IDE1006 // Naming Styles
            {
                get { return _ownerReference.Target as TOwner; }
                set { _ownerReference.Target = value; }
            }
#else
            protected TOwner _owner;
#endif
        }
    }
}