#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Collections.Generic;
using System.Diagnostics;

namespace Proto.Promises
{
    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseAllResultsGroup
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseAllResultsGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Get a new <see cref="PromiseAllResultsGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        public static PromiseAllResultsGroup New(out CancelationToken groupCancelationToken)
            => New(CancelationToken.None, out groupCancelationToken);

        /// <summary>
        /// Get a new <see cref="PromiseAllResultsGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        public static PromiseAllResultsGroup New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseAllResultsGroup Add(Promise promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a list containing their results.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the results. If it is not provided, a new one will be created.</param>
        public Promise<IList<Promise.ResultContainer>> WaitAsync(IList<Promise.ResultContainer> valueContainer = null)
        {

        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseAllResultsGroup<T>
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseAllResultsGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Get a new <see cref="PromiseAllResultsGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        public static PromiseAllResultsGroup<T> New(out CancelationToken groupCancelationToken)
            => New(CancelationToken.None, out groupCancelationToken);

        /// <summary>
        /// Get a new <see cref="PromiseAllResultsGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        public static PromiseAllResultsGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseAllResultsGroup<T> Add(Promise<T> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a list containing their results.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the results. If it is not provided, a new one will be created.</param>
        public Promise<IList<Promise<T>.ResultContainer>> WaitAsync(IList<Promise<T>.ResultContainer> valueContainer = null)
        {

        }
    }
}