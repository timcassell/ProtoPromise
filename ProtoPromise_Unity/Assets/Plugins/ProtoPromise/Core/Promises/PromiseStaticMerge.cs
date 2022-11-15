#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Proto.Promises
{
    public partial struct Promise
    {
        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(Promise promise1, Promise promise2)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateAllPromiseVoid(passThroughs, pendingCount, completedProgress, maxDepth);
            return new Promise(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(Promise promise1, Promise promise2, Promise promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateAllPromiseVoid(passThroughs, pendingCount, completedProgress, maxDepth);
            return new Promise(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateAllPromiseVoid(passThroughs, pendingCount, completedProgress, maxDepth);
            return new Promise(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(params Promise[] promises)
        {
            return All(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(IEnumerable<Promise> promises)
        {
            return All(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, "promises", 1);

            using (promises)
            {
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                int pendingCount = 0;
                ulong completedProgress = 0;
                ushort maxDepth = 0;

                int index = -1;
                while (promises.MoveNext())
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    Internal.PrepareForMerge(p, ref passThroughs, ++index, ref pendingCount, ref completedProgress, ref maxDepth);
                }

                if (pendingCount == 0)
                {
                    return Internal.CreateResolved(maxDepth);
                }
                var promise = Internal.PromiseRefBase.GetOrCreateAllPromiseVoid(passThroughs, pendingCount, completedProgress, maxDepth);
                return new Promise(promise, promise.Id, maxDepth);
            }
        }

        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, IList<T> valueContainer = null)
        {
            return Promise<T>.All(promise1, promise2, valueContainer);
        }

        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, IList<T> valueContainer = null)
        {
            return Promise<T>.All(promise1, promise2, promise3, valueContainer);
        }

        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4, IList<T> valueContainer = null)
        {
            return Promise<T>.All(promise1, promise2, promise3, promise4, valueContainer);
        }

        public static Promise<IList<T>> All<T>(params Promise<T>[] promises)
        {
            return Promise<T>.All(promises);
        }

        public static Promise<IList<T>> All<T>(IEnumerable<Promise<T>> promises)
        {
            return Promise<T>.All(promises);
        }

        public static Promise<IList<T>> All<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise<T>.All(promises);
        }

        [Obsolete("Prefer Promise<T>.All()"), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise<IList<T>> AllNonAlloc<T, TEnumerator>(TEnumerator promises, IList<T> valueContainer) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise<T>.AllNonAlloc(promises, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the value of <paramref name="promise1"/> when both promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T1> Merge<T1>(Promise<T1> promise1, Promise promise2)
        {
            T1 value = default(T1);
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<T1>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2>> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2)
        {
            var value = new ValueTuple<T1, T2>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2>> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2, Promise promise3)
        {
            var value = new ValueTuple<T1, T2>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3>> Merge<T1, T2, T3>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3)
        {
            var value = new ValueTuple<T1, T2, T3>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref value.Item3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2, T3>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3>> Merge<T1, T2, T3>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4)
        {
            var value = new ValueTuple<T1, T2, T3>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref value.Item3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2, T3>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4>> Merge<T1, T2, T3, T4>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4)
        {
            var value = new ValueTuple<T1, T2, T3, T4>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref value.Item3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref value.Item4, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2, T3, T4>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4>> Merge<T1, T2, T3, T4>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5)
        {
            var value = new ValueTuple<T1, T2, T3, T4>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref value.Item3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref value.Item4, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref passThroughs, 4, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2, T3, T4>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5>> Merge<T1, T2, T3, T4, T5>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref value.Item3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref value.Item4, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref value.Item5, ref passThroughs, 4, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5>> Merge<T1, T2, T3, T4, T5>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise promise6)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref value.Item3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref value.Item4, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref value.Item5, ref passThroughs, 4, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref passThroughs, 5, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6>> Merge<T1, T2, T3, T4, T5, T6>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5, T6>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref value.Item3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref value.Item4, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref value.Item5, ref passThroughs, 4, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref value.Item6, ref passThroughs, 5, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5, T6>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6>> Merge<T1, T2, T3, T4, T5, T6>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise promise7)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5, T6>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref value.Item3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref value.Item4, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref value.Item5, ref passThroughs, 4, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref value.Item6, ref passThroughs, 5, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise7, "promise7", 1);
            Internal.PrepareForMerge(promise7, ref passThroughs, 6, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5, T6>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> Merge<T1, T2, T3, T4, T5, T6, T7>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise<T7> promise7)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5, T6, T7>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref value.Item3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref value.Item4, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref value.Item5, ref passThroughs, 4, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref value.Item6, ref passThroughs, 5, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise7, "promise7", 1);
            Internal.PrepareForMerge(promise7, ref value.Item7, ref passThroughs, 6, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> Merge<T1, T2, T3, T4, T5, T6, T7>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise<T7> promise7, Promise promise8)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5, T6, T7>();
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            Internal.PrepareForMerge(promise1, ref value.Item1, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            Internal.PrepareForMerge(promise2, ref value.Item2, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            Internal.PrepareForMerge(promise3, ref value.Item3, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            Internal.PrepareForMerge(promise4, ref value.Item4, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise5, "promise5", 1);
            Internal.PrepareForMerge(promise5, ref value.Item5, ref passThroughs, 4, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise6, "promise6", 1);
            Internal.PrepareForMerge(promise6, ref value.Item6, ref passThroughs, 5, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise7, "promise7", 1);
            Internal.PrepareForMerge(promise7, ref value.Item7, ref passThroughs, 6, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise8, "promise8", 1);
            Internal.PrepareForMerge(promise8, ref passThroughs, 7, ref pendingCount, ref completedProgress, ref maxDepth);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(value, maxDepth);
            }
            var promise = Internal.PromiseRefBase.GetOrCreateMergePromise(passThroughs, value, pendingCount, completedProgress, maxDepth);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>(promise, promise.Id, maxDepth);
        }
    }
}