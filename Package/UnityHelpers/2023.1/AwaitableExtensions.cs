#if UNITY_2023_1_OR_NEWER

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;
using UnityEngine;

namespace Proto.Promises
{
    /// <summary>
    /// Extensions to convert Awaitables to Promises.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static partial class AwaitableExtensions
    {
        /// <summary>
        /// Converts the <paramref name="awaitable"/> to a <see cref="Promise"/>.
        /// </summary>
        public static Promise ToPromise(this Awaitable awaitable)
        {
            ValidateArgument(awaitable, nameof(awaitable), 1);

            return Core(awaitable);

            static async Promise Core(Awaitable a)
            {
                await a;
            }
        }

        /// <summary>
        /// Converts the <paramref name="awaitable"/> to a <see cref="Promise{T}"/>.
        /// </summary>
        public static Promise<T> ToPromise<T>(this Awaitable<T> awaitable)
        {
            ValidateArgument(awaitable, nameof(awaitable), 1);

            return Core(awaitable);

            static async Promise<T> Core(Awaitable<T> a)
            {
                return await a;
            }
        }

        /// <summary>
        /// Converts the <paramref name="awaitable"/> to a <see cref="Promise"/>, using the <paramref name="cancelationToken"/> to cancel the <paramref name="awaitable"/>.
        /// </summary>
        public static Promise ToPromise(this Awaitable awaitable, CancelationToken cancelationToken)
        {
            ValidateArgument(awaitable, nameof(awaitable), 1);

            return Core(awaitable, cancelationToken);

            static async Promise Core(Awaitable a, CancelationToken t)
            {
                using var _ = t.Register(a, static aw => aw.Cancel());
                await a;
            }
        }

        /// <summary>
        /// Converts the <paramref name="awaitable"/> to a <see cref="Promise{T}"/>, using the <paramref name="cancelationToken"/> to cancel the <paramref name="awaitable"/>.
        /// </summary>
        public static Promise<T> ToPromise<T>(this Awaitable<T> awaitable, CancelationToken cancelationToken)
        {
            ValidateArgument(awaitable, nameof(awaitable), 1);

            return Core(awaitable, cancelationToken);

            static async Promise<T> Core(Awaitable<T> a, CancelationToken t)
            {
                using var _ = t.Register(a, static aw => aw.Cancel());
                return await a;
            }
        }

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif
    }
}

#endif // UNITY_2023_1_OR_NEWER