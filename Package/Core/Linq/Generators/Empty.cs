#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Linq;
using System.Diagnostics;

#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER // We only expose AsyncEnumerable where custom async method builders are supported.
    partial class AsyncEnumerable
    {
        /// <summary>
        /// Returns an empty async-enumerable sequence.
        /// </summary>
        /// <typeparam name="T">The type used for the <see cref="AsyncEnumerable{T}"/> type parameter of the resulting sequence.</typeparam>
        /// <returns>An async-enumerable sequence with no elements.</returns>
        public static AsyncEnumerable<T> Empty<T>()
            => AsyncEnumerable<T>.Empty();
    }

    partial struct AsyncEnumerable<T>
    {
        /// <summary>
        /// Returns an empty async-enumerable sequence.
        /// </summary>
        /// <returns>An async-enumerable sequence with no elements.</returns>
#if PROMISE_DEBUG
        // In DEBUG mode we use the Create function so its proper use will be verified.
        public static AsyncEnumerable<T> Empty()
            => Create(async (writer, cancelationToken) => { });
#else
        // In RELEASE mode we use the sentinel object for efficiency.
        public static AsyncEnumerable<T> Empty()
            => new AsyncEnumerable<T>(Internal.AsyncEnumerableEmptySentinel<T>.s_instance);
#endif
    }
#endif // CSHARP_7_3_OR_NEWER
}

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
#if !PROMISE_DEBUG
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AsyncEnumerableEmptySentinel<T> : PromiseRefBase.AsyncEnumerableBase<T>
        {
            internal static readonly AsyncEnumerableEmptySentinel<T> s_instance;

            static AsyncEnumerableEmptySentinel()
            {
                s_instance = new AsyncEnumerableEmptySentinel<T>();
                Discard(s_instance);
            }

            private AsyncEnumerableEmptySentinel() { }

            public override AsyncEnumerator<T> GetAsyncEnumerator(int id, CancelationToken cancelationToken)
                // Don't do any validation, ignore the cancelationToken, just return the enumerator.
                => new AsyncEnumerator<T>(this, id);

            internal override Promise<bool> MoveNextAsync(int id)
                // It's empty, always return false.
                => Promise.Resolved(false);

            internal override Promise DisposeAsync(int id)
                // Do nothing, just return a resolved promise.
                => Promise.Resolved();

            public override AsyncEnumerable<T> GetSelfWithIncrementedId(int id)
                => new AsyncEnumerable<T>(this, id);

            internal override void MaybeDispose() { throw new System.InvalidOperationException(); }
        }
#endif // !PROMISE_DEBUG

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class EmptyHelper
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private readonly struct EmptyIterator<TSource> : IAsyncIterator<TSource>
            {
                private readonly AsyncEnumerator<TSource> _source;

                internal EmptyIterator(AsyncEnumerator<TSource> source)
                {
                    _source = source;
                }

                public Promise DisposeAsyncWithoutStart()
                    => _source.DisposeAsync();

                // We're only using this to dispose the source. No elements will be yielded, so we don't need an async state machine.
                public AsyncIteratorMethod Start(AsyncStreamWriter<TSource> streamWriter, CancelationToken cancelationToken)
                    => new AsyncIteratorMethod(_source.DisposeAsync());
            }

            internal static AsyncEnumerable<TSource> EmptyWithDispose<TSource>(AsyncEnumerator<TSource> enumerator)
                => AsyncEnumerable<TSource>.Create(new EmptyIterator<TSource>(enumerator));
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}