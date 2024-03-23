#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;

namespace Proto.Promises
{
    /// <summary>
    /// A structured concurrency group used to race promises, incorporating their indices. Waits for the first promise to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseRaceWithIndexGroup
    {
        private readonly CancelationSource _cancelationSource;
        private readonly bool _cancelOnNonResolved;

        internal PromiseRaceWithIndexGroup(CancelationSource cancelationSource, bool cancelOnNonResolved)
        {
            _cancelationSource = cancelationSource;
            _cancelOnNonResolved = cancelOnNonResolved;
        }

        /// <summary>
        /// Get a new <see cref="PromiseRaceWithIndexGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceWithIndexGroup New(out CancelationToken groupCancelationToken, bool cancelOnNonResolved = true)
            => New(CancelationToken.None, out groupCancelationToken, cancelOnNonResolved);

        /// <summary>
        /// Get a new <see cref="PromiseRaceWithIndexGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled or any of the promises completed the group.</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceWithIndexGroup New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, bool cancelOnNonResolved = true)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseRaceWithIndexGroup Add(Promise promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If any promise is resolved, the returned promise will be resolved with the index of the promise that resolved first.
        /// If no promises are resolved and any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<int> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to race promises, incorporating their indices. Waits for the first promise to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseRaceWithIndexGroup<T>
    {
        private readonly CancelationSource _cancelationSource;
        private readonly bool _cancelOnNonResolved;

        internal PromiseRaceWithIndexGroup(CancelationSource cancelationSource, bool cancelOnNonResolved)
        {
            _cancelationSource = cancelationSource;
            _cancelOnNonResolved = cancelOnNonResolved;
        }

        /// <summary>
        /// Get a new <see cref="PromiseRaceWithIndexGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled when any of the promises completed the group.</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceWithIndexGroup<T> New(out CancelationToken groupCancelationToken, bool cancelOnNonResolved = true)
            => New(CancelationToken.None, out groupCancelationToken, cancelOnNonResolved);

        /// <summary>
        /// Get a new <see cref="PromiseRaceWithIndexGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled or any of the promises completed the group.</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceWithIndexGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, bool cancelOnNonResolved = true)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseRaceWithIndexGroup<T> Add(Promise<T> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If any promise is resolved, the returned promise will be resolved with the index and value of the promise that resolved first.
        /// If no promises are resolved and any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<(int winIndex, T result)> WaitAsync()
        {

        }
    }
}