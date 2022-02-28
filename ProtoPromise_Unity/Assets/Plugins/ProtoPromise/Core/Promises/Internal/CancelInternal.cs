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
                private void RetainAndSetCanceled()
                {
                    // Subtract 17th bit to set canceled and add 1 to retain. This performs both operations atomically.
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
                    owner.InterlockedRetainDisregardId(); // Retain since Handle will release indiscriminately.
                    MaybeReleaseComplete(owner);
                    owner.HandleFromCancelation();
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

            partial class PromiseSingleAwait
            {
                internal void HandleFromCancelation()
                {
                    var executionScheduler = new ExecutionScheduler(true);
                    HandleablePromiseBase nextHandler;
                    SetResult(CancelContainerVoid.GetOrCreate(), Promise.State.Canceled);
                    Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                    nextHandler = Interlocked.Exchange(ref _waiter, null);
                    
                    HandleProgressListener(Promise.State.Canceled, Depth, ref executionScheduler);
                    MaybeHandleNext(nextHandler, ref executionScheduler);
                    executionScheduler.Execute();
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    var resolveCallback = _resolver;
                    if (handler.State == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        nextHandler = null;
                        WaitForProgressSubscribeAfterCanceled(handler);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (handler.State == Promise.State.Resolved)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        nextHandler = null;
                        WaitForProgressSubscribeAfterCanceled(handler);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    var resolveCallback = _resolver;
                    var rejectCallback = _rejecter;
                    var state = handler.State;
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        handlerDisposedAfterCallback = true;
                        rejectCallback.InvokeRejecter(ref handler, out nextHandler, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        HandleSelf(ref handler , out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        nextHandler = null;
                        WaitForProgressSubscribeAfterCanceled(handler);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    var state = handler.State;
                    if (state == Promise.State.Resolved)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        handlerDisposedAfterCallback = true;
                        rejectCallback.InvokeRejecter(ref handler, out nextHandler, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        nextHandler = null;
                        WaitForProgressSubscribeAfterCanceled(handler);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    handlerDisposedAfterCallback = true;
                    _continuer.Invoke(ref handler, out nextHandler, this, ref _cancelationHelper, ref executionScheduler);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    handlerDisposedAfterCallback = true;
                    callback.Invoke(ref handler, out nextHandler, this, ref _cancelationHelper, ref executionScheduler);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    var callback = _canceler;
                    if (handler.State == Promise.State.Canceled)
                    {
                        callback.InvokeResolver(ref handler, out nextHandler, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        nextHandler = null;
                        WaitForProgressSubscribeAfterCanceled(handler);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    if (_canceler.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    if (handler.State == Promise.State.Canceled)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        callback.InvokeResolver(ref handler, out nextHandler, this, ref _cancelationHelper, ref executionScheduler);
                    }
                    else if (_cancelationHelper.TryUnregister(this))
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        nextHandler = null;
                        WaitForProgressSubscribeAfterCanceled(handler);
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