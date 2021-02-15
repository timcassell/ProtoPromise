#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Runtime.CompilerServices;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            // These static functions help with the implementation so we don't need to type the generics every time.
            internal static class DelegateWrapper
            {
                [MethodImpl(InlineOption)]
                public static DelegateResolvePassthrough CreatePassthrough()
                {
                    return new DelegateResolvePassthrough(true);
                }

                [MethodImpl(InlineOption)]
                public static DelegateVoidVoid Create(Action callback)
                {
                    return new DelegateVoidVoid(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateVoidResult<TResult> Create<TResult>(Func<TResult> callback)
                {
                    return new DelegateVoidResult<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateVoidPromise Create(Func<Promise> callback)
                {
                    return new DelegateVoidPromise(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateVoidPromiseT<TResult> Create<TResult>(Func<Promise<TResult>> callback)
                {
                    return new DelegateVoidPromiseT<TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateArgVoid<TArg> Create<TArg>(Action<TArg> callback)
                {
                    return new DelegateArgVoid<TArg>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateArgResult<TArg, TResult> Create<TArg, TResult>(Func<TArg, TResult> callback)
                {
                    return new DelegateArgResult<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateArgPromise<TArg> Create<TArg>(Func<TArg, Promise> callback)
                {
                    return new DelegateArgPromise<TArg>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateArgPromiseT<TArg, TResult> Create<TArg, TResult>(Func<TArg, Promise<TResult>> callback)
                {
                    return new DelegateArgPromiseT<TArg, TResult>(callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureVoidVoid<TCapture> Create<TCapture>(ref TCapture capturedValue, Action<TCapture> callback)
                {
                    return new DelegateCaptureVoidVoid<TCapture>(ref capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(ref TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    return new DelegateCaptureVoidResult<TCapture, TResult>(ref capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureVoidPromise<TCapture> Create<TCapture>(ref TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    return new DelegateCaptureVoidPromise<TCapture>(ref capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureVoidPromiseT<TCapture, TResult> Create<TCapture, TResult>(ref TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateCaptureVoidPromiseT<TCapture, TResult>(ref capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(ref TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    return new DelegateCaptureArgVoid<TCapture, TArg>(ref capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(ref TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    return new DelegateCaptureArgResult<TCapture, TArg, TResult>(ref capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureArgPromise<TCapture, TArg> Create<TCapture, TArg>(ref TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    return new DelegateCaptureArgPromise<TCapture, TArg>(ref capturedValue, callback);
                }

                [MethodImpl(InlineOption)]
                public static DelegateCaptureArgPromiseT<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(ref TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
                {
                    return new DelegateCaptureArgPromiseT<TCapture, TArg, TResult>(ref capturedValue, callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateResolvePassthrough : IDelegateResolve, IDelegateResolvePromise
            {
                private readonly bool _isActive;

                internal DelegateResolvePassthrough(bool isActive)
                {
                    _isActive = isActive;
                }

                public bool IsNull { get { return !_isActive; } }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    owner.ResolveInternal(valueContainer);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.ResolveInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }

            #region Regular Delegates
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class FinallyDelegate : ITreeHandleable, ITraceable
            {
                internal struct Creator : ICreator<FinallyDelegate>
                {
                    [MethodImpl(InlineOption)]
                    public FinallyDelegate Create()
                    {
                        return new FinallyDelegate();
                    }
                }

#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private Action _onFinally;

                private FinallyDelegate() { }

                [MethodImpl(InlineOption)]
                public static FinallyDelegate GetOrCreate(Action onFinally)
                {
                    var del = ObjectPool<ITreeHandleable>.GetOrCreate<FinallyDelegate, Creator>(new Creator());
                    del._onFinally = onFinally;
                    SetCreatedStacktrace(del, 2);
                    return del;
                }

                [MethodImpl(InlineOption)]
                void Dispose()
                {
                    _onFinally = null;
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                void ITreeHandleable.Handle()
                {
                    ThrowIfInPool(this);
                    var callback = _onFinally;
                    SetCurrentInvoker(this);
#if PROMISE_DEBUG
                    var traceContainer = new CausalityTraceContainer(this); // Store the causality trace so that this can be disposed before the callback is invoked.
#endif
                    Dispose();
                    try
                    {
                        callback.Invoke();
                    }
                    catch (Exception e)
                    {
#if PROMISE_DEBUG
                        AddRejectionToUnhandledStack(e, traceContainer);
#else
                        AddRejectionToUnhandledStack(e, null);
#endif
                    }
                    finally
                    {
                        ClearCurrentInvoker();
                    }
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    ThrowIfInPool(this);
                    handleQueue.Push(this);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    AddToHandleQueueBack(this);
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateVoidVoid : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Action _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateVoidVoid(Action callback)
                {
                    _callback = callback;
                }

                private void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke();
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateArgVoid<TArg> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Action<TArg> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateArgVoid(Action<TArg> callback)
                {
                    _callback = callback;
                }

                private void Invoke(TArg arg, IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(arg);
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    Invoke(arg, valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            Invoke(arg, valueContainer, owner);
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateVoidResult<TResult> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<TResult> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateVoidResult(Func<TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke();
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateArgResult<TArg, TResult> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<TArg, TResult> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateArgResult(Func<TArg, TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(TArg arg, IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(arg);
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    Invoke(arg, valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            Invoke(arg, valueContainer, owner);
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateVoidPromise : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateVoidPromise(Func<Promise> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke();
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateArgPromise<TArg> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<TArg, Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateArgPromise(Func<TArg, Promise> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(TArg arg, IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(arg);
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    Invoke(arg, valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            Invoke(arg, valueContainer, owner);
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateVoidPromiseT<TPromise> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<Promise<TPromise>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateVoidPromiseT(Func<Promise<TPromise>> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke();
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateArgPromiseT<TArg, TPromise> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<TArg, Promise<TPromise>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateArgPromiseT(Func<TArg, Promise<TPromise>> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(TArg arg, IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(arg);
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    Invoke(arg, valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            Invoke(arg, valueContainer, owner);
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueVoidVoid : IDelegateContinue
            {
                private readonly Promise.ContinueAction _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueVoidVoid(Promise.ContinueAction callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueVoidResult<TResult> : IDelegateContinue
            {
                private readonly Promise.ContinueFunc<TResult> _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueVoidResult(Promise.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueArgVoid<TArg> : IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueAction _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueArgVoid(Promise<TArg>.ContinueAction callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueArgResult<TArg, TResult> : IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueFunc<TResult> _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueArgResult(Promise<TArg>.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueVoidPromise : IDelegateContinuePromise
            {
                private readonly Promise.ContinueFunc<Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueVoidPromise(Promise.ContinueFunc<Promise> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueVoidPromiseT<TPromise> : IDelegateContinuePromise
            {
                private readonly Promise.ContinueFunc<Promise<TPromise>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueVoidPromiseT(Promise.ContinueFunc<Promise<TPromise>> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueArgPromise<TArg> : IDelegateContinuePromise
            {
                private readonly Promise<TArg>.ContinueFunc<Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueArgPromise(Promise<TArg>.ContinueFunc<Promise> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueArgPromiseT<TArg, TPromise> : IDelegateContinuePromise
            {
                private readonly Promise<TArg>.ContinueFunc<Promise<TPromise>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueArgPromiseT(Promise<TArg>.ContinueFunc<Promise<TPromise>> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }
            #endregion

            #region Delegates with capture value
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            public sealed class FinallyDelegateCapture<TCapture> : ITreeHandleable, ITraceable
            {
                internal struct Creator : ICreator<FinallyDelegateCapture<TCapture>>
                {
                    [MethodImpl(InlineOption)]
                    public FinallyDelegateCapture<TCapture> Create()
                    {
                        return new FinallyDelegateCapture<TCapture>();
                    }
                }

#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private TCapture _capturedValue;
                private Action<TCapture> _onFinally;

                private FinallyDelegateCapture() { }

                [MethodImpl(InlineOption)]
                public static FinallyDelegateCapture<TCapture> GetOrCreate(ref TCapture capturedValue, Action<TCapture> onFinally)
                {
                    var del = ObjectPool<ITreeHandleable>.GetOrCreate<FinallyDelegateCapture<TCapture>, Creator>(new Creator());
                    del._capturedValue = capturedValue;
                    del._onFinally = onFinally;
                    SetCreatedStacktrace(del, 2);
                    return del;
                }

                [MethodImpl(InlineOption)]
                void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _onFinally = null;
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                void ITreeHandleable.Handle()
                {
                    ThrowIfInPool(this);
                    var value = _capturedValue;
                    var callback = _onFinally;
                    SetCurrentInvoker(this);
#if PROMISE_DEBUG
                    var traceContainer = new CausalityTraceContainer(this); // Store the causality trace so that this can be disposed before the callback is invoked.
#endif
                    Dispose();
                    try
                    {
                        callback.Invoke(value);
                    }
                    catch (Exception e)
                    {
#if PROMISE_DEBUG
                        AddRejectionToUnhandledStack(e, traceContainer);
#else
                        AddRejectionToUnhandledStack(e, null);
#endif
                    }
                    ClearCurrentInvoker();
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    ThrowIfInPool(this);
                    handleQueue.Push(this);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    AddToHandleQueueBack(this);
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureVoidVoid<TCapture> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Action<TCapture> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureVoidVoid(ref TCapture capturedValue, Action<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue);
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureArgVoid<TCapture, TArg> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Action<TCapture, TArg> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureArgVoid(ref TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(TArg arg, IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue, arg);
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    Invoke(arg, valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            Invoke(arg, valueContainer, owner);
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureVoidResult<TCapture, TResult> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TResult> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureVoidResult(ref TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue);
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureArgResult<TCapture, TArg, TResult> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TArg, TResult> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureArgResult(ref TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(TArg arg, IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue, arg);
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    Invoke(arg, valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            Invoke(arg, valueContainer, owner);
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureVoidPromise<TCapture> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureVoidPromise(ref TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(_capturedValue);
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureArgPromise<TCapture, TArg> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TArg, Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureArgPromise(ref TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(TArg arg, IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(_capturedValue, arg);
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    Invoke(arg, valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            Invoke(arg, valueContainer, owner);
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureVoidPromiseT<TCapture, TPromise> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                public readonly Func<TCapture, Promise<TPromise>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureVoidPromiseT(ref TCapture capturedValue, Func<TCapture, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(_capturedValue);
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(valueContainer, owner);
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TArg, Promise<TPromise>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureArgPromiseT(ref TCapture capturedValue, Func<TCapture, TArg, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                private void Invoke(TArg arg, IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(_capturedValue, arg);
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    Invoke(arg, valueContainer, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }

                [MethodImpl(InlineOption)]
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(arg, valueContainer, owner);
                    }
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        if (cancelationHelper.TryUnregister(owner))
                        {
                            Invoke(arg, valueContainer, owner);
                        }
                    }
                    else if (cancelationHelper.TryUnregister(owner))
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureVoidVoid<TCapture> : IDelegateContinue
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueAction<TCapture> _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureVoidVoid(ref TCapture capturedValue, Promise.ContinueAction<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureVoidResult<TCapture, TResult> : IDelegateContinue
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueFunc<TCapture, TResult> _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureVoidResult(ref TCapture capturedValue, Promise.ContinueFunc<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureArgVoid<TCapture, TArg> : IDelegateContinue
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueAction<TCapture> _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureArgVoid(ref TCapture capturedValue, Promise<TArg>.ContinueAction<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureArgResult<TCapture, TArg, TResult> : IDelegateContinue
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueFunc<TCapture, TResult> _callback;

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureArgResult(ref TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureVoidPromise<TCapture> : IDelegateContinuePromise
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueFunc<TCapture, Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureVoidPromise(ref TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureVoidPromiseT<TCapture, TPromise> : IDelegateContinuePromise
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueFunc<TCapture, Promise<TPromise>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureVoidPromiseT(ref TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureArgPromise<TCapture, TArg> : IDelegateContinuePromise
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureArgPromise(ref TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureArgPromiseT<TCapture, TArg, TPromise> : IDelegateContinuePromise
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise<TPromise>> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureArgPromiseT(ref TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    var result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseRef owner, ref CancelationHelper cancelationHelper)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner);
                    }
                }
            }
            #endregion
        }
    }
}