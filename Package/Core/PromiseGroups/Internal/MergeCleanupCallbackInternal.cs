#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static class MergeCleanupCallbackHelper
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.MergeCleanupCallback GetOrCreate<TArg>(in Promise<TArg> promise, Action<TArg> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return GetOrCreate(promise, DelegateWrapper.Create(onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.MergeCleanupCallback GetOrCreate<TArg, TCapture>(in Promise<TArg> promise, in TCapture capturedValue, Action<TCapture, TArg> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return GetOrCreate(promise, DelegateWrapper.Create(capturedValue, onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.MergeCleanupCallback GetOrCreate<TArg>(in Promise<TArg> promise, Func<TArg, Promise> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return GetOrCreate(promise, DelegateWrapper.Create(onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.MergeCleanupCallback GetOrCreate<TArg, TCapture>(in Promise<TArg> promise, in TCapture capturedValue, Func<TCapture, TArg, Promise> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return GetOrCreate(promise, DelegateWrapper.Create(capturedValue, onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        private static Internal.MergeCleanupCallback<TArg, TDelegate> GetOrCreate<TArg, TDelegate>(in Promise<TArg> promise, in TDelegate callback)
            where TDelegate : IFunc<TArg, Promise>
            => Internal.MergeCleanupCallback<TArg, TDelegate>.GetOrCreate(promise, callback);
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class MergeCleanupCallback : HandleablePromiseBase, ILinked<MergeCleanupCallback>
        {
            MergeCleanupCallback ILinked<MergeCleanupCallback>.Next
            {
                [MethodImpl(InlineOption)]
                get => _next.UnsafeAs<MergeCleanupCallback>();
                [MethodImpl(InlineOption)]
                set => _next = value;
            }

            internal abstract void Prepare();
            internal abstract Promise InvokeAndDispose();
            internal abstract Promise InvokeAndDisposeImmediate();
            internal abstract void Dispose();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class MergeCleanupCallback<TArg, TDelegate> : MergeCleanupCallback
            where TDelegate : IFunc<TArg, Promise>
        {
            // We store the owner to retrieve its result. We don't dispose it here, though, it's disposed in PromisePassThrough.
            private PromiseRefBase.PromiseRef<TArg> _owner;
            private TDelegate _callback;
            private TArg _arg;
            private bool _needsCleanup;

            [MethodImpl(InlineOption)]
            private static MergeCleanupCallback<TArg, TDelegate> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<MergeCleanupCallback<TArg, TDelegate>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new MergeCleanupCallback<TArg, TDelegate>()
                    : obj.UnsafeAs<MergeCleanupCallback<TArg, TDelegate>>();
            }

            [MethodImpl(InlineOption)]
            internal static MergeCleanupCallback<TArg, TDelegate> GetOrCreate(in Promise<TArg> promise, in TDelegate callback)
            {
                var cb = GetOrCreate();
                cb._next = null;
                cb._owner = promise._ref;
                cb._callback = callback;
                cb._arg = promise._result;
                return cb;
            }

            internal override void Dispose()
            {
                ThrowIfInPool(this);

                _owner = null;
                _callback = default;
                ClearReferences(ref _arg);
                ObjectPool.MaybeRepool(this);
            }

            internal override void Prepare()
            {
                ThrowIfInPool(this);

                var owner = _owner;
                if (owner == null)
                {
                    _needsCleanup = true;
                }
                else if (owner.State == Promise.State.Resolved)
                {
                    _arg = owner._result;
                    _needsCleanup = true;
                }
                else
                {
                    _needsCleanup = false;
                }
            }

            internal override Promise InvokeAndDispose()
            {
                if (_needsCleanup)
                {
                    return InvokeAndDisposeImmediate();
                }

                Dispose();
                return Promise.Resolved();
            }

            internal override Promise InvokeAndDisposeImmediate()
            {
                ThrowIfInPool(this);

                var callback = _callback;
                var arg = _arg;
                Dispose();

                try
                {
                    return callback.Invoke(arg);
                }
                catch (Exception e)
                {
                    return Promise.FromException(e);
                }
            }
        } // class MergeCleanupCallback<TArg, TDelegate>
    } // class Internal
} // namespace Proto.Promises