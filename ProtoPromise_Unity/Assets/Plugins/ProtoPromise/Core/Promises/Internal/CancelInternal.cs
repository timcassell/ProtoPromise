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
                        // Rejection maybe wasn't caught.
                        previousValue.ReleaseAndMaybeAddToUnhandledStack(true);
                    }

                    ValueLinkedStack<ITreeHandleable> executionStack = new ValueLinkedStack<ITreeHandleable>();
                    owner.HandleWaiter(valueContainer, ref executionStack);
                    owner.HandleProgressListener(Promise.State.Canceled);
                    if (Release())
                    {
                        owner.MaybeDispose();
                    }

                    ExecuteHandlers(executionStack);
                }

                internal bool TryMakeReady(PromiseSingleAwait owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(owner);
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
                    if (Release())
                    {
                        owner.MaybeDispose();
                    }
                    return false;
                }

                internal bool TryUnregister(PromiseRef owner)
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
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolve<TArg, TResult, TResolver> : PromiseSingleAwait, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegate<TArg, TResult>
            {
                private CancelablePromiseResolve() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolve<TArg, TResult, TResolver> GetOrCreate(TResolver resolver, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseResolve<TArg, TResult, TResolver>>()
                        ?? new CancelablePromiseResolve<TArg, TResult, TResolver>();
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner.SuppressRejection = true;
                        executionStack.Push(this);
                        //AddToHandleQueueFront(this);
                    }
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner.SuppressRejection = true;
                        executionStack.Push(this);
                        //AddToHandleQueueBack(this);
                    }
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                protected override void Execute(ref ValueLinkedStack<ITreeHandleable> executionStack, IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        //resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper, ref executionStack);
                        TArg arg = valueContainer.GetValue<TArg>();
                        if (_cancelationHelper.TryUnregister(this))
                        {
                            TResult result = resolveCallback.Invoke(arg);
                            valueContainer.Release();
                            ResolveInternal(CreateResolveContainer(result, 0), ref executionStack);
                        }
                        return;
                    }
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionStack);
                        valueContainer.Release();
                    }
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { ThrowIfInPool(this); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolvePromise<TArg, TResult, TResolver> : PromiseWaitPromise, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegate<TArg, Promise<TResult>>
            {
                private CancelablePromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolvePromise<TArg, TResult, TResolver> GetOrCreate(TResolver resolver, CancelationToken cancelationToken, int depth)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseResolvePromise<TArg, TResult, TResolver>>()
                        ?? new CancelablePromiseResolvePromise<TArg, TResult, TResolver>();
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    else if (!_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        WaitWhileProgressFlags(ProgressFlags.Subscribing);
                        return;
                    }
                    owner.SuppressRejection = true;
                    executionStack.Push(this);
                    //AddToHandleQueueFront(this);
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    else if (!_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        WaitWhileProgressFlags(ProgressFlags.Subscribing);
                        return;
                    }
                    owner.SuppressRejection = true;
                    executionStack.Push(this);
                    //AddToHandleQueueBack(this);
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                protected override void Execute(ref ValueLinkedStack<ITreeHandleable> executionStack, IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionStack);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        //resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper, ref executionStack);
                        TArg arg = valueContainer.GetValue<TArg>();
                        if (_cancelationHelper.TryUnregister(this))
                        {
                            Promise<TResult> result = resolveCallback.Invoke(arg);
                            WaitFor(result, ref executionStack);
                            valueContainer.Release();
                        }
                        return;
                    }
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionStack);
                        valueContainer.Release();
                    }
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { ThrowIfInPool(this); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolveReject<TArgResolve, TResult, TResolver, TArgReject, TRejecter> : PromiseSingleAwait, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegate<TArgResolve, TResult>
                where TRejecter : IDelegate<TArgReject, TResult>
            {
                private CancelablePromiseResolveReject() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolveReject<TArgResolve, TResult, TResolver, TArgReject, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseResolveReject<TArgResolve, TResult, TResolver, TArgReject, TRejecter>>()
                        ?? new CancelablePromiseResolveReject<TArgResolve, TResult, TResolver, TArgReject, TRejecter>();
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner.SuppressRejection = true;
                        executionStack.Push(this);
                        //AddToHandleQueueFront(this);
                    }
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner.SuppressRejection = true;
                        executionStack.Push(this);
                        //AddToHandleQueueBack(this);
                    }
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                protected override void Execute(ref ValueLinkedStack<ITreeHandleable> executionStack, IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    var rejectCallback = _rejecter;
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        //resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper, ref executionStack);
                        TArgResolve arg = valueContainer.GetValue<TArgResolve>();
                        if (_cancelationHelper.TryUnregister(this))
                        {
                            TResult result = resolveCallback.Invoke(arg);
                            valueContainer.Release();
                            ResolveInternal(CreateResolveContainer(result, 0), ref executionStack);
                        }
                        return;
                    }
                    if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        //rejectCallback.InvokeRejecter(valueContainer, this, ref _cancelationHelper, ref executionStack);
                        TArgReject arg;
                        if (valueContainer.TryGetValue(out arg))
                        {
                            if (_cancelationHelper.TryUnregister(this))
                            {
                                TResult result = rejectCallback.Invoke(arg);
                                valueContainer.Release();
                                ResolveInternal(CreateResolveContainer(result, 0), ref executionStack);
                            }
                            return;
                        }
                    }
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionStack);
                        valueContainer.Release();
                    }
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { ThrowIfInPool(this); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class CancelablePromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter> : PromiseWaitPromise, ITreeHandleable, ICancelDelegate
                where TResolver : IDelegate<TArgResolve, Promise<TResult>>
                where TRejecter : IDelegate<TArgReject, Promise<TResult>>
            {
                private CancelablePromiseResolveRejectPromise() { }

                [MethodImpl(InlineOption)]
                internal static CancelablePromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, int depth)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<CancelablePromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter>>()
                        ?? new CancelablePromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter>();
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    else if (!_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        WaitWhileProgressFlags(ProgressFlags.Subscribing);
                        return;
                    }
                    owner.SuppressRejection = true;
                    executionStack.Push(this);
                    //AddToHandleQueueFront(this);
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    else if (!_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        WaitWhileProgressFlags(ProgressFlags.Subscribing);
                        return;
                    }
                    owner.SuppressRejection = true;
                    executionStack.Push(this);
                    //AddToHandleQueueBack(this);
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                protected override void Execute(ref ValueLinkedStack<ITreeHandleable> executionStack, IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionStack);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        //resolveCallback.InvokeResolver(valueContainer, this, ref _cancelationHelper, ref executionStack);
                        TArgResolve arg = valueContainer.GetValue<TArgResolve>();
                        if (_cancelationHelper.TryUnregister(this))
                        {
                            Promise<TResult> result = resolveCallback.Invoke(arg);
                            WaitFor(result, ref executionStack);
                            valueContainer.Release();
                        }
                        return;
                    }
                    if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        //rejectCallback.InvokeRejecter(valueContainer, this, ref _cancelationHelper, ref executionStack);
                        TArgReject arg;
                        if (valueContainer.TryGetValue(out arg))
                        {
                            if (_cancelationHelper.TryUnregister(this))
                            {
                                Promise<TResult> result = rejectCallback.Invoke(arg);
                                WaitFor(result, ref executionStack);
                                valueContainer.Release();
                            }
                            return;
                        }
                    }
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        RejectOrCancelInternal(valueContainer, ref executionStack);
                        valueContainer.Release();
                    }
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { ThrowIfInPool(this); }
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner.SuppressRejection = true;
                        executionStack.Push(this);
                        //AddToHandleQueueFront(this);
                    }
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner.SuppressRejection = true;
                        executionStack.Push(this);
                        //AddToHandleQueueBack(this);
                    }
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                protected override void Execute(ref ValueLinkedStack<ITreeHandleable> executionStack, IValueContainer valueContainer, ref bool invokingRejected)
                {
                    _continuer.Invoke(valueContainer, this, ref _cancelationHelper, ref executionStack);
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { ThrowIfInPool(this); }
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    else if (!_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        WaitWhileProgressFlags(ProgressFlags.Subscribing);
                        return;
                    }
                    owner.SuppressRejection = true;
                    executionStack.Push(this);
                    //AddToHandleQueueFront(this);
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    else if (!_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        WaitWhileProgressFlags(ProgressFlags.Subscribing);
                        return;
                    }
                    owner.SuppressRejection = true;
                    executionStack.Push(this);
                    //AddToHandleQueueBack(this);
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                protected override void Execute(ref ValueLinkedStack<ITreeHandleable> executionStack, IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionStack);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    callback.Invoke(valueContainer, this, ref _cancelationHelper, ref executionStack);
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { ThrowIfInPool(this); }
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

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner.SuppressRejection = true;
                        executionStack.Push(this);
                        //AddToHandleQueueFront(this);
                    }
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    if (_cancelationHelper.TryMakeReady(this, valueContainer))
                    {
                        owner.SuppressRejection = true;
                        executionStack.Push(this);
                        //AddToHandleQueueBack(this);
                    }
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                public override void Handle(ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    ThrowIfInPool(this);
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;

                    if (valueContainer.GetState() != Promise.State.Canceled)
                    {
                        if (_cancelationHelper.TryUnregister(this))
                        {
                            HandleSelf(valueContainer, ref executionStack);
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

                    HandleSelf(valueContainer, ref executionStack);
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    _cancelationHelper.SetCanceled(this, valueContainer);
                }

                void ICancelDelegate.Dispose() { ThrowIfInPool(this); }
            }
        } // PromiseRef
    } // Internal
}