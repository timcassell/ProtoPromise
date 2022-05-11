#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections.Generic;
using System.Threading;

namespace Proto.Promises
{
    public partial struct Promise<T>
    {
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race(Promise<T> promise1, Promise<T> promise2)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, promise2.Depth);

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, false);
                T value = promise1.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, false);
                T value = promise2.Result;
                return Internal.CreateResolved(value, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));

            var promise = Internal.PromiseRefBase.RacePromise<T>.GetOrCreate(passThroughs, 2, depth);
            return new Promise<T>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, promise3.Depth));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3.Id, false);
                T value = promise1.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3.Id, false);
                T value = promise2.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, false);
                T value = promise3.Result;
                return Internal.CreateResolved(value, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));

            var promise = Internal.PromiseRefBase.RacePromise<T>.GetOrCreate(passThroughs, 3, depth);
            return new Promise<T>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, Math.Min(promise3.Depth, promise4.Depth)));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4.Id, false);
                T value = promise1.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4.Id, false);
                T value = promise2.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4.Id, false);
                T value = promise3.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise4._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3.Id, false);
                T value = promise4.Result;
                return Internal.CreateResolved(value, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise4, 3));

            var promise = Internal.PromiseRefBase.RacePromise<T>.GetOrCreate(passThroughs, 4, depth);
            return new Promise<T>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race(params Promise<T>[] promises)
        {
            return Race(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race(IEnumerable<Promise<T>> promises)
        {
            return Race(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, "promises", 1);
            if (!promises.MoveNext())
            {
                throw new EmptyArgumentException("promises", "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
            }
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            T value = default(T);
            int pendingCount = 0;
            ushort minDepth = ushort.MaxValue;

            int index = -1; // Index isn't necessary for Race, but might help with debugging.
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
                        Internal.MaybeMarkAwaitedAndDispose(p._ref, p.Id, false);
                        minDepth = Math.Min(minDepth, p.Depth);
                    }
                    // Repool any created passthroughs.
                    foreach (var passthrough in passThroughs)
                    {
                        passthrough.Dispose();
                    }
                    return Internal.CreateResolved(value, minDepth);
                }
                ++pendingCount;
            } while (promises.MoveNext());

            var promise = Internal.PromiseRefBase.RacePromise<T>.GetOrCreate(passThroughs, pendingCount, minDepth);
            return new Promise<T>(promise, promise.Id, minDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(Promise<T> promise1, Promise<T> promise2)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, promise2.Depth);

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, true);
                T value = promise1.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, true);
                T value = promise2.Result;
                return Internal.CreateResolved(value, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));

            var promise = Internal.PromiseRefBase.FirstPromise<T>.GetOrCreate(passThroughs, 2, depth);
            return new Promise<T>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, promise3.Depth));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3.Id, true);
                T value = promise1.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3.Id, true);
                T value = promise2.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, true);
                T value = promise3.Result;
                return Internal.CreateResolved(value, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));

            var promise = Internal.PromiseRefBase.FirstPromise<T>.GetOrCreate(passThroughs, 3, depth);
            return new Promise<T>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();

            ushort depth = Math.Min(promise1.Depth, Math.Min(promise2.Depth, Math.Min(promise3.Depth, promise4.Depth)));

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4.Id, true);
                T value = promise1.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4.Id, true);
                T value = promise2.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4.Id, true);
                T value = promise3.Result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise4._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2.Id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3.Id, true);
                T value = promise4.Result;
                return Internal.CreateResolved(value, depth);
            }
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise1, 0));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise2, 1));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise3, 2));
            passThroughs.Push(Internal.PromiseRefBase.PromisePassThrough.GetOrCreate(promise4, 3));

            var promise = Internal.PromiseRefBase.FirstPromise<T>.GetOrCreate(passThroughs, 4, depth);
            return new Promise<T>(promise, promise.Id, depth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(params Promise<T>[] promises)
        {
            return First(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(IEnumerable<Promise<T>> promises)
        {
            return First(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, "promises", 1);
            if (!promises.MoveNext())
            {
                throw new EmptyArgumentException("promises", "You must provide at least one element to First.", Internal.GetFormattedStacktrace(1));
            }
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            T value = default(T);
            int pendingCount = 0;
            ushort minDepth = ushort.MaxValue;

            int index = -1; // Index isn't necessary for First, but might help with debugging.
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
                        Internal.MaybeMarkAwaitedAndDispose(p._ref, p.Id, true);
                        minDepth = Math.Min(minDepth, p.Depth);
                    }
                    // Repool any created passthroughs.
                    foreach (var passthrough in passThroughs)
                    {
                        passthrough.Dispose();
                    }
                    return Internal.CreateResolved(value, minDepth);
                }
                ++pendingCount;
            } while (promises.MoveNext());

            var promise = Internal.PromiseRefBase.FirstPromise<T>.GetOrCreate(passThroughs, pendingCount, minDepth);
            return new Promise<T>(promise, promise.Id, minDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, IList<T> valueContainer = null)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ulong totalProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            T v0 = default(T);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref totalProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            T v1 = default(T);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref totalProgress, ref maxDepth);

            if (valueContainer == null)
            {
                valueContainer = new T[2] { v0, v1 };
            }
            else
            {
                // Make sure list has the same count as promises.
                int listSize = valueContainer.Count;
                while (listSize > 2)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                while (listSize < 2)
                {
                    valueContainer.Add(default(T));
                    ++listSize;
                }
                valueContainer[0] = v0;
                valueContainer[1] = v1;
            }

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(valueContainer, maxDepth);
            }
            var promise = Internal.PromiseRefBase.MergePromise<IList<T>>.GetOrCreate(passThroughs, valueContainer, (Internal.PromiseRefBase feed, ref IList<T> target, int index) =>
            {
                target[index] = feed.GetResult<T>();
            }, pendingCount, completedProgress, totalProgress, maxDepth);
            return new Promise<IList<T>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, IList<T> valueContainer = null)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ulong totalProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            T v0 = default(T);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref totalProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            T v1 = default(T);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref totalProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            T v2 = default(T);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref totalProgress, ref maxDepth);

            if (valueContainer == null)
            {
                valueContainer = new T[3] { v0, v1, v2 };
            }
            else
            {
                // Make sure list has the same count as promises.
                int listSize = valueContainer.Count;
                while (listSize > 3)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                while (listSize < 3)
                {
                    valueContainer.Add(default(T));
                    ++listSize;
                }
                valueContainer[0] = v0;
                valueContainer[1] = v1;
                valueContainer[2] = v2;
            }

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(valueContainer, maxDepth);
            }
            var promise = Internal.PromiseRefBase.MergePromise<IList<T>>.GetOrCreate(passThroughs, valueContainer, (Internal.PromiseRefBase feed, ref IList<T> target, int index) =>
            {
                target[index] = feed.GetResult<T>();
            }, pendingCount, completedProgress, totalProgress, maxDepth);
            return new Promise<IList<T>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4, IList<T> valueContainer = null)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ulong totalProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            T v0 = default(T);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref totalProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            T v1 = default(T);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref totalProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            T v2 = default(T);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref totalProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            T v3 = default(T);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref totalProgress, ref maxDepth);

            if (valueContainer == null)
            {
                valueContainer = new T[4] { v0, v1, v2, v3 };
            }
            else
            {
                // Make sure list has the same count as promises.
                int listSize = valueContainer.Count;
                while (listSize > 4)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                while (listSize < 4)
                {
                    valueContainer.Add(default(T));
                    ++listSize;
                }
                valueContainer[0] = v0;
                valueContainer[1] = v1;
                valueContainer[2] = v2;
                valueContainer[3] = v3;
            }

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(valueContainer, maxDepth);
            }
            var promise = Internal.PromiseRefBase.MergePromise<IList<T>>.GetOrCreate(passThroughs, valueContainer, (Internal.PromiseRefBase feed, ref IList<T> target, int index) =>
            {
                target[index] = feed.GetResult<T>();
            }, pendingCount, completedProgress, totalProgress, maxDepth);
            return new Promise<IList<T>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All(params Promise<T>[] promises)
        {
            return All(promises, new T[promises.Length]);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T>[] promises, IList<T> valueContainer = null)
        {
            return All(promises.GetGenericEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(IEnumerable<Promise<T>> promises, IList<T> valueContainer = null)
        {
            return All(promises.GetEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All<TEnumerator>(TEnumerator promises, IList<T> valueContainer = null) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, "promises", 1);
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ulong totalProgress = 0;
            ushort maxDepth = 0;

            if (valueContainer == null)
            {
                valueContainer = new List<T>();
            }

            int i = 0;
            int listSize = valueContainer.Count;
            while (promises.MoveNext())
            {
                var p = promises.Current;
                ValidateElement(p, "promises", 1);
                T value = default(T);
                Internal.PrepareForMerge(p, ref value, ref passThroughs, i, ref pendingCount, ref completedProgress, ref totalProgress, ref maxDepth);
                // Make sure list has the same count as promises.
                if (listSize < (i + 1))
                {
                    ++listSize;
                    valueContainer.Add(value);
                }
                else
                {
                    valueContainer[i] = value;
                }
                ++i;
            }
            // Make sure list has the same count as promises.
            while (listSize > i)
            {
                valueContainer.RemoveAt(--listSize);
            }

            if (pendingCount == 0)
            {
                return Internal.CreateResolved(valueContainer, maxDepth);
            }

            var promise = Internal.PromiseRefBase.MergePromise<IList<T>>.GetOrCreate(passThroughs, valueContainer, (Internal.PromiseRefBase feed, ref IList<T> target, int index) =>
            {
                target[index] = feed.GetResult<T>();
            }, pendingCount, completedProgress, totalProgress, maxDepth);
            return new Promise<IList<T>>(promise, promise.Id, maxDepth);
        }

        [Obsolete("Prefer Promise<T>.All()")]
        public static Promise<IList<T>> AllNonAlloc<TEnumerator>(TEnumerator promises, IList<T> valueContainer) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(valueContainer, "valueContainer", 1);
            return All(promises, valueContainer);
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked with a <see cref="Deferred"/> that controls the state of the new <see cref="Promise{T}"/>.
        /// You may provide a <paramref name="synchronizationOption"/> to control the context on which the <paramref name="resolver"/> is invoked.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>
        /// </summary>
        public static Promise<T> New(Action<Deferred> resolver, SynchronizationOption synchronizationOption = SynchronizationOption.Synchronous)
        {
            ValidateArgument(resolver, "resolver", 1);

            Deferred deferred = Deferred.New();
            Promise.Run(ValueTuple.Create(deferred, resolver), cv =>
            {
                Deferred def = cv.Item1;
                try
                {
                    cv.Item2.Invoke(def);
                }
                catch (OperationCanceledException)
                {
                    def.TryCancel(); // Don't rethrow cancelation.
                }
                catch (Exception e)
                {
                    if (!def.TryReject(e)) throw;
                }
            }, synchronizationOption)
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked with <paramref name="captureValue"/> and a <see cref="Deferred"/> that controls the state of the new <see cref="Promise{T}"/>.
        /// You may provide a <paramref name="synchronizationOption"/> to control the context on which the <paramref name="resolver"/> is invoked.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>
        /// </summary>
        public static Promise<T> New<TCapture>(TCapture captureValue, Action<TCapture, Deferred> resolver, SynchronizationOption synchronizationOption = SynchronizationOption.Synchronous)
        {
            ValidateArgument(resolver, "resolver", 1);

            Deferred deferred = Deferred.New();
            Promise.Run(ValueTuple.Create(deferred, resolver, captureValue), cv =>
            {
                Deferred def = cv.Item1;
                try
                {
                    cv.Item2.Invoke(cv.Item3, def);
                }
                catch (OperationCanceledException)
                {
                    def.TryCancel(); // Don't rethrow cancelation.
                }
                catch (Exception e)
                {
                    if (!def.TryReject(e)) throw;
                }
            }, synchronizationOption)
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked with a <see cref="Deferred"/> that controls the state of the new <see cref="Promise{T}"/> on the provided <paramref name="synchronizationContext"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>
        /// </summary>
		public static Promise<T> New(Action<Deferred> resolver, SynchronizationContext synchronizationContext)
        {
            ValidateArgument(resolver, "resolver", 1);

            Deferred deferred = Deferred.New();
            Promise.Run(ValueTuple.Create(deferred, resolver), cv =>
            {
                Deferred def = cv.Item1;
                try
                {
                    cv.Item2.Invoke(def);
                }
                catch (OperationCanceledException)
                {
                    def.TryCancel(); // Don't rethrow cancelation.
                }
                catch (Exception e)
                {
                    if (!def.TryReject(e)) throw;
                }
            }, synchronizationContext)
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked with <paramref name="captureValue"/> and a <see cref="Deferred"/> that controls the state of the new <see cref="Promise{T}"/> on the provided <paramref name="synchronizationContext"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>
        /// </summary>
        public static Promise<T> New<TCapture>(TCapture captureValue, Action<TCapture, Deferred> resolver, SynchronizationContext synchronizationContext)
        {
            ValidateArgument(resolver, "resolver", 1);

            Deferred deferred = Deferred.New();
            Promise.Run(ValueTuple.Create(deferred, resolver, captureValue), cv =>
            {
                Deferred def = cv.Item1;
                try
                {
                    cv.Item2.Invoke(cv.Item3, def);
                }
                catch (OperationCanceledException)
                {
                    def.TryCancel(); // Don't rethrow cancelation.
                }
                catch (Exception e)
                {
                    if (!def.TryReject(e)) throw;
                }
            }, synchronizationContext)
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already resolved with <paramref name="value"/>.
        /// </summary>
		public static Promise<T> Resolved(T value)
        {
            return Internal.CreateResolved(value, 0);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already rejected with <paramref name="reason"/>.
        /// </summary>
        public static Promise<T> Rejected<TReject>(TReject reason)
        {
            var deferred = NewDeferred();
            deferred.Reject(reason);
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already canceled.
        /// </summary>
        public static Promise<T> Canceled()
        {
            var deferred = NewDeferred();
            deferred.Cancel();
            return deferred.Promise;
        }

        [Obsolete("Cancelation reasons are no longer supported. Use Cancel() instead.", true)]
        public static Promise<T> Canceled<TCancel>(TCancel reason)
        {
            throw new InvalidOperationException("Cancelation reasons are no longer supported. Use Canceled() instead.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}.Deferred"/> object that is linked to and controls the state of a new <see cref="Promise{T}"/>.
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Promise{T}.Deferred"/> is pending, it and the <see cref="Promise{T}"/> will be canceled with its reason.
        /// </summary>
        public static Deferred NewDeferred(CancelationToken cancelationToken = default(CancelationToken))
        {
            return Deferred.New(cancelationToken);
        }
    }
}