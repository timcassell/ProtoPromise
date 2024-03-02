using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

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
            /// <summary>
            /// Clears all currently pooled objects. Does not affect pending or preserved promises.
            /// </summary>
            public static void ClearObjectPool()
            {
                Internal.ClearPool();
            }

            /// <summary>
            /// The <see cref="SynchronizationContext"/> for the current thread, used internally to execute continuations synchronously if the supplied context matches this.
            /// </summary>
            /// <remarks>It is recommended to set this at application startup, at the same as you set <see cref="Config.ForegroundContext"/>.</remarks>
#if NETCOREAPP
            [EditorBrowsable(EditorBrowsableState.Never)]
#endif
            public static SynchronizationContext ThreadStaticSynchronizationContext
            // TODO: update compilation symbol when Unity adopts .Net Core.
#if NETCOREAPP
            // .Net Core stores the SynchronizationContext.Current only in the thread, not the ExecutionContext like .Net Framework does, so we can just use it directly.
            {
                [MethodImpl(Internal.InlineOption)]
                get => SynchronizationContext.Current;
                [MethodImpl(Internal.InlineOption)]
                set => SynchronizationContext.SetSynchronizationContext(value);
            }
#else
            // .Net Framework flows the SynchronizationContext.Current through the ExecutionContext, causing it to be set even on background threads.
            // To avoid needlessly posting background workers to the foreground context, we only store it on the thread and use this instead of SynchronizationContext.Current.
            {
                [MethodImpl(Internal.InlineOption)]
                get => ts_currentContext;
                [MethodImpl(Internal.InlineOption)]
                set => ts_currentContext = value;
            }
            [ThreadStatic]
            private static SynchronizationContext ts_currentContext;
#endif
        }
    }
}