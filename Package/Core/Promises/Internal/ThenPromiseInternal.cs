#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0016 // Use 'throw' expression

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
            private sealed partial class ThenPromise<TArg, TResult, TDelegate> : PromiseSingleAwait<TResult>
                where TDelegate : IFunc<TArg, TResult>
            {
                private ThenPromise() { }

                [MethodImpl(InlineOption)]
                private static ThenPromise<TArg, TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ThenPromise<TArg, TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ThenPromise<TArg, TResult, TDelegate>()
                        : obj.UnsafeAs<ThenPromise<TArg, TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ThenPromise<TArg, TResult, TDelegate> GetOrCreate(in TDelegate callback)
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
                    if (state == Promise.State.Resolved)
                    {
                        var arg = handler.GetResult<TArg>();
                        handler.MaybeDispose();
                        Invoke(arg, callback);
                        return;
                    }

                    HandleSelfWithoutResult(handler, state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ThenWaitPromise<TArg, TDelegate> : PromiseWaitPromise<VoidResult>
                where TDelegate : IFunc<TArg, Promise>
            {
                private ThenWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ThenWaitPromise<TArg, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ThenWaitPromise<TArg, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ThenWaitPromise<TArg, TDelegate>()
                        : obj.UnsafeAs<ThenWaitPromise<TArg, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ThenWaitPromise<TArg, TDelegate> GetOrCreate(in TDelegate callback)
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
                    if (state == Promise.State.Resolved)
                    {
                        var arg = handler.GetResult<TArg>();
                        handler.MaybeDispose();
                        InvokeAndAdoptVoid(arg, callback);
                        return;
                    }

                    HandleSelfWithoutResult(handler, state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ThenWaitPromise<TArg, TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<TArg, Promise<TResult>>
            {
                private ThenWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ThenWaitPromise<TArg, TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ThenWaitPromise<TArg, TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ThenWaitPromise<TArg, TResult, TDelegate>()
                        : obj.UnsafeAs<ThenWaitPromise<TArg, TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ThenWaitPromise<TArg, TResult, TDelegate> GetOrCreate(in TDelegate callback)
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
                    if (state == Promise.State.Resolved)
                    {
                        var arg = handler.GetResult<TArg>();
                        handler.MaybeDispose();
                        InvokeAndAdopt(arg, callback);
                        return;
                    }

                    HandleSelfWithoutResult(handler, state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ThenPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject> : PromiseSingleAwait<TResult>
                where TDelegateResolve : IFunc<TArg, TResult>
                where TDelegateReject : IFunc<TReject, TResult>
            {
                private ThenPromise() { }

                [MethodImpl(InlineOption)]
                private static ThenPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ThenPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ThenPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>()
                        : obj.UnsafeAs<ThenPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>>();
                }

                [MethodImpl(InlineOption)]
                internal static ThenPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject> GetOrCreate(in TDelegateResolve resolveCallback, in TDelegateReject rejectCallback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolveCallback = resolveCallback;
                    promise._rejectCallback = rejectCallback;
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

                    var resolveCallback = _resolveCallback;
                    _resolveCallback = default;
                    var rejectCallback = _rejectCallback;
                    _rejectCallback = default;
                    if (state == Promise.State.Resolved)
                    {
                        var arg = handler.GetResult<TArg>();
                        handler.MaybeDispose();
                        Invoke(arg, resolveCallback);
                        return;
                    }

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    if (state == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                    {
                        InvokeCatch(rejectArg, rejectCallback, rejectContainer);
                        return;
                    }

                    RejectContainer = rejectContainer;

                    // We handle next last, so that if the runtime wants to, it can tail-call optimize.
                    // Unfortunately, C# currently doesn't have a way to add the .tail prefix directly. https://github.com/dotnet/csharplang/discussions/8990
                    HandleNextInternal(state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ThenWaitPromise<TArg, TReject, TDelegateResolve, TDelegateReject> : PromiseWaitPromise<VoidResult>
                where TDelegateResolve : IFunc<TArg, Promise>
                where TDelegateReject : IFunc<TReject, Promise>
            {
                private ThenWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ThenWaitPromise<TArg, TReject, TDelegateResolve, TDelegateReject> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ThenWaitPromise<TArg, TReject, TDelegateResolve, TDelegateReject>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ThenWaitPromise<TArg, TReject, TDelegateResolve, TDelegateReject>()
                        : obj.UnsafeAs<ThenWaitPromise<TArg, TReject, TDelegateResolve, TDelegateReject>>();
                }

                [MethodImpl(InlineOption)]
                internal static ThenWaitPromise<TArg, TReject, TDelegateResolve, TDelegateReject> GetOrCreate(in TDelegateResolve resolveCallback, in TDelegateReject rejectCallback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolveCallback = resolveCallback;
                    promise._rejectCallback = rejectCallback;
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

                    var resolveCallback = _resolveCallback;
                    _resolveCallback = default;
                    var rejectCallback = _rejectCallback;
                    _rejectCallback = default;
                    if (state == Promise.State.Resolved)
                    {
                        var arg = handler.GetResult<TArg>();
                        handler.MaybeDispose();
                        InvokeAndAdoptVoid(arg, resolveCallback);
                        return;
                    }

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    if (state == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                    {
                        InvokeCatchAndAdoptVoid(rejectArg, rejectCallback, rejectContainer);
                        return;
                    }

                    RejectContainer = rejectContainer;

                    // We handle next last, so that if the runtime wants to, it can tail-call optimize.
                    // Unfortunately, C# currently doesn't have a way to add the .tail prefix directly. https://github.com/dotnet/csharplang/discussions/8990
                    HandleNextInternal(state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ThenWaitPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject> : PromiseWaitPromise<TResult>
                where TDelegateResolve : IFunc<TArg, Promise<TResult>>
                where TDelegateReject : IFunc<TReject, Promise<TResult>>
            {
                private ThenWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ThenWaitPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ThenWaitPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ThenWaitPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>()
                        : obj.UnsafeAs<ThenWaitPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject>>();
                }

                [MethodImpl(InlineOption)]
                internal static ThenWaitPromise<TArg, TResult, TReject, TDelegateResolve, TDelegateReject> GetOrCreate(in TDelegateResolve resolveCallback, in TDelegateReject rejectCallback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolveCallback = resolveCallback;
                    promise._rejectCallback = rejectCallback;
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

                    var resolveCallback = _resolveCallback;
                    _resolveCallback = default;
                    var rejectCallback = _rejectCallback;
                    _rejectCallback = default;
                    if (state == Promise.State.Resolved)
                    {
                        var arg = handler.GetResult<TArg>();
                        handler.MaybeDispose();
                        InvokeAndAdopt(arg, resolveCallback);
                        return;
                    }

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    if (state == Promise.State.Rejected && GetShouldInvokeOnRejected(rejectContainer, out TReject rejectArg))
                    {
                        InvokeCatchAndAdopt(rejectArg, rejectCallback, rejectContainer);
                        return;
                    }

                    RejectContainer = rejectContainer;

                    // We handle next last, so that if the runtime wants to, it can tail-call optimize.
                    // Unfortunately, C# currently doesn't have a way to add the .tail prefix directly. https://github.com/dotnet/csharplang/discussions/8990
                    HandleNextInternal(state);
                }
            }
        } // class PromiseRefBase
    } // class Internal
} // namespace Proto.Promises