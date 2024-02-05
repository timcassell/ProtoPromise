#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using Proto.Promises.Linq;
using System.Diagnostics;

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
        /// Generates an async-enumerable sequence that will be immediately rejected with the provided reason when iterated.
        /// </summary>
        /// <typeparam name="TReject">The type of the reject reason.</typeparam>
        /// <param name="reason">The reason that will be used to reject the async-enumerable sequence.</param>
        /// <returns>An async-enumerable sequence that will be immediately rejected when iterated.</returns>
        public static AsyncEnumerable<T> Rejected<TReject>(TReject reason)
            => Create(new Internal.RejectedIterator<T, TReject>(reason));
    }
#endif // CSHARP_7_3_OR_NEWER
}

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
        // Special iterator to reject even if the enumerator is disposed without starting.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct RejectedIterator<TSource, TReject> : IAsyncIterator<TSource>
        {
            private readonly TReject _reason;

            internal RejectedIterator(TReject reason)
                => _reason = reason;

            public Promise DisposeAsyncWithoutStart()
                => Promise.Rejected(_reason);

            // We're only using this to reject. No elements will be yielded, so we don't need an async state machine.
            public AsyncIteratorMethod Start(AsyncStreamWriter<TSource> streamWriter, CancelationToken cancelationToken)
                => new AsyncIteratorMethod(Promise.Rejected(_reason));
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}