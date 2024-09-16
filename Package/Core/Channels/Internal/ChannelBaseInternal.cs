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
        // Wrapping struct fields smaller than 64-bits in another struct fixes issue with extra padding
        // (see https://stackoverflow.com/questions/67068942/c-sharp-why-do-class-fields-of-struct-types-take-up-more-space-than-the-size-of).
        internal struct ChannelSmallFields
        {
            internal SpinLocker _locker;
            internal int _id;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class ChannelBase<T> : HandleablePromiseBase, ITraceable
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            internal IRejectContainer _rejection;
            // These must not be readonly.
            protected ValueLinkedQueue<ChannelReadPromise<T>> _readers;
            protected ValueLinkedQueue<ChannelPeekPromise<T>> _peekers;
            protected ChannelSmallFields _smallFields;
            internal uint _readerCount;
            internal uint _writerCount;

            internal int Id
            {
                [MethodImpl(InlineOption)]
                get => _smallFields._id;
            }

            protected void Reset()
            {
                _readerCount = 1;
                _writerCount = 1;
                SetCreatedStacktrace(this, 3);
            }

            internal bool TryRemoveWaiter(ChannelReadPromise<T> promise)
            {
                _smallFields._locker.Enter();
                bool success = _readers.TryRemove(promise);
                _smallFields._locker.Exit();
                return success;
            }

            internal bool TryRemoveWaiter(ChannelPeekPromise<T> promise)
            {
                _smallFields._locker.Enter();
                bool success = _peekers.TryRemove(promise);
                _smallFields._locker.Exit();
                return success;
            }

            internal void AddReader(int id)
            {
                _smallFields._locker.Enter();
                {
                    uint count = _readerCount;
                    if (id != Id | count == 0 | count == uint.MaxValue)
                    {
                        _smallFields._locker.Exit();
                        if (count == uint.MaxValue)
                        {
                            throw new System.OverflowException();
                        }
                        throw new InvalidOperationException("Channel reader is invalid.", GetFormattedStacktrace(2));
                    }
                    unchecked { _readerCount = count + 1; }
                }
                _smallFields._locker.Exit();
            }

            internal void AddWriter(int id)
            {
                _smallFields._locker.Enter();
                {
                    uint count = _writerCount;
                    if (id != Id | count == 0 | count == uint.MaxValue)
                    {
                        _smallFields._locker.Exit();
                        if (count == uint.MaxValue)
                        {
                            throw new System.OverflowException();
                        }
                        throw new InvalidOperationException("Channel writer is invalid.", GetFormattedStacktrace(2));
                    }
                    unchecked { _writerCount = count + 1; }
                }
                _smallFields._locker.Exit();
            }

            internal abstract int GetCount(int id);
            internal abstract Promise<ChannelPeekResult<T>> PeekAsync(int id, CancelationToken cancelationToken);
            internal abstract Promise<ChannelReadResult<T>> ReadAsync(int id, CancelationToken cancelationToken);
            internal abstract Promise<ChannelWriteResult<T>> WriteAsync(in T item, int id, CancelationToken cancelationToken);
            internal abstract bool TryReject(object reason, int id);
            internal abstract void RemoveReader(int id);
            internal abstract void RemoveWriter(int id);
        }
    }
}