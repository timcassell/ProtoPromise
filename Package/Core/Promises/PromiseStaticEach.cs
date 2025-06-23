#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq;
using System;
using System.Collections.Generic;

namespace Proto.Promises
{
    public partial struct Promise
    {
        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(Promise promise1, Promise promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return Each(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(Promise promise1, Promise promise2, Promise promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return Each(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(Promise promise1, Promise promise2, Promise promise3, Promise promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return Each(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(params Promise[] promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return Each(promises.GetGenericEnumerator());
        }

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(ReadOnlySpan<Promise> promises)
            => Each(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(IEnumerable<Promise> promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return Each(promises.GetEnumerator());
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    return AsyncEnumerable<ResultContainer>.Empty();
                }

                var eachPromise = Internal.GetOrCreatePromiseEachAsyncEnumerableVoid();
                do
                {
                    var promise = promises.Current;
                    ValidateElement(promise, "promises", 1);
                    if (promise._ref == null)
                    {
                        eachPromise.AddResult(ResultContainer.Resolved);
                    }
                    else
                    {
                        eachPromise.AddPromise(promise._ref, promise._id);
                    }
                } while (promises.MoveNext());
                return new AsyncEnumerable<ResultContainer>(eachPromise);
            }
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<Promise<T>.ResultContainer> Each<T>(Promise<T> promise1, Promise<T> promise2)
            => Promise<T>.Each(promise1, promise2);

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<Promise<T>.ResultContainer> Each<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
            => Promise<T>.Each(promise1, promise2, promise3);

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<Promise<T>.ResultContainer> Each<T>(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
            => Promise<T>.Each(promise1, promise2, promise3, promise4);

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<Promise<T>.ResultContainer> Each<T>(params Promise<T>[] promises)
            => Promise<T>.Each(promises);

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<Promise<T>.ResultContainer> Each<T>(ReadOnlySpan<Promise<T>> promises)
            => Promise<T>.Each(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<Promise<T>.ResultContainer> Each<T>(IEnumerable<Promise<T>> promises)
            => Promise<T>.Each(promises);

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<Promise<T>.ResultContainer> Each<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
            => Promise<T>.Each(promises);
    }

    public partial struct Promise<T>
    {
        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(Promise<T> promise1, Promise<T> promise2)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            return Each(Internal.GetEnumerator(promise1, promise2));
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            return Each(Internal.GetEnumerator(promise1, promise2, promise3));
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(Promise<T> promise1, Promise<T> promise2, Promise<T> promise3, Promise<T> promise4)
        {
            ValidateArgument(promise1, nameof(promise1), 1);
            ValidateArgument(promise2, nameof(promise2), 1);
            ValidateArgument(promise3, nameof(promise3), 1);
            ValidateArgument(promise4, nameof(promise4), 1);
            return Each(Internal.GetEnumerator(promise1, promise2, promise3, promise4));
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(params Promise<T>[] promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return Each(promises.GetGenericEnumerator());
        }

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(ReadOnlySpan<Promise<T>> promises)
            => Each(promises.GetPersistedEnumerator());
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each(IEnumerable<Promise<T>> promises)
        {
            ValidateArgument(promises, nameof(promises), 1);
            return Each(promises.GetEnumerator());
        }

        /// <summary>
        /// Creates an <see cref="AsyncEnumerable{T}"/> that will yield the results of the supplied promises as those promises complete.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="PromiseEachGroup{T}"/> instead.
        /// </remarks>
        public static AsyncEnumerable<ResultContainer> Each<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
        {
            ValidateArgument(promises, nameof(promises), 1);

            using (promises)
            {
                if (!promises.MoveNext())
                {
                    return AsyncEnumerable<ResultContainer>.Empty();
                }

                var eachPromise = Internal.GetOrCreatePromiseEachAsyncEnumerable<T>();
                do
                {
                    var promise = promises.Current;
                    ValidateElement(promise, "promises", 1);
                    if (promise._ref == null)
                    {
                        eachPromise.AddResult(promise._result);
                    }
                    else
                    {
                        eachPromise.AddPromise(promise._ref, promise._id);
                    }
                } while (promises.MoveNext());
                return new AsyncEnumerable<ResultContainer>(eachPromise);
            }
        }
    }
}