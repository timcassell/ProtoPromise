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
            private sealed partial class FinallyPromise<TResult, TDelegate> : SingleAwaitPromise<TResult>
                where TDelegate : IAction
            {
                private FinallyPromise() { }

                [MethodImpl(InlineOption)]
                private static FinallyPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<FinallyPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new FinallyPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<FinallyPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> New(Promise previous, in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.ResetWithSentinel();
                    promise._callback = callback;
                    promise.SetPrevious(previous._ref);
                    previous._ref.HookupNewWaiter(previous._id, promise);
                    return new Promise<TResult>(promise);
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

                    // Take the trace before the field is overwritten with the reject container.
                    var trace = TakeCallbackTrace();
                    _result = handler.GetResult<TResult>();
                    RejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    var callback = _callback;
                    _callback = default;
                    var scope = Diagnostics.CausalityTrace.EnterScopeIfNotNull(trace);
                    try
                    {
                        callback.Invoke();
                    }
                    catch (Exception e)
                    {
                        // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                        if (state == Promise.State.Rejected)
                        {
                            RejectContainer.ReportUnhandled();
                        }
                        if (e is OperationCanceledException)
                        {
                            RejectContainer = null;
                            state = Promise.State.Canceled;
                        }
                        else
                        {
                            RejectContainer = CreateRejectContainer(e, false, this);
                            state = Promise.State.Rejected;
                        }
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                    HandleNextInternal(state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class FinallyWaitPromise<TResult, TDelegate> : CallbackWaitPromiseBase<TResult>
                where TDelegate : IFunc<Promise>
            {
                private FinallyWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static FinallyWaitPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<FinallyWaitPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new FinallyWaitPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<FinallyWaitPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> New(Promise previous, in TDelegate callback)
                {
                    var promise = GetOrCreate();
                    promise.ResetWithSentinel();
                    promise._callback = callback;
                    promise.SetPrevious(previous._ref);
                    previous._ref.HookupNewWaiter(previous._id, promise);
                    return new Promise<TResult>(promise);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> New(Promise previous, Promise.State previousState, IRejectContainer previousRejectContainer, in TResult previousResult)
                {
                    // This doesn't invoke a callback, so it doesn't need the traced sentinel.
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._previousState = previousState;
                    promise.RejectContainer = previousRejectContainer;
                    promise._result = previousResult;
                    promise._firstContinue = false;
                    promise.SetPrevious(previous._ref);
                    previous._ref.HookupNewWaiter(previous._id, promise);
                    return new Promise<TResult>(promise);
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
                        // The returned promise is handling this.
                        HandleFromReturnedPromise(handler, state);
                        return;
                    }
                    _firstContinue = false;

                    // Take the trace before the field is overwritten with the reject container.
                    var trace = TakeCallbackTrace();
                    _result = handler.GetResult<TResult>();
                    RejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    var callback = _callback;
                    _callback = default;
                    Promise result;
                    var scope = Diagnostics.CausalityTrace.EnterScopeIfNotNull(trace);
                    try
                    {
                        result = callback.Invoke();
                        ValidateReturn(result);

                        this.SetPrevious(result._ref);
                        if (result._ref != null)
                        {
                            PromiseRefBase promiseSingleAwait = result._ref.AddWaiter(result._id, this, out var previousWaiter);
                            if (previousWaiter == PendingAwaitSentinel.s_instance)
                            {
                                // Store the state until the returned promise is complete.
                                _previousState = state;
                                return;
                            }
                            state = VerifyAndGetResultFromComplete(result._ref, promiseSingleAwait, state);
                        }
                    }
                    catch (Exception e)
                    {
                        // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                        if (state == Promise.State.Rejected)
                        {
                            RejectContainer.ReportUnhandled();
                        }
                        if (e is OperationCanceledException)
                        {
                            RejectContainer = null;
                            state = Promise.State.Canceled;
                        }
                        else
                        {
                            RejectContainer = CreateRejectContainer(e, false, this);
                            state = Promise.State.Rejected;
                        }
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                    HandleNextInternal(state);
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private Promise.State VerifyAndGetResultFromComplete(PromiseRefBase completePromise, PromiseRefBase promiseSingleAwait, Promise.State previousState)
                {
                    if (VerifyWaiter(promiseSingleAwait))
                    {
                        completePromise.WaitUntilStateIsNotPending();
                        var state = completePromise.State;
                        var rejectContainer = completePromise.RejectContainer;
                        completePromise.SuppressRejection = true;
                        completePromise.MaybeDispose();
                        if (state == Promise.State.Resolved)
                        {
                            return previousState;
                        }
                        // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                        if (previousState == Promise.State.Rejected)
                        {
                            RejectContainer.ReportUnhandled();
                        }
                        RejectContainer = rejectContainer;
                        return state;
                    }

                    var exception = new InvalidReturnException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", string.Empty);
                    RejectContainer = CreateRejectContainer(exception, false, this);
                    return Promise.State.Rejected;
                }

                private void HandleFromReturnedPromise(PromiseRefBase handler, Promise.State state)
                {
                    if (state == Promise.State.Resolved)
                    {
                        state = _previousState;
                    }
                    else
                    {
                        if (_previousState == Promise.State.Rejected)
                        {
                            // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                            RejectContainer.ReportUnhandled();
                        }
                        RejectContainer = handler.RejectContainer;
                    }
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    HandleNextInternal(state);
                }
            }
        } // class PromiseRefBase
    } // class Internal
} // namespace Proto.Promises