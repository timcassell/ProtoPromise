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
                internal void Register(CancelationToken cancelationToken, ICancelDelegate cancelable)
                {
                    _retainAndCanceled = (1 << 16) + 1; // 17th bit set is not canceled, 1 retain until TryMakeReady or TryUnregister .
                    cancelationToken.TryRegisterInternal(cancelable, out _cancelationRegistration);
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

                internal void SetCanceled(PromiseSingleAwait owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(owner);
                    RetainAndSetCanceled();
                    object currentValue = Interlocked.Exchange(ref owner._valueOrPrevious, valueContainer);
                    valueContainer.Retain();
                    owner.State = Promise.State.Canceled;

#if CSHARP_7_3_OR_NEWER
                    if (currentValue is IValueContainer previousValue)
#else
                    IValueContainer previousValue = currentValue as IValueContainer;
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

                internal void MaybeMakeReady(PromiseSingleAwait owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    if (TryMakeReady(owner, valueContainer))
                    {
                        executionScheduler.ScheduleSynchronous(owner);
                    }
                    owner.WaitWhileProgressFlags(PromiseFlags.Subscribing);
                }

                internal void MaybeMakeReady(PromiseSingleAwait owner, bool isSecondReady, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
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

                private bool TryMakeReady(PromiseSingleAwait owner, IValueContainer valueContainer)
                {
                    Thread.MemoryBarrier();
                    object oldContainer = owner._valueOrPrevious;
                    bool _, isCancelationRequested;
                    _cancelationRegistration.GetIsRegisteredAndIsCancelationRequested(out _, out isCancelationRequested);
                    if (!isCancelationRequested & !IsCanceled()) // Was the token not in the process of canceling and not already canceled?
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
            private sealed partial class CancelablePromiseResolve<TResolver> : PromiseSingleAwait, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegateResolve
            {
                private CancelablePromiseResolve() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolve<TResolver> GetOrCreate(TResolver resolver, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseResolve<TResolver>>()
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
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    owner.SuppressRejection = true;
                    _cancelationHelper.MaybeMakeReady(this, valueContainer, ref executionScheduler);
                }

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    var resolveCallback = _resolver;
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolvePromise<TResolver> : PromiseWaitPromise, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegateResolvePromise
            {
                private CancelablePromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolvePromise<TResolver> GetOrCreate(TResolver resolver, CancelationToken cancelationToken, int depth)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseResolvePromise<TResolver>>()
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
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    owner.SuppressRejection = true;
                    _cancelationHelper.MaybeMakeReady(this, _resolver.IsNull, valueContainer, ref executionScheduler);
                }

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolveReject<TResolver, TRejecter> : PromiseSingleAwait, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegateResolve
                where TRejecter : IDelegateReject
            {
                private CancelablePromiseResolveReject() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolveReject<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseResolveReject<TResolver, TRejecter>>()
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
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    owner.SuppressRejection = true;
                    _cancelationHelper.MaybeMakeReady(this, valueContainer, ref executionScheduler);
                }

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    var resolveCallback = _resolver;
                    var rejectCallback = _rejecter;
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        suppressRejection = true;
                        rejectCallback.InvokeRejecter(valueContainer, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegateResolvePromise
                where TRejecter : IDelegateRejectPromise
            {
                private CancelablePromiseResolveRejectPromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, int depth)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseResolveRejectPromise<TResolver, TRejecter>>()
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
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    owner.SuppressRejection = true;
                    _cancelationHelper.MaybeMakeReady(this, _resolver.IsNull, valueContainer, ref executionScheduler);
                }

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        suppressRejection = true;
                        rejectCallback.InvokeRejecter(valueContainer, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseContinue<TContinuer> : PromiseSingleAwait, ITreeHandleable, ICancelDelegate
                where TContinuer : IDelegateContinue
            {
                private CancelablePromiseContinue() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseContinue<TContinuer> GetOrCreate(TContinuer continuer, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseContinue<TContinuer>>()
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
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    owner.SuppressRejection = true;
                    _cancelationHelper.MaybeMakeReady(this, valueContainer, ref executionScheduler);
                }

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    suppressRejection = true;
                    _continuer.Invoke(valueContainer, this, ref _cancelationHelper, ref executionScheduler);
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseContinuePromise<TContinuer> : PromiseWaitPromise, ITreeHandleable, ICancelDelegate
                where TContinuer : IDelegateContinuePromise
            {
                private CancelablePromiseContinuePromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseContinuePromise<TContinuer> GetOrCreate(TContinuer continuer, CancelationToken cancelationToken, int depth)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseContinuePromise<TContinuer>>()
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
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    owner.SuppressRejection = true;
                    _cancelationHelper.MaybeMakeReady(this, _continuer.IsNull, valueContainer, ref executionScheduler);
                }

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionScheduler);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    suppressRejection = true;
                    callback.Invoke(valueContainer, this, ref _cancelationHelper, ref executionScheduler);
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseCancel<TCanceler> : PromiseSingleAwait, ITreeHandleable, ICancelDelegate
                where TCanceler : IDelegateSimple
            {
                private CancelablePromiseCancel() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseCancel<TCanceler> GetOrCreate(TCanceler canceler, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseCancel<TCanceler>>()
                        ?? new CancelablePromiseCancel<TCanceler>();
                    promise.Reset();
                    promise._canceler = canceler;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void OnHookupFailed()
                {
                    _cancelationHelper.MaybeReleaseComplete(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    owner.SuppressRejection = true;
                    _cancelationHelper.MaybeMakeReady(this, valueContainer, ref executionScheduler);
                }

                public override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;

                    if (valueContainer.GetState() != Promise.State.Canceled)
                    {
                        if (_cancelationHelper.TryUnregister(this))
                        {
                            HandleSelf(valueContainer, ref executionScheduler);
                        }
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    SetCurrentInvoker(this);
                    try
                    {
                        if (!_cancelationHelper.TryUnregister(this))
                        {
                            ClearCurrentInvoker();
                            return;
                        }
                        callback.Invoke(valueContainer);
                    }
                    catch (Exception e)
                    {
                        AddRejectionToUnhandledStack(e, this);
                    }
                    ClearCurrentInvoker();

                    HandleSelf(valueContainer, ref executionScheduler);
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { }
            }
        } // PromiseRef
    } // Internal
}