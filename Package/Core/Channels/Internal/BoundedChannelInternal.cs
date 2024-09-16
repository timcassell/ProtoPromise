#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Channels;
using System.Diagnostics;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class BoundedChannel<T> : ChannelBase<T>
        {
            // This must not be readonly.
            private ValueLinkedQueue<ChannelWritePromise<T>> _writers;

            internal override int GetCount(int id)
            {
                throw new System.NotImplementedException();
            }

            internal override Promise<ChannelPeekResult<T>> PeekAsync(int id, CancelationToken cancelationToken)
            {
                throw new System.NotImplementedException();
            }

            internal override Promise<ChannelReadResult<T>> ReadAsync(int id, CancelationToken cancelationToken)
            {
                throw new System.NotImplementedException();
            }

            internal override Promise<ChannelWriteResult<T>> WriteAsync(in T item, int id, CancelationToken cancelationToken)
            {
                throw new System.NotImplementedException();
            }

            internal override bool TryReject(object reason, int id)
            {
                throw new System.NotImplementedException();
            }

            internal override void RemoveReader(int id)
            {
                throw new System.NotImplementedException();
            }

            internal override void RemoveWriter(int id)
            {
                throw new System.NotImplementedException();
            }

            internal bool TryRemoveWaiter(ChannelWritePromise<T> promise)
            {
                _smallFields._locker.Enter();
                bool success = _writers.TryRemove(promise);
                _smallFields._locker.Exit();
                return success;
            }
        }
    }
}