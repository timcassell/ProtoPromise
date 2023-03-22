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

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
            internal virtual void MaybeReportUnhandledAndDispose(object rejectContainer, Promise.State state)
            {
                // PromiseSingleAwait

                MaybeReportUnhandledRejection(rejectContainer, state);
                MaybeDispose();
            }

            partial class PromiseMultiAwait<TResult>
            {
                internal override void MaybeReportUnhandledAndDispose(object rejectContainer, Promise.State state)
                {
                    // We don't report unhandled rejection here unless none of the waiters suppressed.
                    // This way we only report it once in case multiple waiters were canceled.
                    MaybeDispose();
                }
            }

            internal sealed partial class CanceledPromise<TResult> : PromiseRef<TResult>
            {
                private static readonly CanceledPromise<TResult> s_instance;

                static CanceledPromise()
                {
                    s_instance = new CanceledPromise<TResult>() { _next = InvalidAwaitSentinel.s_instance, _state = Promise.State.Canceled };
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
                    // If we don't suppress, the finalizer can run when the AppDomain is unloaded, causing a NullReferenceException. This happens in Unity when switching between editmode and playmode.
                    GC.SuppressFinalize(s_instance);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
                }

                internal static CanceledPromise<TResult> GetOrCreate()
                {
                    return s_instance;
                }

                internal override void MaybeDispose()
                {
                    // Do nothing.
                }

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    // Set the previous waiter to pending await sentinel so the caller will do nothing.
                    previousWaiter = PendingAwaitSentinel.s_instance;
                    // Immediately handle the waiter.
                    waiter.Handle(this, null, Promise.State.Canceled);
                    return null;
                }

                internal override bool GetIsCompleted(short promiseId)
                {
                    return true;
                }

                internal override PromiseRef<TResult> GetDuplicateT(short promiseId, ushort depth)
                {
                    return this;
                }

                internal override PromiseRefBase GetDuplicate(short promiseId, ushort depth)
                {
                    return this;
                }

                internal override bool GetIsValid(short promiseId)
                {
                    return promiseId == Id;
                }

                internal override void MaybeMarkAwaitedAndDispose(short promiseId)
                {
                    // Do nothing.
                }

                internal override void MaybeReportUnhandledAndDispose(object rejectContainer, Promise.State state)
                {
                    // Do nothing.
                }

                internal override void Forget(short promiseId)
                {
                    // Do nothing.
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
                private static CancelablePromiseResolve<TResult, TResolver> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseResolve<TResult, TResolver>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseResolve<TResult, TResolver>()
                        : obj.UnsafeAs<CancelablePromiseResolve<TResult, TResolver>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolve<TResult, TResolver> GetOrCreate(TResolver resolver, ushort depth)
                {
                    var promise = GetOrCreate();
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

                protected override void Execute(PromiseRefBase handler, object rejectContainer, Promise.State state, ref bool invokingRejected)
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
                        HandleSelfWithoutResult(handler, rejectContainer, state);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
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
                private static CancelablePromiseResolvePromise<TResult, TResolver> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseResolvePromise<TResult, TResolver>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseResolvePromise<TResult, TResolver>()
                        : obj.UnsafeAs<CancelablePromiseResolvePromise<TResult, TResolver>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolvePromise<TResult, TResolver> GetOrCreate(TResolver resolver, ushort depth)
                {
                    var promise = GetOrCreate();
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

                protected override void Execute(PromiseRefBase handler, object rejectContainer, Promise.State state, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, rejectContainer, state);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Resolved)
                    {
                        _cancelationHelper.TryRelease();
                        resolveCallback.InvokeResolver(handler, state, this);
                    }
                    else if (unregistered)
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelfWithoutResult(handler, rejectContainer, state);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
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
                private static CancelablePromiseResolveReject<TResult, TResolver, TRejecter> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseResolveReject<TResult, TResolver, TRejecter>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseResolveReject<TResult, TResolver, TRejecter>()
                        : obj.UnsafeAs<CancelablePromiseResolveReject<TResult, TResolver, TRejecter>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolveReject<TResult, TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, ushort depth)
                {
                    var promise = GetOrCreate();
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

                protected override void Execute(PromiseRefBase handler, object rejectContainer, Promise.State state, ref bool invokingRejected)
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
                        handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
                        _cancelationHelper.TryRelease();
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(rejectContainer, this);
                    }
                    else
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelfWithoutResult(handler, rejectContainer, state);
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
                private static CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter>()
                        : obj.UnsafeAs<CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolveRejectPromise<TResult, TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, ushort depth)
                {
                    var promise = GetOrCreate();
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

                protected override void Execute(PromiseRefBase handler, object rejectContainer, Promise.State state, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, rejectContainer, state);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
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
                        handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        handler.SuppressRejection = true;
                        _cancelationHelper.TryRelease();
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(handler, rejectContainer, this);
                    }
                    else
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelfWithoutResult(handler, rejectContainer, state);
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
                private static CancelablePromiseContinue<TResult, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseContinue<TResult, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseContinue<TResult, TContinuer>()
                        : obj.UnsafeAs<CancelablePromiseContinue<TResult, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseContinue<TResult, TContinuer> GetOrCreate(TContinuer continuer, ushort depth)
                {
                    var promise = GetOrCreate();
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

                protected override void Execute(PromiseRefBase handler, object rejectContainer, Promise.State state, ref bool invokingRejected)
                {
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        handler.SuppressRejection = true;
                        _cancelationHelper.TryRelease();
                        _continuer.Invoke(handler, rejectContainer, state, this);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
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
                private static CancelablePromiseContinuePromise<TResult, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseContinuePromise<TResult, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseContinuePromise<TResult, TContinuer>()
                        : obj.UnsafeAs<CancelablePromiseContinuePromise<TResult, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseContinuePromise<TResult, TContinuer> GetOrCreate(TContinuer continuer, ushort depth)
                {
                    var promise = GetOrCreate();
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

                protected override void Execute(PromiseRefBase handler, object rejectContainer, Promise.State state, ref bool invokingRejected)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, rejectContainer, state);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        handler.SuppressRejection = true;
                        _cancelationHelper.TryRelease();
                        callback.Invoke(handler, rejectContainer, state, this);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
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
                private static CancelablePromiseCancel<TResult, TCanceler> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseCancel<TResult, TCanceler>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseCancel<TResult, TCanceler>()
                        : obj.UnsafeAs<CancelablePromiseCancel<TResult, TCanceler>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseCancel<TResult, TCanceler> GetOrCreate(TCanceler canceler, ushort depth)
                {
                    var promise = GetOrCreate();
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

                protected override void Execute(PromiseRefBase handler, object rejectContainer, Promise.State state, ref bool invokingRejected)
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
                        HandleSelf(handler, rejectContainer, state);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
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
                private static CancelablePromiseCancelPromise<TResult, TCanceler> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<CancelablePromiseCancelPromise<TResult, TCanceler>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new CancelablePromiseCancelPromise<TResult, TCanceler>()
                        : obj.UnsafeAs<CancelablePromiseCancelPromise<TResult, TCanceler>>();
                }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseCancelPromise<TResult, TCanceler> GetOrCreate(TCanceler canceler, ushort depth)
                {
                    var promise = GetOrCreate();
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

                protected override void Execute(PromiseRefBase handler, object rejectContainer, Promise.State state, ref bool invokingRejected)
                {
                    if (_canceler.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, rejectContainer, state);
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Canceled)
                    {
                        _cancelationHelper.TryRelease();
                        callback.InvokeResolver(handler, state, this);
                    }
                    else if (unregistered)
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelf(handler, rejectContainer, state);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeReportUnhandledAndDispose(rejectContainer, state);
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