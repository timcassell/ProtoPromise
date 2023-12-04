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
    public static partial class AsyncEnumerable
    {
        /// <summary>
        /// Generates an async-enumerable sequence that contains one repeated value.
        /// </summary>
        /// <typeparam name="T">The type of the value to be repeated in the result async-enumerable sequence.</typeparam>
        /// <param name="element">The value to be repeated.</param>
        /// <param name="count">Number of times to repeat the element.</param>
        /// <returns>An async-enumerable sequence that repeats the given element the specified number of times.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero.</exception>
        public static AsyncEnumerable<T> Repeat<T>(T element, int count)
            => AsyncEnumerable<T>.Repeat(element, count);
    }

    partial struct AsyncEnumerable<T>
    {
        /// <summary>
        /// Generates an async-enumerable sequence that contains one repeated value.
        /// </summary>
        /// <param name="element">The value to be repeated.</param>
        /// <param name="count">Number of times to repeat the element.</param>
        /// <returns>An async-enumerable sequence that repeats the given element the specified number of times.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero.</exception>
        public static AsyncEnumerable<T> Repeat(T element, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count is less than zero", Internal.GetFormattedStacktrace(1));
            }

            return count == 0
                ? Empty()
#if PROMISE_DEBUG
                // In DEBUG mode we use the Create function so its proper use will be verified.
                : Create((element, count), async (cv, writer, cancelationToken) =>
                {
                    unchecked
                    {
                        while (--cv.count >= 0)
                        {
                            await writer.YieldAsync(cv.element);
                        }
                    }
                });
#else
                : new AsyncEnumerable<T>(Internal.AsyncEnumerableRepeat<T>.GetOrCreate(element, count));
#endif
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER && !PROMISE_DEBUG
    partial class Internal
    {
        internal sealed class AsyncEnumerableRepeat<T> : PromiseRefBase.AsyncEnumerableBase<T>
        {
            private int _count;

            private AsyncEnumerableRepeat() { }

            [MethodImpl(InlineOption)]
            private static AsyncEnumerableRepeat<T> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncEnumerableRepeat<T>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncEnumerableRepeat<T>()
                    : obj.UnsafeAs<AsyncEnumerableRepeat<T>>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncEnumerableRepeat<T> GetOrCreate(T element, int count)
            {
                var enumerable = GetOrCreate();
                enumerable.Reset();
                enumerable._count = count;
                enumerable._current = element;
                return enumerable;
            }

            internal override Promise<bool> MoveNextAsync(int id)
            {
                unchecked
                {
                    return Promise.Resolved(--_count >= 0);
                }
            }

            internal override Promise DisposeAsync(int id)
            {
                if (Interlocked.CompareExchange(ref _enumerableId, id + 1, id) == id)
                {
                    // This was not already disposed.
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    SetCompletionState(null, Promise.State.Resolved);
#endif
                    DisposeAndReturnToPool();
                }
                // IAsyncDisposable.DisposeAsync must not throw if it's called multiple times, according to MSDN documentation.
                return Promise.Resolved();
            }

            protected override void DisposeAndReturnToPool()
            {
                Dispose();
                _current = default;
                _disposed = true;
                ObjectPool.MaybeRepool(this);
            }

            protected override void Start(int enumerableId) { throw new System.InvalidOperationException(); }
            internal override void MaybeDispose() { throw new System.InvalidOperationException(); }
        }
    }
#endif // CSHARP_7_3_OR_NEWER && !PROMISE_DEBUG
}