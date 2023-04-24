#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable CA1507 // Use nameof to express symbol names
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                T value = promise1._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                T value = promise2._result;
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
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                T value = promise1._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                T value = promise2._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                T value = promise3._result;
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
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                T value = promise1._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                T value = promise2._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, false);
                T value = promise3._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise4._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, false);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, false);
                T value = promise4._result;
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

            using (promises)
            {
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
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, false);
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
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, true);
                T value = promise1._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, true);
                T value = promise2._result;
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
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, true);
                T value = promise1._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, true);
                T value = promise2._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, true);
                T value = promise3._result;
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
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, true);
                T value = promise1._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise2._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, true);
                T value = promise2._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise3._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise4._ref, promise4._id, true);
                T value = promise3._result;
                return Internal.CreateResolved(value, depth);
            }
            if (promise4._ref == null)
            {
                Internal.MaybeMarkAwaitedAndDispose(promise1._ref, promise1._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise2._ref, promise2._id, true);
                Internal.MaybeMarkAwaitedAndDispose(promise3._ref, promise3._id, true);
                T value = promise4._result;
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

            using (promises)
            {
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
                            Internal.MaybeMarkAwaitedAndDispose(p._ref, p._id, true);
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
        }

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        // Old IL2CPP runtime immediately crashes if these methods exist, even if they are not used. So we only include them in newer build targets that old Unity versions cannot consume.
        // See https://github.com/timcassell/ProtoPromise/pull/106 for details.

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(Promise<T> promise1, Promise<T> promise2)
        {
            return Promise.RaceWithIndex(promise1, promise2);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            return Promise.RaceWithIndex(promise1, promise2, promise3);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            return Promise.RaceWithIndex(promise1, promise2, promise3, promise4);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(params Promise<T>[] promises)
        {
            return RaceWithIndex(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(IEnumerable<Promise<T>> promises)
        {
            return RaceWithIndex(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<ValueTuple<int, T>> RaceWithIndex<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise.RaceWithIndex<T, TEnumerator>(promises);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(Promise<T> promise1, Promise<T> promise2)
        {
            return Promise.FirstWithIndex(promise1, promise2);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            return Promise.FirstWithIndex(promise1, promise2, promise3);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            return Promise.FirstWithIndex(promise1, promise2, promise3, promise4);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(params Promise<T>[] promises)
        {
            return FirstWithIndex(promises.GetGenericEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(IEnumerable<Promise<T>> promises)
        {
            return FirstWithIndex(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<ValueTuple<int, T>> FirstWithIndex<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return Promise.FirstWithIndex<T, TEnumerator>(promises);
        }

#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, IList<T> valueContainer = null)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            T v0 = default(T);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            T v1 = default(T);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);

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
            var promise = Internal.PromiseRefBase.GetOrCreateAllPromise(passThroughs, valueContainer, pendingCount, completedProgress, maxDepth);
            return new Promise<IList<T>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, IList<T> valueContainer = null)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            T v0 = default(T);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            T v1 = default(T);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            T v2 = default(T);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);

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
            var promise = Internal.PromiseRefBase.GetOrCreateAllPromise(passThroughs, valueContainer, pendingCount, completedProgress, maxDepth);
            return new Promise<IList<T>>(promise, promise.Id, maxDepth);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="promise4">The fourth promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4, IList<T> valueContainer = null)
        {
            var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
            int pendingCount = 0;
            ulong completedProgress = 0;
            ushort maxDepth = 0;

            ValidateArgument(promise1, "promise1", 1);
            T v0 = default(T);
            Internal.PrepareForMerge(promise1, ref v0, ref passThroughs, 0, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise2, "promise2", 1);
            T v1 = default(T);
            Internal.PrepareForMerge(promise2, ref v1, ref passThroughs, 1, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise3, "promise3", 1);
            T v2 = default(T);
            Internal.PrepareForMerge(promise3, ref v2, ref passThroughs, 2, ref pendingCount, ref completedProgress, ref maxDepth);
            ValidateArgument(promise4, "promise4", 1);
            T v3 = default(T);
            Internal.PrepareForMerge(promise4, ref v3, ref passThroughs, 3, ref pendingCount, ref completedProgress, ref maxDepth);

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
            var promise = Internal.PromiseRefBase.GetOrCreateAllPromise(passThroughs, valueContainer, pendingCount, completedProgress, maxDepth);
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
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T>[] promises, IList<T> valueContainer = null)
        {
            return All(promises.GetGenericEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(IEnumerable<Promise<T>> promises, IList<T> valueContainer = null)
        {
            return All(promises.GetEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        /// <param name="promises">The enumerator of promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All<TEnumerator>(TEnumerator promises, IList<T> valueContainer = null) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, "promises", 1);

            using (promises)
            {
                var passThroughs = new Internal.ValueLinkedStack<Internal.PromiseRefBase.PromisePassThrough>();
                int pendingCount = 0;
                ulong completedProgress = 0;
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
                    Internal.PrepareForMerge(p, ref value, ref passThroughs, i, ref pendingCount, ref completedProgress, ref maxDepth);
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

                var promise = Internal.PromiseRefBase.GetOrCreateAllPromise(passThroughs, valueContainer, pendingCount, completedProgress, maxDepth);
                return new Promise<IList<T>>(promise, promise.Id, maxDepth);
            }
        }

        [Obsolete("Prefer Promise<T>.All()"), EditorBrowsable(EditorBrowsableState.Never)]
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
        /// <param name="resolver">The resolver delegate that will control the completion of the returned <see cref="Promise"/> via the passed in <see cref="Deferred"/>.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="resolver"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the <paramref name="resolver"/> to be invoked asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        public static Promise<T> New(Action<Deferred> resolver, SynchronizationOption synchronizationOption = SynchronizationOption.Synchronous, bool forceAsync = false)
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
            }, synchronizationOption, forceAsync)
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked with <paramref name="captureValue"/> and a <see cref="Deferred"/> that controls the state of the new <see cref="Promise{T}"/>.
        /// You may provide a <paramref name="synchronizationOption"/> to control the context on which the <paramref name="resolver"/> is invoked.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="resolver"/>.</param>
        /// <param name="resolver">The resolver delegate that will control the completion of the returned <see cref="Promise"/> via the passed in <see cref="Deferred"/>.</param>
        /// <param name="synchronizationOption">Indicates on which context the <paramref name="resolver"/> will be invoked.</param>
        /// <param name="forceAsync">If true, forces the <paramref name="resolver"/> to be invoked asynchronously. If <paramref name="synchronizationOption"/> is <see cref="SynchronizationOption.Synchronous"/>, this value will be ignored.</param>
        public static Promise<T> New<TCapture>(TCapture captureValue, Action<TCapture, Deferred> resolver, SynchronizationOption synchronizationOption = SynchronizationOption.Synchronous, bool forceAsync = false)
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
            }, synchronizationOption, forceAsync)
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked with a <see cref="Deferred"/> that controls the state of the new <see cref="Promise{T}"/> on the provided <paramref name="synchronizationContext"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>
        /// </summary>
        /// <param name="resolver">The resolver delegate that will control the completion of the returned <see cref="Promise"/> via the passed in <see cref="Deferred"/>.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="resolver"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the <paramref name="resolver"/> to be invoked asynchronously.</param>
		public static Promise<T> New(Action<Deferred> resolver, SynchronizationContext synchronizationContext, bool forceAsync = false)
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
            }, synchronizationContext, forceAsync)
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked with <paramref name="captureValue"/> and a <see cref="Deferred"/> that controls the state of the new <see cref="Promise{T}"/> on the provided <paramref name="synchronizationContext"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be canceled if it is an <see cref="OperationCanceledException"/>,
        /// or rejected with that <see cref="Exception"/>
        /// </summary>
        /// <param name="captureValue">The value that will be passed to <paramref name="resolver"/>.</param>
        /// <param name="resolver">The resolver delegate that will control the completion of the returned <see cref="Promise"/> via the passed in <see cref="Deferred"/>.</param>
        /// <param name="synchronizationContext">The context on which the <paramref name="resolver"/> will be invoked. If null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> will be used.</param>
        /// <param name="forceAsync">If true, forces the <paramref name="resolver"/> to be invoked asynchronously.</param>
        public static Promise<T> New<TCapture>(TCapture captureValue, Action<TCapture, Deferred> resolver, SynchronizationContext synchronizationContext, bool forceAsync = false)
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
            }, synchronizationContext, forceAsync)
                .Forget();
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already resolved with <paramref name="value"/>.
        /// </summary>
		public static Promise<T> Resolved(T value)
        {
#if PROMISE_DEBUG
            return Internal.CreateResolved(value, 0);
#else
            return new Promise<T>(value);
#endif
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
            return Internal.CreateCanceled<T>();
        }

        [Obsolete("Cancelation reasons are no longer supported. Use Cancel() instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static Promise<T> Canceled<TCancel>(TCancel reason)
        {
            throw new InvalidOperationException("Cancelation reasons are no longer supported. Use Canceled() instead.", Internal.GetFormattedStacktrace(1));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}.Deferred"/> object that is linked to and controls the state of a new <see cref="Promise{T}"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Deferred NewDeferred()
        {
            return Deferred.New();
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}.Deferred"/> object that is linked to and controls the state of a new <see cref="Promise{T}"/>.
        /// <para/>If the <paramref name="cancelationToken"/> is canceled while the <see cref="Promise{T}.Deferred"/> is pending, it and the <see cref="Promise{T}"/> will be canceled with its reason.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        [Obsolete("You should instead register a callback on the CancelationToken to cancel the deferred directly.", false), EditorBrowsable(EditorBrowsableState.Never)]
        public static Deferred NewDeferred(CancelationToken cancelationToken)
        {
            return Deferred.New(cancelationToken);
        }
    }
}