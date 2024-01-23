#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.CompilerServices;
using System;
using System.Diagnostics;

namespace Proto.Promises.Linq
{
#if CSHARP_7_3_OR_NEWER // We only expose AsyncEnumerable where custom async method builders are supported.
    /// <summary>
    /// Provides helper functions to create and operate on <see cref="AsyncEnumerable{T}"/> async streams.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static partial class AsyncEnumerable
    {
        // We use AsyncEnumerableMethod instead of Promise so it can specially handle early-exits (`break` keyword).

        /// <summary>
        /// Create a new <see cref="AsyncEnumerable{T}"/> async stream from the specified <paramref name="asyncIterator"/> function.
        /// </summary>
        public static AsyncEnumerable<T> Create<T>(Func<AsyncStreamWriter<T>, CancelationToken, AsyncEnumerableMethod> asyncIterator)
            => AsyncEnumerable<T>.Create(asyncIterator);

        /// <summary>
        /// Create a new <see cref="AsyncEnumerable{T}"/> async stream from the specified <paramref name="captureValue"/> and <paramref name="asyncIterator"/> function.
        /// </summary>
        public static AsyncEnumerable<T> Create<T, TCapture>(TCapture captureValue, Func<TCapture, AsyncStreamWriter<T>, CancelationToken, AsyncEnumerableMethod> asyncIterator)
            => AsyncEnumerable<T>.Create(captureValue, asyncIterator);
    }

    partial struct AsyncEnumerable<T>
    {
        /// <summary>
        /// Create a new <see cref="AsyncEnumerable{T}"/> async stream from the specified <paramref name="asyncIterator"/> function.
        /// </summary>
        public static AsyncEnumerable<T> Create(Func<AsyncStreamWriter<T>, CancelationToken, AsyncEnumerableMethod> asyncIterator)
        {
            ValidateArgument(asyncIterator, nameof(asyncIterator), 1);

            var enumerable = Internal.AsyncEnumerableCreate<T, Internal.AsyncIterator<T>>.GetOrCreate(new Internal.AsyncIterator<T>(asyncIterator));
            return new AsyncEnumerable<T>(enumerable);
        }

        /// <summary>
        /// Create a new <see cref="AsyncEnumerable{T}"/> async stream from the specified <paramref name="captureValue"/> and <paramref name="asyncIterator"/> function.
        /// </summary>
        public static AsyncEnumerable<T> Create<TCapture>(TCapture captureValue, Func<TCapture, AsyncStreamWriter<T>, CancelationToken, AsyncEnumerableMethod> asyncIterator)
        {
            ValidateArgument(asyncIterator, nameof(asyncIterator), 1);

            var enumerable = Internal.AsyncEnumerableCreate<T, Internal.AsyncIterator<T, TCapture>>.GetOrCreate(new Internal.AsyncIterator<T, TCapture>(captureValue, asyncIterator));
            return new AsyncEnumerable<T>(enumerable);
        }
    }

    partial struct AsyncEnumerable<T>
    {
        // Calls to this get compiled away in RELEASE mode
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);

#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif
    }
#endif // CSHARP_7_3_OR_NEWER
}