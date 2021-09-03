#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if CSHARP_7_3_OR_NEWER
using System.Threading.Tasks;
#endif

namespace Proto.Promises
{
    [System.Diagnostics.DebuggerNonUserCode]
    public static class Extensions
    {
#if CSHARP_7_3_OR_NEWER
        /// <summary>
        /// Convert the <see cref="Promise"/> to a <see cref="Task"/>.
        /// </summary>
        public static async Task ToTask(this Promise promise)
        {
            await promise;
        }

        /// <summary>
        /// Convert the <see cref="Promise{T}"/> to a <see cref="Task{T}"/>.
        /// </summary>
        public static async Task<T> ToTask<T>(this Promise<T> promise)
        {
            return await promise;
        }

        /// <summary>
        /// Convert the <see cref="Task"/> to a <see cref="Promise"/>.
        /// </summary>
        public static async Promise ToPromise(this Task task)
        {
            await task;
        }

        /// <summary>
        /// Convert the <see cref="Task{T}"/> to a <see cref="Promise{T}"/>.
        /// </summary>
        public static async Promise<T> ToPromise<T>(this Task<T> task)
        {
            return await task;
        }
#endif
    }
}
