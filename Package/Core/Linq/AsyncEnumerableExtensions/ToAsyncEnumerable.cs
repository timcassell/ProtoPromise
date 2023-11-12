using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Proto.Promises.Linq
{
#if NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
    public static partial class AsyncEnumerableExtensions
    {
        /// <summary>
        /// Convert the <see cref="IAsyncEnumerable{T}"/> <paramref name="source"/> to an <see cref="AsyncEnumerable{T}"/>.
        /// </summary>
        public static AsyncEnumerable<T> ToAsyncEnumerable<T>(this IAsyncEnumerable<T> source)
        {
            ValidateArgument(source, nameof(source), 1);

            return AsyncEnumerable<T>.Create(source, async (_source, writer, cancelationToken) =>
            {
                // await foreach is only available in C#8, so we have to iterate manually.
                var asyncEnumerator = _source.GetAsyncEnumerator(cancelationToken.ToCancellationToken());
                try
                {
                    while (await asyncEnumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        await writer.YieldAsync(asyncEnumerator.Current);
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync().ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// Returns <paramref name="source"/>.
        /// </summary>
        // This is used to avoid an expensive boxing and wrapping if ToAsyncEnumerable is accidentally used on a raw AsyncEnumerable<T> instance.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(Internal.InlineOption)]
        public static AsyncEnumerable<T> ToAsyncEnumerable<T>(this AsyncEnumerable<T> source) => source;
    }
#endif // NET47_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
}