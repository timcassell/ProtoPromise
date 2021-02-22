using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            internal static class CallbackHelper
            {
                #region Promise Void
                [MethodImpl(InlineOption)]
                internal static void AddResolve<TResolver>(Promise _this, TResolver resolver, CancelationToken cancelationToken, out Promise newPromise)
                    where TResolver : IDelegateResolve
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<TResolver>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseResolve<TResolver>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<TResolver>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolve<TResolver>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolve<TResolver, TResult>(Promise _this, TResolver resolver, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TResolver : IDelegateResolve
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<TResolver>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseResolve<TResolver>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<TResolver>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolve<TResolver>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveWait<TResolver>(Promise _this, TResolver resolver, CancelationToken cancelationToken, out Promise newPromise)
                    where TResolver : IDelegateResolvePromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<TResolver>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<TResolver>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<TResolver>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<TResolver>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveWait<TResolver, TResult>(Promise _this, TResolver resolver, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TResolver : IDelegateResolvePromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<TResolver>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<TResolver>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<TResolver>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<TResolver>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveReject<TResolver, TRejecter>(Promise _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, out Promise newPromise)
                    where TResolver : IDelegateResolve
                    where TRejecter : IDelegateReject
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveReject<TResolver, TRejecter, TResult>(Promise _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TResolver : IDelegateResolve
                    where TRejecter : IDelegateReject
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveRejectWait<TResolver, TRejecter>(Promise _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, out Promise newPromise)
                    where TResolver : IDelegateResolvePromise
                    where TRejecter : IDelegateRejectPromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveRejectWait<TResolver, TRejecter, TResult>(Promise _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TResolver : IDelegateResolvePromise
                    where TRejecter : IDelegateRejectPromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddContinue<TContinuer>(Promise _this, TContinuer resolver, CancelationToken cancelationToken, out Promise newPromise)
                    where TContinuer : IDelegateContinue
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinue<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseContinue<TContinuer>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
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
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddContinue<TContinuer, TResult>(Promise _this, TContinuer resolver, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TContinuer : IDelegateContinue
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinue<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseContinue<TContinuer>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
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
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddContinueWait<TContinuer>(Promise _this, TContinuer resolver, CancelationToken cancelationToken, out Promise newPromise)
                    where TContinuer : IDelegateContinuePromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinuePromise<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseContinuePromise<TContinuer>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
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
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddContinueWait<TContinuer, TResult>(Promise _this, TContinuer resolver, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TContinuer : IDelegateContinuePromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinuePromise<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);
                        }
                        else
                        {
                            promise = PromiseContinuePromise<TContinuer>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
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
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddFinally<TFinally>(Promise _this, TFinally finalizer)
                    where TFinally : IDelegateSimple
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        promise = PromiseFinally<TFinally>.GetOrCreate(finalizer);
                        promise.SetDepth();
                        promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        promise = PromiseFinally<TFinally>.GetOrCreate(finalizer);
                        _this._ref.HookupNewPromise(promise);
                    }
                    return new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise AddCancel<TCanceler>(Promise _this, TCanceler canceler, CancelationToken cancelationToken)
                    where TCanceler : IDelegateSimple
                {
                    if (_this._ref == null)
                    {
                        return _this;
                    }

                    PromiseRef promise;
                    _this._ref.MarkAwaited(_this._id);
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
                    return new Promise(promise, promise.Id);
                }
                #endregion

                #region Promise<T>
                [MethodImpl(InlineOption)]
                internal static void AddResolve<T, TResolver>(Promise<T> _this, TResolver resolver, CancelationToken cancelationToken, out Promise newPromise)
                    where TResolver : IDelegateResolve
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<TResolver>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, ResolveContainer<T>.GetOrCreate(_this._result), null);
                        }
                        else
                        {
                            promise = PromiseResolve<TResolver>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = ResolveContainer<T>.GetOrCreate(_this._result);
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<TResolver>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolve<TResolver>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolve<T, TResolver, TResult>(Promise<T> _this, TResolver resolver, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TResolver : IDelegateResolve
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<TResolver>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseResolve<TResolver>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolve<TResolver>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolve<TResolver>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveWait<T, TResolver>(Promise<T> _this, TResolver resolver, CancelationToken cancelationToken, out Promise newPromise)
                    where TResolver : IDelegateResolvePromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<TResolver>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<TResolver>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<TResolver>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<TResolver>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveWait<T, TResolver, TResult>(Promise<T> _this, TResolver resolver, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TResolver : IDelegateResolvePromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<TResolver>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<TResolver>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolvePromise<TResolver>.GetOrCreate(resolver, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolvePromise<TResolver>.GetOrCreate(resolver);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveReject<T, TResolver, TRejecter>(Promise<T> _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, out Promise newPromise)
                    where TResolver : IDelegateResolve
                    where TRejecter : IDelegateReject
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveReject<T, TResolver, TRejecter, TResult>(Promise<T> _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TResolver : IDelegateResolve
                    where TRejecter : IDelegateReject
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveReject<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveRejectWait<T, TResolver, TRejecter>(Promise<T> _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, out Promise newPromise)
                    where TResolver : IDelegateResolvePromise
                    where TRejecter : IDelegateRejectPromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddResolveRejectWait<T, TResolver, TRejecter, TResult>(Promise<T> _this, TResolver resolver, TRejecter rejecter, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TResolver : IDelegateResolvePromise
                    where TRejecter : IDelegateRejectPromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter, cancelationToken);
                            _this._ref.HookupNewCancelablePromise(promise);
                        }
                        else
                        {
                            promise = PromiseResolveRejectPromise<TResolver, TRejecter>.GetOrCreate(resolver, rejecter);
                            _this._ref.HookupNewPromise(promise);
                        }
                    }
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddContinue<T, TContinuer>(Promise<T> _this, TContinuer resolver, CancelationToken cancelationToken, out Promise newPromise)
                    where TContinuer : IDelegateContinue
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinue<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseContinue<TContinuer>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
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
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddContinue<T, TContinuer, TResult>(Promise<T> _this, TContinuer resolver, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TContinuer : IDelegateContinue
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinue<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseContinue<TContinuer>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
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
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddContinueWait<T, TContinuer>(Promise<T> _this, TContinuer resolver, CancelationToken cancelationToken, out Promise newPromise)
                    where TContinuer : IDelegateContinuePromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinuePromise<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseContinuePromise<TContinuer>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
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
                    newPromise = new Promise(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static void AddContinueWait<T, TContinuer, TResult>(Promise<T> _this, TContinuer resolver, CancelationToken cancelationToken, out Promise<TResult> newPromise)
                    where TContinuer : IDelegateContinuePromise
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        object container = ResolveContainer<T>.GetOrCreate(_this._result);
                        if (cancelationToken.CanBeCanceled)
                        {
                            promise = CancelablePromiseContinuePromise<TContinuer>.GetOrCreate(resolver, cancelationToken);
                            promise.SetDepth();
                            Interlocked.CompareExchange(ref promise._valueOrPrevious, container, null);
                        }
                        else
                        {
                            promise = PromiseContinuePromise<TContinuer>.GetOrCreate(resolver);
                            promise.SetDepth();
                            promise._valueOrPrevious = container;
                        }
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
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
                    newPromise = new Promise<TResult>(promise, promise.Id);
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> AddFinally<TFinally, TResult>(Promise<TResult> _this, TFinally finalizer)
                    where TFinally : IDelegateSimple
                {
                    PromiseRef promise;
                    if (_this._ref == null)
                    {
                        promise = PromiseFinally<TFinally>.GetOrCreate(finalizer);
                        promise.SetDepth();
                        promise._valueOrPrevious = ResolveContainer<TResult>.GetOrCreate(_this._result);
                        AddToHandleQueueBack(promise);
                    }
                    else
                    {
                        _this._ref.MarkAwaited(_this._id);
                        promise = PromiseFinally<TFinally>.GetOrCreate(finalizer);
                        _this._ref.HookupNewPromise(promise);
                    }
                    return new Promise<TResult>(promise, promise.Id);
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
                    _this._ref.MarkAwaited(_this._id);
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
                    return new Promise<TResult>(promise, promise.Id);
                }
                #endregion
            } // CallbackHelper
        } // PromiseRef
    } // Internal
}