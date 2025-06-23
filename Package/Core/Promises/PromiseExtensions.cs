#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;

namespace Proto.Promises
{
    /// <summary>
    /// Provides extension methods for <see cref="Promise"/> and <see cref="Promise{T}"/>.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static class PromiseExtensions
    {
        // AppendResult are extension methods instead of instance methods in case users want to add their own AppendResult extensions with ValueTuple arity greater than 2.
        // Instance methods would take precedence, causing results to be ((T1, T2), T3) instead of (T1, T2, T3).

        /// <summary>
        /// Appends a result to the specified <paramref name="promise"/>, transforming it into a <see cref="Promise{T}"/>
        /// that will yield the <paramref name="value"/> when the <paramref name="promise"/> is resolved.
        /// </summary>
        /// <typeparam name="TAppend">The type of the <paramref name="value"/> to append to the <paramref name="promise"/>.</typeparam>
        /// <param name="promise">The <see cref="Promise"/> to which the result will be appended.</param>
        /// <param name="value">The value that will be yielded from the returned <see cref="Promise{T}"/> when the <paramref name="promise"/> is resolved.</param>
        /// <returns>
        /// A new <see cref="Promise{T}"/> that will be resolved with the specified <paramref name="value"/>
        /// when the <paramref name="promise"/> is resolved, or otherwise adopts the state of <paramref name="promise"/>.
        /// </returns>
        public static Promise<TAppend> AppendResult<TAppend>(this Promise promise, TAppend value)
            => Internal.PromiseRefBase.CallbackHelperResult<TAppend>.AppendResult(promise, value);

        /// <summary>
        /// Appends <paramref name="value"/> to the result of the specified <paramref name="promise"/>, transforming it into a <see cref="Promise{T}"/>
        /// of <see cref="ValueTuple{T, TAppend}"/> that will yield both the original result and the appended <paramref name="value"/> when the <paramref name="promise"/> is resolved.
        /// </summary>
        /// <typeparam name="T">The type of the result of <paramref name="promise"/>.</typeparam>
        /// <typeparam name="TAppend">The type of the <paramref name="value"/> to append to the result of <paramref name="promise"/>.</typeparam>
        /// <param name="promise">The <see cref="Promise"/> to which the result will be appended.</param>
        /// <param name="value">The value that will appended to the result of the <paramref name="promise"/>.</param>
        /// <returns>
        /// A new <see cref="Promise{T}"/> of <see cref="ValueTuple{T, TAppend}"/> that will yield both the original result and the appended
        /// <paramref name="value"/> when the <paramref name="promise"/> is resolved, or otherwise adopts the state of <paramref name="promise"/>.
        /// </returns>
        public static Promise<(T, TAppend)> AppendResult<T, TAppend>(this in Promise<T> promise, TAppend value)
            => Internal.PromiseRefBase.CallbackHelperResult<T>.AppendResult(promise, value);
    } // class PromiseExtensions
}// namespace Proto.Promises