using System.Collections.Generic;
using System.Diagnostics;

namespace Proto.Promises.Linq
{
#if NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
    /// <summary>
    /// Provides extension methods for <see cref="AsyncEnumerable{T}"/>.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static class AsyncEnumerableExtensions
    {
        /// <summary>
        /// Convert the <see cref="IAsyncEnumerable{T}"/> <paramref name="source"/> to an <see cref="AsyncEnumerable{T}"/>.
        /// </summary>
        public static AsyncEnumerable<T> ToAsyncEnumerable<T>(this IAsyncEnumerable<T> source)
        {
            return AsyncEnumerable<T>.Create(source, async (_source, writer, cancelationToken) =>
            {
                await foreach (T item in System.Threading.Tasks.TaskAsyncEnumerableExtensions.WithCancellation(_source, cancelationToken.ToCancellationToken()).ConfigureAwait(false))
                {
                    await writer.YieldAsync(item);
                }
            });
        }
    }
#endif // NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
}