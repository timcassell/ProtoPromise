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
        internal abstract class ChannelPromiseBase<TResult, TOwner> : PromiseRefBase.AsyncSynchronizationPromiseBase<TResult>
            where TOwner : class
        {
#if PROMISE_DEBUG
            // We use a weak reference in DEBUG mode so the owner's finalizer can still run if it's dropped.
            private readonly WeakReference _ownerReference = new WeakReference(null, false);
#pragma warning disable IDE1006 // Naming Styles
            protected TOwner _owner
#pragma warning restore IDE1006 // Naming Styles
            {
                get => _ownerReference.Target as TOwner;
                set => _ownerReference.Target = value;
            }
#else
            protected TOwner _owner;
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
        internal sealed class ChannelWritePromise<T> : ChannelPromiseBase<ChannelWriteResult<T>, BoundedChannel<T>>, ILinked<ChannelWritePromise<T>>
        {
            ChannelWritePromise<T> ILinked<ChannelWritePromise<T>>.Next { get; set; }

            [MethodImpl(InlineOption)]
            private static ChannelWritePromise<T> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ChannelWritePromise<T>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new ChannelWritePromise<T>()
                    : obj.UnsafeAs<ChannelWritePromise<T>>();
            }

            [MethodImpl(InlineOption)]
            internal static ChannelWritePromise<T> GetOrCreate(in T item, BoundedChannel<T> owner, SynchronizationContext callerContext)
            {
                var promise = GetOrCreate();
                promise.Reset(callerContext);
                promise._owner = owner;
                // We store the item in the result so it can be added to the buffer when this is resolved.
                promise._result = new ChannelWriteResult<T>(item, ChannelWriteResult.Success);
                return promise;
            }

            [MethodImpl(InlineOption)]
            internal T GetItem()
            {
                ThrowIfInPool(this);
                return _result._droppedItem;
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
                PrepareEarlyDispose();
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
        internal sealed class ChannelReadPromise<T> : ChannelPromiseBase<ChannelReadResult<T>, ChannelBase<T>>, ILinked<ChannelReadPromise<T>>
        {
            ChannelReadPromise<T> ILinked<ChannelReadPromise<T>>.Next { get; set; }

            [MethodImpl(InlineOption)]
            private static ChannelReadPromise<T> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ChannelReadPromise<T>>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new ChannelReadPromise<T>()
                    : obj.UnsafeAs<ChannelReadPromise<T>>();
            }

            [MethodImpl(InlineOption)]
            internal static ChannelReadPromise<T> GetOrCreate(ChannelBase<T> owner, SynchronizationContext callerContext)
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
                PrepareEarlyDispose();
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
        internal sealed class ChannelWaitToWritePromise : ChannelPromiseBase<bool, ChannelBase>, ILinked<ChannelWaitToWritePromise>
        {
            ChannelWaitToWritePromise ILinked<ChannelWaitToWritePromise>.Next { get; set; }

            [MethodImpl(InlineOption)]
            private static ChannelWaitToWritePromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ChannelWaitToWritePromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new ChannelWaitToWritePromise()
                    : obj.UnsafeAs<ChannelWaitToWritePromise>();
            }

            [MethodImpl(InlineOption)]
            internal static ChannelWaitToWritePromise GetOrCreate(ChannelBase owner, SynchronizationContext callerContext)
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
                PrepareEarlyDispose();
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
        internal sealed class ChannelWaitToReadPromise : ChannelPromiseBase<bool, ChannelBase>, ILinked<ChannelWaitToReadPromise>
        {
            ChannelWaitToReadPromise ILinked<ChannelWaitToReadPromise>.Next { get; set; }

            [MethodImpl(InlineOption)]
            private static ChannelWaitToReadPromise GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<ChannelWaitToReadPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new ChannelWaitToReadPromise()
                    : obj.UnsafeAs<ChannelWaitToReadPromise>();
            }

            [MethodImpl(InlineOption)]
            internal static ChannelWaitToReadPromise GetOrCreate(ChannelBase owner, SynchronizationContext callerContext)
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
                PrepareEarlyDispose();
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