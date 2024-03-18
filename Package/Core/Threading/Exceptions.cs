#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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
    public class AbandonedConditionVariableException : System.Exception
    {
        public AbandonedConditionVariableException(string message) : base(message) { }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class AbandonedResetEventException : System.Exception
    {
        public AbandonedResetEventException(string message) : base(message) { }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class AbandonedSemaphoreException : System.Exception
    {
        public AbandonedSemaphoreException(string message) : base(message) { }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode]
#endif
    public class SemaphoreFullException : System.Threading.SemaphoreFullException
    {
        public SemaphoreFullException(string stackTrace)
        {
            _stackTrace = stackTrace;
        }

        private readonly string _stackTrace;
        public override string StackTrace => _stackTrace ?? base.StackTrace;
    }
}