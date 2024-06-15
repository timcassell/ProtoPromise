#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return Race(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return Race(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return Race(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race(params Promise[] promises)
            // TODO: validate argument before calling GetEnumerator. (Do the same for First, Merge, etc).
            => Race(promises.GetGenericEnumerator());

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race(ReadOnlySpan<Promise> promises)
            => Race(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race(IEnumerable<Promise> promises)
            // TODO: validate argument before calling GetEnumerator. (Do the same for First, Merge, etc).
            => Race(promises.GetEnumerator());

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise Race<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException(nameof(promises), "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
                }

                bool isResolved;
                Promise p;
                do
                {
                    p = promises.Current;
                    isResolved = p._ref == null;
                    if (!isResolved)
                    {
                        goto HookupMaybePending;
                    }
                } while (promises.MoveNext());
                // No non-resolved promises.
                return Resolved();

            HookupMaybePending:
                ValidateElement(p, "promises", 1);
                var promise = Internal.PromiseRefBase.RacePromise<Internal.VoidResult>.GetOrCreate();
                promise.AddWaiter(p._ref, p._id);
                while (promises.MoveNext())
                {
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    bool resolved = p._ref == null;
                    isResolved |= resolved;
                    if (!resolved)
                    {
                        promise.AddWaiter(p._ref, p._id);
                    }
                }
                if (isResolved)
                {
                    promise.Forget(promise.Id);
                    return Resolved();
                }
                return new Promise(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(Promise promise1, Promise promise2)
        {

            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return RaceWithIndex(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return RaceWithIndex(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return RaceWithIndex(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(params Promise[] promises)
            => RaceWithIndex(promises.GetGenericEnumerator());

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(ReadOnlySpan<Promise> promises)
            => RaceWithIndex(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex(IEnumerable<Promise> promises)
            => RaceWithIndex(promises.GetEnumerator());

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<int> RaceWithIndex<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException(nameof(promises), "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
                }

                int index = 0;
                int resolvedIndex = -1;
                Promise p;
                do
                {
                    p = promises.Current;
                    if (p._ref != null)
                    {
                        goto HookupMaybePending;
                    }
                    resolvedIndex = 0;
                    checked { ++index; }
                } while (promises.MoveNext());
                // No non-resolved promises.
                return Resolved(0);

            HookupMaybePending:
                ValidateElement(p, "promises", 1);
                var promise = Internal.PromiseRefBase.RacePromiseWithIndexVoid.GetOrCreate();
                promise.AddWaiterWithIndex(p._ref, p._id, index);
                while (promises.MoveNext())
                {
                    checked { ++index; }
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (p._ref != null)
                    {
                        promise.AddWaiterWithIndex(p._ref, p._id, index);
                    }
                    else if (resolvedIndex < 0)
                    {
                        resolvedIndex = index;
                    }
                }
                if (resolvedIndex >= 0)
                {
                    promise.Forget(promise.Id);
                    return Resolved(resolvedIndex);
                }
                return new Promise<int>(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2)
            => Promise<T>.Race(promise1, promise2);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
            => Promise<T>.Race(promise1, promise2, promise3);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
            => Promise<T>.Race(promise1, promise2, promise3, promise4);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(params Promise<T>[] promises)
            => Promise<T>.Race(promises);

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(ReadOnlySpan<Promise<T>> promises)
            => Promise<T>.Race(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race<T>(IEnumerable<Promise<T>> promises)
            => Promise<T>.Race(promises);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
            => Promise<T>.Race(promises);

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return First(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return First(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the promises has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return First(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(params Promise[] promises)
            => First(promises.GetGenericEnumerator());

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(ReadOnlySpan<Promise> promises)
            => First(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First(IEnumerable<Promise> promises)
            => First(promises.GetEnumerator());

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise First<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException(nameof(promises), "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
                }

                bool isResolved;
                Promise p;
                do
                {
                    p = promises.Current;
                    isResolved = p._ref == null;
                    if (!isResolved)
                    {
                        goto HookupMaybePending;
                    }
                } while (promises.MoveNext());
                // No non-resolved promises.
                return Resolved();

            HookupMaybePending:
                ValidateElement(p, "promises", 1);
                var promise = Internal.PromiseRefBase.FirstPromise<Internal.VoidResult>.GetOrCreate();
                uint pendingCount = 1;
                promise.AddWaiter(p._ref, p._id);
                while (promises.MoveNext())
                {
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    bool resolved = p._ref == null;
                    isResolved |= resolved;
                    if (!resolved)
                    {
                        checked { ++pendingCount; }
                        promise.AddWaiter(p._ref, p._id);
                    }
                }
                promise.MarkReady(pendingCount);
                if (isResolved)
                {
                    promise.SuppressRejection = true;
                    promise.Forget(promise.Id);
                    return Resolved();
                }
                return new Promise(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return FirstWithIndex(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return FirstWithIndex(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return FirstWithIndex(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(params Promise[] promises)
            => FirstWithIndex(promises.GetGenericEnumerator());

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(ReadOnlySpan<Promise> promises)
            => FirstWithIndex(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve when the first of the <paramref name="promises"/> has resolved.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex(IEnumerable<Promise> promises)
            => FirstWithIndex(promises.GetEnumerator());

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="int"/> that will resolve when the first of the promises has resolved with the index of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise"/> that is rejected or canceled.
        /// </summary>
        public static Promise<int> FirstWithIndex<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException(nameof(promises), "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
                }

                int index = 0;
                int resolvedIndex = -1;
                Promise p;
                do
                {
                    p = promises.Current;
                    if (p._ref != null)
                    {
                        goto HookupMaybePending;
                    }
                    resolvedIndex = 0;
                    checked { ++index; }
                } while (promises.MoveNext());
                // No non-resolved promises.
                return Resolved(0);

            HookupMaybePending:
                ValidateElement(p, "promises", 1);
                var promise = Internal.PromiseRefBase.FirstPromiseWithIndexVoid.GetOrCreate();
                uint pendingCount = 1;
                promise.AddWaiterWithIndex(p._ref, p._id, index);
                while (promises.MoveNext())
                {
                    checked { ++index; }
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (p._ref != null)
                    {
                        checked { ++pendingCount; }
                        promise.AddWaiterWithIndex(p._ref, p._id, index);
                    }
                    else if (resolvedIndex < 0)
                    {
                        resolvedIndex = index;
                    }
                }
                promise.MarkReady(pendingCount);
                if (resolvedIndex >= 0)
                {
                    promise.SuppressRejection = true;
                    promise.Forget(promise.Id);
                    return Resolved(resolvedIndex);
                }
                return new Promise<int>(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2)
            => Promise<T>.First(promise1, promise2);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
            => Promise<T>.First(promise1, promise2, promise3);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
            => Promise<T>.First(promise1, promise2, promise3, promise4);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(params Promise<T>[] promises)
            => Promise<T>.First(promises);

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(ReadOnlySpan<Promise<T>> promises)
            => Promise<T>.First(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T>(IEnumerable<Promise<T>> promises)
            => Promise<T>.First(promises);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
            => Promise<T>.First(promises);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return RaceWithIndex<T, Internal.Enumerator<Promise<T>, Internal.TwoItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return RaceWithIndex<T, Internal.Enumerator<Promise<T>, Internal.ThreeItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return RaceWithIndex<T, Internal.Enumerator<Promise<T>, Internal.FourItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(params Promise<T>[] promises)
            => RaceWithIndex<T, Internal.ArrayEnumerator<Promise<T>>>(promises.GetGenericEnumerator());

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(ReadOnlySpan<Promise<T>> promises)
            => RaceWithIndex<T, Internal.PersistedSpanEnumerator<Promise<T>>>(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T>(IEnumerable<Promise<T>> promises)
            => RaceWithIndex<T, IEnumerator<Promise<T>>>(promises.GetEnumerator());

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException(nameof(promises), "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
                }

                int index = 0;
                (int resolvedIndex, T) result = (-1, default);
                var p = promises.Current;
                if (p._ref != null)
                {
                    goto HookupMaybePending;
                }
                result = (0, p._result);
                while (promises.MoveNext())
                {
                    checked { ++index; }
                    p = promises.Current;
                    if (p._ref != null)
                    {
                        goto HookupMaybePending;
                    }
                }
                // No non-resolved promises.
                return Resolved(result);

            HookupMaybePending:
                ValidateElement(p, "promises", 1);
                var promise = Internal.PromiseRefBase.RacePromiseWithIndex<T>.GetOrCreate();
                promise.AddWaiterWithIndex(p._ref, p._id, index);
                while (promises.MoveNext())
                {
                    checked { ++index; }
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (p._ref != null)
                    {
                        promise.AddWaiterWithIndex(p._ref, p._id, index);
                    }
                    else if (result.resolvedIndex < 0)
                    {
                        result = (index, p._result);
                    }
                }
                if (result.resolvedIndex >= 0)
                {
                    promise.Forget(promise.Id);
                    return Resolved(result);
                }
                return new Promise<(int, T)>(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return FirstWithIndex<T, Internal.Enumerator<Promise<T>, Internal.TwoItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return FirstWithIndex<T, Internal.Enumerator<Promise<T>, Internal.ThreeItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return FirstWithIndex<T, Internal.Enumerator<Promise<T>, Internal.FourItems<Promise<T>>>>(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(params Promise<T>[] promises)
            => FirstWithIndex<T, Internal.ArrayEnumerator<Promise<T>>>(promises.GetGenericEnumerator());

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(ReadOnlySpan<Promise<T>> promises)
            => FirstWithIndex<T, Internal.PersistedSpanEnumerator<Promise<T>>>(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T>(IEnumerable<Promise<T>> promises)
            => FirstWithIndex<T, IEnumerator<Promise<T>>>(promises.GetEnumerator());

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException(nameof(promises), "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
                }

                int index = 0;
                (int resolvedIndex, T) result = (-1, default);
                var p = promises.Current;
                if (p._ref != null)
                {
                    goto HookupMaybePending;
                }
                result = (0, p._result);
                while (promises.MoveNext())
                {
                    checked { ++index; }
                    p = promises.Current;
                    if (p._ref != null)
                    {
                        goto HookupMaybePending;
                    }
                }
                // No non-resolved promises.
                return Resolved(result);

            HookupMaybePending:
                ValidateElement(p, "promises", 1);
                var promise = Internal.PromiseRefBase.FirstPromiseWithIndex<T>.GetOrCreate();
                uint pendingCount = 1;
                promise.AddWaiterWithIndex(p._ref, p._id, index);
                while (promises.MoveNext())
                {
                    checked { ++index; }
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (p._ref != null)
                    {
                        checked { ++pendingCount; }
                        promise.AddWaiterWithIndex(p._ref, p._id, index);
                    }
                    else if (result.resolvedIndex < 0)
                    {
                        result = (index, p._result);
                    }
                }
                promise.MarkReady(pendingCount);
                if (result.resolvedIndex >= 0)
                {
                    promise.SuppressRejection = true;
                    promise.Forget(promise.Id);
                    return Resolved(result);
                }
                return new Promise<(int, T)>(promise, promise.Id);
            }
        }
    }
}