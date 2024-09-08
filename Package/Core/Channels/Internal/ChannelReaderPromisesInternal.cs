#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using Proto.Promises.Channels;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class ChannelPromiseBase<T, TResult> : PromiseRefBase.AsyncSynchronizationPromiseBase<TResult>
        {
#if PROMISE_DEBUG
            // We use a weak reference in DEBUG mode so the owner's finalizer can still run if it's dropped.
            private readonly WeakReference _ownerReference = new WeakReference(null, false);
#pragma warning disable IDE1006 // Naming Styles
            protected ChannelBase<T> _owner
#pragma warning restore IDE1006 // Naming Styles
            {
                get => _ownerReference.Target as ChannelBase<T>;
                set => _ownerReference.Target = value;
            }
#else
            protected ChannelBase<T> _owner;
#endif

            [MethodImpl(InlineOption)]
            internal void Resolve(in TResult result)
            {
                ThrowIfInPool(this);

                // We don't need to check if the unregister was successful or not.
                // The fact that this was called means the cancelation was unable to unregister this from the owner.
                // We just dispose to wait for the callback to complete before we continue.
                _cancelationRegistration.Dispose();

                _result = result;
                Continue();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class ChannelReaderPromise<T> : ChannelPromiseBase<T, ChannelReadOrPeekResult<T>>, ILinked<ChannelReaderPromise<T>>
        {
            ChannelReaderPromise<T> ILinked<ChannelReaderPromise<T>>.Next { get; set; }

            [MethodImpl(InlineOption)]
            private static ChannelReaderPromise<T> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ChannelReaderPromise<T>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new ChannelReaderPromise<T>()
                    : obj.UnsafeAs<ChannelReaderPromise<T>>();
            }

            [MethodImpl(InlineOption)]
            internal static ChannelReaderPromise<T> GetOrCreate(ChannelBase<T> owner, SynchronizationContext callerContext)
            {
                var promise = GetOrCreate();
                promise.Reset(callerContext);
                promise._owner = owner;
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                _owner = null;
                ObjectPool.MaybeRepool(this);
            }

            [MethodImpl(InlineOption)]
            internal void DisposeImmediate()
            {
                this.PrepareEarlyDispose();
                MaybeDispose();
            }

            public override void Cancel()
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG
                var _owner = base._owner;
                if (_owner == null)
                {
                    return;
                }
#endif
                if (_owner.TryRemoveWaiter(this))
                {
                    _tempState = Promise.State.Canceled;
                    Continue();
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class ChannelWriterPromise<T> : ChannelPromiseBase<T, ChannelWriteResult<T>>, ILinked<ChannelWriterPromise<T>>
        {
            ChannelWriterPromise<T> ILinked<ChannelWriterPromise<T>>.Next { get; set; }

            [MethodImpl(InlineOption)]
            private static ChannelWriterPromise<T> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ChannelWriterPromise<T>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new ChannelWriterPromise<T>()
                    : obj.UnsafeAs<ChannelWriterPromise<T>>();
            }

            [MethodImpl(InlineOption)]
            internal static ChannelWriterPromise<T> GetOrCreate(ChannelBase<T> owner, SynchronizationContext callerContext)
            {
                var promise = GetOrCreate();
                promise.Reset(callerContext);
                promise._owner = owner;
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                _owner = null;
                ObjectPool.MaybeRepool(this);
            }

            [MethodImpl(InlineOption)]
            internal void DisposeImmediate()
            {
                this.PrepareEarlyDispose();
                MaybeDispose();
            }

            public override void Cancel()
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG
                var _owner = base._owner;
                if (_owner == null)
                {
                    return;
                }
#endif
                if (_owner.TryRemoveWaiter(this))
                {
                    _tempState = Promise.State.Canceled;
                    Continue();
                }
            }
        }
    } // class Internal
} // namespace Proto.Promises