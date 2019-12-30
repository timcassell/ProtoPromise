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

            public abstract partial class PotentialCancelation : ITreeHandleable
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                public virtual void AssignPrevious(IValueContainerOrPrevious cancelValue) { }

                void ITreeHandleable.Handle()
                {
                    Dispose();
                    DisposeBranches();
                }

                protected virtual void TakeBranches(ref ValueLinkedStack<ITreeHandleable> disposeStack) { }

                public abstract void Cancel();

                protected abstract void Dispose();

                protected void DisposeBranches()
                {
                    var branches = new ValueLinkedStack<ITreeHandleable>();
                    TakeBranches(ref branches);
                    while (branches.IsNotEmpty)
                    {
                        var current = (PotentialCancelation) branches.Pop();
                        current.Dispose();
                        current.TakeBranches(ref branches);
                    }
                }
            }

            public sealed class CancelDelegateAny : PotentialCancelation
            {
                private static ValueLinkedStack<ITreeHandleable> _pool;

                private Action _onCanceled;

                private CancelDelegateAny() { }

                static CancelDelegateAny()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegateAny GetOrCreate(Action onCanceled, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegateAny) _pool.Pop() : new CancelDelegateAny();
                    del._onCanceled = onCanceled;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    return del;
                }

                protected override void Dispose()
                {
                    _onCanceled = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public override void Cancel()
                {
                    var callback = _onCanceled;
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
            }

            public sealed class CancelDelegate<T> : PotentialCancelation, IPotentialCancelation, IValueContainerOrPrevious, IValueContainerContainer
            {
                private static ValueLinkedStack<ITreeHandleable> _pool;

                private ValueLinkedQueue<ITreeHandleable> _nextBranches;
                private IValueContainerOrPrevious _valueContainerOrPrevious;
                private Action<T> _onCanceled;
                private uint _retainCounter;

                IValueContainerOrPrevious IValueContainerContainer.ValueContainerOrPrevious { get { return _valueContainerOrPrevious; } }

                private CancelDelegate() { }

                static CancelDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                ~CancelDelegate()
                {
                    if (_retainCounter > 0)
                    {
                        // Delegate wasn't released.
                        var exception = UnhandledExceptionException.GetOrCreate(UnreleasedObjectException.instance);
                        SetStacktraceFromCreated(this, exception);
                        AddRejectionToUnhandledStack(exception);
                    }
                }

                public static CancelDelegate<T> GetOrCreate(Action<T> onCanceled, IValueContainerOrPrevious previous, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegate<T>) _pool.Pop() : new CancelDelegate<T>();
                    del._onCanceled = onCanceled;
                    del._valueContainerOrPrevious = previous;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    del.Retain();
                    return del;
                }

                protected override void TakeBranches(ref ValueLinkedStack<ITreeHandleable> disposeStack)
                {
                    disposeStack.PushAndClear(ref _nextBranches);
                }

                protected override void Dispose()
                {
                    _onCanceled = null;
                    Release();
                }

                public override void Cancel()
                {
                    var callback = _onCanceled;
                    _onCanceled = null;
                    var cancelValue = ((IValueContainerContainer) _valueContainerOrPrevious).ValueContainerOrPrevious;
                    cancelValue.Retain();
                    _valueContainerOrPrevious.Release();
                    T arg;
                    if (cancelValue.TryGetValueAs(out arg))
                    {
                        _valueContainerOrPrevious = null;
                        DisposeBranches();
                        try
                        {
                            callback.Invoke(arg);
                        }
                        catch (Exception e)
                        {
                            UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                            SetStacktraceFromCreated(this, unhandledException);
                            AddRejectionToUnhandledStack(unhandledException);
                        }
                    }
                    else
                    {
                        _valueContainerOrPrevious = cancelValue;
                    }
                    AddToCancelQueueFront(ref _nextBranches);
                    Release();
                }

                void IPotentialCancelation.CatchCancelation(Action onCanceled)
                {
                    ValidatePotentialOperation(_valueContainerOrPrevious, 1);
                    ValidateArgument(onCanceled, "onCanceled", 1);

                    if (_valueContainerOrPrevious != null)
                    {
                        _nextBranches.Enqueue(CancelDelegateAny.GetOrCreate(onCanceled, 1));
                    }
                }

                IPotentialCancelation IPotentialCancelation.CatchCancelation<TCancel>(Action<TCancel> onCanceled)
                {
                    ValidatePotentialOperation(_valueContainerOrPrevious, 1);
                    ValidateArgument(onCanceled, "onCanceled", 1);

                    if (_valueContainerOrPrevious == null)
                    {
                        return this;
                    }
                    var cancelation = CancelDelegate<TCancel>.GetOrCreate(onCanceled, this, 1);
                    _nextBranches.Enqueue(cancelation);
                    Retain();
                    return cancelation;
                }

                public void Retain()
                {
#if DEBUG
                    checked
#endif
                    {
                        ++_retainCounter;
                    }
                }

                public void Release()
                {
#if DEBUG
                    checked
#endif
                    {
                        if (--_retainCounter == 0)
                        {
                            if (_valueContainerOrPrevious != null)
                            {
                                _valueContainerOrPrevious.Release();
                            }
                            SetDisposed(ref _valueContainerOrPrevious);
                            if (Config.ObjectPooling == PoolType.All)
                            {
                                _pool.Push(this);
                            }
                        }
                    }
                }

                // This breaks Interface Segregation Principle, but cuts down on memory.
                bool IValueContainerOrPrevious.TryGetValueAs<U>(out U value) { throw new System.InvalidOperationException(); }
                bool IValueContainerOrPrevious.ContainsType<U>() { throw new System.InvalidOperationException(); }
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

            public sealed class DelegateHandleSelf0 : IDelegateResolve, IDelegateReject, IDelegateResolvePromise, IDelegateRejectPromise
            {
                public static readonly DelegateHandleSelf0 instance = new DelegateHandleSelf0();

                private DelegateHandleSelf0() { }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
                {
                    owner.ResolveInternal();
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
                {
                    owner.RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
                {
                    owner.ResolveInternal();
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
                {
                    owner.RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                }

                void IRetainable.Retain() { }
                void IRetainable.Release() { }
            }

            public sealed class DelegateHandleSelf<T> : IDelegateResolve<T>, IDelegateReject<T>, IDelegateResolvePromise<T>, IDelegateRejectPromise<T>
            {
                public static readonly DelegateHandleSelf<T> instance = new DelegateHandleSelf<T>();

                private DelegateHandleSelf() { }

                void IDelegateResolve<T>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<T> owner)
                {
                    owner.ResolveInternal();
                }

                void IDelegateReject<T>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<T> owner)
                {
                    owner.RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                }

                void IDelegateResolvePromise<T>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<T> owner)
                {
                    owner.ResolveInternal();
                }

                void IDelegateRejectPromise<T>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<T> owner)
                {
                    owner.RejectInternal(feed._rejectedOrCanceledValueOrPrevious);
                }

                void IRetainable.Retain() { }
                void IRetainable.Release() { }
            }


            public sealed class DelegateVoidVoid0 : PoolableDelegate<DelegateVoidVoid0>, IDelegateResolve, IDelegateReject
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

                private void ReleaseAndInvoke(PromiseResolveReject0 owner)
                {
                    var temp = _callback;
                    Release();
                    temp.Invoke();
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
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

            public sealed class DelegateVoidVoid<T> : PoolableDelegate<DelegateVoidVoid<T>>, IDelegateReject
            {
                private Action _callback;
                private int _retainCounter;

                public static DelegateVoidVoid<T> GetOrCreate(Action callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidVoid<T>();
                    del._callback = callback;
                    return del;
                }

                private DelegateVoidVoid() { }

                private void ReleaseAndInvoke(PromiseResolveReject0 owner)
                {
                    var temp = _callback;
                    Release();
                    temp.Invoke();
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    if (rejectValue.ContainsType<T>())
                    {
                        ReleaseAndInvoke(owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
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

            public sealed class DelegateArgVoid<TArg> : PoolableDelegate<DelegateArgVoid<TArg>>, IDelegateResolve, IDelegateReject
            {
                private Action<TArg> _callback;
                private int _retainCounter;

                public static DelegateArgVoid<TArg> GetOrCreate(Action<TArg> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgVoid<TArg>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgVoid() { }

                private void ReleaseAndInvoke(TArg arg, PromiseResolveReject0 owner)
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

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
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

            public sealed class DelegateVoidResult<TResult> : PoolableDelegate<DelegateVoidResult<TResult>>, IDelegateResolve<TResult>, IDelegateReject<TResult>
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

                private void ReleaseAndInvoke(PromiseResolveReject<TResult> owner)
                {
                    var temp = _callback;
                    Release();
                    owner._value = temp.Invoke();
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve<TResult>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<TResult> owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateReject<TResult>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<TResult> owner)
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

            public sealed class DelegateVoidResult<T, TResult> : PoolableDelegate<DelegateVoidResult<T, TResult>>, IDelegateReject<TResult>
            {
                private Func<TResult> _callback;
                private int _retainCounter;

                public static DelegateVoidResult<T, TResult> GetOrCreate(Func<TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidResult<T, TResult>();
                    del._callback = callback;
                    return del;
                }

                private DelegateVoidResult() { }

                private void ReleaseAndInvoke(PromiseResolveReject<TResult> owner)
                {
                    var temp = _callback;
                    Release();
                    owner._value = temp.Invoke();
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateReject<TResult>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<TResult> owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    if (rejectValue.ContainsType<T>())
                    {
                        ReleaseAndInvoke(owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
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

            public sealed class DelegateArgResult<TArg, TResult> : PoolableDelegate<DelegateArgResult<TArg, TResult>>, IDelegateResolve<TResult>, IDelegateReject<TResult>
            {
                private Func<TArg, TResult> _callback;
                private int _retainCounter;

                public static DelegateArgResult<TArg, TResult> GetOrCreate(Func<TArg, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgResult<TArg, TResult>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgResult() { }

                private void ReleaseAndInvoke(TArg arg, PromiseResolveReject<TResult> owner)
                {
                    var temp = _callback;
                    Release();
                    owner._value = temp.Invoke(arg);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve<TResult>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<TResult> owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateReject<TResult>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<TResult> owner)
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

                private void ReleaseAndInvoke(PromiseResolveRejectPromise0 owner)
                {
                    var temp = _callback;
                    Release();
                    owner.WaitFor(temp.Invoke());
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
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

            public sealed class DelegateVoidPromise<T> : PoolableDelegate<DelegateVoidPromise<T>>, IDelegateRejectPromise
            {
                private Func<Promise> _callback;
                private int _retainCounter;

                public static DelegateVoidPromise<T> GetOrCreate(Func<Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidPromise<T>();
                    del._callback = callback;
                    return del;
                }

                private DelegateVoidPromise() { }

                private void ReleaseAndInvoke(PromiseResolveRejectPromise0 owner)
                {
                    var temp = _callback;
                    Release();
                    owner.WaitFor(temp.Invoke());
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    if (rejectValue.ContainsType<T>())
                    {
                        ReleaseAndInvoke(owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
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
                private int _retainCounter;

                public static DelegateArgPromise<TArg> GetOrCreate(Func<TArg, Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgPromise<TArg>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgPromise() { }

                private void ReleaseAndInvoke(TArg arg, PromiseResolveRejectPromise0 owner)
                {
                    var temp = _callback;
                    Dispose();
                    owner.WaitFor(temp.Invoke(arg));
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
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

            public sealed class DelegateVoidPromiseT<TPromise> : PoolableDelegate<DelegateVoidPromiseT<TPromise>>, IDelegateResolvePromise<TPromise>, IDelegateRejectPromise<TPromise>
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

                private void ReleaseAndInvoke(PromiseResolveRejectPromise<TPromise> owner)
                {
                    var temp = _callback;
                    Release();
                    owner.WaitFor(temp.Invoke());
                }

                void IDelegateResolvePromise<TPromise>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<TPromise> owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise<TPromise>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<TPromise> owner)
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

            public sealed class DelegateVoidPromiseT<T, TPromise> : PoolableDelegate<DelegateVoidPromiseT<T, TPromise>>, IDelegateRejectPromise<TPromise>
            {
                private Func<Promise<TPromise>> _callback;
                private int _retainCounter;

                public static DelegateVoidPromiseT<T, TPromise> GetOrCreate(Func<Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateVoidPromiseT<T, TPromise>();
                    del._callback = callback;
                    return del;
                }

                private DelegateVoidPromiseT() { }

                private void ReleaseAndInvoke(PromiseResolveRejectPromise<TPromise> owner)
                {
                    var temp = _callback;
                    Release();
                    owner.WaitFor(temp.Invoke());
                }

                void IDelegateRejectPromise<TPromise>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<TPromise> owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    if (rejectValue.ContainsType<T>())
                    {
                        ReleaseAndInvoke(owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
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

            public sealed class DelegateArgPromiseT<TArg, TPromise> : PoolableDelegate<DelegateArgPromiseT<TArg, TPromise>>, IDelegateResolvePromise<TPromise>, IDelegateRejectPromise<TPromise>
            {
                private Func<TArg, Promise<TPromise>> _callback;
                private int _retainCounter;

                public static DelegateArgPromiseT<TArg, TPromise> GetOrCreate(Func<TArg, Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateArgPromiseT<TArg, TPromise>();
                    del._callback = callback;
                    return del;
                }

                private DelegateArgPromiseT() { }

                private void ReleaseAndInvoke(TArg arg, PromiseResolveRejectPromise<TPromise> owner)
                {
                    var temp = _callback;
                    Release();
                    owner.WaitFor(temp.Invoke(arg));
                }

                void IDelegateResolvePromise<TPromise>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<TPromise> owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise<TPromise>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<TPromise> owner)
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

            public sealed class CancelDelegateAnyCapture<TCapture> : PotentialCancelation
            {
                private static ValueLinkedStack<ITreeHandleable> _pool;

                private TCapture _capturedValue;
                private Action<TCapture> _onCanceled;

                private CancelDelegateAnyCapture() { }

                static CancelDelegateAnyCapture()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegateAnyCapture<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture> onCanceled, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegateAnyCapture<TCapture>) _pool.Pop() : new CancelDelegateAnyCapture<TCapture>();
                    del._capturedValue = capturedValue;
                    del._onCanceled = onCanceled;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    return del;
                }

                protected override void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _onCanceled = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public override void Cancel()
                {
                    var value = _capturedValue;
                    var callback = _onCanceled;
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
            }

            public sealed class CancelDelegateCapture<TCapture, T> : PotentialCancelation, IPotentialCancelation, IValueContainerOrPrevious, IValueContainerContainer
            {
                private static ValueLinkedStack<ITreeHandleable> _pool;

                private ValueLinkedQueue<ITreeHandleable> _nextBranches;
                private IValueContainerOrPrevious _valueContainerOrPrevious;
                private TCapture _capturedValue;
                private Action<TCapture, T> _onCanceled;
                private uint _retainCounter;

                IValueContainerOrPrevious IValueContainerContainer.ValueContainerOrPrevious { get { return _valueContainerOrPrevious; } }

                private CancelDelegateCapture() { }

                static CancelDelegateCapture()
                {
                    OnClearPool += () => _pool.Clear();
                }

                ~CancelDelegateCapture()
                {
                    if (_retainCounter > 0)
                    {
                        // Delegate wasn't released.
                        var exception = UnhandledExceptionException.GetOrCreate(UnreleasedObjectException.instance);
                        SetStacktraceFromCreated(this, exception);
                        AddRejectionToUnhandledStack(exception);
                    }
                }

                public static CancelDelegateCapture<TCapture, T> GetOrCreate(TCapture capturedValue, Action<TCapture, T> onCanceled, IValueContainerOrPrevious previous, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegateCapture<TCapture, T>) _pool.Pop() : new CancelDelegateCapture<TCapture, T>();
                    del._capturedValue = capturedValue;
                    del._onCanceled = onCanceled;
                    del._valueContainerOrPrevious = previous;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    del.Retain();
                    return del;
                }

                protected override void TakeBranches(ref ValueLinkedStack<ITreeHandleable> disposeStack)
                {
                    disposeStack.PushAndClear(ref _nextBranches);
                }

                protected override void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _onCanceled = null;
                    Release();
                }

                public override void Cancel()
                {
                    var value = _capturedValue;
                    _capturedValue = default(TCapture);
                    var callback = _onCanceled;
                    _onCanceled = null;
                    var cancelValue = ((IValueContainerContainer) _valueContainerOrPrevious).ValueContainerOrPrevious;
                    cancelValue.Retain();
                    _valueContainerOrPrevious.Release();
                    T arg;
                    if (cancelValue.TryGetValueAs(out arg))
                    {
                        _valueContainerOrPrevious = null;
                        DisposeBranches();
                        try
                        {
                            callback.Invoke(value, arg);
                        }
                        catch (Exception e)
                        {
                            UnhandledExceptionException unhandledException = UnhandledExceptionException.GetOrCreate(e);
                            SetStacktraceFromCreated(this, unhandledException);
                            AddRejectionToUnhandledStack(unhandledException);
                        }
                    }
                    else
                    {
                        _valueContainerOrPrevious = cancelValue;
                    }
                    AddToCancelQueueFront(ref _nextBranches);
                    Release();
                }

                void IPotentialCancelation.CatchCancelation(Action onCanceled)
                {
                    ValidatePotentialOperation(_valueContainerOrPrevious, 1);
                    ValidateArgument(onCanceled, "onCanceled", 1);

                    if (_valueContainerOrPrevious != null)
                    {
                        _nextBranches.Enqueue(CancelDelegateAny.GetOrCreate(onCanceled, 1));
                    }
                }

                IPotentialCancelation IPotentialCancelation.CatchCancelation<TCancel>(Action<TCancel> onCanceled)
                {
                    ValidatePotentialOperation(_valueContainerOrPrevious, 1);
                    ValidateArgument(onCanceled, "onCanceled", 1);

                    if (_valueContainerOrPrevious == null)
                    {
                        return this;
                    }
                    var cancelation = CancelDelegate<TCancel>.GetOrCreate(onCanceled, this, 1);
                    _nextBranches.Enqueue(cancelation);
                    Retain();
                    return cancelation;
                }

                public void Retain()
                {
#if DEBUG
                    checked
#endif
                    {
                        ++_retainCounter;
                    }
                }

                public void Release()
                {
#if DEBUG
                    checked
#endif
                    {
                        if (--_retainCounter == 0)
                        {
                            if (_valueContainerOrPrevious != null)
                            {
                                _valueContainerOrPrevious.Release();
                            }
                            SetDisposed(ref _valueContainerOrPrevious);
                            if (Config.ObjectPooling == PoolType.All)
                            {
                                _pool.Push(this);
                            }
                        }
                    }
                }

                // This breaks Interface Segregation Principle, but cuts down on memory.
                bool IValueContainerOrPrevious.TryGetValueAs<U>(out U value) { throw new System.InvalidOperationException(); }
                bool IValueContainerOrPrevious.ContainsType<U>() { throw new System.InvalidOperationException(); }
            }


            public sealed class DelegateCaptureVoidVoid<TCapture> : PoolableDelegate<DelegateCaptureVoidVoid<TCapture>>, IDelegateResolve, IDelegateReject
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

                private void ReleaseAndInvoke(PromiseResolveReject0 owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    temp.Invoke(value);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
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

            public class DelegateCaptureVoidVoid<TCapture, T> : PoolableDelegate<DelegateCaptureVoidVoid<TCapture, T>>, IDelegateReject
            {
                private TCapture _capturedValue;
                private Action<TCapture> _callback;
                private int _retainCounter;

                public static DelegateCaptureVoidVoid<TCapture, T> GetOrCreate(TCapture capturedValue, Action<TCapture> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidVoid<TCapture, T>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureVoidVoid() { }

                private void ReleaseAndInvoke(PromiseResolveReject0 owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    temp.Invoke(value);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    if (rejectValue.ContainsType<T>())
                    {
                        ReleaseAndInvoke(owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
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

            public sealed class DelegateCaptureArgVoid<TCapture, TArg> : PoolableDelegate<DelegateCaptureArgVoid<TCapture, TArg>>, IDelegateResolve, IDelegateReject
            {
                private TCapture _capturedValue;
                private Action<TCapture, TArg> _callback;
                private int _retainCounter;

                public static DelegateCaptureArgVoid<TCapture, TArg> GetOrCreate(TCapture capturedValue, Action<TCapture, TArg> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureArgVoid<TCapture, TArg>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureArgVoid() { }

                private void ReleaseAndInvoke(TArg arg, PromiseResolveReject0 owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    temp.Invoke(value, arg);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateReject.ReleaseAndInvoke(Promise feed, PromiseResolveReject0 owner)
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

            public sealed class DelegateCaptureVoidResult<TCapture, TResult> : PoolableDelegate<DelegateCaptureVoidResult<TCapture, TResult>>, IDelegateResolve<TResult>, IDelegateReject<TResult>
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

                private void ReleaseAndInvoke(PromiseResolveReject<TResult> owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    owner._value = temp.Invoke(value);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve<TResult>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<TResult> owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateReject<TResult>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<TResult> owner)
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

            public sealed class DelegateCaptureVoidResult<TCapture, T, TResult> : PoolableDelegate<DelegateCaptureVoidResult<TCapture, T, TResult>>, IDelegateReject<TResult>
            {
                private TCapture _capturedValue;
                private Func<TCapture, TResult> _callback;
                private int _retainCounter;

                public static DelegateCaptureVoidResult<TCapture, T, TResult> GetOrCreate(TCapture capturedValue, Func<TCapture, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidResult<TCapture, T, TResult>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureVoidResult() { }

                private void ReleaseAndInvoke(PromiseResolveReject<TResult> owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    owner._value = temp.Invoke(value);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateReject<TResult>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<TResult> owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    if (rejectValue.ContainsType<T>())
                    {
                        ReleaseAndInvoke(owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
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

            public sealed class DelegateCaptureArgResult<TCapture, TArg, TResult> : PoolableDelegate<DelegateCaptureArgResult<TCapture, TArg, TResult>>, IDelegateResolve<TResult>, IDelegateReject<TResult>
            {
                private TCapture _capturedValue;
                private Func<TCapture, TArg, TResult> _callback;
                private int _retainCounter;

                public static DelegateCaptureArgResult<TCapture, TArg, TResult> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, TResult> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureArgResult<TCapture, TArg, TResult>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureArgResult() { }

                private void ReleaseAndInvoke(TArg arg, PromiseResolveReject<TResult> owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    owner._value = temp.Invoke(value, arg);
                    owner.ResolveInternalIfNotCanceled();
                }

                void IDelegateResolve<TResult>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<TResult> owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateReject<TResult>.ReleaseAndInvoke(Promise feed, PromiseResolveReject<TResult> owner)
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

                private void ReleaseAndInvoke(PromiseResolveRejectPromise0 owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    owner.WaitFor(temp.Invoke(value));
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
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

            public sealed class DelegateCaptureVoidPromise<TCapture, T> : PoolableDelegate<DelegateCaptureVoidPromise<TCapture, T>>, IDelegateRejectPromise
            {
                private TCapture _capturedValue;
                private Func<TCapture, Promise> _callback;
                private int _retainCounter;

                public static DelegateCaptureVoidPromise<TCapture, T> GetOrCreate(TCapture capturedValue, Func<TCapture, Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidPromise<TCapture, T>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureVoidPromise() { }

                private void ReleaseAndInvoke(PromiseResolveRejectPromise0 owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    owner.WaitFor(temp.Invoke(value));
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    if (rejectValue.ContainsType<T>())
                    {
                        ReleaseAndInvoke(owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
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
                private int _retainCounter;

                public static DelegateCaptureArgPromise<TCapture, TArg> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, Promise> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureArgPromise<TCapture, TArg>();
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureArgPromise() { }

                private void ReleaseAndInvoke(TArg arg, PromiseResolveRejectPromise0 owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Dispose();
                    owner.WaitFor(temp.Invoke(value, arg));
                }

                public void Dispose()
                {
                    _callback = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void IDelegateResolvePromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise0 owner)
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

            public sealed class DelegateCaptureVoidPromiseT<TCapture, TPromise> : PoolableDelegate<DelegateCaptureVoidPromiseT<TCapture, TPromise>>, IDelegateResolvePromise<TPromise>, IDelegateRejectPromise<TPromise>
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

                private void ReleaseAndInvoke(PromiseResolveRejectPromise<TPromise> owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    owner.WaitFor(temp.Invoke(value));
                }

                void IDelegateResolvePromise<TPromise>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<TPromise> owner)
                {
                    ReleaseAndInvoke(owner);
                }

                void IDelegateRejectPromise<TPromise>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<TPromise> owner)
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

            public sealed class DelegateCaptureVoidPromiseT<TCapture, T, TPromise> : PoolableDelegate<DelegateCaptureVoidPromiseT<TCapture, T, TPromise>>, IDelegateRejectPromise<TPromise>
            {
                private TCapture _capturedValue;
                private Func<TCapture, Promise<TPromise>> _callback;
                private int _retainCounter;

                public static DelegateCaptureVoidPromiseT<TCapture, T, TPromise> GetOrCreate(TCapture capturedValue, Func<TCapture, Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureVoidPromiseT<TCapture, T, TPromise>();
                    del._capturedValue = capturedValue;
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureVoidPromiseT() { }

                private void ReleaseAndInvoke(PromiseResolveRejectPromise<TPromise> owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    owner.WaitFor(temp.Invoke(value));
                }

                void IDelegateRejectPromise<TPromise>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<TPromise> owner)
                {
                    var rejectValue = feed._rejectedOrCanceledValueOrPrevious;
                    if (rejectValue.ContainsType<T>())
                    {
                        ReleaseAndInvoke(owner);
                    }
                    else
                    {
                        Release();
                        owner.RejectInternalIfNotCanceled(rejectValue);
                    }
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

            public sealed class DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> : PoolableDelegate<DelegateCaptureArgPromiseT<TCapture, TArg, TPromise>>, IDelegateResolvePromise<TPromise>, IDelegateRejectPromise<TPromise>
            {
                private TCapture _capturedValue;
                private Func<TCapture, TArg, Promise<TPromise>> _callback;
                private int _retainCounter;

                public static DelegateCaptureArgPromiseT<TCapture, TArg, TPromise> GetOrCreate(TCapture capturedValue, Func<TCapture, TArg, Promise<TPromise>> callback)
                {
                    var del = _pool.IsNotEmpty ? _pool.Pop() : new DelegateCaptureArgPromiseT<TCapture, TArg, TPromise>();
                    del._callback = callback;
                    return del;
                }

                private DelegateCaptureArgPromiseT() { }

                private void ReleaseAndInvoke(TArg arg, PromiseResolveRejectPromise<TPromise> owner)
                {
                    var value = _capturedValue;
                    var temp = _callback;
                    Release();
                    owner.WaitFor(temp.Invoke(value, arg));
                }

                void IDelegateResolvePromise<TPromise>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<TPromise> owner)
                {
                    ReleaseAndInvoke(((PromiseInternal<TArg>) feed)._value, owner);
                }

                void IDelegateRejectPromise<TPromise>.ReleaseAndInvoke(Promise feed, PromiseResolveRejectPromise<TPromise> owner)
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
        }
    }
}