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
    /// A structured concurrency group used to race promises. Waits for the first promise to resolve.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseRaceGroup
    {
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.RacePromiseGroupVoid _group;
        private readonly int _cancelationId;
        private readonly uint _count;
        private readonly short _groupId;
        private readonly bool _cancelOnNonResolved;
        private readonly bool _isResolved;

        [MethodImpl(Internal.InlineOption)]
        private PromiseRaceGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.RacePromiseGroupVoid group,
            uint count, short groupId, bool cancelOnNonResolved, bool isResolved)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _cancelationId = cancelationRef.SourceId;
            _count = count;
            _groupId = groupId;
            _cancelOnNonResolved = cancelOnNonResolved;
            _isResolved = isResolved;
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
            return new PromiseRaceGroup(cancelationRef, null, 0, 0, cancelOnNonResolved, false);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseRaceGroup Add(Promise promise)
        {
#if PROMISE_DEBUG
            Internal.ValidateArgument(promise, nameof(promise), 1);
#endif
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            var cancelOnNonResolved = _cancelOnNonResolved;
            var isResolved = _isResolved;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidRaceGroup(1);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_groupId))
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }

                if (promise._ref != null)
                {
                    checked { ++count; }
                    group.AddPromise(promise);
                }
                else
                {
                    isResolved = true;
                    group.SetResolved();
                }
                return new PromiseRaceGroup(cancelationRef, group, count, group.Id, cancelOnNonResolved, isResolved);
            }

            if (!cancelationRef.TryIncrementSourceId(_cancelationId))
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateRacePromiseGroup(cancelationRef, cancelOnNonResolved);
                if (isResolved)
                {
                    group.SetResolved();
                }
                group.AddPromise(promise);
                return new PromiseRaceGroup(cancelationRef, group, 1, group.Id, cancelOnNonResolved, isResolved);
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
                group = Internal.GetOrCreateRacePromiseGroup(cancelationRef, false);
                group.RecordException(e);
                group._cancelationOrCleanupThrew = true;
                return new PromiseRaceGroup(cancelationRef, group, 0, group.Id, false, isResolved);
            }

            return new PromiseRaceGroup(cancelationRef, group, 0, _groupId, false, true);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If any promise is resolved, the returned promise will be resolved.
        /// If no promises are resolved and any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if all promises are canceled, the returned promise will be canceled.
        /// </summary>
        public Promise WaitAsync()
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            if (cancelationRef == null | (group == null & !_isResolved))
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
                    Internal.ThrowInvalidAllGroup(1);
                }
                return Promise.Resolved();
            }

            if (!group.TryIncrementId(_groupId))
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
        private readonly Internal.RaceCleanupCallback<T> _cleanupCallback;
        private readonly T _result;
        private readonly int _cancelationId;
        private readonly uint _count;
        private readonly short _groupId;
        private readonly bool _cancelOnNonResolved;
        private readonly bool _isResolved;

        [MethodImpl(Internal.InlineOption)]
        private PromiseRaceGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.RacePromiseGroup<T> group, Internal.RaceCleanupCallback<T> cleanupCallback,
            in T result, uint count, short groupId, bool cancelOnNonResolved, bool isResolved)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _cleanupCallback = cleanupCallback;
            _result = result;
            _cancelationId = cancelationRef.SourceId;
            _count = count;
            _groupId = groupId;
            _cancelOnNonResolved = cancelOnNonResolved;
            _isResolved = isResolved;
        }

        private static PromiseRaceGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, bool cancelOnNonResolved, Internal.RaceCleanupCallback<T> cleanupCallback)
        {
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseRaceGroup<T>(cancelationRef, null, cleanupCallback, default, 0, 0, cancelOnNonResolved, false);
        }

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled when any of the promises completed the group.</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New(out CancelationToken groupCancelationToken, bool cancelOnNonResolved = true)
            => New(CancelationToken.None, out groupCancelationToken, cancelOnNonResolved, null);

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled or any of the promises completed the group.</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, bool cancelOnNonResolved = true)
            => New(sourceCancelationToken, out groupCancelationToken, cancelOnNonResolved, null);

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled when any of the promises completed the group.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element after the first (including the first if it throws).</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New(out CancelationToken groupCancelationToken,
            Action<T> onCleanup, bool cancelOnNonResolved = true)
            => New(CancelationToken.None, out groupCancelationToken, cancelOnNonResolved, RaceCleanupCallbackHelper.GetOrCreate(onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled or any of the promises completed the group.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element after the first (including the first if it throws).</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            Action<T> onCleanup, bool cancelOnNonResolved = true)
            => New(sourceCancelationToken, out groupCancelationToken, cancelOnNonResolved, RaceCleanupCallbackHelper.GetOrCreate(onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled when any of the promises completed the group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element after the first (including the first if it throws).</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New<TCaptureCleanup>(out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T> onCleanup, bool cancelOnNonResolved = true)
            => New(CancelationToken.None, out groupCancelationToken, cancelOnNonResolved, RaceCleanupCallbackHelper.GetOrCreate(cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled or any of the promises completed the group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element after the first (including the first if it throws).</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New<TCaptureCleanup>(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T> onCleanup, bool cancelOnNonResolved = true)
            => New(sourceCancelationToken, out groupCancelationToken, cancelOnNonResolved, RaceCleanupCallbackHelper.GetOrCreate(cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled when any of the promises completed the group.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element after the first (including the first if it throws).</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New(out CancelationToken groupCancelationToken,
            Func<T, Promise> onCleanup, bool cancelOnNonResolved = true)
            => New(CancelationToken.None, out groupCancelationToken, cancelOnNonResolved, RaceCleanupCallbackHelper.GetOrCreate(onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled or any of the promises completed the group.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element after the first (including the first if it throws).</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            Func<T, Promise> onCleanup, bool cancelOnNonResolved = true)
            => New(sourceCancelationToken, out groupCancelationToken, cancelOnNonResolved, RaceCleanupCallbackHelper.GetOrCreate(onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled when any of the promises completed the group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element after the first (including the first if it throws).</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New<TCaptureCleanup>(out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T, Promise> onCleanup, bool cancelOnNonResolved = true)
            => New(CancelationToken.None, out groupCancelationToken, cancelOnNonResolved, RaceCleanupCallbackHelper.GetOrCreate(cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseRaceGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled when <paramref name="sourceCancelationToken"/> is canceled or any of the promises completed the group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element after the first (including the first if it throws).</param>
        /// <param name="cancelOnNonResolved">If <see langword="true"/>, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved, canceled, or rejected.
        /// Otherwise, the <paramref name="groupCancelationToken"/> will be canceled when any promise is resolved.</param>
        public static PromiseRaceGroup<T> New<TCaptureCleanup>(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T, Promise> onCleanup, bool cancelOnNonResolved = true)
            => New(sourceCancelationToken, out groupCancelationToken, cancelOnNonResolved, RaceCleanupCallbackHelper.GetOrCreate(cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseRaceGroup<T> Add(Promise<T> promise)
        {
#if PROMISE_DEBUG
            Internal.ValidateArgument(promise, nameof(promise), 1);
#endif
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            var cancelOnNonResolved = _cancelOnNonResolved;
            var isResolved = _isResolved;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidRaceGroup(1);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_groupId))
                {
                    Internal.ThrowInvalidRaceGroup(1);
                }

                if (promise._ref != null)
                {
                    // Add 2 counts, 1 for the promise and 1 for the cleanup.
                    checked { count += 2; }
                    group.AddPromise(promise);
                }
                else if (!isResolved)
                {
                    isResolved = true;
                    group.SetResolved(promise._result);
                }
                else
                {
                    MaybeInvokeCleanup(ref count, promise._result, group);
                }
                return new PromiseRaceGroup<T>(cancelationRef, group, _cleanupCallback, default, count, group.Id, cancelOnNonResolved, isResolved);
            }

            if (!cancelationRef.TryIncrementSourceId(_cancelationId))
            {
                Internal.ThrowInvalidRaceGroup(1);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateRacePromiseGroup(cancelationRef, cancelOnNonResolved, _cleanupCallback);
                if (isResolved)
                {
                    group.SetResolved(_result);
                }
                group.AddPromise(promise);
                // Add 2 counts, 1 for the promise and 1 for the cleanup.
                return new PromiseRaceGroup<T>(cancelationRef, group, _cleanupCallback, default, 2, group.Id, cancelOnNonResolved, isResolved);
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
                group = Internal.GetOrCreateRacePromiseGroup(cancelationRef, false, _cleanupCallback);
                if (isResolved)
                {
                    group.SetResolved(_result);
                }
                group.RecordException(e);
                group._cancelationOrCleanupThrew = true;
                MaybeInvokeCleanup(ref count, promise._result, group);
                return new PromiseRaceGroup<T>(cancelationRef, group, _cleanupCallback, default, count, group.Id, false, isResolved);
            }

            if (!isResolved)
            {
                return new PromiseRaceGroup<T>(cancelationRef, group, _cleanupCallback, promise._result, 0, _groupId, false, true);
            }

            var cleanupCallback = _cleanupCallback;
            if (cleanupCallback == null)
            {
                return new PromiseRaceGroup<T>(cancelationRef, group, _cleanupCallback, _result, 0, _groupId, false, true);
            }

            var cleanupPromise = cleanupCallback.Invoke(promise._result);
            if (cleanupPromise._ref == null)
            {
                return new PromiseRaceGroup<T>(cancelationRef, group, _cleanupCallback, _result, 0, _groupId, false, true);
            }

            group = Internal.GetOrCreateRacePromiseGroup(cancelationRef, false, cleanupCallback);
            group.SetResolved(_result);
            group.HookupCleanupPromise(cleanupPromise);
            return new PromiseRaceGroup<T>(cancelationRef, group, cleanupCallback, default, 1, group.Id, false, isResolved);
        }

        private void MaybeInvokeCleanup(ref uint count, in T arg, Internal.PromiseRefBase.RacePromiseGroup<T> group)
        {
            var cleanupCallback = _cleanupCallback;
            if (cleanupCallback == null)
            {
                return;
            }

            var cleanupPromise = cleanupCallback.Invoke(arg);
            if (cleanupPromise._ref == null)
            {
                return;
            }

            checked { ++count; }
            group.HookupCleanupPromise(cleanupPromise);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If any promise is resolved, the returned promise will be resolved with the value of the promise that resolved first.
        /// If no promises are resolved and any promise is rejected, or if any cleanup delegate threw, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if all promises are canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<T> WaitAsync()
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            if (cancelationRef == null | (group == null & !_isResolved))
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
                _cleanupCallback?.Dispose();
                return Promise.Resolved(_result);
            }

            if (!group.TryIncrementId(_groupId))
            {
                Internal.ThrowInvalidRaceGroup(1);
            }
            group.MarkReady(count);
            return new Promise<T>(group, group.Id);
        }
    }
}