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
        internal readonly uint _count;
        internal readonly short _id;

        internal PromiseMergeGroup(Internal.CancelationRef cancelationRef)
        {
            _cancelationRef = cancelationRef;
            _count = 0;
#if PROMISE_DEBUG
            // We always create the promise backing reference in DEBUG mode to ensure the group is used properly.
            _group = Internal.GetOrCreateMergePromiseGroupVoid(cancelationRef);
            _id = _group.Id;
#else
            // In RELEASE mode, we only create the backing reference when it's needed.
            _group = null;
            _id = 0;
#endif
        }

        [MethodImpl(Internal.InlineOption)]
        internal PromiseMergeGroup(Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.MergePromiseGroupVoid group, uint count, short id)
        {
            _cancelationRef = cancelationRef;
            _group = group;
            _count = count;
            _id = id;
        }

        /// <summary>
        /// Get a new <see cref="PromiseMergeGroup"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        public static PromiseMergeGroup New(out CancelationToken groupCancelationToken)
            => New(CancelationToken.None, out groupCancelationToken);

        /// <summary>
        /// Get a new <see cref="PromiseMergeGroup"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        public static PromiseMergeGroup New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken)
        {
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseMergeGroup(cancelationRef);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseMergeGroup Add(Promise promise)
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            uint count = _count;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(1);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_id))
                {
                    Internal.ThrowInvalidMergeGroup(1);
                }

                // We don't need to do anything if the ref is null.
                if (promise._ref != null)
                {
                    checked { ++count; }
                    group.AddPromise(promise._ref, promise._id);
                }
                return new PromiseMergeGroup(cancelationRef, group, count, group.Id);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateMergePromiseGroupVoid(cancelationRef);
                group.AddPromise(promise._ref, promise._id);
                return new PromiseMergeGroup(cancelationRef, group, 1, group.Id);
            }

            return this;
        }

        internal PromiseMergeGroup Merge(Promise promise, int index)
        {
            var cancelationRef = _cancelationRef;
            var group = _group;
            uint count = _count;
            if (cancelationRef == null)
            {
                Internal.ThrowInvalidMergeGroup(2);
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_id))
                {
                    Internal.ThrowInvalidMergeGroup(2);
                }

                // We don't need to do anything if the ref is null.
                if (promise._ref != null)
                {
                    checked { ++count; }
                    group.AddPromiseForMerge(promise._ref, promise._id, index);
                }
                return new PromiseMergeGroup(cancelationRef, group, count, group.Id);
            }

            if (promise._ref != null)
            {
                group = Internal.GetOrCreateMergePromiseGroupVoid(cancelationRef);
                group.AddPromiseForMerge(promise._ref, promise._id, index);
                return new PromiseMergeGroup(cancelationRef, group, 1, group.Id);
            }

            return this;
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseMergeGroup<T1> Add<T1>(Promise<T1> promise)
            => new PromiseMergeGroup<T1>(Merge(promise, 0), promise._result);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
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
                cancelationRef.Dispose();
                return Promise.Resolved();
            }

            if (!group.TryIncrementId(_id))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(count);
            return new Promise(group, group.Id);
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
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with the resolved value.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
        /// </summary>
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
                mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseGroup(_value, Promise.MergeResultFuncs.GetOne<T1>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<T1>(promise, promise.Id);
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
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
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
                mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseGroup(_value, Promise.MergeResultFuncs.GetTwo<T1, T2>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2)>(promise, promise.Id);
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
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
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
                mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseGroup(_value, Promise.MergeResultFuncs.GetThree<T1, T2, T3>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3)>(promise, promise.Id);
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
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
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
                mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseGroup(_value, Promise.MergeResultFuncs.GetFour<T1, T2, T3, T4>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4)>(promise, promise.Id);
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
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
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
                mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseGroup(_value, Promise.MergeResultFuncs.GetFive<T1, T2, T3, T4, T5>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4, T5)>(promise, promise.Id);
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
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
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
                mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseGroup(_value, Promise.MergeResultFuncs.GetSix<T1, T2, T3, T4, T5, T6>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4, T5, T6)>(promise, promise.Id);
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
            => new PromiseMergeGroup(_mergeGroup._cancelationRef)
            .Add(WaitAsync())
            .Add(promise);

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// If all promises are resolved, the returned promise will be resolved with a tuple containing each of their resolved values.
        /// If any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, if any promise is canceled, the returned promise will be canceled.
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
                mergeGroup._cancelationRef.Dispose();
                return Promise.Resolved(_value);
            }

            if (!group.TryIncrementId(mergeGroup._id))
            {
                Internal.ThrowInvalidMergeGroup(1);
            }
            group.MarkReady(mergeGroup._count);
            var promise = Internal.GetOrCreateMergePromiseGroup(_value, Promise.MergeResultFuncs.GetSeven<T1, T2, T3, T4, T5, T6, T7>());
            group.HookupNewPromise(group.Id, promise);
            return new Promise<(T1, T2, T3, T4, T5, T6, T7)>(promise, promise.Id);
        }
    }
}