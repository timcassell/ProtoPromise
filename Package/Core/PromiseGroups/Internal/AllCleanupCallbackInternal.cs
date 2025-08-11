#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

using Proto.Promises.Collections;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static class AllCleanupCallbackHelper
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AllCleanupCallback<TArg> GetOrCreate<TArg>(Action<TArg> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreate(DelegateWrapper.Create(onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AllCleanupCallback<TArg> GetOrCreate<TArg, TCapture>(in TCapture capturedValue, Action<TCapture, TArg> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreate(DelegateWrapper.Create(capturedValue, onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AllCleanupCallback<TArg> GetOrCreate<TArg>(Func<TArg, Promise> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreate(DelegateWrapper.Create(onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AllCleanupCallback<TArg> GetOrCreate<TArg, TCapture>(in TCapture capturedValue, Func<TCapture, TArg, Promise> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreate(DelegateWrapper.Create(capturedValue, onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AllCleanupCallback<TArg> GetOrCreate<TArg>(Action<TArg, int> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreateWithIndex(DelegateWrapper.Create(onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AllCleanupCallback<TArg> GetOrCreate<TArg, TCapture>(in TCapture capturedValue, Action<TCapture, TArg, int> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreateWithIndex(DelegateWrapper.Create(capturedValue, onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AllCleanupCallback<TArg> GetOrCreate<TArg>(Func<TArg, int, Promise> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreateWithIndex(DelegateWrapper.Create(onCleanup));
        }

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AllCleanupCallback<TArg> GetOrCreate<TArg, TCapture>(in TCapture capturedValue, Func<TCapture, TArg, int, Promise> onCleanup)
        {
            if (onCleanup == null)
            {
                throw new ArgumentNullException(nameof(onCleanup), Internal.GetFormattedStacktrace(2));
            }

            return Helper<TArg>.GetOrCreateWithIndex(DelegateWrapper.Create(capturedValue, onCleanup));
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private static class Helper<TArg>
        {
            [MethodImpl(Internal.InlineOption)]
            internal static Internal.AllCleanupCallback<TArg, TDelegate> GetOrCreate<TDelegate>(in TDelegate callback)
                where TDelegate : IFunc<TArg, Promise>
                => Internal.AllCleanupCallback<TArg, TDelegate>.GetOrCreate(callback);

            [MethodImpl(Internal.InlineOption)]
            internal static Internal.AllCleanupWithIndexCallback<TArg, TDelegate> GetOrCreateWithIndex<TDelegate>(in TDelegate callback)
                where TDelegate : IFunc<TArg, int, Promise>
                => Internal.AllCleanupWithIndexCallback<TArg, TDelegate>.GetOrCreate(callback);
        }
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class AllCleanupCallback<TArg> : HandleablePromiseBase
        {
            protected TempCollectionBuilder<int> _resolvedIndices;

            internal void AddResolvedIndex(int index)
                => _resolvedIndices.Add(index);

            internal ReadOnlySpan<int> ResolvedIndices
                => _resolvedIndices.ReadOnlySpan;

            internal abstract Promise Invoke(in TArg arg, int index);
            internal abstract void Dispose();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AllCleanupCallback<TArg, TDelegate> : AllCleanupCallback<TArg>
            where TDelegate : IFunc<TArg, Promise>
        {
            private TDelegate _callback;

            [MethodImpl(InlineOption)]
            private static AllCleanupCallback<TArg, TDelegate> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AllCleanupCallback<TArg, TDelegate>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new AllCleanupCallback<TArg, TDelegate>()
                    : obj.UnsafeAs<AllCleanupCallback<TArg, TDelegate>>();
            }

            [MethodImpl(InlineOption)]
            internal static AllCleanupCallback<TArg, TDelegate> GetOrCreate(in TDelegate callback)
            {
                var cb = GetOrCreate();
                cb._next = null;
                cb._resolvedIndices = new TempCollectionBuilder<int>(0);
                cb._callback = callback;
                return cb;
            }

            internal override void Dispose()
            {
                ThrowIfInPool(this);

                _resolvedIndices.Dispose();
                _callback = default;
                ObjectPool.MaybeRepool(this);
            }

            internal override Promise Invoke(in TArg arg, int index)
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
        } // class AllCleanupCallback<TArg, TDelegate>

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AllCleanupWithIndexCallback<TArg, TDelegate> : AllCleanupCallback<TArg>
            where TDelegate : IFunc<TArg, int, Promise>
        {
            private TDelegate _callback;

            [MethodImpl(InlineOption)]
            private static AllCleanupWithIndexCallback<TArg, TDelegate> GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<AllCleanupWithIndexCallback<TArg, TDelegate>>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new AllCleanupWithIndexCallback<TArg, TDelegate>()
                    : obj.UnsafeAs<AllCleanupWithIndexCallback<TArg, TDelegate>>();
            }

            [MethodImpl(InlineOption)]
            internal static AllCleanupWithIndexCallback<TArg, TDelegate> GetOrCreate(in TDelegate callback)
            {
                var cb = GetOrCreate();
                cb._next = null;
                cb._resolvedIndices = new TempCollectionBuilder<int>(0);
                cb._callback = callback;
                return cb;
            }

            internal override void Dispose()
            {
                ThrowIfInPool(this);

                _resolvedIndices.Dispose();
                _callback = default;
                ObjectPool.MaybeRepool(this);
            }

            internal override Promise Invoke(in TArg arg, int index)
            {
                try
                {
                    return _callback.Invoke(arg, index);
                }
                catch (Exception e)
                {
                    return Promise.FromException(e);
                }
            }
        } // class AllCleanupWithIndexCallback<TArg, TDelegate>
    } // class Internal
} // namespace Proto.Promises