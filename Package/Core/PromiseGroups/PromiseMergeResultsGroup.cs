#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;

namespace Proto.Promises
{
    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// If/when any of the promises are rejected or canceled, the group will be canceled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseMergeResultsGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Get a new <see cref="PromiseMergeResultsGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        public static PromiseMergeResultsGroup New(out CancelationToken groupCancelationToken)
            => New(CancelationToken.None, out groupCancelationToken);

        /// <summary>
        /// Get a new <see cref="PromiseMergeResultsGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        public static PromiseMergeResultsGroup New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T> Add<T>(Promise<T> promise)
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// If/when any of the promises are rejected or canceled, the group will be canceled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1>
    {
        private readonly CancelationSource _cancelationSource;
        private readonly Promise<T1> _promise1;

        internal PromiseMergeResultsGroup(CancelationSource cancelationSource,
            Promise<T1> promise1)
        {
            _cancelationSource = cancelationSource;
            _promise1 = promise1;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2> Add<T2>(Promise<T2> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for the promise in this group to complete and yields its result.
        /// </summary>
        public Promise<Promise<T1>.ResultContainer> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// If/when any of the promises are rejected or canceled, the group will be canceled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2>
    {
        private readonly CancelationSource _cancelationSource;
        private readonly Promise<T1> _promise1;
        private readonly Promise<T2> _promise2;

        internal PromiseMergeResultsGroup(CancelationSource cancelationSource,
            Promise<T1> promise1,
            Promise<T2> promise2)
        {
            _cancelationSource = cancelationSource;
            _promise1 = promise1;
            _promise2 = promise2;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3> Add<T3>(Promise<T3> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer)> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// If/when any of the promises are rejected or canceled, the group will be canceled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3>
    {
        private readonly CancelationSource _cancelationSource;
        private readonly Promise<T1> _promise1;
        private readonly Promise<T2> _promise2;
        private readonly Promise<T3> _promise3;

        internal PromiseMergeResultsGroup(CancelationSource cancelationSource,
            Promise<T1> promise1,
            Promise<T2> promise2,
            Promise<T3> promise3)
        {
            _cancelationSource = cancelationSource;
            _promise1 = promise1;
            _promise2 = promise2;
            _promise3 = promise3;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4> Add<T4>(Promise<T4> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// If/when any of the promises are rejected or canceled, the group will be canceled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4>
    {
        private readonly CancelationSource _cancelationSource;
        private readonly Promise<T1> _promise1;
        private readonly Promise<T2> _promise2;
        private readonly Promise<T3> _promise3;
        private readonly Promise<T4> _promise4;

        internal PromiseMergeResultsGroup(CancelationSource cancelationSource,
            Promise<T1> promise1,
            Promise<T2> promise2,
            Promise<T3> promise3,
            Promise<T4> promise4)
        {
            _cancelationSource = cancelationSource;
            _promise1 = promise1;
            _promise2 = promise2;
            _promise3 = promise3;
            _promise4 = promise4;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, T5> Add<T5>(Promise<T5> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// If/when any of the promises are rejected or canceled, the group will be canceled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4, T5>
    {
        private readonly CancelationSource _cancelationSource;
        private readonly Promise<T1> _promise1;
        private readonly Promise<T2> _promise2;
        private readonly Promise<T3> _promise3;
        private readonly Promise<T4> _promise4;
        private readonly Promise<T5> _promise5;

        internal PromiseMergeResultsGroup(CancelationSource cancelationSource,
            Promise<T1> promise1,
            Promise<T2> promise2,
            Promise<T3> promise3,
            Promise<T4> promise4,
            Promise<T5> promise5)
        {
            _cancelationSource = cancelationSource;
            _promise1 = promise1;
            _promise2 = promise2;
            _promise3 = promise3;
            _promise4 = promise4;
            _promise5 = promise5;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6> Add<T6>(Promise<T6> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// If/when any of the promises are rejected or canceled, the group will be canceled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6>
    {
        private readonly CancelationSource _cancelationSource;
        private readonly Promise<T1> _promise1;
        private readonly Promise<T2> _promise2;
        private readonly Promise<T3> _promise3;
        private readonly Promise<T4> _promise4;
        private readonly Promise<T5> _promise5;
        private readonly Promise<T6> _promise6;

        internal PromiseMergeResultsGroup(CancelationSource cancelationSource,
            Promise<T1> promise1,
            Promise<T2> promise2,
            Promise<T3> promise3,
            Promise<T4> promise4,
            Promise<T5> promise5,
            Promise<T6> promise6)
        {
            _cancelationSource = cancelationSource;
            _promise1 = promise1;
            _promise2 = promise2;
            _promise3 = promise3;
            _promise4 = promise4;
            _promise5 = promise5;
            _promise6 = promise6;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6, T7> Add<T7>(Promise<T7> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)>
            WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// If/when any of the promises are rejected or canceled, the group will be canceled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6, T7>
    {
        private readonly CancelationSource _cancelationSource;
        private readonly Promise<T1> _promise1;
        private readonly Promise<T2> _promise2;
        private readonly Promise<T3> _promise3;
        private readonly Promise<T4> _promise4;
        private readonly Promise<T5> _promise5;
        private readonly Promise<T6> _promise6;
        private readonly Promise<T7> _promise7;

        internal PromiseMergeResultsGroup(CancelationSource cancelationSource,
            Promise<T1> promise1,
            Promise<T2> promise2,
            Promise<T3> promise3,
            Promise<T4> promise4,
            Promise<T5> promise5,
            Promise<T6> promise6,
            Promise<T7> promise7)
        {
            _cancelationSource = cancelationSource;
            _promise1 = promise1;
            _promise2 = promise2;
            _promise3 = promise3;
            _promise4 = promise4;
            _promise5 = promise5;
            _promise6 = promise6;
            _promise7 = promise7;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        // Merging more than 7 types should be fairly rare. To support N types greater than 7, we just wrap it in another group.
        public PromiseMergeResultsGroup<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer), T8>
            Add<T8>(Promise<T8> promise)
            => new PromiseMergeResultsGroup(_cancelationSource)
            .Add(WaitAsync())
            .Add(promise);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)>
            WaitAsync()
        {

        }
    }
}