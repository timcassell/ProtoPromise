#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Threading.Tasks;

using System.Diagnostics;

namespace Proto.Promises
{
    /// <summary>
    /// Helpful extensions to convert promises to and from other asynchronous types.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static partial class Extensions
    {
        /// <summary>
        /// Convert the <paramref name="promise"/> to a <see cref="Task"/>.
        /// </summary>
        public static async Task ToTask(this Promise promise)
        {
            await promise;
        }

        /// <summary>
        /// Convert the <paramref name="promise"/> to a <see cref="Task{T}"/>.
        /// </summary>
        public static async Task<T> ToTask<T>(this Promise<T> promise)
        {
            return await promise;
        }

        /// <summary>
        /// Convert the <paramref name="task"/> to a <see cref="Promise"/>.
        /// </summary>
        public static async Promise ToPromise(this Task task)
        {
            await task;
        }

        /// <summary>
        /// Convert the <paramref name="task"/> to a <see cref="Promise{T}"/>.
        /// </summary>
        public static async Promise<T> ToPromise<T>(this Task<T> task)
        {
            return await task;
        }

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
        /// <summary>
        /// Convert the <paramref name="task"/> to a <see cref="Promise"/>.
        /// </summary>
        public static async Promise ToPromise(this ValueTask task)
        {
            await task;
        }

        /// <summary>
        /// Convert the <paramref name="task"/> to a <see cref="Promise{T}"/>.
        /// </summary>
        public static async Promise<T> ToPromise<T>(this ValueTask<T> task)
        {
            return await task;
        }
#endif
    }
}