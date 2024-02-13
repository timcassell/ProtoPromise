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
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Generates an async-enumerable sequence that will be immediately canceled when iterated.
        /// </summary>
        /// <typeparam name="T">The type used for the <see cref="AsyncEnumerable{T}"/> type parameter of the resulting sequence.</typeparam>
        /// <returns>An async-enumerable sequence that will be immediately canceled when iterated.</returns>
        public static AsyncEnumerable<T> Canceled<T>()
            => AsyncEnumerable<T>.Canceled();
    }

    partial struct AsyncEnumerable<T>
    {
        /// <summary>
        /// Generates an async-enumerable sequence that will be immediately canceled when iterated.
        /// </summary>
        /// <returns>An async-enumerable sequence that will be immediately canceled when iterated.</returns>
        public static AsyncEnumerable<T> Canceled()
        {
#if PROMISE_DEBUG
            // In DEBUG mode we use the Create function so its proper use will be verified.
            return Create(async (writer, cancelationToken) => await Promise.Canceled());
#else
            // In RELEASE mode we use the sentinel object for efficiency.
            return new AsyncEnumerable<T>(Internal.AsyncEnumerableCanceledSentinel<T>.s_instance);
#endif
        }
    }
}

namespace Proto.Promises
{
#if !PROMISE_DEBUG
    partial class Internal
    {
        internal sealed class AsyncEnumerableCanceledSentinel<T> : PromiseRefBase.AsyncEnumerableBase<T>
        {
            internal static readonly AsyncEnumerableCanceledSentinel<T> s_instance;

            static AsyncEnumerableCanceledSentinel()
            {
                s_instance = new AsyncEnumerableCanceledSentinel<T>();
                Discard(s_instance);
            }

            private AsyncEnumerableCanceledSentinel() { }

            public override Linq.AsyncEnumerator<T> GetAsyncEnumerator(int id, CancelationToken cancelationToken)
                // Don't do any validation, ignore the cancelationToken, just return the enumerator.
                => new Linq.AsyncEnumerator<T>(this, id);

            internal override Promise<bool> MoveNextAsync(int id)
                // Always return a canceled promise.
                => Promise<bool>.Canceled();

            internal override Promise DisposeAsync(int id)
                // Do nothing, just return a resolved promise.
                => Promise.Resolved();

            public override Linq.AsyncEnumerable<T> GetSelfWithIncrementedId(int id)
                => new Linq.AsyncEnumerable<T>(this, id);

            internal override void MaybeDispose() { throw new System.InvalidOperationException(); }
        }
    }
#endif // !PROMISE_DEBUG
}