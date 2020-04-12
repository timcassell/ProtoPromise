// define PROTO_PROMISE_DEBUG_ENABLE to enable debugging options in RELEASE mode. define PROTO_PROMISE_DEBUG_DISABLE to disable debugging options in DEBUG mode.
#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
// define PROTO_PROMISE_CANCEL_DISABLE to disable cancelations on promises.
// If Cancelations are enabled, it breaks the Promises/A+ spec "2.1. Promise States", but allows breaking promise chains. Execution is also a little slower.
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
// define PROTO_PROMISE_PROGRESS_DISABLE to disable progress reports on promises.
// If Progress is enabled, promises use more memory, and it creates an upper bound to the depth of a promise chain (see Config for details).
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter

using System;

namespace Proto.Promises
{
    partial class Promise
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

        public enum PoolType : byte
        {
            /// <summary>
            /// Don't pool any objects.
            /// </summary>
            None,
            /// <summary>
            /// Only pool internal objects.
            /// </summary>
            Internal,
            /// <summary>
            /// Pool all objects, internal and public.
            /// </summary>
            All
        }

        /// <summary>
        /// Promise configuration. Configuration settings affect the global behaviour of promises.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        public static class Config
        {
#if PROMISE_PROGRESS
            /// <summary>
            /// If you need to support longer promise chains, decrease decimalBits. If you need higher precision, increase decimalBits.
            /// <para/>
            /// Wait promise chain limit: 2^(32-<see cref="ProgressDecimalBits"/>),
            /// Precision: 1/(N*2^<see cref="ProgressDecimalBits"/>) where N is the number of wait promises in the chain where Progress is subscribed.
            /// <para/>
            /// NOTE: promises that don't wait (.Then with an onResolved that simply returns a value or void) don't count towards the promise chain limit.
            /// </summary>
            public const int ProgressDecimalBits = 13;
#endif

#if PROMISE_DEBUG
            public static PoolType ObjectPooling { get { return PoolType.Internal; } set { } }
#else
            private static PoolType _objectPooling = PoolType.Internal;
            public static PoolType ObjectPooling { get { return _objectPooling; } set { _objectPooling = value; } }
#endif

#if PROMISE_DEBUG
            private static TraceLevel _debugCausalityTracer = TraceLevel.Rejections;
            /// <summary>
            /// Set how causality is traced in DEBUG mode. Causality traces are readable from an UnhandledException's Stacktrace property.
            /// </summary>
            public static TraceLevel DebugCausalityTracer { get { return _debugCausalityTracer; } set { _debugCausalityTracer = value; } }
#else
            /// <summary>
            /// Set how causality is traced in DEBUG mode. Causality traces are readable from an UnhandledException's Stacktrace property.
            /// </summary>
            public static TraceLevel DebugCausalityTracer { get { return default(TraceLevel); } set { } }
#endif

            /// <summary>
            /// If this is not null, uncaught rejections get routed through this instead of being thrown.
            /// </summary>
            public static Action<UnhandledException> UncaughtRejectionHandler { get; set; }
#if UNITY_2019_2_OR_NEWER
                // Unity changed AggregateException logging to not include the InnerException, so make the default rejection handler route to UnityEngine.Debug.LogException.
                = UnityEngine.Debug.LogException;
#endif
        }
    }
}