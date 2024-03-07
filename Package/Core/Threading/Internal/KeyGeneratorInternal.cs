#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        // We use a shared key generator per type instead of per instance, because the key is stored in a struct next to a reference.
        // If the user writes the struct value to a field concurrently, it could tear the struct, resulting in a key/reference mismatch.
        // This protects against that mis-use.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class KeyGenerator<T> // Separate generator per type.
        {
            // We start with 1 and add 2 so we will only get odd numbered keys to prevent ever getting 0.
            // This is more efficient than using a lock or a loop. We lose out on half of the possible values,
            // but 64-bit long is sufficiently large enough that it doesn't matter practically.
            // Most applications will not run long enough to overflow a 64-bit integer, even while skipping half of the values.
            private static long s_current = 1;

            // We don't check for overflow to let the key generator wrap around for infinite re-use.
            [MethodImpl(InlineOption)]
            internal static long Next() => Interlocked.Add(ref s_current, 2);
        }
    }
}