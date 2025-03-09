#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0251 // Make member 'readonly'

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
            internal virtual void MaybeReportUnhandledAndDispose(Promise.State state)
            {
                // PromiseSingleAwait

                MaybeReportUnhandledRejection(state);
                MaybeDispose();
            }

            partial class PromiseMultiAwait<TResult>
            {
                internal override void MaybeReportUnhandledAndDispose(Promise.State state)
                    // We don't report unhandled rejection here unless none of the waiters suppressed.
                    // This way we only report it once in case multiple waiters were canceled.
                    => MaybeDispose();
            }

            partial class PromiseRetainer<TResult>
            {
                internal override void MaybeReportUnhandledAndDispose(Promise.State state)
                    // We don't report unhandled rejection here unless none of the waiters suppressed.
                    // This way we only report it once in case multiple waiters were canceled.
                    => MaybeDispose();
            }

            internal partial struct CancelationHelper
            {
                internal bool IsCompleted
                {
                    [MethodImpl(InlineOption)]
                    get => _isCompletedFlag != 0;
                }

                [MethodImpl(InlineOption)]
                internal void Reset(int retainCounter = 2)
                {
                    _isCompletedFlag = 0;
                    _retainCounter = retainCounter;
                }

                [MethodImpl(InlineOption)]
                internal void Register(CancelationToken cancelationToken, ICancelable owner)
                    => _cancelationRegistration = cancelationToken.Register(owner);

                [MethodImpl(InlineOption)]
                internal void RegisterWithoutImmediateInvoke(CancelationToken cancelationToken, ICancelable owner, out bool alreadyCanceled)
                    => _cancelationRegistration = cancelationToken.RegisterWithoutImmediateInvoke(owner, out alreadyCanceled);

                [MethodImpl(InlineOption)]
                internal bool TrySetCompleted()
                    => Interlocked.Exchange(ref _isCompletedFlag, 1) == 0;

                [MethodImpl(InlineOption)]
                internal void UnregisterAndWait()
                    => _cancelationRegistration.Dispose();

                [MethodImpl(InlineOption)]
                internal void Retain()
                    => InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);

                [MethodImpl(InlineOption)]
                internal bool TryRelease(int releaseCount = -1)
                    => InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, releaseCount) == 0;

                [MethodImpl(InlineOption)]
                internal void RetainUnchecked(int retainCount)
                    => Interlocked.Add(ref _retainCounter, retainCount);

                [MethodImpl(InlineOption)]
                internal bool TryReleaseUnchecked()
                    => Interlocked.Add(ref _retainCounter, -1) == 0;

                // As an optimization, we can skip one Interlocked operation if the async op completed before the cancelation callback.
                [MethodImpl(InlineOption)]
                internal void ReleaseOne()
                    => _retainCounter = 1;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinuePromise<TArg, TResult, TContinuer> : ContinuePromiseBase<TArg, TResult, TContinuer>, ICancelable
                where TContinuer : IContinuer<TArg, TResult>
            {
                private CancelableContinuePromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinuePromise<TArg, TResult, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinuePromise<TArg, TResult, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinuePromise<TArg, TResult, TContinuer>()
                        : obj.UnsafeAs<CancelableContinuePromise<TArg, TResult, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinuePromise<TArg, TResult, TContinuer> GetOrCreate(in TContinuer continuer)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._continuer = continuer;
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                        _cancelationHelper = default;
                        _continuer = default;
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

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
            } // class CancelableContinuePromise<TArg, TResult, TContinuer>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinueWaitPromise<TArg, TContinuer> : ContinueWaitPromiseBase<TArg, TContinuer>, ICancelable
                where TContinuer : IContinuer<TArg, Promise>
            {
                private CancelableContinueWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinueWaitPromise<TArg, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinueWaitPromise<TArg, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinueWaitPromise<TArg, TContinuer>()
                        : obj.UnsafeAs<CancelableContinueWaitPromise<TArg, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinueWaitPromise<TArg, TContinuer> GetOrCreate(in TContinuer continuer)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._continuer = continuer;
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
                    _continuer = default;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    if (!_firstContinue)
                    {
                        handler.SetCompletionState(state);
                        HandleSelfWithoutResult(handler, state);
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
            } // class CancelableContinueWaitPromise<TArg, TContinuer>

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelableContinueWaitPromise<TArg, TResult, TContinuer> : ContinueWaitPromiseBase<TArg, TResult, TContinuer>, ICancelable
                where TContinuer : IContinuer<TArg, Promise<TResult>>
            {
                private CancelableContinueWaitPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelableContinueWaitPromise<TArg, TResult, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelableContinueWaitPromise<TArg, TResult, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelableContinueWaitPromise<TArg, TResult, TContinuer>()
                        : obj.UnsafeAs<CancelableContinueWaitPromise<TArg, TResult, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelableContinueWaitPromise<TArg, TResult, TContinuer> GetOrCreate(in TContinuer continuer)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._continuer = continuer;
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
                    _continuer = default;
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
            } // class CancelableContinueWaitPromise<TArg, TContinuer>
        } // PromiseRefBase
    } // Internal
} // namespace Proto.Promises