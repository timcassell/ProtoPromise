#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using System;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Computes the average of an async-enumerable sequence of <see cref="int"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="int"/> values to calculate the average of.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the average of the sequence of values.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements.</exception>
        public static Promise<double> AverageAsync(this AsyncEnumerable<int> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<double> Core(AsyncEnumerator<int> asyncEnumerator)
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source contains no elements.");
                    }

                    long sum = asyncEnumerator.Current;
                    long count = 1;
                    checked
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            sum += asyncEnumerator.Current;
                            ++count;
                        }
                    }

                    return (double) sum / count;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the average of an async-enumerable sequence of <see cref="long"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="long"/> values to calculate the average of.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the average of the sequence of values.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements.</exception>
        public static Promise<double> AverageAsync(this AsyncEnumerable<long> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<double> Core(AsyncEnumerator<long> asyncEnumerator)
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source contains no elements.");
                    }

                    long sum = asyncEnumerator.Current;
                    long count = 1;
                    checked
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            sum += asyncEnumerator.Current;
                            ++count;
                        }
                    }

                    return (double) sum / count;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the average of an async-enumerable sequence of <see cref="float"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="float"/> values to calculate the average of.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the average of the sequence of values.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements.</exception>
        public static Promise<float> AverageAsync(this AsyncEnumerable<float> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<float> Core(AsyncEnumerator<float> asyncEnumerator)
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source contains no elements.");
                    }

                    double sum = asyncEnumerator.Current;
                    long count = 1;
                    checked
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            sum += asyncEnumerator.Current;
                            ++count;
                        }
                    }

                    return (float) (sum / count);
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the average of an async-enumerable sequence of <see cref="double"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="double"/> values to calculate the average of.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the average of the sequence of values.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements.</exception>
        public static Promise<double> AverageAsync(this AsyncEnumerable<double> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<double> Core(AsyncEnumerator<double> asyncEnumerator)
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source contains no elements.");
                    }

                    double sum = asyncEnumerator.Current;
                    long count = 1;
                    checked
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            sum += asyncEnumerator.Current;
                            ++count;
                        }
                    }

                    return sum / count;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the average of an async-enumerable sequence of <see cref="decimal"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="decimal"/> values to calculate the average of.</param>
        /// <returns>A <see cref="Promise{T}"/> resulting in the average of the sequence of values.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements.</exception>
        public static Promise<decimal> AverageAsync(this AsyncEnumerable<decimal> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<decimal> Core(AsyncEnumerator<decimal> asyncEnumerator)
            {
                try
                {
                    if (!await asyncEnumerator.MoveNextAsync())
                    {
                        throw new InvalidOperationException("source contains no elements.");
                    }

                    decimal sum = asyncEnumerator.Current;
                    long count = 1;
                    checked
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            sum += asyncEnumerator.Current;
                            ++count;
                        }
                    }

                    return sum / count;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the average of an async-enumerable sequence of nullable <see cref="int"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="int"/> values to calculate the average of.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the average of the sequence of values,
        /// or <see langword="null"/> if the source sequence is empty or contains only values that are <see langword="null"/>.
        /// </returns>
        public static Promise<double?> AverageAsync(this AsyncEnumerable<int?> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<double?> Core(AsyncEnumerator<int?> asyncEnumerator)
            {
                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        int? v = asyncEnumerator.Current;
                        if (v.HasValue)
                        {
                            long sum = v.GetValueOrDefault();
                            long count = 1;
                            checked
                            {
                                while (await asyncEnumerator.MoveNextAsync())
                                {
                                    v = asyncEnumerator.Current;
                                    if (v.HasValue)
                                    {
                                        sum += v.GetValueOrDefault();
                                        ++count;
                                    }
                                }
                            }

                            return (double) sum / count;
                        }
                    }

                    return null;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the average of an async-enumerable sequence of nullable <see cref="long"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="long"/> values to calculate the average of.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the average of the sequence of values,
        /// or <see langword="null"/> if the source sequence is empty or contains only values that are <see langword="null"/>.
        /// </returns>
        public static Promise<double?> AverageAsync(this AsyncEnumerable<long?> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<double?> Core(AsyncEnumerator<long?> asyncEnumerator)
            {
                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        long? v = asyncEnumerator.Current;
                        if (v.HasValue)
                        {
                            long sum = v.GetValueOrDefault();
                            long count = 1;
                            checked
                            {
                                while (await asyncEnumerator.MoveNextAsync())
                                {
                                    v = asyncEnumerator.Current;
                                    if (v.HasValue)
                                    {
                                        sum += v.GetValueOrDefault();
                                        ++count;
                                    }
                                }
                            }

                            return (double) sum / count;
                        }
                    }

                    return null;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the average of an async-enumerable sequence of nullable <see cref="float"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="float"/> values to calculate the average of.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the average of the sequence of values,
        /// or <see langword="null"/> if the source sequence is empty or contains only values that are <see langword="null"/>.
        /// </returns>
        public static Promise<float?> AverageAsync(this AsyncEnumerable<float?> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<float?> Core(AsyncEnumerator<float?> asyncEnumerator)
            {
                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        float? v = asyncEnumerator.Current;
                        if (v.HasValue)
                        {
                            double sum = v.GetValueOrDefault();
                            long count = 1;
                            checked
                            {
                                while (await asyncEnumerator.MoveNextAsync())
                                {
                                    v = asyncEnumerator.Current;
                                    if (v.HasValue)
                                    {
                                        sum += v.GetValueOrDefault();
                                        ++count;
                                    }
                                }
                            }

                            return (float) (sum / count);
                        }
                    }

                    return null;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the average of an async-enumerable sequence of nullable <see cref="double"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="double"/> values to calculate the average of.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the average of the sequence of values,
        /// or <see langword="null"/> if the source sequence is empty or contains only values that are <see langword="null"/>.
        /// </returns>
        public static Promise<double?> AverageAsync(this AsyncEnumerable<double?> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<double?> Core(AsyncEnumerator<double?> asyncEnumerator)
            {
                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        double? v = asyncEnumerator.Current;
                        if (v.HasValue)
                        {
                            double sum = v.GetValueOrDefault();
                            long count = 1;
                            checked
                            {
                                while (await asyncEnumerator.MoveNextAsync())
                                {
                                    v = asyncEnumerator.Current;
                                    if (v.HasValue)
                                    {
                                        sum += v.GetValueOrDefault();
                                        ++count;
                                    }
                                }
                            }

                            return sum / count;
                        }
                    }

                    return null;
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Computes the average of an async-enumerable sequence of nullable <see cref="decimal"/> values.
        /// </summary>
        /// <param name="source">An async-enumerable sequence of <see cref="decimal"/> values to calculate the average of.</param>
        /// <returns>
        /// A <see cref="Promise{T}"/> resulting in the average of the sequence of values,
        /// or <see langword="null"/> if the source sequence is empty or contains only values that are <see langword="null"/>.
        /// </returns>
        public static Promise<decimal?> AverageAsync(this AsyncEnumerable<decimal?> source)
        {
            return Core(source.GetAsyncEnumerator());

            async Promise<decimal?> Core(AsyncEnumerator<decimal?> asyncEnumerator)
            {
                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        decimal? v = asyncEnumerator.Current;
                        if (v.HasValue)
                        {
                            decimal sum = v.GetValueOrDefault();
                            long count = 1;
                            checked
                            {
                                while (await asyncEnumerator.MoveNextAsync())
                                {
                                    v = asyncEnumerator.Current;
                                    if (v.HasValue)
                                    {
                                        sum += v.GetValueOrDefault();
                                        ++count;
                                    }
                                }
                            }

                            return sum / count;
                        }
                    }

                    return null;
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