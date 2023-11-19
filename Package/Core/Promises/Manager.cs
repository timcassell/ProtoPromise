#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0031 // Use null propagation
#pragma warning disable IDE1005 // Delegate invocation can be simplified.
#pragma warning disable CA1041 // Provide ObsoleteAttribute message
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

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

            /// <summary>
            /// Resets the runtime context so that any unreleased objects will not throw exceptions.
            /// </summary>
            /// <remarks>
            /// This should be called if you are stopping and restarting code without resetting the assembly (AssemblyLoadContext in Core, or AppDomain in Framework).
            /// For example, exiting and re-entering play mode in Unity Editor with reload AppDomain disabled.
            /// </remarks>
            public static void ResetRuntimeContext()
            {
                Internal.ClearPool();
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                Internal.SuppressAllFinalizables();
#endif
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