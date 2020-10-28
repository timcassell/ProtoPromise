#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter

#if CSHARP_7_OR_LATER
using System.Threading.Tasks;
#endif

namespace Proto.Promises
{
    [System.Diagnostics.DebuggerNonUserCode]
    public static partial class Extensions
    {
#if CSHARP_7_OR_LATER
        /// <summary>
        /// Convert the <see cref="Promise"/> to a <see cref="Task"/>.
        /// </summary>
        public static async Task ToTask(this Promise promise)
        {
            ValidateThreadAccess(1);

            await promise;
        }

        /// <summary>
        /// Convert the <see cref="Promise{T}"/> to a <see cref="Task{T}"/>.
        /// </summary>
        public static async Task<T> ToTask<T>(this Promise<T> promise)
        {
            ValidateThreadAccess(1);

            return await promise;
        }

        /// <summary>
        /// Convert the <see cref="Task"/> to a <see cref="Promise"/>.
        /// </summary>
        public static async Promise ToPromise(this Task task)
        {
            // No thread safety in the Promise library yet, so try to force continuation on main thread.
            // User must call this method from the main thread in order for this to work.
            ValidateThreadAccess(1);
            await task.ConfigureAwait(true);
        }

        /// <summary>
        /// Convert the <see cref="Task{T}"/> to a <see cref="Promise{T}"/>.
        /// </summary>
        public static async Promise<T> ToPromise<T>(this Task<T> task)
        {
            // No thread safety in the Promise library yet, so try to force continuation on main thread.
            // User must call this method from the main thread in order for this to work.
            ValidateThreadAccess(1);
            return await task.ConfigureAwait(true);
        }
#endif

        // Calls to this get compiled away in RELEASE mode
        static partial void ValidateThreadAccess(int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateThreadAccess(int skipFrames)
        {
            Internal.ValidateThreadAccess(skipFrames + 1);
        }
#endif
    }
}
