#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static class CleanupCallbackHelper
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.CleanupCallbackBase GetOrCreate<TArg>(in Promise<TArg> promise, Action<TArg> onCleanup)
        {
            if (onCleanup == null) throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));

            return GetOrCreate(promise, DelegateWrapper.Create(onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.CleanupCallbackBase GetOrCreate<TArg, TCapture>(in Promise<TArg> promise, in TCapture capturedValue, Action<TCapture, TArg> onCleanup)
        {
            if (onCleanup == null) throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));

            return GetOrCreate(promise, DelegateWrapper.Create(capturedValue, onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.CleanupCallbackBase GetOrCreate<TArg>(in Promise<TArg> promise, Func<TArg, Promise> onCleanup)
        {
            if (onCleanup == null) throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));

            return GetOrCreate(promise, DelegateWrapper.Create(onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.CleanupCallbackBase GetOrCreate<TArg, TCapture>(in Promise<TArg> promise, in TCapture capturedValue, Func<TCapture, TArg, Promise> onCleanup)
        {
            if (onCleanup == null) throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));

            return GetOrCreate(promise, DelegateWrapper.Create(capturedValue, onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        private static Internal.CleanupCallback<TArg, TDelegate> GetOrCreate<TArg, TDelegate>(in Promise<TArg> promise, in TDelegate callback)
            where TDelegate : IFunc<TArg, Promise>
            => Internal.CleanupCallback<TArg, TDelegate>.GetOrCreate(promise, callback);
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class CleanupCallbackBase : HandleablePromiseBase, ILinked<CleanupCallbackBase>
        {
            CleanupCallbackBase ILinked<CleanupCallbackBase>.Next
            {
                [MethodImpl(InlineOption)]
                get => _next.UnsafeAs<CleanupCallbackBase>();
                [MethodImpl(InlineOption)]
                set => _next = value;
            }

            internal abstract void Prepare();
            internal abstract Promise DisposeAndInvoke();
            internal abstract void Dispose();
        }


#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class CleanupCallback<TArg, TDelegate> : CleanupCallbackBase, ITraceable
            where TDelegate : IFunc<TArg, Promise>
        {
#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            // We store the owner to retrieve its result. We don't dispose it here, though, it's disposed in PromisePassThrough.
            private PromiseRefBase.PromiseRef<TArg> _owner;
            private TDelegate _callback;
            private TArg _arg;
            private bool _needsCleanup;

            [MethodImpl(InlineOption)]
            private static CleanupCallback<TArg, TDelegate> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<CleanupCallback<TArg, TDelegate>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new CleanupCallback<TArg, TDelegate>()
                    : obj.UnsafeAs<CleanupCallback<TArg, TDelegate>>();
            }

            [MethodImpl(InlineOption)]
            internal static CleanupCallback<TArg, TDelegate> GetOrCreate(in Promise<TArg> promise, in TDelegate callback)
            {
                var cb = GetOrCreate();
                cb._next = null;
                SetCreatedStacktrace(cb, 2);
                cb._owner = promise._ref;
                cb._callback = callback;
                cb._arg = promise._result;
                return cb;
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

            internal override Promise DisposeAndInvoke()
            {
                ThrowIfInPool(this);

                if (!_needsCleanup)
                {
                    Dispose();
                    return Promise.Resolved();
                }

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

            internal override void Dispose()
            {
                ThrowIfInPool(this);

                _owner = null;
                _callback = default;
                ClearReferences(ref _arg);
                ObjectPool.MaybeRepool(this);
            }
        }
    }
}