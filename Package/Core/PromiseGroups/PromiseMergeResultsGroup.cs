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
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup
    {
        private readonly PromiseMergeGroup _mergeGroup;

        private PromiseMergeResultsGroup(PromiseMergeGroup mergeGroup)
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
        public PromiseMergeResultsGroup<T> Add<T>(Promise<T> promise)
            => new PromiseMergeResultsGroup<T>(_mergeGroup.Add(promise.AsPromise()), promise._result);
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;

        internal PromiseMergeResultsGroup(PromiseMergeGroup mergeGroup, T1 result1)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2> Add<T2>(Promise<T2> promise)
            => new PromiseMergeResultsGroup<T1, T2>(_mergeGroup.Add(promise.AsPromise()), _result1, promise._result);
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;

        internal PromiseMergeResultsGroup(PromiseMergeGroup mergeGroup, T1 result1, T2 result2)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3> Add<T3>(Promise<T3> promise)
            => new PromiseMergeResultsGroup<T1, T2, T3>(_mergeGroup.Add(promise.AsPromise()), _result1, _result2, promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer)> WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer)
                value = (_result1, _result2);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetSettled0<T1, T2>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;
        private readonly T3 _result3;

        internal PromiseMergeResultsGroup(PromiseMergeGroup mergeGroup, T1 result1, T2 result2, T3 result3)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
            _result3 = result3;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4> Add<T4>(Promise<T4> promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, T4>(_mergeGroup.Add(promise.AsPromise()), _result1, _result2, _result3, promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)> WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)
                value = (_result1, _result2, _result3);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetSettled0<T1, T2, T3>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;
        private readonly T3 _result3;
        private readonly T4 _result4;

        internal PromiseMergeResultsGroup(PromiseMergeGroup mergeGroup, T1 result1, T2 result2, T3 result3, T4 result4)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
            _result3 = result3;
            _result4 = result4;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, T5> Add<T5>(Promise<T5> promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, T4, T5>(_mergeGroup.Add(promise.AsPromise()), _result1, _result2, _result3, _result4, promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)> WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)
                value = (_result1, _result2, _result3, _result4);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetSettled0<T1, T2, T3, T4>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4, T5>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;
        private readonly T3 _result3;
        private readonly T4 _result4;
        private readonly T5 _result5;

        internal PromiseMergeResultsGroup(PromiseMergeGroup mergeGroup, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
            _result3 = result3;
            _result4 = result4;
            _result5 = result5;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6> Add<T6>(Promise<T6> promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6>(_mergeGroup.Add(promise.AsPromise()), _result1, _result2, _result3, _result4, _result5, promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)> WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)
                value = (_result1, _result2, _result3, _result4, _result5);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetSettled0<T1, T2, T3, T4, T5>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;
        private readonly T3 _result3;
        private readonly T4 _result4;
        private readonly T5 _result5;
        private readonly T6 _result6;

        internal PromiseMergeResultsGroup(PromiseMergeGroup mergeGroup, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
            _result3 = result3;
            _result4 = result4;
            _result5 = result5;
            _result6 = result6;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6, T7> Add<T7>(Promise<T7> promise)
            => new PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6, T7>(_mergeGroup.Add(promise.AsPromise()), _result1, _result2, _result3, _result4, _result5, _result6, promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)>
            WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)
                value = (_result1, _result2, _result3, _result4, _result5, _result6);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetSettled0<T1, T2, T3, T4, T5, T6>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroup<T1, T2, T3, T4, T5, T6, T7>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;
        private readonly T3 _result3;
        private readonly T4 _result4;
        private readonly T5 _result5;
        private readonly T6 _result6;
        private readonly T7 _result7;

        internal PromiseMergeResultsGroup(PromiseMergeGroup mergeGroup, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
            _result3 = result3;
            _result4 = result4;
            _result5 = result5;
            _result6 = result6;
            _result7 = result7;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        // Merging more than 7 types should be fairly rare. To support N types greater than 7, we use PromiseMergeResultsGroupExtended.
        public PromiseMergeResultsGroupExtended<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer), T8>
            Add<T8>(Promise<T8> promise)
        {
            var waitAsyncPromise = WaitAsync();
            return new PromiseMergeResultsGroupExtended<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer), T8>
            (
                new PromiseMergeGroup(_mergeGroup._cancelationRef)
                    .Add(waitAsyncPromise.AsPromise())
                    .Add(promise.AsPromise()),
                waitAsyncPromise._result,
                promise._result
            );
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)>
            WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)
                value = (_result1, _result2, _result3, _result4, _result5, _result6, _result7);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetSettled0<T1, T2, T3, T4, T5, T6, T7>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(Promise<T1>.ResultContainer, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;

        internal PromiseMergeResultsGroupExtended(PromiseMergeGroup mergeGroup, T1 result1, T2 result2)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3> Add<T3>(Promise<T3> promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3>(_mergeGroup.Add(promise.AsPromise()), _result1, _result2, promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(T1, Promise<T2>.ResultContainer)>
            WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (T1, Promise<T2>.ResultContainer)
                value = (_result1, _result2);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetMergeResultsExtended<T1, T2>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, Promise<T2>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2, T3>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;
        private readonly T3 _result3;

        internal PromiseMergeResultsGroupExtended(PromiseMergeGroup mergeGroup, T1 result1, T2 result2, T3 result3)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
            _result3 = result3;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, T4> Add<T4>(Promise<T4> promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, T4>(_mergeGroup.Add(promise.AsPromise()), _result1, _result2, _result3, promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)>
            WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)
                value = (_result1, _result2, _result3);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetMergeResultsExtended<T1, T2, T3>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2, T3, T4>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;
        private readonly T3 _result3;
        private readonly T4 _result4;

        internal PromiseMergeResultsGroupExtended(PromiseMergeGroup mergeGroup, T1 result1, T2 result2, T3 result3, T4 result4)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
            _result3 = result3;
            _result4 = result4;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5> Add<T5>(Promise<T5> promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5>(_mergeGroup.Add(promise.AsPromise()), _result1, _result2, _result3, _result4, promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)>
            WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)
                value = (_result1, _result2, _result3, _result4);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetMergeResultsExtended<T1, T2, T3, T4>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;
        private readonly T3 _result3;
        private readonly T4 _result4;
        private readonly T5 _result5;

        internal PromiseMergeResultsGroupExtended(PromiseMergeGroup mergeGroup, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
            _result3 = result3;
            _result4 = result4;
            _result5 = result5;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6> Add<T6>(Promise<T6> promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6>(_mergeGroup.Add(promise.AsPromise()), _result1, _result2, _result3, _result4, _result5, promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)>
            WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)
                value = (_result1, _result2, _result3, _result4, _result5);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetMergeResultsExtended<T1, T2, T3, T4, T5>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;
        private readonly T3 _result3;
        private readonly T4 _result4;
        private readonly T5 _result5;
        private readonly T6 _result6;

        internal PromiseMergeResultsGroupExtended(PromiseMergeGroup mergeGroup, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
            _result3 = result3;
            _result4 = result4;
            _result5 = result5;
            _result6 = result6;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6, T7> Add<T7>(Promise<T7> promise)
            => new PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6, T7>(_mergeGroup.Add(promise.AsPromise()), _result1, _result2, _result3, _result4, _result5, _result6, promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)>
            WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)
                value = (_result1, _result2, _result3, _result4, _result5, _result6);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetMergeResultsExtended<T1, T2, T3, T4, T5, T6>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    /// <summary>
    /// A structured concurrency group used to merge promises and yield their results. Waits for all promises to complete.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseMergeResultsGroupExtended<T1, T2, T3, T4, T5, T6, T7>
    {
        private readonly PromiseMergeGroup _mergeGroup;
        private readonly T1 _result1;
        private readonly T2 _result2;
        private readonly T3 _result3;
        private readonly T4 _result4;
        private readonly T5 _result5;
        private readonly T6 _result6;
        private readonly T7 _result7;

        internal PromiseMergeResultsGroupExtended(PromiseMergeGroup mergeGroup, T1 result1, T2 result2, T3 result3, T4 result4, T5 result5, T6 result6, T7 result7)
        {
            _mergeGroup = mergeGroup;
            _result1 = result1;
            _result2 = result2;
            _result3 = result3;
            _result4 = result4;
            _result5 = result5;
            _result6 = result6;
            _result7 = result7;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        // To support N types greater than 13, we wrap it in another PromiseMergeResultsGroupExtended.
        public PromiseMergeResultsGroupExtended<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer), T8>
            Add<T8>(Promise<T8> promise)
        {
            var waitAsyncPromise = WaitAsync();
            return new PromiseMergeResultsGroupExtended<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer), T8>
            (
                new PromiseMergeGroup(_mergeGroup._cancelationRef)
                    .Add(waitAsyncPromise.AsPromise())
                    .Add(promise.AsPromise()),
                waitAsyncPromise._result,
                promise._result
            );
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete and yields a tuple containing their results.
        /// </summary>
        public Promise<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)>
            WaitAsync()
        {
            if (_mergeGroup._cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup();
            }

            (T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)
                value = (_result1, _result2, _result3, _result4, _result5, _result6, _result7);

            var group = _mergeGroup._group;
            if (group == null)
            {
                _mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(value);
            }

            if (!group.TryIncrementId(_mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup();
            }
            group.MarkReady(_mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseResultsGroup(value, Promise.MergeResultFuncs.GetMergeResultsExtended<T1, T2, T3, T4, T5, T6, T7>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)>
                (promise, promise.Id);
        }
    }

    partial struct Promise
    {
        static partial class MergeResultFuncs
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class MergeResultsExtended<T1, T2>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, Promise<T2>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer)>
                GetMergeResultsExtended<T1, T2>() => MergeResultsExtended<T1, T2>.Func;

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class MergeResultsExtended<T1, T2, T3>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer)>
                GetMergeResultsExtended<T1, T2, T3>() => MergeResultsExtended<T1, T2, T3>.Func;

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class MergeResultsExtended<T1, T2, T3, T4>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer)>
                GetMergeResultsExtended<T1, T2, T3, T4>() => MergeResultsExtended<T1, T2, T3, T4>.Func;

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class MergeResultsExtended<T1, T2, T3, T4, T5>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer)>
                GetMergeResultsExtended<T1, T2, T3, T4, T5>() => MergeResultsExtended<T1, T2, T3, T4, T5>.Func;

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class MergeResultsExtended<T1, T2, T3, T4, T5, T6>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new Promise<T6>.ResultContainer(handler.GetResult<T6>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer)>
                GetMergeResultsExtended<T1, T2, T3, T4, T5, T6>() => MergeResultsExtended<T1, T2, T3, T4, T5, T6>.Func;

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class MergeResultsExtended<T1, T2, T3, T4, T5, T6, T7>
            {
                [MethodImpl(Internal.InlineOption)]
                private static void GetMergeResult(Internal.PromiseRefBase handler, Internal.IRejectContainer rejectContainer, State state, int index, ref (T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer) result)
                {
                    switch (index)
                    {
                        case 0:
                            result.Item1 = handler.GetResult<T1>();
                            break;
                        case 1:
                            result.Item2 = new Promise<T2>.ResultContainer(handler.GetResult<T2>(), rejectContainer, state);
                            break;
                        case 2:
                            result.Item3 = new Promise<T3>.ResultContainer(handler.GetResult<T3>(), rejectContainer, state);
                            break;
                        case 3:
                            result.Item4 = new Promise<T4>.ResultContainer(handler.GetResult<T4>(), rejectContainer, state);
                            break;
                        case 4:
                            result.Item5 = new Promise<T5>.ResultContainer(handler.GetResult<T5>(), rejectContainer, state);
                            break;
                        case 5:
                            result.Item6 = new Promise<T6>.ResultContainer(handler.GetResult<T6>(), rejectContainer, state);
                            break;
                        case 6:
                            result.Item7 = new Promise<T7>.ResultContainer(handler.GetResult<T7>(), rejectContainer, state);
                            break;
                    }
                }

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                internal static unsafe Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)> Func
                {
                    [MethodImpl(Internal.InlineOption)]
                    get => new(&GetMergeResult);
                }
#else
                internal static readonly Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)> Func
                    = GetMergeResult;
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.GetResultContainerDelegate<(T1, Promise<T2>.ResultContainer, Promise<T3>.ResultContainer, Promise<T4>.ResultContainer, Promise<T5>.ResultContainer, Promise<T6>.ResultContainer, Promise<T7>.ResultContainer)>
                GetMergeResultsExtended<T1, T2, T3, T4, T5, T6, T7>() => MergeResultsExtended<T1, T2, T3, T4, T5, T6, T7>.Func;
        }
    }
}