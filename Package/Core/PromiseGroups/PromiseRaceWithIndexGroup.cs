#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.RacePromiseWithIndexGroupVoid _group;
        private readonly int _cancelationId;
        private readonly int _count;
        private readonly int _index;
        private readonly int _winIndex;
        private readonly short _groupId;
        private readonly bool _cancelOnNonResolved;

        [MethodImpl(Internal.InlineOption)]
        private PromiseRaceWithIndexGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.RacePromiseWithIndexGroupVoid group,
            int count, int index, int winIndex, short groupId, bool cancelOnNonResolved)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _cancelationId = cancelationRef.SourceId;
            _count = count;
            _index = index;
            _winIndex = winIndex;
            _groupId = groupId;
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
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseRaceWithIndexGroup(cancelationRef, null, count: 0, index: -1, groupId: 0, winIndex: -1, cancelOnNonResolved: cancelOnNonResolved);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseRaceWithIndexGroup Add(Promise promise)
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            var index = _index;
            var winIndex = _winIndex;
            var cancelOnNonResolved = _cancelOnNonResolved;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidRaceGroup(1);
            }

            checked { ++index; }
            if (group != null)
            {
                if (!group.TryIncrementId(_groupId))
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }

                if (promise._ref != null)
                {
                    ++count;
                    group.AddPromiseWithIndex(promise, index);
                }
                else if (winIndex == -1)
                {
                    winIndex = index;
                    group.SetResolved(index);
                }
                return new PromiseRaceWithIndexGroup(cancelationRef, group, count, index, winIndex, group.Id, cancelOnNonResolved);
            }

            if (!cancelationRef.TryIncrementSourceId(_cancelationId))
            {
                Internal.ThrowInvalidRaceGroup(1);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateRacePromiseWithIndexGroupVoid(cancelationRef, cancelOnNonResolved);
                group.AddPromiseWithIndex(promise, index);
                if (winIndex != -1)
                {
                    group.SetResolved(winIndex);
                }
                return new PromiseRaceWithIndexGroup(cancelationRef, group, 1, index, winIndex, group.Id, cancelOnNonResolved);
            }

            // The promise is already resolved, we need to cancel the group token,
            // and catch any exceptions to propagate them out of WaitAsync().
            try
            {
                _cancelationRef.CancelUnsafe();
            }
            catch (Exception e)
            {
                // We already canceled the group token, no need to cancel it again if a promise is non-resolved.
                group = Internal.GetOrCreateRacePromiseWithIndexGroupVoid(cancelationRef, false);
                group.RecordException(e);
                group._cancelationThrew = true;
                return new PromiseRaceWithIndexGroup(cancelationRef, group, 0, index, winIndex, group.Id, false);
            }

            return new PromiseRaceWithIndexGroup(cancelationRef, group, 0, index, winIndex != -1 ? winIndex : index, _groupId, false);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If any promise is resolved, the returned promise will be resolved with the index of the promise that resolved first.
        /// If no promises are resolved and any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if all promises are canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<int> WaitAsync()
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            var winIndex = _winIndex;
            if (cancelationRef == null | (group == null & winIndex == -1))
            {
                if (cancelationRef == null)
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }
                Internal.ThrowAtLeastOneRaceGroup(1);
            }

            if (group == null)
            {
                if (!cancelationRef.TryDispose(_cancelationId))
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }
                return Promise.Resolved(winIndex);
            }

            if (!group.TryIncrementId(_groupId))
            {
                Internal.ThrowInvalidRaceGroup(1);
            }
            group.MarkReady(count);
            return new Promise<int>(group, group.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to race promises, incorporating their indices. Waits for the first promise to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    [StructLayout(LayoutKind.Auto)] // If T is 1 byte large, it can be packed efficiently by the runtime.
    public readonly struct PromiseRaceWithIndexGroup<T>
    {
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.RacePromiseWithIndexGroup<T> _group;
        private readonly T _result;
        private readonly int _cancelationId;
        private readonly int _count;
        private readonly int _index;
        private readonly int _winIndex;
        private readonly short _groupId;
        private readonly bool _cancelOnNonResolved;

        [MethodImpl(Internal.InlineOption)]
        private PromiseRaceWithIndexGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.RacePromiseWithIndexGroup<T> group,
            in T result, int count, int index, short groupId, bool cancelOnNonResolved, int winIndex)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _cancelationId = cancelationRef.SourceId;
            _count = count;
            _index = index;
            _groupId = groupId;
            _cancelOnNonResolved = cancelOnNonResolved;
            _winIndex = winIndex;
            _result = result;
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
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseRaceWithIndexGroup<T>(cancelationRef, null, default, count: 0, index: -1, groupId: 0, cancelOnNonResolved: cancelOnNonResolved, winIndex: -1);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseRaceWithIndexGroup<T> Add(Promise<T> promise)
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            var index = _index;
            var cancelOnNonResolved = _cancelOnNonResolved;
            var winIndex = _winIndex;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidRaceGroup(1);
            }

            checked { ++index; }
            if (group != null)
            {
                if (!group.TryIncrementId(_groupId))
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }

                if (promise._ref != null)
                {
                    ++count;
                    group.AddPromiseWithIndex(promise, index);
                }
                else if (winIndex == -1)
                {
                    winIndex = index;
                    group.SetResolved((index, promise._result));
                }
                return new PromiseRaceWithIndexGroup<T>(cancelationRef, group, default, count, index, group.Id, cancelOnNonResolved, winIndex);
            }

            if (!cancelationRef.TryIncrementSourceId(_cancelationId))
            {
                Internal.ThrowInvalidRaceGroup(1);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateRacePromiseWithIndexGroup<T>(cancelationRef, cancelOnNonResolved);
                group.AddPromiseWithIndex(promise, index);
                if (winIndex != -1)
                {
                    group.SetResolved((winIndex, _result));
                }
                return new PromiseRaceWithIndexGroup<T>(cancelationRef, group, default, 1, index, group.Id, cancelOnNonResolved, winIndex);
            }

            // The promise is already resolved, we need to cancel the group token,
            // and catch any exceptions to propagate them out of WaitAsync().
            try
            {
                _cancelationRef.CancelUnsafe();
            }
            catch (Exception e)
            {
                // We already canceled the group token, no need to cancel it again if a promise is non-resolved.
                group = Internal.GetOrCreateRacePromiseWithIndexGroup<T>(cancelationRef, false);
                group.RecordException(e);
                group._cancelationThrew = true;
                return new PromiseRaceWithIndexGroup<T>(cancelationRef, group, default, 0, index, group.Id, false, winIndex);
            }

            return winIndex != -1
                ? new PromiseRaceWithIndexGroup<T>(cancelationRef, group, _result, 0, index, _groupId, false, winIndex)
                : new PromiseRaceWithIndexGroup<T>(cancelationRef, group, promise._result, 0, index, _groupId, false, index);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If any promise is resolved, the returned promise will be resolved with the index and value of the promise that resolved first.
        /// If no promises are resolved and any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if all promises are canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<(int winIndex, T result)> WaitAsync()
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            var winIndex = _winIndex;
            if (cancelationRef == null | (group == null & winIndex == -1))
            {
                if (cancelationRef == null)
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }
                Internal.ThrowAtLeastOneRaceGroup(1);
            }

            if (group == null)
            {
                if (!cancelationRef.TryDispose(_cancelationId))
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }
                return Promise.Resolved((winIndex, _result));
            }

            if (!group.TryIncrementId(_groupId))
            {
                Internal.ThrowInvalidRaceGroup(1);
            }
            group.MarkReady(count);
            return new Promise<(int, T)>(group, group.Id);
        }
    }
}