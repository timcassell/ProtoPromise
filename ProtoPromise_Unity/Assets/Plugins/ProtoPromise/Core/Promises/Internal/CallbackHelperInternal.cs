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

using System;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            private static class Invoker<TArg, TResult>
            {
                [MethodImpl(InlineOption)]
                private static Promise<TResult> AdoptDirect(Promise<TResult> promise, int currentDepth)
                {
#if !PROMISE_PROGRESS
                    return promise;
#else
                    // TODO: normalize progress
                    return promise;
#endif
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, Promise<TArg> resolved) where TDelegate : IDelegate<TArg, Promise<TResult>>
                {
                    try
                    {
                        return AdoptDirect(resolver.Invoke(resolved.Result), resolved.Depth);
                    }
                    // TODO: depth
                    catch (OperationCanceledException e)
                    {
                        return Promise<TResult>.Canceled(e);
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.Rejected(e);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackDirect<TDelegate>(TDelegate resolver, Promise<TArg> resolved) where TDelegate : IDelegate<TArg, TResult>
                {
                    try
                    {
                        TResult result = resolver.Invoke(resolved.Result);
                        return new Promise<TResult>(null, ValidIdFromApi, resolved.Depth);
                    }
                    // TODO: depth
                    catch (OperationCanceledException e)
                    {
                        return Promise<TResult>.Canceled(e);
                    }
                    catch (Exception e)
                    {
                        return Promise<TResult>.Rejected(e);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class CallbackHelper
            {
                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddFinally<TFinally, TResult>(Promise<TResult> _this, TFinally finalizer)
                    where TFinally : IDelegateSimple, IDelegate<TResult, TResult>
                {
                    if (_this._ref == null)
                    {
                        return Invoker<TResult, TResult>.InvokeCallbackDirect(finalizer, _this);
                    }
                    _this._ref.MarkAwaited(_this.Id);
                    PromiseRef promise = PromiseFinally<TFinally>.GetOrCreate(finalizer);
                    _this._ref.HookupNewPromise(promise);
                    return new Promise<TResult>(promise, promise.Id, promise.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddCancel<TCanceler, TResult>(Promise<TResult> _this, TCanceler canceler, CancelationToken cancelationToken)
                    where TCanceler : IDelegateSimple
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    PromiseRef promise;
                    _this._ref.MarkAwaited(_this.Id);
                    if (cancelationToken.CanBeCanceled)
                    {
                        promise = CancelablePromiseCancel<TCanceler>.GetOrCreate(canceler, cancelationToken);
                        _this._ref.HookupNewCancelablePromise(promise);
                    }
                    else
                    {
                        promise = PromiseCancel<TCanceler>.GetOrCreate(canceler);
                        _this._ref.HookupNewPromise(promise);
                    }
                    return new Promise<TResult>(promise, promise.Id, promise.Depth);
                }

                [MethodImpl(InlineOption)]
                private static int GetNextDepth(int depth)
                {
#if !PROMISE_PROGRESS
                    return 0;
#elif PROMISE_DEBUG
                    return new Fixed32(depth).GetIncrementedWholeTruncated().WholePart;
#else
                    return depth + 1;
#endif
                }

#if PROMISE_PROGRESS
                internal static void InvokeAndCatchProgress<TProgress>(TProgress progress, float value, ITraceable traceable)
                    where TProgress : IProgress<float>
                {
                    SetCurrentInvoker(traceable);
                    try
                    {
                        progress.Report(value);
                    }
                    catch (Exception e)
                    {
                        AddRejectionToUnhandledStack(e, traceable);
                    }
                    ClearCurrentInvoker();
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddProgress<TResult, TProgress>(Promise<TResult> _this, TProgress progress, CancelationToken cancelationToken)
                    where TProgress : IProgress<float>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return _this.Duplicate();
                    }

                    if (_this._ref == null)
                    {
                        CallbackHelper.InvokeAndCatchProgress(progress, 1, null);
                        return new Promise<TResult>(null, ValidIdFromApi, _this.Depth);
                    }

                    _this._ref.MarkAwaited(_this.Id);
                    PromiseProgress<TProgress> promise = PromiseProgress<TProgress>.GetOrCreate(progress, cancelationToken);
                    _this._ref.HookupNewPromiseWithProgress(promise);
                    return new Promise<TResult>(promise, promise.Id, promise.Depth);
                }
#endif
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            // This helps reduce typed out generics.
            // The C# compiler does not use generic constraints for automatic type inference, so the class must be made generic instead.
            // <TArg, TResult, TDelegate>() where TDelegate : IDelegate<TArg, TResult>
            internal static class CallbackHelper<TArg, TResult>
            {
                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolve<TResolver>(Promise<TArg> _this, TResolver resolver, CancelationToken cancelationToken)
                    where TResolver : IDelegate<TArg, TResult>
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolve<TArg, TResult, TResolver>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth(_this.Depth);
                            //Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                            //AddToHandleQueueBack(promise);
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this.Id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<TArg, TResult, TResolver>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolve<TArg, TResult, TResolver>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, promise.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TResolver>(Promise<TArg> _this, TResolver resolver, CancelationToken cancelationToken)
                    where TResolver : IDelegate<TArg, Promise<TResult>>
                {
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolvePromise<TArg, TResult, TResolver>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this.Id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<TArg, TResult, TResolver>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<TArg, TResult, TResolver>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, promise.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinue<TContinuer>(Promise<TArg> _this, TContinuer resolver, CancelationToken cancelationToken)
                    where TContinuer : IDelegateContinue, IDelegate<TArg, TResult>
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseContinue<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this.Id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinue<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseContinue<TContinuer>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, promise.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TContinuer>(Promise<TArg> _this, TContinuer resolver, CancelationToken cancelationToken)
                    where TContinuer : IDelegateContinuePromise, IDelegate<TArg, Promise<TResult>>
                {
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseContinuePromise<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this.Id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinuePromise<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseContinuePromise<TContinuer>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, promise.Depth);
                }
            } // CallbackHelper<TArg, TResult>

            internal static class CallbackHelper<TArgResolve, TArgReject, TResult>
            {
                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveReject<TResolver, TRejecter>(Promise<TArgResolve> _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken)
                    where TResolver : IDelegate<TArgResolve, TResult>
                    where TRejecter : IDelegate<TArgReject, TResult>
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveReject<TArgResolve, TResult, TResolver, TArgReject, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise.SetDepth();
                        }
                        else
                        {
                            return Invoker<TArgResolve, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this.Id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<TArgResolve, TResult, TResolver, TArgReject, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveReject<TArgResolve, TResult, TResolver, TArgReject, TRejecter>.GetOrCreate(resolver, rejecter);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, promise.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TResolver, TRejecter>(Promise<TArgResolve> _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken)
                    where TResolver : IDelegate<TArgResolve, Promise<TResult>>
                    where TRejecter : IDelegate<TArgReject, Promise<TResult>>
                {
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise.SetDepth();
                        }
                        else
                        {
                            return Invoker<TArgResolve, TResult>.InvokeCallbackAndAdoptDirect(resolver, _this);
                        }
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this.Id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter>.GetOrCreate(resolver, rejecter);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, promise.Depth);
                }
            } // CallbackHelper<TArgResolve, TArgReject, TResult>
        } // PromiseRef
    } // Internal
}