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
    internal static class RaceCleanupCallbackHelper
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.RaceCleanupCallback<TArg> GetOrCreate<TArg>(Action<TArg> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreate(DelegateWrapper.Create(onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.RaceCleanupCallback<TArg> GetOrCreate<TArg, TCapture>(in TCapture capturedValue, Action<TCapture, TArg> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreate(DelegateWrapper.Create(capturedValue, onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.RaceCleanupCallback<TArg> GetOrCreate<TArg>(Func<TArg, Promise> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreate(DelegateWrapper.Create(onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.RaceCleanupCallback<TArg> GetOrCreate<TArg, TCapture>(in TCapture capturedValue, Func<TCapture, TArg, Promise> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreate(DelegateWrapper.Create(capturedValue, onCleanup));
        }


        private static class Helper<TArg>
        {
            [MethodImpl(Internal.InlineOption)]
            internal static Internal.RaceCleanupCallback<TArg, TDelegate> GetOrCreate<TDelegate>(in TDelegate callback)
                where TDelegate : IFunc<TArg, Promise>
                => Internal.RaceCleanupCallback<TArg, TDelegate>.GetOrCreate(callback);
        }
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class RaceCleanupCallback<TArg> : HandleablePromiseBase
        {
            internal abstract Promise Invoke(in TArg arg);
            internal abstract void Dispose();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class RaceCleanupCallback<TArg, TDelegate> : RaceCleanupCallback<TArg>
            where TDelegate : IFunc<TArg, Promise>
        {
            private TDelegate _callback;

            [MethodImpl(InlineOption)]
            private static RaceCleanupCallback<TArg, TDelegate> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<RaceCleanupCallback<TArg, TDelegate>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new RaceCleanupCallback<TArg, TDelegate>()
                    : obj.UnsafeAs<RaceCleanupCallback<TArg, TDelegate>>();
            }

            [MethodImpl(InlineOption)]
            internal static RaceCleanupCallback<TArg, TDelegate> GetOrCreate(in TDelegate callback)
            {
                var cb = GetOrCreate();
                cb._next = null;
                cb._callback = callback;
                return cb;
            }

            internal override void Dispose()
            {
                ThrowIfInPool(this);

                _callback = default;
                ObjectPool.MaybeRepool(this);
            }

            internal override Promise Invoke(in TArg arg)
            {
                try
                {
                    return _callback.Invoke(arg);
                }
                catch (Exception e)
                {
                    return Promise.FromException(e);
                }
            }
        } // class RaceCleanupCallback<TArg, TDelegate>
    } // class Internal
} // namespace Proto.Promises