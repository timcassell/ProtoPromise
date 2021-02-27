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

using Proto.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class DeferredPromiseVoidCancel : DeferredPromiseVoid, ICancelDelegate
            {
                private struct Creator : ICreator<DeferredPromiseVoidCancel>
                {
                    [MethodImpl(InlineOption)]
                    public DeferredPromiseVoidCancel Create()
                    {
                        return new DeferredPromiseVoidCancel();
                    }
                }

                private CancelationRegistration _cancelationRegistration;

                private DeferredPromiseVoidCancel() { }

                protected override void Dispose()
                {
                    SuperDispose();
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                internal static DeferredPromiseVoidCancel GetOrCreate(CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromiseVoidCancel, Creator>(new Creator());
                    promise.Reset();
                    promise.ResetDepth();
                    cancelationToken.TryRegisterInternal(promise, out promise._cancelationRegistration);
                    return promise;
                }

                protected override bool TryUnregisterCancelation()
                {
                    ThrowIfInPool(this);
                    return TryUnregisterAndIsNotCanceling(ref _cancelationRegistration);
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    CancelFromToken(valueContainer);
                }

                void ICancelDelegate.Dispose() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class DeferredPromiseCancel<T> : DeferredPromise<T>, ICancelDelegate
            {
                private struct Creator : ICreator<DeferredPromiseCancel<T>>
                {
                    [MethodImpl(InlineOption)]
                    public DeferredPromiseCancel<T> Create()
                    {
                        return new DeferredPromiseCancel<T>();
                    }
                }

                private CancelationRegistration _cancelationRegistration;

                private DeferredPromiseCancel() { }

                protected override void Dispose()
                {
                    SuperDispose();
                    _cancelationRegistration = default(CancelationRegistration);
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                internal static DeferredPromiseCancel<T> GetOrCreate(CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromiseCancel<T>, Creator>(new Creator());
                    promise.Reset();
                    promise.ResetDepth();
                    cancelationToken.TryRegisterInternal(promise, out promise._cancelationRegistration);
                    return promise;
                }

                protected override bool TryUnregisterCancelation()
                {
                    ThrowIfInPool(this);
                    return TryUnregisterAndIsNotCanceling(ref _cancelationRegistration);
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    CancelFromToken(valueContainer);
                }

                void ICancelDelegate.Dispose() { }
            }

            internal struct CancelationHelper
            {
                private CancelationRegistration _cancelationRegistration;
                private int _retainCounter;
                volatile private bool _isCanceled;

                [MethodImpl(InlineOption)]
                internal void Register(CancelationToken cancelationToken, ICancelDelegate cancelable)
                {
                    _retainCounter = 1;
                    _isCanceled = false;
                    cancelationToken.TryRegisterInternal(cancelable, out _cancelationRegistration);
                }

                [MethodImpl(InlineOption)]
                private bool Release()
                {
                    return Interlocked.Decrement(ref _retainCounter) == 0;
                }

                internal void SetCanceled(PromiseBranch owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(owner);
                    Interlocked.Increment(ref _retainCounter);
                    _isCanceled = true;
                    object currentValue = Interlocked.Exchange(ref owner._valueOrPrevious, valueContainer);
                    valueContainer.Retain();
                    owner._state = Promise.State.Canceled;

#if CSHARP_7_OR_LATER
                    if (currentValue is PromiseRef previous)
#else
                    PromiseRef previous = currentValue as PromiseRef;
                    if (previous != null)
#endif
                    {
                        // Try to remove owner from previous' next branches.
                        if (previous.TryRemoveWaiter(owner))
                        {
                            Release();
                        }
                    }
                    else if (currentValue != null)
                    {
                        // Rejection maybe wasn't caught.
                        ((IValueContainer) currentValue).ReleaseAndMaybeAddToUnhandledStack(true);
                    }

                    owner.HandleWaiter(valueContainer);
                    owner.CancelProgressListeners(currentValue);
                    if (Release())
                    {
                        owner.MaybeDispose();
                    }
                }

                internal bool TryMakeReady(PromiseRef owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(owner);
                    Thread.MemoryBarrier();
                    object oldContainer = owner._valueOrPrevious;
                    bool _, isCancelationRequested;
                    _cancelationRegistration.GetIsRegisteredAndIsCancelationRequested(out _, out isCancelationRequested);
                    if (!isCancelationRequested & !_isCanceled) // Was the token not in the process of canceling and not already canceled?
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
                    if (Release() && _isCanceled)
                    {
                        owner.MaybeDispose();
                    }
                    return false;
                }

                internal bool TryUnregister(PromiseRef owner)
                {
                    bool unregistered = TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) && !_isCanceled;
                    if (!unregistered)
                    {
                        if (Release() && _isCanceled)
                        {
                            owner.MaybeDispose();
                        }
                    }
                    return unregistered;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class CancelablePromiseResolve<TResolver> : PromiseBranch, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegateResolve
            {
                private struct Creator : ICreator<CancelablePromiseResolve<TResolver>>
                {
                    [MethodImpl(InlineOption)]
                    public CancelablePromiseResolve<TResolver> Create()
                    {
                        return new CancelablePromiseResolve<TResolver>();
                    }
                }

                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;

                private CancelablePromiseResolve() { }

                [MethodImpl(InlineOption)]
                public static CancelablePromiseResolve<TResolver> GetOrCreate(TResolver resolver, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<CancelablePromiseResolve<TResolver>, Creator>(new Creator());
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        handleQueue.Push(this);
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        AddToHandleQueueBack(this);
                    }
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer);
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
            private sealed class CancelablePromiseResolvePromise<TResolver> : PromiseWaitPromise, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegateResolvePromise
            {
                private struct Creator : ICreator<CancelablePromiseResolvePromise<TResolver>>
                {
                    [MethodImpl(InlineOption)]
                    public CancelablePromiseResolvePromise<TResolver> Create()
                    {
                        return new CancelablePromiseResolvePromise<TResolver>();
                    }
                }

                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;

                private CancelablePromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                public static CancelablePromiseResolvePromise<TResolver> GetOrCreate(TResolver resolver, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<CancelablePromiseResolvePromise<TResolver>, Creator>(new Creator());
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        handleQueue.Push(this);
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        AddToHandleQueueBack(this);
                    }
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer);
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
            private sealed class CancelablePromiseResolveReject<TResolver, TRejecter> : PromiseBranch, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegateResolve
                where TRejecter : IDelegateReject
            {
                private struct Creator : ICreator<CancelablePromiseResolveReject<TResolver, TRejecter>>
                {
                    [MethodImpl(InlineOption)]
                    public CancelablePromiseResolveReject<TResolver, TRejecter> Create()
                    {
                        return new CancelablePromiseResolveReject<TResolver, TRejecter>();
                    }
                }

                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;

                private CancelablePromiseResolveReject() { }

                [MethodImpl(InlineOption)]
                public static CancelablePromiseResolveReject<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<CancelablePromiseResolveReject<TResolver, TRejecter>, Creator>(new Creator());
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        handleQueue.Push(this);
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        AddToHandleQueueBack(this);
                    }
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    var rejectCallback = _rejecter;
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(valueContainer, this, ref _cancelationHelper);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer);
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
            private sealed class CancelablePromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegateResolvePromise
                where TRejecter : IDelegateRejectPromise
            {
                private struct Creator : ICreator<CancelablePromiseResolveRejectPromise<TResolver, TRejecter>>
                {
                    [MethodImpl(InlineOption)]
                    public CancelablePromiseResolveRejectPromise<TResolver, TRejecter> Create()
                    {
                        return new CancelablePromiseResolveRejectPromise<TResolver, TRejecter>();
                    }
                }

                private CancelationHelper _cancelationHelper;
                private TResolver _resolver;
                private TRejecter _rejecter;

                private CancelablePromiseResolveRejectPromise() { }

                [MethodImpl(InlineOption)]
                public static CancelablePromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<CancelablePromiseResolveRejectPromise<TResolver, TRejecter>, Creator>(new Creator());
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        handleQueue.Push(this);
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        AddToHandleQueueBack(this);
                    }
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(valueContainer, this, ref _cancelationHelper);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer);
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
            private sealed class CancelablePromiseContinue<TContinuer> : PromiseBranch, ITreeHandleable, ICancelDelegate
                where TContinuer : IDelegateContinue
            {
                private struct Creator : ICreator<CancelablePromiseContinue<TContinuer>>
                {
                    [MethodImpl(InlineOption)]
                    public CancelablePromiseContinue<TContinuer> Create()
                    {
                        return new CancelablePromiseContinue<TContinuer>();
                    }
                }

                private CancelationHelper _cancelationHelper;
                private TContinuer _continuer;

                private CancelablePromiseContinue() { }

                [MethodImpl(InlineOption)]
                public static CancelablePromiseContinue<TContinuer> GetOrCreate(TContinuer continuer, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<CancelablePromiseContinue<TContinuer>, Creator>(new Creator());
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        handleQueue.Push(this);
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        AddToHandleQueueBack(this);
                    }
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    _continuer.Invoke(valueContainer, this, ref _cancelationHelper);
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
            private sealed class CancelablePromiseContinuePromise<TContinuer> : PromiseWaitPromise, ITreeHandleable, ICancelDelegate
                where TContinuer : IDelegateContinuePromise
            {
                private struct Creator : ICreator<CancelablePromiseContinuePromise<TContinuer>>
                {
                    [MethodImpl(InlineOption)]
                    public CancelablePromiseContinuePromise<TContinuer> Create()
                    {
                        return new CancelablePromiseContinuePromise<TContinuer>();
                    }
                }

                private CancelationHelper _cancelationHelper;
                private TContinuer _continuer;

                private CancelablePromiseContinuePromise() { }

                [MethodImpl(InlineOption)]
                public static CancelablePromiseContinuePromise<TContinuer> GetOrCreate(TContinuer continuer, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<CancelablePromiseContinuePromise<TContinuer>, Creator>(new Creator());
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        handleQueue.Push(this);
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        AddToHandleQueueBack(this);
                    }
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    callback.Invoke(valueContainer, this, ref _cancelationHelper);
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
            private sealed class CancelablePromiseCancel<TCanceler> : PromiseBranch, ITreeHandleable, ICancelDelegate
                where TCanceler : IDelegateSimple
            {
                private struct Creator : ICreator<CancelablePromiseCancel<TCanceler>>
                {
                    [MethodImpl(InlineOption)]
                    public CancelablePromiseCancel<TCanceler> Create()
                    {
                        return new CancelablePromiseCancel<TCanceler>();
                    }
                }

                private CancelationHelper _cancelationHelper;
                private TCanceler _canceler;

                private CancelablePromiseCancel() { }

                [MethodImpl(InlineOption)]
                public static CancelablePromiseCancel<TCanceler> GetOrCreate(TCanceler canceler, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<CancelablePromiseCancel<TCanceler>, Creator>(new Creator());
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        handleQueue.Push(this);
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner._suppressRejection = true;
                        AddToHandleQueueBack(this);
                    }
                }

                public override void Handle()
                {
                    ThrowIfInPool(this);
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;

                    if (valueContainer.GetState() != Promise.State.Canceled)
                    {
                        HandleSelf(valueContainer);
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

                    HandleSelf(valueContainer);
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