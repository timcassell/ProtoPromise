#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Promise manager. This can be used to clear pooled objects (if enabled).
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public static class Manager
        {
            [Obsolete("Promise.Manager.HandleCompletes is no longer valid. Set Promise.Config.ForegroundContext instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public static void HandleCompletes()
            {
                throw new System.InvalidOperationException("Promise.Manager.HandleCompletes is no longer valid. Set Promise.Config.ForegroundContext instead.");
            }

            [Obsolete("Promise.Manager.HandleCompletesAndProgress is no longer valid. Set Promise.Config.ForegroundContext instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public static void HandleCompletesAndProgress()
            {
                throw new System.InvalidOperationException("Promise.Manager.HandleCompletesAndProgress is no longer valid. Set Promise.Config.ForegroundContext instead.");
            }

            [Obsolete("Promise.Manager.HandleProgress is no longer valid. Set Promise.Config.ForegroundContext instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
            public static void HandleProgress()
            {
                throw new System.InvalidOperationException("Promise.Manager.HandleProgress is no longer valid. Set Promise.Config.ForegroundContext instead.");
            }

            /// <summary>
            /// Clears all currently pooled objects. Does not affect pending or preserved promises.
            /// </summary>
            public static void ClearObjectPool()
            {
                Internal.ClearPool();
            }

            [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
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