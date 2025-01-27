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
        /// Generates an async-enumerable sequence that contains a single element.
        /// </summary>
        /// <typeparam name="T">The type of the element that will be returned in the generated sequence.</typeparam>
        /// <param name="value">Single element in the resulting async-enumerable sequence.</param>
        /// <returns>An async-enumerable sequence that contains a range of sequential integral numbers.</returns>
        public static AsyncEnumerable<T> Return<T>(T value)
            => AsyncEnumerable<T>.Return(value);
    }

    partial struct AsyncEnumerable<T>
    {
        /// <summary>
        /// Generates an async-enumerable sequence that contains a single element.
        /// </summary>
        /// <param name="value">Single element in the resulting async-enumerable sequence.</param>
        /// <returns>An async-enumerable sequence that contains a range of sequential integral numbers.</returns>
        public static AsyncEnumerable<T> Return(T value)
        {
#if PROMISE_DEBUG
            // In DEBUG mode we use the Create function so its proper use will be verified.
            return Create(value, async (cv, writer, cancelationToken) => await writer.YieldAsync(cv));
#else
            return new AsyncEnumerable<T>(Internal.AsyncEnumerableReturn<T>.GetOrCreate(value));
#endif
        }
    }
}

namespace Proto.Promises
{
#if !PROMISE_DEBUG
    partial class Internal
    {
        internal sealed class AsyncEnumerableReturn<T> : PromiseRefBase.AsyncEnumerableBase<T>
        {
            private AsyncEnumerableReturn() { }

            [MethodImpl(InlineOption)]
            private static AsyncEnumerableReturn<T> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AsyncEnumerableReturn<T>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new AsyncEnumerableReturn<T>()
                    : obj.UnsafeAs<AsyncEnumerableReturn<T>>();
            }

            [MethodImpl(InlineOption)]
            internal static AsyncEnumerableReturn<T> GetOrCreate(T value)
            {
                var enumerable = GetOrCreate();
                enumerable.Reset();
                enumerable._current = value;
                // We don't actually use this type as a backing reference for Promises, so we need to suppress the UnobservedPromiseException from the base finalizer.
                enumerable.WasAwaitedOrForgotten = true;
                return enumerable;
            }

            internal override Promise<bool> MoveNextAsync(int id)
            {
                bool isStarted = _isStarted;
                _isStarted = true;
                return Promise.Resolved(!isStarted);
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
                ClearReferences(ref _current);
                _enumerableDisposed = true;
                ObjectPool.MaybeRepool(this);
            }

            internal override void MaybeDispose() { throw new System.InvalidOperationException(); }
        }
    }
#endif // !PROMISE_DEBUG
}