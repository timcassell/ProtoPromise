using System;
using System.Collections.Generic;

namespace Proto.Promises
{
    partial class Promise
    {
        /// <summary>
        /// Promise manager. This can be used to cleared pooled objects (if enabled) or manually handle promises (not recommended for RELEASE builds).
        /// </summary>
        public static class Manager
        {
            private static bool _willThrow;

            /// <summary>
            /// Invokes callbacks for completed promises,
            /// then throws all unhandled rejections as <see cref="AggregateException"/>.
            /// <para/>Does nothing if completes are already being handled.
            /// </summary>
            public static void HandleCompletes()
            {
                bool willThrow = _willThrow;
                _willThrow = true;

                HandleComplete();

                if (!willThrow)
                {
                    _willThrow = false;
                    ThrowUnhandledRejections();
                }
            }

            /// <summary>
            /// Invokes callbacks for completed promises,
            /// then invokes progress callbacks for all promises that had their progress updated,
            /// then throws all unhandled rejections as <see cref="AggregateException"/>.
            /// <para/>Does not handle completes if completes are already being handled. Does not handle progress if progress is already being handled or if progress is disabled.
            /// </summary>
            public static void HandleCompletesAndProgress()
            {
                bool willThrow = _willThrow;
                _willThrow = true;

                HandleComplete();
                InvokeProgressListeners();

                if (!willThrow)
                {
                    _willThrow = false;
                    ThrowUnhandledRejections();
                }
            }

            /// <summary>
            /// Invokes progress callbacks for all promises that had their progress updated,
            /// then throws all unhandled rejections as <see cref="AggregateException"/>.
            /// <para/>Does nothing if progress is already being handled or if progress is disabled.
            /// </summary>
            public static void HandleProgress()
            {
                bool willThrow = _willThrow;
                _willThrow = true;

                InvokeProgressListeners();

                if (!willThrow)
                {
                    _willThrow = false;
                    ThrowUnhandledRejections();
                }
            }

            /// <summary>
            /// Clears all currently pooled objects. Does not affect pending or retained promises.
            /// </summary>
            public static void ClearObjectPool()
            {
                ClearPooledProgress();
                Internal.OnClearPool.Invoke();
            }

            private static void HandleComplete()
            {
                if (_runningHandles)
                {
                    // HandleComplete is running higher in the program stack, so just return.
                    return;
                }

                _runningHandles = true;

                // Cancels are high priority, make sure those delegates are invoked before anything else.
                HandleCanceled();

                while (_handleQueue.IsNotEmpty)
                {
                    _handleQueue.DequeueRisky().Handle();

                    // In case a promise was canceled from a callback.
                    HandleCanceled();
                }

                _handleQueue.ClearLast();
                _runningHandles = false;
            }

            private static void ThrowUnhandledRejections()
            {
                if (_unhandledExceptions.IsEmpty)
                {
                    return;
                }

                var unhandledExceptions = _unhandledExceptions;
                _unhandledExceptions.Clear();
                // Reset handled flag.
                foreach (Internal.UnhandledExceptionInternal unhandled in unhandledExceptions)
                {
                    unhandled.handled = false;
                    // Allow to re-use.
                    unhandled.Release();
                }

#if CSHARP_7_OR_LATER
                throw new AggregateException(unhandledExceptions);
#else
                // .Net 3.5 dumb compiler can't convert IEnumerable<UnhandledExceptionInternal> to IEnumerable<Exception>
                var exceptions = new List<Exception>();
                foreach (var ex in unhandledExceptions)
                {
                    exceptions.Add(ex);
                }
                throw new AggregateException(exceptions);
#endif
            }
        }
    }
}