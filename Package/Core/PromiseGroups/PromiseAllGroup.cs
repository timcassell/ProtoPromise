#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Proto.Promises
{
    /// <summary>
    /// A structured concurrency group used to merge promises. Waits for all promises to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseAllGroup<T>
    {
        private readonly CancelationSource _cancelationSource;

        internal PromiseAllGroup(CancelationSource cancelationSource)
        {
            _cancelationSource = cancelationSource;
        }

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        public static PromiseAllGroup<T> New(out CancelationToken groupCancelationToken)
            => New(CancelationToken.None, out groupCancelationToken);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        public static PromiseAllGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken)
        {

        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseAllGroup<T> Add(Promise<T> promise)
        {

        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a list containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public Promise<IList<T>> WaitAsync(IList<T> valueContainer = null)
        {

        }
    }
}