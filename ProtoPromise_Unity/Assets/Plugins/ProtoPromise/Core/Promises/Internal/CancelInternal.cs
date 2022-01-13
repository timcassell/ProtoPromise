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
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

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
                    return Interlocked.Decrement(ref _retainAndCanceled) == 0; // If all bits are 0, canceled was set and all calls are complete.
                }

                internal void SetCanceled(PromiseSingleAwait owner)
                {
                    ThrowIfInPool(owner);
                    RetainAndSetCanceled();
                    ValueContainer valueContainer = CancelContainerVoid.GetOrCreate(1);
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

                    ExecutionScheduler executionScheduler = new ExecutionScheduler(true);
                    owner.HandleWaiter(valueContainer, ref executionScheduler);
                    owner.HandleProgressListener(Promise.State.Canceled, ref executionScheduler);
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
                    }
                    else if (!TryMakeReady(owner, valueContainer))
                    {
                        owner.WaitWhileProgressFlags(PromiseFlags.Subscribing);
                        return;
                    }
                    executionScheduler.ScheduleSynchronous(owner);
                    owner.WaitWhileProgressFlags(PromiseFlags.Subscribing);
                }

                private bool TryMakeReady(PromiseSingleAwait owner, ValueContainer valueContainer)
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
                internal static CancelablePromiseResolve<TResolver> GetOrCreate(TResolver resolver, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseResolve<TResolver>>()
                        ?? new CancelablePromiseResolve<TResolver>();
                    promise.Reset();
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

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;

                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        var resolveCallback = CreateResolveWrapper(this, _resolver);
                        InvokeAndHandle(resolveCallback, valueContainer, ref _cancelationHelper, false, false, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
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
                internal static CancelablePromiseResolvePromise<TResolver> GetOrCreate(TResolver resolver, CancelationToken cancelationToken, int depth)
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

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;

                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionScheduler);
                        return;
                    }

                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        var resolveCallback = CreateResolveWrapper(this, _resolver);
                        _resolver = default(TResolver);
                        InvokeAndHandle(resolveCallback, valueContainer, ref _cancelationHelper, false, false, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
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
                internal static CancelablePromiseResolveReject<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseResolveReject<TResolver, TRejecter>>()
                        ?? new CancelablePromiseResolveReject<TResolver, TRejecter>();
                    promise.Reset();
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

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;

                    var resolveCallback = CreateResolveWrapper(this, _resolver);
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        InvokeAndHandle(resolveCallback, valueContainer, ref _cancelationHelper, false, false, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        var rejectCallback = CreateRejectWrapper(this, _rejecter);
                        InvokeAndHandle(rejectCallback, valueContainer, ref _cancelationHelper, true, true, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
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
                internal static CancelablePromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, int depth)
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

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;

                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = CreateResolveWrapper(this, _resolver);
                    _resolver = default(TResolver);
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        InvokeAndHandle(resolveCallback, valueContainer, ref _cancelationHelper, false, false, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        var rejectCallback = CreateRejectWrapper(this, _rejecter);
                        InvokeAndHandle(rejectCallback, valueContainer, ref _cancelationHelper, true, true, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
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
                internal static CancelablePromiseContinue<TContinuer> GetOrCreate(TContinuer continuer, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseContinue<TContinuer>>()
                        ?? new CancelablePromiseContinue<TContinuer>();
                    promise.Reset();
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

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;

                    var continueCallback = CreateContinueWrapper(this, _continuer);
                    InvokeAndHandle(continueCallback, valueContainer, ref _cancelationHelper, false, true, ref executionScheduler);
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
                internal static CancelablePromiseContinuePromise<TContinuer> GetOrCreate(TContinuer continuer, CancelationToken cancelationToken, int depth)
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

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;

                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionScheduler);
                        return;
                    }

                    var continueCallback = CreateContinueWrapper(this, _continuer);
                    _continuer = default(TContinuer);
                    InvokeAndHandle(continueCallback, valueContainer, ref _cancelationHelper, false, true, ref executionScheduler);
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
                internal static CancelablePromiseCancel<TCanceler> GetOrCreate(TCanceler canceler, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<CancelablePromiseCancel<TCanceler>>()
                        ?? new CancelablePromiseCancel<TCanceler>();
                    promise.Reset();
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

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;

                    if (valueContainer.GetState() == Promise.State.Canceled)
                    {
                        var cancelCallback = CreateResolveWrapper(this, _canceler);
                        InvokeAndHandle(cancelCallback, valueContainer, ref _cancelationHelper, false, false, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
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
                internal static CancelablePromiseCancelPromise<TCanceler> GetOrCreate(TCanceler canceler, CancelationToken cancelationToken, int depth)
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

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;

                    if (_canceler.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionScheduler);
                        return;
                    }

                    if (valueContainer.GetState() == Promise.State.Canceled)
                    {
                        var cancelCallback = CreateResolveWrapper(this, _canceler);
                        _canceler = default(TCanceler);
                        InvokeAndHandle(cancelCallback, valueContainer, ref _cancelationHelper, false, false, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
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