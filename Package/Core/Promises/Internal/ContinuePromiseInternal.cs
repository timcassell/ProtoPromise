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
            partial class PromiseWaitPromise<TResult>
            {
                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                protected Promise.State VerifyAndGetResultFromComplete(PromiseRefBase completePromise, PromiseRefBase promiseSingleAwait)
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
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            // The generics are flattened instead of nested because Unity IL2CPP has a maximum nested generic depth.
            // Continuer and transformers are constrained to struct because old runtimes don't support static interface methods.
            private abstract partial class ContinuePromiseBase<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer> : PromiseWaitPromise<TResult>
                where TDelegate : IFunc<TDelegateArg, TDelegateResult>
                where TContinuer : struct, IContinuer
                where TArgTransformer : struct, ITransformer<Promise<TArg>.ResultContainer, TDelegateArg>
                where TResultTransformer : struct, ITransformer<TDelegateResult, PromiseWrapper<TResult>>
            {
                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);
                    var rejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;

                    bool firstContinue = _firstContinue;
                    _firstContinue = false;
                    var callback = _callback;
                    _callback = default;
                    if (firstContinue & default(TContinuer).ShouldInvoke(rejectContainer, state, out var invokeType))
                    {
                        var arg = handler.GetResult<TArg>();
                        handler.MaybeDispose();
                        SetCurrentInvoker(this);
                        try
                        {
                            var delArg = default(TArgTransformer).Transform(new Promise<TArg>.ResultContainer(arg, rejectContainer, state));
                            var delResult = callback.Invoke(delArg);
                            var promise = default(TResultTransformer).Transform(delResult);
                            ValidateReturn(promise._ref, promise._id);

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
                        catch (RethrowException e)
                        {
                            // Old Unity IL2CPP doesn't support catch `when` filters, so we have to check it inside the catch block.
                            if (state == Promise.State.Rejected && invokeType == Promise.State.Rejected)
                            {
                                RejectContainer = rejectContainer;
                            }
                            else
                            {
                                RejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                                state = Promise.State.Rejected;
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
                    }
                    else
                    {
                        if (invokeType != Promise.State.Resolved)
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
            private sealed partial class ContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>
                : ContinuePromiseBase<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>
                where TDelegate : IFunc<TDelegateArg, TDelegateResult>
                where TContinuer : struct, IContinuer
                where TArgTransformer : struct, ITransformer<Promise<TArg>.ResultContainer, TDelegateArg>
                where TResultTransformer : struct, ITransformer<TDelegateResult, PromiseWrapper<TResult>>
            {
                private ContinuePromise() { }

                [MethodImpl(InlineOption)]
                private static ContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>()
                        : obj.UnsafeAs<ContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>>();
                }

                [MethodImpl(InlineOption)]
                internal static ContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer> GetOrCreate(in TDelegate callback)
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
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>
                : ContinuePromiseBase<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>, ICancelable
                where TDelegate : IFunc<TDelegateArg, TDelegateResult>
                where TContinuer : struct, IContinuer
                where TArgTransformer : struct, ITransformer<Promise<TArg>.ResultContainer, TDelegateArg>
                where TResultTransformer : struct, ITransformer<TDelegateResult, PromiseWrapper<TResult>>
            {
                private CancelableContinuePromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>()
                        : obj.UnsafeAs<CancelableContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinuePromise<TArg, TResult, TDelegateArg, TDelegateResult, TDelegate, TContinuer, TArgTransformer, TResultTransformer> GetOrCreate(in TDelegate callback)
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

                    if (!_firstContinue)
                    {
                        handler.SetCompletionState(state);
                        HandleSelf(handler, state);
                        return;
                    }

                    if (!_cancelationHelper.TrySetCompleted())
                    {
                        handler.SetCompletionState(state);
                        handler.MaybeReportUnhandledAndDispose(state);
                        MaybeDispose();
                        return;
                    }

                    _cancelationHelper.UnregisterAndWait();
                    _cancelationHelper.ReleaseOne();

                    base.Handle(handler, state);
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