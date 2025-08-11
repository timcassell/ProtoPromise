#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises.Linq
{
    partial class AsyncEnumerable
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
                : AsyncEnumerable<int>.Create((start, unchecked(start + count)), async (cv, writer, cancelationToken) =>
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
                : new AsyncEnumerable<int>(Internal.AsyncEnumerableRange.GetOrCreate(start, count));
#endif
        }
    }
}

namespace Proto.Promises
{
#if !PROMISE_DEBUG
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
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
            internal static AsyncEnumerableRange GetOrCreate(int start, int count)
            {
                var enumerable = GetOrCreate();
                enumerable.Reset();
                unchecked
                {
                    enumerable._end = start + count - 1;
                    // Subtract 1 so that we can implement MoveNextAsync branchlessly.
                    enumerable._current = start - 1;
                }
                // We don't actually use this type as a backing reference for Promises, so we need to suppress the UnobservedPromiseException from the base finalizer.
                enumerable.WasAwaitedOrForgotten = true;
                return enumerable;
            }

            internal override Promise<bool> MoveNextAsync(int id)
            {
                unchecked
                {
                    // Make sure when this returns false, subsequent calls will also return false.
                    bool hasValue = _current != _end;
                    // JIT optimizes this to be branchless.
                    int increment = hasValue ? 1 : 0;
                    _current += increment;
                    return Promise.Resolved(hasValue);
                }
            }

            internal override Promise DisposeAsync(int id)
            {
                if (Interlocked.CompareExchange(ref _enumerableId, id + 1, id) == id)
                {
                    // This was not already disposed.
                    Dispose();
                }
                // IAsyncDisposable.DisposeAsync must not throw if it's called multiple times, according to MSDN documentation.
                return Promise.Resolved();
            }

            new private void Dispose()
            {
                PrepareEarlyDispose();
                base.Dispose();
                _enumerableDisposed = true;
                ObjectPool.MaybeRepool(this);
            }

            internal override void MaybeDispose() { throw new System.InvalidOperationException(); }
        }
    }
#endif // !PROMISE_DEBUG
}