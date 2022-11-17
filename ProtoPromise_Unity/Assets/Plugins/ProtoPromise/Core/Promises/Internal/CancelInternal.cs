#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
            internal virtual void ReportUnhandledAndMaybeDispose()
            {
                // PromiseSingleAwait

                // Rejection maybe wasn't caught.
                if (State == Promise.State.Rejected & !SuppressRejection)
                {
                    SuppressRejection = true;
                    _rejectContainerOrPreviousOrLink.UnsafeAs<IRejectContainer>().ReportUnhandled();
                }
                MaybeDispose();
            }

            partial class PromiseMultiAwait<TResult>
            {
                internal override void ReportUnhandledAndMaybeDispose()
                {
                    // We don't report unhandled rejection here unless none of the waiters suppressed.
                    // This way we only report it once in case multiple waiters were canceled.
                    MaybeDispose();
                }
            }

            internal partial struct CancelationHelper
            {
                [MethodImpl(InlineOption)]
                internal void Reset()
                {
                    // _retainCounter is necessary to make sure the promise is disposed after the cancelation has invoked or unregistered,
                    // and the previous promise has handled this.
                    _retainCounter = 2;
                }

                [MethodImpl(InlineOption)]
                internal void Register(CancelationToken cancelationToken, ICancelable owner)
                {
                    cancelationToken.TryRegister(owner, out _cancelationRegistration);
                }

                internal bool TryUnregister(PromiseRefBase owner)
                {
                    ThrowIfInPool(owner);
                    return TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & owner.State == Promise.State.Pending;
                }

                [MethodImpl(InlineOption)]
                internal bool TryRelease()
                {
                    return InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0;
                }
            }

            [MethodImpl(InlineOption)]
            protected void HandleFromCancelation()
            {
                ThrowIfInPool(this);
                HandleNextInternal(null, Promise.State.Canceled);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelablePromiseResolve<TResult, TResolver> : PromiseSingleAwait<TResult>, ICancelable
                where TResolver : IDelegateResolveOrCancel
            {
                private CancelablePromiseResolve() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolve<TResult, TResolver> GetOrCreate(TResolver resolver, ushort depth)
                {
                    var promise = ObjectPool.TryTake<CancelablePromiseResolve<TResult, TResolver>>()
                        ?? new CancelablePromiseResolve<TResult, TResolver>();
                    promise.Reset(depth);
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
                    _cancelationHelper = default(CancelationHelper);
                    _resolver = default(TResolver);
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Resolved)
                    {
                        _cancelationHelper.TryRelease();
                        resolveCallback.InvokeResolver(handler, this);
                    }
                    else if (unregistered)
                    {
                        _cancelationHelper.TryRelease();
                        HandleNextFromHandler(handler);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.ReportUnhandledAndMaybeDispose();
                    }
                }

                void ICancelable.Cancel()
                {
                    HandleFromCancelation();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelablePromiseResolvePromise<TResult, TResolver> : PromiseWaitPromise<TResult>, ICancelable
                where TResolver : IDelegateResolveOrCancelPromise
            {
                private CancelablePromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolvePromise<TResult, TResolver> GetOrCreate(TResolver resolver, ushort depth)
                {
                    var promise = ObjectPool.TryTake<CancelablePromiseResolvePromise<TResult, TResolver>>()
                        ?? new CancelablePromiseResolvePromise<TResult, TResolver>();
                    promise.Reset(depth);
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
                    _cancelationHelper = default(CancelationHelper);
                    _resolver = default(TResolver);
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Resolved)
                    {
                        _cancelationHelper.TryRelease();
                        resolveCallback.InvokeResolver(handler, this);
                    }
                    else if (unregistered)
                    {
                        _cancelationHelper.TryRelease();
                        HandleNextFromHandler(handler);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.ReportUnhandledAndMaybeDispose();
                    }
                }

                void ICancelable.Cancel()
                {
                    HandleFromCancelation();
                }
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
                internal static CancelablePromiseResolveReject<TResult, TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, ushort depth)
                {
                    var promise = ObjectPool.TryTake<CancelablePromiseResolveReject<TResult, TResolver, TRejecter>>()
                        ?? new CancelablePromiseResolveReject<TResult, TResolver, TRejecter>();
                    promise.Reset(depth);
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
                    _cancelationHelper = default(CancelationHelper);
                    _resolver = default(TResolver);
                    _rejecter = default(TRejecter);
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
                        resolveCallback.InvokeResolver(handler, this);
                    }
                    else if (!unregistered)
                    {
                        MaybeDispose();
                        handler.ReportUnhandledAndMaybeDispose();
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        handler.SuppressRejection = true;
                        _cancelationHelper.TryRelease();
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(handler, this);
                    }
                    else
                    {
                        _cancelationHelper.TryRelease();
                        HandleNextFromHandler(handler);
                    }
                }

                void ICancelable.Cancel()
                {
                    HandleFromCancelation();
                }
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
                internal static CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, ushort depth)
                {
                    var promise = ObjectPool.TryTake<CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter>>()
                        ?? new CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter>();
                    promise.Reset(depth);
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
                    _cancelationHelper = default(CancelationHelper);
                    _resolver = default(TResolver);
                    _rejecter = default(TRejecter);
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Resolved)
                    {
                        _cancelationHelper.TryRelease();
                        resolveCallback.InvokeResolver(handler, this);
                    }
                    else if (!unregistered)
                    {
                        MaybeDispose();
                        handler.ReportUnhandledAndMaybeDispose();
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        handler.SuppressRejection = true;
                        _cancelationHelper.TryRelease();
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(handler, this);
                    }
                    else
                    {
                        _cancelationHelper.TryRelease();
                        HandleNextFromHandler(handler);
                    }
                }

                void ICancelable.Cancel()
                {
                    HandleFromCancelation();
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
                internal static CancelablePromiseContinue<TResult, TContinuer> GetOrCreate(TContinuer continuer, ushort depth)
                {
                    var promise = ObjectPool.TryTake<CancelablePromiseContinue<TResult, TContinuer>>()
                        ?? new CancelablePromiseContinue<TResult, TContinuer>();
                    promise.Reset(depth);
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
                    _cancelationHelper = default(CancelationHelper);
                    _continuer = default(TContinuer);
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        handler.SuppressRejection = true;
                        _cancelationHelper.TryRelease();
                        _continuer.Invoke(handler, this);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.ReportUnhandledAndMaybeDispose();
                    }
                }

                void ICancelable.Cancel()
                {
                    HandleFromCancelation();
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
                internal static CancelablePromiseContinuePromise<TResult, TContinuer> GetOrCreate(TContinuer continuer, ushort depth)
                {
                    var promise = ObjectPool.TryTake<CancelablePromiseContinuePromise<TResult, TContinuer>>()
                        ?? new CancelablePromiseContinuePromise<TResult, TContinuer>();
                    promise.Reset(depth);
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
                    _cancelationHelper = default(CancelationHelper);
                    _continuer = default(TContinuer);
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        handler.SuppressRejection = true;
                        _cancelationHelper.TryRelease();
                        callback.Invoke(handler, this);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.ReportUnhandledAndMaybeDispose();
                    }
                }

                void ICancelable.Cancel()
                {
                    HandleFromCancelation();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelablePromiseCancel<TResult, TCanceler> : PromiseSingleAwait<TResult>, ICancelable
                where TCanceler : IDelegateResolveOrCancel
            {
                private CancelablePromiseCancel() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseCancel<TResult, TCanceler> GetOrCreate(TCanceler canceler, ushort depth)
                {
                    var promise = ObjectPool.TryTake<CancelablePromiseCancel<TResult, TCanceler>>()
                        ?? new CancelablePromiseCancel<TResult, TCanceler>();
                    promise.Reset(depth);
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
                    _cancelationHelper = default(CancelationHelper);
                    _canceler = default(TCanceler);
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    var callback = _canceler;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Canceled)
                    {
                        _cancelationHelper.TryRelease();
                        callback.InvokeResolver(handler, this);
                    }
                    else if (unregistered)
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelf(handler);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.ReportUnhandledAndMaybeDispose();
                    }
                }

                void ICancelable.Cancel()
                {
                    HandleFromCancelation();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class CancelablePromiseCancelPromise<TResult, TCanceler> : PromiseWaitPromise<TResult>, ICancelable
                where TCanceler : IDelegateResolveOrCancelPromise
            {
                private CancelablePromiseCancelPromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseCancelPromise<TResult, TCanceler> GetOrCreate(TCanceler canceler, ushort depth)
                {
                    var promise = ObjectPool.TryTake<CancelablePromiseCancelPromise<TResult, TCanceler>>()
                        ?? new CancelablePromiseCancelPromise<TResult, TCanceler>();
                    promise.Reset(depth);
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
                    _cancelationHelper = default(CancelationHelper);
                    _canceler = default(TCanceler);
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_canceler.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler);
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Canceled)
                    {
                        _cancelationHelper.TryRelease();
                        callback.InvokeResolver(handler, this);
                    }
                    else if (unregistered)
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelf(handler);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.ReportUnhandledAndMaybeDispose();
                    }
                }

                void ICancelable.Cancel()
                {
                    HandleFromCancelation();
                }
            }
        } // PromiseRefBase
    } // Internal
}