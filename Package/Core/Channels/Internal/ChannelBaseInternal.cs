#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Channels;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class ChannelBase<T> : ITraceable
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            protected int id;

            internal int Id
            {
                [MethodImpl(InlineOption)]
                get => id;
            }

            internal abstract int GetCount(int id);
            internal abstract Promise<ChannelReadOrPeekResult<T>> TryPeekAsync(int id, CancelationToken cancelationToken);
            internal abstract Promise<ChannelReadOrPeekResult<T>> TryReadAsync(int id, CancelationToken cancelationToken);
            internal abstract void AddReader(int id);
            internal abstract void RemoveReader(int id);
            internal abstract Promise<ChannelWriteResult<T>> WriteAsync(in T item, int id, CancelationToken cancelationToken);
            internal abstract bool TryReject(IRejectContainer rejectContainer);
            internal abstract void AddWriter(int id);
            internal abstract void RemoveWriter(int id);
            internal abstract bool TryRemoveWaiter(ChannelReaderPromise<T> promise);
            internal abstract bool TryRemoveWaiter(ChannelWriterPromise<T> promise);
        }
    }
}