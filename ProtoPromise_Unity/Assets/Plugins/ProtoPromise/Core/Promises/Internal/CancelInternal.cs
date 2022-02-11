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

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            internal partial struct CancelationHelper
            {
                [MethodImpl(InlineOption)]
                internal void Register(CancelationToken cancelationToken, ICancelable cancelable)
                {
                    _retainAndCanceled = (1 << 16) + 1; // 17th bit set is not canceled, 1 retain until TryMakeReady or TryUnregister .
                    cancelationToken.TryRegister(cancelable, out _cancelationRegistration);
                }

                [MethodImpl(InlineOption)]
                private bool IsCanceled()
                {
                    return _retainAndCanceled >> 16 == 0;
                }

                [MethodImpl(InlineOption)]
                private void RetainAndSetCanceled()
                {
                    // Subtract 17th bit to set canceled and add 1 to retain. This performs both operations atomically and simultaneously.
                    Interlocked.Add(ref _retainAndCanceled, (-(1 << 16)) + 1);
                }

                [MethodImpl(InlineOption)]
                private bool Release()
                {
                    return InterlockedAddWithOverflowCheck(ref _retainAndCanceled, -1, 0) == 0; // If all bits are 0, canceled was set and all calls are complete.
                }

                internal void SetCanceled(PromiseSingleAwait owner)
                {
                    ThrowIfInPool(owner);
                    RetainAndSetCanceled();
                    ValueContainer valueContainer = CancelContainerVoid.GetOrCreate();
                    object currentValue = Interlocked.Exchange(ref owner._valueOrPrevious, valueContainer);
                    owner.State = Promise.State.Canceled;

#if CSHARP_7_3_OR_NEWER
                    if (currentValue is ValueContainer previousValue)
#else
                    ValueContainer previousValue = currentValue as ValueContainer;
                    if (previousValue != null)
#endif
                    {
                        previousValue.Release(); // Just release, don't report rejection.
                    }

                    var executionScheduler = new ExecutionScheduler(true);
                    owner.HandleProgressListener(Promise.State.Canceled, ref executionScheduler);
                    owner.InterlockedRetainDisregardId(); // Retain since Handle will release indiscriminately.
                    owner.Handle(valueContainer, Promise.State.Canceled, ref executionScheduler);
                    MaybeReleaseComplete(owner);
                    executionScheduler.Execute();
                }

                internal void MaybeMakeReady(PromiseSingleAwait owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    if (TryMakeReady(owner, valueContainer))
                    {
                        executionScheduler.ScheduleSynchronous(owner);
                    }
                    owner.WaitWhileProgressFlags(PromiseFlags.Subscribing);
                }

                internal void MaybeMakeReady(PromiseSingleAwait owner, bool isSecondReady, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    if (isSecondReady)
                    {
                        // The returned promise is handling owner.
                        valueContainer.Retain();
                        owner._valueOrPrevious = valueContainer;
                        executionScheduler.ScheduleSynchronous(owner);
                        owner.WaitWhileProgressFlags(PromiseFlags.Subscribing);
                    }
                    else
                    {
                        MaybeMakeReady(owner, valueContainer, ref executionScheduler);
                    }
                }

                internal void MaybeHandle(PromiseSingleAwait owner, ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    bool madeReady = TryMakeReady(owner, valueContainer);
                    owner.WaitWhileProgressFlags(PromiseFlags.Subscribing);
                    handler.MaybeDispose();
                    if (madeReady)
                    {
                        owner.HandleWithCatch(ref valueContainer, ref state, ref handler, ref executionScheduler);
                    }
                    else
                    {
                        handler = null;
                    }
                }

                internal void MaybeHandle(PromiseSingleAwait owner, bool isSecondReady, ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    if (isSecondReady)
                    {
                        // The returned promise is handling owner.
                        valueContainer.Retain();
                        var oldHandler = handler;
                        owner.SetResultAndMaybeHandle(valueContainer, state, out handler, ref executionScheduler);
                        owner.WaitWhileProgressFlags(PromiseFlags.Subscribing);
                        oldHandler.MaybeDispose();
                    }
                    else
                    {
                        MaybeHandle(owner, ref valueContainer, ref state, ref handler, ref executionScheduler);
                    }
                }

                internal bool TryMakeReady(PromiseSingleAwait owner, ValueContainer valueContainer)
                {
                    Thread.MemoryBarrier();
                    object oldContainer = owner._valueOrPrevious;
                    if (!_cancelationRegistration.Token.IsCancelationRequested & !IsCanceled()) // Was the token not in the process of canceling and not already canceled?
                    {
                        valueContainer.Retain();
                        if (Interlocked.CompareExchange(ref owner._valueOrPrevious, valueContainer, oldContainer) == oldContainer) // Are we able to set the value container before the token?
                        {
                            return true;
                        }
                        else
                        {
                            valueContainer.Release();
                        }
                    }
                    MaybeReleaseComplete(owner);
                    return false;
                }

                internal bool TryUnregister(PromiseSingleAwait owner)
                {
                    ThrowIfInPool(owner);
                    bool isCanceling;
                    bool unregistered = _cancelationRegistration.TryUnregister(out isCanceling);
                    if (unregistered)
                    {
                        return true;
                    }
                    if (Release())
                    {
                        owner.MaybeDispose();
                        return false;
                    }
                    return !isCanceling;
                }

                internal void MaybeReleaseComplete(PromiseSingleAwait owner)
                {
                    // This is called in HookupNewCancelablePromise when SetCanceled has set the _valueOrPrevious, so this may also be racing with that function on another thread.
                    if (Release())
                    {
                        owner.MaybeDispose();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolve<TResolver> : PromiseSingleAwait, ICancelable
                where TResolver : IDelegateResolveOrCancel
            {
                private CancelablePromiseResolve() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolve<TResolver> GetOrCreate(TResolver resolver, CancelationToken cancelationToken, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseResolve<TResolver>>()
                        ?? new CancelablePromiseResolve<TResolver>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default(CancelationHelper);
                    _resolver = default(TResolver);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeMakeReady(this, valueContainer, ref executionScheduler);
                }

                internal override void Handle(ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeHandle(this, ref valueContainer, ref state, ref handler, ref executionScheduler);
                }

                protected override void Execute(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, ref bool invokingRejected, ref bool suppressRejection, ref ExecutionScheduler executionScheduler)
                {
                    var resolveCallback = _resolver;
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref valueContainer, ref state, out nextRef, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        SetResultAndMaybeHandle(valueContainer, state, out nextRef, ref executionScheduler);
                    }
                    else
                    {
                        nextRef = null;
                    }
                }

                void ICancelable.Cancel()
                {
                    _cancelationHelper.SetCanceled(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolvePromise<TResolver> : PromiseWaitPromise, ICancelable
                where TResolver : IDelegateResolveOrCancelPromise
            {
                private CancelablePromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolvePromise<TResolver> GetOrCreate(TResolver resolver, CancelationToken cancelationToken, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseResolvePromise<TResolver>>()
                        ?? new CancelablePromiseResolvePromise<TResolver>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default(CancelationHelper);
                    _resolver = default(TResolver);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeMakeReady(this, _resolver.IsNull, valueContainer, ref executionScheduler);
                }

                internal override void Handle(ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeHandle(this, _resolver.IsNull, ref valueContainer, ref state, ref handler, ref executionScheduler);
                }

                protected override void Execute(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, ref bool invokingRejected, ref bool suppressRejection, ref ExecutionScheduler executionScheduler)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        SetResultAndMaybeHandle(valueContainer, state, out nextRef, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref valueContainer, ref state, out nextRef, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        SetResultAndMaybeHandle(valueContainer, state, out nextRef, ref executionScheduler);
                    }
                    else
                    {
                        nextRef = null;
                    }
                }

                void ICancelable.Cancel()
                {
                    _cancelationHelper.SetCanceled(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolveReject<TResolver, TRejecter> : PromiseSingleAwait, ICancelable
                where TResolver : IDelegateResolveOrCancel
                where TRejecter : IDelegateReject
            {
                private CancelablePromiseResolveReject() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolveReject<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseResolveReject<TResolver, TRejecter>>()
                        ?? new CancelablePromiseResolveReject<TResolver, TRejecter>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default(CancelationHelper);
                    _resolver = default(TResolver);
                    _rejecter = default(TRejecter);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeMakeReady(this, valueContainer, ref executionScheduler);
                }

                internal override void Handle(ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeHandle(this, ref valueContainer, ref state, ref handler, ref executionScheduler);
                }

                protected override void Execute(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, ref bool invokingRejected, ref bool suppressRejection, ref ExecutionScheduler executionScheduler)
                {
                    var resolveCallback = _resolver;
                    var rejectCallback = _rejecter;
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref valueContainer, ref state, out nextRef, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        suppressRejection = true;
                        rejectCallback.InvokeRejecter(ref valueContainer, ref state, out nextRef, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        SetResultAndMaybeHandle(valueContainer, state, out nextRef, ref executionScheduler);
                    }
                    else
                    {
                        nextRef = null;
                    }
                }

                void ICancelable.Cancel()
                {
                    _cancelationHelper.SetCanceled(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise, ICancelable
                where TResolver : IDelegateResolveOrCancelPromise
                where TRejecter : IDelegateRejectPromise
            {
                private CancelablePromiseResolveRejectPromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseResolveRejectPromise<TResolver, TRejecter>>()
                        ?? new CancelablePromiseResolveRejectPromise<TResolver, TRejecter>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default(CancelationHelper);
                    _resolver = default(TResolver);
                    _rejecter = default(TRejecter);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeMakeReady(this, _resolver.IsNull, valueContainer, ref executionScheduler);
                }

                internal override void Handle(ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeHandle(this, _resolver.IsNull, ref valueContainer, ref state, ref handler, ref executionScheduler);
                }

                protected override void Execute(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, ref bool invokingRejected, ref bool suppressRejection, ref ExecutionScheduler executionScheduler)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        SetResultAndMaybeHandle(valueContainer, state, out nextRef, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref valueContainer, ref state, out nextRef, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        suppressRejection = true;
                        rejectCallback.InvokeRejecter(ref valueContainer, ref state, out nextRef, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        SetResultAndMaybeHandle(valueContainer, state, out nextRef, ref executionScheduler);
                    }
                    else
                    {
                        nextRef = null;
                    }
                }

                void ICancelable.Cancel()
                {
                    _cancelationHelper.SetCanceled(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseContinue<TContinuer> : PromiseSingleAwait, ICancelable
                where TContinuer : IDelegateContinue
            {
                private CancelablePromiseContinue() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseContinue<TContinuer> GetOrCreate(TContinuer continuer, CancelationToken cancelationToken, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseContinue<TContinuer>>()
                        ?? new CancelablePromiseContinue<TContinuer>();
                    promise.Reset(depth);
                    promise._continuer = continuer;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default(CancelationHelper);
                    _continuer = default(TContinuer);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeMakeReady(this, valueContainer, ref executionScheduler);
                }

                internal override void Handle(ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeHandle(this, ref valueContainer, ref state, ref handler, ref executionScheduler);
                }

                protected override void Execute(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, ref bool invokingRejected, ref bool suppressRejection, ref ExecutionScheduler executionScheduler)
                {
                    suppressRejection = true;
                    _continuer.Invoke(ref valueContainer, ref state, out nextRef, this, ref _cancelationHelper, ref executionScheduler);
                }

                void ICancelable.Cancel()
                {
                    _cancelationHelper.SetCanceled(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseContinuePromise<TContinuer> : PromiseWaitPromise, ICancelable
                where TContinuer : IDelegateContinuePromise
            {
                private CancelablePromiseContinuePromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseContinuePromise<TContinuer> GetOrCreate(TContinuer continuer, CancelationToken cancelationToken, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseContinuePromise<TContinuer>>()
                        ?? new CancelablePromiseContinuePromise<TContinuer>();
                    promise.Reset(depth);
                    promise._continuer = continuer;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default(CancelationHelper);
                    _continuer = default(TContinuer);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeMakeReady(this, _continuer.IsNull, valueContainer, ref executionScheduler);
                }

                internal override void Handle(ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeHandle(this, _continuer.IsNull, ref valueContainer, ref state, ref handler, ref executionScheduler);
                }

                protected override void Execute(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, ref bool invokingRejected, ref bool suppressRejection, ref ExecutionScheduler executionScheduler)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        SetResultAndMaybeHandle(valueContainer, state, out nextRef, ref executionScheduler);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    suppressRejection = true;
                    callback.Invoke(ref valueContainer, ref state, out nextRef, this, ref _cancelationHelper, ref executionScheduler);
                }

                void ICancelable.Cancel()
                {
                    _cancelationHelper.SetCanceled(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseCancel<TCanceler> : PromiseSingleAwait, ICancelable
                where TCanceler : IDelegateResolveOrCancel
            {
                private CancelablePromiseCancel() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseCancel<TCanceler> GetOrCreate(TCanceler canceler, CancelationToken cancelationToken, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseCancel<TCanceler>>()
                        ?? new CancelablePromiseCancel<TCanceler>();
                    promise.Reset(depth);
                    promise._canceler = canceler;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default(CancelationHelper);
                    _canceler = default(TCanceler);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeMakeReady(this, valueContainer, ref executionScheduler);
                }

                internal override void Handle(ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeHandle(this, ref valueContainer, ref state, ref handler, ref executionScheduler);
                }

                protected override void Execute(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, ref bool invokingRejected, ref bool suppressRejection, ref ExecutionScheduler executionScheduler)
                {
                    var callback = _canceler;
                    if (valueContainer.GetState() == Promise.State.Canceled)
                    {
                        callback.InvokeResolver(ref valueContainer, ref state, out nextRef, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        SetResultAndMaybeHandle(valueContainer, state, out nextRef, ref executionScheduler);
                    }
                    else
                    {
                        nextRef = null;
                    }
                }

                void ICancelable.Cancel()
                {
                    _cancelationHelper.SetCanceled(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseCancelPromise<TCanceler> : PromiseWaitPromise, ICancelable
                where TCanceler : IDelegateResolveOrCancelPromise
            {
                private CancelablePromiseCancelPromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseCancelPromise<TCanceler> GetOrCreate(TCanceler canceler, CancelationToken cancelationToken, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseCancelPromise<TCanceler>>()
                        ?? new CancelablePromiseCancelPromise<TCanceler>();
                    promise.Reset(depth);
                    promise._canceler = canceler;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cancelationHelper = default(CancelationHelper);
                    _canceler = default(TCanceler);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeMakeReady(this, valueContainer, ref executionScheduler);
                }

                internal override void Handle(ref ValueContainer valueContainer, ref Promise.State state, ref PromiseSingleAwait handler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _cancelationHelper.MaybeHandle(this, _canceler.IsNull, ref valueContainer, ref state, ref handler, ref executionScheduler);
                }

                protected override void Execute(ref ValueContainer valueContainer, ref Promise.State state, out PromiseSingleAwait nextRef, ref bool invokingRejected, ref bool suppressRejection, ref ExecutionScheduler executionScheduler)
                {
                    if (_canceler.IsNull)
                    {
                        // The returned promise is handling this.
                        SetResultAndMaybeHandle(valueContainer, state, out nextRef, ref executionScheduler);
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    if (valueContainer.GetState() == Promise.State.Canceled)
                    {
                        callback.InvokeResolver(ref valueContainer, ref state, out nextRef, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        SetResultAndMaybeHandle(valueContainer, state, out nextRef, ref executionScheduler);
                    }
                    else
                    {
                        nextRef = null;
                    }
                }

                void ICancelable.Cancel()
                {
                    _cancelationHelper.SetCanceled(this);
                }
            }
        } // PromiseRef
    } // Internal
}