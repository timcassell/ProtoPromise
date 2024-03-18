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
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal readonly partial struct AsyncStreamAwaiterForLinqExtension<T> : ICriticalNotifyCompletion, IPromiseAwaiter
            {
                private readonly AsyncEnumerableWithIterator<T> _target;
                private readonly int _enumerableId;

                [MethodImpl(InlineOption)]
                internal AsyncStreamAwaiterForLinqExtension(AsyncEnumerableWithIterator<T> target, int enumerableId)
                {
                    _target = target;
                    _enumerableId = enumerableId;
                    CreateOverride();
                }

                static partial void CreateOverride();

#if !NETCOREAPP
                // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
                [MethodImpl(InlineOption)]
                static partial void CreateOverride()
#else
                static AsyncStreamAwaiterForLinqExtension()
#endif
                {
                    AwaitOverriderImpl<AsyncStreamAwaiterForLinqExtension<T>>.Create();
                }
#endif

                [MethodImpl(InlineOption)]
                public AsyncStreamAwaiterForLinqExtension<T> GetAwaiter() => this;

                public bool IsCompleted
                {
                    [MethodImpl(InlineOption)]
                    get { return false; }
                }

                [MethodImpl(InlineOption)]
                public void GetResult()
                {
                    // Reset in case the async iterator function completes synchronously from Start.
                    _target.ResetWithoutStacktrace();
                    // Don't throw for dispose.
                }

                [MethodImpl(InlineOption)]
                void IPromiseAwaiter.AwaitOnCompletedInternal(PromiseRefBase asyncPromiseRef)
                    => _target.AwaitOnCompletedForAsyncStreamYielder(asyncPromiseRef, _enumerableId, hasValue: false);

                void INotifyCompletion.OnCompleted(Action continuation) => throw new System.InvalidOperationException("Must only be used in async Linq extension methods.");
                void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) => throw new System.InvalidOperationException("Must only be used in async Linq extension methods.");
            }
        }
    }
}