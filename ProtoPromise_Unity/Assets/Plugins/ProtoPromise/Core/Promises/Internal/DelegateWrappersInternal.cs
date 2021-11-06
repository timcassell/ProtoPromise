#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration

using System;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal static class DelegateWrapper
            {
                // These static functions help with the implementation so we don't need to type the generics in every method.

                [MethodImpl(InlineOption)]
                public static DelegateResolvePassthrough<T> CreatePassthrough<T>()
                {
                    return new DelegateResolvePassthrough<T>(true);
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
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateResolvePassthrough<T> : IDelegate<T, T>, IDelegate<T, Promise<T>>
            {
                private readonly bool _isActive;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return !_isActive; }
                }

                [MethodImpl(InlineOption)]
                internal DelegateResolvePassthrough(bool isActive)
                {
                    _isActive = isActive;
                }

                [MethodImpl(InlineOption)]
                public T Invoke(T arg)
                {
                    return arg;
                }

                [MethodImpl(InlineOption)]
                Promise<T> IDelegate<T, Promise<T>>.Invoke(T arg)
                {
                    return CreateResolved(arg);
                }
            }

            #region Regular Delegates
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateVoidVoid : IDelegate<VoidResult, VoidResult>, IDelegate<VoidResult, Promise<VoidResult>>
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

                [MethodImpl(InlineOption)]
                public VoidResult Invoke(VoidResult arg)
                {
                    _callback.Invoke();
                    return new VoidResult();
                }

                [MethodImpl(InlineOption)]
                Promise<VoidResult> IDelegate<VoidResult, Promise<VoidResult>>.Invoke(VoidResult arg)
                {
                    _callback.Invoke();
                    return CreateResolved(new VoidResult());
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateArgVoid<TArg> : IDelegate<TArg, VoidResult>, IDelegate<TArg, Promise<VoidResult>>
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

                [MethodImpl(InlineOption)]
                public VoidResult Invoke(TArg arg)
                {
                    _callback.Invoke(arg);
                    return new VoidResult();
                }

                Promise<VoidResult> IDelegate<TArg, Promise<VoidResult>>.Invoke(TArg arg)
                {
                    _callback.Invoke(arg);
                    return CreateResolved(new VoidResult());
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateVoidResult<TResult> : IDelegate<VoidResult, TResult>, IDelegate<VoidResult, Promise<TResult>>
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
                public TResult Invoke(VoidResult arg)
                {
                    return _callback.Invoke();
                }

                Promise<TResult> IDelegate<VoidResult, Promise<TResult>>.Invoke(VoidResult arg)
                {
                    return CreateResolved(_callback.Invoke());
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateArgResult<TArg, TResult> : IDelegate<TArg, TResult>, IDelegate<TArg, Promise<TResult>>
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
                public TResult Invoke(TArg arg)
                {
                    return _callback.Invoke(arg);
                }

                Promise<TResult> IDelegate<TArg, Promise<TResult>>.Invoke(TArg arg)
                {
                    return CreateResolved(_callback.Invoke(arg));
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateVoidPromise : IDelegate<VoidResult, Promise<VoidResult>>
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
                public Promise<VoidResult> Invoke(VoidResult arg)
                {
                    return _callback.Invoke()._target;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateArgPromise<TArg> : IDelegate<TArg, Promise<VoidResult>>
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
                public Promise<VoidResult> Invoke(TArg arg)
                {
                    return _callback.Invoke(arg)._target;
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueVoidVoid : IDelegateContinue, IDelegate<VoidResult, VoidResult>
            {
                private readonly Promise.ContinueAction _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueVoidVoid(Promise.ContinueAction callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public VoidResult Invoke(VoidResult arg)
                {
                    _callback.Invoke(new Promise.ResultContainer(null));
                    return new VoidResult();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate(), ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueVoidResult<TResult> : IDelegateContinue, IDelegate<VoidResult, TResult>
            {
                private readonly Promise.ContinueFunc<TResult> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueVoidResult(Promise.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(VoidResult arg)
                {
                    return _callback.Invoke(new Promise.ResultContainer(null));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    TResult result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result, 0), ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueArgVoid<TArg> : IDelegateContinue, IDelegate<TArg, VoidResult>
            {
                private readonly Promise<TArg>.ContinueAction _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueArgVoid(Promise<TArg>.ContinueAction callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public VoidResult Invoke(TArg arg)
                {
                    _callback.Invoke(new Promise<TArg>.ResultContainer(arg));
                    return new VoidResult();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate(), ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueArgResult<TArg, TResult> : IDelegateContinue, IDelegate<TArg, TResult>
            {
                private readonly Promise<TArg>.ContinueFunc<TResult> _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueArgResult(Promise<TArg>.ContinueFunc<TResult> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg arg)
                {
                    return _callback.Invoke(new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    TResult result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result, 0), ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueVoidPromise : IDelegateContinuePromise, IDelegate<VoidResult, Promise<VoidResult>>
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
                public Promise<VoidResult> Invoke(VoidResult arg)
                {
                    return _callback.Invoke(new Promise.ResultContainer(null))._target;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    var result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result._target, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueVoidPromiseT<TPromise> : IDelegateContinuePromise, IDelegate<VoidResult, Promise<TPromise>>
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
                public Promise<TPromise> Invoke(VoidResult arg)
                {
                    return _callback.Invoke(new Promise.ResultContainer(null));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    var result = _callback.Invoke(new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueArgPromise<TArg> : IDelegateContinuePromise, IDelegate<TArg, Promise<VoidResult>>
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
                public Promise<VoidResult> Invoke(TArg arg)
                {
                    return _callback.Invoke(new Promise<TArg>.ResultContainer(arg))._target;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    var result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result._target, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueArgPromiseT<TArg, TPromise> : IDelegateContinuePromise, IDelegate<TArg, Promise<TPromise>>
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
                public Promise<TPromise> Invoke(TArg arg)
                {
                    return _callback.Invoke(new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    var result = _callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateFinally<T> : IDelegateSimple, IDelegate<T, T>
            {
                private readonly Action _callback;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateFinally(Action callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public T Invoke(T arg)
                {
                    _callback.Invoke();
                    return arg;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer)
                {
                    _callback.Invoke();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCancel : IDelegateSimple
            {
                private readonly Promise.CanceledAction _callback;

                [MethodImpl(InlineOption)]
                public DelegateCancel(Promise.CanceledAction callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer)
                {
                    _callback.Invoke(new ReasonContainer(valueContainer, InvokeId));
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateProgress : IProgress<float>
            {
                private readonly Action<float> _callback;

                [MethodImpl(InlineOption)]
                public DelegateProgress(Action<float> callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Report(float value)
                {
                    _callback.Invoke(value);
                }
            }
            #endregion

            #region Delegates with capture value
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureVoidVoid<TCapture> : IDelegate<VoidResult, VoidResult>, IDelegate<VoidResult, Promise<VoidResult>>
            {
                private readonly Action<TCapture> _callback;
                private readonly TCapture _capturedValue;

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
                public VoidResult Invoke(VoidResult arg)
                {
                    _callback.Invoke(_capturedValue);
                    return new VoidResult();
                }

                Promise<VoidResult> IDelegate<VoidResult, Promise<VoidResult>>.Invoke(VoidResult arg)
                {
                    _callback.Invoke(_capturedValue);
                    return CreateResolved(new VoidResult());
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureArgVoid<TCapture, TArg> : IDelegate<TArg, VoidResult>, IDelegate<TArg, Promise<VoidResult>>
            {
                private readonly Action<TCapture, TArg> _callback;
                private readonly TCapture _capturedValue;

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
                public VoidResult Invoke(TArg arg)
                {
                    _callback.Invoke(_capturedValue, arg);
                    return new VoidResult();
                }

                Promise<VoidResult> IDelegate<TArg, Promise<VoidResult>>.Invoke(TArg arg)
                {
                    _callback.Invoke(_capturedValue, arg);
                    return CreateResolved(new VoidResult());
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureVoidResult<TCapture, TResult> : IDelegate<VoidResult, TResult>, IDelegate<VoidResult, Promise<TResult>>
            {
                private readonly Func<TCapture, TResult> _callback;
                private readonly TCapture _capturedValue;

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
                public TResult Invoke(VoidResult arg)
                {
                    return _callback.Invoke(_capturedValue);
                }

                Promise<TResult> IDelegate<VoidResult, Promise<TResult>>.Invoke(VoidResult arg)
                {
                    return CreateResolved(_callback.Invoke(_capturedValue));
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureArgResult<TCapture, TArg, TResult> : IDelegate<TArg, TResult>, IDelegate<TArg, Promise<TResult>>
            {
                private readonly Func<TCapture, TArg, TResult> _callback;
                private readonly TCapture _capturedValue;

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
                public TResult Invoke(TArg arg)
                {
                    return _callback.Invoke(_capturedValue, arg);
                }

                Promise<TResult> IDelegate<TArg, Promise<TResult>>.Invoke(TArg arg)
                {
                    return CreateResolved(_callback.Invoke(_capturedValue, arg));
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureVoidPromise<TCapture> : IDelegate<VoidResult, Promise<VoidResult>>
            {
                private readonly Func<TCapture, Promise> _callback;
                private readonly TCapture _capturedValue;

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
                public Promise<VoidResult> Invoke(VoidResult arg)
                {
                    return _callback.Invoke(_capturedValue)._target;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureArgPromise<TCapture, TArg> : IDelegate<TArg, Promise<VoidResult>>
            {
                private readonly Func<TCapture, TArg, Promise> _callback;
                private readonly TCapture _capturedValue;

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
                public Promise<VoidResult> Invoke(TArg arg)
                {
                    return _callback.Invoke(_capturedValue, arg)._target;
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureVoidVoid<TCapture> : IDelegateContinue, IDelegate<VoidResult, VoidResult>
            {
                private readonly Promise.ContinueAction<TCapture> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureVoidVoid(ref TCapture capturedValue, Promise.ContinueAction<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public VoidResult Invoke(VoidResult arg)
                {
                    _callback.Invoke(_capturedValue, new Promise.ResultContainer(null));
                    return new VoidResult();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate(), ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureVoidResult<TCapture, TResult> : IDelegateContinue, IDelegate<VoidResult, TResult>
            {
                private readonly Promise.ContinueFunc<TCapture, TResult> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureVoidResult(ref TCapture capturedValue, Promise.ContinueFunc<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(VoidResult arg)
                {
                    return _callback.Invoke(_capturedValue, new Promise.ResultContainer(null));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    TResult result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result, 0), ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureArgVoid<TCapture, TArg> : IDelegateContinue, IDelegate<TArg, VoidResult>
            {
                private readonly Promise<TArg>.ContinueAction<TCapture> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureArgVoid(ref TCapture capturedValue, Promise<TArg>.ContinueAction<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public VoidResult Invoke(TArg arg)
                {
                    _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                    return new VoidResult();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate(), ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureArgResult<TCapture, TArg, TResult> : IDelegateContinue, IDelegate<TArg, TResult>
            {
                private readonly Promise<TArg>.ContinueFunc<TCapture, TResult> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateContinueCaptureArgResult(ref TCapture capturedValue, Promise<TArg>.ContinueFunc<TCapture, TResult> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public TResult Invoke(TArg arg)
                {
                    return _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    TResult result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    valueContainer.Release();
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result, 0), ref executionScheduler);
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureVoidPromise<TCapture> : IDelegateContinuePromise, IDelegate<VoidResult, Promise<VoidResult>>
            {
                private readonly Promise.ContinueFunc<TCapture, Promise> _callback;
                private readonly TCapture _capturedValue;

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
                public Promise<VoidResult> Invoke(VoidResult arg)
                {
                    return _callback.Invoke(_capturedValue, new Promise.ResultContainer(null))._target;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    var result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result._target, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureVoidPromiseT<TCapture, TPromise> : IDelegateContinuePromise, IDelegate<VoidResult, Promise<TPromise>>
            {
                private readonly Promise.ContinueFunc<TCapture, Promise<TPromise>> _callback;
                private readonly TCapture _capturedValue;

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
                public Promise<TPromise> Invoke(VoidResult arg)
                {
                    return _callback.Invoke(_capturedValue, new Promise.ResultContainer(null));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    var result = _callback.Invoke(_capturedValue, new Promise.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureArgPromise<TCapture, TArg> : IDelegateContinuePromise, IDelegate<TArg, Promise<VoidResult>>
            {
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise> _callback;
                private readonly TCapture _capturedValue;

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
                public Promise<VoidResult> Invoke(TArg arg)
                {
                    return _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg))._target;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    var result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result._target, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateContinueCaptureArgPromiseT<TCapture, TArg, TPromise> : IDelegateContinuePromise, IDelegate<TArg, Promise<TPromise>>
            {
                private readonly Promise<TArg>.ContinueFunc<TCapture, Promise<TPromise>> _callback;
                private readonly TCapture _capturedValue;

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
                public Promise<TPromise> Invoke(TArg arg)
                {
                    return _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(arg));
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref ExecutionScheduler executionScheduler)
                {
                    var result = _callback.Invoke(_capturedValue, new Promise<TArg>.ResultContainer(valueContainer));
                    ((PromiseWaitPromise) owner).WaitFor(result, ref executionScheduler);
                    valueContainer.Release();
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer, PromiseSingleAwait owner, ref CancelationHelper cancelationHelper, ref ExecutionScheduler executionScheduler)
                {
                    if (cancelationHelper.TryUnregister(owner))
                    {
                        Invoke(valueContainer, owner, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureFinally<T, TCapture> : IDelegateSimple, IDelegate<T, T>
            {
                private readonly Action<TCapture> _callback;
                private readonly TCapture _capturedValue;

                public bool IsNull
                {
                    [MethodImpl(InlineOption)]
                    get { return _callback == null; }
                }

                [MethodImpl(InlineOption)]
                public DelegateCaptureFinally(ref TCapture capturedValue, Action<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public T Invoke(T arg)
                {
                    _callback.Invoke(_capturedValue);
                    return arg;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer)
                {
                    _callback.Invoke(_capturedValue);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureCancel<TCapture> : IDelegateSimple
            {
                private readonly Promise.CanceledAction<TCapture> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateCaptureCancel(ref TCapture capturedValue, Promise.CanceledAction<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer)
                {
                    _callback.Invoke(_capturedValue, new ReasonContainer(valueContainer, InvokeId));
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct DelegateCaptureProgress<TCapture> : IProgress<float>
            {
                private readonly Action<TCapture, float> _callback;
                private readonly TCapture _capturedValue;

                [MethodImpl(InlineOption)]
                public DelegateCaptureProgress(ref TCapture capturedValue, Action<TCapture, float> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Report(float value)
                {
                    _callback.Invoke(_capturedValue, value);
                }
            }
            #endregion
        }
    }
}