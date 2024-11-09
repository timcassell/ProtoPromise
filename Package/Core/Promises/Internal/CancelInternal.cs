#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;

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
                [MethodImpl(InlineOption)]
                internal void Reset()
                    // _retainCounter is necessary to make sure the promise is disposed after the cancelation has invoked or unregistered,
                    // and the previous promise has handled this.
                    => _retainCounter = 2;

                [MethodImpl(InlineOption)]
                internal void Register(CancelationToken cancelationToken, ICancelable owner)
                    => cancelationToken.TryRegister(owner, out _cancelationRegistration);

                internal bool TryUnregister(PromiseRefBase owner)
                {
                    ThrowIfInPool(owner);
                    return TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & owner.State == Promise.State.Pending;
                }

                [MethodImpl(InlineOption)]
                internal bool TryRelease()
                    => InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0;
            }

            [MethodImpl(InlineOption)]
            protected void HandleFromCancelation()
            {
                ThrowIfInPool(this);
                HandleNextInternal(Promise.State.Canceled);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelablePromiseResolve<TResult, TResolver> : PromiseSingleAwait<TResult>, ICancelable
                where TResolver : IDelegateResolveOrCancel
            {
                private CancelablePromiseResolve() { }

                [MethodImpl(InlineOption)]
                private static CancelablePromiseResolve<TResult, TResolver> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseResolve<TResult, TResolver>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseResolve<TResult, TResolver>()
                        : obj.UnsafeAs<CancelablePromiseResolve<TResult, TResolver>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolve<TResult, TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolver = resolver;
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
                    _resolver = default;
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Resolved)
                    {
                        _cancelationHelper.TryRelease();
                        resolveCallback.InvokeResolver(handler, state, this);
                    }
                    else if (unregistered)
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelfWithoutResult(handler, state);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
                }

                void ICancelable.Cancel() => HandleFromCancelation();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelablePromiseResolvePromise<TResult, TResolver> : PromiseWaitPromise<TResult>, ICancelable
                where TResolver : IDelegateResolveOrCancelPromise
            {
                private CancelablePromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                private static CancelablePromiseResolvePromise<TResult, TResolver> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseResolvePromise<TResult, TResolver>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseResolvePromise<TResult, TResolver>()
                        : obj.UnsafeAs<CancelablePromiseResolvePromise<TResult, TResolver>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolvePromise<TResult, TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolver = resolver;
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
                    _resolver = default;
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, state);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Resolved)
                    {
                        _cancelationHelper.TryRelease();
                        resolveCallback.InvokeResolver(handler, state, this);
                    }
                    else if (unregistered)
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelfWithoutResult(handler, state);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
                }

                void ICancelable.Cancel() => HandleFromCancelation();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelablePromiseResolveReject<TResult, TResolver, TRejecter> : PromiseSingleAwait<TResult>, ICancelable
                where TResolver : IDelegateResolveOrCancel
                where TRejecter : IDelegateReject
            {
                private CancelablePromiseResolveReject() { }

                [MethodImpl(InlineOption)]
                private static CancelablePromiseResolveReject<TResult, TResolver, TRejecter> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseResolveReject<TResult, TResolver, TRejecter>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseResolveReject<TResult, TResolver, TRejecter>()
                        : obj.UnsafeAs<CancelablePromiseResolveReject<TResult, TResolver, TRejecter>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolveReject<TResult, TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
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
                    _resolver = default;
                    _rejecter = default;
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    var rejectCallback = _rejecter;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Resolved)
                    {
                        _cancelationHelper.TryRelease();
                        resolveCallback.InvokeResolver(handler, state, this);
                    }
                    else if (!unregistered)
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        var rejectContainer = handler.RejectContainer;
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
                        _cancelationHelper.TryRelease();
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(rejectContainer, this);
                    }
                    else
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelfWithoutResult(handler, state);
                    }
                }

                void ICancelable.Cancel() => HandleFromCancelation();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter> : PromiseWaitPromise<TResult>, ICancelable
                where TResolver : IDelegateResolveOrCancelPromise
                where TRejecter : IDelegateRejectPromise
            {
                private CancelablePromiseResolveRejectPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter>()
                        : obj.UnsafeAs<CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
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
                    _resolver = default;
                    _rejecter = default;
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, state);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default;
                    var rejectCallback = _rejecter;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Resolved)
                    {
                        _cancelationHelper.TryRelease();
                        resolveCallback.InvokeResolver(handler, state, this);
                    }
                    else if (!unregistered)
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        handler.SuppressRejection = true;
                        _cancelationHelper.TryRelease();
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(handler, handler.RejectContainer, this);
                    }
                    else
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelfWithoutResult(handler, state);
                    }
                }

                void ICancelable.Cancel() => HandleFromCancelation();
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
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        handler.SuppressRejection = true;
                        _cancelationHelper.TryRelease();
                        _continuer.Invoke(handler, handler.RejectContainer, state, this);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
                }

                void ICancelable.Cancel() => HandleFromCancelation();
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
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        handler.SuppressRejection = true;
                        _cancelationHelper.TryRelease();
                        callback.Invoke(handler, handler.RejectContainer, state, this);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
                }

                void ICancelable.Cancel() => HandleFromCancelation();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelablePromiseCancel<TResult, TCanceler> : PromiseSingleAwait<TResult>, ICancelable
                where TCanceler : IDelegateResolveOrCancel
            {
                private CancelablePromiseCancel() { }

                [MethodImpl(InlineOption)]
                private static CancelablePromiseCancel<TResult, TCanceler> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseCancel<TResult, TCanceler>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseCancel<TResult, TCanceler>()
                        : obj.UnsafeAs<CancelablePromiseCancel<TResult, TCanceler>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseCancel<TResult, TCanceler> GetOrCreate(TCanceler canceler)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._canceler = canceler;
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
                    _canceler = default;
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    var callback = _canceler;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Canceled)
                    {
                        _cancelationHelper.TryRelease();
                        callback.InvokeResolver(handler, state, this);
                    }
                    else if (unregistered)
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelf(handler, state);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
                }

                void ICancelable.Cancel() => HandleFromCancelation();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelablePromiseCancelPromise<TResult, TCanceler> : PromiseWaitPromise<TResult>, ICancelable
                where TCanceler : IDelegateResolveOrCancelPromise
            {
                private CancelablePromiseCancelPromise() { }

                [MethodImpl(InlineOption)]
                private static CancelablePromiseCancelPromise<TResult, TCanceler> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseCancelPromise<TResult, TCanceler>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseCancelPromise<TResult, TCanceler>()
                        : obj.UnsafeAs<CancelablePromiseCancelPromise<TResult, TCanceler>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseCancelPromise<TResult, TCanceler> GetOrCreate(TCanceler canceler)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._canceler = canceler;
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
                    _canceler = default;
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_canceler.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, state);
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Canceled)
                    {
                        _cancelationHelper.TryRelease();
                        callback.InvokeResolver(handler, state, this);
                    }
                    else if (unregistered)
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelf(handler, state);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(state);
                    }
                }

                void ICancelable.Cancel() => HandleFromCancelation();
            }
        } // PromiseRefBase
    } // Internal
}