#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Channels;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0090 // Use 'new(...)'

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
            internal static readonly object ClosedResolvedReason = new object();
            internal static readonly object ClosedCanceledReason = Promise.CancelException();
            internal static readonly object DisposedReason = new object();

            internal SpinLocker _locker;
            internal int _id;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class ChannelBase : HandleablePromiseBase, ITraceable
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            ~ChannelBase()
            {
                if (_closedReason != ChannelSmallFields.DisposedReason)
                {
                    ReportRejection(new UnreleasedObjectException($"A Channel's resources were garbage collected without being disposed. {this}"), this);
                }
            }

            internal object _closedReason;
            protected ChannelSmallFields _smallFields;
            // This must not be readonly.
            protected ValueLinkedQueue<ChannelWaitToReadPromise> _waitToReaders = new ValueLinkedQueue<ChannelWaitToReadPromise>();

            internal int Id
            {
                [MethodImpl(InlineOption)]
                get => _smallFields._id;
            }

            protected void Reset()
            {
                _next = null;
                _closedReason = null;
                SetCreatedStacktrace(this, 3);
            }

            internal bool TryRemoveWaiter(ChannelWaitToReadPromise promise)
            {
                _smallFields._locker.Enter();
                bool success = _waitToReaders.TryRemove(promise);
                _smallFields._locker.Exit();
                return success;
            }

            internal virtual bool TryRemoveWaiter(ChannelWaitToWritePromise promise) => throw new System.InvalidOperationException();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class ChannelBase<T> : ChannelBase, ITraceable
        {
            // This must not be readonly.
            protected ValueLinkedQueue<ChannelReadPromise<T>> _readers = new ValueLinkedQueue<ChannelReadPromise<T>>();

            internal bool TryRemoveWaiter(ChannelReadPromise<T> promise)
            {
                _smallFields._locker.Enter();
                bool success = _readers.TryRemove(promise);
                _smallFields._locker.Exit();
                return success;
            }

            protected void ValidateInsideLock(int id)
            {
                if (id != Id | _closedReason == ChannelSmallFields.DisposedReason)
                {
                    _smallFields._locker.Exit();
                    throw new System.ObjectDisposedException(nameof(Channel<T>));
                }
                ThrowIfInPool(this);
            }

            internal abstract int GetCount(int id);
            internal abstract ChannelPeekResult<T> TryPeek(int id);
            internal abstract ChannelReadResult<T> TryRead(int id);
            internal abstract ChannelWriteResult<T> TryWrite(in T item, int id);
            internal abstract Promise<ChannelReadResult<T>> ReadAsync(int id, CancelationToken cancelationToken, bool continueOnCapturedContext);
            internal abstract Promise<ChannelWriteResult<T>> WriteAsync(in T item, int id, CancelationToken cancelationToken, bool continueOnCapturedContext);
            internal abstract Promise<bool> WaitToReadAsync(int id, CancelationToken cancelationToken, bool continueOnCapturedContext);
            internal abstract Promise<bool> WaitToWriteAsync(int id, CancelationToken cancelationToken, bool continueOnCapturedContext);
            internal abstract bool TryReject(object reason, int id);
            internal abstract bool TryCancel(int id);
            internal abstract bool TryClose(int id);
            internal abstract void Dispose(int id);
        }
    }
}