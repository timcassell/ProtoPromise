#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using System;

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Applies an accumulator function over an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="accumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty.</exception>
        public static Promise<TSource> AggregateAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, TSource, TSource> accumulator, CancelationToken cancelationToken = default)
        {
            ValidateArgument(accumulator, nameof(accumulator), 1);

            return AggregateCoreSync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(accumulator));
        }

        /// <summary>
        /// Applies an accumulator function over an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="accumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty.</exception>
        public static Promise<TSource> AggregateAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, TSource, TSource> accumulator, CancelationToken cancelationToken = default)
        {
            ValidateArgument(accumulator, nameof(accumulator), 1);

            return AggregateCoreSync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, accumulator));
        }

        private static async Promise<TSource> AggregateCoreSync<TSource, TAccumulator>(AsyncEnumerator<TSource> asyncEnumerator, TAccumulator accumulator)
            where TAccumulator : Internal.IFunc<TSource, TSource, TSource>
        {
            try
            {
                if (!await asyncEnumerator.MoveNextAsync())
                {
                    throw new InvalidOperationException("source contains no elements.");
                }

                var acc = asyncEnumerator.Current;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    acc = accumulator.Invoke(acc, asyncEnumerator.Current);
                }
                return acc;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Applies an accumulator function over a configured async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="accumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty.</exception>
        public static Promise<TSource> AggregateAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, TSource, TSource> accumulator)
        {
            ValidateArgument(accumulator, nameof(accumulator), 1);

            return AggregateCoreSync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(accumulator));
        }

        /// <summary>
        /// Applies an accumulator function over a configured async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="accumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty.</exception>
        public static Promise<TSource> AggregateAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, TSource, TSource> accumulator)
        {
            ValidateArgument(accumulator, nameof(accumulator), 1);

            return AggregateCoreSync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, accumulator));
        }

        private static async Promise<TSource> AggregateCoreSync<TSource, TAccumulator>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TAccumulator accumulator)
            where TAccumulator : Internal.IFunc<TSource, TSource, TSource>
        {
            try
            {
                if (!await asyncEnumerator.MoveNextAsync())
                {
                    throw new InvalidOperationException("configuredSource contains no elements.");
                }

                var acc = asyncEnumerator.Current;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    acc = accumulator.Invoke(acc, asyncEnumerator.Current);
                }
                return acc;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Applies an accumulator function over an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="asyncAccumulator">An async accumulator function to be invoked on each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAccumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty.</exception>
        public static Promise<TSource> AggregateAsync<TSource>(this AsyncEnumerable<TSource> source, Func<TSource, TSource, Promise<TSource>> asyncAccumulator, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAccumulator, nameof(asyncAccumulator), 1);

            return AggregateCoreAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(asyncAccumulator));
        }

        /// <summary>
        /// Applies an accumulator function over an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="asyncAccumulator"/>.</param>
        /// <param name="asyncAccumulator">An async accumulator function to be invoked on each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAccumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty.</exception>
        public static Promise<TSource> AggregateAsync<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, TSource, TSource, Promise<TSource>> asyncAccumulator, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAccumulator, nameof(asyncAccumulator), 1);

            return AggregateCoreAsync(source.GetAsyncEnumerator(cancelationToken), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, asyncAccumulator));
        }

        private static async Promise<TSource> AggregateCoreAsync<TSource, TAccumulator>(AsyncEnumerator<TSource> asyncEnumerator, TAccumulator asyncAccumulator)
            where TAccumulator : Internal.IFunc<TSource, TSource, Promise<TSource>>
        {
            try
            {
                if (!await asyncEnumerator.MoveNextAsync())
                {
                    throw new InvalidOperationException("source contains no elements.");
                }

                var acc = asyncEnumerator.Current;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    acc = await asyncAccumulator.Invoke(acc, asyncEnumerator.Current);
                }
                return acc;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Applies an accumulator function over a configured async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="asyncAccumulator">An async accumulator function to be invoked on each element.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAccumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty.</exception>
        public static Promise<TSource> AggregateAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, Func<TSource, TSource, Promise<TSource>> asyncAccumulator)
        {
            ValidateArgument(asyncAccumulator, nameof(asyncAccumulator), 1);

            return AggregateCoreAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(asyncAccumulator));
        }

        /// <summary>
        /// Applies an accumulator function over a configured async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="asyncAccumulator"/>.</param>
        /// <param name="asyncAccumulator">An async accumulator function to be invoked on each element.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAccumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty.</exception>
        public static Promise<TSource> AggregateAsync<TSource, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, Func<TCapture, TSource, TSource, Promise<TSource>> asyncAccumulator)
        {
            ValidateArgument(asyncAccumulator, nameof(asyncAccumulator), 1);

            return AggregateCoreAsync(configuredSource.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, asyncAccumulator));
        }

        private static async Promise<TSource> AggregateCoreAsync<TSource, TAccumulator>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TAccumulator asyncAccumulator)
            where TAccumulator : Internal.IFunc<TSource, TSource, Promise<TSource>>
        {
            try
            {
                if (!await asyncEnumerator.MoveNextAsync())
                {
                    throw new InvalidOperationException("configuredSource contains no elements.");
                }

                var acc = asyncEnumerator.Current;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    acc = await asyncAccumulator.Invoke(acc, asyncEnumerator.Current);
                }
                return acc;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Applies an accumulator function over an async-enumerable sequence. The specified seed value is used as the initial accumulator value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="accumulator"/> is null.</exception>
        public static Promise<TAccumulate> AggregateAsync<TSource, TAccumulate>(this AsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator, CancelationToken cancelationToken = default)
        {
            ValidateArgument(accumulator, nameof(accumulator), 1);

            return AggregateCoreSync(source.GetAsyncEnumerator(cancelationToken), seed, Internal.PromiseRefBase.DelegateWrapper.Create(accumulator));
        }

        /// <summary>
        /// Applies an accumulator function over an async-enumerable sequence. The specified seed value is used as the initial accumulator value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="accumulator"/> is null.</exception>
        public static Promise<TAccumulate> AggregateAsync<TSource, TAccumulate, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, TAccumulate seed, Func<TCapture, TAccumulate, TSource, TAccumulate> accumulator, CancelationToken cancelationToken = default)
        {
            ValidateArgument(accumulator, nameof(accumulator), 1);

            return AggregateCoreSync(source.GetAsyncEnumerator(cancelationToken), seed, Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, accumulator));
        }

        private static async Promise<TAccumulate> AggregateCoreSync<TSource, TAccumulate, TAccumulator>(AsyncEnumerator<TSource> asyncEnumerator, TAccumulate seed, TAccumulator accumulator)
            where TAccumulator : Internal.IFunc<TAccumulate, TSource, TAccumulate>
        {
            try
            {
                var acc = seed;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    acc = accumulator.Invoke(acc, asyncEnumerator.Current);
                }
                return acc;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Applies an accumulator function over a configured async-enumerable sequence. The specified seed value is used as the initial accumulator value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="accumulator"/> is null.</exception>
        public static Promise<TAccumulate> AggregateAsync<TSource, TAccumulate>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
        {
            ValidateArgument(accumulator, nameof(accumulator), 1);

            return AggregateCoreSync(configuredSource.GetAsyncEnumerator(), seed, Internal.PromiseRefBase.DelegateWrapper.Create(accumulator));
        }

        /// <summary>
        /// Applies an accumulator function over a configured async-enumerable sequence. The specified seed value is used as the initial accumulator value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="accumulator"/>.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="accumulator">An accumulator function to be invoked on each element.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="accumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty.</exception>
        public static Promise<TAccumulate> AggregateAsync<TSource, TAccumulate, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, TAccumulate seed, Func<TCapture, TAccumulate, TSource, TAccumulate> accumulator)
        {
            ValidateArgument(accumulator, nameof(accumulator), 1);

            return AggregateCoreSync(configuredSource.GetAsyncEnumerator(), seed, Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, accumulator));
        }

        private static async Promise<TAccumulate> AggregateCoreSync<TSource, TAccumulate, TAccumulator>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TAccumulate seed, TAccumulator accumulator)
            where TAccumulator : Internal.IFunc<TAccumulate, TSource, TAccumulate>
        {
            try
            {
                var acc = seed;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    acc = accumulator.Invoke(acc, asyncEnumerator.Current);
                }
                return acc;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Applies an accumulator function over an async-enumerable sequence. The specified seed value is used as the initial accumulator value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="asyncAccumulator">An async accumulator function to be invoked on each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAccumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty.</exception>
        public static Promise<TAccumulate> AggregateAsync<TSource, TAccumulate>(this AsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, Promise<TAccumulate>> asyncAccumulator, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAccumulator, nameof(asyncAccumulator), 1);

            return AggregateCoreAsync(source.GetAsyncEnumerator(cancelationToken), seed, Internal.PromiseRefBase.DelegateWrapper.Create(asyncAccumulator));
        }

        /// <summary>
        /// Applies an accumulator function over an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="asyncAccumulator"/>.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="asyncAccumulator">An async accumulator function to be invoked on each element.</param>
        /// <param name="cancelationToken">The optional cancelation token to be used for canceling the sequence at any time.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAccumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty.</exception>
        public static Promise<TAccumulate> AggregateAsync<TSource, TAccumulate, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, TAccumulate seed, Func<TCapture, TAccumulate, TSource, Promise<TAccumulate>> asyncAccumulator, CancelationToken cancelationToken = default)
        {
            ValidateArgument(asyncAccumulator, nameof(asyncAccumulator), 1);

            return AggregateCoreAsync(source.GetAsyncEnumerator(cancelationToken), seed, Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, asyncAccumulator));
        }

        private static async Promise<TAccumulate> AggregateCoreAsync<TSource, TAccumulate, TAccumulator>(AsyncEnumerator<TSource> asyncEnumerator, TAccumulate seed, TAccumulator asyncAccumulator)
            where TAccumulator : Internal.IFunc<TAccumulate, TSource, Promise<TAccumulate>>
        {
            try
            {
                var acc = seed;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    acc = await asyncAccumulator.Invoke(acc, asyncEnumerator.Current);
                }
                return acc;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }

        /// <summary>
        /// Applies an accumulator function over a configured async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="asyncAccumulator">An async accumulator function to be invoked on each element.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAccumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty.</exception>
        public static Promise<TAccumulate> AggregateAsync<TSource, TAccumulate>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TAccumulate seed, Func<TAccumulate, TSource, Promise<TAccumulate>> asyncAccumulator)
        {
            ValidateArgument(asyncAccumulator, nameof(asyncAccumulator), 1);

            return AggregateCoreAsync(configuredSource.GetAsyncEnumerator(), seed, Internal.PromiseRefBase.DelegateWrapper.Create(asyncAccumulator));
        }

        /// <summary>
        /// Applies an accumulator function over a configured async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence and the result of the aggregation.</typeparam>
        /// <typeparam name="TAccumulate">The type of the result of the aggregation.</typeparam>
        /// <typeparam name="TCapture">The type of the captured value.</typeparam>
        /// <param name="configuredSource">Configured source sequence.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="asyncAccumulator"/>.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="asyncAccumulator">An async accumulator function to be invoked on each element.</param>
        /// <returns><see cref="Promise{T}"/> containing the final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncAccumulator"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="configuredSource"/> is empty.</exception>
        public static Promise<TAccumulate> AggregateAsync<TSource, TAccumulate, TCapture>(this in ConfiguredAsyncEnumerable<TSource> configuredSource, TCapture captureValue, TAccumulate seed, Func<TCapture, TAccumulate, TSource, Promise<TAccumulate>> asyncAccumulator)
        {
            ValidateArgument(asyncAccumulator, nameof(asyncAccumulator), 1);

            return AggregateCoreAsync(configuredSource.GetAsyncEnumerator(), seed, Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, asyncAccumulator));
        }

        private static async Promise<TAccumulate> AggregateCoreAsync<TSource, TAccumulate, TAccumulator>(ConfiguredAsyncEnumerable<TSource>.Enumerator asyncEnumerator, TAccumulate seed, TAccumulator asyncAccumulator)
            where TAccumulator : Internal.IFunc<TAccumulate, TSource, Promise<TAccumulate>>
        {
            try
            {
                var acc = seed;
                while (await asyncEnumerator.MoveNextAsync())
                {
                    acc = await asyncAccumulator.Invoke(acc, asyncEnumerator.Current);
                }
                return acc;
            }
            finally
            {
                await asyncEnumerator.DisposeAsync();
            }
        }
    }
}