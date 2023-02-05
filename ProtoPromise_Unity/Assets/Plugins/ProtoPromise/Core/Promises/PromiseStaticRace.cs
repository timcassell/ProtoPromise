#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
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
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, promise2.Depth);

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null | promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                return Internal.CreateResolved(depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));

            var promise = Internal.PromiseRefBase.RacePromise<Internal.VoidResult>.GetOrCreate(passThroughs, 2, depth);
            return new Promise(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2, Promise promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, promise3.Depth));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                return Internal.CreateResolved(depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));

            var promise = Internal.PromiseRefBase.RacePromise<Internal.VoidResult>.GetOrCreate(passThroughs, 3, depth);
            return new Promise(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, Math.Min(promise3.Depth, promise4.Depth)));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null | promise4._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                return Internal.CreateResolved(depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise4, 3));

            var promise = Internal.PromiseRefBase.RacePromise<Internal.VoidResult>.GetOrCreate(passThroughs, 4, depth);
            return new Promise(promise, promise.Id, depth);
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

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
                }
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                int pendingCount = 0;
                ushort minDepth = ushort.MaxValue;

                int index = -1; // Index isn't necessary for Race, but might help with debugging.
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref passThroughs, ++index, ref minDepth))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
                            minDepth = Math.Min(minDepth, p.Depth);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Internal.CreateResolved(minDepth);
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.RacePromise<Internal.VoidResult>.GetOrCreate(passThroughs, pendingCount, minDepth);
                return new Promise(promise, promise.Id, minDepth);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(Promise promise1, Promise promise2)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, promise2.Depth);

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null | promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                int resolveIndex = promise1._ref == null ? 0 : 1;
                return Internal.CreateResolved(resolveIndex, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));

            var promise = Internal.PromiseRefBase.RacePromiseWithIndexVoid.GetOrCreate(passThroughs, 2, depth);
            return new Promise<int>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(Promise promise1, Promise promise2, Promise promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, promise3.Depth));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                int resolveIndex = promise1._ref == null ? 0
                    : promise2._ref == null ? 1
                    : 2;
                return Internal.CreateResolved(resolveIndex, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));

            var promise = Internal.PromiseRefBase.RacePromiseWithIndexVoid.GetOrCreate(passThroughs, 3, depth);
            return new Promise<int>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, Math.Min(promise3.Depth, promise4.Depth)));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null | promise4._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                int resolveIndex = promise1._ref == null ? 0
                    : promise2._ref == null ? 1
                    : promise3._ref == null ? 2
                    : 3;
                return Internal.CreateResolved(resolveIndex, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise4, 3));

            var promise = Internal.PromiseRefBase.RacePromiseWithIndexVoid.GetOrCreate(passThroughs, 4, depth);
            return new Promise<int>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(params Promise[] promises)
        {
            return RaceWithIndex(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(IEnumerable<Promise> promises)
        {
            return RaceWithIndex(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
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
                ushort minDepth = ushort.MaxValue;

                int index = -1;
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref passThroughs, ++index, ref minDepth))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
                            minDepth = Math.Min(minDepth, p.Depth);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Internal.CreateResolved(index, minDepth);
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.RacePromiseWithIndexVoid.GetOrCreate(passThroughs, pendingCount, minDepth);
                return new Promise<int>(promise, promise.Id, minDepth);
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
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, promise2.Depth);

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null | promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, true);
                return Internal.CreateResolved(depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));

            var promise = Internal.PromiseRefBase.FirstPromise<Internal.VoidResult>.GetOrCreate(passThroughs, 2, depth);
            return new Promise(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2, Promise promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, promise3.Depth));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, true);
                return Internal.CreateResolved(depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));

            var promise = Internal.PromiseRefBase.FirstPromise<Internal.VoidResult>.GetOrCreate(passThroughs, 3, depth);
            return new Promise(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, Math.Min(promise3.Depth, promise4.Depth)));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null | promise4._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, true);
                return Internal.CreateResolved(depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise4, 3));

            var promise = Internal.PromiseRefBase.FirstPromise<Internal.VoidResult>.GetOrCreate(passThroughs, 4, depth);
            return new Promise(promise, promise.Id, depth);
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

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to First.", Internal.GetFormattedStacktrace(1));
                }
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                int pendingCount = 0;
                ushort minDepth = ushort.MaxValue;

                int index = -1; // Index isn't necessary for First, but might help with debugging.
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref passThroughs, ++index, ref minDepth))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, true);
                            minDepth = Math.Min(minDepth, p.Depth);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Internal.CreateResolved(minDepth);
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.FirstPromise<Internal.VoidResult>.GetOrCreate(passThroughs, pendingCount, minDepth);
                return new Promise(promise, promise.Id, minDepth);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(Promise promise1, Promise promise2)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, promise2.Depth);

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null | promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                int resolveIndex = promise1._ref == null ? 0 : 1;
                return Internal.CreateResolved(resolveIndex, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));

            var promise = Internal.PromiseRefBase.FirstPromiseWithIndexVoid.GetOrCreate(passThroughs, 2, depth);
            return new Promise<int>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(Promise promise1, Promise promise2, Promise promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, promise3.Depth));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                int resolveIndex = promise1._ref == null ? 0
                    : promise2._ref == null ? 1
                    : 2;
                return Internal.CreateResolved(resolveIndex, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));

            var promise = Internal.PromiseRefBase.FirstPromiseWithIndexVoid.GetOrCreate(passThroughs, 3, depth);
            return new Promise<int>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, Math.Min(promise3.Depth, promise4.Depth)));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null | promise2._ref == null | promise3._ref == null | promise4._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                int resolveIndex = promise1._ref == null ? 0
                    : promise2._ref == null ? 1
                    : promise3._ref == null ? 2
                    : 3;
                return Internal.CreateResolved(resolveIndex, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise4, 3));

            var promise = Internal.PromiseRefBase.FirstPromiseWithIndexVoid.GetOrCreate(passThroughs, 4, depth);
            return new Promise<int>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(params Promise[] promises)
        {
            return FirstWithIndex(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(IEnumerable<Promise> promises)
        {
            return FirstWithIndex(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
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
                ushort minDepth = ushort.MaxValue;

                int index = -1;
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref passThroughs, ++index, ref minDepth))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
                            minDepth = Math.Min(minDepth, p.Depth);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Internal.CreateResolved(index, minDepth);
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.FirstPromiseWithIndexVoid.GetOrCreate(passThroughs, pendingCount, minDepth);
                return new Promise<int>(promise, promise.Id, minDepth);
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
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2)
#else
        public static Promise<ValueTuple<int, T>> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2)
