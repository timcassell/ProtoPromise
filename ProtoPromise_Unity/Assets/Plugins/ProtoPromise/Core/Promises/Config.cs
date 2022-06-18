// define PROTO_PROMISE_DEBUG_ENABLE to enable debugging options in RELEASE mode. define PROTO_PROMISE_DEBUG_DISABLE to disable debugging options in DEBUG mode.
#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
// define PROTO_PROMISE_PROGRESS_DISABLE to disable progress reports on promises.
// If Progress is enabled, promises use more memory, and it creates an upper bound to the depth of a promise chain (see Config for details).
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
#pragma warning disable 1574 // XML comment has cref attribute that could not be resolved

using System;
using System.ComponentModel;
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
            /// Track causality when Deferred.Reject is called and every time a promise is created or a delegate is added to a promise (i.e. with .Then or .Progress).
            /// <para/>
            /// NOTE: This can be extremely expensive, so you should only enable this if you ran into an error and you are not sure where it came from.
            /// </summary>
            All
        }

        [Obsolete("Promise Config now uses a simple boolean for object pooling."), EditorBrowsable(EditorBrowsableState.Never)]
        public enum PoolType : byte
        {
            None,
            Internal,
            All
        }

        /// <summary>
        /// Promise configuration. Configuration settings affect the global behaviour of promises.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public static partial class Config
        {
            [Obsolete("Use ProgressPrecision to get the precision of progress reports."), EditorBrowsable(EditorBrowsableState.Never)]
            public static readonly int ProgressDecimalBits = Internal.PromiseRefBase.Fixed32.DecimalBits;

            /// <summary>
            /// The maximum precision of progress reports.
            /// </summary>
#if !PROMISE_PROGRESS
            [Obsolete(Internal.ProgressDisabledMessage, false)]
#endif
            public static readonly float ProgressPrecision = (float) (1d / Math.Pow(2d, Internal.PromiseRefBase.Fixed32.DecimalBits));

            [Obsolete("Use ObjectPoolingEnabled instead."), EditorBrowsable(EditorBrowsableState.Never)]
            public static PoolType ObjectPooling 
            {
                get { return ObjectPoolingEnabled ? PoolType.All : PoolType.None; }
                set { ObjectPoolingEnabled = value != PoolType.None; }
            }

            /// <summary>
            /// Should objects be pooled or not. If this is enabled, objects can be reused to reduce GC pressure.
            /// </summary>
#if PROMISE_DEBUG
            // Object pooling disabled in DEBUG mode to prevent thread race conditions when validating for circular awaits.
            public static bool ObjectPoolingEnabled
            {
                [MethodImpl(Internal.InlineOption)]
                get { return false; }
                [MethodImpl(Internal.InlineOption)]
                set { }
            }
#else
            volatile private static bool s_objectPoolingEnabled = true; // Enabled by default.
            public static bool ObjectPoolingEnabled
            {
                [MethodImpl(Internal.InlineOption)]
                get { return s_objectPoolingEnabled; } 
                [MethodImpl(Internal.InlineOption)]
                set { s_objectPoolingEnabled = value; } 
            }
#endif

            /// <summary>
            /// Set how causality is traced in DEBUG mode. Causality traces are readable from an UnhandledException's Stacktrace property.
            /// </summary>
#if PROMISE_DEBUG
            public static TraceLevel DebugCausalityTracer
            {
                [MethodImpl(Internal.InlineOption)]
                get { return s_debugCausalityTracer; }
                [MethodImpl(Internal.InlineOption)]
                set { s_debugCausalityTracer = value; }
            }
            volatile private static TraceLevel s_debugCausalityTracer = TraceLevel.Rejections;
#else
            public static TraceLevel DebugCausalityTracer
            {
                [MethodImpl(Internal.InlineOption)]
                get { return default(TraceLevel); }
                [MethodImpl(Internal.InlineOption)]
                set { }
            }
#endif

            /// <summary>
            /// Uncaught rejections get routed through this delegate.
            /// This must be set to a non-null delegate, otherwise uncaught rejections will be thrown in the <see cref="ForegroundContext"/> or <see cref="BackgroundContext"/>.
            /// </summary>
            public static Action<UnhandledException> UncaughtRejectionHandler
            {
                [MethodImpl(Internal.InlineOption)]
                get { return s_uncaughtRejectionHandler; }
                [MethodImpl(Internal.InlineOption)]
                set { s_uncaughtRejectionHandler = value; }
            }
            volatile private static Action<UnhandledException> s_uncaughtRejectionHandler;

            /// <summary>
            /// The <see cref="SynchronizationContext"/> used to marshal work to the UI thread.
            /// </summary>
            public static SynchronizationContext ForegroundContext
            {
                [MethodImpl(Internal.InlineOption)]
                get { return s_foregroundContext; }
                set
                {
                    s_foregroundContext = value;
                }
            }
            volatile private static SynchronizationContext s_foregroundContext;

            /// <summary>
            /// The <see cref="SynchronizationContext"/> used to marshal work to a background thread. If this is null, <see cref="ThreadPool.QueueUserWorkItem(WaitCallback, object)"/> is used.
            /// </summary>
            public static SynchronizationContext BackgroundContext
            {
                [MethodImpl(Internal.InlineOption)]
                get { return s_backgroundContext; }
                [MethodImpl(Internal.InlineOption)]
                set { s_backgroundContext = value; }
            }
            volatile private static SynchronizationContext s_backgroundContext;

            /// <summary>
            /// When enabled, <see cref="AsyncLocal{T}"/> objects are supported in async <see cref="Promise"/> and async <see cref="Promise{T}"/> methods.
            /// </summary>
            /// <remarks>
            /// This is disabled by default, and cannot be disabled after enabled.
            /// </remarks>
            public static bool AsyncFlowExecutionContextEnabled
            {
                [MethodImpl(Internal.InlineOption)]
                get { return s_asyncFlowExecutionContextEnabled; }
                [MethodImpl(Internal.InlineOption)]
                set
                {
                    if (!value)
                    {
                        ThrowCannotDisableAsyncLocal();
                    }
                    s_asyncFlowExecutionContextEnabled = true;
                }
            }
            private static bool s_asyncFlowExecutionContextEnabled;

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowCannotDisableAsyncLocal()
            {
                throw new InvalidOperationException("Cannot disable AsyncLocal. It may only be enabled.");
            }

            [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
            public static Action<string> WarningHandler { get; set; }
        }
    }
}