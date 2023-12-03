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
        /// Generates an async-enumerable sequence of integral numbers within a specified range.
        /// </summary>
        /// <param name="start">The value of the first integer in the sequence.</param>
        /// <param name="count">The number of sequential integers to generate.</param>
        /// <returns>An async-enumerable sequence that contains a range of sequential integral numbers.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero. -or- <paramref name="start"/> + <paramref name="count"/> - 1 is larger than <see cref="int.MaxValue"/>.</exception>
        public static AsyncEnumerable<int> Range(int start, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count is less than zero", Internal.GetFormattedStacktrace(1));
            }

            var end = (long) start + count - 1L;
            if (end > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "start + count - 1 is larger than int.MaxValue", Internal.GetFormattedStacktrace(1));
            }

            return count == 0
                ? Empty<int>()
#if PROMISE_DEBUG
                // In DEBUG mode we use the Create function so its proper use will be verified.
                : AsyncEnumerable<int>.Create((start, start + count), async (cv, writer, cancelationToken) =>
                {
                    while (cv.start != cv.Item2)
                    {
                        await writer.YieldAsync(cv.start);
                        unchecked
                        {
                            ++cv.start;
                        }
                    }
                });
#else
                : new AsyncEnumerable<int>(Internal.AsyncEnumerableRange.GetOrCreate(start, start + count));
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
        internal sealed class AsyncEnumerableRange : PromiseRefBase.AsyncEnumerableBase<int>
        {
            private int _end;

            private AsyncEnumerableRange() { }

            [MethodImpl(InlineOption)]
            private static AsyncEnumerableRange GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncEnumerableRange>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncEnumerableRange()
                    : obj.UnsafeAs<AsyncEnumerableRange>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncEnumerableRange GetOrCreate(int start, int end)
            {
                var enumerable = GetOrCreate();
                enumerable.Reset();
                enumerable._end = end;
                unchecked
                {
                    // Subtract 1 so that we can implement MoveNextAsync branchlessly.
                    enumerable._current = start - 1;
                }
                return enumerable;
            }

            internal override Promise<bool> MoveNextAsync(int id)
            {
                unchecked
                {
                    return Promise.Resolved(++_current != _end);
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
                ObjectPool.MaybeRepool(this);
            }

            protected override void Start(int enumerableId) { throw new System.InvalidOperationException(); }
            internal override void MaybeDispose() { throw new System.InvalidOperationException(); }
        }
    }
#endif // CSHARP_7_3_OR_NEWER && !PROMISE_DEBUG
}