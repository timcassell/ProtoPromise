#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    /// <summary>
    /// A structured concurrency group used to race promises, incorporating their indices. Waits for the first promise to resolve.
    /// </summary>
    /// <remarks>
    /// This type is obsolete. Prefer <see cref="PromiseExtensions.AppendResult{TAppend}(Promise, TAppend)"/> and <see cref="PromiseRaceGroup{T}"/> instead.
    /// </remarks>
    [Obsolete("Prefer Promise.AppendResult(int) and PromiseRaceGroup<int>", false), EditorBrowsable(EditorBrowsableState.Never)]
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseRaceWithIndexGroup
    {
        private readonly PromiseRaceGroup<int> _group;
        private readonly int _index;

        [MethodImpl(Internal.InlineOption)]
        private PromiseRaceWithIndexGroup(PromiseRaceGroup<int> raceGroup, int index)
        {
            _group = raceGroup;
            _index = index;
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
            => new PromiseRaceWithIndexGroup(PromiseRaceGroup<int>.New(sourceCancelationToken, out groupCancelationToken, cancelOnNonResolved), -1);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise"/> to add to this group.</param>
        public PromiseRaceWithIndexGroup Add(Promise promise)
        {
            var index = checked(_index + 1);
            return new PromiseRaceWithIndexGroup(_group.Validate(promise).Add(promise.AppendResult(index)), index);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// </summary>
        /// <remarks>
        /// If the group is not canceled and any promise is resolved, the returned promise will be resolved with the index of the promise that resolved first.
        /// Otherwise, if any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, the returned promise will be canceled.
        /// </remarks>
        public Promise<int> WaitAsync()
            => _group.WaitAsync();
    }

    /// <summary>
    /// A structured concurrency group used to race promises, incorporating their indices. Waits for the first promise to resolve.
    /// </summary>
    /// <remarks>
    /// This type is obsolete. Prefer <see cref="PromiseExtensions.AppendResult{T, TAppend}(in Promise{T}, TAppend)"/> and <see cref="PromiseRaceGroup{T}"/> instead.
    /// </remarks>
    [Obsolete("Prefer Promise.AppendResult(int) and PromiseRaceGroup<(T, int)>", false), EditorBrowsable(EditorBrowsableState.Never)]
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly struct PromiseRaceWithIndexGroup<T>
    {
        private readonly PromiseRaceGroup<(T, int)> _group;
        private readonly int _index;

        [MethodImpl(Internal.InlineOption)]
        private PromiseRaceWithIndexGroup(PromiseRaceGroup<(T, int)> group, int index)
        {
            _group = group;
            _index = index;
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
            => new PromiseRaceWithIndexGroup<T>(PromiseRaceGroup<(T, int)>.New(sourceCancelationToken, out groupCancelationToken, cancelOnNonResolved), -1);

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseRaceWithIndexGroup<T> Add(Promise<T> promise)
        {
            var index = checked(_index + 1);
            return new PromiseRaceWithIndexGroup<T>(_group.Validate(promise).Add(promise.AppendResult(index)), index);
        }

        /// <summary>
        /// Waits asynchronously for all of the promises in this group to complete.
        /// </summary>
        /// <remarks>
        /// If the group is not canceled and any promise is resolved, the returned promise will be resolved with the index and value of the promise that resolved first.
        /// Otherwise, if any promise is rejected, the returned promise will be rejected with an <see cref="AggregateException"/> containing all of the rejections.
        /// Otherwise, the returned promise will be canceled.
        /// </remarks>
        public Promise<(int winIndex, T result)> WaitAsync()
            => _group.WaitAsync().Then(tuple => (tuple.Item2, tuple.Item1));
    }
}