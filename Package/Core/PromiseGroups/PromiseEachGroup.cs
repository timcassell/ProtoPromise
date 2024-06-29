#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Proto.Promises
{
    /// <summary>
    /// A structured concurrency group used to yield promise results as they complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseEachGroup
    {
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.EachPromiseGroup<Promise.ResultContainer> _group;
        private readonly int _cancelationId;
        private readonly int _groupId;
        private readonly int _pendingCount;
        private readonly int _totalCount;

        [MethodImpl(Internal.InlineOption)]
        private PromiseEachGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.EachPromiseGroup<Promise.ResultContainer> group,
            int groupId, int pendingCount, int totalCount)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _cancelationId = cancelationRef.SourceId;
            _groupId = groupId;
            _pendingCount = pendingCount;
            _totalCount = totalCount;
        }

        /// <summary>
        /// Get a new <see cref="PromiseEachGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">
        /// The token that will be canceled when the associated <see cref="AsyncEnumerator{T}"/> is disposed,
        /// or when the associated <see cref="AsyncEnumerator{T}"/>'s <see cref="CancelationToken"/> is canceled, if it has one attached.
        /// </param>
        public static PromiseEachGroup New(out CancelationToken groupCancelationToken)
            => New(CancelationToken.None, out groupCancelationToken);

        /// <summary>
        /// Get a new <see cref="PromiseEachGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">
        /// The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled,
        /// or when the associated <see cref="AsyncEnumerator{T}"/> is disposed,
        /// or when the associated <see cref="AsyncEnumerator{T}"/>'s <see cref="CancelationToken"/> is canceled, if it has one attached.
        /// </param>
        /// <remarks>
        /// If the <paramref name="sourceCancelationToken"/> is canceled before the iteration is complete, iteration will not be canceled.
        /// To cancel the iteration, use <see cref="AsyncEnumerable{T}.WithCancelation(CancelationToken)"/> on the <see cref="AsyncEnumerable{T}"/> returned from <see cref="GetAsyncEnumerable"/>.
        /// </remarks>
        public static PromiseEachGroup New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken)
        {
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseEachGroup(cancelationRef, null, 0, 0, 0);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseEachGroup Add(Promise promise)
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var pendingCount = _pendingCount;
            var totalCount = _totalCount;
            if (group == null)
            {
                if (cancelationRef?.TryIncrementSourceId(_cancelationId) != true)
                {
                    Internal.ThrowInvalidEachGroup(1);
                }

                group = Internal.GetOrCreateEachPromiseGroup(cancelationRef);
            }
            else if (!group.TryIncrementId(_groupId))
            {
                Internal.ThrowInvalidEachGroup(1);
            }

            checked { ++totalCount; }
            if (promise._ref == null)
            {
                group.AddResult(Promise.ResultContainer.Resolved);
            }
            else
            {
                ++pendingCount;
                group.AddPromise(promise._ref, promise._id);
            }
            return new PromiseEachGroup(cancelationRef, group, group.EnumerableId, pendingCount, totalCount);
        }

        /// <summary>
        /// Gets the <see cref="AsyncEnumerable{T}"/> that will yield the result of each promise as they complete.
        /// </summary>
        /// <param name="suppressUnobservedRejections">
        /// If <see langword="true"/>, unobserved rejections will be suppressed.
        /// Otherwise, if the associated <see cref="AsyncEnumerator{T}"/> is disposed before all results were observed, and any promise was rejected,
        /// the <see cref="AsyncEnumerator{T}.DisposeAsync"/> promise will be rejected with an <see cref="AggregateException"/> containing all of the unobserved rejections.
        /// </param>
        public AsyncEnumerable<Promise.ResultContainer> GetAsyncEnumerable(bool suppressUnobservedRejections = false)
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var pendingCount = _pendingCount;
            var totalCount = _totalCount;
            if (group == null)
            {
                if (cancelationRef?.TryDispose(_cancelationId) != true)
                {
                    Internal.ThrowInvalidEachGroup(1);
                }
                return AsyncEnumerable<Promise.ResultContainer>.Empty();
            }
            else if (!group.TryIncrementId(_groupId))
            {
                Internal.ThrowInvalidEachGroup(1);
            }

            group.MarkReady(suppressUnobservedRejections, pendingCount, totalCount);
            return new AsyncEnumerable<Promise.ResultContainer>(group);
        }
    }

    /// <summary>
    /// A structured concurrency group used to yield promise results as they complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    [StructLayout(LayoutKind.Auto)]
    public readonly struct PromiseEachGroup<T>
    {
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.EachPromiseGroup<Promise<T>.ResultContainer> _group;
        private readonly int _cancelationId;
        private readonly int _groupId;
        private readonly int _pendingCount;
        private readonly int _totalCount;

        [MethodImpl(Internal.InlineOption)]
        private PromiseEachGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.EachPromiseGroup<Promise<T>.ResultContainer> group,
            int groupId, int pendingCount, int totalCount)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _cancelationId = cancelationRef.SourceId;
            _groupId = groupId;
            _pendingCount = pendingCount;
            _totalCount = totalCount;
        }

        /// <summary>
        /// Get a new <see cref="PromiseEachGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">
        /// The token that will be canceled when the associated <see cref="AsyncEnumerator{T}"/> is disposed,
        /// or when the associated <see cref="AsyncEnumerator{T}"/>'s <see cref="CancelationToken"/> is canceled, if it has one attached.
        /// </param>
        public static PromiseEachGroup<T> New(out CancelationToken groupCancelationToken)
            => New(CancelationToken.None, out groupCancelationToken);

        /// <summary>
        /// Get a new <see cref="PromiseEachGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">
        /// The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled,
        /// or when the associated <see cref="AsyncEnumerator{T}"/> is disposed,
        /// or when the associated <see cref="AsyncEnumerator{T}"/>'s <see cref="CancelationToken"/> is canceled, if it has one attached.
        /// </param>
        /// <remarks>
        /// If the <paramref name="sourceCancelationToken"/> is canceled before the iteration is complete, iteration will not be canceled.
        /// To cancel the iteration, use <see cref="AsyncEnumerable{T}.WithCancelation(CancelationToken)"/> on the <see cref="AsyncEnumerable{T}"/> returned from <see cref="GetAsyncEnumerable"/>.
        /// </remarks>
        public static PromiseEachGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken)
        {
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseEachGroup<T>(cancelationRef, null, 0, 0, 0);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseEachGroup<T> Add(Promise<T> promise)
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var pendingCount = _pendingCount;
            var totalCount = _totalCount;
            if (group == null)
            {
                if (cancelationRef?.TryIncrementSourceId(_cancelationId) != true)
                {
                    Internal.ThrowInvalidEachGroup(1);
                }

                group = Internal.GetOrCreateEachPromiseGroup<T>(cancelationRef);
            }
            else if (!group.TryIncrementId(_groupId))
            {
                Internal.ThrowInvalidEachGroup(1);
            }

            checked { ++totalCount; }
            if (promise._ref == null)
            {
                group.AddResult(promise._result);
            }
            else
            {
                ++pendingCount;
                group.AddPromise(promise._ref, promise._id);
            }
            return new PromiseEachGroup<T>(cancelationRef, group, group.EnumerableId, pendingCount, totalCount);
        }

        /// <summary>
        /// Gets the <see cref="AsyncEnumerable{T}"/> that will yield the result of each promise as they complete.
        /// </summary>
        /// <param name="suppressUnobservedRejections">
        /// If <see langword="true"/>, unobserved rejections will be suppressed.
        /// Otherwise, if the associated <see cref="AsyncEnumerator{T}"/> is disposed before all results were observed, and any promise was rejected,
        /// the <see cref="AsyncEnumerator{T}.DisposeAsync"/> promise will be rejected with an <see cref="AggregateException"/> containing all of the unobserved rejections.
        /// </param>
        public AsyncEnumerable<Promise<T>.ResultContainer> GetAsyncEnumerable(bool suppressUnobservedRejections = false)
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var pendingCount = _pendingCount;
            var totalCount = _totalCount;
            if (group == null)
            {
                if (cancelationRef?.TryDispose(_cancelationId) != true)
                {
                    Internal.ThrowInvalidEachGroup(1);
                }
                return AsyncEnumerable<Promise<T>.ResultContainer>.Empty();
            }
            else if (!group.TryIncrementId(_groupId))
            {
                Internal.ThrowInvalidEachGroup(1);
            }

            group.MarkReady(suppressUnobservedRejections, pendingCount, totalCount);
            return new AsyncEnumerable<Promise<T>.ResultContainer>(group);
        }
    }
}