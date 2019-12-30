#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif

using System;
using System.Collections.Generic;
using Proto.Utils;

namespace Proto.Promises
{
    public partial class Promise
    {
        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(params Promise[] promises)
        {
            return Internal._All(new ArrayEnumerator<Promise>(promises), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(IEnumerable<Promise> promises)
        {
            return Internal._All(promises.GetEnumerator(), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise AllNonAlloc<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            return Internal._All(promises, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise AllNonAlloc(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));

            return Internal.AllPromise0.GetOrCreate(passThroughs, 2, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise AllNonAlloc(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));

            return Internal.AllPromise0.GetOrCreate(passThroughs, 3, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise AllNonAlloc(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 3, 1));

            return Internal.AllPromise0.GetOrCreate(passThroughs, 4, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All<T>(params Promise<T>[] promises)
        {
            return Internal._All(new ArrayEnumerator<Promise<T>>(promises), new List<T>(promises.Length), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All<T>(IEnumerable<Promise<T>> promises)
        {
            return Internal._All(promises.GetEnumerator(), new List<T>(), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with <paramref name="valueContainer"/> in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> AllNonAlloc<T, TEnumerator>(TEnumerator promises, IList<T> valueContainer) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(valueContainer, "valueContainer", 1);
            return Internal._All(promises, valueContainer, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(params Promise[] promises)
        {
            return Internal._Race(new ArrayEnumerator<Promise>(promises), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(IEnumerable<Promise> promises)
        {
            return Internal._Race(promises.GetEnumerator(), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise RaceNonAlloc<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            return Internal._Race(promises, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise RaceNonAlloc(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));

            return Internal.RacePromise0.GetOrCreate(passThroughs, 2, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise RaceNonAlloc(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 0, 1));

            return Internal.RacePromise0.GetOrCreate(passThroughs, 3, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise RaceNonAlloc(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 0, 1));

            return Internal.RacePromise0.GetOrCreate(passThroughs, 4, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(params Promise<T>[] promises)
        {
            return Internal._Race<T, ArrayEnumerator<Promise<T>>>(new ArrayEnumerator<Promise<T>>(promises), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(IEnumerable<Promise<T>> promises)
        {
            return Internal._Race<T, IEnumerator<Promise<T>>>(promises.GetEnumerator(), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> RaceNonAlloc<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Internal._Race<T, IEnumerator<Promise<T>>>(promises, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise RaceNonAlloc<T>(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));

            return Internal.RacePromise<T>.GetOrCreate(passThroughs, 2, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise RaceNonAlloc<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 0, 1));

            return Internal.RacePromise<T>.GetOrCreate(passThroughs, 3, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise RaceNonAlloc<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 0, 1));

            return Internal.RacePromise<T>.GetOrCreate(passThroughs, 4, 1);
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Sequence(params Func<Promise>[] promiseFuncs)
        {
            return Internal._Sequence(new ArrayEnumerator<Func<Promise>>(promiseFuncs), 1);
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Sequence(IEnumerable<Func<Promise>> promiseFuncs)
        {
            return Internal._Sequence(promiseFuncs.GetEnumerator(), 1);
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise SequenceNonAlloc<TEnumerator>(TEnumerator promiseFuncs) where TEnumerator : IEnumerator<Func<Promise>>
        {
            return Internal._Sequence(promiseFuncs, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(params Promise[] promises)
        {
            return Internal._First(new ArrayEnumerator<Promise>(promises), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(IEnumerable<Promise> promises)
        {
            return Internal._First(promises.GetEnumerator(), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise FirstNonAlloc<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            return Internal._First(promises, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise FirstNonAlloc(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));

            return Internal.FirstPromise0.GetOrCreate(passThroughs, 2, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise FirstNonAlloc(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 0, 1));

            return Internal.FirstPromise0.GetOrCreate(passThroughs, 3, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise FirstNonAlloc(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 0, 1));

            return Internal.FirstPromise0.GetOrCreate(passThroughs, 4, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(params Promise<T>[] promises)
        {
            return Internal._First<T, ArrayEnumerator<Promise<T>>>(new ArrayEnumerator<Promise<T>>(promises), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(IEnumerable<Promise<T>> promises)
        {
            return Internal._First<T, IEnumerator<Promise<T>>>(promises.GetEnumerator(), 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> FirstNonAlloc<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Internal._First<T, TEnumerator>(promises, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise FirstNonAlloc<T>(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));

            return Internal.FirstPromise<T>.GetOrCreate(passThroughs, 2, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise FirstNonAlloc<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 0, 1));

            return Internal.FirstPromise<T>.GetOrCreate(passThroughs, 3, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise FirstNonAlloc<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 0, 1));

            return Internal.FirstPromise<T>.GetOrCreate(passThroughs, 4, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the value of <paramref name="promise1"/> when both promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T1> Merge<T1>(Promise<T1> promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));

            return Internal.MergePromise<T1>.GetOrCreate(passThroughs, (feed, target, index) =>
                {
                    if (index == 0)
                    {
                        target._value = ((Internal.PromiseInternal<T1>) feed)._value;
                    }
                }, 2, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2>> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));

            return Internal.MergePromise<ValueTuple<T1, T2>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                if (index == 0)
                {
                    target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                }
                else
                {
                    target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                }
            }, 2, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2>> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));

            return Internal.MergePromise<ValueTuple<T1, T2>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                }
            }, 3, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3>> Merge<T1, T2, T3>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));

            return Internal.MergePromise<ValueTuple<T1, T2, T3>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                    case 2:
                        target._value.Item3 = ((Internal.PromiseInternal<T3>) feed)._value;
                        break;
                }
            }, 3, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3>> Merge<T1, T2, T3>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 3, 1));

            return Internal.MergePromise<ValueTuple<T1, T2, T3>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                    case 2:
                        target._value.Item3 = ((Internal.PromiseInternal<T3>) feed)._value;
                        break;
                }
            }, 4, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4>> Merge<T1, T2, T3, T4>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 3, 1));

            return Internal.MergePromise<ValueTuple<T1, T2, T3, T4>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                    case 2:
                        target._value.Item3 = ((Internal.PromiseInternal<T3>) feed)._value;
                        break;
                    case 3:
                        target._value.Item4 = ((Internal.PromiseInternal<T4>) feed)._value;
                        break;
                }
            }, 4, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4>> Merge<T1, T2, T3, T4>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            ValidateArgument(promise5, "promise5", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 3, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise5, 4, 1));

            return Internal.MergePromise<ValueTuple<T1, T2, T3, T4>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                    case 2:
                        target._value.Item3 = ((Internal.PromiseInternal<T3>) feed)._value;
                        break;
                    case 3:
                        target._value.Item4 = ((Internal.PromiseInternal<T4>) feed)._value;
                        break;
                }
            }, 5, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5>> Merge<T1, T2, T3, T4, T5>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            ValidateArgument(promise5, "promise5", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 3, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise5, 4, 1));

            return Internal.MergePromise<ValueTuple<T1, T2, T3, T4, T5>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                    case 2:
                        target._value.Item3 = ((Internal.PromiseInternal<T3>) feed)._value;
                        break;
                    case 3:
                        target._value.Item4 = ((Internal.PromiseInternal<T4>) feed)._value;
                        break;
                    case 4:
                        target._value.Item5 = ((Internal.PromiseInternal<T5>) feed)._value;
                        break;
                }
            }, 5, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5>> Merge<T1, T2, T3, T4, T5>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise promise6)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            ValidateArgument(promise5, "promise5", 1);
            ValidateArgument(promise6, "promise6", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 3, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise5, 4, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise6, 5, 1));

            return Internal.MergePromise<ValueTuple<T1, T2, T3, T4, T5>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                    case 2:
                        target._value.Item3 = ((Internal.PromiseInternal<T3>) feed)._value;
                        break;
                    case 3:
                        target._value.Item4 = ((Internal.PromiseInternal<T4>) feed)._value;
                        break;
                    case 4:
                        target._value.Item5 = ((Internal.PromiseInternal<T5>) feed)._value;
                        break;
                }
            }, 6, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6>> Merge<T1, T2, T3, T4, T5, T6>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            ValidateArgument(promise5, "promise5", 1);
            ValidateArgument(promise6, "promise6", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 3, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise5, 4, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise6, 5, 1));

