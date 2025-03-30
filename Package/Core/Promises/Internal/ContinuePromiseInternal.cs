#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0016 // Use 'throw' expression
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0290 // Use primary constructor

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    // Unity IL2CPP has a maximum nested generic depth, so unfortunately we have to create separate delegate wrappers,
    // so the generic will only nest <TArg> instead of <Promise<TArg>.ResultContainer>
    partial class DelegateWrapper
    {
        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateResultContainerArgVoid<TArg> Create<TArg>(Action<Promise<TArg>.ResultContainer> callback)
            => new Internal.DelegateResultContainerArgVoid<TArg>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateResultContainerArgResult<TArg, TResult> Create<TArg, TResult>(Func<Promise<TArg>.ResultContainer, TResult> callback)
            => new Internal.DelegateResultContainerArgResult<TArg, TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateResultContainerCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Action<TCapture, Promise<TArg>.ResultContainer> callback)
            => new Internal.DelegateResultContainerCaptureArgVoid<TCapture, TArg>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.DelegateResultContainerCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, TResult> callback)
            => new Internal.DelegateResultContainerCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateResultContainerArgVoid<TArg> Create<TArg>(Func<Promise<TArg>.ResultContainer, Promise> callback)
            => new Internal.AsyncDelegateResultContainerArgVoid<TArg>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateResultContainerArgResult<TArg, TResult> Create<TArg, TResult>(Func<Promise<TArg>.ResultContainer, Promise<TResult>> callback)
            => new Internal.AsyncDelegateResultContainerArgResult<TArg, TResult>(callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateResultContainerCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise> callback)
            => new Internal.AsyncDelegateResultContainerCaptureArgVoid<TCapture, TArg>(capturedValue, callback);

        [MethodImpl(Internal.InlineOption)]
        internal static Internal.AsyncDelegateResultContainerCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise<TResult>> callback)
            => new Internal.AsyncDelegateResultContainerCaptureArgResult<TCapture, TArg, TResult>(capturedValue, callback);
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerArgVoid<TArg> : IAction<Promise<TArg>.ResultContainer>,
            IFunc<Promise<TArg>.ResultContainer, VoidResult>
        {
            private readonly Action<Promise<TArg>.ResultContainer> _callback;

            [MethodImpl(InlineOption)]
            public DelegateResultContainerArgVoid(Action<Promise<TArg>.ResultContainer> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public void Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);

            [MethodImpl(InlineOption)]
            VoidResult IFunc<Promise<TArg>.ResultContainer, VoidResult>.Invoke(in Promise<TArg>.ResultContainer arg)
            {
                _callback.Invoke(arg);
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerArgResult<TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, TResult>
        {
            private readonly Func<Promise<TArg>.ResultContainer, TResult> _callback;

            [MethodImpl(InlineOption)]
            public DelegateResultContainerArgResult(Func<Promise<TArg>.ResultContainer, TResult> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public TResult Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerCaptureArgVoid<TCapture, TArg> : IAction<Promise<TArg>.ResultContainer>,
            IFunc<Promise<TArg>.ResultContainer, VoidResult>
        {
            private readonly Action<TCapture, Promise<TArg>.ResultContainer> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateResultContainerCaptureArgVoid(in TCapture capturedValue, Action<TCapture, Promise<TArg>.ResultContainer> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public void Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);

            [MethodImpl(InlineOption)]
            VoidResult IFunc<Promise<TArg>.ResultContainer, VoidResult>.Invoke(in Promise<TArg>.ResultContainer arg)
            {
                _callback.Invoke(_capturedValue, arg);
                return default;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct DelegateResultContainerCaptureArgResult<TCapture, TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, TResult>
        {
            private readonly Func<TCapture, Promise<TArg>.ResultContainer, TResult> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public DelegateResultContainerCaptureArgResult(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, TResult> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public TResult Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateResultContainerArgVoid<TArg> : IFunc<Promise<TArg>.ResultContainer, Promise>
        {
            private readonly Func<Promise<TArg>.ResultContainer, Promise> _callback;

            [MethodImpl(InlineOption)]
            public AsyncDelegateResultContainerArgVoid(Func<Promise<TArg>.ResultContainer, Promise> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public Promise Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateResultContainerArgResult<TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
        {
            private readonly Func<Promise<TArg>.ResultContainer, Promise<TResult>> _callback;

            [MethodImpl(InlineOption)]
            public AsyncDelegateResultContainerArgResult(Func<Promise<TArg>.ResultContainer, Promise<TResult>> callback)
                => _callback = callback;

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateResultContainerCaptureArgVoid<TCapture, TArg> : IFunc<Promise<TArg>.ResultContainer, Promise>
        {
            private readonly Func<TCapture, Promise<TArg>.ResultContainer, Promise> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public AsyncDelegateResultContainerCaptureArgVoid(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal readonly struct AsyncDelegateResultContainerCaptureArgResult<TCapture, TArg, TResult> : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
        {
            private readonly Func<TCapture, Promise<TArg>.ResultContainer, Promise<TResult>> _callback;
            private readonly TCapture _capturedValue;

            [MethodImpl(InlineOption)]
            public AsyncDelegateResultContainerCaptureArgResult(in TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, Promise<TResult>> callback)
            {
                _callback = callback;
                _capturedValue = capturedValue;
            }

            [MethodImpl(InlineOption)]
            public Promise<TResult> Invoke(in Promise<TArg>.ResultContainer arg)
                => _callback.Invoke(_capturedValue, arg);
        }

        partial class PromiseRefBase
        {
            partial class PromiseSingleAwait<TResult>
            {
                [MethodImpl(InlineOption)]
                protected void Invoke<TArg, TDelegate>(in TArg arg, in TDelegate callback)
                    where TDelegate : IFunc<TArg, TResult>
                {
                    Promise.State state;
                    SetCurrentInvoker(this);
                    try
                    {
                        _result = callback.Invoke(arg);
                        state = Promise.State.Resolved;
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
                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private Promise.State VerifyAndGetResultFromComplete(PromiseRefBase completePromise, PromiseRefBase promiseSingleAwait)
                {
                    if (VerifyWaiter(promiseSingleAwait))
                    {
                        completePromise.WaitUntilStateIsNotPending();
                        RejectContainer = completePromise.RejectContainer;
                        completePromise.SuppressRejection = true;
                        _result = completePromise.GetResult<TResult>();
                        var state = completePromise.State;
                        completePromise.MaybeDispose();
                        return state;
                    }

                    var exception = new InvalidReturnException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", string.Empty);
                    RejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                    return Promise.State.Rejected;
                }

                [MethodImpl(InlineOption)]
                protected void InvokeAndAdoptVoid<TArg, TDelegate>(in TArg arg, in TDelegate callback)
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

                [MethodImpl(InlineOption)]
                protected void InvokeAndAdopt<TArg, TDelegate>(in TArg arg, in TDelegate callback)
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

            // Ideally all of these would simply be `ContinuePromise<TArg, TResult, TDelegate>`,
            // but `Promise.ResultContainer` is not the same type as `Promise<VoidResult>.ResultContainer`,
            // and `Promise` is not the same type as `Promise<VoidResult>`.
            // There would be much less code duplication if C# supported void in generics, but we work with what's available.
            // At least we can still use synchronous `VoidResult` return in place of true `void`.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinueVoidResultPromise<TResult, TDelegate> : PromiseSingleAwait<TResult>
                where TDelegate : IFunc<Promise.ResultContainer, TResult>
            {
                private ContinueVoidResultPromise() { }

                [MethodImpl(InlineOption)]
                private static ContinueVoidResultPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinueVoidResultPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinueVoidResultPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<ContinueVoidResultPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinueVoidResultPromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
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
                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    Invoke(new Promise.ResultContainer(rejectContainer, state), callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinueVoidVoidWaitPromise<TDelegate> : PromiseWaitPromise<VoidResult>
                where TDelegate : IFunc<Promise.ResultContainer, Promise>
            {
                private ContinueVoidVoidWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ContinueVoidVoidWaitPromise<TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinueVoidVoidWaitPromise<TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinueVoidVoidWaitPromise<TDelegate>()
                        : obj.UnsafeAs<ContinueVoidVoidWaitPromise<TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinueVoidVoidWaitPromise<TDelegate> GetOrCreate(in TDelegate callback)
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

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdoptVoid(new Promise.ResultContainer(rejectContainer, state), callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinueVoidResultWaitPromise<TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
            {
                private ContinueVoidResultWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ContinueVoidResultWaitPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinueVoidResultWaitPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinueVoidResultWaitPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<ContinueVoidResultWaitPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinueVoidResultWaitPromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
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

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise.ResultContainer(rejectContainer, state), callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinueArgResultPromise<TArg, TResult, TDelegate> : PromiseSingleAwait<TResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
            {
                private ContinueArgResultPromise() { }

                [MethodImpl(InlineOption)]
                private static ContinueArgResultPromise<TArg, TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinueArgResultPromise<TArg, TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinueArgResultPromise<TArg, TResult, TDelegate>()
                        : obj.UnsafeAs<ContinueArgResultPromise<TArg, TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinueArgResultPromise<TArg, TResult, TDelegate> GetOrCreate(in TDelegate callback)
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
                    var rejectContainer = handler.RejectContainer;
                    var arg = handler.GetResult<TArg>();
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    Invoke(new Promise<TArg>.ResultContainer(arg, rejectContainer, state), callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinueArgVoidWaitPromise<TArg, TDelegate> : PromiseWaitPromise<VoidResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
            {
                private ContinueArgVoidWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ContinueArgVoidWaitPromise<TArg, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinueArgVoidWaitPromise<TArg, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinueArgVoidWaitPromise<TArg, TDelegate>()
                        : obj.UnsafeAs<ContinueArgVoidWaitPromise<TArg, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinueArgVoidWaitPromise<TArg, TDelegate> GetOrCreate(in TDelegate callback)
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

                    var rejectContainer = handler.RejectContainer;
                    var arg = handler.GetResult<TArg>();
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdoptVoid(new Promise<TArg>.ResultContainer(arg, rejectContainer, state), callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinueArgResultWaitPromise<TArg, TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
            {
                private ContinueArgResultWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ContinueArgResultWaitPromise<TArg, TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinueArgResultWaitPromise<TArg, TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinueArgResultWaitPromise<TArg, TResult, TDelegate>()
                        : obj.UnsafeAs<ContinueArgResultWaitPromise<TArg, TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinueArgResultWaitPromise<TArg, TResult, TDelegate> GetOrCreate(in TDelegate callback)
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

                    var rejectContainer = handler.RejectContainer;
                    var arg = handler.GetResult<TArg>();
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise<TArg>.ResultContainer(arg, rejectContainer, state), callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinueVoidResultPromise<TResult, TDelegate> : PromiseSingleAwait<TResult>, ICancelable
                where TDelegate : IFunc<Promise.ResultContainer, TResult>
            {
                private CancelableContinueVoidResultPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinueVoidResultPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinueVoidResultPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinueVoidResultPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<CancelableContinueVoidResultPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinueVoidResultPromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default;
                    _callback = default;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    Invoke(new Promise.ResultContainer(rejectContainer, state), callback);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinueVoidVoidWaitPromise<TDelegate> : PromiseWaitPromise<VoidResult>, ICancelable
                where TDelegate : IFunc<Promise.ResultContainer, Promise>
            {
                private CancelableContinueVoidVoidWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinueVoidVoidWaitPromise<TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinueVoidVoidWaitPromise<TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinueVoidVoidWaitPromise<TDelegate>()
                        : obj.UnsafeAs<CancelableContinueVoidVoidWaitPromise<TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinueVoidVoidWaitPromise<TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default;
                    _callback = default;
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

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdoptVoid(new Promise.ResultContainer(rejectContainer, state), callback);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinueVoidResultWaitPromise<TResult, TDelegate> : PromiseWaitPromise<TResult>, ICancelable
                where TDelegate : IFunc<Promise.ResultContainer, Promise<TResult>>
            {
                private CancelableContinueVoidResultWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinueVoidResultWaitPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinueVoidResultWaitPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinueVoidResultWaitPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<CancelableContinueVoidResultWaitPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinueVoidResultWaitPromise<TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default;
                    _callback = default;
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

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise.ResultContainer(rejectContainer, state), callback);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinueArgResultPromise<TArg, TResult, TDelegate> : PromiseSingleAwait<TResult>, ICancelable
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, TResult>
            {
                private CancelableContinueArgResultPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinueArgResultPromise<TArg, TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinueArgResultPromise<TArg, TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinueArgResultPromise<TArg, TResult, TDelegate>()
                        : obj.UnsafeAs<CancelableContinueArgResultPromise<TArg, TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinueArgResultPromise<TArg, TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default;
                    _callback = default;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    var rejectContainer = handler.RejectContainer;
                    var arg = handler.GetResult<TArg>();
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    Invoke(new Promise<TArg>.ResultContainer(arg, rejectContainer, state), callback);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinueArgVoidWaitPromise<TArg, TDelegate> : PromiseWaitPromise<VoidResult>, ICancelable
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise>
            {
                private CancelableContinueArgVoidWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinueArgVoidWaitPromise<TArg, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinueArgVoidWaitPromise<TArg, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinueArgVoidWaitPromise<TArg, TDelegate>()
                        : obj.UnsafeAs<CancelableContinueArgVoidWaitPromise<TArg, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinueArgVoidWaitPromise<TArg, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default;
                    _callback = default;
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

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    var rejectContainer = handler.RejectContainer;
                    var arg = handler.GetResult<TArg>();
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdoptVoid(new Promise<TArg>.ResultContainer(arg, rejectContainer, state), callback);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinueArgResultWaitPromise<TArg, TResult, TDelegate> : PromiseWaitPromise<TResult>, ICancelable
                where TDelegate : IFunc<Promise<TArg>.ResultContainer, Promise<TResult>>
            {
                private CancelableContinueArgResultWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinueArgResultWaitPromise<TArg, TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinueArgResultWaitPromise<TArg, TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinueArgResultWaitPromise<TArg, TResult, TDelegate>()
                        : obj.UnsafeAs<CancelableContinueArgResultWaitPromise<TArg, TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinueArgResultWaitPromise<TArg, TResult, TDelegate> GetOrCreate(in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._callback = callback;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                    }
                }

                new private void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default;
                    _callback = default;
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

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    var rejectContainer = handler.RejectContainer;
                    var arg = handler.GetResult<TArg>();
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();

                    var callback = _callback;
                    _callback = default;
                    InvokeAndAdopt(new Promise<TArg>.ResultContainer(arg, rejectContainer, state), callback);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                }
            }
        } // class PromiseRefBase
    } // class Internal
} // namespace Proto.Promises