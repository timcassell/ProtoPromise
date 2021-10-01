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

using System;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial struct Promise
    {
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

        [Obsolete("Promise Config now uses a simple boolean for object pooling.")]
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
        public static class Config
        {
            /// <summary>
            /// If you need to support longer promise chains, decrease decimalBits. If you need higher precision, increase decimalBits.
            /// <para/>
            /// Wait promise chain limit: 2^(31-<see cref="ProgressDecimalBits"/>),
            /// Precision: 1/(N*2^<see cref="ProgressDecimalBits"/>) where N is the number of wait promises in the chain where Progress is subscribed.
            /// <para/>
            /// NOTE: promises that don't wait (.Then with an onResolved that simply returns a value or void) don't count towards the promise chain limit.
            /// The limit is removed when progress is disabled (this is compiled with the symbol PROTO_PROMISE_PROGRESS_DISABLE defined).
            /// </summary>
#if !PROMISE_PROGRESS
            [Obsolete("Progress is disabled. Remove PROTO_PROMISE_PROGRESS_DISABLE from your compiler symbols to enable progress reports.", false)]
#endif
            // TODO: just use (u)short in Fixed32 instead of bit shift.
            public const int ProgressDecimalBits = 13; // Must be const. Allowing this to change at runtime could mess up progress in flight.

            [Obsolete("Use ObjectPoolingEnabled instead.")]
            public static PoolType ObjectPooling { get { return _objectPoolingEnabled ? PoolType.All : PoolType.None; } set { _objectPoolingEnabled = value != PoolType.None; } }

            volatile private static bool _objectPoolingEnabled = true; // Enabled by default.
            public static bool ObjectPoolingEnabled { get { return _objectPoolingEnabled; } set { _objectPoolingEnabled = value; } }

            /// <summary>
            /// Set how causality is traced in DEBUG mode. Causality traces are readable from an UnhandledException's Stacktrace property.
            /// </summary>
#if PROMISE_DEBUG
            public static TraceLevel DebugCausalityTracer { get { return _debugCausalityTracer; } set { _debugCausalityTracer = value; } }
            volatile private static TraceLevel _debugCausalityTracer = TraceLevel.Rejections;
#else
            public static TraceLevel DebugCausalityTracer { get { return default(TraceLevel); } set { } }
#endif

            // Used so that libraries can have a ProtoPromise dependency without forcing progress enabled/disabled on those libraries' users.
            // e.g. a library depends on ProtoPromise v2.0.0 or higher, a user of that library could opt to use ProtoPromise v2.0.0.0 (no progress) or v2.0.0.1 (with progress)
            public static bool IsProgressEnabled
            {
                [MethodImpl(MethodImplOptions.NoInlining)] // Don't allow inlining, otherwise it could break library code that functions depending on if progress is enabled or not.
                get
                {
#if PROMISE_PROGRESS
                    return true;
#else
                    return false;
#endif
                }
            }

            /// <summary>
            /// If this is not null, uncaught rejections get routed through this instead of being thrown.
            /// </summary>
            public static Action<UnhandledException> UncaughtRejectionHandler { get; set; }

            /// <summary>
            /// Warning handler.
            /// </summary>
            public static Action<string> WarningHandler { get; set; }
        }
    }
}