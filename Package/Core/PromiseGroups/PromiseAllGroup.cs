#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0028 // Collection initialization can be simplified

using Proto.Promises.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    /// <summary>
    /// A structured concurrency group used to merge promises of a type.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseAllGroup<T>
    {
        private readonly IList<T> _valueContainer;
        private readonly Internal.CancelationRef _cancelationRef;
        private readonly Internal.PromiseRefBase.AllPromiseGroup<T> _group;
        private readonly Internal.AllCleanupCallback<T> _cleanupCallback;
        private readonly int _cancelationId;
        private readonly int _count;
        private readonly int _index;
        private readonly short _groupId;

        [MethodImpl(Internal.InlineOption)]
        private PromiseAllGroup(IList<T> valueContainer, Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.AllPromiseGroup<T> group, Internal.AllCleanupCallback<T> cleanupCallback,
            int count, int index, short groupId)
        {
            _valueContainer = valueContainer;
            _cancelationRef = cancelationRef;
            _cancelationId = cancelationRef.SourceId;
            _cleanupCallback = cleanupCallback;
            _group = group;
            _count = count;
            _index = index;
            _groupId = groupId;
        }

        private static PromiseAllGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, IList<T> valueContainer, Internal.AllCleanupCallback<T> cleanupCallback)
        {
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseAllGroup<T>(valueContainer ?? new List<T>(), cancelationRef, null, cleanupCallback, 0, 0, 0);
        }

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(out CancelationToken groupCancelationToken, IList<T> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, valueContainer, null);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, IList<T> valueContainer = null)
            => New(sourceCancelationToken, out groupCancelationToken, valueContainer, null);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(out CancelationToken groupCancelationToken,
            Action<T> onCleanup, IList<T> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, onCleanup, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            Action<T> onCleanup, IList<T> valueContainer = null)
            => New(sourceCancelationToken, out groupCancelationToken, valueContainer, AllCleanupCallbackHelper.GetOrCreate(onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New<TCaptureCleanup>(out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T> onCleanup, IList<T> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, cleanupCaptureValue, onCleanup, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New<TCaptureCleanup>(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T> onCleanup, IList<T> valueContainer = null)
            => New(sourceCancelationToken, out groupCancelationToken, valueContainer, AllCleanupCallbackHelper.GetOrCreate(cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(out CancelationToken groupCancelationToken,
            Func<T, Promise> onCleanup, IList<T> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, onCleanup, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            Func<T, Promise> onCleanup, IList<T> valueContainer = null)
            => New(sourceCancelationToken, out groupCancelationToken, valueContainer, AllCleanupCallbackHelper.GetOrCreate(onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New<TCaptureCleanup>(out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T, Promise> onCleanup, IList<T> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, cleanupCaptureValue, onCleanup, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New<TCaptureCleanup>(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T, Promise> onCleanup, IList<T> valueContainer = null)
            => New(sourceCancelationToken, out groupCancelationToken, valueContainer, AllCleanupCallbackHelper.GetOrCreate(cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element and its index if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(out CancelationToken groupCancelationToken,
            Action<T, int> onCleanup, IList<T> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, onCleanup, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element and its index if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            Action<T, int> onCleanup, IList<T> valueContainer = null)
            => New(sourceCancelationToken, out groupCancelationToken, valueContainer, AllCleanupCallbackHelper.GetOrCreate(onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element and its index if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New<TCaptureCleanup>(out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T, int> onCleanup, IList<T> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, cleanupCaptureValue, onCleanup, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked for each resolved element and its index if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New<TCaptureCleanup>(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T, int> onCleanup, IList<T> valueContainer = null)
            => New(sourceCancelationToken, out groupCancelationToken, valueContainer, AllCleanupCallbackHelper.GetOrCreate(cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element and its index if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(out CancelationToken groupCancelationToken,
            Func<T, int, Promise> onCleanup, IList<T> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, onCleanup, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element and its index if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            Func<T, int, Promise> onCleanup, IList<T> valueContainer = null)
            => New(sourceCancelationToken, out groupCancelationToken, valueContainer, AllCleanupCallbackHelper.GetOrCreate(onCleanup));

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element and its index if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New<TCaptureCleanup>(out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T, int, Promise> onCleanup, IList<T> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, cleanupCaptureValue, onCleanup, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked for each resolved element and its index if any promises in this group are canceled or rejected.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New<TCaptureCleanup>(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken,
            TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T, int, Promise> onCleanup, IList<T> valueContainer = null)
            => New(sourceCancelationToken, out groupCancelationToken, valueContainer, AllCleanupCallbackHelper.GetOrCreate(cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseAllGroup<T> Add(Promise<T> promise)
        {
#if PROMISE_DEBUG
            Internal.ValidateArgument(promise, nameof(promise), 1);
#endif
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
                list.SetOrAdd(promise._result, index);
                if (promise._ref == null)
                {
                    _cleanupCallback?.AddResolvedIndex(index);
                }
                else
                {
                    ++count;
                    group.AddPromiseForMerge(promise, index);
                }
                return new PromiseAllGroup<T>(list, cancelationRef, group, _cleanupCallback, count, index + 1, group.Id);
            }

            if (!cancelationRef.TryIncrementSourceId(_cancelationId))
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            // We don't protect the list with a lock, because we ensure this is only used by a single caller.
            list.SetOrAdd(promise._result, index);
            if (promise._ref == null)
            {
                _cleanupCallback?.AddResolvedIndex(index);
                return new PromiseAllGroup<T>(list, cancelationRef, null, _cleanupCallback, 0, index + 1, 0);
            }

            group = Internal.GetOrCreateAllPromiseGroup(cancelationRef, list, _cleanupCallback);
            group.AddPromiseForMerge(promise, index);
            return new PromiseAllGroup<T>(list, cancelationRef, group, _cleanupCallback, 1, index + 1, group.Id);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a list containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
        public Promise<IList<T>> WaitAsync()
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
                _cleanupCallback?.Dispose();
                return Promise.Resolved(list);
            }

            if (!group.TryIncrementId(_groupId))
            {
                Internal.ThrowInvalidAllGroup(1);
            }

            // Make sure list has the same count as promises.
            list.MaybeShrink(index);
            group.MarkReady(count);
            return new Promise<IList<T>>(group, group.Id);
        }
    }
}