#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER // We only expose AsyncEnumerable where custom async method builders are supported.
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Generates an async-enumerable sequence that will be immediately rejected with the provided reason when iterated.
        /// </summary>
        /// <typeparam name="T">The type used for the <see cref="AsyncEnumerable{T}"/> type parameter of the resulting sequence.</typeparam>
        /// <typeparam name="TReject">The type of the reject reason.</typeparam>
        /// <param name="reason">The reason that will be used to reject the async-enumerable sequence.</param>
        /// <returns>An async-enumerable sequence that will be immediately rejected when iterated.</returns>
        public static AsyncEnumerable<T> Rejected<T, TReject>(TReject reason)
            => AsyncEnumerable<T>.Rejected(reason);
    }

    partial struct AsyncEnumerable<T>
    {
        /// <summary>
        /// Generates an async-enumerable sequence that will be immediately rejected when iterated.
        /// </summary>
        /// <typeparam name="TReject">The type of the reject reason.</typeparam>
        /// <param name="reason">The reason that will be used to reject the async-enumerable sequence.</param>
        /// <returns>An async-enumerable sequence that will be immediately rejected when iterated.</returns>
        public static AsyncEnumerable<T> Rejected<TReject>(TReject reason)
        {
            // We always use the Create function, even in RELEASE mode, because the performance of Reject is unimportant.
            return Create(reason, async (r, writer, cancelationToken) => await Promise.Rejected(r));
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}