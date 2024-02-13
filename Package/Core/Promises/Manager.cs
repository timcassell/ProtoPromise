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
            public static SynchronizationContext ThreadStaticSynchronizationContext
            {
                [MethodImpl(Internal.InlineOption)]
                get { return Internal.ts_currentContext; }
                [MethodImpl(Internal.InlineOption)]
                set { Internal.ts_currentContext = value; }
            }
        }
    }
}