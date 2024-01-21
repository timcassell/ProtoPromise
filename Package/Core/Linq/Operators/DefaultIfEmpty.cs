#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Returns an async-enumerable sequence containing the elements of the <paramref name="source"/> sequence or the default value for the <typeparamref name="TSource"/> type if the <paramref name="source"/> sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source">The sequence to return a default value for if it is empty.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements of the <paramref name="source"/> sequence or the default value for the <typeparamref name="TSource"/> type if the <paramref name="source"/> sequence is empty.</returns>
        public static AsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this AsyncEnumerable<TSource> source)
            => DefaultIfEmpty(source, default(TSource));

        /// <summary>
        /// Returns an async-enumerable sequence containing the elements of the <paramref name="source"/> sequence or the specified <paramref name="defaultValue"/> if the <paramref name="source"/> sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source">The sequence to return a default value for if it is empty.</param>
        /// <param name="defaultValue">The value to return if the sequence is empty.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements of the <paramref name="source"/> sequence or the default value for the <typeparamref name="TSource"/> type if the <paramref name="source"/> sequence is empty.</returns>
        public static AsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this AsyncEnumerable<TSource> source, TSource defaultValue)
            => AsyncEnumerable<TSource>.Create((asyncEnumerator: source.GetAsyncEnumerator(), defaultValue), async (cv, writer, cancelationToken) =>
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                cv.asyncEnumerator._target._cancelationToken = cancelationToken;
                try
                {
                    if (!await cv.asyncEnumerator.MoveNextAsync())
                    {
                        await writer.YieldAsync(cv.defaultValue);
                        return;
                    }
                    do
                    {
                        await writer.YieldAsync(cv.asyncEnumerator.Current);
                    } while (await cv.asyncEnumerator.MoveNextAsync());
                }
                finally
                {
                    await cv.asyncEnumerator.DisposeAsync();
                }
            });

        /// <summary>
        /// Returns an async-enumerable sequence containing the elements of the <paramref name="source"/> sequence or the result of the <paramref name="defaultValueRetriever"/> if the <paramref name="source"/> sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="source">The sequence to return a default value for if it is empty.</param>
        /// <param name="defaultValueRetriever">An async function to retrieve the default value to return if the sequence is empty.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements of the <paramref name="source"/> sequence or the result of the <paramref name="defaultValueRetriever"/> if the <paramref name="source"/> sequence is empty.</returns>
        public static AsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this AsyncEnumerable<TSource> source, Func<Promise<TSource>> defaultValueRetriever)
        {
            ValidateArgument(defaultValueRetriever, nameof(defaultValueRetriever), 1);

            return DefaultIfEmptyCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(defaultValueRetriever));
        }

        /// <summary>
        /// Returns an async-enumerable sequence containing the elements of the <paramref name="source"/> sequence or the result of the <paramref name="defaultValueRetriever"/> if the <paramref name="source"/> sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TCapture">The type of the <paramref name="captureValue"/>.</typeparam>
        /// <param name="source">The sequence to return a default value for if it is empty.</param>
        /// <param name="captureValue">The extra value that will be passed to <paramref name="defaultValueRetriever"/>.</param>
        /// <param name="defaultValueRetriever">An async function to retrieve the default value to return if the sequence is empty.</param>
        /// <returns>An <see cref="AsyncEnumerable{T}"/> that contains the elements of the <paramref name="source"/> sequence or the result of the <paramref name="defaultValueRetriever"/> if the <paramref name="source"/> sequence is empty.</returns>
        public static AsyncEnumerable<TSource> DefaultIfEmpty<TSource, TCapture>(this AsyncEnumerable<TSource> source, TCapture captureValue, Func<TCapture, Promise<TSource>> defaultValueRetriever)
        {
            ValidateArgument(defaultValueRetriever, nameof(defaultValueRetriever), 1);

            return DefaultIfEmptyCore(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, defaultValueRetriever));
        }

        private static AsyncEnumerable<TSource> DefaultIfEmptyCore<TSource, TValueRetriever>(AsyncEnumerator<TSource> asyncEnumerator, TValueRetriever defaultValueRetriever)
            where TValueRetriever : Internal.IFunc<Promise<TSource>>
            => AsyncEnumerable<TSource>.Create((asyncEnumerator, defaultValueRetriever), async (cv, writer, cancelationToken) =>
            {
                // The enumerator was retrieved without a cancelation token when the original function was called.
                // We need to propagate the token that was passed in, so we assign it before starting iteration.
                cv.asyncEnumerator._target._cancelationToken = cancelationToken;
                try
                {
                    if (!await cv.asyncEnumerator.MoveNextAsync())
                    {
                        await writer.YieldAsync(await cv.defaultValueRetriever.Invoke());
                        return;
                    }
                    do
                    {
                        await writer.YieldAsync(cv.asyncEnumerator.Current);
                    } while (await cv.asyncEnumerator.MoveNextAsync());
                }
                finally
                {
                    await cv.asyncEnumerator.DisposeAsync();
                }
            });
    }
#endif // CSHARP_7_3_OR_NEWER
}