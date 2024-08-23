#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0074 // Use compound assignment

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    public partial struct Promise<T>
    {
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return Race(Internal.GetEnumerator(promise1, promise2));
        }
        
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return Race(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return Race(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race(params Promise<T>[] promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return Race(promises.GetGenericEnumerator());
        }

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race(ReadOnlySpan<Promise<T>> promises)
            => Race(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race(IEnumerable<Promise<T>> promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return Race(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<T> Race<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException(nameof(promises), "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
                }

                bool isResolved = false;
                T result = default;
                var p = promises.Current;
                if (p._ref != null)
                {
                    goto HookupMaybePending;
                }
                isResolved = true;
                result = p._result;
                while (promises.MoveNext())
                {
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
                var promise = Internal.PromiseRefBase.RacePromise<T>.GetOrCreate();
                promise.AddWaiter(p._ref, p._id);
                while (promises.MoveNext())
                {
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    bool resolved = p._ref == null;
                    if (!resolved)
                    {
                        promise.AddWaiter(p._ref, p._id);
                    }
                    else if (!isResolved)
                    {
                        isResolved = true;
                        result = p._result;
                    }
                }
                if (isResolved)
                {
                    promise.Forget(promise.Id);
                    return Resolved(result);
                }
                return new Promise<T>(promise, promise.Id);
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return First(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return First(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the promises has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return First(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(params Promise<T>[] promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return First(promises.GetGenericEnumerator());
        }

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(ReadOnlySpan<Promise<T>> promises)
            => First(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First(IEnumerable<Promise<T>> promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return First(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve when the first of the <paramref name="promises"/> has resolved with the same value as that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<T> First<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException(nameof(promises), "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(1));
                }

                bool isResolved = false;
                T result = default;
                var p = promises.Current;
                if (p._ref != null)
                {
                    goto HookupMaybePending;
                }
                isResolved = true;
                result = p._result;
                while (promises.MoveNext())
                {
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
                var promise = Internal.PromiseRefBase.FirstPromise<T>.GetOrCreate();
                uint pendingCount = 1;
                promise.AddWaiter(p._ref, p._id);
                while (promises.MoveNext())
                {
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    bool resolved = p._ref == null;
                    if (!resolved)
                    {
                        checked { ++pendingCount; }
                        promise.AddWaiter(p._ref, p._id);
                    }
                    else if (!isResolved)
                    {
                        isResolved = true;
                        result = p._result;
                    }
                }
                promise.MarkReady(pendingCount);
                if (isResolved)
                {
                    promise.SuppressRejection = true;
                    promise.Forget(promise.Id);
                    return Resolved(result);
                }
                return new Promise<T>(promise, promise.Id);
            }
        }

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        // Old IL2CPP runtime immediately crashes if these methods exist, even if they are not used. So we only include them in newer build targets that old Unity versions cannot consume.
        // See https://github.com/timcassell/ProtoPromise/pull/106 for details.

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(Promise<T> promise1, Promise<T> promise2)
            => Promise.RaceWithIndex(promise1, promise2);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
            => Promise.RaceWithIndex(promise1, promise2, promise3);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
            => Promise.RaceWithIndex(promise1, promise2, promise3, promise4);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(params Promise<T>[] promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return RaceWithIndex(promises.GetGenericEnumerator());
        }

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(ReadOnlySpan<Promise<T>> promises)
            => RaceWithIndex(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex(IEnumerable<Promise<T>> promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return RaceWithIndex(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<(int winIndex, T result)> RaceWithIndex<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
            => Promise.RaceWithIndex<T, TEnumerator>(promises);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(Promise<T> promise1, Promise<T> promise2)
            => Promise.FirstWithIndex(promise1, promise2);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
            => Promise.FirstWithIndex(promise1, promise2, promise3);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
            => Promise.FirstWithIndex(promise1, promise2, promise3, promise4);

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(params Promise<T>[] promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return FirstWithIndex(promises.GetGenericEnumerator());
        }

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(ReadOnlySpan<Promise<T>> promises)
            => FirstWithIndex(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex(IEnumerable<Promise<T>> promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return FirstWithIndex(promises.GetEnumerator());
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> of <see cref="ValueTuple{T1, T2}"/> that will resolve when the first of the promises has resolved with the index and result of that promise.
        /// If all promises are rejected or canceled, the returned <see cref="Promise{T}"/> will be canceled or rejected with the same reason as the last <see cref="Promise{T}"/> that is rejected or canceled.
        /// </summary>
        public static Promise<(int winIndex, T result)> FirstWithIndex<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
            => Promise.FirstWithIndex<T, TEnumerator>(promises);

#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP

        [MethodImpl(Internal.InlineOption)]
        private static void GetAllResult(Internal.PromiseRefBase handler, int index, ref IList<T> result)
            => result[index] = handler.GetResult<T>();

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
        private static unsafe Internal.GetResultDelegate<IList<T>> GetAllResultFunc
        {
            [MethodImpl(Internal.InlineOption)]
            get => new(&GetAllResult);
        }
#else
        private static readonly Internal.GetResultDelegate<IList<T>> GetAllResultFunc = GetAllResult;
#endif

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, IList<T> valueContainer = null)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return All(Internal.GetEnumerator(promise1, promise2), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, IList<T> valueContainer = null)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return All(Internal.GetEnumerator(promise1, promise2, promise3), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise"/> that will resolve with a list of the promises' values in the same order when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="promise4">The fourth promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4, IList<T> valueContainer = null)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return All(Internal.GetEnumerator(promise1, promise2, promise3, promise4), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        public static Promise<IList<T>> All(params Promise<T>[] promises)
            => All(promises, new T[promises.Length]);

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(ReadOnlySpan<Promise<T>> promises, IList<T> valueContainer = null)
            => All(promises.GetPersistedEnumerator(), valueContainer);
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(Promise<T>[] promises, IList<T> valueContainer = null)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return All(promises.GetGenericEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of values in the same order as <paramref name="promises"/>s when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All(IEnumerable<Promise<T>> promises, IList<T> valueContainer = null)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return All(promises.GetEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve a list of values in the same order as <paramref name="promises"/> when they have all resolved.
        /// If any promise is rejected or canceled, the returned <see cref="Promise{T}"/> will immediately be canceled or rejected with the same reason.
        /// </summary>
        /// <param name="promises">The enumerator of promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the resolved values. If it is not provided, a new one will be created.</param>
        public static Promise<IList<T>> All<TEnumerator>(TEnumerator promises, IList<T> valueContainer = null) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (valueContainer == null)
                {
                    valueContainer = new List<T>();
                }

                int i = 0;
                int index = 0;
                int listSize = valueContainer.Count;
                Promise<T> p;
                while (promises.MoveNext())
                {
                    index = i;
                    ++i;
                    // Make sure list has the same count as promises.
                    if (listSize < i)
                    {
                        ++listSize;
                        valueContainer.Add(default);
                    }
                    p = promises.Current;
                    if (p._ref == null)
                    {
                        valueContainer[index] = p._result;
                    }
                    else
                    {
                        goto HookupMaybePending;
                    }
                }
                // No non-resolved promises.
                // Make sure list has the same count as promises.
                while (listSize > i)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                return Promise.Resolved(valueContainer);

            HookupMaybePending:
                ValidateElement(p, "promises", 1);

                // In order to prevent a race condition with the list being expanded and results being assigned concurrently,
                // we create the passthroughs and link them together in a queue before creating the return promise
                // so that we can make sure the list's size is correct before hooking up any promises.
                var passthroughs = new Internal.ValueLinkedQueue<Internal.PromiseRefBase.PromisePassThroughForAll>(
                    Internal.PromiseRefBase.PromisePassThroughForAll.GetOrCreate(p._ref, p._id, index));
                uint waitCount = 1;
                while (promises.MoveNext())
                {
                    index = i;
                    ++i;
                    // Make sure list has the same count as promises.
                    if (listSize < i)
                    {
                        ++listSize;
                        valueContainer.Add(default);
                    }
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (p._ref == null)
                    {
                        valueContainer[index] = p._result;
                    }
                    else
                    {
                        checked { ++waitCount; }
                        passthroughs.EnqueueUnsafe(
                            Internal.PromiseRefBase.PromisePassThroughForAll.GetOrCreate(p._ref, p._id, index));
                    }
                }
                // Make sure list has the same count as promises.
                while (listSize > i)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                var promise = Internal.PromiseRefBase.GetOrCreateAllPromise(valueContainer, GetAllResultFunc, passthroughs.MoveElementsToStack(), waitCount);
                return new Promise<IList<T>>(promise, promise.Id);
            }
        }

        [MethodImpl(Internal.InlineOption)]
        private static void GetAllResultContainer(Internal.PromiseRefBase handler, int index, ref IList<ResultContainer> result)
            => result[index] = new ResultContainer(handler.GetResult<T>(), handler._rejectContainer, handler.State);
        
#if NETCOREAPP || UNITY_2021_2_OR_NEWER
        private static unsafe Internal.GetResultDelegate<IList<ResultContainer>> GetAllResultContainerFunc
        {
            [MethodImpl(Internal.InlineOption)]
            get => new(&GetAllResultContainer);
        }
#else
        private static readonly Internal.GetResultDelegate<IList<ResultContainer>> GetAllResultContainerFunc = GetAllResultContainer;
#endif

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise<T> promise1, Promise<T> promise2, IList<ResultContainer> valueContainer = null)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return AllSettled(Internal.GetEnumerator(promise1, promise2), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, IList<ResultContainer> valueContainer = null)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return AllSettled(Internal.GetEnumerator(promise1, promise2, promise3), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order when they have all completed.
        /// </summary>
        /// <param name="promise1">The first promise to combine.</param>
        /// <param name="promise2">The second promise to combine.</param>
        /// <param name="promise3">The third promise to combine.</param>
        /// <param name="promise4">The fourth promise to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4, IList<ResultContainer> valueContainer = null)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return AllSettled(Internal.GetEnumerator(promise1, promise2, promise3, promise4), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        public static Promise<IList<ResultContainer>> AllSettled(params Promise<T>[] promises)
            => AllSettled(promises, new ResultContainer[promises.Length]);

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(ReadOnlySpan<Promise<T>> promises, IList<ResultContainer> valueContainer = null)
            => AllSettled(promises.GetPersistedEnumerator(), valueContainer);
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(Promise<T>[] promises, IList<ResultContainer> valueContainer = null)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return AllSettled(promises.GetGenericEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled(IEnumerable<Promise<T>> promises, IList<ResultContainer> valueContainer = null)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return AllSettled(promises.GetEnumerator(), valueContainer);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that will resolve with a list of <see cref="ResultContainer"/>s in the same order as <paramref name="promises"/> when they have all completed.
        /// </summary>
        /// <param name="promises">The enumerator of promises to combine.</param>
        /// <param name="valueContainer">Optional list that will be used to contain the result containers. If it is not provided, a new one will be created.</param>
        public static Promise<IList<ResultContainer>> AllSettled<TEnumerator>(TEnumerator promises, IList<ResultContainer> valueContainer = null) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (valueContainer == null)
                {
                    valueContainer = new List<ResultContainer>();
                }

                int i = 0;
                int index = 0;
                int listSize = valueContainer.Count;
                Promise<T> p;
                while (promises.MoveNext())
                {
                    index = i;
                    ++i;
                    // Make sure list has the same count as promises.
                    if (listSize < i)
                    {
                        ++listSize;
                        valueContainer.Add(default);
                    }
                    p = promises.Current;
                    if (p._ref == null)
                    {
                        valueContainer[index] = p._result;
                    }
                    else
                    {
                        goto HookupMaybePending;
                    }
                }
                // No non-resolved promises.
                // Make sure list has the same count as promises.
                while (listSize > i)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                return Promise.Resolved(valueContainer);

            HookupMaybePending:
                ValidateElement(p, "promises", 1);

                // In order to prevent a race condition with the list being expanded and results being assigned concurrently,
                // we create the passthroughs and link them together in a queue before creating the return promise
                // so that we can make sure the list's size is correct before hooking up any promises.
                var passthroughs = new Internal.ValueLinkedQueue<Internal.PromiseRefBase.PromisePassThroughForAll>(
                    Internal.PromiseRefBase.PromisePassThroughForAll.GetOrCreate(p._ref, p._id, index));
                uint waitCount = 1;
                while (promises.MoveNext())
                {
                    index = i;
                    ++i;
                    // Make sure list has the same count as promises.
                    if (listSize < i)
                    {
                        ++listSize;
                        valueContainer.Add(default);
                    }
                    p = promises.Current;
                    ValidateElement(p, "promises", 1);
                    if (p._ref == null)
                    {
                        valueContainer[index] = p._result;
                    }
                    else
                    {
                        checked { ++waitCount; }
                        passthroughs.EnqueueUnsafe(
                            Internal.PromiseRefBase.PromisePassThroughForAll.GetOrCreate(p._ref, p._id, index));
                    }
                }
                // Make sure list has the same count as promises.
                while (listSize > i)
                {
                    valueContainer.RemoveAt(--listSize);
                }
                var promise = Internal.PromiseRefBase.GetOrCreateAllSettledPromise(valueContainer, GetAllResultContainerFunc, passthroughs.MoveElementsToStack(), waitCount);
                return new Promise<IList<ResultContainer>>(promise, promise.Id);
            }
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
            ValidateArgument(resolver, nameof(resolver), 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.New(Internal.PromiseRefBase.DelegateWrapper.Create<T>(resolver), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
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
            ValidateArgument(resolver, nameof(resolver), 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.New(Internal.PromiseRefBase.DelegateWrapper.Create<TCapture, T>(captureValue, resolver), (Internal.SynchronizationOption) synchronizationOption, null, forceAsync);
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
            ValidateArgument(resolver, nameof(resolver), 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.New(Internal.PromiseRefBase.DelegateWrapper.Create<T>(resolver), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
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
            ValidateArgument(resolver, nameof(resolver), 1);

            return Internal.PromiseRefBase.CallbackHelperResult<T>.New(Internal.PromiseRefBase.DelegateWrapper.Create<TCapture, T>(captureValue, resolver), Internal.SynchronizationOption.Explicit, synchronizationContext, forceAsync);
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is already resolved with <paramref name="value"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
		public static Promise<T> Resolved(T value)
            => Promise.Resolved(value);

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
        [MethodImpl(Internal.InlineOption)]
        public static Promise<T> Canceled()
            => Internal.CreateCanceled<T>();

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> that is either canceled or rejected with the provided <paramref name="exception"/>.
        /// </summary>
        /// <param name="exception">If an <see cref="OperationCanceledException"/>, the returned promise will be canceled, otherwise it will be rejected.</param>
        public static Promise<T> FromException(Exception exception)
            => exception is OperationCanceledException ? Canceled() : Rejected(exception);

        /// <summary>
        /// Returns a <see cref="Promise{T}.Deferred"/> object that is linked to and controls the state of a new <see cref="Promise{T}"/>.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public static Deferred NewDeferred()
            => Deferred.New();
    }
}