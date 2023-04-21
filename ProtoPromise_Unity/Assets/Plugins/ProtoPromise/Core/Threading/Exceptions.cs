#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System.Diagnostics;

namespace Proto.Promises.Threading
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class AbandonedLockException : System.Exception
    {
        public AbandonedLockException(string message) : base(message) { }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class AbandonedResetEventException : System.Exception
    {
        public AbandonedResetEventException(string message) : base(message) { }
    }
}