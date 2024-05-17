#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    // Promise merge results groups use 2 backing references.
    // The first one is to merge the promises before the final type is known,
    // the second one is to realize the actual type from WaitAsync.

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup
    {
        private readonly PromiseMergeGroup _mergeGroup;

        [MethodImpl(Internal.InlineOption)]
        private PromiseMergeResultsGroup(in PromiseMergeGroup mergeGroup)
        {
            _mergeGroup = mergeGroup;
        }

        /// <summary>
        /// Get a new <see cref="PromiseMergeResultsGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        public static PromiseMergeResultsGroup New(out CancelationToken groupCancelationToken)
            => New(CancelationToken.None, out groupCancelationToken);

        /// <summary>
        /// Get a new <see cref="PromiseMergeResultsGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        public static PromiseMergeResultsGroup New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken)
            => new PromiseMergeResultsGroup(PromiseMergeGroup.New(sourceCancelationToken, out groupCancelationToken));

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroup<Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 0),
                Promise.ResultContainer.Resolved,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<Promise<T1>.ResultContainer> Add<T1>(Promise<T1> promise)
            => new PromiseMergeResultsGroup<Promise<T1>.ResultContainer>(
                _mergeGroup.Merge(promise, 0),
                promise._result,
                Promise.MergeResultFuncs.GetMergeResult<T1>());
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1>
    {
        // The generic type can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegate.
        private static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroup(in PromiseMergeGroup mergeGroup, in T1 value, Internal.GetResultContainerDelegate<T1> getResult1Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult1Delegate = getResult1Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroup<T1, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 1),
                (_value, Promise.ResultContainer.Resolved),
                s_getResult1Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, Promise<T2>.ResultContainer> Add<T2>(Promise<T2> promise)
            => new PromiseMergeResultsGroup<T1, Promise<T2>.ResultContainer>(
                _mergeGroup.Merge(promise, 1),
                (_value, promise._result),
                s_getResult1Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T2>());
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroup(in PromiseMergeGroup mergeGroup, in (T1, T2) value,
            Internal.GetResultContainerDelegate<T1> getResult1Delegate,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult1Delegate = getResult1Delegate;
            s_getResult2Delegate = getResult2Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroup<T1, T2, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 2),
                (_value.Item1, _value.Item2, Promise.ResultContainer.Resolved),
                s_getResult1Delegate,
                s_getResult2Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, Promise<T3>.ResultContainer> Add<T3>(Promise<T3> promise)
            => new PromiseMergeResultsGroup<T1, T2, Promise<T3>.ResultContainer>(
                _mergeGroup.Merge(promise, 2),
                (_value.Item1, _value.Item2, promise._result),
                s_getResult1Delegate,
                s_getResult2Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T3>());

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    s_getResult1Delegate,
                    s_getResult2Delegate),
                false
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
        private static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroup(in PromiseMergeGroup mergeGroup, in (T1, T2, T3) value,
            Internal.GetResultContainerDelegate<T1> getResult1Delegate,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate,
            Internal.GetResultContainerDelegate<T3> getResult3Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult1Delegate = getResult1Delegate;
            s_getResult2Delegate = getResult2Delegate;
            s_getResult3Delegate = getResult3Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 3),
                (_value.Item1, _value.Item2, _value.Item3, Promise.ResultContainer.Resolved),
                s_getResult1Delegate,
                s_getResult2Delegate,
                s_getResult3Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, Promise<T4>.ResultContainer> Add<T4>(Promise<T4> promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, Promise<T4>.ResultContainer>(
                _mergeGroup.Merge(promise, 3),
                (_value.Item1, _value.Item2, _value.Item3, promise._result),
                s_getResult1Delegate,
                s_getResult2Delegate,
                s_getResult3Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T4>());

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    s_getResult1Delegate,
                    s_getResult2Delegate,
                    s_getResult3Delegate),
                false
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
        private static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
        private static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroup(in PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4) value,
            Internal.GetResultContainerDelegate<T1> getResult1Delegate,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate,
            Internal.GetResultContainerDelegate<T3> getResult3Delegate,
            Internal.GetResultContainerDelegate<T4> getResult4Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult1Delegate = getResult1Delegate;
            s_getResult2Delegate = getResult2Delegate;
            s_getResult3Delegate = getResult3Delegate;
            s_getResult4Delegate = getResult4Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, T4, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 4),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, Promise.ResultContainer.Resolved),
                s_getResult1Delegate,
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, Promise<T5>.ResultContainer> Add<T5>(Promise<T5> promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, T4, Promise<T5>.ResultContainer>(
                _mergeGroup.Merge(promise, 4),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, promise._result),
                s_getResult1Delegate,
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T5>());

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    s_getResult1Delegate,
                    s_getResult2Delegate,
                    s_getResult3Delegate,
                    s_getResult4Delegate),
                false
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4, T5>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
        private static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
        private static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;
        private static Internal.GetResultContainerDelegate<T5> s_getResult5Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4, T5) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroup(in PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4, T5) value,
            Internal.GetResultContainerDelegate<T1> getResult1Delegate,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate,
            Internal.GetResultContainerDelegate<T3> getResult3Delegate,
            Internal.GetResultContainerDelegate<T4> getResult4Delegate,
            Internal.GetResultContainerDelegate<T5> getResult5Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult1Delegate = getResult1Delegate;
            s_getResult2Delegate = getResult2Delegate;
            s_getResult3Delegate = getResult3Delegate;
            s_getResult4Delegate = getResult4Delegate;
            s_getResult5Delegate = getResult5Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, T5, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, T4, T5, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 5),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, Promise.ResultContainer.Resolved),
                s_getResult1Delegate,
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                s_getResult5Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, T5, Promise<T6>.ResultContainer> Add<T6>(Promise<T6> promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, T4, T5, Promise<T6>.ResultContainer>(
                _mergeGroup.Merge(promise, 5),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, promise._result),
                s_getResult1Delegate,
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                s_getResult5Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T6>());

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    s_getResult1Delegate,
                    s_getResult2Delegate,
                    s_getResult3Delegate,
                    s_getResult4Delegate,
                    s_getResult5Delegate),
                false
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4, T5)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
        private static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
        private static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;
        private static Internal.GetResultContainerDelegate<T5> s_getResult5Delegate;
        private static Internal.GetResultContainerDelegate<T6> s_getResult6Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4, T5, T6) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroup(in PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4, T5, T6) value,
            Internal.GetResultContainerDelegate<T1> getResult1Delegate,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate,
            Internal.GetResultContainerDelegate<T3> getResult3Delegate,
            Internal.GetResultContainerDelegate<T4> getResult4Delegate,
            Internal.GetResultContainerDelegate<T5> getResult5Delegate,
            Internal.GetResultContainerDelegate<T6> getResult6Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult1Delegate = getResult1Delegate;
            s_getResult2Delegate = getResult2Delegate;
            s_getResult3Delegate = getResult3Delegate;
            s_getResult4Delegate = getResult4Delegate;
            s_getResult5Delegate = getResult5Delegate;
            s_getResult6Delegate = getResult6Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 6),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, _value.Item6, Promise.ResultContainer.Resolved),
                s_getResult1Delegate,
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                s_getResult5Delegate,
                s_getResult6Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6, Promise<T7>.ResultContainer> Add<T7>(Promise<T7> promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6, Promise<T7>.ResultContainer>(
                _mergeGroup.Merge(promise, 6),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, _value.Item6, promise._result),
                s_getResult1Delegate,
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                s_getResult5Delegate,
                s_getResult6Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T7>());

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    s_getResult1Delegate,
                    s_getResult2Delegate,
                    s_getResult3Delegate,
                    s_getResult4Delegate,
                    s_getResult5Delegate,
                    s_getResult6Delegate),
                false
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4, T5, T6)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6, T7>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
        private static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
        private static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;
        private static Internal.GetResultContainerDelegate<T5> s_getResult5Delegate;
        private static Internal.GetResultContainerDelegate<T6> s_getResult6Delegate;
        private static Internal.GetResultContainerDelegate<T7> s_getResult7Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4, T5, T6, T7) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroup(in PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4, T5, T6, T7) value,
            Internal.GetResultContainerDelegate<T1> getResult1Delegate,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate,
            Internal.GetResultContainerDelegate<T3> getResult3Delegate,
            Internal.GetResultContainerDelegate<T4> getResult4Delegate,
            Internal.GetResultContainerDelegate<T5> getResult5Delegate,
            Internal.GetResultContainerDelegate<T6> getResult6Delegate,
            Internal.GetResultContainerDelegate<T7> getResult7Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult1Delegate = getResult1Delegate;
            s_getResult2Delegate = getResult2Delegate;
            s_getResult3Delegate = getResult3Delegate;
            s_getResult4Delegate = getResult4Delegate;
            s_getResult5Delegate = getResult5Delegate;
            s_getResult6Delegate = getResult6Delegate;
            s_getResult7Delegate = getResult7Delegate;
        }

        // Merging more than 7 types should be fairly rare. To support N types greater than 7, we use PromiseMergeResultsGroupExtended.
        // We use PromiseMergeResultsGroupExtended instead of wrapping it in another PromiseMergeResultsGroup to avoid any issues with users combining ValueTuples of ResultContainers,
        // so that the GetResultContainerDelegates will always be correct (they are stored statically per type).

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<(T1, T2, T3, T4, T5, T6, T7), Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroupExtended<(T1, T2, T3, T4, T5, T6, T7), Promise.ResultContainer>
            (
                SetupExtension().Merge(promise, 1),
                (_value, Promise.ResultContainer.Resolved),
                Promise.MergeResultFuncs.GetMergeResultVoid()
            );

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<(T1, T2, T3, T4, T5, T6, T7), Promise<T8>.ResultContainer> Add<T8>(Promise<T8> promise)
            => new PromiseMergeResultsGroupExtended<(T1, T2, T3, T4, T5, T6, T7), Promise<T8>.ResultContainer>
            (
                SetupExtension().Merge(promise, 1),
                (_value, promise._result),
                Promise.MergeResultFuncs.GetMergeResult<T8>()
            );

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
                return new PromiseMergeGroup(mergeGroup._cancelationRef, true);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(2);
            }

            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    s_getResult1Delegate,
                    s_getResult2Delegate,
                    s_getResult3Delegate,
                    s_getResult4Delegate,
                    s_getResult5Delegate,
                    s_getResult6Delegate,
                    s_getResult7Delegate),
                false
                );
            group.HookupNewPromise(group.Id, promise);

            return new PromiseMergeGroup(mergeGroup._cancelationRef, true)
                .MergeForExtension(promise, promise.Id);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    s_getResult1Delegate,
                    s_getResult2Delegate,
                    s_getResult3Delegate,
                    s_getResult4Delegate,
                    s_getResult5Delegate,
                    s_getResult6Delegate,
                    s_getResult7Delegate),
                false
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4, T5, T6, T7)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroupExtended(in PromiseMergeGroup mergeGroup, in (T1, T2) value,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult2Delegate = getResult2Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 2),
                (_value.Item1, _value.Item2, Promise.ResultContainer.Resolved),
                s_getResult2Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, Promise<T3>.ResultContainer> Add<T3>(Promise<T3> promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, Promise<T3>.ResultContainer>(
                _mergeGroup.Merge(promise, 2),
                (_value.Item1, _value.Item2, promise._result),
                s_getResult2Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T3>());

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    Promise.MergeResultFuncs.GetMergeValue<T1>(),
                    s_getResult2Delegate),
                true
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2, T3>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
        private static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroupExtended(in PromiseMergeGroup mergeGroup, in (T1, T2, T3) value,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate,
            Internal.GetResultContainerDelegate<T3> getResult3Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult2Delegate = getResult2Delegate;
            s_getResult3Delegate = getResult3Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 3),
                (_value.Item1, _value.Item2, _value.Item3, Promise.ResultContainer.Resolved),
                s_getResult2Delegate,
                s_getResult3Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, Promise<T4>.ResultContainer> Add<T4>(Promise<T4> promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, Promise<T4>.ResultContainer>(
                _mergeGroup.Merge(promise, 3),
                (_value.Item1, _value.Item2, _value.Item3, promise._result),
                s_getResult2Delegate,
                s_getResult3Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T4>());

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    Promise.MergeResultFuncs.GetMergeValue<T1>(),
                    s_getResult2Delegate,
                    s_getResult3Delegate),
                true
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2, T3, T4>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
        private static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
        private static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroupExtended(in PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4) value,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate,
            Internal.GetResultContainerDelegate<T3> getResult3Delegate,
            Internal.GetResultContainerDelegate<T4> getResult4Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult2Delegate = getResult2Delegate;
            s_getResult3Delegate = getResult3Delegate;
            s_getResult4Delegate = getResult4Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, T4, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, T4, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 4),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, Promise.ResultContainer.Resolved),
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, T4, Promise<T5>.ResultContainer> Add<T5>(Promise<T5> promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, T4, Promise<T5>.ResultContainer>(
                _mergeGroup.Merge(promise, 4),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, promise._result),
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T5>());

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    Promise.MergeResultFuncs.GetMergeValue<T1>(),
                    s_getResult2Delegate,
                    s_getResult3Delegate,
                    s_getResult4Delegate),
                true
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
        private static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
        private static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;
        private static Internal.GetResultContainerDelegate<T5> s_getResult5Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4, T5) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroupExtended(in PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4, T5) value,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate,
            Internal.GetResultContainerDelegate<T3> getResult3Delegate,
            Internal.GetResultContainerDelegate<T4> getResult4Delegate,
            Internal.GetResultContainerDelegate<T5> getResult5Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult2Delegate = getResult2Delegate;
            s_getResult3Delegate = getResult3Delegate;
            s_getResult4Delegate = getResult4Delegate;
            s_getResult5Delegate = getResult5Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 5),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, Promise.ResultContainer.Resolved),
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                s_getResult5Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, Promise<T6>.ResultContainer> Add<T6>(Promise<T6> promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, Promise<T6>.ResultContainer>(
                _mergeGroup.Merge(promise, 5),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, promise._result),
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                s_getResult5Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T6>());

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    Promise.MergeResultFuncs.GetMergeValue<T1>(),
                    s_getResult2Delegate,
                    s_getResult3Delegate,
                    s_getResult4Delegate,
                    s_getResult5Delegate),
                true
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4, T5)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
        private static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
        private static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;
        private static Internal.GetResultContainerDelegate<T5> s_getResult5Delegate;
        private static Internal.GetResultContainerDelegate<T6> s_getResult6Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4, T5, T6) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroupExtended(in PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4, T5, T6) value,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate,
            Internal.GetResultContainerDelegate<T3> getResult3Delegate,
            Internal.GetResultContainerDelegate<T4> getResult4Delegate,
            Internal.GetResultContainerDelegate<T5> getResult5Delegate,
            Internal.GetResultContainerDelegate<T6> getResult6Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult2Delegate = getResult2Delegate;
            s_getResult3Delegate = getResult3Delegate;
            s_getResult4Delegate = getResult4Delegate;
            s_getResult5Delegate = getResult5Delegate;
            s_getResult6Delegate = getResult6Delegate;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6, Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6, Promise.ResultContainer>(
                _mergeGroup.Merge(promise, 6),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, _value.Item6, Promise.ResultContainer.Resolved),
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                s_getResult5Delegate,
                s_getResult6Delegate,
                Promise.MergeResultFuncs.GetMergeResultVoid());

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6, Promise<T7>.ResultContainer> Add<T7>(Promise<T7> promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6, Promise<T7>.ResultContainer>(
                _mergeGroup.Merge(promise, 6),
                (_value.Item1, _value.Item2, _value.Item3, _value.Item4, _value.Item5, _value.Item6, promise._result),
                s_getResult2Delegate,
                s_getResult3Delegate,
                s_getResult4Delegate,
                s_getResult5Delegate,
                s_getResult6Delegate,
                Promise.MergeResultFuncs.GetMergeResult<T7>());

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    Promise.MergeResultFuncs.GetMergeValue<T1>(),
                    s_getResult2Delegate,
                    s_getResult3Delegate,
                    s_getResult4Delegate,
                    s_getResult5Delegate,
                    s_getResult6Delegate),
                true
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4, T5, T6)>(promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises of one or more types and yield their results.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6, T7>
    {
        // The generic types can be Promise<T>.ResultContainer, or Promise.ResultContainer.
        // In order to resolve the correct type, we have to store the GetResultContainerDelegates.
        private static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
        private static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
        private static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;
        private static Internal.GetResultContainerDelegate<T5> s_getResult5Delegate;
        private static Internal.GetResultContainerDelegate<T6> s_getResult6Delegate;
        private static Internal.GetResultContainerDelegate<T7> s_getResult7Delegate;

        private readonly PromiseMergeGroup _mergeGroup;
        private readonly (T1, T2, T3, T4, T5, T6, T7) _value;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeResultsGroupExtended(in PromiseMergeGroup mergeGroup, in (T1, T2, T3, T4, T5, T6, T7) value,
            Internal.GetResultContainerDelegate<T2> getResult2Delegate,
            Internal.GetResultContainerDelegate<T3> getResult3Delegate,
            Internal.GetResultContainerDelegate<T4> getResult4Delegate,
            Internal.GetResultContainerDelegate<T5> getResult5Delegate,
            Internal.GetResultContainerDelegate<T6> getResult6Delegate,
            Internal.GetResultContainerDelegate<T7> getResult7Delegate)
        {
            _mergeGroup = mergeGroup;
            _value = value;
            s_getResult2Delegate = getResult2Delegate;
            s_getResult3Delegate = getResult3Delegate;
            s_getResult4Delegate = getResult4Delegate;
            s_getResult5Delegate = getResult5Delegate;
            s_getResult6Delegate = getResult6Delegate;
            s_getResult7Delegate = getResult7Delegate;
        }

        // Merging more than 13 types should be rare. To support N types greater than 13, we wrap it in another PromiseMergeResultsGroupExtended.

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<(T1, T2, T3, T4, T5, T6, T7), Promise.ResultContainer> Add(Promise promise)
            => new PromiseMergeResultsGroupExtended<(T1, T2, T3, T4, T5, T6, T7), Promise.ResultContainer>
            (
                SetupExtension().Merge(promise, 1),
                (_value, Promise.ResultContainer.Resolved),
                Promise.MergeResultFuncs.GetMergeResultVoid()
            );

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<(T1, T2, T3, T4, T5, T6, T7), Promise<T8>.ResultContainer> Add<T8>(Promise<T8> promise)
            => new PromiseMergeResultsGroupExtended<(T1, T2, T3, T4, T5, T6, T7), Promise<T8>.ResultContainer>
            (
                SetupExtension().Merge(promise, 1),
                (_value, promise._result),
                Promise.MergeResultFuncs.GetMergeResult<T8>()
            );

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
                return new PromiseMergeGroup(mergeGroup._cancelationRef, true);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(2);
            }

            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    Promise.MergeResultFuncs.GetMergeValue<T1>(),
                    s_getResult2Delegate,
                    s_getResult3Delegate,
                    s_getResult4Delegate,
                    s_getResult5Delegate,
                    s_getResult6Delegate,
                    s_getResult7Delegate),
                true
                );
            group.HookupNewPromise(group.Id, promise);

            return new PromiseMergeGroup(mergeGroup._cancelationRef, true)
                .MergeForExtension(promise, promise.Id);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
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
                mergeGroup.DisposeCancelationOrThrow();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._groupId))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(_value,
                Promise.MergeResultFuncs.GetCombinedMerger(
                    Promise.MergeResultFuncs.GetMergeValue<T1>(),
                    s_getResult2Delegate,
                    s_getResult3Delegate,
                    s_getResult4Delegate,
                    s_getResult5Delegate,
                    s_getResult6Delegate,
                    s_getResult7Delegate),
                true
                );
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4, T5, T6, T7)>(promise, promise.Id);
        }
    }

    partial struct Promise
    {
        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class MergeResult<T>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref Promise<T>.ResultContainer result)
                {
                    result = new Promise<T>.ResultContainer(handler.GetResult<T>(), rejectContainer, state);
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<Promise<T>.ResultContainer> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<Promise<T>.ResultContainer> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<Promise<T>.ResultContainer> GetMergeResult<T>() => MergeResult<T>.Func;

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class MergeResultVoid
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref ResultContainer result)
                {
                    result = new ResultContainer(rejectContainer, state);
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<ResultContainer> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<ResultContainer> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<ResultContainer> GetMergeResultVoid() => MergeResultVoid.Func;

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class MergeValue<T>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeValue(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref T result)
                {
                    result = handler.GetResult<T>();
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<T> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeValue);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<T> Func
                    = GetMergeValue;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<T> GetMergeValue<T>() => MergeValue<T>.Func;

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class CombinedMerger<T1, T2>
            {
                internal static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
                internal static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;

                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, T2) result)
                {
                    switch (index)
                    {
                        case 0:
                            s_getResult1Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item1);
                            break;
                        case 1:
                            s_getResult2Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item2);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, T2)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, T2)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, T2)> GetCombinedMerger<T1, T2>(
                Internal.GetResultContainerDelegate<T1> getResult1Delegate,
                Internal.GetResultContainerDelegate<T2> getResult2Delegate)
            {
                CombinedMerger<T1, T2>.s_getResult1Delegate = getResult1Delegate;
                CombinedMerger<T1, T2>.s_getResult2Delegate = getResult2Delegate;
                return CombinedMerger<T1, T2>.Func;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class CombinedMerger<T1, T2, T3>
            {
                internal static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
                internal static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
                internal static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;

                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, T2, T3) result)
                {
                    switch (index)
                    {
                        case 0:
                            s_getResult1Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item1);
                            break;
                        case 1:
                            s_getResult2Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item2);
                            break;
                        case 2:
                            s_getResult3Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item3);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, T2, T3)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, T2, T3)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, T2, T3)> GetCombinedMerger<T1, T2, T3>(
                Internal.GetResultContainerDelegate<T1> getResult1Delegate,
                Internal.GetResultContainerDelegate<T2> getResult2Delegate,
                Internal.GetResultContainerDelegate<T3> getResult3Delegate)
            {
                CombinedMerger<T1, T2, T3>.s_getResult1Delegate = getResult1Delegate;
                CombinedMerger<T1, T2, T3>.s_getResult2Delegate = getResult2Delegate;
                CombinedMerger<T1, T2, T3>.s_getResult3Delegate = getResult3Delegate;
                return CombinedMerger<T1, T2, T3>.Func;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class CombinedMerger<T1, T2, T3, T4>
            {
                internal static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
                internal static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
                internal static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
                internal static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;

                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, T2, T3, T4) result)
                {
                    switch (index)
                    {
                        case 0:
                            s_getResult1Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item1);
                            break;
                        case 1:
                            s_getResult2Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item2);
                            break;
                        case 2:
                            s_getResult3Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item3);
                            break;
                        case 3:
                            s_getResult4Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item4);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, T2, T3, T4)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, T2, T3, T4)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, T2, T3, T4)> GetCombinedMerger<T1, T2, T3, T4>(
                Internal.GetResultContainerDelegate<T1> getResult1Delegate,
                Internal.GetResultContainerDelegate<T2> getResult2Delegate,
                Internal.GetResultContainerDelegate<T3> getResult3Delegate,
                Internal.GetResultContainerDelegate<T4> getResult4Delegate)
            {
                CombinedMerger<T1, T2, T3, T4>.s_getResult1Delegate = getResult1Delegate;
                CombinedMerger<T1, T2, T3, T4>.s_getResult2Delegate = getResult2Delegate;
                CombinedMerger<T1, T2, T3, T4>.s_getResult3Delegate = getResult3Delegate;
                CombinedMerger<T1, T2, T3, T4>.s_getResult4Delegate = getResult4Delegate;
                return CombinedMerger<T1, T2, T3, T4>.Func;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class CombinedMerger<T1, T2, T3, T4, T5>
            {
                internal static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
                internal static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
                internal static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
                internal static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;
                internal static Internal.GetResultContainerDelegate<T5> s_getResult5Delegate;

                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, T2, T3, T4, T5) result)
                {
                    switch (index)
                    {
                        case 0:
                            s_getResult1Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item1);
                            break;
                        case 1:
                            s_getResult2Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item2);
                            break;
                        case 2:
                            s_getResult3Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item3);
                            break;
                        case 3:
                            s_getResult4Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item4);
                            break;
                        case 4:
                            s_getResult5Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item5);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, T2, T3, T4, T5)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, T2, T3, T4, T5)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, T2, T3, T4, T5)> GetCombinedMerger<T1, T2, T3, T4, T5>(
                Internal.GetResultContainerDelegate<T1> getResult1Delegate,
                Internal.GetResultContainerDelegate<T2> getResult2Delegate,
                Internal.GetResultContainerDelegate<T3> getResult3Delegate,
                Internal.GetResultContainerDelegate<T4> getResult4Delegate,
                Internal.GetResultContainerDelegate<T5> getResult5Delegate)
            {
                CombinedMerger<T1, T2, T3, T4, T5>.s_getResult1Delegate = getResult1Delegate;
                CombinedMerger<T1, T2, T3, T4, T5>.s_getResult2Delegate = getResult2Delegate;
                CombinedMerger<T1, T2, T3, T4, T5>.s_getResult3Delegate = getResult3Delegate;
                CombinedMerger<T1, T2, T3, T4, T5>.s_getResult4Delegate = getResult4Delegate;
                CombinedMerger<T1, T2, T3, T4, T5>.s_getResult5Delegate = getResult5Delegate;
                return CombinedMerger<T1, T2, T3, T4, T5>.Func;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class CombinedMerger<T1, T2, T3, T4, T5, T6>
            {
                internal static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
                internal static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
                internal static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
                internal static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;
                internal static Internal.GetResultContainerDelegate<T5> s_getResult5Delegate;
                internal static Internal.GetResultContainerDelegate<T6> s_getResult6Delegate;

                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, T2, T3, T4, T5, T6) result)
                {
                    switch (index)
                    {
                        case 0:
                            s_getResult1Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item1);
                            break;
                        case 1:
                            s_getResult2Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item2);
                            break;
                        case 2:
                            s_getResult3Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item3);
                            break;
                        case 3:
                            s_getResult4Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item4);
                            break;
                        case 4:
                            s_getResult5Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item5);
                            break;
                        case 5:
                            s_getResult6Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item6);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, T2, T3, T4, T5, T6)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, T2, T3, T4, T5, T6)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, T2, T3, T4, T5, T6)> GetCombinedMerger<T1, T2, T3, T4, T5, T6>(
                Internal.GetResultContainerDelegate<T1> getResult1Delegate,
                Internal.GetResultContainerDelegate<T2> getResult2Delegate,
                Internal.GetResultContainerDelegate<T3> getResult3Delegate,
                Internal.GetResultContainerDelegate<T4> getResult4Delegate,
                Internal.GetResultContainerDelegate<T5> getResult5Delegate,
                Internal.GetResultContainerDelegate<T6> getResult6Delegate)
            {
                CombinedMerger<T1, T2, T3, T4, T5, T6>.s_getResult1Delegate = getResult1Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6>.s_getResult2Delegate = getResult2Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6>.s_getResult3Delegate = getResult3Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6>.s_getResult4Delegate = getResult4Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6>.s_getResult5Delegate = getResult5Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6>.s_getResult6Delegate = getResult6Delegate;
                return CombinedMerger<T1, T2, T3, T4, T5, T6>.Func;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class CombinedMerger<T1, T2, T3, T4, T5, T6, T7>
            {
                internal static Internal.GetResultContainerDelegate<T1> s_getResult1Delegate;
                internal static Internal.GetResultContainerDelegate<T2> s_getResult2Delegate;
                internal static Internal.GetResultContainerDelegate<T3> s_getResult3Delegate;
                internal static Internal.GetResultContainerDelegate<T4> s_getResult4Delegate;
                internal static Internal.GetResultContainerDelegate<T5> s_getResult5Delegate;
                internal static Internal.GetResultContainerDelegate<T6> s_getResult6Delegate;
                internal static Internal.GetResultContainerDelegate<T7> s_getResult7Delegate;

                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, T2, T3, T4, T5, T6, T7) result)
                {
                    switch (index)
                    {
                        case 0:
                            s_getResult1Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item1);
                            break;
                        case 1:
                            s_getResult2Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item2);
                            break;
                        case 2:
                            s_getResult3Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item3);
                            break;
                        case 3:
                            s_getResult4Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item4);
                            break;
                        case 4:
                            s_getResult5Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item5);
                            break;
                        case 5:
                            s_getResult6Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item6);
                            break;
                        case 6:
                            s_getResult7Delegate.Invoke(handler, rejectContainer, state, index, ref result.Item7);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, T2, T3, T4, T5, T6, T7)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, T2, T3, T4, T5, T6, T7)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, T2, T3, T4, T5, T6, T7)> GetCombinedMerger<T1, T2, T3, T4, T5, T6, T7>(
                Internal.GetResultContainerDelegate<T1> getResult1Delegate,
                Internal.GetResultContainerDelegate<T2> getResult2Delegate,
                Internal.GetResultContainerDelegate<T3> getResult3Delegate,
                Internal.GetResultContainerDelegate<T4> getResult4Delegate,
                Internal.GetResultContainerDelegate<T5> getResult5Delegate,
                Internal.GetResultContainerDelegate<T6> getResult6Delegate,
                Internal.GetResultContainerDelegate<T7> getResult7Delegate)
            {
                CombinedMerger<T1, T2, T3, T4, T5, T6, T7>.s_getResult1Delegate = getResult1Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6, T7>.s_getResult2Delegate = getResult2Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6, T7>.s_getResult3Delegate = getResult3Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6, T7>.s_getResult4Delegate = getResult4Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6, T7>.s_getResult5Delegate = getResult5Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6, T7>.s_getResult6Delegate = getResult6Delegate;
                CombinedMerger<T1, T2, T3, T4, T5, T6, T7>.s_getResult7Delegate = getResult7Delegate;
                return CombinedMerger<T1, T2, T3, T4, T5, T6, T7>.Func;
            }
        }
    }
}