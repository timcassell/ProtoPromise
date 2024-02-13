#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;

namespace Proto.Promises.Linq
{
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
            => DefaultIfEmptyHelper.DefaultIfEmpty(source.GetAsyncEnumerator(), defaultValue);

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

            return DefaultIfEmptyHelper.DefaultIfEmpty(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(defaultValueRetriever));
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

            return DefaultIfEmptyHelper.DefaultIfEmpty(source.GetAsyncEnumerator(), Internal.PromiseRefBase.DelegateWrapper.Create(captureValue, defaultValueRetriever));
        }

        private static class DefaultIfEmptyHelper
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct DefaultIfEmptyIterator<TSource> : IAsyncIterator<TSource>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TSource _defaultValue;

                internal DefaultIfEmptyIterator(AsyncEnumerator<TSource> asyncEnumerator, TSource defaultValue)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _defaultValue = defaultValue;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(_defaultValue);
                            return;
                        }
                        do
                        {
                            await writer.YieldAsync(_asyncEnumerator.Current);
                        } while (await _asyncEnumerator.MoveNextAsync());
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> DefaultIfEmpty<TSource>(AsyncEnumerator<TSource> asyncEnumerator, TSource defaultValue)
            {
                return AsyncEnumerable<TSource>.Create(new DefaultIfEmptyIterator<TSource>(asyncEnumerator, defaultValue));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct DefaultIfEmptyIterator<TSource, TValueRetriever> : IAsyncIterator<TSource>
                where TValueRetriever : Internal.IFunc<Promise<TSource>>
            {
                private readonly AsyncEnumerator<TSource> _asyncEnumerator;
                private readonly TValueRetriever _defaultValueRetriever;

                internal DefaultIfEmptyIterator(AsyncEnumerator<TSource> asyncEnumerator, TValueRetriever defaultValueRetriever)
                {
                    _asyncEnumerator = asyncEnumerator;
                    _defaultValueRetriever = defaultValueRetriever;
                }

                public async AsyncIteratorMethod Start(AsyncStreamWriter<TSource> writer, CancelationToken cancelationToken)
                {
                    // The enumerator was retrieved without a cancelation token when the original function was called.
                    // We need to propagate the token that was passed in, so we assign it before starting iteration.
                    _asyncEnumerator._target._cancelationToken = cancelationToken;
                    try
                    {
                        if (!await _asyncEnumerator.MoveNextAsync())
                        {
                            await writer.YieldAsync(await _defaultValueRetriever.Invoke());
                            return;
                        }
                        do
                        {
                            await writer.YieldAsync(_asyncEnumerator.Current);
                        } while (await _asyncEnumerator.MoveNextAsync());
                    }
                    finally
                    {
                        await _asyncEnumerator.DisposeAsync();
                    }
                }

                public Promise DisposeAsyncWithoutStart()
                    => _asyncEnumerator.DisposeAsync();
            }

            internal static AsyncEnumerable<TSource> DefaultIfEmpty<TSource, TValueRetriever>(AsyncEnumerator<TSource> asyncEnumerator, TValueRetriever defaultValueRetriever)
                where TValueRetriever : Internal.IFunc<Promise<TSource>>
            {
                return AsyncEnumerable<TSource>.Create(new DefaultIfEmptyIterator<TSource, TValueRetriever>(asyncEnumerator, defaultValueRetriever));
            }
        }
    }
}