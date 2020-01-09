#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable RECS0001 // Class is declared partial but has only one part
#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        // Calls to this get compiled away when CANCEL is defined.
        static partial void ValidateCancel(int skipFrames);

        static partial void AddToCancelQueueBack(Internal.ITreeHandleable cancelation);
        static partial void AddToCancelQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations);
        static partial void HandleCanceled();
#if PROMISE_CANCEL
        // Cancel promises in a depth-first manner.
        private static ValueLinkedQueue<Internal.ITreeHandleable> _cancelQueue;

        static partial void AddToCancelQueueBack(Internal.ITreeHandleable cancelation)
        {
            _cancelQueue.Enqueue(cancelation);
        }

        static partial void AddToCancelQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations)
        {
            _cancelQueue.PushAndClear(ref cancelations);
        }

        static partial void HandleCanceled()
        {
            while (_cancelQueue.IsNotEmpty)
            {
                _cancelQueue.DequeueRisky().Cancel();
            }
            _cancelQueue.ClearLast();
        }
#else
        static protected void ThrowCancelException(int skipFrames)
        {
            throw new InvalidOperationException("Cancelations are disabled. Remove PROTO_PROMISE_CANCEL_DISABLE from your compiler symbols to enable cancelations.", GetFormattedStacktrace(skipFrames + 1));
        }

        static partial void ValidateCancel(int skipFrames)
        {
            ThrowCancelException(skipFrames + 1);
        }
#endif

        partial class Internal
        {
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

                void IPotentialCancelation.CatchCancelation<TCapture>(TCapture captureValue, Action<TCapture> onCanceled)
                {
                    ValidatePotentialOperation(_valueContainerOrPrevious, 1);
                    ValidateArgument(onCanceled, "onCanceled", 1);

                    if (_valueContainerOrPrevious != null)
                    {
                        _nextBranches.Enqueue(CancelDelegateAnyCapture<TCapture>.GetOrCreate(captureValue, onCanceled, 1));
                    }
                }

                IPotentialCancelation IPotentialCancelation.CatchCancelation<TCapture, TCancel>(TCapture captureValue, Action<TCapture, TCancel> onCanceled)
                {
                    ValidatePotentialOperation(_valueContainerOrPrevious, 1);
                    ValidateArgument(onCanceled, "onCanceled", 1);

                    if (_valueContainerOrPrevious == null)
                    {
                        return this;
                    }
                    var cancelation = CancelDelegateCapture<TCapture, TCancel>.GetOrCreate(captureValue, onCanceled, this, 1);
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

                void IPotentialCancelation.CatchCancelation<TCapture1>(TCapture1 captureValue, Action<TCapture1> onCanceled)
                {
                    ValidatePotentialOperation(_valueContainerOrPrevious, 1);
                    ValidateArgument(onCanceled, "onCanceled", 1);

                    if (_valueContainerOrPrevious != null)
                    {
                        _nextBranches.Enqueue(CancelDelegateAnyCapture<TCapture1>.GetOrCreate(captureValue, onCanceled, 1));
                    }
                }

                IPotentialCancelation IPotentialCancelation.CatchCancelation<TCapture1, TCancel>(TCapture1 captureValue, Action<TCapture1, TCancel> onCanceled)
                {
                    ValidatePotentialOperation(_valueContainerOrPrevious, 1);
                    ValidateArgument(onCanceled, "onCanceled", 1);

                    if (_valueContainerOrPrevious == null)
                    {
                        return this;
                    }
                    var cancelation = CancelDelegateCapture<TCapture1, TCancel>.GetOrCreate(captureValue, onCanceled, this, 1);
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
            }
        }

        private void ResolveDirectIfNotCanceled()
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                ReleaseInternal();
            }
            else
#endif
            {
                _state = State.Resolved;
                AddToHandleQueueBack(this);
            }
        }

        protected void RejectDirectIfNotCanceled(Internal.IValueContainerOrPrevious rejectValue)
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                AddRejectionToUnhandledStack((Internal.UnhandledExceptionInternal) rejectValue);
                ReleaseInternal();
            }
            else
#endif
            {
                RejectDirect(rejectValue);
            }
        }

        protected void ResolveInternalIfNotCanceled()
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                ReleaseInternal();
            }
            else
#endif
            {
                ResolveInternal();
            }
        }

        protected void RejectInternalIfNotCanceled(Internal.IValueContainerOrPrevious rejectValue)
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                AddRejectionToUnhandledStack((Internal.UnhandledExceptionInternal) rejectValue);
                ReleaseInternal();
            }
            else
#endif
            {
                RejectInternal(rejectValue);
            }
        }

        protected void AddWaiter(Internal.ITreeHandleable waiter)
        {
            RetainInternal();
            if (_state == State.Pending)
            {
                _nextBranches.Enqueue(waiter);
            }
#if PROMISE_CANCEL
            else if (_state == State.Canceled)
            {
                AddToCancelQueueBack(waiter);
            }
#endif
            else
            {
                AddToHandleQueueBack(waiter);
            }
        }

        void Internal.ITreeHandleable.Handle()
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                ReleaseInternal();
            }
            else
#endif
            {
                Handle();
            }
        }
    }

    partial class Promise<T>
    {
        // Calls to this get compiled away when CANCEL is defined.
        static partial void ValidateCancel(int skipFrames);
#if !PROMISE_CANCEL
        static partial void ValidateCancel(int skipFrames)
        {
            ThrowCancelException(skipFrames + 1);
        }
#endif
    }
}