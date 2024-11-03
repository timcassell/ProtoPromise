#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
                private SynchronizationContext _continuationContext;
                protected CancelationRegistration _cancelationRegistration;
                // We have to store the state in a separate field until the next awaiter is ready to be invoked on the proper context.
                protected Promise.State _tempState;

                [MethodImpl(InlineOption)]
                protected void Reset(bool continueOnCapturedContext)
                {
                    Reset();
                    _continuationContext = continueOnCapturedContext ? ContinuationOptions.CaptureContext() : null;
                    // Assume the resolved state will occur. If this is actually canceled or rejected, the state will be set at that time.
                    _tempState = Promise.State.Resolved;
                }

                new protected void Dispose()
                {
                    base.Dispose();
                    _continuationContext = null;
                    _cancelationRegistration = default;
                }

                protected void Continue()
                {
                    var context = _continuationContext;
                    if (context == null)
                    {
                        // This was configured to continue synchronously.
                        HandleNextInternal(_tempState);
                        return;
                    }
                    // This was configured to continuation on the context.
                    ScheduleContextCallback(context, this,
                        obj => obj.UnsafeAs<AsyncSynchronizationPromiseBase<TResult>>().HandleFromContext(),
                        obj => obj.UnsafeAs<AsyncSynchronizationPromiseBase<TResult>>().HandleFromContext()
                    );
                }

                [MethodImpl(InlineOption)]
                private void HandleFromContext()
                    => HandleNextInternal(_tempState);

                [MethodImpl(InlineOption)]
                internal bool HookupAndGetIsCanceled(CancelationToken cancelationToken)
                {
                    ThrowIfInPool(this);
                    // We register without immediate invoke because we hold a spin lock here, and we don't want to cause a deadlock from it trying to re-enter from the invoke.
                    cancelationToken.TryRegisterWithoutImmediateInvoke<ICancelable>(this, out _cancelationRegistration, out var alreadyCanceled);
                    return alreadyCanceled;
                }

                public abstract void Cancel();

                internal override sealed void Handle(PromiseRefBase handler, Promise.State state) => throw new System.InvalidOperationException();

                internal void Reject(IRejectContainer rejectContainer)
                {
                    _cancelationRegistration.Dispose();
                    _rejectContainer = rejectContainer;
                    _tempState = Promise.State.Rejected;
                    Continue();
                }

                internal void CancelDirect()
                {
                    _cancelationRegistration.Dispose();
                    _tempState = Promise.State.Canceled;
                    Continue();
                }
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
                get => _nextEventPromise;
                set => _nextEventPromise = value;
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
                get => _ownerReference.Target as TOwner;
                set => _ownerReference.Target = value;
            }
#else
            protected TOwner _owner;
#endif
        }
    }
}