#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseAllResultsGroup
    {
        private readonly IList<Promise.ResultContainer> _valueContainer;
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.AllPromiseResultsGroupVoid _group;
        private readonly int _cancelationId;
        private readonly int _count;
        private readonly int _index;
        private readonly short _groupId;

        [MethodImpl(Internal.InlineOption)]
        private PromiseAllResultsGroup(IList<Promise.ResultContainer> valueContainer, Internal.CancelationRef cancelationRef,
            Internal.PromiseRefBase.AllPromiseResultsGroupVoid group, int count, int index, short groupId)
        {
            _valueContainer = valueContainer;
            _cancelationRef = cancelationRef;
            _cancelationId = cancelationRef.SourceId;
            _group = group;
            _count = count;
            _index = index;
            _groupId = groupId;
        }

        /// <summary>
        /// Get a new <see cref="PromiseAllResultsGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllResultsGroup New(out CancelationToken groupCancelationToken, IList<Promise.ResultContainer> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllResultsGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllResultsGroup New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, IList<Promise.ResultContainer> valueContainer = null)
        {
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseAllResultsGroup(valueContainer ?? new List<Promise.ResultContainer>(), cancelationRef, null, 0, 0, 0);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseAllResultsGroup Add(Promise promise)
        {
            var list = _valueContainer;
            var cancelationRef = _cancelationRef;
            var group = _group;
            int count = _count;
            int index = _index;
            if (cancelationRef == null | list == null)
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_groupId))
                {
                    Internal.ThrowInvalidAllGroup(1);
                }

                // We don't protect the list with a lock, because we ensure this is only used by a single caller.
                list.SetOrAdd(Promise.ResultContainer.Resolved, index);
                // We don't need to do anything else if the ref is null.
                if (promise._ref != null)
                {
                    ++count;
                    group.AddPromiseForMerge(promise._ref, promise._id, index);
                }
                return new PromiseAllResultsGroup(list, cancelationRef, group, count, index + 1, group.Id);
            }

            if (!cancelationRef.TryIncrementSourceId(_cancelationId))
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            // We don't protect the list with a lock, because we ensure this is only used by a single caller.
            list.SetOrAdd(Promise.ResultContainer.Resolved, index);
            if (promise._ref != null)
            {
                group = Internal.GetOrCreateAllPromiseResultsGroup(cancelationRef, list);
                group.AddPromiseForMerge(promise._ref, promise._id, index);
                return new PromiseAllResultsGroup(list, cancelationRef, group, 1, index + 1, group.Id);
            }

            return new PromiseAllResultsGroup(list, cancelationRef, null, 0, index + 1, 0);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a list containing their results.
        /// </summary>
        public Promise<IList<Promise.ResultContainer>> WaitAsync()
        {
            var list = _valueContainer;
            var cancelationRef = _cancelationRef;
            var group = _group;
            int count = _count;
            int index = _index;
            if (cancelationRef == null | list == null)
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            if (group == null)
            {
                if (!cancelationRef.TryDispose(_cancelationId))
                {
                    Internal.ThrowInvalidAllGroup(1);
                }

                // Make sure list has the same count as promises.
                list.MaybeShrink(index);
                return Promise.Resolved(list);
            }

            if (!group.TryIncrementId(_groupId))
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            // Make sure list has the same count as promises.
            list.MaybeShrink(index);
            group.MarkReady(count);
            return new Promise<IList<Promise.ResultContainer>>(group, group.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of a type and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseAllResultsGroup<T>
    {
        private readonly IList<Promise<T>.ResultContainer> _valueContainer;
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.AllPromiseResultsGroup<T> _group;
        private readonly int _cancelationId;
        private readonly int _count;
        private readonly int _index;
        private readonly short _id;

        [MethodImpl(Internal.InlineOption)]
        private PromiseAllResultsGroup(IList<Promise<T>.ResultContainer> valueContainer, Internal.CancelationRef cancelationRef,
            Internal.PromiseRefBase.AllPromiseResultsGroup<T> group, int count, int index, short groupId)
        {
            _valueContainer = valueContainer;
            _cancelationRef = cancelationRef;
            _cancelationId = cancelationRef.SourceId;
            _group = group;
            _count = count;
            _index = index;
            _id = groupId;
        }

        /// <summary>
        /// Get a new <see cref="PromiseAllResultsGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllResultsGroup<T> New(out CancelationToken groupCancelationToken, IList<Promise<T>.ResultContainer> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllResultsGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllResultsGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, IList<Promise<T>.ResultContainer> valueContainer = null)
        {
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseAllResultsGroup<T>(valueContainer ?? new List<Promise<T>.ResultContainer>(), cancelationRef, null, 0, 0, 0);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseAllResultsGroup<T> Add(Promise<T> promise)
        {
            var list = _valueContainer;
            var cancelationRef = _cancelationRef;
            var group = _group;
            int count = _count;
            int index = _index;
            if (cancelationRef == null | list == null)
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_id))
                {
                    Internal.ThrowInvalidAllGroup(1);
                }

                // We don't protect the list with a lock, because we ensure this is only used by a single caller.
                list.SetOrAdd(promise._result, index);
                // We don't need to do anything else if the ref is null.
                if (promise._ref != null)
                {
                    ++count;
                    group.AddPromiseForMerge(promise._ref, promise._id, index);
                }
                return new PromiseAllResultsGroup<T>(list, cancelationRef, group, count, index + 1, group.Id);
            }

            if (!cancelationRef.TryIncrementSourceId(_cancelationId))
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            // We don't protect the list with a lock, because we ensure this is only used by a single caller.
            list.SetOrAdd(promise._result, index);
            if (promise._ref != null)
            {
                group = Internal.GetOrCreateAllPromiseResultsGroup(cancelationRef, list);
                group.AddPromiseForMerge(promise._ref, promise._id, index);
                return new PromiseAllResultsGroup<T>(list, cancelationRef, group, 1, index + 1, group.Id);
            }

            return new PromiseAllResultsGroup<T>(list, cancelationRef, null, 0, index + 1, 0);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a list containing their results.
        /// </summary>
        public Promise<IList<Promise<T>.ResultContainer>> WaitAsync()
        {
            var list = _valueContainer;
            var cancelationRef = _cancelationRef;
            var group = _group;
            int count = _count;
            int index = _index;
            if (cancelationRef == null | list == null)
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            if (group == null)
            {
                if (!cancelationRef.TryDispose(_cancelationId))
                {
                    Internal.ThrowInvalidAllGroup(1);
                }

                // Make sure list has the same count as promises.
                list.MaybeShrink(index);
                return Promise.Resolved(list);
            }

            if (!group.TryIncrementId(_id))
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            // Make sure list has the same count as promises.
            list.MaybeShrink(index);
            group.MarkReady(count);
            return new Promise<IList<Promise<T>.ResultContainer>>(group, group.Id);
        }
    }
}