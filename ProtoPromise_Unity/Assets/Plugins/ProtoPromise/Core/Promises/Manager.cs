#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
# endif

using System;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Promise manager. This can be used to cleared pooled objects (if enabled) or manually handle promises (not recommended for RELEASE builds).
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public static class Manager
        {
            /// <summary>
            /// Invokes callbacks for completed promises.
            /// </summary>
            public static void HandleCompletes()
            {
                Internal.HandleEvents();
                Internal.MaybeReportUnhandledRejections();
            }

            /// <summary>
            /// Invokes callbacks for completed promises,
            /// then invokes progress callbacks for all promises that had their progress updated.
            /// </summary>
            public static void HandleCompletesAndProgress()
            {
                Internal.HandleEvents();
#if PROMISE_PROGRESS
                Internal.PromiseRef.InvokeProgressListeners();
#endif
                Internal.MaybeReportUnhandledRejections();
            }

            /// <summary>
            /// Invokes progress callbacks for all promises that had their progress updated.
            /// </summary>
            public static void HandleProgress()
            {
#if PROMISE_PROGRESS
                Internal.PromiseRef.InvokeProgressListeners();
#endif
                Internal.MaybeReportUnhandledRejections();
            }

            /// <summary>
            /// Clears all currently pooled objects. Does not affect pending or preserved promises.
            /// </summary>
            public static void ClearObjectPool()
            {
                Internal.ClearPool();
            }

            /// <summary>
            /// Sends the message to <see cref="Config.WarningHandler"/> if it exists.
            /// </summary>
            public static void LogWarning(string message)
            {
                var temp = Config.WarningHandler;
                if (temp != null)
                {
                    temp.Invoke(message);
                }
            }
        }
    }
}