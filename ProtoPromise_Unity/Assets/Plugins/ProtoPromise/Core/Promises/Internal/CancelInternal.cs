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
                internal void Register<TOwner>(CancelationToken cancelationToken, TOwner owner) where TOwner : PromiseSingleAwait, ICancelable
                {
                    _isCanceled = false;
                    owner.InterlockedRetainDisregardId();
                    cancelationToken.TryRegister(owner, out _cancelationRegistration);
                }

                internal void SetCanceled(PromiseSingleAwait owner)
                {
                    ThrowIfInPool(owner);
                    _isCanceled = true;
                    owner.HandleFromCancelation();
                }

                internal bool TryUnregister(PromiseSingleAwait owner)
                {
                    ThrowIfInPool(owner);
                    bool isCanceling;
                    bool unregistered = _cancelationRegistration.TryUnregister(out isCanceling);
                    if (unregistered | (!isCanceling & !_isCanceled))
                    {
                        owner._smallFields.InterlockedTryReleaseComplete();
                        return true;
                    }
                    return false;
                }

                internal static void SetNextAfterCanceled(PromiseSingleAwait owner, ref PromiseRef handler, out HandleablePromiseBase nextHandler)
                {
                    nextHandler = null;
                    handler.MaybeDispose();
                    handler = owner;
                }
            }

            partial class PromiseSingleAwait
            {
                internal void HandleFromCancelation()
                {
                    HandleablePromiseBase nextHandler;
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        SetResult(CancelContainerVoid.GetOrCreate(), Promise.State.Canceled);
                        Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                        nextHandler = Interlocked.Exchange(ref _waiter, null);
                    }
                    var executionScheduler = new ExecutionScheduler(true);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    var resolveCallback = _resolver;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & handler.State == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else if (unregistered)
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        CancelationHelper.SetNextAfterCanceled(this, ref handler, out nextHandler);
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
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & handler.State == Promise.State.Resolved)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else if (unregistered)
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        CancelationHelper.SetNextAfterCanceled(this, ref handler, out nextHandler);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    var resolveCallback = _resolver;
                    var rejectCallback = _rejecter;
                    var state = handler.State;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else if (!unregistered)
                    {
                        CancelationHelper.SetNextAfterCanceled(this, ref handler, out nextHandler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        handlerDisposedAfterCallback = true;
                        rejectCallback.InvokeRejecter(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler , out nextHandler, ref executionScheduler);
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
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & state == Promise.State.Resolved)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else if (!unregistered)
                    {
                        CancelationHelper.SetNextAfterCanceled(this, ref handler, out nextHandler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        handlerDisposedAfterCallback = true;
                        rejectCallback.InvokeRejecter(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    handlerDisposedAfterCallback = true;
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        _continuer.Invoke(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        CancelationHelper.SetNextAfterCanceled(this, ref handler, out nextHandler);
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
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        callback.Invoke(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        CancelationHelper.SetNextAfterCanceled(this, ref handler, out nextHandler);
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

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    var callback = _canceler;
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & handler.State == Promise.State.Canceled)
                    {
                        callback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else if (unregistered)
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        CancelationHelper.SetNextAfterCanceled(this, ref handler, out nextHandler);
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
                    bool unregistered = _cancelationHelper.TryUnregister(this);
                    if (unregistered & handler.State == Promise.State.Canceled)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        callback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else if (unregistered)
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        CancelationHelper.SetNextAfterCanceled(this, ref handler, out nextHandler);
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