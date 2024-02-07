#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0062 // Make local function 'static'

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Computes the sum of an async-enumerable sequence of <see cref="int"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="int"/> values to calculate the sum of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the sum of the values in the async-enumerable sequence.</returns>
        /// <remarks>The result will be zero if <paramref name="source"/> contains no elements.</remarks>
        public static Promise<int> SumAsync(this AsyncEnumerable<int> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<int> Core(AsyncEnumerator<int> asyncEnumerator)
            {
                try
                {
                    int sum = 0;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        checked
                        {
                            sum += asyncEnumerator.Current;
                        }
                    }
                    return sum;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the sum of an async-enumerable sequence of <see cref="long"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="long"/> values to calculate the sum of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the sum of the values in the async-enumerable sequence.</returns>
        /// <remarks>The result will be zero if <paramref name="source"/> contains no elements.</remarks>
        public static Promise<long> SumAsync(this AsyncEnumerable<long> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<long> Core(AsyncEnumerator<long> asyncEnumerator)
            {
                try
                {
                    long sum = 0;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        checked
                        {
                            sum += asyncEnumerator.Current;
                        }
                    }
                    return sum;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the sum of an async-enumerable sequence of <see cref="float"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="float"/> values to calculate the sum of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the sum of the values in the async-enumerable sequence.</returns>
        /// <remarks>The result will be zero if <paramref name="source"/> contains no elements.</remarks>
        public static Promise<float> SumAsync(this AsyncEnumerable<float> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<float> Core(AsyncEnumerator<float> asyncEnumerator)
            {
                try
                {
                    float sum = 0;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        sum += asyncEnumerator.Current;
                    }
                    return sum;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the sum of an async-enumerable sequence of <see cref="double"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="double"/> values to calculate the sum of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the sum of the values in the async-enumerable sequence.</returns>
        /// <remarks>The result will be zero if <paramref name="source"/> contains no elements.</remarks>
        public static Promise<double> SumAsync(this AsyncEnumerable<double> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<double> Core(AsyncEnumerator<double> asyncEnumerator)
            {
                try
                {
                    double sum = 0;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        sum += asyncEnumerator.Current;
                    }
                    return sum;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the sum of an async-enumerable sequence of <see cref="decimal"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="decimal"/> values to calculate the sum of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the sum of the values in the async-enumerable sequence.</returns>
        /// <remarks>The result will be zero if <paramref name="source"/> contains no elements.</remarks>
        public static Promise<decimal> SumAsync(this AsyncEnumerable<decimal> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<decimal> Core(AsyncEnumerator<decimal> asyncEnumerator)
            {
                try
                {
                    decimal sum = 0;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        sum += asyncEnumerator.Current;
                    }
                    return sum;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the sum of an async-enumerable sequence of nullable <see cref="int"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="int"/> values to calculate the sum of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the sum of the values in the async-enumerable sequence.</returns>
        /// <remarks>
        /// Items in <paramref name="source"/> that are <see langword="null"/> are excluded from the computation of the sum.
        /// The result will be zero if <paramref name="source"/> contains no elements or all elements are <see langword="null"/>.
        /// </remarks>
        public static Promise<int> SumAsync(this AsyncEnumerable<int?> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<int> Core(AsyncEnumerator<int?> asyncEnumerator)
            {
                try
                {
                    int sum = 0;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        checked
                        {
                            sum += asyncEnumerator.Current.GetValueOrDefault();
                        }
                    }
                    return sum;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the sum of an async-enumerable sequence of nullable <see cref="long"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="long"/> values to calculate the sum of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the sum of the values in the async-enumerable sequence.</returns>
        /// <remarks>
        /// Items in <paramref name="source"/> that are <see langword="null"/> are excluded from the computation of the sum.
        /// The result will be zero if <paramref name="source"/> contains no elements or all elements are <see langword="null"/>.
        /// </remarks>
        public static Promise<long> SumAsync(this AsyncEnumerable<long?> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<long> Core(AsyncEnumerator<long?> asyncEnumerator)
            {
                try
                {
                    long sum = 0;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        checked
                        {
                            sum += asyncEnumerator.Current.GetValueOrDefault();
                        }
                    }
                    return sum;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the sum of an async-enumerable sequence of nullable <see cref="float"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="float"/> values to calculate the sum of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the sum of the values in the async-enumerable sequence.</returns>
        /// <remarks>
        /// Items in <paramref name="source"/> that are <see langword="null"/> are excluded from the computation of the sum.
        /// The result will be zero if <paramref name="source"/> contains no elements or all elements are <see langword="null"/>.
        /// </remarks>
        public static Promise<float> SumAsync(this AsyncEnumerable<float?> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<float> Core(AsyncEnumerator<float?> asyncEnumerator)
            {
                try
                {
                    float sum = 0;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        sum += asyncEnumerator.Current.GetValueOrDefault();
                    }
                    return sum;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the sum of an async-enumerable sequence of nullable <see cref="double"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="double"/> values to calculate the sum of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the sum of the values in the async-enumerable sequence.</returns>
        /// <remarks>
        /// Items in <paramref name="source"/> that are <see langword="null"/> are excluded from the computation of the sum.
        /// The result will be zero if <paramref name="source"/> contains no elements or all elements are <see langword="null"/>.
        /// </remarks>
        public static Promise<double> SumAsync(this AsyncEnumerable<double?> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<double> Core(AsyncEnumerator<double?> asyncEnumerator)
            {
                try
                {
                    double sum = 0;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        sum += asyncEnumerator.Current.GetValueOrDefault();
                    }
                    return sum;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the sum of an async-enumerable sequence of nullable <see cref="decimal"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="decimal"/> values to calculate the sum of.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the sum of the values in the async-enumerable sequence.</returns>
        /// <remarks>
        /// Items in <paramref name="source"/> that are <see langword="null"/> are excluded from the computation of the sum.
        /// The result will be zero if <paramref name="source"/> contains no elements or all elements are <see langword="null"/>.
        /// </remarks>
        public static Promise<decimal> SumAsync(this AsyncEnumerable<decimal?> source, CancelationToken cancelationToken = default)
        {
            return Core(source.GetAsyncEnumerator(cancelationToken));

            async Promise<decimal> Core(AsyncEnumerator<decimal?> asyncEnumerator)
            {
                try
                {
                    decimal sum = 0;
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        sum += asyncEnumerator.Current.GetValueOrDefault();
                    }
                    return sum;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}