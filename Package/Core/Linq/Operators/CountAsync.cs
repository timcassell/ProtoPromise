#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Async.CompilerServices;
using System;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        // Implementation note: unlike System.Linq which does a type-check for ICollection<T> before iterating, we don't do it.
        // This is mostly because async enumerable values are expected to be evaluated lazily rather than all at once.
        // There are a few cases where the optimization would help (like array.ToAsyncEnumerable()), but it's not worth the added complexity to support those.

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in the number of elements in an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence that contains elements to be counted.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in number of elements in the input sequence.</returns>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource>(this AsyncEnumerable<TSource> source)
            => CountCore(source.GetAsyncEnumerator());

        private static async Promise<int> CountCore<TSource>(AsyncEnumerator<TSource> asyncEnumerator)
        {
            try
            {
                int count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    checked
                    {
                        ++count;
                    }
                }
                return count;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence in which to locate a value.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence in which to locate a value.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<int> CountCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, bool>
        {
            try
            {
                int count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (predicate.Invoke(asyncEnumerator.Current))
                    {
                        checked
                        {
                            ++count;
                        }
                    }
                }
                return count;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence in which to locate a value.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountAwaitCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence in which to locate a value.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountAwaitCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<int> CountAwaitCore<TSource, TPredicate>(AsyncEnumerator<TSource> asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, Promise<bool>>
        {
            try
            {
                int count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (await predicate.Invoke(asyncEnumerator.Current))
                    {
                        checked
                        {
                            ++count;
                        }
                    }
                }
                return count;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence in which to locate a value.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource>(this ConfiguredAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence in which to locate a value.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource, TCapture>(this ConfiguredAsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, bool> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<int> CountCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, bool>
        {
            try
            {
                int count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (predicate.Invoke(asyncEnumerator.Current))
                    {
                        checked
                        {
                            ++count;
                        }
                    }
                }
                return count;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence in which to locate a value.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource>(this ConfiguredAsyncEnumerable<TSource> source, Func<TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountAwaitCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(predicate));
        }

        /// <summary>
        /// Returns a <see cref="Promise{T}"/> resulting in how many elements in the specified configured async-enumerable sequence satisfy a condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence in which to locate a value.</param>
        /// <param name="predicate">An async function to test each element for a condition.</param>
        /// <param name="captureValue">The extra value that will be passed to the <paramref name="predicate"/>.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in how many elements in the sequence satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="OverflowException">The number of elements in the source sequence is larger than <see cref="int.MaxValue"/>.</exception>
        public static Promise<int> CountAsync<TSource, TCapture>(this ConfiguredAsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, Promise<bool>> predicate)
        {
            ValidateArgument(predicate, nameof(predicate), 1);

            return CountAwaitCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, predicate));
        }

        private static async Promise<int> CountAwaitCore<TSource, TPredicate>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TPredicate predicate)
            where TPredicate : Internal.IFunc<TSource, Promise<bool>>
        {
            try
            {
                int count = 0;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    if (await predicate.Invoke(asyncEnumerator.Current))
                    {
                        checked
                        {
                            ++count;
                        }
                    }
                }
                return count;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}