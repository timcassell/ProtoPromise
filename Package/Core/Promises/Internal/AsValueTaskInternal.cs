#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0090 // Use 'new(...)'

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
#endif

namespace Proto.Promises
{
#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
    partial class Internal
    {
        partial class PromiseRefBase
        {
            internal ValueTask ToValueTaskVoid(short id, bool suppressContextScheduling)
            {
                if (State == Promise.State.Resolved)
                {
                    MaybeMarkAwaitedAndDispose(id);
                    return new ValueTask();
                }

                var source = PooledValueTaskSource<VoidResult>.GetOrCreate(suppressContextScheduling);
                HookupNewWaiter(id, source);
                return source.TaskVoid;
            }

            partial class PromiseRef<TResult>
            {
                internal ValueTask<TResult> ToValueTask(short id)
                {
                    if (State == Promise.State.Resolved)
                    {
                        var result = GetResult<TResult>();
                        MaybeMarkAwaitedAndDispose(id);
                        return new ValueTask<TResult>(result);
                    }

                    var source = PooledValueTaskSource<TResult>.GetOrCreate(false);
                    HookupNewWaiter(id, source);
                    return source.Task;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class PooledValueTaskSource<TResult> : HandleablePromiseBase, IValueTaskSource, IValueTaskSource<TResult>
            {
                private ManualResetValueTaskSourceCore<TResult> _core;
                private ValueTaskSourceOnCompletedFlags _flagsMask;

                internal ValueTask TaskVoid
                {
                    [MethodImpl(InlineOption)]
                    get => new ValueTask(this, _core.Version);
                }

                internal ValueTask<TResult> Task
                {
                    [MethodImpl(InlineOption)]
                    get => new ValueTask<TResult>(this, _core.Version);
                }

                private PooledValueTaskSource() { }

                [MethodImpl(InlineOption)]
                private static PooledValueTaskSource<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PooledValueTaskSource<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PooledValueTaskSource<TResult>()
                        : obj.UnsafeAs<PooledValueTaskSource<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static PooledValueTaskSource<TResult> GetOrCreate(bool suppressContextScheduling)
                {
                    var source = GetOrCreate();
                    // If the promise we're converting to a ValueTask is already configured to execute on a certain context,
                    // we ignore the context scheduling of the ValueTask continuation, and continue synchronously.
                    source._flagsMask = suppressContextScheduling
                        ? ~ValueTaskSourceOnCompletedFlags.UseSchedulingContext
                        : ~ValueTaskSourceOnCompletedFlags.None;
                    return source;
                }

                [MethodImpl(InlineOption)]
                private void Dispose()
                {
                    _core.Reset();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
                    if (state == Promise.State.Resolved)
                    {
                        var result = handler.GetResult<TResult>();
                        handler.MaybeDispose();
                        _core.SetResult(result);
                    }
                    else
                    {
                        var exception = state == Promise.State.Rejected ? handler.RejectContainer.GetValueAsException() : Promise.CancelException();
                        handler.MaybeDispose();
                        _core.SetException(exception);
                    }
                }

                void IValueTaskSource.GetResult(short token)
                {
                    try
                    {
                        _core.GetResult(token);
                    }
                    finally
                    {
                        Dispose();
                    }
                }

                TResult IValueTaskSource<TResult>.GetResult(short token)
                {
                    try
                    {
                        return _core.GetResult(token);
                    }
                    finally
                    {
                        Dispose();
                    }
                }

                public ValueTaskSourceStatus GetStatus(short token)
                    => _core.GetStatus(token);

                public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
                    => _core.OnCompleted(continuation, state, token, flags & _flagsMask);
            }
        }
    } // class Internal
#endif // UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
}