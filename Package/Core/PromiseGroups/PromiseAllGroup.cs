#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
        internal readonly Internal.CancelationRef _cancelationRef;
        internal readonly Internal.PromiseRefBase.AllPromiseGroup<T> _group;
        internal readonly int _count;
        internal readonly short _id;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseAllGroup(IList<T> valueContainer, Internal.CancelationRef cancelationRef)
        {
            _valueContainer = valueContainer ?? new List<T>();
            _cancelationRef = cancelationRef;
            _count = 0;
#if PROMISE_DEBUG
            // We always create the promise backing reference in DEBUG mode to ensure the group is used properly.
            _group = Internal.GetOrCreateAllPromiseGroup(cancelationRef, _valueContainer);
            _id = _group.Id;
#else
            // In RELEASE mode, we only create the backing reference when it's needed.
            _group = null;
            _id = 0;
#endif
        }

        [MethodImpl(Internal.InlineOption)]
        private PromiseAllGroup(IList<T> valueContainer, Internal.CancelationRef cancelationRef, Internal.PromiseRefBase.AllPromiseGroup<T> group, int count, short id)
        {
            _valueContainer = valueContainer;
            _cancelationRef = cancelationRef;
            _group = group;
            _count = count;
            _id = id;
        }

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="groupCancelationToken">The token that will be canceled if any of the promises in the group are rejected or canceled.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(out CancelationToken groupCancelationToken, IList<T> valueContainer = null)
            => New(CancelationToken.None, out groupCancelationToken, valueContainer);

        /// <summary>
        /// Get a new <see cref="PromiseAllGroup{T}"/> that will be canceled when the <paramref name="sourceCancelationToken"/> is canceled, and the <see cref="CancelationToken"/> tied to it.
        /// </summary>
        /// <param name="sourceCancelationToken">The token used to cancel the group early.</param>
        /// <param name="groupCancelationToken">The token that will be canceled if <paramref name="sourceCancelationToken"/> is canceled or any of the promises in the group are rejected or canceled.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static PromiseAllGroup<T> New(CancelationToken sourceCancelationToken, out CancelationToken groupCancelationToken, IList<T> valueContainer = null)
        {
            var cancelationRef = Internal.CancelationRef.GetOrCreate();
            cancelationRef.MaybeLinkToken(sourceCancelationToken);
            groupCancelationToken = new CancelationToken(cancelationRef, cancelationRef.TokenId);
            return new PromiseAllGroup<T>(valueContainer, cancelationRef);
        }

        /// <summary>
        /// Returns a new group with the <paramref name="promise"/> added to it.
        /// </summary>
        /// <param name="promise">The <see cref="Promise{T}"/> to add to this group.</param>
        public PromiseAllGroup<T> Add(Promise<T> promise)
        {
            var list = _valueContainer;
            var cancelationRef = _cancelationRef;
            var group = _group;
            int count = _count;
            if (cancelationRef == null | list == null)
            {
                Internal.ThrowInvalidAllGroup();
            }

            if (group != null)
            {
                if (!group.TryIncrementId(_id))
                {
                    Internal.ThrowInvalidAllGroup();
                }

                AddOrSetResult(list, promise._result, count);
                // We don't need to do anything else if the ref is null.
                if (promise._ref != null)
                {
                    ++count;
                    group.AddPromise(promise._ref, promise._id);
                }
                return new PromiseAllGroup<T>(list, cancelationRef, group, count, group.Id);
            }

            AddOrSetResult(list, promise._result, count);
            if (promise._ref != null)
            {
                group = Internal.GetOrCreateAllPromiseGroup(cancelationRef, list);
                group.AddPromise(promise._ref, promise._id);
                return new PromiseAllGroup<T>(list, cancelationRef, group, 1, group.Id);
            }

            return this;
        }

        private static void AddOrSetResult(IList<T> list, in T result, int index)
        {
            // We don't protect the list with a lock, because we ensure this is only used by a single caller.
            if (list.Count <= index)
            {
                list.Add(result);
            }
            else
            {
                list[index] = result;
            }
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
            if (cancelationRef == null | list == null)
            {
                Internal.ThrowInvalidAllGroup();
            }

            // Make sure list has the same count as promises.
            int listCount = list.Count;
            while (listCount > count)
            {
                list.RemoveAt(--listCount);
            }

            if (group == null)
            {
                cancelationRef.Dispose();
                return Promise.Resolved(list);
            }

            if (!group.TryIncrementId(_id))
            {
                Internal.ThrowInvalidAllGroup();
            }
            group.MarkReady(count);
            return new Promise<IList<T>>(group, group.Id);
        }
    }
}