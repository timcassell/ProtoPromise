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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        internal enum SynchronizationOption
        {
            Synchronous,
            Foreground,
            Background,
            Explicit
        }

        partial class PromiseRef
        {
            private static class Invoker<TArg, TResult>
            {
                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackAndAdoptDirect<TDelegate>(TDelegate resolver, Promise<TArg> resolved) where TDelegate : IDelegate<TArg, Promise<TResult>>
                {
                    try
                    {
                        return CallbackHelper.AdoptDirect(resolver.Invoke(resolved.Result), resolved.Depth);
                    }
                    catch (OperationCanceledException e)
                    {
                        var promise = Promise<TResult>.Canceled(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> InvokeCallbackDirect<TDelegate>(TDelegate resolver, Promise<TArg> resolved) where TDelegate : IDelegate<TArg, TResult>
                {
                    try
                    {
                        TResult result = resolver.Invoke(resolved.Result);
                        return new Promise<TResult>(null, ValidIdFromApi, resolved.Depth, result);
                    }
                    catch (OperationCanceledException e)
                    {
                        var promise = Promise<TResult>.Canceled(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                    catch (Exception e)
                    {
                        var promise = Promise<TResult>.Rejected(e);
                        return new Promise<TResult>(promise._ref, promise.Id, resolved.Depth + 1, promise.Result);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class CallbackHelper
            {
#if !PROMISE_PROGRESS
                [MethodImpl(InlineOption)]
#endif
                internal static Promise<TResult> AdoptDirect<TResult>(Promise<TResult> promise, int currentDepth)
                {
#if !PROMISE_PROGRESS
                    return promise;
#else
                    if (promise._ref == null)
                    {
                        return new Promise<TResult>(null, ValidIdFromApi, currentDepth + 1, promise.Result);
                    }
#if !PROMISE_DEBUG
                    if (promise._ref.State == Promise.State.Resolved)
                    {
                        TResult result = ((IValueContainer) promise._ref._valueOrPrevious).GetValue<TResult>();
                        promise.Forget();
                        return new Promise<TResult>(null, ValidIdFromApi, currentDepth + 1, result);
                    }
#endif
                    // Normalize progress. Passing a default resolver makes the Execute method adopt the promise's state without attempting to invoke.
                    var newRef = PromiseResolvePromise<TResult, TResult, DelegateResolvePassthrough<TResult>>.GetOrCreate(default(DelegateResolvePassthrough<TResult>), currentDepth + 1);
                    newRef.WaitForWithprogress(promise);
                    return new Promise<TResult>(newRef, newRef.Id, currentDepth + 1);
#endif
                }

                internal static Promise<TResult> WaitAsync<TResult>(Promise<TResult> _this, SynchronizationOption continuationOption, SynchronizationContext synchronizationContext)
                {
                    PromiseRef newPromise;
                    switch (continuationOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            if (_this._ref == null)
                            {
                                return _this;
                            }
                            newPromise = _this._ref.GetDuplicate(_this.Id);
                            break;
                        }
                        case SynchronizationOption.Foreground:
                        {
                            synchronizationContext = Promise.Config.ForegroundContext;
                            if (synchronizationContext == null)
                            {
                                throw new InvalidOperationException(
                                    "Promise.ContinuationOption.Foreground was provided to WaitAsync, but Promise.Config.ForegroundContext was null. " +
                                    "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                                    GetFormattedStacktrace(2));
                            }
                            goto default;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext;
                            goto default;
                        }
                        default: // ContinuationOption.Explicit
                        {
                            if (_this._ref == null)
                            {
                                newPromise = ConfiguredPromise.GetOrCreate(false, synchronizationContext);
                            }
                            else
                            {
                                _this._ref.MarkAwaited(_this.Id);
                                newPromise = ConfiguredPromise.GetOrCreate(false, synchronizationContext);
                                _this._ref.HookupNewPromise(newPromise);
                            }
                            break;
                        }
                    }
                    return new Promise<TResult>(newPromise, newPromise.Id, _this.Depth, _this.Result);
                }

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
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
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
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static int GetNextDepth(int depth)
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
                internal static void InvokeAndCatchProgress<TProgress>(ref TProgress progress, float value, ITraceable traceable)
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
                internal static Promise<TResult> AddProgress<TResult, TProgress>(Promise<TResult> _this, TProgress progress, CancelationToken cancelationToken, SynchronizationOption continuationOption, SynchronizationContext synchronizationContext)
                    where TProgress : IProgress<float>
                {
                    if (cancelationToken.IsCancelationRequested)
                    {
                        return _this.Duplicate();
                    }

                    switch (continuationOption)
                    {
                        case SynchronizationOption.Synchronous:
                        {
                            if (_this._ref == null)
                            {
                                InvokeAndCatchProgress(ref progress, 1, null);
                                return new Promise<TResult>(null, ValidIdFromApi, _this.Depth, _this.Result);
                            }
                            // TODO:
//#if !PROMISE_DEBUG
//                            else if (_this._ref.State == Promise.State.Resolved)
//                            {
//                                InvokeAndCatchProgress(ref progress, 1, null);
//                            }
//#endif
                            break;
                        }
                        case SynchronizationOption.Foreground:
                        {
                            synchronizationContext = Promise.Config.ForegroundContext;
                            if (synchronizationContext == null)
                            {
                                throw new InvalidOperationException(
                                    "Promise.ContinuationOption.Foreground was provided to Progress, but Promise.Config.ForegroundContext was null. " +
                                    "You should set Promise.Config.ForegroundContext at the start of your application (which may be as simple as 'Promise.Config.ForegroundContext = SynchronizationContext.Current;').",
                                    GetFormattedStacktrace(2));
                            }
                            break;
                        }
                        case SynchronizationOption.Background:
                        {
                            synchronizationContext = Promise.Config.BackgroundContext;
                            break;
                        }
                    }

                    _this._ref.MarkAwaited(_this.Id);
                    PromiseProgress<TProgress> promise = PromiseProgress<TProgress>.GetOrCreate(progress, cancelationToken, _this.Depth, continuationOption == SynchronizationOption.Synchronous, synchronizationContext);
                    _this._ref.HookupNewPromiseWithProgress(promise, _this.Depth);
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }
#endif
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            // This helps reduce typed out generics.
            // The C# compiler does not use generic constraints for automatic type inference, so the class must be made generic instead.
            // <TArg, TResult, TDelegate>(TDelegate arg) where TDelegate : IDelegate<TArg, TResult>
            internal static class CallbackHelper2<TArg, TResult>
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
                            //Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                            //AddToHandleQueueBack(promise);
                        }
                        else
                        {
                            return Invoker<TArg, TResult>.InvokeCallbackDirect(resolver, _this);
                        }
                    }
                    // TODO: sync callback if ref-backed and already complete in RELEASE mode only.
                    // else if (_this._ref.State != Promise.State.Pending) { }
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
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveWait<TResolver>(Promise<TArg> _this, TResolver resolver, CancelationToken cancelationToken)
                    where TResolver : IDelegate<TArg, Promise<TResult>>
                {
                    int nextDepth = CallbackHelper.GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolvePromise<TArg, TResult, TResolver>.GetOrCreate(resolver, cancelationToken, nextDepth);
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
                            promise = CancelablePromiseResolvePromise<TArg, TResult, TResolver>.GetOrCreate(resolver, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<TArg, TResult, TResolver>.GetOrCreate(resolver, nextDepth);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
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
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddContinueWait<TContinuer>(Promise<TArg> _this, TContinuer resolver, CancelationToken cancelationToken)
                    where TContinuer : IDelegateContinuePromise, IDelegate<TArg, Promise<TResult>>
                {
                    int nextDepth = CallbackHelper.GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseContinuePromise<TContinuer>.GetOrCreate(resolver, cancelationToken, nextDepth);
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
                            promise = CancelablePromiseContinuePromise<TContinuer>.GetOrCreate(resolver, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseContinuePromise<TContinuer>.GetOrCreate(resolver, nextDepth);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }
            } // CallbackHelper<TArg, TResult>

            internal static class CallbackHelper3<TArgResolve, TArgReject, TResult>
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
                    return new Promise<TResult>(promise, promise.Id, _this.Depth);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddResolveRejectWait<TResolver, TRejecter>(Promise<TArgResolve> _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken)
                    where TResolver : IDelegate<TArgResolve, Promise<TResult>>
                    where TRejecter : IDelegate<TArgReject, Promise<TResult>>
                {
                    int nextDepth = CallbackHelper.GetNextDepth(_this.Depth);
                    PromiseWaitPromise promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
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
                            promise = CancelablePromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken, nextDepth);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<TArgResolve, TResult, TResolver, TArgReject, TRejecter>.GetOrCreate(resolver, rejecter, nextDepth);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    return new Promise<TResult>(promise, promise.Id, nextDepth);
                }
            } // CallbackHelper<TArgResolve, TArgReject, TResult>
        } // PromiseRef
    } // Internal
}