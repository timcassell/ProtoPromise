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
                    => cancelationToken.TryRegister(owner, out _cancelationRegistration);

                [MethodImpl(InlineOption)]
                internal void RegisterWithoutImmediateInvoke(CancelationToken cancelationToken, ICancelable owner, out bool alreadyCanceled)
                    => cancelationToken.TryRegisterWithoutImmediateInvoke(owner, out _cancelationRegistration, out alreadyCanceled);

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
            internal sealed partial class PromiseDuplicateCancel<TResult> : PromiseSingleAwait<TResult>, ICancelable
            {
                private PromiseDuplicateCancel() { }

                internal override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                        _cancelationHelper = default;
                        ObjectPool.MaybeRepool(this);
                    }
                }

                [MethodImpl(InlineOption)]
                private static PromiseDuplicateCancel<TResult> GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseDuplicateCancel<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseDuplicateCancel<TResult>()
                        : obj.UnsafeAs<PromiseDuplicateCancel<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseDuplicateCancel<TResult> GetOrCreate()
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    promise._cancelationHelper.Reset();
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        _cancelationHelper.UnregisterAndWait();
                        _cancelationHelper.ReleaseOne();
                        HandleSelf(handler, state);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
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
            private sealed partial class CancelablePromiseContinue<TResult, TContinuer> : PromiseSingleAwait<TResult>, ICancelable
                where TContinuer : IDelegateContinue
            {
                private CancelablePromiseContinue() { }

                [MethodImpl(InlineOption)]
                private static CancelablePromiseContinue<TResult, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseContinue<TResult, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseContinue<TResult, TContinuer>()
                        : obj.UnsafeAs<CancelablePromiseContinue<TResult, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseContinue<TResult, TContinuer> GetOrCreate(TContinuer continuer)
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

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        _cancelationHelper.UnregisterAndWait();
                        _cancelationHelper.ReleaseOne();
                        handler.SuppressRejection = true;
                        _continuer.Invoke(handler, handler.RejectContainer, state, this);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
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
            private sealed partial class CancelablePromiseContinuePromise<TResult, TContinuer> : PromiseWaitPromise<TResult>, ICancelable
                where TContinuer : IDelegateContinuePromise
            {
                private CancelablePromiseContinuePromise() { }

                [MethodImpl(InlineOption)]
                private static CancelablePromiseContinuePromise<TResult, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseContinuePromise<TResult, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseContinuePromise<TResult, TContinuer>()
                        : obj.UnsafeAs<CancelablePromiseContinuePromise<TResult, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseContinuePromise<TResult, TContinuer> GetOrCreate(TContinuer continuer)
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

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, state);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default;
                    if (_cancelationHelper.TrySetCompleted())
                    {
                        _cancelationHelper.UnregisterAndWait();
                        _cancelationHelper.ReleaseOne();
                        handler.SuppressRejection = true;
                        callback.Invoke(handler, handler.RejectContainer, state, this);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
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
        } // PromiseRefBase
    } // Internal
} // namespace Proto.Promises