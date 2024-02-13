#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;
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
            private static long s_current;

            internal static long Next()
            {
                long newKey;
                do
                {
                    // We don't check for overflow to let the key generator wrap around for infinite re-use.
                    newKey = Interlocked.Increment(ref s_current);
                } while (newKey == 0); // Don't allow 0 key.
                return newKey;
            }
        }
    }
}