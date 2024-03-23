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
    /// A structured concurrency group used to merge promises. Waits for all promises to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseMergeGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Get a new <see cref="PromiseMergeGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        public static PromiseMergeGroup New(out CancelationToken groupCancelationToken)
            => New(CancelationToken.None, out groupCancelationToken);

        /// <summary>
        /// Get a new <see cref="PromiseMergeGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        public static PromiseMergeGroup New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup Add(Promise promise)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T> Add<T>(Promise<T> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises. Waits for all promises to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1>
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseMergeGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1> Add(Promise promise)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2> Add<T2>(Promise<T2> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with the resolved value.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<T1> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises. Waits for all promises to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2>
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseMergeGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2> Add(Promise promise)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3> Add<T3>(Promise<T3> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<(T1, T2)> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises. Waits for all promises to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2, T3>
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseMergeGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3> Add(Promise promise)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4> Add<T4>(Promise<T4> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<(T1, T2, T3)> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises. Waits for all promises to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2, T3, T4>
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseMergeGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4> Add(Promise promise)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5> Add<T5>(Promise<T5> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<(T1, T2, T3, T4)> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises. Waits for all promises to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2, T3, T4, T5>
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseMergeGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5> Add(Promise promise)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6> Add<T6>(Promise<T6> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<(T1, T2, T3, T4, T5)> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises. Waits for all promises to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2, T3, T4, T5, T6>
    {
        private readonly CancelationSource _cancelationSource;
        private readonly Promise<T6> _promise6;

        internal PromiseMergeGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6> Add(Promise promise)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7> Add<T7>(Promise<T7> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<(T1, T2, T3, T4, T5, T6)> WaitAsync()
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises. Waits for all promises to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7>
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseMergeGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7> Add(Promise promise)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        // Merging more than 7 types should be fairly rare. To support N types greater than 7, we just wrap it in another group.
        public PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7), T8> Add<T8>(Promise<T8> promise)
            => new PromiseMergeGroup(_cancelationSource)
            .Add(WaitAsync())
            .Add(promise);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<(T1, T2, T3, T4, T5, T6, T7)> WaitAsync()
        {

        }
    }
}