            return Internal.MergePromise<ValueTuple<T1, T2, T3, T4, T5, T6>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                    case 2:
                        target._value.Item3 = ((Internal.PromiseInternal<T3>) feed)._value;
                        break;
                    case 3:
                        target._value.Item4 = ((Internal.PromiseInternal<T4>) feed)._value;
                        break;
                    case 4:
                        target._value.Item5 = ((Internal.PromiseInternal<T5>) feed)._value;
                        break;
                    case 5:
                        target._value.Item6 = ((Internal.PromiseInternal<T6>) feed)._value;
                        break;
                }
            }, 6, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6>> Merge<T1, T2, T3, T4, T5, T6>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise promise7)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            ValidateArgument(promise5, "promise5", 1);
            ValidateArgument(promise6, "promise6", 1);
            ValidateArgument(promise7, "promise7", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 3, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise5, 4, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise6, 5, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise7, 6, 1));

            return Internal.MergePromise<ValueTuple<T1, T2, T3, T4, T5, T6>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                    case 2:
                        target._value.Item3 = ((Internal.PromiseInternal<T3>) feed)._value;
                        break;
                    case 3:
                        target._value.Item4 = ((Internal.PromiseInternal<T4>) feed)._value;
                        break;
                    case 4:
                        target._value.Item5 = ((Internal.PromiseInternal<T5>) feed)._value;
                        break;
                    case 5:
                        target._value.Item6 = ((Internal.PromiseInternal<T6>) feed)._value;
                        break;
                }
            }, 7, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> Merge<T1, T2, T3, T4, T5, T6, T7>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise<T7> promise7)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            ValidateArgument(promise5, "promise5", 1);
            ValidateArgument(promise6, "promise6", 1);
            ValidateArgument(promise7, "promise7", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 3, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise5, 4, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise6, 5, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise7, 6, 1));

            return Internal.MergePromise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                    case 2:
                        target._value.Item3 = ((Internal.PromiseInternal<T3>) feed)._value;
                        break;
                    case 3:
                        target._value.Item4 = ((Internal.PromiseInternal<T4>) feed)._value;
                        break;
                    case 4:
                        target._value.Item5 = ((Internal.PromiseInternal<T5>) feed)._value;
                        break;
                    case 5:
                        target._value.Item6 = ((Internal.PromiseInternal<T6>) feed)._value;
                        break;
                    case 6:
                        target._value.Item7 = ((Internal.PromiseInternal<T7>) feed)._value;
                        break;
                }
            }, 7, 1);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> Merge<T1, T2, T3, T4, T5, T6, T7>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise<T7> promise7, Promise promise8)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            ValidateArgument(promise5, "promise5", 1);
            ValidateArgument(promise6, "promise6", 1);
            ValidateArgument(promise7, "promise7", 1);
            ValidateArgument(promise8, "promise8", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromisePassThrough>(Internal.PromisePassThrough.GetOrCreate(promise1, 0, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise2, 1, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise3, 2, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise4, 3, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise5, 4, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise6, 5, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise7, 6, 1));
            passThroughs.Push(Internal.PromisePassThrough.GetOrCreate(promise8, 7, 1));

            return Internal.MergePromise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>.GetOrCreate(passThroughs, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target._value.Item1 = ((Internal.PromiseInternal<T1>) feed)._value;
                        break;
                    case 1:
                        target._value.Item2 = ((Internal.PromiseInternal<T2>) feed)._value;
                        break;
                    case 2:
                        target._value.Item3 = ((Internal.PromiseInternal<T3>) feed)._value;
                        break;
                    case 3:
                        target._value.Item4 = ((Internal.PromiseInternal<T4>) feed)._value;
                        break;
                    case 4:
                        target._value.Item5 = ((Internal.PromiseInternal<T5>) feed)._value;
                        break;
                    case 5:
                        target._value.Item6 = ((Internal.PromiseInternal<T6>) feed)._value;
                        break;
                    case 6:
                        target._value.Item7 = ((Internal.PromiseInternal<T7>) feed)._value;
                        break;
                }
            }, 8, 1);
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/>. <paramref name="resolver"/> is invoked immediately with a <see cref="Deferred"/> that controls the state of the promise.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// </summary>
		public static Promise New(Action<Deferred> resolver)
        {
            var promise = Internal.DeferredPromise0.GetOrCreate(1);
            try
            {
                resolver.Invoke(promise.deferred);
            }
            catch (Exception e)
            {
                var deferred = promise.deferred;
                if (deferred.State == State.Pending)
                {
                    deferred.Reject(e);
                }
                else
                {
                    var rejectValue = Internal.UnhandledExceptionException.GetOrCreate(e);
                    _SetStackTraceFromCreated(promise, rejectValue);
                    AddRejectionToUnhandledStack(rejectValue);
                }
            }
            return promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked immediately with a <see cref="Deferred"/> that controls the state of the promise.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// </summary>
		public static Promise<T> New<T>(Action<Promise<T>.Deferred> resolver)
        {
            var promise = Internal.DeferredPromise<T>.GetOrCreate(1);
            try
            {
                resolver.Invoke(promise.deferred);
            }
            catch (Exception e)
            {
                var deferred = promise.deferred;
                if (deferred.State == State.Pending)
                {
                    deferred.Reject(e);
                }
                else
                {
                    var rejectValue = Internal.UnhandledExceptionException.GetOrCreate(e);
                    _SetStackTraceFromCreated(promise, rejectValue);
                    AddRejectionToUnhandledStack(rejectValue);
                }
            }
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already resolved.
        /// </summary>
		public static Promise Resolved()
        {
#if PROMISE_DEBUG
            // Create new because stack trace can be different.
            var promise = Internal.LitePromise0.GetOrCreate(1);
            promise.ResolveDirect();
            return promise;
#else
            // Reuse a single resolved instance.
            return Internal.ResolvedPromise.instance;
#endif
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already resolved with <paramref name="value"/>.
        /// </summary>
		public static Promise<T> Resolved<T>(T value)
        {
            var promise = Internal.LitePromise<T>.GetOrCreate(1);
            promise.ResolveDirect(value);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already rejected without a reason.
        /// </summary>
        public static Promise Rejected()
        {
            var promise = Internal.LitePromise0.GetOrCreate(1);
            var rejection = CreateRejection(1);
            promise.RejectDirect(rejection);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already rejected with <paramref name="reason"/>.
        /// </summary>
        public static Promise Rejected<TReject>(TReject reason)
        {
            var promise = Internal.LitePromise0.GetOrCreate(1);
            var rejection = CreateRejection(reason, 1);
            promise.RejectDirect(rejection);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already rejected without a reason.
        /// </summary>
        public static Promise<T> Rejected<T>()
        {
            var promise = Internal.LitePromise<T>.GetOrCreate(1);
            var rejection = CreateRejection(1);
            promise.RejectDirect(rejection);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already rejected with <paramref name="reason"/>.
        /// </summary>
        public static Promise<T> Rejected<T, TReject>(TReject reason)
        {
            var promise = Internal.LitePromise<T>.GetOrCreate(1);
            var rejection = CreateRejection(reason, 1);
            promise.RejectDirect(rejection);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already canceled without a reason.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public static Promise Canceled()
        {
            ValidateCancel(1);

            var promise = Internal.LitePromise0.GetOrCreate(1);
            promise.Cancel();
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already canceled with <paramref name="reason"/>.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public static Promise Canceled<TCancel>(TCancel reason)
        {
            ValidateCancel(1);

            var promise = Internal.LitePromise0.GetOrCreate(1);
            promise.Cancel(reason);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already canceled without a reason.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public static Promise<T> Canceled<T>()
        {
            ValidateCancel(1);

            var promise = Internal.LitePromise<T>.GetOrCreate(1);
            promise.Cancel();
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already canceled with <paramref name="reason"/>.
        /// </summary>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public static Promise<T> Canceled<T, TCancel>(TCancel reason)
        {
            ValidateCancel(1);

            var promise = Internal.LitePromise<T>.GetOrCreate(1);
            promise.Cancel(reason);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Deferred"/> object that is linked to and controls the state of a new <see cref="Promise"/>.
        /// </summary>
		public static Deferred NewDeferred()
        {
            return Internal.DeferredPromise0.GetOrCreate(1).deferred;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}.Deferred"/> object that is linked to and controls the state of a new <see cref="Promise{T}"/>.
        /// </summary>
        public static Promise<T>.Deferred NewDeferred<T>()
        {
            return Internal.DeferredPromise<T>.GetOrCreate(1).deferred;
        }

        /// <summary>
        /// Get a <see cref="RethrowException"/> that can be thrown inside an onRejected callback to rethrow the caught rejection, preserving the stacktrace.
        /// This should be used as "throw Promise.Rethrow;"
        /// This is similar to "throw;" in a synchronous catch clause.
        /// </summary>
        /// <value>The rethrow.</value>
        public static RethrowException Rethrow
        {
            get
            {
                if (Internal._invokingRejected)
                {
                    // Ensure _rethrow is set by the static constructor.
                    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(RethrowException).TypeHandle);
                    return _rethrow;
                }
                throw new InvalidOperationException("Rethrow can only be accessed inside an onRejected callback.", GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Get a <see cref="CanceledException"/> that can be thrown to cancel the promise without a reason from an onResolved or onRejected callback.
        /// This should be used as "throw Promise.CancelException();"
        /// <para/>
        /// If this is called while not inside an onResolved or onRejected handler, this will throw an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public static CanceledException CancelException()
        {
#if !PROMISE_CANCEL
            ThrowCancelException(1);
            return null;
#else
            if (Internal._invokingResolved | Internal._invokingRejected)
            {
                return Internal.CancelVoid.GetOrCreate();
            }
            throw new InvalidOperationException("CancelationException can only be accessed inside an onResolved or onRejected callback.", GetFormattedStacktrace(1));
#endif
        }

        /// <summary>
        /// Get a <see cref="CanceledException"/> that can be thrown to cancel the promise with the provided reason from an onResolved or onRejected callback.
        /// This should be used as "throw Promise.CancelException(value);"
        /// <para/>
        /// If this is called while not inside an onResolved or onRejected handler, this will throw an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
#if !PROMISE_CANCEL
        [Obsolete("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", true)]
#endif
        public static CanceledException CancelException<T>(T value)
        {
#if !PROMISE_CANCEL
            ThrowCancelException(1);
            return null;
#else
            if (Internal._invokingResolved | Internal._invokingRejected)
            {
                return Internal.CancelValue<T>.GetOrCreate(value);
            }
            throw new InvalidOperationException("CancelationException can only be accessed inside an onResolved or onRejected callback.", GetFormattedStacktrace(1));
#endif
        }

        /// <summary>
        /// Get an <see cref="Exception"/> that can be thrown to cancel the promise from an onResolved or onRejected callback.
        /// This should be used as "throw Promise.RejectException();"
        /// <para/>
        /// If this is called while not inside an onRejected handler, this will throw an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public static Exception RejectException()
        {
            if (Internal._invokingResolved | Internal._invokingRejected)
            {
                return Internal.UnhandledExceptionVoid.GetOrCreate();
            }
            throw new InvalidOperationException("RejectedException can only be accessed inside an onResolved or onRejected callback.", GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Get an <see cref="Exception"/> that can be thrown to cancel the promise from an onResolved or onRejected callback.
        /// This should be used as "throw Promise.RejectException(value);"
        /// <para/>
        /// If this is called while not inside an onResolved or onRejected handler, this will throw an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public static Exception RejectException<T>(T value)
        {
            if (Internal._invokingResolved | Internal._invokingRejected)
            {
                if (typeof(Exception).IsAssignableFrom(typeof(T)))
                {
                    // No need to wrap the exception, just return it as-is.
                    Logger.LogWarning("An exception was passed to RejectedException, returning that exception as-is.");
                    return value as Exception;
                }
                return Internal.UnhandledException<T>.GetOrCreate(value);
            }
            throw new InvalidOperationException("RejectedException can only be accessed inside an onResolved or onRejected callback.", GetFormattedStacktrace(1));
        }
    }
}