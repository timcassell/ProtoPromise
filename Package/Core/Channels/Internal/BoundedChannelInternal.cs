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

            internal bool TryRemoveWaiter(ChannelWritePromise<T> promise)
            {
                _smallFields._locker.Enter();
                bool success = _writers.TryRemove(promise);
                _smallFields._locker.Exit();
                return success;
            }

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

            internal override bool TryClose(int id)
            {
                throw new System.NotImplementedException();
            }

            internal override bool TryReject(object reason, int id)
            {
                throw new System.NotImplementedException();
            }

            protected override void Dispose()
            {
                base.Dispose();
                _smallFields._locker.Enter();
                {
                    //_queue.Clear();
                    var peekers = _peekers.MoveElementsToStack();
                    var readers = _readers.MoveElementsToStack();
                    var writers = _writers.MoveElementsToStack();
                    _smallFields._locker.Exit();

                    if (peekers.IsNotEmpty | readers.IsNotEmpty | writers.IsNotEmpty)
                    {
                        var rejection = CreateRejectContainer(new System.ObjectDisposedException(nameof(Channel<T>)), 3, null, this);
                        while (peekers.IsNotEmpty)
                        {
                            peekers.Pop().Reject(rejection);
                        }
                        while (readers.IsNotEmpty)
                        {
                            readers.Pop().Reject(rejection);
                        }
                        while (writers.IsNotEmpty)
                        {
                            writers.Pop().Reject(rejection);
                        }
                    }
                }
                ObjectPool.MaybeRepool(this);
            }
        }
    }
}