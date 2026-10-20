// define PROTO_PROMISE_DEBUG_ENABLE to enable debugging options in RELEASE mode. define PROTO_PROMISE_DEBUG_DISABLE to disable debugging options in DEBUG mode.
#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter

using Proto.Timers;
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
        [Obsolete("Prefer Proto.Diagnostics.CausalityTrace.IsEnabled", false)]
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
            // The fields have no initializers, so that the class has no static constructor. Otherwise, IL2CPP checks that the class is initialized
            // where the fields are accessed, which includes hot paths (e.g. async methods read s_asyncFlowExecutionContextEnabled).
            /// <summary>
            /// Should objects be pooled or not. If this is enabled, objects can be reused to reduce GC pressure.
            /// </summary>
            /// <remarks>
            /// Objects are not pooled while <see cref="Diagnostics.CausalityTrace.IsEnabled"/> is <see langword="true"/>.
            /// </remarks>
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
                get => !s_objectPoolingDisabled;
                set
                {
                    s_objectPoolingDisabled = !value;
                    Internal.UpdateIsObjectPoolingEnabled();
                }
            }
            // Inverted, so that pooling is enabled by default without an initializer.
            private static bool s_objectPoolingDisabled;
#endif

            /// <summary>
            /// Set how causality is traced in DEBUG mode. Causality traces are readable from an UnhandledException's Stacktrace property.
            /// </summary>
            [Obsolete("Prefer Proto.Diagnostics.CausalityTrace.IsEnabled", false)]
            public static TraceLevel DebugCausalityTracer
            {
#if PROMISE_DEBUG
                get => Diagnostics.CausalityTrace.IsEnabled ? TraceLevel.All : TraceLevel.None;
                set
                {
                    // Rejections are always traced while causality traces are enabled, so Rejections leaves it unchanged.
                    if (value != TraceLevel.Rejections)
                    {
                        Diagnostics.CausalityTrace.IsEnabled = value == TraceLevel.All;
                    }
                }
#else
                // This only applies in DEBUG mode, like it always did, so that it doesn't enable the expensive causality traces in RELEASE builds.
                get => TraceLevel.None;
                set { }
#endif
            }

            /// <summary>
            /// Uncaught rejections get routed through this delegate.
            /// </summary>
            /// <remarks>
            /// This must be set to a non-null delegate, otherwise uncaught rejections will be thrown in the <see cref="ForegroundContext"/> or <see cref="BackgroundContext"/>.
            /// If the delegate throws, its exception is thrown in the <see cref="ForegroundContext"/> or <see cref="BackgroundContext"/> instead.
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
            /// The default <see cref="TimerFactory"/> to use for time-based methods when one is not provided.
            /// </summary>
            public static TimerFactory DefaultTimerFactory
            {
                [MethodImpl(Internal.InlineOption)]
                get => DefaultTimerFactoryHolder.s_value;
                [MethodImpl(Internal.InlineOption)]
                set
                {
                    if (value == null)
                    {
                        ThrowNullTimerFactory();
                    }
                    DefaultTimerFactoryHolder.s_value = value;
                }
            }
            // The field is in its own class, so that its initializer doesn't give this class a static constructor.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class DefaultTimerFactoryHolder
            {
                internal static TimerFactory s_value = TimerFactory.System;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowNullTimerFactory()
                => throw new ArgumentNullException("value", $"{nameof(TimerFactory)} may not be null.");
        }
    }
}