#endif
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, promise2.Depth);

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                var result = new ValueTuple<int, T>(0, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                var result = new ValueTuple<int, T>(1, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));

            var promise = Internal.PromiseRefBase.RacePromiseWithIndex<T>.GetOrCreate(passThroughs, 2, depth);
            return new Promise<ValueTuple<int, T>>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
#else
        public static Promise<ValueTuple<int, T>> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
#endif
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, promise3.Depth));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                var result = new ValueTuple<int, T>(0, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                var result = new ValueTuple<int, T>(1, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                var result = new ValueTuple<int, T>(2, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));

            var promise = Internal.PromiseRefBase.RacePromiseWithIndex<T>.GetOrCreate(passThroughs, 3, depth);
            return new Promise<ValueTuple<int, T>>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
#else
        public static Promise<ValueTuple<int, T>> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
#endif
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, Math.Min(promise3.Depth, promise4.Depth)));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                var result = new ValueTuple<int, T>(0, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                var result = new ValueTuple<int, T>(1, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                var result = new ValueTuple<int, T>(2, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise4._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                var result = new ValueTuple<int, T>(3, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise4, 3));

            var promise = Internal.PromiseRefBase.RacePromiseWithIndex<T>.GetOrCreate(passThroughs, 4, depth);
            return new Promise<ValueTuple<int, T>>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(params Promise<T>[] promises)
#else
        public static Promise<ValueTuple<int, T>> RaceWithIndex<T>(params Promise<T>[] promises)
#endif
        {
            return RaceWithIndex<T, ArrayExtensions.Enumerator<Promise<T>>>(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(IEnumerable<Promise<T>> promises)
#else
        public static Promise<ValueTuple<int, T>> RaceWithIndex<T>(IEnumerable<Promise<T>> promises)
#endif
        {
            return RaceWithIndex<T, IEnumerator<Promise<T>>>(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
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
                ushort minDepth = ushort.MaxValue;

                int index = -1;
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref value, ref passThroughs, ++index, ref minDepth))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
                            minDepth = Math.Min(minDepth, p.Depth);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Internal.CreateResolved(new ValueTuple<int, T>(index, value), minDepth);
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.RacePromiseWithIndex<T>.GetOrCreate(passThroughs, pendingCount, minDepth);
                return new Promise<ValueTuple<int, T>>(promise, promise.Id, minDepth);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2)
#else
        public static Promise<ValueTuple<int, T>> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2)
#endif
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, promise2.Depth);

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                var result = new ValueTuple<int, T>(0, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                var result = new ValueTuple<int, T>(1, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));

            var promise = Internal.PromiseRefBase.FirstPromiseWithIndex<T>.GetOrCreate(passThroughs, 2, depth);
            return new Promise<ValueTuple<int, T>>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
#else
        public static Promise<ValueTuple<int, T>> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
#endif
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, promise3.Depth));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                var result = new ValueTuple<int, T>(0, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                var result = new ValueTuple<int, T>(1, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                var result = new ValueTuple<int, T>(2, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));

            var promise = Internal.PromiseRefBase.FirstPromiseWithIndex<T>.GetOrCreate(passThroughs, 3, depth);
            return new Promise<ValueTuple<int, T>>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
#else
        public static Promise<ValueTuple<int, T>> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
#endif
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, Math.Min(promise3.Depth, promise4.Depth)));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                var result = new ValueTuple<int, T>(0, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                var result = new ValueTuple<int, T>(1, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                var result = new ValueTuple<int, T>(2, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            if (promise4._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                var result = new ValueTuple<int, T>(3, promise1._result);
                return Internal.CreateResolved(result, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise4, 3));

            var promise = Internal.PromiseRefBase.FirstPromiseWithIndex<T>.GetOrCreate(passThroughs, 4, depth);
            return new Promise<ValueTuple<int, T>>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(params Promise<T>[] promises)
#else
        public static Promise<ValueTuple<int, T>> FirstWithIndex<T>(params Promise<T>[] promises)
#endif
        {
            return FirstWithIndex<T, ArrayExtensions.Enumerator<Promise<T>>>(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
#if CSHARP_7_3_OR_NEWER
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(IEnumerable<Promise<T>> promises)
#else
        public static Promise<ValueTuple<int, T>> FirstWithIndex<T>(IEnumerable<Promise<T>> promises)
#endif
        {
            return FirstWithIndex<T, IEnumerator<Promise<T>>>(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
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
                ushort minDepth = ushort.MaxValue;

                int index = -1;
                do
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (!Internal.TryPrepareForRace(p, ref value, ref passThroughs, ++index, ref minDepth))
                    {
                        // Validate and release remaining elements.
                        while (promises.MoveNext())
                        {
                            p = promises.Current;
                            ValidateElement(p, "promises", 1);
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
                            minDepth = Math.Min(minDepth, p.Depth);
                        }
                        // Repool any created passthroughs.
                        foreach (var passthrough in passThroughs)
                        {
                            passthrough.Dispose();
                        }
                        return Internal.CreateResolved(new ValueTuple<int, T>(index, value), minDepth);
                    }
                    ++pendingCount;
                } while (promises.MoveNext());

                var promise = Internal.PromiseRefBase.FirstPromiseWithIndex<T>.GetOrCreate(passThroughs, pendingCount, minDepth);
                return new Promise<ValueTuple<int, T>>(promise, promise.Id, minDepth);
            }
        }
    }
}