// define PROTO_PROMISE_DEBUG_ENABLE to enable debugging options in RELEASE mode. define PROTO_PROMISE_DEBUG_DISABLE to disable debugging options in DEBUG mode.
#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// At what granularity should stack traces be captured when a promise is created or rejected. Higher values are more costly, but give more information for debugging purposes.
        /// </summary>
        public enum TraceLevel : byte
        {
            /// <summary>
            /// Don't track any causality traces.
            /// </summary>
            None,
            /// <summary>
            /// Track causality only when Deferred.Reject is called.
            /// </summary>
            Rejections,
            /// <summary>
            /// Track causality when Deferred.Reject is called and every time a promise is created or a delegate is added to a promise (i.e. with .Then).
            /// <para/>
            /// NOTE: This can be extremely expensive, so you should only enable this if you ran into an error and you are not sure where it came from.
            /// </summary>
            All
        }

        /// <summary>
        /// Promise configuration. Configuration settings affect the global behaviour of promises.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public static class Config
        {
            /// <summary>
            /// Should objects be pooled or not. If this is enabled, objects can be reused to reduce GC pressure.
            /// </summary>
#if PROMISE_DEBUG
            // Object pooling disabled in DEBUG mode to prevent thread race conditions when validating for circular awaits.
            public static bool ObjectPoolingEnabled
            {
                [MethodImpl(Internal.InlineOption)]
                get => false;
                [MethodImpl(Internal.InlineOption)]
                set { }
            }
#else
            public static bool ObjectPoolingEnabled
            {
                [MethodImpl(Internal.InlineOption)]
                get => s_objectPoolingEnabled;
                [MethodImpl(Internal.InlineOption)]
                set => s_objectPoolingEnabled = value;
            }
            private static bool s_objectPoolingEnabled = true; // Enabled by default.
#endif

            /// <summary>
            /// Set how causality is traced in DEBUG mode. Causality traces are readable from an UnhandledException's Stacktrace property.
            /// </summary>
#if PROMISE_DEBUG
            public static TraceLevel DebugCausalityTracer
            {
                [MethodImpl(Internal.InlineOption)]
                get => s_debugCausalityTracer;
                [MethodImpl(Internal.InlineOption)]
                set => s_debugCausalityTracer = value;
            }
            private static TraceLevel s_debugCausalityTracer = TraceLevel.Rejections;
#else
            public static TraceLevel DebugCausalityTracer
            {
                [MethodImpl(Internal.InlineOption)]
                get => default;
                [MethodImpl(Internal.InlineOption)]
                set { }
            }
#endif

            /// <summary>
            /// Uncaught rejections get routed through this delegate.
            /// </summary>
            /// <remarks>
            /// This must be set to a non-null delegate, otherwise uncaught rejections will be thrown in the <see cref="ForegroundContext"/> or <see cref="BackgroundContext"/>.
            /// </remarks>
            public static Action<UnhandledException> UncaughtRejectionHandler
            {
                [MethodImpl(Internal.InlineOption)]
                get => s_uncaughtRejectionHandler;
                [MethodImpl(Internal.InlineOption)]
                set => s_uncaughtRejectionHandler = value;
            }
            private static Action<UnhandledException> s_uncaughtRejectionHandler;

            /// <summary>
            /// The <see cref="SynchronizationContext"/> used to marshal work to the UI thread.
            /// </summary>
            /// <remarks>It is recommended to set this at application startup. It is also recommended to set <see cref="Manager.ThreadStaticSynchronizationContext"/> at the same time
            /// if you are in a .Net environment that is older than .Net 6.</remarks>
            /// <example>
            /// <code>
            /// Promise.Config.ForegroundContext = SynchronizationContext.Current;
            /// </code>
            /// </example>
            public static SynchronizationContext ForegroundContext
            {
                [MethodImpl(Internal.InlineOption)]
                get => s_foregroundContext;
                [MethodImpl(Internal.InlineOption)]
                set => s_foregroundContext = value;
            }
            private static SynchronizationContext s_foregroundContext;

            /// <summary>
            /// The <see cref="SynchronizationContext"/> used to marshal work to a background thread. If this is null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> is used.
            /// </summary>
            public static SynchronizationContext BackgroundContext
            {
                [MethodImpl(Internal.InlineOption)]
                get => s_backgroundContext;
                [MethodImpl(Internal.InlineOption)]
                set => s_backgroundContext = value;
            }
            private static SynchronizationContext s_backgroundContext;

            /// <summary>
            /// When enabled, <see cref="AsyncLocal{T}"/> objects are supported in async <see cref="Promise"/> and async <see cref="Promise{T}"/> methods.
            /// </summary>
            /// <remarks>
            /// This is disabled by default, and cannot be disabled after enabled.
            /// </remarks>
            public static bool AsyncFlowExecutionContextEnabled
            {
                [MethodImpl(Internal.InlineOption)]
                get => s_asyncFlowExecutionContextEnabled;
                [MethodImpl(Internal.InlineOption)]
                set
                {
                    if (!value)
                    {
                        ThrowCannotDisableAsyncFlow();
                    }
                    s_asyncFlowExecutionContextEnabled = true;
                }
            }
            // Internal so that tests can disable it directly.
            internal static bool s_asyncFlowExecutionContextEnabled;

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowCannotDisableAsyncFlow()
                => throw new InvalidOperationException("Cannot disable AsyncFlowExecutionContext. It may only be enabled.");

            /// <summary>
            /// The default <see cref="TimeProvider"/> to use for time-based methods when one is not provided.
            /// </summary>
            public static TimeProvider DefaultTimeProvider
            {
                [MethodImpl(Internal.InlineOption)]
                get => s_defaultTimeProvider;
                [MethodImpl(Internal.InlineOption)]
                set
                {
                    if (value == null)
                    {
                        ThrowNullTimeProvider();
                    }
                    s_defaultTimeProvider = value;
                }
            }
            private static TimeProvider s_defaultTimeProvider = PooledSystemTimeProvider.Instance;

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowNullTimeProvider()
                => throw new ArgumentNullException("value", "TimeProvider may not be null.");
        }
    }
}