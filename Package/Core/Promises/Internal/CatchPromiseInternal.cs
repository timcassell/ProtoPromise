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
            partial class PromiseSingleAwait<TResult>
            {
                protected void InvokeCatch<TArg, TDelegate>(in TArg arg, in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IFunc<TArg, TResult>
                {
                    Promise.State state;
                    SetCurrentInvoker(this);
                    try
                    {
                        _result = callback.Invoke(arg);
                        state = Promise.State.Resolved;
                    }
                    catch (RethrowException)
                    {
                        RejectContainer = rejectContainer;
                        state = Promise.State.Rejected;
                    }
                    catch (OperationCanceledException)
                    {
                        state = Promise.State.Canceled;
                    }
                    catch (Exception e)
                    {
                        RejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        state = Promise.State.Rejected;
                    }
                    finally
                    {
                        ClearCurrentInvoker();
                    }

                    // We handle next last, so that if the runtime wants to, it can tail-call optimize.
                    // Unfortunately, C# currently doesn't have a way to add the .tail prefix directly. https://github.com/dotnet/csharplang/discussions/8990
                    HandleNextInternal(state);
                }
            }

            partial class PromiseWaitPromise<TResult>
            {
                protected void InvokeCatchAndAdoptVoid<TArg, TDelegate>(in TArg arg, in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IFunc<TArg, Promise>
                {
                    Promise.State state;
                    SetCurrentInvoker(this);
                    try
                    {
                        var result = callback.Invoke(arg);
                        ValidateReturn(result);

                        this.SetPrevious(result._ref);
                        if (result._ref == null)
                        {
                            state = Promise.State.Resolved;
                        }
                        else
                        {
                            PromiseRefBase promiseSingleAwait = result._ref.AddWaiter(result._id, this, out var previousWaiter);
                            if (previousWaiter == PendingAwaitSentinel.s_instance)
                            {
                                return;
                            }
                            state = VerifyAndGetResultFromComplete(result._ref, promiseSingleAwait);
                        }
                    }
                    catch (RethrowException)
                    {
                        RejectContainer = rejectContainer;
                        state = Promise.State.Rejected;
                    }
                    catch (OperationCanceledException)
                    {
                        state = Promise.State.Canceled;
                    }
                    catch (Exception e)
                    {
                        RejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        state = Promise.State.Rejected;
                    }
                    finally
                    {
                        ClearCurrentInvoker();
                    }

                    // We handle next last, so that if the runtime wants to, it can tail-call optimize.
                    // Unfortunately, C# currently doesn't have a way to add the .tail prefix directly. https://github.com/dotnet/csharplang/discussions/8990
                    HandleNextInternal(state);
                }

                protected void InvokeCatchAndAdopt<TArg, TDelegate>(in TArg arg, in TDelegate callback, IRejectContainer rejectContainer)
                    where TDelegate : IFunc<TArg, Promise<TResult>>
                {
                    Promise.State state;
                    SetCurrentInvoker(this);
                    try
                    {
                        var result = callback.Invoke(arg);
                        ValidateReturn(result);

                        this.SetPrevious(result._ref);
                        if (result._ref == null)
                        {
                            _result = result._result;
                            state = Promise.State.Resolved;
                        }
                        else
                        {
                            PromiseRefBase promiseSingleAwait = result._ref.AddWaiter(result._id, this, out var previousWaiter);
                            if (previousWaiter == PendingAwaitSentinel.s_instance)
                            {
                                return;
                            }
                            state = VerifyAndGetResultFromComplete(result._ref, promiseSingleAwait);
                        }
                    }
                    catch (RethrowException)
                    {
                        RejectContainer = rejectContainer;
                        state = Promise.State.Rejected;
                    }
                    catch (OperationCanceledException)
                    {
                        state = Promise.State.Canceled;
                    }
                    catch (Exception e)
                    {
                        RejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        state = Promise.State.Rejected;
                    }
                    finally
                    {
                        ClearCurrentInvoker();
                    }

                    // We handle next last, so that if the runtime wants to, it can tail-call optimize.
                    // Unfortunately, C# currently doesn't have a way to add the .tail prefix directly. https://github.com/dotnet/csharplang/discussions/8990
                    HandleNextInternal(state);
                }
            }

            private static bool GetShouldInvokeOnRejected<TReject>(IRejectContainer rejectContainer, out TReject rejectArg)
            {
                if (null != default(TReject) && typeof(TReject) == typeof(VoidResult))
                {
                    rejectArg = default;
                    return true;
                }
                if (rejectContainer.Value is TReject reject)
                {
                    rejectArg = reject;
                    return true;
                }
                rejectArg = default;
                return false;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CatchPromise<TResult, TReject, TDelegate> : PromiseSingleAwait<TResult>
                where TDelegate : IFunc<TReject, TResult>
            {
                private CatchPromise() { }

                [MethodImpl(InlineOption)]
                private static CatchPromise<TResult, TReject, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CatchPromise<TResult, TReject, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CatchPromise<TResult, TReject, TDelegate>()
                        : obj.UnsafeAs<CatchPromise<TResult, TReject, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CatchPromise<TResult, TReject, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    var callback = _callback;
                    _callback = default;
                    var rejectContainer = handler.RejectContainer;
                    if (state != Promise.State.Rejected || !GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                    {
                        HandleSelf(handler, state);
                        return;
                    }

                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    InvokeCatch(rejectArg, callback, rejectContainer);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CatchWaitPromise<TReject, TDelegate> : PromiseWaitPromise<VoidResult>
                where TDelegate : IFunc<TReject, Promise>
            {
                private CatchWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CatchWaitPromise<TReject, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CatchWaitPromise<TReject, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CatchWaitPromise<TReject, TDelegate>()
                        : obj.UnsafeAs<CatchWaitPromise<TReject, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CatchWaitPromise<TReject, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_firstContinue)
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    var callback = _callback;
                    _callback = default;
                    var rejectContainer = handler.RejectContainer;
                    if (state != Promise.State.Rejected || !GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                    {
                        HandleSelf(handler, state);
                        return;
                    }

                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    InvokeCatchAndAdoptVoid(rejectArg, callback, rejectContainer);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CatchWaitPromise<TResult, TReject, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<TReject, Promise<TResult>>
            {
                private CatchWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CatchWaitPromise<TResult, TReject, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CatchWaitPromise<TResult, TReject, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CatchWaitPromise<TResult, TReject, TDelegate>()
                        : obj.UnsafeAs<CatchWaitPromise<TResult, TReject, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CatchWaitPromise<TResult, TReject, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_firstContinue)
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    var callback = _callback;
                    _callback = default;
                    var rejectContainer = handler.RejectContainer;
                    if (state != Promise.State.Rejected || !GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                    {
                        HandleSelf(handler, state);
                        return;
                    }

                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    InvokeCatchAndAdopt(rejectArg, callback, rejectContainer);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CatchCancelationPromise<TResult, TDelegate> : PromiseSingleAwait<TResult>
                where TDelegate : IFunc<VoidResult, TResult>
            {
                private CatchCancelationPromise() { }

                [MethodImpl(InlineOption)]
                private static CatchCancelationPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CatchCancelationPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CatchCancelationPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<CatchCancelationPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CatchCancelationPromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    var callback = _callback;
                    _callback = default;
                    if (state != Promise.State.Canceled)
                    {
                        HandleSelf(handler, state);
                        return;
                    }

                    handler.MaybeDispose();
                    Invoke(default(VoidResult), callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CatchCancelationWaitPromise<TDelegate> : PromiseWaitPromise<VoidResult>
                where TDelegate : IFunc<VoidResult, Promise>
            {
                private CatchCancelationWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CatchCancelationWaitPromise<TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CatchCancelationWaitPromise<TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CatchCancelationWaitPromise<TDelegate>()
                        : obj.UnsafeAs<CatchCancelationWaitPromise<TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CatchCancelationWaitPromise<TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_firstContinue)
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    var callback = _callback;
                    _callback = default;
                    if (state != Promise.State.Canceled)
                    {
                        HandleSelf(handler, state);
                        return;
                    }

                    handler.MaybeDispose();
                    InvokeAndAdoptVoid(default(VoidResult), callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CatchCancelationWaitPromise<TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<VoidResult, Promise<TResult>>
            {
                private CatchCancelationWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CatchCancelationWaitPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CatchCancelationWaitPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CatchCancelationWaitPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<CatchCancelationWaitPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CatchCancelationWaitPromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_firstContinue)
                    {
                        HandleSelf(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    var callback = _callback;
                    _callback = default;
                    if (state != Promise.State.Canceled)
                    {
                        HandleSelf(handler, state);
                        return;
                    }

                    handler.MaybeDispose();
                    InvokeAndAdopt(default(VoidResult), callback);
                }
            }
        } // class PromiseRefBase
    } // class Internal
} // namespace Proto.Promises