#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0090 // Use 'new(...)'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
                private SynchronizationContext _callerContext;
                protected CancelationRegistration _cancelationRegistration;

                [MethodImpl(InlineOption)]
                protected void Reset(SynchronizationContext callerContext)
                {
                    Reset();
                    _callerContext = callerContext;
                    // Assume the resolved state will occur. If this is actually canceled or rejected, the state will be set at that time.
                    State = Promise.State.Resolved;
                }

                new protected void Dispose()
                {
                    base.Dispose();
                    _callerContext = null;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                protected void Continue()
                {
                    if (_callerContext == null)
                    {
                        // It was a synchronous lock or wait, handle next continuation synchronously so that the PromiseSynchronousWaiter will be pulsed to wake the waiting thread.
                        HandleNextInternal(State);
                        return;
                    }
                    // Post the continuation to the caller's context. This prevents blocking the current thread and avoids StackOverflowException.
                    ScheduleContextCallback(_callerContext, this,
                        obj => obj.UnsafeAs<AsyncSynchronizationPromiseBase<TResult>>().HandleFromContext(),
                        obj => obj.UnsafeAs<AsyncSynchronizationPromiseBase<TResult>>().HandleFromContext()
                    );
                }

                private void HandleFromContext()
                {
                    var currentContext = ts_currentContext;
                    ts_currentContext = _callerContext;

                    HandleNextInternal(State);

                    ts_currentContext = currentContext;
                }

                internal void MaybeHookupCancelation(CancelationToken cancelationToken)
                {
                    ThrowIfInPool(this);
                    cancelationToken.TryRegister(this, out _cancelationRegistration);
                }

                public abstract void Cancel();

                internal override sealed void Handle(PromiseRefBase handler, Promise.State state) { throw new System.InvalidOperationException(); }

#if PROMISE_DEBUG
                internal void Reject(IRejectContainer rejectContainer)
                {
                    _cancelationRegistration.Dispose();
                    _rejectContainer = rejectContainer;
                    State = Promise.State.Rejected;
                    Continue();
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
                Continue();
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