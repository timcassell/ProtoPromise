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
            [System.Diagnostics.DebuggerNonUserCode]
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
                    owner.ResolveInternal(valueContainer);
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    owner.RejectOrCancelInternal(valueContainer);
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    owner.ResolveInternal(valueContainer);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    owner.RejectOrCancelInternal(valueContainer);
                }

                void IDisposable.Dispose() { }
            }


            [System.Diagnostics.DebuggerNonUserCode]
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

                public static FinallyDelegate GetOrCreate(Action onFinally)
                {
                    var del = _pool.IsNotEmpty ? (FinallyDelegate) _pool.Pop() : new FinallyDelegate();
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
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void ITreeHandleable.Handle()
                {
                    InvokeAndCatchAndDispose();
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    handleQueue.Push(this);
                }

                void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
                {
                    AddToHandleQueueBack(this);
                }
            }

            #region Regular Delegates
            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateVoidVoid : ILinked<DelegateVoidVoid>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateVoidVoid ILinked<DelegateVoidVoid>.Next { get; set; }

                private static ValueLinkedStack<DelegateVoidVoid> _pool;

                static DelegateVoidVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

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
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateArgVoid<TArg> : ILinked<DelegateArgVoid<TArg>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateArgVoid<TArg> ILinked<DelegateArgVoid<TArg>>.Next { get; set; }

                private static ValueLinkedStack<DelegateArgVoid<TArg>> _pool;

                static DelegateArgVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

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
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
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
                        owner.RejectOrCancelInternal(valueContainer);
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateVoidResult<TResult> : ILinked<DelegateVoidResult<TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateVoidResult<TResult> ILinked<DelegateVoidResult<TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateVoidResult<TResult>> _pool;

                static DelegateVoidResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

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
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateArgResult<TArg, TResult> : ILinked<DelegateArgResult<TArg, TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateArgResult<TArg, TResult> ILinked<DelegateArgResult<TArg, TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateArgResult<TArg, TResult>> _pool;

                static DelegateArgResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

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
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
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
                        owner.RejectOrCancelInternal(valueContainer);
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


            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateVoidPromise : ILinked<DelegateVoidPromise>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateVoidPromise ILinked<DelegateVoidPromise>.Next { get; set; }

                private static ValueLinkedStack<DelegateVoidPromise> _pool;

                static DelegateVoidPromise()
                {
                    OnClearPool += () => _pool.Clear();
                }

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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateArgPromise<TArg> : ILinked<DelegateArgPromise<TArg>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateArgPromise<TArg> ILinked<DelegateArgPromise<TArg>>.Next { get; set; }

                private static ValueLinkedStack<DelegateArgPromise<TArg>> _pool;

                static DelegateArgPromise()
                {
                    OnClearPool += () => _pool.Clear();
                }

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
                        owner.RejectOrCancelInternal(valueContainer);
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateVoidPromiseT<TPromise> : ILinked<DelegateVoidPromiseT<TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateVoidPromiseT<TPromise> ILinked<DelegateVoidPromiseT<TPromise>>.Next { get; set; }

                private static ValueLinkedStack<DelegateVoidPromiseT<TPromise>> _pool;

                static DelegateVoidPromiseT()
                {
                    OnClearPool += () => _pool.Clear();
                }

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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateArgPromiseT<TArg, TPromise> : ILinked<DelegateArgPromiseT<TArg, TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateArgPromiseT<TArg, TPromise> ILinked<DelegateArgPromiseT<TArg, TPromise>>.Next { get; set; }

                private static ValueLinkedStack<DelegateArgPromiseT<TArg, TPromise>> _pool;

                static DelegateArgPromiseT()
                {
                    OnClearPool += () => _pool.Clear();
                }

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
                        owner.RejectOrCancelInternal(valueContainer);
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


            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueVoidVoid : ILinked<DelegateContinueVoidVoid>, IDelegateContinue
            {
                DelegateContinueVoidVoid ILinked<DelegateContinueVoidVoid>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueVoidVoid> _pool;

                static DelegateContinueVoidVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueVoidResult<TResult> : ILinked<DelegateContinueVoidResult<TResult>>, IDelegateContinue<TResult>
            {
                DelegateContinueVoidResult<TResult> ILinked<DelegateContinueVoidResult<TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueVoidResult<TResult>> _pool;

                static DelegateContinueVoidResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueArgVoid<TArg> : ILinked<DelegateContinueArgVoid<TArg>>, IDelegateContinue
            {
                DelegateContinueArgVoid<TArg> ILinked<DelegateContinueArgVoid<TArg>>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueArgVoid<TArg>> _pool;

                static DelegateContinueArgVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueArgResult<TArg, TResult> : ILinked<DelegateContinueArgResult<TArg, TResult>>, IDelegateContinue<TResult>
            {
                DelegateContinueArgResult<TArg, TResult> ILinked<DelegateContinueArgResult<TArg, TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueArgResult<TArg, TResult>> _pool;

                static DelegateContinueArgResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

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
            #endregion

            #region Delegates with capture value
            [System.Diagnostics.DebuggerNonUserCode]
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

                public static FinallyDelegateCapture<TCapture> GetOrCreate(ref TCapture capturedValue, Action<TCapture> onFinally)
                {
                    var del = _pool.IsNotEmpty ? (FinallyDelegateCapture<TCapture>) _pool.Pop() : new FinallyDelegateCapture<TCapture>();
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
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void ITreeHandleable.Handle()
                {
                    InvokeAndCatchAndDispose();
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    handleQueue.Push(this);
                }

                void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
                {
                    AddToHandleQueueBack(this);
                }
            }


            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateCaptureVoidVoid<TCapture> : ILinked<DelegateCaptureVoidVoid<TCapture>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateCaptureVoidVoid<TCapture> ILinked<DelegateCaptureVoidVoid<TCapture>>.Next { get; set; }

                private static ValueLinkedStack<DelegateCaptureVoidVoid<TCapture>> _pool;

                static DelegateCaptureVoidVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Action<TCapture> _callback;

                public static DelegateCaptureVoidVoid<TCapture> GetOrCreate(ref TCapture capturedValue, Action<TCapture> callback)
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
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateCaptureArgVoid<TCapture, TArg> : ILinked<DelegateCaptureArgVoid<TCapture, TArg>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateCaptureArgVoid<TCapture, TArg> ILinked<DelegateCaptureArgVoid<TCapture, TArg>>.Next { get; set; }

                private static ValueLinkedStack<DelegateCaptureArgVoid<TCapture, TArg>> _pool;

                static DelegateCaptureArgVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Action<TCapture, TArg> _callback;

                public static DelegateCaptureArgVoid<TCapture, TArg> GetOrCreate(ref TCapture capturedValue, Action<TCapture, TArg> callback)
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
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
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
                        owner.RejectOrCancelInternal(valueContainer);
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateCaptureVoidResult<TCapture, TResult> : ILinked<DelegateCaptureVoidResult<TCapture, TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateCaptureVoidResult<TCapture, TResult> ILinked<DelegateCaptureVoidResult<TCapture, TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateCaptureVoidResult<TCapture, TResult>> _pool;

                static DelegateCaptureVoidResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Func<TCapture, TResult> _callback;

                public static DelegateCaptureVoidResult<TCapture, TResult> GetOrCreate(ref TCapture capturedValue, Func<TCapture, TResult> callback)
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
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateCaptureArgResult<TCapture, TArg, TResult> : ILinked<DelegateCaptureArgResult<TCapture, TArg, TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateCaptureArgResult<TCapture, TArg, TResult> ILinked<DelegateCaptureArgResult<TCapture, TArg, TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateCaptureArgResult<TCapture, TArg, TResult>> _pool;

                static DelegateCaptureArgResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Func<TCapture, TArg, TResult> _callback;

                public static DelegateCaptureArgResult<TCapture, TArg, TResult> GetOrCreate(ref TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
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
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
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
                        owner.RejectOrCancelInternal(valueContainer);
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


            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateCaptureVoidPromise<TCapture> : ILinked<DelegateCaptureVoidPromise<TCapture>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateCaptureVoidPromise<TCapture> ILinked<DelegateCaptureVoidPromise<TCapture>>.Next { get; set; }

                private static ValueLinkedStack<DelegateCaptureVoidPromise<TCapture>> _pool;

                static DelegateCaptureVoidPromise()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Func<TCapture, Promise> _callback;

                private DelegateCaptureVoidPromise() { }

                public static DelegateCaptureVoidPromise<TCapture> GetOrCreate(ref TCapture capturedValue, Func<TCapture, Promise> callback)
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateCaptureArgPromise<TCapture, TArg> : ILinked<DelegateCaptureArgPromise<TCapture, TArg>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateCaptureArgPromise<TCapture, TArg> ILinked<DelegateCaptureArgPromise<TCapture, TArg>>.Next { get; set; }

                private static ValueLinkedStack<DelegateCaptureArgPromise<TCapture, TArg>> _pool;

                static DelegateCaptureArgPromise()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Func<TCapture, TArg, Promise> _callback;

                public static DelegateCaptureArgPromise<TCapture, TArg> GetOrCreate(ref TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
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
                        owner.RejectOrCancelInternal(valueContainer);
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateCaptureVoidPromiseT<TCapture, TPromise> : ILinked<DelegateCaptureVoidPromiseT<TCapture, TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateCaptureVoidPromiseT<TCapture, TPromise> ILinked<DelegateCaptureVoidPromiseT<TCapture, TPromise>>.Next { get; set; }

                private static ValueLinkedStack<DelegateCaptureVoidPromiseT<TCapture, TPromise>> _pool;

                static DelegateCaptureVoidPromiseT()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Func<TCapture, Promise<TPromise>> _callback;

                public static DelegateCaptureVoidPromiseT<TCapture, TPromise> GetOrCreate(ref TCapture capturedValue, Func<TCapture, Promise<TPromise>> callback)
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> : ILinked<DelegateCaptureArgPromiseT<TCapture, TArg, TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> ILinked<DelegateCaptureArgPromiseT<TCapture, TArg, TPromise>>.Next { get; set; }

                private static ValueLinkedStack<DelegateCaptureArgPromiseT<TCapture, TArg, TPromise>> _pool;

                static DelegateCaptureArgPromiseT()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Func<TCapture, TArg, Promise<TPromise>> _callback;

                public static DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> GetOrCreate(ref TCapture capturedValue, Func<TCapture, TArg, Promise<TPromise>> callback)
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
                        owner.RejectOrCancelInternal(valueContainer);
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


            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueCaptureVoidVoid<TCapture> : ILinked<DelegateContinueCaptureVoidVoid<TCapture>>, IDelegateContinue
            {
                DelegateContinueCaptureVoidVoid<TCapture> ILinked<DelegateContinueCaptureVoidVoid<TCapture>>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueCaptureVoidVoid<TCapture>> _pool;

                static DelegateContinueCaptureVoidVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Action<TCapture, ResultContainer> _callback;

                public static DelegateContinueCaptureVoidVoid<TCapture> GetOrCreate(ref TCapture capturedValue, Action<TCapture, ResultContainer> callback)
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueCaptureVoidResult<TCapture, TResult> : ILinked<DelegateContinueCaptureVoidResult<TCapture, TResult>>, IDelegateContinue<TResult>
            {
                DelegateContinueCaptureVoidResult<TCapture, TResult> ILinked<DelegateContinueCaptureVoidResult<TCapture, TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueCaptureVoidResult<TCapture, TResult>> _pool;

                static DelegateContinueCaptureVoidResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Func<TCapture, ResultContainer, TResult> _callback;

                public static DelegateContinueCaptureVoidResult<TCapture, TResult> GetOrCreate(ref TCapture capturedValue, Func<TCapture, ResultContainer, TResult> callback)
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueCaptureArgVoid<TCapture, TArg> : ILinked<DelegateContinueCaptureArgVoid<TCapture, TArg>>, IDelegateContinue
            {
                DelegateContinueCaptureArgVoid<TCapture, TArg> ILinked<DelegateContinueCaptureArgVoid<TCapture, TArg>>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueCaptureArgVoid<TCapture, TArg>> _pool;

                static DelegateContinueCaptureArgVoid()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Action<TCapture, Promise<TArg>.ResultContainer> _callback;

                public static DelegateContinueCaptureArgVoid<TCapture, TArg> GetOrCreate(ref TCapture capturedValue, Action<TCapture, Promise<TArg>.ResultContainer> callback)
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

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueCaptureArgResult<TCapture, TArg, TResult> : ILinked<DelegateContinueCaptureArgResult<TCapture, TArg, TResult>>, IDelegateContinue<TResult>
            {
                DelegateContinueCaptureArgResult<TCapture, TArg, TResult> ILinked<DelegateContinueCaptureArgResult<TCapture, TArg, TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueCaptureArgResult<TCapture, TArg, TResult>> _pool;

                static DelegateContinueCaptureArgResult()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private TCapture _capturedValue;
                private Func<TCapture, Promise<TArg>.ResultContainer, TResult> _callback;

                public static DelegateContinueCaptureArgResult<TCapture, TArg, TResult> GetOrCreate(ref TCapture capturedValue, Func<TCapture, Promise<TArg>.ResultContainer, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueCaptureArgResult<TCapture, TArg, TResult>();
                    del._capturedValue = capturedValue;
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
            #endregion

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegatePassthroughCancel : ILinked<DelegatePassthroughCancel>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegatePassthroughCancel ILinked<DelegatePassthroughCancel>.Next { get; set; }

                private static ValueLinkedStack<DelegatePassthroughCancel> _pool;

                static DelegatePassthroughCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private CancelationToken _cancelationToken;

                private DelegatePassthroughCancel() { }

                public static DelegatePassthroughCancel GetOrCreate(CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegatePassthroughCancel();
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                void IDelegateResolve.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndReleaseAndMaybeThrow();
                    owner.ResolveInternal(valueContainer);
                }

                void IDelegateReject.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndReleaseAndMaybeThrow();
                    owner.RejectOrCancelInternal(valueContainer);
                }

                void IDelegateResolvePromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndReleaseAndMaybeThrow();
                    owner.ResolveInternal(valueContainer);
                }

                void IDelegateRejectPromise.DisposeAndInvoke(IValueContainer valueContainer, Promise owner)
                {
                    DisposeAndReleaseAndMaybeThrow();
                    owner.RejectOrCancelInternal(valueContainer);
                }

                private void DisposeAndReleaseAndMaybeThrow()
                {
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                }

                public void Dispose()
                {
                    _cancelationToken = default(CancelationToken);
                }
            }

            #region Delegates with cancelation token
            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateVoidVoidCancel : ILinked<DelegateVoidVoidCancel>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateVoidVoidCancel ILinked<DelegateVoidVoidCancel>.Next { get; set; }

                private static ValueLinkedStack<DelegateVoidVoidCancel> _pool;

                static DelegateVoidVoidCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Action<CancelationToken> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                private DelegateVoidVoidCancel() { }

                public static DelegateVoidVoidCancel GetOrCreate(Action<CancelationToken> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidVoidCancel();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private void DisposeAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    temp.Invoke(_cancelationToken);
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
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
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateArgVoidCancel<TArg> : ILinked<DelegateArgVoidCancel<TArg>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateArgVoidCancel<TArg> ILinked<DelegateArgVoidCancel<TArg>>.Next { get; set; }

                private static ValueLinkedStack<DelegateArgVoidCancel<TArg>> _pool;

                static DelegateArgVoidCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Action<TArg, CancelationToken> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                public static DelegateArgVoidCancel<TArg> GetOrCreate(Action<TArg, CancelationToken> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgVoidCancel<TArg>();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private DelegateArgVoidCancel() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    temp.Invoke(arg, token);
                    owner.ResolveInternal(ResolveContainerVoid.GetOrCreate());
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
                        owner.RejectOrCancelInternal(valueContainer);
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
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateVoidResultCancel<TResult> : ILinked<DelegateVoidResultCancel<TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateVoidResultCancel<TResult> ILinked<DelegateVoidResultCancel<TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateVoidResultCancel<TResult>> _pool;

                static DelegateVoidResultCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Func<CancelationToken, TResult> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                public static DelegateVoidResultCancel<TResult> GetOrCreate(Func<CancelationToken, TResult> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidResultCancel<TResult>();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private DelegateVoidResultCancel() { }

                private void DisposeAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    TResult result = temp.Invoke(token);
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
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
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateArgResultCancel<TArg, TResult> : ILinked<DelegateArgResultCancel<TArg, TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateArgResultCancel<TArg, TResult> ILinked<DelegateArgResultCancel<TArg, TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateArgResultCancel<TArg, TResult>> _pool;

                static DelegateArgResultCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Func<TArg, CancelationToken, TResult> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                public static DelegateArgResultCancel<TArg, TResult> GetOrCreate(Func<TArg, CancelationToken, TResult> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgResultCancel<TArg, TResult>();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private DelegateArgResultCancel() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    TResult result = temp.Invoke(arg, token);
                    owner.ResolveInternal(ResolveContainer<TResult>.GetOrCreate(ref result));
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
                        owner.RejectOrCancelInternal(valueContainer);
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
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateVoidPromiseCancel : ILinked<DelegateVoidPromiseCancel>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateVoidPromiseCancel ILinked<DelegateVoidPromiseCancel>.Next { get; set; }

                private static ValueLinkedStack<DelegateVoidPromiseCancel> _pool;

                static DelegateVoidPromiseCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Func<CancelationToken, Promise> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                private DelegateVoidPromiseCancel() { }

                public static DelegateVoidPromiseCancel GetOrCreate(Func<CancelationToken, Promise> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidPromiseCancel();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private void DisposeAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    ((PromiseResolveRejectPromise0) owner).WaitFor(temp.Invoke(token));
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
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateArgPromiseCancel<TArg> : ILinked<DelegateArgPromiseCancel<TArg>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateArgPromiseCancel<TArg> ILinked<DelegateArgPromiseCancel<TArg>>.Next { get; set; }

                private static ValueLinkedStack<DelegateArgPromiseCancel<TArg>> _pool;

                static DelegateArgPromiseCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Func<TArg, CancelationToken, Promise> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                public static DelegateArgPromiseCancel<TArg> GetOrCreate(Func<TArg, CancelationToken, Promise> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgPromiseCancel<TArg>();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private DelegateArgPromiseCancel() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    ((PromiseResolveRejectPromise0) owner).WaitFor(temp.Invoke(arg, token));
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
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                }

                public void Dispose()
                {
                    _callback = null;
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateVoidPromiseTCancel<TPromise> : ILinked<DelegateVoidPromiseTCancel<TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateVoidPromiseTCancel<TPromise> ILinked<DelegateVoidPromiseTCancel<TPromise>>.Next { get; set; }

                private static ValueLinkedStack<DelegateVoidPromiseTCancel<TPromise>> _pool;

                static DelegateVoidPromiseTCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Func<CancelationToken, Promise<TPromise>> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                public static DelegateVoidPromiseTCancel<TPromise> GetOrCreate(Func<CancelationToken, Promise<TPromise>> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidPromiseTCancel<TPromise>();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private DelegateVoidPromiseTCancel() { }

                private void DisposeAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    ((PromiseResolveRejectPromise<TPromise>) owner).WaitFor(temp.Invoke(token));
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
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateArgPromiseTCancel<TArg, TPromise> : ILinked<DelegateArgPromiseTCancel<TArg, TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                DelegateArgPromiseTCancel<TArg, TPromise> ILinked<DelegateArgPromiseTCancel<TArg, TPromise>>.Next { get; set; }

                private static ValueLinkedStack<DelegateArgPromiseTCancel<TArg, TPromise>> _pool;

                static DelegateArgPromiseTCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Func<TArg, CancelationToken, Promise<TPromise>> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                public static DelegateArgPromiseTCancel<TArg, TPromise> GetOrCreate(Func<TArg, CancelationToken, Promise<TPromise>> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgPromiseTCancel<TArg, TPromise>();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private DelegateArgPromiseTCancel() { }

                private void DisposeAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    ((PromiseResolveRejectPromise<TPromise>) owner).WaitFor(temp.Invoke(arg, token));
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
                        owner.RejectOrCancelInternal(valueContainer);
                    }
                }

                public void Dispose()
                {
                    _callback = null;
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueVoidVoidCancel : ILinked<DelegateContinueVoidVoidCancel>, IDelegateContinue
            {
                DelegateContinueVoidVoidCancel ILinked<DelegateContinueVoidVoidCancel>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueVoidVoidCancel> _pool;

                static DelegateContinueVoidVoidCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Action<ResultContainer, CancelationToken> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                public static DelegateContinueVoidVoidCancel GetOrCreate(Action<ResultContainer, CancelationToken> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueVoidVoidCancel();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private DelegateContinueVoidVoidCancel() { }

                void IDelegateContinue.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    callback.Invoke(new ResultContainer(valueContainer), token);
                }

                public void Dispose()
                {
                    _callback = null;
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueVoidResultCancel<TResult> : ILinked<DelegateContinueVoidResultCancel<TResult>>, IDelegateContinue<TResult>
            {
                DelegateContinueVoidResultCancel<TResult> ILinked<DelegateContinueVoidResultCancel<TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueVoidResultCancel<TResult>> _pool;

                static DelegateContinueVoidResultCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Func<ResultContainer, CancelationToken, TResult> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                public static DelegateContinueVoidResultCancel<TResult> GetOrCreate(Func<ResultContainer, CancelationToken, TResult> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueVoidResultCancel<TResult>();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private DelegateContinueVoidResultCancel() { }

                TResult IDelegateContinue<TResult>.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    return callback.Invoke(new ResultContainer(valueContainer), token);
                }

                public void Dispose()
                {
                    _callback = null;
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueArgVoidCancel<TArg> : ILinked<DelegateContinueArgVoidCancel<TArg>>, IDelegateContinue
            {
                DelegateContinueArgVoidCancel<TArg> ILinked<DelegateContinueArgVoidCancel<TArg>>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueArgVoidCancel<TArg>> _pool;

                static DelegateContinueArgVoidCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Action<Promise<TArg>.ResultContainer, CancelationToken> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                public static DelegateContinueArgVoidCancel<TArg> GetOrCreate(Action<Promise<TArg>.ResultContainer, CancelationToken> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueArgVoidCancel<TArg>();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private DelegateContinueArgVoidCancel() { }

                void IDelegateContinue.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer), token);
                }

                public void Dispose()
                {
                    _callback = null;
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed class DelegateContinueArgResultCancel<TArg, TResult> : ILinked<DelegateContinueArgResultCancel<TArg, TResult>>, IDelegateContinue<TResult>
            {
                DelegateContinueArgResultCancel<TArg, TResult> ILinked<DelegateContinueArgResultCancel<TArg, TResult>>.Next { get; set; }

                private static ValueLinkedStack<DelegateContinueArgResultCancel<TArg, TResult>> _pool;

                static DelegateContinueArgResultCancel()
                {
                    OnClearPool += () => _pool.Clear();
                }

                private Func<Promise<TArg>.ResultContainer, CancelationToken, TResult> _callback;
                private CancelationToken _cancelationToken;
                public CancelationRegistration cancelationRegistration;

                public static DelegateContinueArgResultCancel<TArg, TResult> GetOrCreate(Func<Promise<TArg>.ResultContainer, CancelationToken, TResult> callback, CancelationToken cancelationToken)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateContinueArgResultCancel<TArg, TResult>();
                    del._callback = callback;
                    del._cancelationToken = cancelationToken;
                    return del;
                }

                private DelegateContinueArgResultCancel() { }

                TResult IDelegateContinue<TResult>.DisposeAndInvoke(IValueContainer valueContainer)
                {
                    var callback = _callback;
                    var token = _cancelationToken;
                    Dispose();
                    ReleaseAndMaybeThrow(token);
                    return callback.Invoke(new Promise<TArg>.ResultContainer(valueContainer), token);
                }

                public void Dispose()
                {
                    _callback = null;
                    _cancelationToken = default(CancelationToken);
                    UnregisterAndMakeDefault(ref cancelationRegistration);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }
            #endregion
        }
    }
}