#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections.Generic;
using Proto.Utils;

namespace Proto.Promises
{
    public partial class Promise
    {
        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));

            return InternalProtected.AllPromiseVoid.GetOrCreate(passThroughs, 2);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));

            return InternalProtected.AllPromiseVoid.GetOrCreate(passThroughs, 3);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 3));

            return InternalProtected.AllPromiseVoid.GetOrCreate(passThroughs, 4);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(params Promise[] promises)
        {
            return InternalProtected.CreateAll(new ArrayEnumerator<Promise>(promises));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(IEnumerable<Promise> promises)
        {
            return InternalProtected.CreateAll(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all <paramref name="promises"/> have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            return InternalProtected.CreateAll(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All<T>(params Promise<T>[] promises)
        {
            return InternalProtected.CreateAll(new ArrayEnumerator<Promise<T>>(promises), new List<T>(promises.Length));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All<T>(IEnumerable<Promise<T>> promises)
        {
            return InternalProtected.CreateAll(promises.GetEnumerator(), new List<T>());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return InternalProtected.CreateAll(promises, new List<T>());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with <paramref name="valueContainer"/> in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> AllNonAlloc<T, TEnumerator>(TEnumerator promises, IList<T> valueContainer) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(valueContainer, "valueContainer", 1);
            return InternalProtected.CreateAll(promises, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));

            return InternalProtected.RacePromiseVoid.GetOrCreate(passThroughs, 2);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 0));

            return InternalProtected.RacePromiseVoid.GetOrCreate(passThroughs, 3);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 0));

            return InternalProtected.RacePromiseVoid.GetOrCreate(passThroughs, 4);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(params Promise[] promises)
        {
            return InternalProtected.CreateRace(new ArrayEnumerator<Promise>(promises));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(IEnumerable<Promise> promises)
        {
            return InternalProtected.CreateRace(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            return InternalProtected.CreateRace(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));

            return InternalProtected.RacePromise<T>.GetOrCreate(passThroughs, 2);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 0));

            return InternalProtected.RacePromise<T>.GetOrCreate(passThroughs, 3);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 0));

            return InternalProtected.RacePromise<T>.GetOrCreate(passThroughs, 4);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(params Promise<T>[] promises)
        {
            return InternalProtected.CreateRace<T, ArrayEnumerator<Promise<T>>>(new ArrayEnumerator<Promise<T>>(promises));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(IEnumerable<Promise<T>> promises)
        {
            return InternalProtected.CreateRace<T, IEnumerator<Promise<T>>>(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return InternalProtected.CreateRace<T, TEnumerator>(promises);
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Sequence(params Func<Promise>[] promiseFuncs)
        {
            return InternalProtected.CreateSequence(new ArrayEnumerator<Func<Promise>>(promiseFuncs));
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled with its reason.
        /// </summary>
        public static Promise Sequence(CancelationToken cancelationToken, params Func<Promise>[] promiseFuncs)
        {
            return InternalProtected.CreateSequence(new ArrayEnumerator<Func<Promise>>(promiseFuncs), cancelationToken);
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Sequence(IEnumerable<Func<Promise>> promiseFuncs)
        {
            return InternalProtected.CreateSequence(promiseFuncs.GetEnumerator());
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled with its reason.
        /// </summary>
        public static Promise Sequence(CancelationToken cancelationToken, IEnumerable<Func<Promise>> promiseFuncs)
        {
            return InternalProtected.CreateSequence(promiseFuncs.GetEnumerator(), cancelationToken);
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Sequence<TEnumerator>(TEnumerator promiseFuncs) where TEnumerator : IEnumerator<Func<Promise>>
        {
            return InternalProtected.CreateSequence(promiseFuncs);
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled with its reason.
        /// </summary>
        public static Promise Sequence<TEnumerator>(CancelationToken cancelationToken, TEnumerator promiseFuncs) where TEnumerator : IEnumerator<Func<Promise>>
        {
            return InternalProtected.CreateSequence(promiseFuncs, cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));

            return InternalProtected.FirstPromiseVoid.GetOrCreate(passThroughs, 2);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 0));

            return InternalProtected.FirstPromiseVoid.GetOrCreate(passThroughs, 3);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 0));

            return InternalProtected.FirstPromiseVoid.GetOrCreate(passThroughs, 4);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(params Promise[] promises)
        {
            return InternalProtected.CreateFirst(new ArrayEnumerator<Promise>(promises));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(IEnumerable<Promise> promises)
        {
            return InternalProtected.CreateFirst(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            return InternalProtected.CreateFirst(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));

            return InternalProtected.FirstPromise<T>.GetOrCreate(passThroughs, 2);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 0));

            return InternalProtected.FirstPromise<T>.GetOrCreate(passThroughs, 3);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 0));

            return InternalProtected.FirstPromise<T>.GetOrCreate(passThroughs, 4);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(params Promise<T>[] promises)
        {
            return InternalProtected.CreateFirst<T, ArrayEnumerator<Promise<T>>>(new ArrayEnumerator<Promise<T>>(promises));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(IEnumerable<Promise<T>> promises)
        {
            return InternalProtected.CreateFirst<T, IEnumerator<Promise<T>>>(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return InternalProtected.CreateFirst<T, TEnumerator>(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with the value of <paramref name="promise1"/> when both promises have resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T1> Merge<T1>(Promise<T1> promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));

            var val = default(T1);
            return InternalProtected.MergePromise<T1>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
                {
                    if (index == 0)
                    {
                        target.value = ((Internal.ResolveContainer<T1>) feed).value;
                    }
                }, 2);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any <see cref="Promise"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2>> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));

            var val = default(ValueTuple<T1, T2>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                if (index == 0)
                {
                    target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                }
                else
                {
                    target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                }
            }, 2);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));

            var val = default(ValueTuple<T1, T2>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                }
            }, 3);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));

            var val = default(ValueTuple<T1, T2, T3>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2, T3>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        target.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                }
            }, 3);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 3));

            var val = default(ValueTuple<T1, T2, T3>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2, T3>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        target.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                }
            }, 4);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 3));

            var val = default(ValueTuple<T1, T2, T3, T4>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2, T3, T4>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        target.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        target.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                }
            }, 4);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 3));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise5, 4));

            var val = default(ValueTuple<T1, T2, T3, T4>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2, T3, T4>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        target.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        target.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                }
            }, 5);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 3));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise5, 4));

            var val = default(ValueTuple<T1, T2, T3, T4, T5>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2, T3, T4, T5>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        target.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        target.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        target.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                }
            }, 5);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 3));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise5, 4));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise6, 5));

            var val = default(ValueTuple<T1, T2, T3, T4, T5>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2, T3, T4, T5>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        target.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        target.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        target.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                }
            }, 6);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 3));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise5, 4));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise6, 5));

            var val = default(ValueTuple<T1, T2, T3, T4, T5, T6>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2, T3, T4, T5, T6>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        target.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        target.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        target.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                    case 5:
                        target.value.Item6 = ((Internal.ResolveContainer<T6>) feed).value;
                        break;
                }
            }, 6);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 3));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise5, 4));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise6, 5));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise7, 6));

            var val = default(ValueTuple<T1, T2, T3, T4, T5, T6>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2, T3, T4, T5, T6>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        target.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        target.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        target.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                    case 5:
                        target.value.Item6 = ((Internal.ResolveContainer<T6>) feed).value;
                        break;
                }
            }, 7);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 3));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise5, 4));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise6, 5));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise7, 6));

            var val = default(ValueTuple<T1, T2, T3, T4, T5, T6, T7>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        target.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        target.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        target.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                    case 5:
                        target.value.Item6 = ((Internal.ResolveContainer<T6>) feed).value;
                        break;
                    case 6:
                        target.value.Item7 = ((Internal.ResolveContainer<T7>) feed).value;
                        break;
                }
            }, 7);
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
            var passThroughs = new ValueLinkedStack<InternalProtected.PromisePassThrough>(InternalProtected.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise4, 3));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise5, 4));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise6, 5));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise7, 6));
            passThroughs.Push(InternalProtected.PromisePassThrough.GetOrCreate(promise8, 7));

            var val = default(ValueTuple<T1, T2, T3, T4, T5, T6, T7>);
            return InternalProtected.MergePromise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>.GetOrCreate(passThroughs, ref val, (feed, target, index) =>
            {
                switch (index)
                {
                    case 0:
                        target.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        target.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        target.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        target.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        target.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                    case 5:
                        target.value.Item6 = ((Internal.ResolveContainer<T6>) feed).value;
                        break;
                    case 6:
                        target.value.Item7 = ((Internal.ResolveContainer<T7>) feed).value;
                        break;
                }
            }, 8);
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/>. <paramref name="resolver"/> is invoked immediately with a <see cref="Deferred"/> that controls the state of the new <see cref="Promise"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// </summary>
		public static Promise New(Action<Deferred> resolver)
        {
            Deferred deferred = Deferred.New();
            try
            {
                resolver.Invoke(deferred);
            }
            catch (Exception e)
            {
                if (deferred.State == State.Pending)
                {
                    deferred.Reject(e);
                }
                else
                {
                    Internal.AddRejectionToUnhandledStack(e, deferred.Promise);
                }
            }
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked immediately with a <see cref="Promise{T}.Deferred"/> that controls the state of the new <see cref="Promise{T}"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// </summary>
		public static Promise<T> New<T>(Action<Promise<T>.Deferred> resolver)
        {
            Promise<T>.Deferred deferred = Promise<T>.Deferred.New();
            try
            {
                resolver.Invoke(deferred);
            }
            catch (Exception e)
            {
                if (deferred.State == State.Pending)
                {
                    deferred.Reject(e);
                }
                else
                {
                    Internal.AddRejectionToUnhandledStack(e, deferred.Promise);
                }
            }
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/>. <paramref name="resolver"/> is invoked immediately with <paramref name="captureValue"/> and a <see cref="Deferred"/> that controls the state of the <see cref="Promise"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/>, the new <see cref="Promise"/> will be rejected with that <see cref="Exception"/>.
        /// </summary>
        public static Promise New<TCapture>(TCapture captureValue, Action<TCapture, Deferred> resolver)
        {
            Deferred deferred = Deferred.New();
            try
            {
                resolver.Invoke(captureValue, deferred);
            }
            catch (Exception e)
            {
                if (deferred.State == State.Pending)
                {
                    deferred.Reject(e);
                }
                else
                {
                    Internal.AddRejectionToUnhandledStack(e, deferred.Promise);
                }
            }
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked immediately with <paramref name="captureValue"/> and a <see cref="Promise{T}.Deferred"/> that controls the state of the new <see cref="Promise{T}"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/>, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// </summary>
        public static Promise<T> New<TCapture, T>(TCapture captureValue, Action<TCapture, Promise<T>.Deferred> resolver)
        {
            Promise<T>.Deferred deferred = Promise<T>.Deferred.New();
            try
            {
                resolver.Invoke(captureValue, deferred);
            }
            catch (Exception e)
            {
                if (deferred.State == State.Pending)
                {
                    deferred.Reject(e);
                }
                else
                {
                    Internal.AddRejectionToUnhandledStack(e, deferred.Promise);
                }
            }
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already resolved.
        /// </summary>
		public static Promise Resolved()
        {
#if PROMISE_DEBUG
            // Make new promise in DEBUG mode for separate causality traces.
            var promise = InternalProtected.DeferredPromiseVoid.GetOrCreate();
            promise.ResolveDirect();
            return promise;
#else
            // Reuse a single resolved instance.
            return InternalProtected.SettledPromise.GetOrCreateResolved();
#endif
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already resolved with <paramref name="value"/>.
        /// </summary>
		public static Promise<T> Resolved<T>(T value)
        {
            var promise = InternalProtected.DeferredPromise<T>.GetOrCreate();
            promise.ResolveDirect(ref value);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already rejected with <paramref name="reason"/>.
        /// </summary>
        public static Promise Rejected<TReject>(TReject reason)
        {
            var promise = InternalProtected.DeferredPromiseVoid.GetOrCreate();
            promise.RejectDirect(ref reason, int.MinValue);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already rejected with <paramref name="reason"/>.
        /// </summary>
        public static Promise<T> Rejected<T, TReject>(TReject reason)
        {
            var promise = InternalProtected.DeferredPromise<T>.GetOrCreate();
            promise.RejectDirect(ref reason, int.MinValue);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already canceled without a reason.
        /// </summary>
        public static Promise Canceled()
        {
#if PROMISE_DEBUG
            // Make new promise in DEBUG mode for separate causality traces.
            var promise = InternalProtected.DeferredPromiseVoid.GetOrCreate();
            promise.CancelDirect();
            return promise;
#else
            // Reuse a single canceled instance.
            return InternalProtected.SettledPromise.GetOrCreateCanceled();
#endif
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already canceled with <paramref name="reason"/>.
        /// </summary>
        public static Promise Canceled<TCancel>(TCancel reason)
        {
            var promise = InternalProtected.DeferredPromiseVoid.GetOrCreate();
            promise.CancelDirect(ref reason);
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already canceled without a reason.
        /// </summary>
        public static Promise<T> Canceled<T>()
        {
            var promise = InternalProtected.DeferredPromise<T>.GetOrCreate();
            promise.CancelDirect();
            return promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already canceled with <paramref name="reason"/>.
        /// </summary>
        public static Promise<T> Canceled<T, TCancel>(TCancel reason)
        {
            var promise = InternalProtected.DeferredPromise<T>.GetOrCreate();
            promise.CancelDirect(ref reason);
            return promise;
        }

        /// <summary>
        /// Returns a new <see cref="Deferred"/> instance that is linked to and controls the state of a new <see cref="Promise"/>.
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Deferred"/> is pending, it and the <see cref="Promise"/> will be canceled with its reason.
        /// </summary>
        public static Deferred NewDeferred(CancelationToken cancelationToken = default(CancelationToken))
        {
            return Deferred.New(cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}.Deferred"/> object that is linked to and controls the state of a new <see cref="Promise{T}"/>.
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Promise{T}.Deferred"/> is pending, it and the <see cref="Promise{T}"/> will be canceled with its reason.
        /// </summary>
        public static Promise<T>.Deferred NewDeferred<T>(CancelationToken cancelationToken = default(CancelationToken))
        {
            return Promise<T>.Deferred.New(cancelationToken);
        }

        /// <summary>
        /// Get a <see cref="RethrowException"/> that can be thrown inside an onRejected callback to rethrow the caught rejection, preserving the stack trace.
        /// This should be used as "throw Promise.Rethrow;"
        /// This is similar to "throw;" in a synchronous catch clause.
        /// </summary>
        /// <value>The rethrow.</value>
        public static RethrowException Rethrow
        {
            get
            {
                if (Internal.invokingRejected)
                {
                    return RethrowException.instance;
                }
                throw new InvalidOperationException("Rethrow can only be accessed inside an onRejected callback.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Get a <see cref="CancelException"/> that can be thrown to cancel the promise without a reason from an onResolved or onRejected callback, or in an async Promise function.
        /// This should be used as "throw Promise.CancelException();"
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public static CancelException CancelException()
        {
            return Internal.CancelExceptionVoidInternal.GetOrCreate();
        }

        /// <summary>
        /// Get a <see cref="Promises.CancelException"/> that can be thrown to cancel the promise with the provided reason from an onResolved or onRejected callback, or in an async Promise function.
        /// This should be used as "throw Promise.CancelException(value);"
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public static CancelException CancelException<T>(T value)
        {
            return Internal.CancelExceptionInternal<T>.GetOrCreate(value);
        }

        /// <summary>
        /// Get a <see cref="Promises.RejectException"/> that can be thrown to reject the promise from an onResolved or onRejected callback, or in an async Promise function.
        /// This should be used as "throw Promise.RejectException(value);"
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public static RejectException RejectException<T>(T value)
        {
            return Internal.RejectExceptionInternal<T>.GetOrCreate(value);
        }
    }
}