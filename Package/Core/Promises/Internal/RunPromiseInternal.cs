#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RunPromise<TResult, TDelegate> : PromiseSingleAwait<TResult>
                where TDelegate : IFunc<VoidResult, TResult>
            {
                private RunPromise() { }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static RunPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RunPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RunPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<RunPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static RunPromise<TResult, TDelegate> GetOrCreate(TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal void ScheduleOnContext(SynchronizationContext context)
                    => ScheduleContextCallback(context, this,
                        obj => obj.UnsafeAs<RunPromise<TResult, TDelegate>>().Run(),
                        obj => obj.UnsafeAs<RunPromise<TResult, TDelegate>>().Run()
                    );

                private void Run()
                {
                    ThrowIfInPool(this);

                    var callback = _callback;
                    _callback = default;
                    Invoke(default(VoidResult), callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RunWaitPromise<TDelegate> : PromiseWaitPromise<VoidResult>
                where TDelegate : IFunc<VoidResult, Promise>
            {
                private RunWaitPromise() { }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static RunWaitPromise<TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RunWaitPromise<TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RunWaitPromise<TDelegate>()
                        : obj.UnsafeAs<RunWaitPromise<TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static RunWaitPromise<TDelegate> GetOrCreate(TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal void ScheduleOnContext(SynchronizationContext context)
                    => ScheduleContextCallback(context, this,
                        obj => obj.UnsafeAs<RunWaitPromise<TDelegate>>().Run(),
                        obj => obj.UnsafeAs<RunWaitPromise<TDelegate>>().Run()
                    );

                private void Run()
                {
                    ThrowIfInPool(this);

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdoptVoid(default(VoidResult), callback);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    // The returned promise is handling this.
                    handler.SetCompletionState(state);
                    HandleSelf(handler, state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RunWaitPromise<TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<VoidResult, Promise<TResult>>
            {
                private RunWaitPromise() { }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static RunWaitPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RunWaitPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RunWaitPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<RunWaitPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static RunWaitPromise<TResult, TDelegate> GetOrCreate(TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal void ScheduleOnContext(SynchronizationContext context)
                    => ScheduleContextCallback(context, this,
                        obj => obj.UnsafeAs<RunWaitPromise<TResult, TDelegate>>().Run(),
                        obj => obj.UnsafeAs<RunWaitPromise<TResult, TDelegate>>().Run()
                    );

                private void Run()
                {
                    ThrowIfInPool(this);

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(default(VoidResult), callback);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    // The returned promise is handling this.
                    handler.SetCompletionState(state);
                    HandleSelf(handler, state);
                }
            }
        } // class PromiseRefBase
    } // class Internal
} // namespace Proto.Promises