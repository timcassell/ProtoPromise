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
        public enum GeneratedStacktrace : byte
        {
            /// <summary>
            /// Don't generate any extra stack traces.
            /// </summary>
            None,
            /// <summary>
            /// Generate stack traces when Deferred.Reject is called.
            /// If Reject is called with an exception, the generated stack trace is appended to the exception's stacktrace.
            /// </summary>
            Rejections,
            /// <summary>
            /// Generate stack traces when Deferred.Reject is called.
            /// Also generate stack traces every time a promise is created (i.e. with .Then). This can help debug where an invalid object was returned from a .Then delegate.
            /// If a .Then/.Catch callback throws an exception, the generated stack trace is appended to the exception's stacktrace.
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
            private static GeneratedStacktrace _debugStacktraceGenerator = GeneratedStacktrace.Rejections;
            public static GeneratedStacktrace DebugStacktraceGenerator { get { return _debugStacktraceGenerator; } set { _debugStacktraceGenerator = value; } }
#else
            public static GeneratedStacktrace DebugStacktraceGenerator { get { return default(GeneratedStacktrace); } set { } }
#endif

            /// <summary>
            /// If this is not null, uncaught rejections get routed through this instead of being thrown.
            /// </summary>
            public static Action<UnhandledException> UncaughtRejectionHandler { get; set; }
        }
    }
}