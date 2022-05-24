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

#pragma warning disable IDE0090 // Use 'new(...)'

using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        internal abstract partial class PromiseRefBase : HandleablePromiseBase, ITraceable
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseCompletionSentinel : HandleablePromiseBase
            {
                // A singleton instance used to mark the promise as completed.
                internal static readonly PromiseCompletionSentinel s_instance = new PromiseCompletionSentinel();

                private PromiseCompletionSentinel() { }

                internal override void Handle(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler)
                {
                    throw new System.InvalidOperationException("PromiseCompletionSentinel handled from " + handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseForgetSentinel : HandleablePromiseBase
            {
                // A singleton instance used to cap off the promise and prevent further awaits.
                internal static readonly PromiseForgetSentinel s_instance = new PromiseForgetSentinel();

                private PromiseForgetSentinel() { }

                internal override void Handle(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler)
                {
                    nextHandler = null;
                    handler.MaybeDispose();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class InvalidAwaitSentinel : PromiseRefBase
            {
                // A singleton instance used to indicate that an await was invalid (after the PromiseMultiAwait was forgotten or PromiseSingleAwait awaited).
                internal static readonly InvalidAwaitSentinel s_instance = new InvalidAwaitSentinel();

                private InvalidAwaitSentinel()
                {
                    _next = this; // Set _waiter to this so that CompareExchangeWaiter will always fail.
                    _smallFields = new SmallFields(-5); // Set an id that is unlikely to match (though this should never be used in a Promise struct).
                }

                internal override void Handle(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler)
                {
                    throw new System.InvalidOperationException("InvalidAwaitSentinel handled from " + handler);
                }

                protected override void MaybeDispose() { throw new System.InvalidOperationException(); }
                protected override void OnForget(short promiseId) { throw new System.InvalidOperationException(); }
                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter) { throw new System.InvalidOperationException(); }
                internal override PromiseRefBase GetConfigured(short promiseId, SynchronizationContext synchronizationContext, ushort depth) { throw new System.InvalidOperationException(); }
                internal override PromiseRefBase GetDuplicate(short promiseId, ushort depth) { throw new System.InvalidOperationException(); }
                internal override bool GetIsCompleted(short promiseId) { throw new System.InvalidOperationException(); }
                internal override bool GetIsValid(short promiseId) { throw new System.InvalidOperationException(); }
                internal override PromiseRefBase GetPreserved(short promiseId, ushort depth) { throw new System.InvalidOperationException(); }
                internal override void MaybeMarkAwaitedAndDispose(short promiseId) { throw new System.InvalidOperationException(); }
            }

            internal sealed partial class ResolvedSentinel : PromiseRefBase
            {
                internal static readonly ResolvedSentinel s_instance = new ResolvedSentinel();

                private ResolvedSentinel()
                {
                    State = Promise.State.Resolved;
                    _next = InvalidAwaitSentinel.s_instance;
                    _rejectContainer = RejectContainer.s_completionSentinel;
                    _smallFields._promiseId = ValidIdFromApi;
                }

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    previousWaiter = PromiseCompletionSentinel.s_instance;
                    return null;
                }

                internal override bool GetIsValid(short promiseId)
                {
                    return promiseId == ValidIdFromApi;
                }

                internal override bool GetIsCompleted(short promiseId)
                {
                    return true;
                }

                internal override PromiseRefBase GetDuplicate(short promiseId, ushort depth)
                {
                    return this;
                }

                internal override PromiseRefBase GetPreserved(short promiseId, ushort depth)
                {
                    return this;
                }

                protected override void OnForget(short promiseId) { }
                internal override void MaybeMarkAwaitedAndDispose(short promiseId) { }
                protected override void MaybeDispose() { }

                internal override void Handle(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler) { throw new System.InvalidOperationException(); }
                internal override PromiseRefBase GetConfigured(short promiseId, SynchronizationContext synchronizationContext, ushort depth) { throw new System.InvalidOperationException(); }
            }
        } // PromiseRefBase

        internal static PromiseRefBase.ResolvedSentinel PromiseResolvedSentinel
        {
            [MethodImpl(InlineOption)]
            get
            {
                return PromiseRefBase.ResolvedSentinel.s_instance;
            }
        }

        internal class BackgroundSynchronizationContextSentinel : SynchronizationContext
        {
            internal static readonly BackgroundSynchronizationContextSentinel s_instance = new BackgroundSynchronizationContextSentinel();

            public override void Post(SendOrPostCallback d, object state) { throw new System.InvalidOperationException(); }
            public override void Send(SendOrPostCallback d, object state) { throw new System.InvalidOperationException(); }
            public override SynchronizationContext CreateCopy() { throw new System.InvalidOperationException(); }
        }
    } // Internal
}