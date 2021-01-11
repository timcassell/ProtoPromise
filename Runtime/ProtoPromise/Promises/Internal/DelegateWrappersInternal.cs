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
            private static class DelegateWrapper
            {
                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateResolvePassthroughCancel CreatePassthroughCancelable()
                {
                    return new DelegateResolvePassthroughCancel(true);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateResolvePassthrough CreatePassthrough()
                {
                    return new DelegateResolvePassthrough();
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateVoidVoidCancel CreateCancelable(Action callback)
                {
                    return new DelegateVoidVoidCancel(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateVoidVoid Create(Action callback)
                {
                    return new DelegateVoidVoid(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateVoidResultCancel<TResult> CreateCancelable<TResult>(Func<TResult> callback)
                {
                    return new DelegateVoidResultCancel<TResult>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateVoidResult<TResult> Create<TResult>(Func<TResult> callback)
                {
                    return new DelegateVoidResult<TResult>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateVoidPromiseCancel CreateCancelable(Func<Promise> callback)
                {
                    return new DelegateVoidPromiseCancel(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateVoidPromise Create(Func<Promise> callback)
                {
                    return new DelegateVoidPromise(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateVoidPromiseTCancel<TResult> CreateCancelable<TResult>(Func<Promise<TResult>> callback)
                {
                    return new DelegateVoidPromiseTCancel<TResult>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateVoidPromiseT<TResult> Create<TResult>(Func<Promise<TResult>> callback)
                {
                    return new DelegateVoidPromiseT<TResult>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateArgVoidCancel<TArg> CreateCancelable<TArg>(Action<TArg> callback)
                {
                    return new DelegateArgVoidCancel<TArg>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateArgVoid<TArg> Create<TArg>(Action<TArg> callback)
                {
                    return new DelegateArgVoid<TArg>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateArgResultCancel<TArg, TResult> CreateCancelable<TArg, TResult>(Func<TArg, TResult> callback)
                {
                    return new DelegateArgResultCancel<TArg, TResult>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateArgResult<TArg, TResult> Create<TArg, TResult>(Func<TArg, TResult> callback)
                {
                    return new DelegateArgResult<TArg, TResult>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateArgPromiseCancel<TArg> CreateCancelable<TArg>(Func<TArg, Promise> callback)
                {
                    return new DelegateArgPromiseCancel<TArg>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateArgPromise<TArg> Create<TArg>(Func<TArg, Promise> callback)
                {
                    return new DelegateArgPromise<TArg>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateArgPromiseTCancel<TArg, TResult> CreateCancelable<TArg, TResult>(Func<TArg, Promise<TResult>> callback)
                {
                    return new DelegateArgPromiseTCancel<TArg, TResult>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateArgPromiseT<TArg, TResult> Create<TArg, TResult>(Func<TArg, Promise<TResult>> callback)
                {
                    return new DelegateArgPromiseT<TArg, TResult>(callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureVoidVoidCancel<TCapture> CreateCancelable<TCapture>(ref TCapture capturedValue, Action<TCapture> callback)
                {
                    return new DelegateCaptureVoidVoidCancel<TCapture>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureVoidVoid<TCapture> Create<TCapture>(ref TCapture capturedValue, Action<TCapture> callback)
                {
                    return new DelegateCaptureVoidVoid<TCapture>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureVoidResultCancel<TCapture, TResult> CreateCancelable<TCapture, TResult>(ref TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    return new DelegateCaptureVoidResultCancel<TCapture, TResult>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureVoidResult<TCapture, TResult> Create<TCapture, TResult>(ref TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    return new DelegateCaptureVoidResult<TCapture, TResult>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureVoidPromiseCancel<TCapture> CreateCancelable<TCapture>(ref TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    return new DelegateCaptureVoidPromiseCancel<TCapture>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureVoidPromise<TCapture> Create<TCapture>(ref TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    return new DelegateCaptureVoidPromise<TCapture>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureVoidPromiseTCancel<TCapture, TResult> CreateCancelable<TCapture, TResult>(ref TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateCaptureVoidPromiseTCancel<TCapture, TResult>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureVoidPromiseT<TCapture, TResult> Create<TCapture, TResult>(ref TCapture capturedValue, Func<TCapture, Promise<TResult>> callback)
                {
                    return new DelegateCaptureVoidPromiseT<TCapture, TResult>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureArgVoidCancel<TCapture, TArg> CreateCancelable<TCapture, TArg>(ref TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    return new DelegateCaptureArgVoidCancel<TCapture, TArg>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureArgVoid<TCapture, TArg> Create<TCapture, TArg>(ref TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    return new DelegateCaptureArgVoid<TCapture, TArg>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureArgResultCancel<TCapture, TArg, TResult> CreateCancelable<TCapture, TArg, TResult>(ref TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    return new DelegateCaptureArgResultCancel<TCapture, TArg, TResult>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureArgResult<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(ref TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    return new DelegateCaptureArgResult<TCapture, TArg, TResult>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureArgPromiseCancel<TCapture, TArg> CreateCancelable<TCapture, TArg>(ref TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    return new DelegateCaptureArgPromiseCancel<TCapture, TArg>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureArgPromise<TCapture, TArg> Create<TCapture, TArg>(ref TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    return new DelegateCaptureArgPromise<TCapture, TArg>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureArgPromiseTCancel<TCapture, TArg, TResult> CreateCancelable<TCapture, TArg, TResult>(ref TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
                {
                    return new DelegateCaptureArgPromiseTCancel<TCapture, TArg, TResult>(ref capturedValue, callback);
                }

                [MethodImpl((MethodImplOptions) 256)]
                public static DelegateCaptureArgPromiseT<TCapture, TArg, TResult> Create<TCapture, TArg, TResult>(ref TCapture capturedValue, Func<TCapture, TArg, Promise<TResult>> callback)
                {
                    return new DelegateCaptureArgPromiseT<TCapture, TArg, TResult>(ref capturedValue, callback);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateResolvePassthrough : IDelegateResolve, IDelegateResolvePromise
            {
                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    owner.ResolveInternal(valueContainer);
                }

                public void MaybeUnregisterCancelation() { }
                public bool IsNull { get { throw new System.InvalidOperationException(); } }
            }

            #region Regular Delegates
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class FinallyDelegate : ITreeHandleable, ITraceable
            {
                private struct Creator : ICreator<FinallyDelegate>
                {
                    [MethodImpl((MethodImplOptions) 256)]
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

                public static FinallyDelegate GetOrCreate(Action onFinally)
                {
                    var del = ObjectPool<ITreeHandleable>.GetOrCreate<FinallyDelegate, Creator>(new Creator());
                    del._onFinally = onFinally;
                    SetCreatedStacktrace(del, 2);
                    return del;
                }

                private void InvokeAndCatchAndDispose()
                {
                    var callback = _onFinally;
                    SetCurrentInvoker(this);
                    Dispose();
                    try
                    {
                        callback.Invoke();
                    }
                    catch (Exception e)
                    {
                        AddRejectionToUnhandledStack(e, this);
                    }
                    ClearCurrentInvoker();
                }

                void Dispose()
                {
                    _onFinally = null;
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                void ITreeHandleable.Handle()
                {
                    ThrowIfInPool(this);
                    InvokeAndCatchAndDispose();
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
            private struct DelegateVoidVoid : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Action _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateVoidVoid(Action callback)
                {
                    _callback = callback;
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateArgVoid<TArg> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Action<TArg> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateArgVoid(Action<TArg> callback)
                {
                    _callback = callback;
                }

                private void Invoke(TArg arg, PromiseRef owner)
                {
                    _callback.Invoke(arg);
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateVoidResult<TResult> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<TResult> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateVoidResult(Func<TResult> callback)
                {
                    _callback = callback;
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateArgResult<TArg, TResult> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<TArg, TResult> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateArgResult(Func<TArg, TResult> callback)
                {
                    _callback = callback;
                }

                private void Invoke(TArg arg, PromiseRef owner)
                {
                    var temp = _callback;
                    TResult result = temp.Invoke(arg);
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                }

                public void MaybeUnregisterCancelation() { }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateVoidPromise : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<Promise> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateVoidPromise(Func<Promise> callback)
                {
                    _callback = callback;
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke());
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke());
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateArgPromise<TArg> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<TArg, Promise> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateArgPromise(Func<TArg, Promise> callback)
                {
                    _callback = callback;
                }

                private void Invoke(TArg arg, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(arg));
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateVoidPromiseT<TPromise> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<Promise<TPromise>> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateVoidPromiseT(Func<Promise<TPromise>> callback)
                {
                    _callback = callback;
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke());
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke());
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateArgPromiseT<TArg, TPromise> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly Func<TArg, Promise<TPromise>> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateArgPromiseT(Func<TArg, Promise<TPromise>> callback)
                {
                    _callback = callback;
                }

                private void Invoke(TArg arg, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(arg));
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                }

                public void MaybeUnregisterCancelation() { }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueVoidVoid : IDelegateContinue
            {
                private readonly Promise.ContinueAction _callback;

                public DelegateContinueVoidVoid(Promise.ContinueAction callback)
                {
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueVoidResult<TResult> : IDelegateContinue
            {
                private readonly Promise.ContinueFunc<TResult> _callback;

                public DelegateContinueVoidResult(Promise.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueArgVoid<TArg> : IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueAction _callback;

                public DelegateContinueArgVoid(Promise<TArg>.ContinueAction callback)
                {
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueArgResult<TArg, TResult> : IDelegateContinue
            {
                private readonly Promise<TArg>.ContinueFunc<TResult> _callback;

                public DelegateContinueArgResult(Promise<TArg>.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueVoidPromise : IDelegateContinuePromise
            {
                private readonly Promise.ContinueFunc<Promise> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueVoidPromise(Promise.ContinueFunc<Promise> callback)
                {
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    Promise result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueVoidPromiseT<TPromise> : IDelegateContinuePromise
            {
                private readonly Promise.ContinueFunc<Promise<TPromise>> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueVoidPromiseT(Promise.ContinueFunc<Promise<TPromise>> callback)
                {
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    Promise<TPromise> result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueArgPromise<TArg> : IDelegateContinuePromise
            {
                private readonly Promise<TArg>.ContinueFunc<Promise> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueArgPromise(Promise<TArg>.ContinueFunc<Promise> callback)
                {
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    Promise result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueArgPromiseT<TArg, TPromise> : IDelegateContinuePromise
            {
                private readonly Promise<TArg>.ContinueFunc<Promise<TPromise>> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueArgPromiseT(Promise<TArg>.ContinueFunc<Promise<TPromise>> callback)
                {
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    Promise<TPromise> result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }
            #endregion

            #region Delegates with capture value
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            public sealed class FinallyDelegateCapture<TCapture> : ITreeHandleable, ITraceable
            {
                private struct Creator : ICreator<FinallyDelegateCapture<TCapture>>
                {
                    [MethodImpl((MethodImplOptions) 256)]
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

                public static FinallyDelegateCapture<TCapture> GetOrCreate(ref TCapture capturedValue, Action<TCapture> onFinally)
                {
                    var del = ObjectPool<ITreeHandleable>.GetOrCreate<FinallyDelegateCapture<TCapture>, Creator>(new Creator());
                    del._capturedValue = capturedValue;
                    del._onFinally = onFinally;
                    SetCreatedStacktrace(del, 2);
                    return del;
                }

                private void InvokeAndCatchAndDispose()
                {
                    var value = _capturedValue;
                    var callback = _onFinally;
                    SetCurrentInvoker(this);
                    Dispose();
                    try
                    {
                        callback.Invoke(value);
                    }
                    catch (Exception e)
                    {
                        AddRejectionToUnhandledStack(e, this);
                    }
                    ClearCurrentInvoker();
                }

                void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _onFinally = null;
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                void ITreeHandleable.Handle()
                {
                    ThrowIfInPool(this);
                    InvokeAndCatchAndDispose();
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
            private struct DelegateCaptureVoidVoid<TCapture> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Action<TCapture> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureVoidVoid(ref TCapture capturedValue, Action<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue);
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue);
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureArgVoid<TCapture, TArg> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Action<TCapture, TArg> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureArgVoid(ref TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                private void Invoke(TArg arg, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue, arg);
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureVoidResult<TCapture, TResult> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TResult> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureVoidResult(ref TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue);
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue);
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureArgResult<TCapture, TArg, TResult> : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TArg, TResult> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureArgResult(ref TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                private void Invoke(TArg arg, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue, arg);
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                }

                public void MaybeUnregisterCancelation() { }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureVoidPromise<TCapture> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, Promise> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureVoidPromise(ref TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(_capturedValue));
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(_capturedValue));
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureArgPromise<TCapture, TArg> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TArg, Promise> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureArgPromise(ref TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                private void Invoke(TArg arg, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(_capturedValue, arg));
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureVoidPromiseT<TCapture, TPromise> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                public readonly Func<TCapture, Promise<TPromise>> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureVoidPromiseT(ref TCapture capturedValue, Func<TCapture, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(_capturedValue));
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(_capturedValue));
                }

                public void MaybeUnregisterCancelation() { }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> : IDelegateResolvePromise, IDelegateRejectPromise
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TArg, Promise<TPromise>> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureArgPromiseT(ref TCapture capturedValue, Func<TCapture, TArg, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                private void Invoke(TArg arg, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(_capturedValue, arg));
                }

                void IDelegateResolvePromise.InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    Invoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                public void InvokeRejecter(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        Invoke(arg, owner);
                    }
                    else
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                }

                public void MaybeUnregisterCancelation() { }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureVoidVoid<TCapture> : IDelegateContinue
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueAction<TCapture> _callback;

                public DelegateContinueCaptureVoidVoid(ref TCapture capturedValue, Promise.ContinueAction<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureVoidResult<TCapture, TResult> : IDelegateContinue
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueFunc<TCapture, TResult> _callback;

                public DelegateContinueCaptureVoidResult(ref TCapture capturedValue, Promise.ContinueFunc<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureArgVoid<TCapture, TArg> : IDelegateContinue
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueAction<TCapture> _callback;

                public DelegateContinueCaptureArgVoid(ref TCapture capturedValue, Promise<TArg>.ContinueAction<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureArgResult<TCapture, TArg, TResult> : IDelegateContinue
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueFunc<TCapture, TResult> _callback;

                public DelegateContinueCaptureArgResult(ref TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureVoidPromise<TCapture> : IDelegateContinuePromise
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueFunc<TCapture, Promise> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureVoidPromise(ref TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    Promise result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureVoidPromiseT<TCapture, TPromise> : IDelegateContinuePromise
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueFunc<TCapture, Promise<TPromise>> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureVoidPromiseT(ref TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    Promise<TPromise> result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureArgPromise<TCapture, TArg> : IDelegateContinuePromise
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureArgPromise(ref TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    Promise result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureArgPromiseT<TCapture, TArg, TPromise> : IDelegateContinuePromise
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise<TPromise>> _callback;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureArgPromiseT(ref TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    Promise<TPromise> result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result);
                }

                public void CancelCallback() { throw new System.InvalidOperationException(); }
            }
            #endregion

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateResolvePassthroughCancel : IDelegateResolve, IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly bool _isActive;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return !_isActive; } }

                public DelegateResolvePassthroughCancel(bool isActive)
                {
                    _isActive = isActive;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    owner.ResolveInternal(valueContainer);
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

            #region Delegates with cancelation token
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateVoidVoidCancel : IDelegateResolve, IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly Action _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateVoidVoidCancel(Action callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateArgVoidCancel<TArg> : IDelegateResolve, IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly Action<TArg> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateArgVoidCancel(Action<TArg> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(((ResolveContainer<TArg>) valueContainer).value);
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateVoidResultCancel<TResult> : IDelegateResolve, IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly Func<TResult> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateVoidResultCancel(Func<TResult> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateArgResultCancel<TArg, TResult> : IDelegateResolve, IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly Func<TArg, TResult> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateArgResultCancel(Func<TArg, TResult> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(((ResolveContainer<TArg>) valueContainer).value);
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateVoidPromiseCancel : IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly Func<Promise> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateVoidPromiseCancel(Func<Promise> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke());
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateArgPromiseCancel<TArg> : IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly Func<TArg, Promise> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateArgPromiseCancel(Func<TArg, Promise> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(arg));
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateVoidPromiseTCancel<TPromise> : IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly Func<Promise<TPromise>> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateVoidPromiseTCancel(Func<Promise<TPromise>> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke());
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateArgPromiseTCancel<TArg, TPromise> : IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly Func<TArg, Promise<TPromise>> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateArgPromiseTCancel(Func<TArg, Promise<TPromise>> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(arg));
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueVoidVoidCancel : IDelegateContinue, ICancelableDelegate
            {
                private readonly Promise.ContinueAction _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueVoidVoidCancel(Promise.ContinueAction callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        _callback.Invoke(new Promise.ResultContainer(valueContainer));
                        owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueVoidResultCancel<TResult> : IDelegateContinue, ICancelableDelegate
            {
                private readonly Promise.ContinueFunc<TResult> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueVoidResultCancel(Promise.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        TResult result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                        owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueArgVoidCancel<TArg> : IDelegateContinue, ICancelableDelegate
            {
                private readonly Promise<TArg>.ContinueAction _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueArgVoidCancel(Promise<TArg>.ContinueAction callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                        owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueArgResultCancel<TArg, TResult> : IDelegateContinue, ICancelableDelegate
            {
                private readonly Promise<TArg>.ContinueFunc<TResult> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueArgResultCancel(Promise<TArg>.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        TResult result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                        owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueVoidPromiseCancel : IDelegateContinuePromise, ICancelableDelegate
            {
                private readonly Promise.ContinueFunc<Promise> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueVoidPromiseCancel(Promise.ContinueFunc<Promise> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        Promise result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                        ((PromiseWaitPromise) owner).WaitFor(result);
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueVoidPromiseTCancel<TPromise> : IDelegateContinuePromise, ICancelableDelegate
            {
                private readonly Promise.ContinueFunc<Promise<TPromise>> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueVoidPromiseTCancel(Promise.ContinueFunc<Promise<TPromise>> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        Promise<TPromise> result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                        ((PromiseWaitPromise) owner).WaitFor(result);
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueArgPromiseCancel<TArg> : IDelegateContinuePromise, ICancelableDelegate
            {
                private readonly Promise<TArg>.ContinueFunc<Promise> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueArgPromiseCancel(Promise<TArg>.ContinueFunc<Promise> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        Promise result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                        ((PromiseWaitPromise) owner).WaitFor(result);
                    }
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueArgPromiseTCancel<TArg, TPromise> : IDelegateContinuePromise, ICancelableDelegate
            {
                private readonly Promise<TArg>.ContinueFunc<Promise<TPromise>> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueArgPromiseTCancel(Promise<TArg>.ContinueFunc<Promise<TPromise>> callback)
                {
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        Promise<TPromise> result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                        ((PromiseWaitPromise) owner).WaitFor(result);
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }
            #endregion

            #region Delegates with capture value and cancelation token
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureVoidVoidCancel<TCapture> : IDelegateResolve, IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Action<TCapture> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureVoidVoidCancel(ref TCapture capturedValue, Action<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue);
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureArgVoidCancel<TCapture, TArg> : IDelegateResolve, IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Action<TCapture, TArg> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureArgVoidCancel(ref TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    _callback.Invoke(_capturedValue, ((ResolveContainer<TArg>) valueContainer).value);
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureVoidResultCancel<TCapture, TResult> : IDelegateResolve, IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TResult> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureVoidResultCancel(ref TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue);
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureArgResultCancel<TCapture, TArg, TResult> : IDelegateResolve, IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TArg, TResult> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureArgResultCancel(ref TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TResult result = _callback.Invoke(_capturedValue, ((ResolveContainer<TArg>) valueContainer).value);
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureVoidPromiseCancel<TCapture> : IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, Promise> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureVoidPromiseCancel(ref TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(_capturedValue));
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureArgPromiseCancel<TCapture, TArg> : IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TArg, Promise> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureArgPromiseCancel(ref TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(_capturedValue, arg));
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureVoidPromiseTCancel<TCapture, TPromise> : IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, Promise<TPromise>> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureVoidPromiseTCancel(ref TCapture capturedValue, Func<TCapture, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(_capturedValue));
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateCaptureArgPromiseTCancel<TCapture, TArg, TPromise> : IDelegateResolvePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Func<TCapture, TArg, Promise<TPromise>> _callback;
                private CancelationRegistration _cancelationRegistration;

                public bool IsNull { get { return _callback == null; } }

                public DelegateCaptureArgPromiseTCancel(ref TCapture capturedValue, Func<TCapture, TArg, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                }

                public void InvokeResolver(IValueContainer valueContainer, PromiseRef owner)
                {
                    TArg arg = ((ResolveContainer<TArg>) valueContainer).value;
                    ((PromiseWaitPromise) owner).WaitFor(_callback.Invoke(_capturedValue, arg));
                }

                public void MaybeUnregisterCancelation()
                {
                    _cancelationRegistration.TryUnregister();
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureVoidVoidCancel<TCapture> : IDelegateContinue, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueAction<TCapture> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureVoidVoidCancel(ref TCapture capturedValue, Promise.ContinueAction<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                        owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureVoidResultCancel<TCapture, TResult> : IDelegateContinue, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueFunc<TCapture, TResult> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureVoidResultCancel(ref TCapture capturedValue, Promise.ContinueFunc<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        TResult result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                        owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureArgVoidCancel<TCapture, TArg> : IDelegateContinue, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueAction<TCapture> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureArgVoidCancel(ref TCapture capturedValue, Promise<TArg>.ContinueAction<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                        owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureArgResultCancel<TCapture, TArg, TResult> : IDelegateContinue, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueFunc<TCapture, TResult> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureArgResultCancel(ref TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        TResult result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                        owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureVoidPromiseCancel<TCapture> : IDelegateContinuePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueFunc<TCapture, Promise> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureVoidPromiseCancel(ref TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        Promise result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                        ((PromiseWaitPromise) owner).WaitFor(result);
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureVoidPromiseTCancel<TCapture, TPromise> : IDelegateContinuePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.ContinueFunc<TCapture, Promise<TPromise>> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureVoidPromiseTCancel(ref TCapture capturedValue, Promise.ContinueFunc<TCapture, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        Promise<TPromise> result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                        ((PromiseWaitPromise) owner).WaitFor(result);
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureArgPromiseCancel<TCapture, TArg> : IDelegateContinuePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureArgPromiseCancel(ref TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        Promise result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                        ((PromiseWaitPromise) owner).WaitFor(result);
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private struct DelegateContinueCaptureArgPromiseTCancel<TCapture, TArg, TPromise> : IDelegateContinuePromise, ICancelableDelegate
            {
                private readonly TCapture _capturedValue;
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise<TPromise>> _callback;
                private CancelationRegistration _cancelationRegistration;
                private bool _canceled;

                public bool IsNull { get { return _callback == null; } }

                public DelegateContinueCaptureArgPromiseTCancel(ref TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, Promise<TPromise>> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                    _cancelationRegistration = default(CancelationRegistration);
                    _canceled = false;
                }

                public void Invoke(IValueContainer valueContainer, PromiseRef owner)
                {
                    if (_canceled)
                    {
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                    else
                    {
                        _cancelationRegistration.TryUnregister();
                        Promise<TPromise> result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                        ((PromiseWaitPromise) owner).WaitFor(result);
                    }
                }

                public void CancelCallback()
                {
                    _canceled = true;
                }

                public void SetCancelationRegistration(CancelationRegistration cancelationRegistration)
                {
                    _cancelationRegistration = cancelationRegistration;
                }
            }
            #endregion
        }
    }
}