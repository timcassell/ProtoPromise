#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections.Generic;
using Proto.Utils;

namespace Proto.Promises
{
    public partial struct Promise
    {
        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null | promise2._ref == null)
            {
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise1, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise2, false);
                return Internal.CreateResolved();
            }
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise2, 1));

            var promise = Internal.PromiseRef.RacePromise.GetOrCreate(passThroughs, 2);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2, Promise promise3)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null)
            {
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise1, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise2, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise3, false);
                return Internal.CreateResolved();
            }
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise3, 2));

            var promise = Internal.PromiseRef.RacePromise.GetOrCreate(passThroughs, 3);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null | promise4._ref == null)
            {
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise1, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise2, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise3, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise4, false);
                return Internal.CreateResolved();
            }
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise4, 3));

            var promise = Internal.PromiseRef.RacePromise.GetOrCreate(passThroughs, 4);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(params Promise[] promises)
        {
            return Race(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(IEnumerable<Promise> promises)
        {
            return Race(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, "promises", 1);
            if (!promises.MoveNext())
            {
                throw new EmptyArgumentException("promises", "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
            }
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            int i = 0; // Index isn't necessary for Race, but might help with debugging.

            do
            {
                var p = promises.Current;
                ValidateElement(p, "promises", 1);
                if (Internal.PrepareForMulti(p, ref passThroughs, i++) == 0)
                {
                    // Validate and release remaining elements.
                    while (promises.MoveNext())
                    {
                        p = promises.Current;
                        ValidateElement(p, "promises", 1);
                        Internal.PromiseRef.MaybeMarkAwaitedAndDispose(p, false);
                    }
                    // Repool any created passthroughs.
                    foreach (var passthrough in passThroughs)
                    {
                        passthrough.Release();
                    }
                    return Internal.CreateResolved();
                }
                ++pendingCount;
            } while (promises.MoveNext());

            var promise = Internal.PromiseRef.RacePromise.GetOrCreate(passThroughs, pendingCount);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2)
        {
            return Promise<T>.Race(promise1, promise2);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            return Promise<T>.Race(promise1, promise2, promise3);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            return Promise<T>.Race(promise1, promise2, promise3, promise4);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(params Promise<T>[] promises)
        {
            return Promise<T>.Race(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(IEnumerable<Promise<T>> promises)
        {
            return Promise<T>.Race(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise<T>.Race(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null | promise2._ref == null)
            {
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise1, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise2, false);
                return Internal.CreateResolved();
            }
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise2, 1));

            var promise = Internal.PromiseRef.FirstPromise.GetOrCreate(passThroughs, 2);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2, Promise promise3)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null)
            {
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise1, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise2, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise3, false);
                return Internal.CreateResolved();
            }
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise3, 2));

            var promise = Internal.PromiseRef.FirstPromise.GetOrCreate(passThroughs, 3);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null | promise4._ref == null)
            {
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise1, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise2, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise3, false);
                Internal.PromiseRef.MaybeMarkAwaitedAndDispose(promise4, false);
                return Internal.CreateResolved();
            }
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(Internal.PromiseRef.PromisePassThrough.GetOrCreate(promise4, 3));

            var promise = Internal.PromiseRef.FirstPromise.GetOrCreate(passThroughs, 4);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(params Promise[] promises)
        {
            return First(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(IEnumerable<Promise> promises)
        {
            return First(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, "promises", 1);
            if (!promises.MoveNext())
            {
                throw new EmptyArgumentException("promises", "You must provide at least one element to First.", Internal.GetFormattedStacktrace(1));
            }
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            int i = 0; // Index isn't necessary for First, but might help with debugging.

            do
            {
                var p = promises.Current;
                ValidateElement(p, "promises", 1);
                if (Internal.PrepareForMulti(p, ref passThroughs, i++) == 0)
                {
                    // Validate and release remaining elements.
                    while (promises.MoveNext())
                    {
                        p = promises.Current;
                        ValidateElement(p, "promises", 1);
                        Internal.PromiseRef.MaybeMarkAwaitedAndDispose(p, true);
                    }
                    // Repool any created passthroughs.
                    foreach (var passthrough in passThroughs)
                    {
                        passthrough.Release();
                    }
                    return Internal.CreateResolved();
                }
                ++pendingCount;
            } while (promises.MoveNext());

            var promise = Internal.PromiseRef.FirstPromise.GetOrCreate(passThroughs, pendingCount);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2)
        {
            return Promise<T>.First(promise1, promise2);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            return Promise<T>.First(promise1, promise2, promise3);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            return Promise<T>.First(promise1, promise2, promise3, promise4);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(params Promise<T>[] promises)
        {
            return Promise<T>.First(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(IEnumerable<Promise<T>> promises)
        {
            return Promise<T>.First(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise<T>.First(promises);
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Sequence(params Func<Promise>[] promiseFuncs)
        {
            return Sequence(default(CancelationToken), promiseFuncs.GetGenericEnumerator());
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled with its reason.
        /// </summary>
        public static Promise Sequence(CancelationToken cancelationToken, params Func<Promise>[] promiseFuncs)
        {
            return Sequence(cancelationToken, promiseFuncs.GetGenericEnumerator());
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Sequence(IEnumerable<Func<Promise>> promiseFuncs)
        {
            return Sequence(default(CancelationToken), promiseFuncs.GetEnumerator());
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled with its reason.
        /// </summary>
        public static Promise Sequence(CancelationToken cancelationToken, IEnumerable<Func<Promise>> promiseFuncs)
        {
            return Sequence(cancelationToken, promiseFuncs.GetEnumerator());
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Sequence<TEnumerator>(TEnumerator promiseFuncs) where TEnumerator : IEnumerator<Func<Promise>>
        {
            return Sequence(default(CancelationToken), promiseFuncs);
        }

        /// <summary>
        /// Runs <paramref name="promiseFuncs"/> in sequence, returning a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// 
        /// <para/>If the <paramref name="cancelationToken"/> is canceled before all of the <paramref name="promiseFuncs"/> have been invoked,
        /// the <paramref name="promiseFuncs"/> will stop being invoked, and the returned <see cref="Promise"/> will be canceled with its reason.
        /// </summary>
        public static Promise Sequence<TEnumerator>(CancelationToken cancelationToken, TEnumerator promiseFuncs) where TEnumerator : IEnumerator<Func<Promise>>
        {
            return Internal.PromiseRef.CreateSequence(promiseFuncs, cancelationToken);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(Promise promise1, Promise promise2)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref passThroughs, 1, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved();
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, pendingCount, 2, completedProgress);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(Promise promise1, Promise promise2, Promise promise3)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref passThroughs, 2, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved();
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, pendingCount, 3, completedProgress);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise All(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref passThroughs, 2, ref completedProgress);
            ValidateArgument(promise4, "promise4", 1);
            pendingCount += Internal.PrepareForMulti(promise4, ref passThroughs, 3, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved();
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, pendingCount, 4, completedProgress);
            return new Promise(promise, promise.Id);
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
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            uint totalCount = 0;
            ulong completedProgress = 0;

            int i = 0;
            while (promises.MoveNext())
            {
                var p = promises.Current;
                ValidateElement(p, "promises", 1);
                pendingCount += Internal.PrepareForMulti(p, ref passThroughs, i++, ref completedProgress);
                ++totalCount;
            }

            if (pendingCount == 0)
            {
                return Internal.CreateResolved();
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, pendingCount, totalCount, completedProgress);
            return new Promise(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, IList<T> valueContainer = null)
        {
            return Promise<T>.All(promise1, promise2, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, IList<T> valueContainer = null)
        {
            return Promise<T>.All(promise1, promise2, promise3, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4, IList<T> valueContainer = null)
        {
            return Promise<T>.All(promise1, promise2, promise3, promise4, valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All<T>(params Promise<T>[] promises)
        {
            return Promise<T>.All(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All<T>(IEnumerable<Promise<T>> promises)
        {
            return Promise<T>.All(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise<T>.All(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with <paramref name="valueContainer"/> where the values are in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any <see cref="Promise{T}"/> is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
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
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref passThroughs, 1, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                if (index == 0)
                {
                    ((Internal.ResolveContainer<T1>) target).value = ((Internal.ResolveContainer<T1>) feed).value;
                }
            }, pendingCount, 2, completedProgress);
            return new Promise<T1>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2>> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2)
        {
            var value = new ValueTuple<T1, T2>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                if (index == 0)
                {
                    ((Internal.ResolveContainer<ValueTuple<T1, T2>>) target).value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                }
                else
                {
                    ((Internal.ResolveContainer<ValueTuple<T1, T2>>) target).value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                }
            }, pendingCount, 2, completedProgress);
            return new Promise<ValueTuple<T1, T2>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2>> Merge<T1, T2>(Promise<T1> promise1, Promise<T2> promise2, Promise promise3)
        {
            var value = new ValueTuple<T1, T2>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref passThroughs, 2, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                }
            }, pendingCount, 3, completedProgress);
            return new Promise<ValueTuple<T1, T2>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3>> Merge<T1, T2, T3>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3)
        {
            var value = new ValueTuple<T1, T2, T3>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref value.Item3, ref passThroughs, 2, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2, T3>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        container.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                }
            }, pendingCount, 3, completedProgress);
            return new Promise<ValueTuple<T1, T2, T3>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3>> Merge<T1, T2, T3>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise promise4)
        {
            var value = new ValueTuple<T1, T2, T3>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref value.Item3, ref passThroughs, 2, ref completedProgress);
            ValidateArgument(promise4, "promise4", 1);
            pendingCount += Internal.PrepareForMulti(promise4, ref passThroughs, 3, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2, T3>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        container.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                }
            }, pendingCount, 4, completedProgress);
            return new Promise<ValueTuple<T1, T2, T3>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4>> Merge<T1, T2, T3, T4>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4)
        {
            var value = new ValueTuple<T1, T2, T3, T4>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref value.Item3, ref passThroughs, 2, ref completedProgress);
            ValidateArgument(promise4, "promise4", 1);
            pendingCount += Internal.PrepareForMulti(promise4, ref value.Item4, ref passThroughs, 3, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2, T3, T4>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        container.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        container.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                }
            }, pendingCount, 4, completedProgress);
            return new Promise<ValueTuple<T1, T2, T3, T4>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4>> Merge<T1, T2, T3, T4>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise promise5)
        {
            var value = new ValueTuple<T1, T2, T3, T4>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref value.Item3, ref passThroughs, 2, ref completedProgress);
            ValidateArgument(promise4, "promise4", 1);
            pendingCount += Internal.PrepareForMulti(promise4, ref value.Item4, ref passThroughs, 3, ref completedProgress);
            ValidateArgument(promise5, "promise5", 1);
            pendingCount += Internal.PrepareForMulti(promise5, ref passThroughs, 4, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2, T3, T4>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        container.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        container.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                }
            }, pendingCount, 5, completedProgress);
            return new Promise<ValueTuple<T1, T2, T3, T4>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5>> Merge<T1, T2, T3, T4, T5>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref value.Item3, ref passThroughs, 2, ref completedProgress);
            ValidateArgument(promise4, "promise4", 1);
            pendingCount += Internal.PrepareForMulti(promise4, ref value.Item4, ref passThroughs, 3, ref completedProgress);
            ValidateArgument(promise5, "promise5", 1);
            pendingCount += Internal.PrepareForMulti(promise5, ref value.Item5, ref passThroughs, 4, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2, T3, T4, T5>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        container.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        container.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        container.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                }
            }, pendingCount, 5, completedProgress);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5>> Merge<T1, T2, T3, T4, T5>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise promise6)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref value.Item3, ref passThroughs, 2, ref completedProgress);
            ValidateArgument(promise4, "promise4", 1);
            pendingCount += Internal.PrepareForMulti(promise4, ref value.Item4, ref passThroughs, 3, ref completedProgress);
            ValidateArgument(promise5, "promise5", 1);
            pendingCount += Internal.PrepareForMulti(promise5, ref value.Item5, ref passThroughs, 4, ref completedProgress);
            ValidateArgument(promise6, "promise6", 1);
            pendingCount += Internal.PrepareForMulti(promise6, ref passThroughs, 5, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2, T3, T4, T5>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        container.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        container.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        container.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                }
            }, pendingCount, 6, completedProgress);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6>> Merge<T1, T2, T3, T4, T5, T6>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5, T6>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref value.Item3, ref passThroughs, 2, ref completedProgress);
            ValidateArgument(promise4, "promise4", 1);
            pendingCount += Internal.PrepareForMulti(promise4, ref value.Item4, ref passThroughs, 3, ref completedProgress);
            ValidateArgument(promise5, "promise5", 1);
            pendingCount += Internal.PrepareForMulti(promise5, ref value.Item5, ref passThroughs, 4, ref completedProgress);
            ValidateArgument(promise6, "promise6", 1);
            pendingCount += Internal.PrepareForMulti(promise6, ref value.Item6, ref passThroughs, 5, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2, T3, T4, T5, T6>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        container.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        container.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        container.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                    case 5:
                        container.value.Item6 = ((Internal.ResolveContainer<T6>) feed).value;
                        break;
                }
            }, pendingCount, 6, completedProgress);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5, T6>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6>> Merge<T1, T2, T3, T4, T5, T6>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise promise7)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5, T6>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref value.Item3, ref passThroughs, 2, ref completedProgress);
            ValidateArgument(promise4, "promise4", 1);
            pendingCount += Internal.PrepareForMulti(promise4, ref value.Item4, ref passThroughs, 3, ref completedProgress);
            ValidateArgument(promise5, "promise5", 1);
            pendingCount += Internal.PrepareForMulti(promise5, ref value.Item5, ref passThroughs, 4, ref completedProgress);
            ValidateArgument(promise6, "promise6", 1);
            pendingCount += Internal.PrepareForMulti(promise6, ref value.Item6, ref passThroughs, 5, ref completedProgress);
            ValidateArgument(promise7, "promise7", 1);
            pendingCount += Internal.PrepareForMulti(promise7, ref passThroughs, 6, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2, T3, T4, T5, T6>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        container.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        container.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        container.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                    case 5:
                        container.value.Item6 = ((Internal.ResolveContainer<T6>) feed).value;
                        break;
                }
            }, pendingCount, 7, completedProgress);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5, T6>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> Merge<T1, T2, T3, T4, T5, T6, T7>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise<T7> promise7)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5, T6, T7>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref value.Item3, ref passThroughs, 2, ref completedProgress);
            ValidateArgument(promise4, "promise4", 1);
            pendingCount += Internal.PrepareForMulti(promise4, ref value.Item4, ref passThroughs, 3, ref completedProgress);
            ValidateArgument(promise5, "promise5", 1);
            pendingCount += Internal.PrepareForMulti(promise5, ref value.Item5, ref passThroughs, 4, ref completedProgress);
            ValidateArgument(promise6, "promise6", 1);
            pendingCount += Internal.PrepareForMulti(promise6, ref value.Item6, ref passThroughs, 5, ref completedProgress);
            ValidateArgument(promise7, "promise7", 1);
            pendingCount += Internal.PrepareForMulti(promise7, ref value.Item7, ref passThroughs, 6, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        container.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        container.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        container.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                    case 5:
                        container.value.Item6 = ((Internal.ResolveContainer<T6>) feed).value;
                        break;
                    case 6:
                        container.value.Item7 = ((Internal.ResolveContainer<T7>) feed).value;
                        break;
                }
            }, pendingCount, 7, completedProgress);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/> that will resolve with the values of the promises when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> Merge<T1, T2, T3, T4, T5, T6, T7>(Promise<T1> promise1, Promise<T2> promise2, Promise<T3> promise3, Promise<T4> promise4, Promise<T5> promise5, Promise<T6> promise6, Promise<T7> promise7, Promise promise8)
        {
            var value = new ValueTuple<T1, T2, T3, T4, T5, T6, T7>();
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            uint pendingCount = 0;
            ulong completedProgress = 0;

            ValidateArgument(promise1, "promise1", 1);
            pendingCount += Internal.PrepareForMulti(promise1, ref value.Item1, ref passThroughs, 0, ref completedProgress);
            ValidateArgument(promise2, "promise2", 1);
            pendingCount += Internal.PrepareForMulti(promise2, ref value.Item2, ref passThroughs, 1, ref completedProgress);
            ValidateArgument(promise3, "promise3", 1);
            pendingCount += Internal.PrepareForMulti(promise3, ref value.Item3, ref passThroughs, 2, ref completedProgress);
            ValidateArgument(promise4, "promise4", 1);
            pendingCount += Internal.PrepareForMulti(promise4, ref value.Item4, ref passThroughs, 3, ref completedProgress);
            ValidateArgument(promise5, "promise5", 1);
            pendingCount += Internal.PrepareForMulti(promise5, ref value.Item5, ref passThroughs, 4, ref completedProgress);
            ValidateArgument(promise6, "promise6", 1);
            pendingCount += Internal.PrepareForMulti(promise6, ref value.Item6, ref passThroughs, 5, ref completedProgress);
            ValidateArgument(promise7, "promise7", 1);
            pendingCount += Internal.PrepareForMulti(promise7, ref value.Item7, ref passThroughs, 6, ref completedProgress);
            ValidateArgument(promise8, "promise8", 1);
            pendingCount += Internal.PrepareForMulti(promise8, ref passThroughs, 7, ref completedProgress);

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref value, (feed, target, index) =>
            {
                var container = (Internal.ResolveContainer<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>) target;
                switch (index)
                {
                    case 0:
                        container.value.Item1 = ((Internal.ResolveContainer<T1>) feed).value;
                        break;
                    case 1:
                        container.value.Item2 = ((Internal.ResolveContainer<T2>) feed).value;
                        break;
                    case 2:
                        container.value.Item3 = ((Internal.ResolveContainer<T3>) feed).value;
                        break;
                    case 3:
                        container.value.Item4 = ((Internal.ResolveContainer<T4>) feed).value;
                        break;
                    case 4:
                        container.value.Item5 = ((Internal.ResolveContainer<T5>) feed).value;
                        break;
                    case 5:
                        container.value.Item6 = ((Internal.ResolveContainer<T6>) feed).value;
                        break;
                    case 6:
                        container.value.Item7 = ((Internal.ResolveContainer<T7>) feed).value;
                        break;
                }
            }, pendingCount, 8, completedProgress);
            return new Promise<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/>. <paramref name="resolver"/> is invoked immediately with a <see cref="Deferred"/> that controls the state of the new <see cref="Promise"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>.
        /// </summary>
		public static Promise New(Action<Deferred> resolver)
        {
            Deferred deferred = Deferred.New();
            Internal.CreateResolved()
                .Finally(ValueTuple.Create(deferred, resolver), cv =>
                {
                    Deferred def = cv.Item1;
                    try
                    {
                        cv.Item2.Invoke(def);
                    }
                    catch (OperationCanceledException e)
                    {
                        def.TryCancel(e); // Don't rethrow cancelation.
                    }
                    catch (Exception e)
                    {
                        if (!def.TryReject(e)) throw;
                    }
                });
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked immediately with a <see cref="Promise{T}.Deferred"/> that controls the state of the new <see cref="Promise{T}"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Promise{T}.Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>.
        /// </summary>
		public static Promise<T> New<T>(Action<Promise<T>.Deferred> resolver)
        {
            return Promise<T>.New(resolver);
        }

        /// <summary>
        /// Returns a new <see cref="Promise"/>. <paramref name="resolver"/> is invoked immediately with <paramref name="captureValue"/> and a <see cref="Deferred"/> that controls the state of the <see cref="Promise"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>.
        /// </summary>
        public static Promise New<TCapture>(TCapture captureValue, Action<TCapture, Deferred> resolver)
        {
            Deferred deferred = Deferred.New();
            Internal.CreateResolved()
                .Finally(ValueTuple.Create(deferred, resolver, captureValue), cv =>
                {
                    Deferred def = cv.Item1;
                    try
                    {
                        cv.Item2.Invoke(cv.Item3, def);
                    }
                    catch (OperationCanceledException e)
                    {
                        def.TryCancel(e); // Don't rethrow cancelation.
                    }
                    catch (Exception e)
                    {
                        if (!def.TryReject(e)) throw;
                    }
                })
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked immediately with <paramref name="captureValue"/> and a <see cref="Promise{T}.Deferred"/> that controls the state of the new <see cref="Promise{T}"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Promise{T}.Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>.
        /// </summary>
        public static Promise<T> New<TCapture, T>(TCapture captureValue, Action<TCapture, Promise<T>.Deferred> resolver)
        {
            return Promise<T>.New(captureValue, resolver);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already resolved.
        /// </summary>
		public static Promise Resolved()
        {
            return Internal.CreateResolved();
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already resolved with <paramref name="value"/>.
        /// </summary>
		public static Promise<T> Resolved<T>(T value)
        {
            return Internal.CreateResolved(ref value);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already rejected with <paramref name="reason"/>.
        /// </summary>
        public static Promise Rejected<TReject>(TReject reason)
        {
            var deferred = NewDeferred();
            deferred.Reject(reason);
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already rejected with <paramref name="reason"/>.
        /// </summary>
        public static Promise<T> Rejected<T, TReject>(TReject reason)
        {
            return Promise<T>.Rejected(reason);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already canceled without a reason.
        /// </summary>
        public static Promise Canceled()
        {
            var deferred = NewDeferred();
            deferred.Cancel();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that is already canceled with <paramref name="reason"/>.
        /// </summary>
        public static Promise Canceled<TCancel>(TCancel reason)
        {
            var deferred = NewDeferred();
            deferred.Cancel(reason);
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already canceled without a reason.
        /// </summary>
        public static Promise<T> Canceled<T>()
        {
            return Promise<T>.Canceled();
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already canceled with <paramref name="reason"/>.
        /// </summary>
        public static Promise<T> Canceled<T, TCancel>(TCancel reason)
        {
            return Promise<T>.Canceled(reason);
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
        public static RethrowException Rethrow
        {
            get
            {
                return RethrowException.GetOrCreate();
            }
        }

        /// <summary>
        /// Get a <see cref="CancelException"/> that can be thrown to cancel the promise without a reason from an onResolved or onRejected callback, or in an async Promise function.
        /// This should be used as "throw Promise.CancelException();"
        /// </summary>
        public static CanceledException CancelException()
        {
            return Internal.CanceledExceptionInternalVoid.GetOrCreate();
        }

        /// <summary>
        /// Get a <see cref="Promises.CancelException"/> that can be thrown to cancel the promise with the provided reason from an onResolved or onRejected callback, or in an async Promise function.
        /// This should be used as "throw Promise.CancelException(value);"
        /// </summary>
        public static CanceledException CancelException<T>(T value)
        {
            Type type = typeof(T).IsValueType ? typeof(T) : value.GetType();
            string message = "Operation was canceled with a reason, type: " + type + ", value: " + value.ToString();
            return new Internal.CanceledExceptionInternal<T>(value, message);
        }

        /// <summary>
        /// Get a <see cref="Promises.RejectException"/> that can be thrown to reject the promise from an onResolved or onRejected callback, or in an async Promise function.
        /// This should be used as "throw Promise.RejectException(value);"
        /// </summary>
        public static RejectException RejectException<T>(T value)
        {
            return new Internal.RejectExceptionInternal<T>(value);
        }
    }
}