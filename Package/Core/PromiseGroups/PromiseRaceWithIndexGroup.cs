#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        internal readonly int _count;
        private readonly short _id;
        private readonly bool _cancelOnNonResolved;
        private readonly bool _hasAtLeastOnePromise;

        private PromiseRaceWithIndexGroup(Internal.CancelationRef cancelationRef, bool cancelOnNonResolved)
        {
            _cancelationRef = cancelationRef;
            _count = 0;
            _cancelOnNonResolved = cancelOnNonResolved;
            _hasAtLeastOnePromise = false;
#if PROMISE_DEBUG
            // We always create the promise backing reference in DEBUG mode to ensure the group is used properly.
            _group = Internal.GetOrCreateRacePromiseWithIndexGroupVoid(cancelationRef, cancelOnNonResolved);
            _id = _group.Id;
#else
            // In RELEASE mode, we only create the backing reference when it's needed.
            _group = null;
            _id = 0;
#endif
        }

        [MethodImpl(Internal.InlineOption)]
        private PromiseRaceWithIndexGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.RacePromiseWithIndexGroupVoid group, int count, short id, bool cancelOnNonResolved)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _count = count;
            _id = id;
            _cancelOnNonResolved = cancelOnNonResolved;
            _hasAtLeastOnePromise = true;
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
            return new PromiseRaceWithIndexGroup(cancelationRef, cancelOnNonResolved);
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
            var cancelOnNonResolved = _cancelOnNonResolved;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidRaceGroup(1);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_id))
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }

                if (promise._ref != null)
                {
                    group.AddPromiseWithIndex(promise._ref, promise._id, checked(count++));
                }
                else
                {
                    group.SetResolved(count);
                }
                return new PromiseRaceWithIndexGroup(cancelationRef, group, count, group.Id, cancelOnNonResolved);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateRacePromiseWithIndexGroupVoid(cancelationRef, cancelOnNonResolved);
                group.AddPromise(promise._ref, promise._id);
                return new PromiseRaceWithIndexGroup(cancelationRef, group, 1, group.Id, cancelOnNonResolved);
            }

            // The promise is already resolved, we need to cancel the group token,
            // and catch any exceptions to propagate them out of WaitAsync().
            try
            {
                _cancelationRef.Cancel();
            }
            catch (Exception e)
            {
                // We already canceled the group token, no need to cancel it again if a promise is non-resolved.
                group = Internal.GetOrCreateRacePromiseWithIndexGroupVoid(cancelationRef, false);
                group.RecordException(e);
                return new PromiseRaceWithIndexGroup(cancelationRef, group, 0, group.Id, false);
            }

            return new PromiseRaceWithIndexGroup(cancelationRef, group, 0, _id, false);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If any promise is resolved, the returned promise will be resolved with the index of the promise that resolved first.
        /// If no promises are resolved and any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<int> WaitAsync()
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            if (cancelationRef == null | !_hasAtLeastOnePromise)
            {
                if (cancelationRef == null)
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }
                Internal.ThrowAtLeastOneRaceGroup(1);
            }

            if (group == null)
            {
                cancelationRef.Dispose();
                return Promise.Resolved(0);
            }

            if (!group.TryIncrementId(_id))
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
    public readonly struct PromiseRaceWithIndexGroup<T>
    {
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.RacePromiseWithIndexGroup<T> _group;
        private readonly T _result;
        internal readonly int _count;
        private readonly short _id;
        private readonly bool _cancelOnNonResolved;
        private readonly bool _hasAtLeastOnePromise;

        private PromiseRaceWithIndexGroup(Internal.CancelationRef cancelationRef, bool cancelOnNonResolved)
        {
            _cancelationRef = cancelationRef;
            _result = default;
            _count = 0;
            _cancelOnNonResolved = cancelOnNonResolved;
            _hasAtLeastOnePromise = false;
#if PROMISE_DEBUG
            // We always create the promise backing reference in DEBUG mode to ensure the group is used properly.
            _group = Internal.GetOrCreateRacePromiseWithIndexGroup<T>(cancelationRef, cancelOnNonResolved);
            _id = _group.Id;
#else
            // In RELEASE mode, we only create the backing reference when it's needed.
            _group = null;
            _id = 0;
#endif
        }

        [MethodImpl(Internal.InlineOption)]
        private PromiseRaceWithIndexGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.RacePromiseWithIndexGroup<T> group, in T result, int count, short id, bool cancelOnNonResolved)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _result = result;
            _count = count;
            _id = id;
            _cancelOnNonResolved = cancelOnNonResolved;
            _hasAtLeastOnePromise = true;
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
            return new PromiseRaceWithIndexGroup<T>(cancelationRef, cancelOnNonResolved);
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
            var cancelOnNonResolved = _cancelOnNonResolved;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidRaceGroup(1);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_id))
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }

                if (promise._ref != null)
                {
                    group.AddPromiseWithIndex(promise._ref, promise._id, checked(count++));
                }
                else
                {
                    group.SetResolved((count, promise._result));
                }
                return new PromiseRaceWithIndexGroup<T>(cancelationRef, group, default, count, group.Id, cancelOnNonResolved);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateRacePromiseWithIndexGroup<T>(cancelationRef, cancelOnNonResolved);
                group.AddPromise(promise._ref, promise._id);
                return new PromiseRaceWithIndexGroup<T>(cancelationRef, group, default, 1, group.Id, cancelOnNonResolved);
            }

            // The promise is already resolved, we need to cancel the group token,
            // and catch any exceptions to propagate them out of WaitAsync().
            try
            {
                _cancelationRef.Cancel();
            }
            catch (Exception e)
            {
                // We already canceled the group token, no need to cancel it again if a promise is non-resolved.
                group = Internal.GetOrCreateRacePromiseWithIndexGroup<T>(cancelationRef, false);
                group.RecordException(e);
                return new PromiseRaceWithIndexGroup<T>(cancelationRef, group, default, 0, group.Id, false);
            }

            return new PromiseRaceWithIndexGroup<T>(cancelationRef, group, promise._result, 0, _id, false);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If any promise is resolved, the returned promise will be resolved with the index and value of the promise that resolved first.
        /// If no promises are resolved and any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<(int winIndex, T result)> WaitAsync()
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            if (cancelationRef == null | !_hasAtLeastOnePromise)
            {
                if (cancelationRef == null)
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }
                Internal.ThrowAtLeastOneRaceGroup(1);
            }

            if (group == null)
            {
                cancelationRef.Dispose();
                return Promise.Resolved((0, _result));
            }

            if (!group.TryIncrementId(_id))
            {
                Internal.ThrowInvalidRaceGroup(1);
            }
            group.MarkReady(count);
            return new Promise<(int, T)>(group, group.Id);
        }
    }
}