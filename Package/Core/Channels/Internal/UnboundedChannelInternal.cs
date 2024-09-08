#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class UnboundedChannel<T> : ChannelBase<T>
        {
            private ValueLinkedQueue<ChannelReaderPromise<T>> _readers;
            private int _readerCount;
            private int _writerCount;
            private SpinLocker _locker;
        }
    }
}