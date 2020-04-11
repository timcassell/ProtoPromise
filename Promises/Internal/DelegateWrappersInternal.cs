#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0001 // Class is declared partial but has only one part

using System;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        partial class Internal
        {
            [System.Diagnostics.DebuggerStepThrough]
            public sealed partial class FinallyDelegate : ITreeHandleable
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private static ValueLinkedStack<ITreeHandleable> _pool;

                private Action _onFinally;

                private FinallyDelegate() { }

                static FinallyDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static FinallyDelegate GetOrCreate(Action onFinally, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (FinallyDelegate) _pool.Pop() : new FinallyDelegate();
                    del._onFinally = onFinally;
                    SetCreatedStacktrace(del, skipFrames + 1);
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
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void ITreeHandleable.Handle()
                {
                    InvokeAndCatchAndDispose();
                }

                void ITreeHandleable.Cancel()
                {
                    InvokeAndCatchAndDispose();
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue, ref ValueLinkedQueue<ITreeHandleable> cancelQueue)
                {
                    handleQueue.Push(this);
                }

                void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
                {
                    AddToHandleQueueBack(this);
                }
            }


            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegatePassthrough : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private static readonly DelegatePassthrough _instance = new DelegatePassthrough();

                private DelegatePassthrough() { }

                public static DelegatePassthrough GetOrCreate()
                {
                    return _instance;
                }

                void IDelegateResolve.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    owner.ResolveInternal();
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    owner.RejectInternal();
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    owner.ResolveInternal();
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    owner.RejectInternal();
                }

                void IDisposable.Dispose() { }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateVoidVoid : PoolableObject<DelegateVoidVoid>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Action _callback;

                private DelegateVoidVoid() { }

                public static DelegateVoidVoid GetOrCreate(Action callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidVoid();
                    del._callback = callback;
                    return del;
                }

                private void DisposeAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    Dispose();
                    temp.Invoke();
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateArgVoid<TArg> : PoolableObject<DelegateArgVoid<TArg>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Action<TArg> _callback;

                public static DelegateArgVoid<TArg> GetOrCreate(Action<TArg> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgVoid<TArg>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgVoid() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    Dispose();
                    temp.Invoke(arg);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                private void TryDisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        DisposeAndInvoke(arg, owner);
                    }
                    else
                    {
                        Dispose();
                        owner.RejectInternal();
                    }
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TryDisposeAndInvoke(valueContainer, owner);
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TryDisposeAndInvoke(valueContainer, owner);
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateVoidResult<TResult> : PoolableObject<DelegateVoidResult<TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<TResult> _callback;

                public static DelegateVoidResult<TResult> GetOrCreate(Func<TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidResult<TResult>();
                    del._callback = callback;
                    return del;
                }

                private DelegateVoidResult() { }

                private void DisposeAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    Dispose();
                    TResult result = temp.Invoke();
                    owner.ResolveInternalIfNotCanceled(result);
                }

                void IDelegateResolve.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateArgResult<TArg, TResult> : PoolableObject<DelegateArgResult<TArg, TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<TArg, TResult> _callback;

                public static DelegateArgResult<TArg, TResult> GetOrCreate(Func<TArg, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgResult<TArg, TResult>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgResult() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    Dispose();
                    TResult result = temp.Invoke(arg);
                    owner.ResolveInternalIfNotCanceled(result);
                }

                void IDelegateResolve.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                private void TryDisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        DisposeAndInvoke(arg, owner);
                    }
                    else
                    {
                        Dispose();
                        owner.RejectInternal();
                    }
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TryDisposeAndInvoke(valueContainer, owner);
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TryDisposeAndInvoke(valueContainer, owner);
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateVoidPromise : PoolableObject<DelegateVoidPromise>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<Promise> _callback;

                private DelegateVoidPromise() { }

                public static DelegateVoidPromise GetOrCreate(Func<Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidPromise();
                    del._callback = callback;
                    return del;
                }

                private void DisposeAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    Dispose();
                    ((PromiseResolveRejectPromise0) owner).WaitFor(temp.Invoke());
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateArgPromise<TArg> : PoolableObject<DelegateArgPromise<TArg>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<TArg, Promise> _callback;

                public static DelegateArgPromise<TArg> GetOrCreate(Func<TArg, Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgPromise<TArg>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgPromise() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    Dispose();
                    ((PromiseResolveRejectPromise0) owner).WaitFor(temp.Invoke(arg));
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        DisposeAndInvoke(arg, owner);
                    }
                    else
                    {
                        Dispose();
                        owner.RejectInternal();
                    }
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateVoidPromiseT<TPromise> : PoolableObject<DelegateVoidPromiseT<TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<Promise<TPromise>> _callback;

                public static DelegateVoidPromiseT<TPromise> GetOrCreate(Func<Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidPromiseT<TPromise>();
                    del._callback = callback;
                    return del;
                }

                private DelegateVoidPromiseT() { }

                private void DisposeAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    Dispose();
                    ((PromiseResolveRejectPromise<TPromise>) owner).WaitFor(temp.Invoke());
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateArgPromiseT<TArg, TPromise> : PoolableObject<DelegateArgPromiseT<TArg, TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<TArg, Promise<TPromise>> _callback;

                public static DelegateArgPromiseT<TArg, TPromise> GetOrCreate(Func<TArg, Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgPromiseT<TArg, TPromise>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgPromiseT() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    Dispose();
                    ((PromiseResolveRejectPromise<TPromise>) owner).WaitFor(temp.Invoke(arg));
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        DisposeAndInvoke(arg, owner);
                    }
                    else
                    {
                        Dispose();
                        owner.RejectInternal();
                    }
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateContinueVoidVoid : PoolableObject<DelegateContinueVoidVoid>, IDelegateContinue
            {
                private Action<ResultContainer> _callback;

                public static DelegateContinueVoidVoid GetOrCreate(Action<ResultContainer> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueVoidVoid();
                    del._callback = callback;
                    return del;
                }
                
                private DelegateContinueVoidVoid() { }

                void IDelegateContinue.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    Dispose();
                    callback.Invoke(new ResultContainer(valueContainer));
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateContinueVoidResult<TResult> : PoolableObject<DelegateContinueVoidResult<TResult>>, IDelegateContinue<TResult>
            {
                private Func<ResultContainer, TResult> _callback;

                public static DelegateContinueVoidResult<TResult> GetOrCreate(Func<ResultContainer, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueVoidResult<TResult>();
                    del._callback = callback;
                    return del;
                }

                private DelegateContinueVoidResult() { }

                TResult IDelegateContinue<TResult>.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    Dispose();
                    return callback.Invoke(new ResultContainer(valueContainer));
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateContinueArgVoid<TArg> : PoolableObject<DelegateContinueArgVoid<TArg>>, IDelegateContinue
            {
                private Action<Promise<TArg>.ResultContainer> _callback;

                public static DelegateContinueArgVoid<TArg> GetOrCreate(Action<Promise<TArg>.ResultContainer> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueArgVoid<TArg>();
                    del._callback = callback;
                    return del;
                }

                private DelegateContinueArgVoid() { }

                void IDelegateContinue.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    Dispose();
                    callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateContinueArgResult<TArg, TResult> : PoolableObject<DelegateContinueArgResult<TArg, TResult>>, IDelegateContinue<TResult>
            {
                private Func<Promise<TArg>.ResultContainer, TResult> _callback;

                public static DelegateContinueArgResult<TArg, TResult> GetOrCreate(Func<Promise<TArg>.ResultContainer, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueArgResult<TArg, TResult>();
                    del._callback = callback;
                    return del;
                }

                private DelegateContinueArgResult() { }

                TResult IDelegateContinue<TResult>.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    Dispose();
                    return callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer));
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


            [System.Diagnostics.DebuggerStepThrough]
            public sealed partial class FinallyDelegateCapture<TCapture> : ITreeHandleable
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private static ValueLinkedStack<ITreeHandleable> _pool;

                private TCapture _capturedValue;
                private Action<TCapture> _onFinally;

                private FinallyDelegateCapture() { }

                static FinallyDelegateCapture()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static FinallyDelegateCapture<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture> onFinally, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (FinallyDelegateCapture<TCapture>) _pool.Pop() : new FinallyDelegateCapture<TCapture>();
                    del._capturedValue = capturedValue;
                    del._onFinally = onFinally;
                    SetCreatedStacktrace(del, skipFrames + 1);
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
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void ITreeHandleable.Handle()
                {
                    InvokeAndCatchAndDispose();
                }

                void ITreeHandleable.Cancel()
                {
                    InvokeAndCatchAndDispose();
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue, ref ValueLinkedQueue<ITreeHandleable> cancelQueue)
                {
                    handleQueue.Push(this);
                }

                void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
                {
                    AddToHandleQueueBack(this);
                }
            }


            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateCaptureVoidVoid<TCapture> : PoolableObject<DelegateCaptureVoidVoid<TCapture>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Action<TCapture> _callback;

                public static DelegateCaptureVoidVoid<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidVoid<TCapture>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureVoidVoid() { }

                private void DisposeAndInvoke(Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Dispose();
                    temp.Invoke(value);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                public void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateCaptureArgVoid<TCapture, TArg> : PoolableObject<DelegateCaptureArgVoid<TCapture, TArg>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Action<TCapture, TArg> _callback;

                public static DelegateCaptureArgVoid<TCapture, TArg> GetOrCreate(TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureArgVoid<TCapture, TArg>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureArgVoid() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Dispose();
                    temp.Invoke(value, arg);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                private void TryReleaseAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        DisposeAndInvoke(arg, owner);
                    }
                    else
                    {
                        Dispose();
                        owner.RejectInternal();
                    }
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TryReleaseAndInvoke(valueContainer, owner);
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TryReleaseAndInvoke(valueContainer, owner);
                }

                public void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateCaptureVoidResult<TCapture, TResult> : PoolableObject<DelegateCaptureVoidResult<TCapture, TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, TResult> _callback;

                public static DelegateCaptureVoidResult<TCapture, TResult> GetOrCreate(TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidResult<TCapture, TResult>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureVoidResult() { }

                private void DisposeAndInvoke(Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Dispose();
                    TResult result = temp.Invoke(value);
                    owner.ResolveInternalIfNotCanceled(result);
                }

                void IDelegateResolve.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                public void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateCaptureArgResult<TCapture, TArg, TResult> : PoolableObject<DelegateCaptureArgResult<TCapture, TArg, TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, TArg, TResult> _callback;

                public static DelegateCaptureArgResult<TCapture, TArg, TResult> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureArgResult<TCapture, TArg, TResult>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureArgResult() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Dispose();
                    TResult result = temp.Invoke(value, arg);
                    owner.ResolveInternalIfNotCanceled(result);
                }

                void IDelegateResolve.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                private void TryDisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        DisposeAndInvoke(arg, owner);
                    }
                    else
                    {
                        Dispose();
                        owner.RejectInternal();
                    }
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TryDisposeAndInvoke(valueContainer, owner);
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TryDisposeAndInvoke(valueContainer, owner);
                }

                public void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateCaptureVoidPromise<TCapture> : PoolableObject<DelegateCaptureVoidPromise<TCapture>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, Promise> _callback;

                private DelegateCaptureVoidPromise() { }

                public static DelegateCaptureVoidPromise<TCapture> GetOrCreate(TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidPromise<TCapture>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private void DisposeAndInvoke(Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Dispose();
                    ((PromiseResolveRejectPromise0) owner).WaitFor(temp.Invoke(value));
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                public void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateCaptureArgPromise<TCapture, TArg> : PoolableObject<DelegateCaptureArgPromise<TCapture, TArg>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, TArg, Promise> _callback;

                public static DelegateCaptureArgPromise<TCapture, TArg> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureArgPromise<TCapture, TArg>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureArgPromise() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Dispose();
                    ((PromiseResolveRejectPromise0) owner).WaitFor(temp.Invoke(value, arg));
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        DisposeAndInvoke(arg, owner);
                    }
                    else
                    {
                        Dispose();
                        owner.RejectInternal();
                    }
                }

                public void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateCaptureVoidPromiseT<TCapture, TPromise> : PoolableObject<DelegateCaptureVoidPromiseT<TCapture, TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, Promise<TPromise>> _callback;

                public static DelegateCaptureVoidPromiseT<TCapture, TPromise> GetOrCreate(TCapture capturedValue, Func<TCapture, Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidPromiseT<TCapture, TPromise>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureVoidPromiseT() { }

                private void DisposeAndInvoke(Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Dispose();
                    ((PromiseResolveRejectPromise<TPromise>) owner).WaitFor(temp.Invoke(value));
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(owner);
                }

                public void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> : PoolableObject<DelegateCaptureArgPromiseT<TCapture, TArg, TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, TArg, Promise<TPromise>> _callback;

                public static DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureArgPromiseT<TCapture, TArg, TPromise>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureArgPromiseT() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Dispose();
                    ((PromiseResolveRejectPromise<TPromise>) owner).WaitFor(temp.Invoke(value, arg));
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndInvoke(((ResolveContainer<TArg>) valueContainer).value, owner);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    TArg arg;
                    if (TryConvert(valueContainer, out arg))
                    {
                        DisposeAndInvoke(arg, owner);
                    }
                    else
                    {
                        Dispose();
                        owner.RejectInternal();
                    }
                }

                public void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateContinueCaptureVoidVoid<TCapture> : PoolableObject<DelegateContinueCaptureVoidVoid<TCapture>>, IDelegateContinue
            {
                private TCapture _capturedValue;
                private Action<TCapture, ResultContainer> _callback;

                public static DelegateContinueCaptureVoidVoid<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture, ResultContainer> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueCaptureVoidVoid<TCapture>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateContinueCaptureVoidVoid() { }

                void IDelegateContinue.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    var value = _capturedValue;
                    Dispose();
                    callback.Invoke(value, new ResultContainer(valueContainer));
                }

                public void Dispose()
                {
                    _callback = null;
                    _capturedValue = default(TCapture);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateContinueCaptureVoidResult<TCapture, TResult> : PoolableObject<DelegateContinueCaptureVoidResult<TCapture, TResult>>, IDelegateContinue<TResult>
            {
                private TCapture _capturedValue;
                private Func<TCapture, ResultContainer, TResult> _callback;

                public static DelegateContinueCaptureVoidResult<TCapture, TResult> GetOrCreate(TCapture capturedValue, Func<TCapture, ResultContainer, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueCaptureVoidResult<TCapture, TResult>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateContinueCaptureVoidResult() { }

                TResult IDelegateContinue<TResult>.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    var value = _capturedValue;
                    Dispose();
                    return callback.Invoke(value, new ResultContainer(valueContainer));
                }

                public void Dispose()
                {
                    _callback = null;
                    _capturedValue = default(TCapture);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateContinueCaptureArgVoid<TCapture, TArg> : PoolableObject<DelegateContinueCaptureArgVoid<TCapture, TArg>>, IDelegateContinue
            {
                private TCapture _capturedValue;
                private Action<TCapture, Promise<TArg>.ResultContainer> _callback;

                public static DelegateContinueCaptureArgVoid<TCapture, TArg> GetOrCreate(TCapture capturedValue, Action<TCapture, Promise<TArg>.ResultContainer> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueCaptureArgVoid<TCapture, TArg>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateContinueCaptureArgVoid() { }

                void IDelegateContinue.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    var value = _capturedValue;
                    Dispose();
                    callback.Invoke(value, new Promise<TArg>.ResultContainer(valueContainer));
                }

                public void Dispose()
                {
                    _callback = null;
                    _capturedValue = default(TCapture);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerStepThrough]
            public sealed class DelegateContinueCaptureArgResult<TCapture, TArg, TResult> : PoolableObject<DelegateContinueCaptureArgResult<TCapture, TArg, TResult>>, IDelegateContinue<TResult>
            {
                private TCapture _capturedValue;
                private Func<TCapture, Promise<TArg>.ResultContainer, TResult> _callback;

                public static DelegateContinueCaptureArgResult<TCapture, TArg, TResult> GetOrCreate(TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueCaptureArgResult<TCapture, TArg, TResult>();
                    del._callback = callback;
                    return del;
                }

                private DelegateContinueCaptureArgResult() { }

                TResult IDelegateContinue<TResult>.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    var value = _capturedValue;
                    Dispose();
                    return callback.Invoke(value, new Promise<TArg>.ResultContainer(valueContainer));
                }

                public void Dispose()
                {
                    _callback = null;
                    _capturedValue = default(TCapture);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }
        }
    }
}