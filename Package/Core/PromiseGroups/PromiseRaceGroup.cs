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
    /// A structured concurrency group used to race promises. Waits for the first promise to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseRaceGroup
    {
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.RacePromiseGroup<Internal.VoidResult> _group;
        internal readonly uint _count;
        private readonly short _id;
        private readonly bool _cancelOnNonResolved;
        private readonly bool _hasAtLeastOnePromise;

        private PromiseRaceGroup(Internal.CancelationRef cancelationRef, bool cancelOnNonResolved)
        {
            _cancelationRef = cancelationRef;
            _count = 0;
            _cancelOnNonResolved = cancelOnNonResolved;
            _hasAtLeastOnePromise = false;
#if PROMISE_DEBUG
            // We always create the promise backing reference in DEBUG mode to ensure the group is used properly.
            _group = Internal.GetOrCreateRacePromiseGroup<Internal.VoidResult>(cancelationRef, cancelOnNonResolved);
            _id = _group.Id;
#else
            // In RELEASE mode, we only create the backing reference when it's needed.
            _group = null;
            _id = 0;
#endif
        }

        [MethodImpl(Internal.InlineOption)]
        private PromiseRaceGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.RacePromiseGroup<Internal.VoidResult> group, uint count, short id, bool cancelOnNonResolved)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _count = count;
            _id = id;
            _cancelOnNonResolved = cancelOnNonResolved;
            _hasAtLeastOnePromise = true;
        }

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled when any of the promises completed the group.</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup New(out CancelationToken groupCancelationToken, bool cancelOnNonResolved = true)
            => New(CancelationToken.None, out groupCancelationToken, cancelOnNonResolved);

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled or any of the promises completed the group.</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, bool cancelOnNonResolved = true)
        {
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseRaceGroup(cancelationRef, cancelOnNonResolved);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseRaceGroup Add(Promise promise)
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
                    checked { ++count; }
                    group.AddPromise(promise._ref, promise._id);
                }
                else
                {
                    group.SetResolved();
                }
                return new PromiseRaceGroup(cancelationRef, group, count, group.Id, cancelOnNonResolved);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateRacePromiseGroup<Internal.VoidResult>(cancelationRef, cancelOnNonResolved);
                group.AddPromise(promise._ref, promise._id);
                return new PromiseRaceGroup(cancelationRef, group, 1, group.Id, cancelOnNonResolved);
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
                group = Internal.GetOrCreateRacePromiseGroup<Internal.VoidResult>(cancelationRef, false);
                group.RecordException(e);
                return new PromiseRaceGroup(cancelationRef, group, 0, group.Id, false);
            }

            return new PromiseRaceGroup(cancelationRef, group, 0, _id, false);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If any promise is resolved, the returned promise will be resolved.
        /// If no promises are resolved and any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise WaitAsync()
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
                return Promise.Resolved();
            }

            if (!group.TryIncrementId(_id))
            {
                Internal.ThrowInvalidRaceGroup(1);
            }
            group.MarkReady(count);
            return new Promise(group, group.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to race promises. Waits for the first promise to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseRaceGroup<T>
    {
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.RacePromiseGroup<T> _group;
        private readonly T _result;
        internal readonly uint _count;
        private readonly short _id;
        private readonly bool _cancelOnNonResolved;
        private readonly bool _hasAtLeastOnePromise;

        private PromiseRaceGroup(Internal.CancelationRef cancelationRef, bool cancelOnNonResolved)
        {
            _cancelationRef = cancelationRef;
            _result = default;
            _count = 0;
            _cancelOnNonResolved = cancelOnNonResolved;
            _hasAtLeastOnePromise = false;
#if PROMISE_DEBUG
            // We always create the promise backing reference in DEBUG mode to ensure the group is used properly.
            _group = Internal.GetOrCreateRacePromiseGroup<T>(cancelationRef, cancelOnNonResolved);
            _id = _group.Id;
#else
            // In RELEASE mode, we only create the backing reference when it's needed.
            _group = null;
            _id = 0;
#endif
        }

        [MethodImpl(Internal.InlineOption)]
        private PromiseRaceGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.RacePromiseGroup<T> group, in T result, uint count, short id, bool cancelOnNonResolved)
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
        /// Get a new <see cref="PromiseRaceGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled when any of the promises completed the group.</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New(out CancelationToken groupCancelationToken, bool cancelOnNonResolved = true)
            => New(CancelationToken.None, out groupCancelationToken, cancelOnNonResolved);

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled or any of the promises completed the group.</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, bool cancelOnNonResolved = true)
        {
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseRaceGroup<T>(cancelationRef, cancelOnNonResolved);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseRaceGroup<T> Add(Promise<T> promise)
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
                    checked { ++count; }
                    group.AddPromise(promise._ref, promise._id);
                }
                else
                {
                    group.SetResolved();
                }
                return new PromiseRaceGroup<T>(cancelationRef, group, default, count, group.Id, cancelOnNonResolved);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateRacePromiseGroup<T>(cancelationRef, cancelOnNonResolved);
                group.AddPromise(promise._ref, promise._id);
                return new PromiseRaceGroup<T>(cancelationRef, group, default, 1, group.Id, cancelOnNonResolved);
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
                group = Internal.GetOrCreateRacePromiseGroup<T>(cancelationRef, false);
                group.RecordException(e);
                return new PromiseRaceGroup<T>(cancelationRef, group, default, 0, group.Id, false);
            }

            return new PromiseRaceGroup<T>(cancelationRef, group, promise._result, 0, _id, false);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If any promise is resolved, the returned promise will be resolved with the value of the promise that resolved first.
        /// If no promises are resolved and any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<T> WaitAsync()
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
                return Promise.Resolved(_result);
            }

            if (!group.TryIncrementId(_id))
            {
                Internal.ThrowInvalidRaceGroup(1);
            }
            group.MarkReady(count);
            return new Promise<T>(group, group.Id);
        }
    }
}