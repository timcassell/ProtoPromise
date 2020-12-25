#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections.Generic;
using Proto.Utils;

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
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null)
            {
                T value = promise1._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise2._ref == null)
            {
                T value = promise2._result;
                return Internal.CreateResolved(ref value);
            }
            passThroughs.Push(Internal.CreatePassthrough(promise1, 0));
            passThroughs.Push(Internal.CreatePassthrough(promise2, 1));

            var promise = Internal.PromiseRef.RacePromise.GetOrCreate(passThroughs, 2);
            return new Promise<T>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null)
            {
                T value = promise1._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise2._ref == null)
            {
                T value = promise2._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise3._ref == null)
            {
                T value = promise3._result;
                return Internal.CreateResolved(ref value);
            }
            passThroughs.Push(Internal.CreatePassthrough(promise1, 0));
            passThroughs.Push(Internal.CreatePassthrough(promise2, 1));
            passThroughs.Push(Internal.CreatePassthrough(promise3, 2));

            var promise = Internal.PromiseRef.RacePromise.GetOrCreate(passThroughs, 3);
            return new Promise<T>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<T> Race(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null)
            {
                T value = promise1._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise2._ref == null)
            {
                T value = promise2._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise3._ref == null)
            {
                T value = promise3._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise4._ref == null)
            {
                T value = promise4._result;
                return Internal.CreateResolved(ref value);
            }
            passThroughs.Push(Internal.CreatePassthrough(promise1, 0));
            passThroughs.Push(Internal.CreatePassthrough(promise2, 1));
            passThroughs.Push(Internal.CreatePassthrough(promise3, 2));
            passThroughs.Push(Internal.CreatePassthrough(promise4, 3));

            var promise = Internal.PromiseRef.RacePromise.GetOrCreate(passThroughs, 4);
            return new Promise<T>(promise, promise.Id);
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
                throw new EmptyArgumentException("promises", "You must provide at least one element to First.", Internal.GetFormattedStacktrace(1));
            }
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            T value = default(T);
            bool alreadyResolved;
            int count = 0;
            int i = 0; // Index isn't necessary for First, but might help with debugging.

            do
            {
                var p = promises.Current;
                ValidateElement(p, "promises", 1);
                alreadyResolved = Internal.PrepareForMulti(p, ref value, ref passThroughs, i++) == 0;
                ++count;
            } while (!alreadyResolved && promises.MoveNext());

            if (alreadyResolved)
            {
                // Validate and release remaining elements.
                while (promises.MoveNext())
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    Internal.PromiseRef.MaybeRelease(p._ref);
                }
                // Repool any created passthroughs.
                foreach (var passthrough in passThroughs)
                {
                    passthrough.Release();
                }
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.RacePromise.GetOrCreate(passThroughs, count);
            return new Promise<T>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(Promise<T> promise1, Promise<T> promise2)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            if (promise1._ref == null)
            {
                T value = promise1._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise2._ref == null)
            {
                T value = promise2._result;
                return Internal.CreateResolved(ref value);
            }
            passThroughs.Push(Internal.CreatePassthrough(promise1, 0));
            passThroughs.Push(Internal.CreatePassthrough(promise2, 1));

            var promise = Internal.PromiseRef.FirstPromise.GetOrCreate(passThroughs, 2);
            return new Promise<T>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            if (promise1._ref == null)
            {
                T value = promise1._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise2._ref == null)
            {
                T value = promise2._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise3._ref == null)
            {
                T value = promise3._result;
                return Internal.CreateResolved(ref value);
            }
            passThroughs.Push(Internal.CreatePassthrough(promise1, 0));
            passThroughs.Push(Internal.CreatePassthrough(promise2, 1));
            passThroughs.Push(Internal.CreatePassthrough(promise3, 2));

            var promise = Internal.PromiseRef.FirstPromise.GetOrCreate(passThroughs, 3);
            return new Promise<T>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be rejected or canceled with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();

            ValidateArgument(promise1, "promise1", 1);
            ValidateArgument(promise2, "promise2", 1);
            ValidateArgument(promise3, "promise3", 1);
            ValidateArgument(promise4, "promise4", 1);
            if (promise1._ref == null)
            {
                T value = promise1._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise2._ref == null)
            {
                T value = promise2._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise3._ref == null)
            {
                T value = promise3._result;
                return Internal.CreateResolved(ref value);
            }
            if (promise4._ref == null)
            {
                T value = promise4._result;
                return Internal.CreateResolved(ref value);
            }
            passThroughs.Push(Internal.CreatePassthrough(promise1, 0));
            passThroughs.Push(Internal.CreatePassthrough(promise2, 1));
            passThroughs.Push(Internal.CreatePassthrough(promise3, 2));
            passThroughs.Push(Internal.CreatePassthrough(promise4, 3));

            var promise = Internal.PromiseRef.FirstPromise.GetOrCreate(passThroughs, 4);
            return new Promise<T>(promise, promise.Id);
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
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            T value = default(T);
            bool alreadyResolved;
            int count = 0;
            int i = 0; // Index isn't necessary for First, but might help with debugging.

            do
            {
                var p = promises.Current;
                ValidateElement(p, "promises", 1);
                alreadyResolved = Internal.PrepareForMulti(p, ref value, ref passThroughs, i++) == 0;
                ++count;
            } while (!alreadyResolved && promises.MoveNext());

            if (alreadyResolved)
            {
                // Validate and release remaining elements.
                while (promises.MoveNext())
                {
                    var p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    Internal.PromiseRef.MaybeMarkAndRelease(p._ref); // Mark promise as waited on to suppress rejection.
                }
                // Repool any created passthroughs.
                foreach (var passthrough in passThroughs)
                {
                    passthrough.Release();
                }
                return Internal.CreateResolved(ref value);
            }
            var promise = Internal.PromiseRef.FirstPromise.GetOrCreate(passThroughs, count);
            return new Promise<T>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2)
        {
            T[] values = new T[2];
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            int count = 0;

            ValidateArgument(promise1, "promise1", 1);
            count += Internal.PrepareForMulti(promise1, ref values[0], ref passThroughs, 0);
            ValidateArgument(promise2, "promise2", 1);
            count += Internal.PrepareForMulti(promise2, ref values[1], ref passThroughs, 1);

            IList<T> vals = values;
            if (count == 0)
            {
                return Internal.CreateResolved(ref vals);
            }

            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref vals, (feed, target, index) =>
            {
                ((Internal.ResolveContainer<IList<T>>) target).value[index] = ((Internal.ResolveContainer<T>) feed).value;
            }, count);
            return new Promise<IList<T>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            T[] values = new T[3];
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            int count = 0;

            ValidateArgument(promise1, "promise1", 1);
            count += Internal.PrepareForMulti(promise1, ref values[0], ref passThroughs, 0);
            ValidateArgument(promise2, "promise2", 1);
            count += Internal.PrepareForMulti(promise2, ref values[1], ref passThroughs, 1);
            ValidateArgument(promise3, "promise3", 1);
            count += Internal.PrepareForMulti(promise3, ref values[2], ref passThroughs, 2);

            IList<T> vals = values;
            if (count == 0)
            {
                return Internal.CreateResolved(ref vals);
            }

            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref vals, (feed, target, index) =>
            {
                ((Internal.ResolveContainer<IList<T>>) target).value[index] = ((Internal.ResolveContainer<T>) feed).value;
            }, count);
            return new Promise<IList<T>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when all promises have resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            T[] values = new T[4];
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            int count = 0;

            ValidateArgument(promise1, "promise1", 1);
            count += Internal.PrepareForMulti(promise1, ref values[0], ref passThroughs, 0);
            ValidateArgument(promise2, "promise2", 1);
            count += Internal.PrepareForMulti(promise2, ref values[1], ref passThroughs, 1);
            ValidateArgument(promise3, "promise3", 1);
            count += Internal.PrepareForMulti(promise3, ref values[2], ref passThroughs, 2);
            ValidateArgument(promise4, "promise4", 1);
            count += Internal.PrepareForMulti(promise4, ref values[3], ref passThroughs, 3);

            IList<T> vals = values;
            if (count == 0)
            {
                return Internal.CreateResolved(ref vals);
            }

            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref vals, (feed, target, index) =>
            {
                ((Internal.ResolveContainer<IList<T>>) target).value[index] = ((Internal.ResolveContainer<T>) feed).value;
            }, count);
            return new Promise<IList<T>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All(params Promise<T>[] promises)
        {
            return AllNonAlloc(promises.GetGenericEnumerator(), new T[promises.Length]);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All(IEnumerable<Promise<T>> promises)
        {
            return AllNonAlloc(promises.GetEnumerator(), new List<T>());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> All<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            return AllNonAlloc(promises, new List<T>());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with <paramref name="valueContainer"/> in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be rejected or canceled with the same reason.
        /// </summary>
        public static Promise<IList<T>> AllNonAlloc<TEnumerator>(TEnumerator promises, IList<T> valueContainer) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, "promises", 1);
            ValidateArgument(valueContainer, "valueContainer", 1);
            var passThroughs = new ValueLinkedStack<Internal.PromiseRef.PromisePassThrough>();
            int promisesCount = 0;
            int pendingCount = 0;
            int i = 0;
            int listSize = valueContainer.Count;
            while (promises.MoveNext())
            {
                var p = promises.Current;
                ValidateElement(p, "promises", 1);
                T value = default(T);
                pendingCount += Internal.PrepareForMulti(p, ref value, ref passThroughs, i);
                // Make sure list has the same count as promises.
                if (listSize < ++promisesCount)
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
            while (listSize > promisesCount)
            {
                valueContainer.RemoveAt(--listSize);
            }
            if (pendingCount == 0)
            {
                return Internal.CreateResolved(ref valueContainer);
            }

            var promise = Internal.PromiseRef.MergePromise.GetOrCreate(passThroughs, ref valueContainer, (feed, target, index) =>
            {
                ((Internal.ResolveContainer<IList<T>>) target).value[index] = ((Internal.ResolveContainer<T>) feed).value;
            }, pendingCount);
            return new Promise<IList<T>>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked with a <see cref="Deferred"/> that controls the state of the new <see cref="Promise{T}"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// </summary>
		public static Promise<T> New(Action<Deferred> resolver)
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
                    catch (Exception e)
                    {
                        if (!def.TryReject(e)) throw;
                    }
                });
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a new <see cref="Promise{T}"/>. <paramref name="resolver"/> is invoked immediately with <paramref name="captureValue"/> and a <see cref="Deferred"/> that controls the state of the new <see cref="Promise{T}"/>.
        /// <para/>If <paramref name="resolver"/> throws an <see cref="Exception"/> and the <see cref="Deferred"/> is still pending, the new <see cref="Promise{T}"/> will be rejected with that <see cref="Exception"/>.
        /// </summary>
        public static Promise<T> New<TCapture>(TCapture captureValue, Action<TCapture, Deferred> resolver)
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
                    catch (Exception e)
                    {
                        if (!def.TryReject(e)) throw;
                    }
                });
            return deferred.Promise;
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already resolved with <paramref name="value"/>.
        /// </summary>
		public static Promise<T> Resolved(T value)
        {
            return Internal.CreateResolved(ref value);
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
        /// Returns a <see cref="Promise{T}"/> that is already canceled without a reason.
        /// </summary>
        public static Promise<T> Canceled()
        {
            var promise = Internal.PromiseRef.DeferredPromise<T>.GetOrCreate();
            promise.CancelDirect();
            return new Promise<T>(promise, promise.Id);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already canceled with <paramref name="reason"/>.
        /// </summary>
        public static Promise<T> Canceled<TCancel>(TCancel reason)
        {
            var promise = Internal.PromiseRef.DeferredPromise<T>.GetOrCreate();
            promise.CancelDirect(ref reason);
            return new Promise<T>(promise, promise.Id);
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