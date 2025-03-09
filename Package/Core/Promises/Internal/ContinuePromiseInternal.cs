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
            // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
            [MethodImpl(MethodImplOptions.NoInlining)]
            private Promise.State VerifyAndGetResultFromComplete(PromiseRefBase completePromise, PromiseRefBase promiseSingleAwait)
            {
                if (VerifyWaiter(promiseSingleAwait))
                {
                    completePromise.WaitUntilStateIsNotPending();
                    RejectContainer = completePromise.RejectContainer;
                    completePromise.SuppressRejection = true;
                    var state = completePromise.State;
                    completePromise.MaybeDispose();
                    return state;
                }

                var exception = new InvalidReturnException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", string.Empty);
                RejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                return Promise.State.Rejected;
            }

            partial class PromiseWaitPromise<TResult>
            {
                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                protected Promise.State VerifyAndGetResultFromComplete(PromiseRef<TResult> completePromise, PromiseRefBase promiseSingleAwait)
                {
                    if (VerifyWaiter(promiseSingleAwait))
                    {
                        completePromise.WaitUntilStateIsNotPending();
                        RejectContainer = completePromise.RejectContainer;
                        completePromise.SuppressRejection = true;
                        _result = completePromise._result;
                        var state = completePromise.State;
                        completePromise.MaybeDispose();
                        return state;
                    }

                    var exception = new InvalidReturnException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", string.Empty);
                    RejectContainer = CreateRejectContainer(exception, int.MinValue, null, this);
                    return Promise.State.Rejected;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private abstract partial class ContinuePromiseBase<TArg, TResult, TContinuer> : PromiseSingleAwait<TResult>
                where TContinuer : IContinuer<TArg, TResult>
            {
                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);
                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;

                    var continuer = _continuer;
                    _continuer = default;
                    if (continuer.ShouldInvoke(rejectContainer, state, out bool isCatch))
                    {
                        var arg = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                        handler.MaybeDispose();
                        SetCurrentInvoker(this);
                        try
                        {
                            _result = continuer.Invoke(arg);
                            state = Promise.State.Resolved;
                        }
                        catch (RethrowException) when (isCatch)
                        {
                            RejectContainer = rejectContainer;
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
                        ClearCurrentInvoker();
                    }
                    else
                    {
                        if (isCatch)
                        {
                            _result = handler.GetResult<TResult>();
                        }
                        handler.MaybeDispose();
                        RejectContainer = rejectContainer;
                    }

                    // We call HandleNextInternal last, so that if the runtime wants to, it can tail-call optimize.
                    // Unfortunately, C# currently doesn't have a way to add the .tail prefix directly. https://github.com/dotnet/csharplang/discussions/8990
                    HandleNextInternal(state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private abstract partial class ContinueWaitPromiseBase<TArg, TContinuer> : PromiseWaitPromise<VoidResult>
                where TContinuer : IContinuer<TArg, Promise>
            {
                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);
                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;

                    bool firstContinue = _firstContinue;
                    _firstContinue = false;
                    var continuer = _continuer;
                    _continuer = default;
                    if (firstContinue & continuer.ShouldInvoke(rejectContainer, state, out bool isCatch))
                    {
                        var arg = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                        handler.MaybeDispose();
                        SetCurrentInvoker(this);
                        try
                        {
                            var promise = continuer.Invoke(arg);
                            ValidateReturn(promise);

                            this.SetPrevious(promise._ref);
                            if (promise._ref == null)
                            {
                                state = Promise.State.Resolved;
                            }
                            else
                            {
                                PromiseRefBase promiseSingleAwait = promise._ref.AddWaiter(promise._id, this, out var previousWaiter);
                                if (previousWaiter == PendingAwaitSentinel.s_instance)
                                {
                                    return;
                                }
                                state = VerifyAndGetResultFromComplete(promise._ref, promiseSingleAwait);
                            }
                        }
                        catch (RethrowException) when (isCatch)
                        {
                            RejectContainer = rejectContainer;
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
                    }
                    else
                    {
                        handler.MaybeDispose();
                        RejectContainer = rejectContainer;
                    }

                    // We call HandleNextInternal last, so that if the runtime wants to, it can tail-call optimize.
                    // Unfortunately, C# currently doesn't have a way to add the .tail prefix directly. https://github.com/dotnet/csharplang/discussions/8990
                    HandleNextInternal(state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private abstract partial class ContinueWaitPromiseBase<TArg, TResult, TContinuer> : PromiseWaitPromise<TResult>
                where TContinuer : IContinuer<TArg, Promise<TResult>>
            {
                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);
                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;

                    bool firstContinue = _firstContinue;
                    _firstContinue = false;
                    var continuer = _continuer;
                    _continuer = default;
                    if (firstContinue & continuer.ShouldInvoke(rejectContainer, state, out bool isCatch))
                    {
                        var arg = new Promise<TArg>.ResultContainer(handler.GetResult<TArg>(), rejectContainer, state);
                        handler.MaybeDispose();
                        SetCurrentInvoker(this);
                        try
                        {
                            var promise = continuer.Invoke(arg);
                            ValidateReturn(promise);

                            this.SetPrevious(promise._ref);
                            if (promise._ref == null)
                            {
                                _result = promise._result;
                                state = Promise.State.Resolved;
                            }
                            else
                            {
                                PromiseRefBase promiseSingleAwait = promise._ref.AddWaiter(promise._id, this, out var previousWaiter);
                                if (previousWaiter == PendingAwaitSentinel.s_instance)
                                {
                                    return;
                                }
                                state = VerifyAndGetResultFromComplete(promise._ref, promiseSingleAwait);
                            }
                        }
                        catch (RethrowException) when (isCatch)
                        {
                            RejectContainer = rejectContainer;
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
                    }
                    else
                    {
                        if (!firstContinue | isCatch)
                        {
                            _result = handler.GetResult<TResult>();
                        }
                        handler.MaybeDispose();
                        RejectContainer = rejectContainer;
                    }

                    // We call HandleNextInternal last, so that if the runtime wants to, it can tail-call optimize.
                    // Unfortunately, C# currently doesn't have a way to add the .tail prefix directly. https://github.com/dotnet/csharplang/discussions/8990
                    HandleNextInternal(state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinuePromise<TArg, TResult, TContinuer> : ContinuePromiseBase<TArg, TResult, TContinuer>
                where TContinuer : IContinuer<TArg, TResult>
            {
                private ContinuePromise() { }

                [MethodImpl(InlineOption)]
                private static ContinuePromise<TArg, TResult, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinuePromise<TArg, TResult, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinuePromise<TArg, TResult, TContinuer>()
                        : obj.UnsafeAs<ContinuePromise<TArg, TResult, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinuePromise<TArg, TResult, TContinuer> GetOrCreate(in TContinuer continuer)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._continuer = continuer;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinueWaitPromise<TArg, TContinuer> : ContinueWaitPromiseBase<TArg, TContinuer>
                where TContinuer : IContinuer<TArg, Promise>
            {
                private ContinueWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ContinueWaitPromise<TArg, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinueWaitPromise<TArg, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinueWaitPromise<TArg, TContinuer>()
                        : obj.UnsafeAs<ContinueWaitPromise<TArg, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinueWaitPromise<TArg, TContinuer> GetOrCreate(in TContinuer continuer)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._continuer = continuer;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ContinueWaitPromise<TArg, TResult, TContinuer> : ContinueWaitPromiseBase<TArg, TResult, TContinuer>
                where TContinuer : IContinuer<TArg, Promise<TResult>>
            {
                private ContinueWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static ContinueWaitPromise<TArg, TResult, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinueWaitPromise<TArg, TResult, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinueWaitPromise<TArg, TResult, TContinuer>()
                        : obj.UnsafeAs<ContinueWaitPromise<TArg, TResult, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinueWaitPromise<TArg, TResult, TContinuer> GetOrCreate(in TContinuer continuer)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._continuer = continuer;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }
            }
        } // class PromiseRefBase
    } // class Internal
} // namespace Proto.Promises