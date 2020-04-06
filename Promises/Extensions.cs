#if CSHARP_7_OR_LATER
using System.Threading.Tasks;
using Proto.Promises.Await;
#endif

namespace Proto.Promises
{
    public static class Extensions
    {
#if CSHARP_7_OR_LATER
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
        /// <para/>NOTE: This must be called from the UI/main thread. If called from a different thread, Promise states could get corrupted from a race condition.
        /// </summary>
        public static async Promise ToPromise(this Task task)
        {
            // No thread safety in the Promise library yet, so try to force continuation on main thread.
            // User must call this method from the main thread in order for this to work.
            await task.ConfigureAwait(true);
        }

        /// <summary>
        /// Convert the <see cref="Task{T}"/> to a <see cref="Promise{T}"/>.
        /// <para/>NOTE: This must be called from the UI/main thread. If called from a different thread, Promise states could get corrupted from a race condition.
        /// </summary>
        public static async Promise<T> ToPromise<T>(this Task<T> task)
        {
            // No thread safety in the Promise library yet, so try to force continuation on main thread.
            // User must call this method from the main thread in order for this to work.
            return await task.ConfigureAwait(true);
        }
#endif
    }
}
