using System;

namespace Proto.Promises
{
    partial class Promise
    {
        /// <summary>
        /// Promise manager. This can be used to cleared pooled objects (if enabled) or manually handle promises (not recommended for RELEASE builds).
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public static class Manager
        {
            private static bool _willThrow;

            /// <summary>
            /// Invokes callbacks for completed promises,
            /// then if <see cref="Config.UncaughtRejectionHandler"/> is not null, invokes it with each unhandled rejection,
            /// otherwise throws all unhandled rejections as <see cref="AggregateException"/>.
            /// <para/>Does nothing if completes are already being handled.
            /// </summary>
            public static void HandleCompletes()
            {
                ValidateThreadAccess(1, false);

                bool willThrow = _willThrow;
                _willThrow = true;

                Internal.HandleEvents();

                if (!willThrow)
                {
                    _willThrow = false;
                    Internal.ThrowUnhandledRejections();
                }
            }

            /// <summary>
            /// Invokes callbacks for completed promises,
            /// then invokes progress callbacks for all promises that had their progress updated,
            /// then if <see cref="Config.UncaughtRejectionHandler"/> is not null, invokes it with each unhandled rejection,
            /// otherwise throws all unhandled rejections as <see cref="AggregateException"/>.
            /// <para/>Does not handle completes if completes are already being handled. Does not handle progress if progress is already being handled or if progress is disabled.
            /// </summary>
            public static void HandleCompletesAndProgress()
            {
                ValidateThreadAccess(1, false);

                bool willThrow = _willThrow;
                _willThrow = true;

                Internal.HandleEvents();
                InvokeProgressListeners();

                if (!willThrow)
                {
                    _willThrow = false;
                    Internal.ThrowUnhandledRejections();
                }
            }

            /// <summary>
            /// Invokes progress callbacks for all promises that had their progress updated,
            /// then if <see cref="Config.UncaughtRejectionHandler"/> is not null, invokes it with each unhandled rejection,
            /// otherwise throws all unhandled rejections as <see cref="AggregateException"/>.
            /// <para/>Does nothing if progress is already being handled or if progress is disabled.
            /// </summary>
            public static void HandleProgress()
            {
                ValidateThreadAccess(1, false);

                bool willThrow = _willThrow;
                _willThrow = true;

                InvokeProgressListeners();

                if (!willThrow)
                {
                    _willThrow = false;
                    Internal.ThrowUnhandledRejections();
                }
            }

            /// <summary>
            /// Clears all currently pooled objects. Does not affect pending or retained promises.
            /// </summary>
            public static void ClearObjectPool()
            {
                ValidateThreadAccess(1);

                ClearPooledProgress();
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