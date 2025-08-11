#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    // Promise merge groups use 1 backing reference if only void promises are merged,
    // 2 backing references if a promise of any other type is merged.
    // The first one is to merge the promises before the final type is known,
    // the second one is to realize the actual type from WaitAsync.

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup
    {
        internal readonly Internal.CancelationRef _cancelationRef;
        internal readonly Internal.PromiseRefBase.MergePromiseGroupVoid _group;
        internal readonly Internal.ValueLinkedStack<Internal.MergeCleanupCallback> _cleanupCallbacks;
        internal readonly int _cancelationId;
        internal readonly int _cleanupCount;
        internal readonly uint _count;
        internal readonly short _groupId;
        internal readonly bool _isExtended;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeGroup(Internal.CancelationRef cancelationRef, Internal.ValueLinkedStack<Internal.MergeCleanupCallback> cleanupCallbacks, int cleanupCount, bool isExtended)
            : this(cancelationRef, null, cleanupCallbacks, cleanupCount, 0, 0, isExtended)
        {
        }

        [MethodImpl(Internal.InlineOption)]
        private PromiseMergeGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.MergePromiseGroupVoid group, Internal.ValueLinkedStack<Internal.MergeCleanupCallback> cleanupCallbacks,
            int cleanupCount, uint count, short groupId, bool isExtended)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _cleanupCallbacks = cleanupCallbacks;
            _cleanupCount = cleanupCount;
            _cancelationId = cancelationRef.SourceId;
            _count = count;
            _groupId = groupId;
            _isExtended = isExtended;
        }

        /// <summary>
        /// Get a new <see cref="PromiseMergeGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if the group is rejected or canceled.</param>
        public static PromiseMergeGroup New(out CancelationToken groupCancelationToken)
            => New(CancelationToken.None, out groupCancelationToken);

        /// <summary>
        /// Get a new <see cref="PromiseMergeGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if the group is rejected or canceled.</param>
        public static PromiseMergeGroup New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken)
        {
            var groupCancelationSource = CancelationSource.New(sourceCancelationToken);
            groupCancelationToken = groupCancelationSource.Token;
            return new PromiseMergeGroup(groupCancelationSource._ref, new Internal.ValueLinkedStack<Internal.MergeCleanupCallback>(), 0, false);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup Add(Promise promise)
        {
#if PROMISE_DEBUG
            Internal.ValidateArgument(promise, nameof(promise), 1);
#endif
            var cancelationRef = _cancelationRef;
            var group = _group;
            uint count = _count;
            var isExtended = _isExtended;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_groupId))
                {
                    Internal.ThrowInvalidMergeGroup(1);
                }

                // We don't need to do anything if the ref is null.
                if (promise._ref != null)
                {
                    checked { ++count; }
                    group.AddPromise(promise);
                }
                return new PromiseMergeGroup(cancelationRef, group, _cleanupCallbacks, _cleanupCount, count, group.Id, isExtended);
            }

            if (!cancelationRef.TryIncrementSourceId(_cancelationId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateMergePromiseGroupVoid(cancelationRef);
                group.AddPromise(promise);
                return new PromiseMergeGroup(cancelationRef, group, _cleanupCallbacks, _cleanupCount, 1, group.Id, isExtended);
            }

            return new PromiseMergeGroup(cancelationRef, _cleanupCallbacks, _cleanupCount, isExtended);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1> Add<T1>(Promise<T1> promise)
            => new PromiseMergeGroup<T1>(Merge(promise, 0), promise._result);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1> Add<T1>(Promise<T1> promise, Action<T1> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1> Add<T1, TCaptureCleanup>(Promise<T1> promise, TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T1> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1> Add<T1>(Promise<T1> promise, Func<T1, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1> Add<T1, TCaptureCleanup>(Promise<T1> promise, TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T1, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));
        
        [MethodImpl(Internal.InlineOption)]
        private PromiseMergeGroup<T1> Add<T1>(in Promise<T1> promise, Internal.MergeCleanupCallback cleanupCallback)
            => new PromiseMergeGroup<T1>(Merge(promise, 0, cleanupCallback), promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// </summary>
        /// <remarks>
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// If the group is canceled, the returned promise will be canceled.
        /// Otherwise, the returned promise will be resolved.
        /// </remarks>
        public Promise WaitAsync()
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            var count = _count;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            if (group == null)
            {
                bool canceled = _cancelationRef.IsCanceledUnsafe();
                if (!cancelationRef.TryDispose(_cancelationId))
                {
                    Internal.ThrowInvalidMergeGroup(1);
                }
                return canceled ? Promise.Canceled() : Promise.Resolved();
            }

            if (!group.TryIncrementId(_groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(count);
            return new Promise(group, group.Id);
        }

        internal void DisposeCancelationOrThrow()
        {
            if (!_cancelationRef.TryDispose(_cancelationId))
            {
                Internal.ThrowInvalidMergeGroup(2);
            }
            var cleanupCallbacks = _cleanupCallbacks;
            while (cleanupCallbacks.IsNotEmpty)
            {
                cleanupCallbacks.Pop().Dispose();
            }
        }

        internal Promise<T> CleanupAndGetImmediatePromise<T>(in T value)
        {
            bool canceled = _cancelationRef.IsCanceledUnsafe();
            if (!_cancelationRef.TryDispose(_cancelationId))
            {
                Internal.ThrowInvalidMergeGroup(2);
            }

            var cleanupCallbacks = _cleanupCallbacks;
            if (canceled)
            {
                if (cleanupCallbacks.IsEmpty)
                {
                    return Promise<T>.Canceled();
                }

                // Cleanup, and cancel if none of the cleanup callbacks throw.
                var cleanupGroup = New(out var _);
                do
                {
                    cleanupGroup = cleanupGroup.Add(cleanupCallbacks.Pop().InvokeAndDisposeImmediate());
                } while (cleanupCallbacks.IsNotEmpty);
                return cleanupGroup.WaitAsync()
                    .Then(() => Promise<T>.Canceled());
            }

            while (cleanupCallbacks.IsNotEmpty)
            {
                cleanupCallbacks.Pop().Dispose();
            }
            return Promise.Resolved(value);
        }

        internal PromiseMergeGroup Merge(Promise promise, int index)
        {
#if PROMISE_DEBUG
            Internal.ValidateArgument(promise, nameof(promise), 2);
#endif
            var cancelationRef = _cancelationRef;
            var group = _group;
            uint count = _count;
            var isExtended = _isExtended;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(2);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_groupId))
                {
                    Internal.ThrowInvalidMergeGroup(2);
                }

                // We don't need to do anything if the ref is null.
                if (promise._ref != null)
                {
                    checked { ++count; }
                    group.AddPromiseForMerge(promise, index);
                }
                return new PromiseMergeGroup(cancelationRef, group, _cleanupCallbacks, _cleanupCount, count, group.Id, isExtended);
            }

            if (!cancelationRef.TryIncrementSourceId(_cancelationId))
            {
                Internal.ThrowInvalidMergeGroup(2);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateMergePromiseGroupVoid(cancelationRef);
                group.AddPromiseForMerge(promise, index);
                return new PromiseMergeGroup(cancelationRef, group, _cleanupCallbacks, _cleanupCount, 1, group.Id, isExtended);
            }

            return new PromiseMergeGroup(cancelationRef, _cleanupCallbacks, _cleanupCount, isExtended);
        }

        internal PromiseMergeGroup Merge(Promise promise, int index, Internal.MergeCleanupCallback cleanupCallback)
        {
#if PROMISE_DEBUG
            Internal.ValidateArgument(promise, nameof(promise), 2);
#endif
            var cancelationRef = _cancelationRef;
            var group = _group;
            var cleanupCallbacks = _cleanupCallbacks;
            uint count = _count;
            var isExtended = _isExtended;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(2);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_groupId))
                {
                    Internal.ThrowInvalidMergeGroup(2);
                }

                // We don't need to do anything if the ref is null.
                if (promise._ref != null)
                {
                    checked { ++count; }
                    group.AddPromiseForMerge(promise, index);
                }
                cleanupCallbacks.Push(cleanupCallback);
                return new PromiseMergeGroup(cancelationRef, group, cleanupCallbacks, unchecked(_cleanupCount + 1), count, group.Id, isExtended);
            }

            if (!cancelationRef.TryIncrementSourceId(_cancelationId))
            {
                Internal.ThrowInvalidMergeGroup(2);
            }

            cleanupCallbacks.Push(cleanupCallback);
            if (promise._ref != null)
            {
                group = Internal.GetOrCreateMergePromiseGroupVoid(cancelationRef);
                group.AddPromiseForMerge(promise, index);
                return new PromiseMergeGroup(cancelationRef, group, cleanupCallbacks, unchecked(_cleanupCount + 1), 1, group.Id, isExtended);
            }

            return new PromiseMergeGroup(cancelationRef, cleanupCallbacks, unchecked(_cleanupCount + 1), isExtended);
        }

        internal PromiseMergeGroup MergeForExtension(Promise promise)
        {
            // We don't do any validation checks here, because they were already done in the caller.
            var group = Internal.GetOrCreateMergePromiseGroupVoid(_cancelationRef);
            group.AddPromiseForMerge(promise, 0);
            // The previous promise stores the head to its cleanup stack, the new promise adds on top of the stack without modifying the previous stack (the items are a linked-list).
            // The cleanup count is reset to 0 so when the new promise is complete, it won't double-prepare the previous cleanups.
            var cleanupCallbacks = new Internal.ValueLinkedStack<Internal.MergeCleanupCallback>(_cleanupCallbacks.Peek());
            return new PromiseMergeGroup(_cancelationRef, group, cleanupCallbacks, 0, 1, group.Id, true);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeGroup(PromiseMergeGroup mergeGroup, in T1 value)
        {
            _mergeGroup = mergeGroup;
            _value = value;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1> Add(Promise promise)
            => new PromiseMergeGroup<T1>(_mergeGroup.Add(promise), _value);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2> Add<T2>(Promise<T2> promise)
            => new PromiseMergeGroup<T1, T2>(_mergeGroup.Merge(promise, 1), (_value, promise._result));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2> Add<T2>(Promise<T2> promise, Action<T2> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2> Add<T2, TCaptureCleanup>(Promise<T2> promise, TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T2> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2> Add<T2>(Promise<T2> promise, Func<T2, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2> Add<T2, TCaptureCleanup>(Promise<T2> promise, TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T2, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        [MethodImpl(Internal.InlineOption)]
        private PromiseMergeGroup<T1, T2> Add<T2>(in Promise<T2> promise, Internal.MergeCleanupCallback cleanupCallback)
            => new PromiseMergeGroup<T1, T2>(_mergeGroup.Merge(promise, 1, cleanupCallback), (_value, promise._result));

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// </summary>
        /// <remarks>
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// If the group is canceled, the returned promise will be canceled.
        /// Otherwise, the returned promise will be resolved the resolved value.
        /// </remarks>
        public Promise<T1> WaitAsync()
        {
            var mergeGroup = _mergeGroup;
            if (mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            var group = mergeGroup._group;
            if (group == null)
            {
                // Equivalent to mergeGroup.CleanupAndGetImmediatePromise(_value), but more efficient since we know there can be max 1 cleanup callback here.
                bool canceled = mergeGroup._cancelationRef.IsCanceledUnsafe();
                if (!mergeGroup._cancelationRef.TryDispose(mergeGroup._cancelationId))
                {
                    Internal.ThrowInvalidMergeGroup(1);
                }

                var cleanupCallbacks = mergeGroup._cleanupCallbacks;
                if (canceled)
                {
                    if (cleanupCallbacks.IsEmpty)
                    {
                        return Promise<T1>.Canceled();
                    }

                    // Cleanup, and cancel if the cleanup callback does not throw.
                    // We know there can only be 1 cleanup callback here.
                    return cleanupCallbacks.Pop().InvokeAndDisposeImmediate()
                        .Then(() => Promise<T1>.Canceled());
                }

                // We know there can only be max 1 cleanup callback here.
                if (cleanupCallbacks.IsNotEmpty)
                {
                    cleanupCallbacks.Pop().Dispose();
                }
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            return Internal.NewMergePromiseGroup(group, _value, Promise.MergeResultFuncs.GetOne<T1>(), false, true, mergeGroup._cleanupCallbacks, mergeGroup._cleanupCount);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeGroup(PromiseMergeGroup mergeGroup, in (T1, T2) value)
        {
            _mergeGroup = mergeGroup;
            _value = value;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2> Add(Promise promise)
            => new PromiseMergeGroup<T1, T2>(_mergeGroup.Add(promise), _value);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3> Add<T3>(Promise<T3> promise)
            => new PromiseMergeGroup<T1, T2, T3>(_mergeGroup.Merge(promise, 2), (_value.Item1, _value.Item2, promise._result));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3> Add<T3>(Promise<T3> promise, Action<T3> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3> Add<T3, TCaptureCleanup>(Promise<T3> promise, TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T3> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3> Add<T3>(Promise<T3> promise, Func<T3, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3> Add<T3, TCaptureCleanup>(Promise<T3> promise, TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T3, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        [MethodImpl(Internal.InlineOption)]
        private PromiseMergeGroup<T1, T2, T3> Add<T3>(in Promise<T3> promise, Internal.MergeCleanupCallback cleanupCallback)
            => new PromiseMergeGroup<T1, T2, T3>(_mergeGroup.Merge(promise, 1, cleanupCallback), (_value.Item1, _value.Item2, promise._result));

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// </summary>
        /// <remarks>
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// If the group is canceled, the returned promise will be canceled.
        /// Otherwise, the returned promise will be resolved with a tuple containing the resolved values of each of the promises.
        /// </remarks>
        public Promise<(T1, T2)> WaitAsync()
        {
            var mergeGroup = _mergeGroup;
            if (mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            var group = mergeGroup._group;
            if (group == null)
            {
                return mergeGroup.CleanupAndGetImmediatePromise(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            return Internal.NewMergePromiseGroup(group, _value, Promise.MergeResultFuncs.GetTwo<T1, T2>(), mergeGroup._isExtended, true, mergeGroup._cleanupCallbacks, mergeGroup._cleanupCount);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2, T3>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeGroup(PromiseMergeGroup mergeGroup, in (T1, T2, T3) value)
        {
            _mergeGroup = mergeGroup;
            _value = value;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3> Add(Promise promise)
            => new PromiseMergeGroup<T1, T2, T3>(_mergeGroup.Add(promise), _value);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4> Add<T4>(Promise<T4> promise)
            => new PromiseMergeGroup<T1, T2, T3, T4>(_mergeGroup.Merge(promise, 3), (_value.Item1, _value.Item2, _value.Item3, promise._result));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4> Add<T4>(Promise<T4> promise, Action<T4> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4> Add<T4, TCaptureCleanup>(Promise<T4> promise, TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T4> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4> Add<T4>(Promise<T4> promise, Func<T4, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4> Add<T4, TCaptureCleanup>(Promise<T4> promise, TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T4, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        [MethodImpl(Internal.InlineOption)]
        private PromiseMergeGroup<T1, T2, T3, T4> Add<T4>(in Promise<T4> promise, Internal.MergeCleanupCallback cleanupCallback)
            => new PromiseMergeGroup<T1, T2, T3, T4>(_mergeGroup.Merge(promise, 1, cleanupCallback), (_value.Item1, _value.Item2, _value.Item3, promise._result));

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// </summary>
        /// <remarks>
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// If the group is canceled, the returned promise will be canceled.
        /// Otherwise, the returned promise will be resolved with a tuple containing the resolved values of each of the promises.
        /// </remarks>
        public Promise<(T1, T2, T3)> WaitAsync()
        {
            var mergeGroup = _mergeGroup;
            if (mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            var group = mergeGroup._group;
            if (group == null)
            {
                return mergeGroup.CleanupAndGetImmediatePromise(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            return Internal.NewMergePromiseGroup(group, _value, Promise.MergeResultFuncs.GetThree<T1, T2, T3>(), mergeGroup._isExtended, true, mergeGroup._cleanupCallbacks, mergeGroup._cleanupCount);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2, T3, T4>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeGroup(PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4) value)
        {
            _mergeGroup = mergeGroup;
            _value = value;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4> Add(Promise promise)
            => new PromiseMergeGroup<T1, T2, T3, T4>(_mergeGroup.Add(promise), _value);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5> Add<T5>(Promise<T5> promise)
            => new PromiseMergeGroup<T1, T2, T3, T4, T5>(_mergeGroup.Merge(promise, 4), (_value.Item1, _value.Item2, _value.Item3, _value.Item4, promise._result));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5> Add<T5>(Promise<T5> promise, Action<T5> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5> Add<T5, TCaptureCleanup>(Promise<T5> promise, TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T5> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5> Add<T5>(Promise<T5> promise, Func<T5, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5> Add<T5, TCaptureCleanup>(Promise<T5> promise, TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T5, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        [MethodImpl(Internal.InlineOption)]
        private PromiseMergeGroup<T1, T2, T3, T4, T5> Add<T5>(in Promise<T5> promise, Internal.MergeCleanupCallback cleanupCallback)
            => new PromiseMergeGroup<T1, T2, T3, T4, T5>(_mergeGroup.Merge(promise, 1, cleanupCallback), (_value.Item1, _value.Item2, _value.Item3, _value.Item4, promise._result));

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// </summary>
        /// <remarks>
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// If the group is canceled, the returned promise will be canceled.
        /// Otherwise, the returned promise will be resolved with a tuple containing the resolved values of each of the promises.
        /// </remarks>
        public Promise<(T1, T2, T3, T4)> WaitAsync()
        {
            var mergeGroup = _mergeGroup;
            if (mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            var group = mergeGroup._group;
            if (group == null)
            {
                return mergeGroup.CleanupAndGetImmediatePromise(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            return Internal.NewMergePromiseGroup(group, _value, Promise.MergeResultFuncs.GetFour<T1, T2, T3, T4>(), mergeGroup._isExtended, true, mergeGroup._cleanupCallbacks, mergeGroup._cleanupCount);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2, T3, T4, T5>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4, T5) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeGroup(PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4, T5) value)
        {
            _mergeGroup = mergeGroup;
            _value = value;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5> Add(Promise promise)
            => new PromiseMergeGroup<T1, T2, T3, T4, T5>(_mergeGroup.Add(promise), _value);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6> Add<T6>(Promise<T6> promise)
            => new PromiseMergeGroup<T1, T2, T3, T4, T5, T6>(_mergeGroup.Merge(promise, 5), (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, promise._result));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6> Add<T6>(Promise<T6> promise, Action<T6> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6> Add<T6, TCaptureCleanup>(Promise<T6> promise, TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T6> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6> Add<T6>(Promise<T6> promise, Func<T6, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6> Add<T6, TCaptureCleanup>(Promise<T6> promise, TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T6, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        [MethodImpl(Internal.InlineOption)]
        private PromiseMergeGroup<T1, T2, T3, T4, T5, T6> Add<T6>(in Promise<T6> promise, Internal.MergeCleanupCallback cleanupCallback)
            => new PromiseMergeGroup<T1, T2, T3, T4, T5, T6>(_mergeGroup.Merge(promise, 1, cleanupCallback), (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, promise._result));

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// </summary>
        /// <remarks>
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// If the group is canceled, the returned promise will be canceled.
        /// Otherwise, the returned promise will be resolved with a tuple containing the resolved values of each of the promises.
        /// </remarks>
        public Promise<(T1, T2, T3, T4, T5)> WaitAsync()
        {
            var mergeGroup = _mergeGroup;
            if (mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            var group = mergeGroup._group;
            if (group == null)
            {
                return mergeGroup.CleanupAndGetImmediatePromise(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            return Internal.NewMergePromiseGroup(group, _value, Promise.MergeResultFuncs.GetFive<T1, T2, T3, T4, T5>(), mergeGroup._isExtended, true, mergeGroup._cleanupCallbacks, mergeGroup._cleanupCount);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2, T3, T4, T5, T6>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4, T5, T6) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeGroup(PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4, T5, T6) value)
        {
            _mergeGroup = mergeGroup;
            _value = value;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6> Add(Promise promise)
            => new PromiseMergeGroup<T1, T2, T3, T4, T5, T6>(_mergeGroup.Add(promise), _value);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7> Add<T7>(Promise<T7> promise)
            => new PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7>(_mergeGroup.Merge(promise, 6), (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, _value.Item6, promise._result));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7> Add<T7>(Promise<T7> promise, Action<T7> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7> Add<T7, TCaptureCleanup>(Promise<T7> promise, TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T7> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7> Add<T7>(Promise<T7> promise, Func<T7, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, onCleanup));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7> Add<T7, TCaptureCleanup>(Promise<T7> promise, TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T7, Promise> onCleanup)
            => Add(promise, MergeCleanupCallbackHelper.GetOrCreate(promise, cleanupCaptureValue, onCleanup));

        [MethodImpl(Internal.InlineOption)]
        private PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7> Add<T7>(in Promise<T7> promise, Internal.MergeCleanupCallback cleanupCallback)
            => new PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7>(_mergeGroup.Merge(promise, 1, cleanupCallback), (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, _value.Item6, promise._result));

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// </summary>
        /// <remarks>
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// If the group is canceled, the returned promise will be canceled.
        /// Otherwise, the returned promise will be resolved with a tuple containing the resolved values of each of the promises.
        /// </remarks>
        public Promise<(T1, T2, T3, T4, T5, T6)> WaitAsync()
        {
            var mergeGroup = _mergeGroup;
            if (mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            var group = mergeGroup._group;
            if (group == null)
            {
                return mergeGroup.CleanupAndGetImmediatePromise(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            return Internal.NewMergePromiseGroup(group, _value, Promise.MergeResultFuncs.GetSix<T1, T2, T3, T4, T5, T6>(), mergeGroup._isExtended, true, mergeGroup._cleanupCallbacks, mergeGroup._cleanupCount);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4, T5, T6, T7) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeGroup(PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4, T5, T6, T7) value)
        {
            _mergeGroup = mergeGroup;
            _value = value;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7> Add(Promise promise)
            => new PromiseMergeGroup<T1, T2, T3, T4, T5, T6, T7>(_mergeGroup.Add(promise), _value);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        // Merging more than 7 types should be fairly rare. To support N types greater than 7, we just wrap it in another group.
        public PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7), T8> Add<T8>(Promise<T8> promise)
            => new PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7)>(SetupExtension(), _value).Add(promise);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7), T8> Add<T8>(Promise<T8> promise, Action<T8> onCleanup)
            => new PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7)>(SetupExtension(), _value).Add(promise, onCleanup);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7), T8> Add<T8, TCaptureCleanup>(Promise<T8> promise, TCaptureCleanup cleanupCaptureValue, Action<TCaptureCleanup, T8> onCleanup)
            => new PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7)>(SetupExtension(), _value).Add(promise, cleanupCaptureValue, onCleanup);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7), T8> Add<T8>(Promise<T8> promise, Func<T8, Promise> onCleanup)
            => new PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7)>(SetupExtension(), _value).Add(promise, onCleanup);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        /// <param name="cleanupCaptureValue">The captured value that will be passed to <paramref name="onCleanup"/>.</param>
        /// <param name="onCleanup">The async delegate that will be invoked if the <paramref name="promise"/> is resolved and any other promise in this group is canceled or rejected.</param>
        public PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7), T8> Add<T8, TCaptureCleanup>(Promise<T8> promise, TCaptureCleanup cleanupCaptureValue, Func<TCaptureCleanup, T8, Promise> onCleanup)
            => new PromiseMergeGroup<(T1, T2, T3, T4, T5, T6, T7)>(SetupExtension(), _value).Add(promise, cleanupCaptureValue, onCleanup);

        private PromiseMergeGroup SetupExtension()
        {
            var mergeGroup = _mergeGroup;
            // We're wrapping this in another group, so we just increment its SourceId instead of disposing.
            if (mergeGroup._cancelationRef == null || !mergeGroup._cancelationRef.TryIncrementSourceId(mergeGroup._cancelationId))
            {
                Internal.ThrowInvalidMergeGroup(2);
            }

            var group = mergeGroup._group;
            if (group == null)
            {
                return new PromiseMergeGroup(mergeGroup._cancelationRef, mergeGroup._cleanupCallbacks, mergeGroup._cleanupCount, true);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(2);
            }

            group.MarkReady(mergeGroup._count);
            var promise = Internal.NewMergePromiseGroup(group, _value, Promise.MergeResultFuncs.GetSeven<T1, T2, T3, T4, T5, T6, T7>(), mergeGroup._isExtended, false, mergeGroup._cleanupCallbacks, mergeGroup._cleanupCount);

            return mergeGroup.MergeForExtension(promise);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// </summary>
        /// <remarks>
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// If the group is canceled, the returned promise will be canceled.
        /// Otherwise, the returned promise will be resolved with a tuple containing the resolved values of each of the promises.
        /// </remarks>
        public Promise<(T1, T2, T3, T4, T5, T6, T7)> WaitAsync()
        {
            var mergeGroup = _mergeGroup;
            if (mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            var group = mergeGroup._group;
            if (group == null)
            {
                return mergeGroup.CleanupAndGetImmediatePromise(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            return Internal.NewMergePromiseGroup(group, _value, Promise.MergeResultFuncs.GetSeven<T1, T2, T3, T4, T5, T6, T7>(), mergeGroup._isExtended, true, mergeGroup._cleanupCallbacks, mergeGroup._cleanupCount);
        }
    }
}