﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CA1507 // Use nameof to express symbol names
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;

namespace Proto.Promises
{
    public partial struct Promise
    {
        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            return Race(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            return Race(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            return Race(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race(params Promise[] promises)
        {
            return Race(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race(IEnumerable<Promise> promises)
        {
            return Race(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, "promises", 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
                }
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                int pendingCount = 0;

                int index = -1; // Index isn't necessary for Race, but might help with debugging.
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref passThroughs, ++index))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Resolved();
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.RacePromise<Internal.VoidResult>.GetOrCreate(passThroughs, pendingCount);
                return new Promise(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(Promise promise1, Promise promise2)
        {

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            return RaceWithIndex(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            return RaceWithIndex(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            return RaceWithIndex(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(params Promise[] promises)
        {
            return RaceWithIndex(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(IEnumerable<Promise> promises)
        {
            return RaceWithIndex(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, "promises", 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to RaceWithIndex.", Internal.GetFormattedStacktrace(1));
                }
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                int pendingCount = 0;

                int index = -1;
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref passThroughs, ++index))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Resolved(index);
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.RacePromiseWithIndexVoid.GetOrCreate(passThroughs, pendingCount);
                return new Promise<int>(promise, promise.Id);
            }
        }

        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2)
        {
            return Promise<T>.Race(promise1, promise2);
        }

        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            return Promise<T>.Race(promise1, promise2, promise3);
        }

        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            return Promise<T>.Race(promise1, promise2, promise3, promise4);
        }

        public static Promise<T> Race<T>(params Promise<T>[] promises)
        {
            return Promise<T>.Race(promises);
        }

        public static Promise<T> Race<T>(IEnumerable<Promise<T>> promises)
        {
            return Promise<T>.Race(promises);
        }

        public static Promise<T> Race<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise<T>.Race(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            return First(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            return First(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            return First(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(params Promise[] promises)
        {
            return First(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(IEnumerable<Promise> promises)
        {
            return First(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, "promises", 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to First.", Internal.GetFormattedStacktrace(1));
                }
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                int pendingCount = 0;

                int index = -1; // Index isn't necessary for First, but might help with debugging.
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref passThroughs, ++index))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, true);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Resolved();
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.FirstPromise<Internal.VoidResult>.GetOrCreate(passThroughs, pendingCount);
                return new Promise(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            return FirstWithIndex(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            return FirstWithIndex(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            return FirstWithIndex(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(params Promise[] promises)
        {
            return FirstWithIndex(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(IEnumerable<Promise> promises)
        {
            return FirstWithIndex(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, "promises", 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to FirstWithIndex.", Internal.GetFormattedStacktrace(1));
                }
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                int pendingCount = 0;

                int index = -1;
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref passThroughs, ++index))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Resolved(index);
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.FirstPromiseWithIndexVoid.GetOrCreate(passThroughs, pendingCount);
                return new Promise<int>(promise, promise.Id);
            }
        }

        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2)
        {
            return Promise<T>.First(promise1, promise2);
        }

        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            return Promise<T>.First(promise1, promise2, promise3);
        }

        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            return Promise<T>.First(promise1, promise2, promise3, promise4);
        }

        public static Promise<T> First<T>(params Promise<T>[] promises)
        {
            return Promise<T>.First(promises);
        }

        public static Promise<T> First<T>(IEnumerable<Promise<T>> promises)
        {
            return Promise<T>.First(promises);
        }

        public static Promise<T> First<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise<T>.First(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            return RaceWithIndex<T, Internal.Enumerator<Promise<T>, Internal.TwoItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            return RaceWithIndex<T, Internal.Enumerator<Promise<T>, Internal.ThreeItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            return RaceWithIndex<T, Internal.Enumerator<Promise<T>, Internal.FourItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(params Promise<T>[] promises)
        {
            return RaceWithIndex<T, Internal.ArrayEnumerator<Promise<T>>>(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(IEnumerable<Promise<T>> promises)
        {
            return RaceWithIndex<T, IEnumerator<Promise<T>>>(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<ValueTuple<int, T>> RaceWithIndex<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, "promises", 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to RaceWithIndex.", Internal.GetFormattedStacktrace(1));
                }
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                T value = default(T);
                int pendingCount = 0;

                int index = -1;
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref value, ref passThroughs, ++index))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Resolved(new ValueTuple<int, T>(index, value));
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.RacePromiseWithIndex<T>.GetOrCreate(passThroughs, pendingCount);
                return new Promise<ValueTuple<int, T>>(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            return FirstWithIndex<T, Internal.Enumerator<Promise<T>, Internal.TwoItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            return FirstWithIndex<T, Internal.Enumerator<Promise<T>, Internal.ThreeItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            return FirstWithIndex<T, Internal.Enumerator<Promise<T>, Internal.FourItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(params Promise<T>[] promises)
        {
            return FirstWithIndex<T, Internal.ArrayEnumerator<Promise<T>>>(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(IEnumerable<Promise<T>> promises)
        {
            return FirstWithIndex<T, IEnumerator<Promise<T>>>(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<ValueTuple<int, T>> FirstWithIndex<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, "promises", 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to FirstWithIndex.", Internal.GetFormattedStacktrace(1));
                }
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                T value = default(T);
                int pendingCount = 0;

                int index = -1;
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref value, ref passThroughs, ++index))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Resolved(new ValueTuple<int, T>(index, value));
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.FirstPromiseWithIndex<T>.GetOrCreate(passThroughs, pendingCount);
                return new Promise<ValueTuple<int, T>>(promise, promise.Id);
            }
        }
    }
}