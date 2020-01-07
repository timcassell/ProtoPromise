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

                public static FinallyDelegate GetOrCreate(Action onFinally, Promise owner, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (FinallyDelegate) _pool.Pop() : new FinallyDelegate();
                    del._onFinally = onFinally;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    return del;
                }

                private void InvokeAndCatchAndDispose()
                {
                    var callback = _onFinally;
                    Dispose();
                    try
                    {
                        callback.Invoke();
                    }
                    catch (Exception e)
                    {
                        UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                        SetStacktraceFromCreated(this, unhandledException);
                        AddRejectionToUnhandledStack(unhandledException);
                    }
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
            }


            public sealed class DelegatePassthrough : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private static readonly DelegatePassthrough _instance = new DelegatePassthrough();

                private DelegatePassthrough() { }

                public static DelegatePassthrough GetOrCreate()
                {
                    return _instance;
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    owner.ResolveInternal(feed);
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    owner.RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    owner.ResolveInternal(feed);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    owner.RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                }

                void IRetainable.Retain() { }
                void IRetainable.Release() { }
            }


            public abstract class PoolableDelegate<TDelegate> : ILinked<TDelegate> where TDelegate : PoolableDelegate<TDelegate>
            {
                TDelegate ILinked<TDelegate>.Next { get; set; }

                protected static ValueLinkedStack<TDelegate> _pool;

                static PoolableDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }
            }

            public sealed class DelegateVoidVoid0 : PoolableDelegate<DelegateVoidVoid0>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Action _callback;
                private int _retainCounter;

                private DelegateVoidVoid0() { }

                public static DelegateVoidVoid0 GetOrCreate(Action callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidVoid0();
                    del._callback = callback;
                    return del;
                }

                private void ReleaseAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    Release();
                    temp.Invoke();
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                public void Retain()
                {
                    ++_retainCounter;
                }

                public void Release()
                {
                    if (--_retainCounter == 0)
                    {
                        _callback = null;
                        if (Config.ObjectPooling != PoolType.None)
                        {
                            _pool.Push(this);
                        }
                    }
                }
            }

            public sealed class DelegateArgVoid<TArg> : PoolableDelegate<DelegateArgVoid<TArg>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Action<TArg> _callback;

                public static DelegateArgVoid<TArg> GetOrCreate(Action<TArg> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgVoid<TArg>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgVoid() { }

                private void ReleaseAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    Dispose();
                    temp.Invoke(arg);
                    owner.ResolveInternalIfNotCanceled();
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                private void TryReleaseAndInvoke(Promise feed, Promise owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    TArg arg;
                    if (rejectValue.TryGetValueAs(out arg))
                    {
                        ReleaseAndInvoke(arg, owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    TryReleaseAndInvoke(feed, owner);
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    TryReleaseAndInvoke(feed, owner);
                }

                void IRetainable.Retain() { }

                public void Release()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            public sealed class DelegateVoidResult<TResult> : PoolableDelegate<DelegateVoidResult<TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<TResult> _callback;
                private int _retainCounter;

                public static DelegateVoidResult<TResult> GetOrCreate(Func<TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidResult<TResult>();
                    del._callback = callback;
                    return del;
                }

                private DelegateVoidResult() { }

                private void ReleaseAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    Release();
                    ((PromiseInternal<TResult>) owner)._value = temp.Invoke();
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                public void Retain()
                {
                    ++_retainCounter;
                }

                public void Release()
                {
                    if (--_retainCounter == 0)
                    {
                        _callback = null;
                        if (Config.ObjectPooling != PoolType.None)
                        {
                            _pool.Push(this);
                        }
                    }
                }
            }

            public sealed class DelegateArgResult<TArg, TResult> : PoolableDelegate<DelegateArgResult<TArg, TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<TArg, TResult> _callback;

                public static DelegateArgResult<TArg, TResult> GetOrCreate(Func<TArg, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgResult<TArg, TResult>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgResult() { }

                private void ReleaseAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    Release();
                    ((PromiseInternal<TResult>) owner)._value = temp.Invoke(arg);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                private void TryReleaseAndInvoke(Promise feed, Promise owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    TArg arg;
                    if (rejectValue.TryGetValueAs(out arg))
                    {
                        ReleaseAndInvoke(arg, owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    TryReleaseAndInvoke(feed, owner);
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    TryReleaseAndInvoke(feed, owner);
                }

                void IRetainable.Retain() { }

                public void Release()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


            public sealed class DelegateVoidPromise0 : PoolableDelegate<DelegateVoidPromise0>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<Promise> _callback;
                private int _retainCounter;

                private DelegateVoidPromise0() { }

                public static DelegateVoidPromise0 GetOrCreate(Func<Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidPromise0();
                    del._callback = callback;
                    return del;
                }

                private void ReleaseAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    Release();
                    ((PromiseResolveRejectPromise0) owner).WaitFor(temp.Invoke());
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                public void Retain()
                {
                    ++_retainCounter;
                }

                public void Release()
                {
                    if (--_retainCounter == 0)
                    {
                        _callback = null;
                        if (Config.ObjectPooling != PoolType.None)
                        {
                            _pool.Push(this);
                        }
                    }
                }
            }

            public sealed class DelegateArgPromise<TArg> : PoolableDelegate<DelegateArgPromise<TArg>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<TArg, Promise> _callback;

                public static DelegateArgPromise<TArg> GetOrCreate(Func<TArg, Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgPromise<TArg>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgPromise() { }

                private void ReleaseAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    Dispose();
                    ((PromiseResolveRejectPromise0) owner).WaitFor(temp.Invoke(arg));
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    TArg arg;
                    if (rejectValue.TryGetValueAs(out arg))
                    {
                        ReleaseAndInvoke(arg, owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
                }

                void IRetainable.Retain() { }

                public void Release()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            public sealed class DelegateVoidPromiseT<TPromise> : PoolableDelegate<DelegateVoidPromiseT<TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<Promise<TPromise>> _callback;
                private int _retainCounter;

                public static DelegateVoidPromiseT<TPromise> GetOrCreate(Func<Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidPromiseT<TPromise>();
                    del._callback = callback;
                    return del;
                }

                private DelegateVoidPromiseT() { }

                private void ReleaseAndInvoke(Promise owner)
                {
                    var temp = _callback;
                    Release();
                    ((PromiseResolveRejectPromise<TPromise>) owner).WaitFor(temp.Invoke());
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                public void Retain()
                {
                    ++_retainCounter;
                }

                public void Release()
                {
                    if (--_retainCounter == 0)
                    {
                        _callback = null;
                        if (Config.ObjectPooling != PoolType.None)
                        {
                            _pool.Push(this);
                        }
                    }
                }
            }

            public sealed class DelegateArgPromiseT<TArg, TPromise> : PoolableDelegate<DelegateArgPromiseT<TArg, TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private Func<TArg, Promise<TPromise>> _callback;

                public static DelegateArgPromiseT<TArg, TPromise> GetOrCreate(Func<TArg, Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgPromiseT<TArg, TPromise>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgPromiseT() { }

                private void ReleaseAndInvoke(TArg arg, Promise owner)
                {
                    var temp = _callback;
                    Release();
                    ((PromiseResolveRejectPromise<TPromise>) owner).WaitFor(temp.Invoke(arg));
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    TArg arg;
                    if (rejectValue.TryGetValueAs(out arg))
                    {
                        ReleaseAndInvoke(arg, owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
                }

                void IRetainable.Retain() { }

                public void Release()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


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

                public static FinallyDelegateCapture<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture> onFinally, Promise owner, int skipFrames)
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
                    Dispose();
                    try
                    {
                        callback.Invoke(value);
                    }
                    catch (Exception e)
                    {
                        UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                        SetStacktraceFromCreated(this, unhandledException);
                        AddRejectionToUnhandledStack(unhandledException);
                    }
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
            }


            public sealed class DelegateCaptureVoidVoid<TCapture> : PoolableDelegate<DelegateCaptureVoidVoid<TCapture>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Action<TCapture> _callback;
                private int _retainCounter;

                public static DelegateCaptureVoidVoid<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidVoid<TCapture>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureVoidVoid() { }

                private void ReleaseAndInvoke(Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    temp.Invoke(value);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                public void Retain()
                {
                    ++_retainCounter;
                }

                public void Release()
                {
                    if (--_retainCounter == 0)
                    {
                        _capturedValue = default(TCapture);
                        _callback = null;
                        if (Config.ObjectPooling != PoolType.None)
                        {
                            _pool.Push(this);
                        }
                    }
                }
            }

            public sealed class DelegateCaptureArgVoid<TCapture, TArg> : PoolableDelegate<DelegateCaptureArgVoid<TCapture, TArg>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
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

                private void ReleaseAndInvoke(TArg arg, Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    temp.Invoke(value, arg);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                private void TryReleaseAndInvoke(Promise feed, Promise owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    TArg arg;
                    if (rejectValue.TryGetValueAs(out arg))
                    {
                        ReleaseAndInvoke(arg, owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    TryReleaseAndInvoke(feed, owner);
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    TryReleaseAndInvoke(feed, owner);
                }

                void IRetainable.Retain() { }

                public void Release()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            public sealed class DelegateCaptureVoidResult<TCapture, TResult> : PoolableDelegate<DelegateCaptureVoidResult<TCapture, TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, TResult> _callback;
                private int _retainCounter;

                public static DelegateCaptureVoidResult<TCapture, TResult> GetOrCreate(TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidResult<TCapture, TResult>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureVoidResult() { }

                private void ReleaseAndInvoke(Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    ((PromiseInternal<TResult>) owner)._value = temp.Invoke(value);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                public void Retain()
                {
                    ++_retainCounter;
                }

                public void Release()
                {
                    if (--_retainCounter == 0)
                    {
                        _capturedValue = default(TCapture);
                        _callback = null;
                        if (Config.ObjectPooling != PoolType.None)
                        {
                            _pool.Push(this);
                        }
                    }
                }
            }

            public sealed class DelegateCaptureArgResult<TCapture, TArg, TResult> : PoolableDelegate<DelegateCaptureArgResult<TCapture, TArg, TResult>>, IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
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

                private void ReleaseAndInvoke(TArg arg, Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    ((PromiseInternal<TResult>) owner)._value = temp.Invoke(value, arg);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                private void TryReleaseAndInvoke(Promise feed, Promise owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    TArg arg;
                    if (rejectValue.TryGetValueAs(out arg))
                    {
                        ReleaseAndInvoke(arg, owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    TryReleaseAndInvoke(feed, owner);
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    TryReleaseAndInvoke(feed, owner);
                }

                void IRetainable.Retain() { }

                public void Release()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


            public sealed class DelegateCaptureVoidPromise<TCapture> : PoolableDelegate<DelegateCaptureVoidPromise<TCapture>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, Promise> _callback;
                private int _retainCounter;

                private DelegateCaptureVoidPromise() { }

                public static DelegateCaptureVoidPromise<TCapture> GetOrCreate(TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidPromise<TCapture>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private void ReleaseAndInvoke(Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    ((PromiseResolveRejectPromise0) owner).WaitFor(temp.Invoke(value));
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                public void Retain()
                {
                    ++_retainCounter;
                }

                public void Release()
                {
                    if (--_retainCounter == 0)
                    {
                        _capturedValue = default(TCapture);
                        _callback = null;
                        if (Config.ObjectPooling != PoolType.None)
                        {
                            _pool.Push(this);
                        }
                    }
                }
            }

            public sealed class DelegateCaptureArgPromise<TCapture, TArg> : PoolableDelegate<DelegateCaptureArgPromise<TCapture, TArg>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, TArg, Promise> _callback;

                public static DelegateCaptureArgPromise<TCapture, TArg> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureArgPromise<TCapture, TArg>();
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureArgPromise() { }

                private void ReleaseAndInvoke(TArg arg, Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Dispose();
                    ((PromiseResolveRejectPromise0) owner).WaitFor(temp.Invoke(value, arg));
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    TArg arg;
                    if (rejectValue.TryGetValueAs(out arg))
                    {
                        ReleaseAndInvoke(arg, owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
                }

                void IRetainable.Retain() { }

                public void Release()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            public sealed class DelegateCaptureVoidPromiseT<TCapture, TPromise> : PoolableDelegate<DelegateCaptureVoidPromiseT<TCapture, TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, Promise<TPromise>> _callback;
                private int _retainCounter;

                public static DelegateCaptureVoidPromiseT<TCapture, TPromise> GetOrCreate(TCapture capturedValue, Func<TCapture, Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidPromiseT<TCapture, TPromise>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureVoidPromiseT() { }

                private void ReleaseAndInvoke(Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    ((PromiseResolveRejectPromise<TPromise>) owner).WaitFor(temp.Invoke(value));
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                public void Retain()
                {
                    ++_retainCounter;
                }

                public void Release()
                {
                    if (--_retainCounter == 0)
                    {
                        _capturedValue = default(TCapture);
                        _callback = null;
                        if (Config.ObjectPooling != PoolType.None)
                        {
                            _pool.Push(this);
                        }
                    }
                }
            }

            public sealed class DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> : PoolableDelegate<DelegateCaptureArgPromiseT<TCapture, TArg, TPromise>>, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, TArg, Promise<TPromise>> _callback;

                public static DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureArgPromiseT<TCapture, TArg, TPromise>();
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureArgPromiseT() { }

                private void ReleaseAndInvoke(TArg arg, Promise owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    ((PromiseResolveRejectPromise<TPromise>) owner).WaitFor(temp.Invoke(value, arg));
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    TArg arg;
                    if (rejectValue.TryGetValueAs(out arg))
                    {
                        ReleaseAndInvoke(arg, owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
                }

                void IRetainable.Retain() { }

                public void Release()
                {
                    _capturedValue = default(TCapture);
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }


            public sealed class DelegateCapture<TCapture> : PoolableDelegate<DelegateCapture<TCapture>>, IDelegateReject, IDelegateRejectPromise
            {
                TCapture _capturedValue;

                private DelegateCapture() { }

                public static DelegateCapture<TCapture> GetOrCreate(TCapture capturedValue)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCapture<TCapture>();
                    del._capturedValue = capturedValue;
                    return del;
                }

                private void ReleaseAndInvoke(Promise owner)
                {
                    ((PromiseInternal<TCapture>) owner)._value = _capturedValue;
                    Release();
                    owner.ResolveInternal();
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IRetainable.Retain() { }

                public void Release()
                {
                    _capturedValue = default(TCapture);
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }
            }

            public sealed class DelegateCapturePreserve : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                private static readonly DelegateCapturePreserve _instance = new DelegateCapturePreserve();

                private DelegateCapturePreserve() { }

                public static DelegateCapturePreserve GetOrCreate()
                {
                    return _instance;
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    owner.ResolveInternal();
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    owner.ResolveInternal();
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    owner.ResolveInternal();
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, Promise owner)
                {
                    owner.ResolveInternal();
                }

                void IRetainable.Retain() { }
                void IRetainable.Release() { }
            }
        }
    }
}