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
#pragma warning disable CS0618 // Type or member is obsolete

using System;
using System.Collections.Generic;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        // Calls to this get compiled away when CANCEL is defined.
        static partial void ValidateCancel(int skipFrames);

        static partial void AddToCancelQueueBack(Internal.ITreeHandleable cancelation);
        static partial void AddToCancelQueueFront(Internal.ITreeHandleable cancelation);
        static partial void AddToCancelQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations);
        static partial void AddToCancelQueueBack(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations);
        static partial void HandleCanceled();
        partial void CancelDirectIfPending();
#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        partial void CancelDirectIfPending<TCancel>(in TCancel reason);
#else
        partial void CancelDirectIfPending<TCancel>(TCancel reason);
#endif

#if PROMISE_CANCEL
        // Cancel promises in a depth-first manner.
        private static ValueLinkedQueue<Internal.ITreeHandleable> _cancelQueue;

        static partial void AddToCancelQueueFront(Internal.ITreeHandleable cancelation)
        {
            _cancelQueue.Push(cancelation);
        }

        static partial void AddToCancelQueueBack(Internal.ITreeHandleable cancelation)
        {
            _cancelQueue.Enqueue(cancelation);
        }

        static partial void AddToCancelQueueFront(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations)
        {
            _cancelQueue.PushAndClear(ref cancelations);
        }

        static partial void AddToCancelQueueBack(ref ValueLinkedQueue<Internal.ITreeHandleable> cancelations)
        {
            _cancelQueue.EnqueueAndClear(ref cancelations);
        }

        static partial void HandleCanceled()
        {
            while (_cancelQueue.IsNotEmpty)
            {
                _cancelQueue.DequeueRisky().Cancel();
            }
            _cancelQueue.ClearLast();
        }

        void Internal.ITreeHandleable.Cancel()
        {
            if (_state == State.Pending)
            {
                HandleCancel();
            }
            else
            {
                ReleaseInternal();
            }
        }

        private void MaybeReleaseContainer()
        {
            // Handle edge case where the promise is pending with a value container and is canceled before it's handled (it's canceled during its own callback).
#if CSHARP_7_OR_LATER
            if (((object) _valueOrPrevious) is Internal.IValueContainer container)
#else
            Internal.IValueContainer container = _valueOrPrevious as Internal.IValueContainer;
            if (container != null)
#endif
            {
                container.ReleaseAndMaybeAddToUnhandledStack();
            }
        }

        partial void CancelDirectIfPending()
        {
            if (_state != State.Pending)
            {
                if (_retainCounter == 0)
                {
                    AddToHandleQueueFront(this);
                }
            }
            else
            {
                _state = State.Canceled;
                MaybeReleaseContainer();
                var cancelValue = Internal.CancelContainerVoid.GetOrCreate();
                _valueOrPrevious = cancelValue;
                AddBranchesToCancelQueueBack(cancelValue);
                CancelProgressListeners();
                OnCancel();
            }
        }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        partial void CancelDirectIfPending<TCancel>(in TCancel reason)
#else
        partial void CancelDirectIfPending<TCancel>(TCancel reason)
#endif
        {
            if (_state != State.Pending)
            {
                if (_retainCounter == 0)
                {
                    AddToHandleQueueFront(this);
                }
                return;
            }

            _state = State.Canceled;
            MaybeReleaseContainer();
            Internal.IValueContainer cancelValue = CreateCancelContainer(reason);
            cancelValue.Retain();
            _valueOrPrevious = cancelValue;
            AddBranchesToCancelQueueBack(cancelValue);
            CancelProgressListeners();
            OnCancel();
        }

        private static Internal.ICancelValueContainer CreateCancelContainer<TCancel>(TCancel reason)
        {
            Internal.ICancelValueContainer cancelValue;
            if (typeof(TCancel).IsValueType)
            {
                cancelValue = Internal.CancelContainer<TCancel>.GetOrCreate(reason);
            }
            else
            {
#if CSHARP_7_OR_LATER
                if (((object) reason) is Internal.ICancelationToContainer internalCancelation)
#else
                Internal.ICancelationToContainer internalCancelation = reason as Internal.ICancelationToContainer;
                if (internalCancelation != null)
#endif
                {
                    // reason is an internal cancelation object, get its container instead of wrapping it.
                    cancelValue = internalCancelation.ToContainer();
                }
                else if (reason is OperationCanceledException)
                {
                    // Use void container instead of wrapping OperationCanceledException.
                    cancelValue = Internal.CancelContainerVoid.GetOrCreate();
                }
                else
                {
                    // Only need to generate one object pool for reference types.
                    cancelValue = Internal.CancelContainer<object>.GetOrCreate(reason);
                }
            }
            return cancelValue;
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
            [System.Diagnostics.DebuggerNonUserCode]
            public sealed partial class CancelDelegate : ICancelDelegate
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private static ValueLinkedStack<ITreeHandleable> _pool;

                private Action<ReasonContainer> _onCanceled;
                private IValueContainer _valueContainer;

                private CancelDelegate() { }

                static CancelDelegate()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegate GetOrCreate(Action<ReasonContainer> onCanceled, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegate) _pool.Pop() : new CancelDelegate();
                    del._onCanceled = onCanceled;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    return del;
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue, ref ValueLinkedQueue<ITreeHandleable> cancelQueue)
                {
                    if (valueContainer.GetState() == State.Canceled)
                    {
                        valueContainer.Retain();
                        _valueContainer = valueContainer;
                        cancelQueue.Push(this);
                    }
                    else
                    {
                        Dispose();
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
                {
                    if (valueContainer.GetState() == State.Canceled)
                    {
                        valueContainer.Retain();
                        _valueContainer = valueContainer;
                        AddToCancelQueueBack(this);
                    }
                    else
                    {
                        Dispose();
                    }
                }

                void ICancelDelegate.Invoke(IValueContainer valueContainer)
                {
                    valueContainer.Retain();
                    _valueContainer = valueContainer;
                    Invoke();
                }

                void ITreeHandleable.Cancel()
                {
                    Invoke();
                }

                private void Invoke()
                {
                    SetCurrentInvoker(this);
                    var callback = _onCanceled;
                    var container = _valueContainer;
                    Dispose();
                    try
                    {
                        callback.Invoke(new ReasonContainer(container));
                    }
                    catch (Exception e)
                    {
                        AddRejectionToUnhandledStack(e, this);
                    }
                    container.Release();
                    ClearCurrentInvoker();
                }

                public void Dispose()
                {
                    _onCanceled = null;
                    _valueContainer = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public sealed partial class CancelDelegateCapture<TCapture> : ICancelDelegate
            {
                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }

                private static ValueLinkedStack<ITreeHandleable> _pool;

                private TCapture _capturedValue;
                private Action<TCapture, ReasonContainer> _onCanceled;
                private IValueContainer _valueContainer;

                private CancelDelegateCapture() { }

                static CancelDelegateCapture()
                {
                    OnClearPool += () => _pool.Clear();
                }

                public static CancelDelegateCapture<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture, ReasonContainer> onCanceled, int skipFrames)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegateCapture<TCapture>) _pool.Pop() : new CancelDelegateCapture<TCapture>();
                    del._onCanceled = onCanceled;
                    del._capturedValue = capturedValue;
                    SetCreatedStacktrace(del, skipFrames + 1);
                    return del;
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue, ref ValueLinkedQueue<ITreeHandleable> cancelQueue)
                {
                    if (valueContainer.GetState() == State.Canceled)
                    {
                        valueContainer.Retain();
                        _valueContainer = valueContainer;
                        cancelQueue.Push(this);
                    }
                    else
                    {
                        Dispose();
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(IValueContainer valueContainer)
                {
                    if (valueContainer.GetState() == State.Canceled)
                    {
                        valueContainer.Retain();
                        _valueContainer = valueContainer;
                        AddToCancelQueueBack(this);
                    }
                    else
                    {
                        Dispose();
                    }
                }

                void ICancelDelegate.Invoke(IValueContainer valueContainer)
                {
                    valueContainer.Retain();
                    _valueContainer = valueContainer;
                    Invoke();
                }

                void ITreeHandleable.Cancel()
                {
                    Invoke();
                }

                private void Invoke()
                {
                    SetCurrentInvoker(this);
                    var value = _capturedValue;
                    var callback = _onCanceled;
                    var container = _valueContainer;
                    Dispose();
                    try
                    {
                        callback.Invoke(value, new ReasonContainer(container));
                    }
                    catch (Exception e)
                    {
                        AddRejectionToUnhandledStack(e, this);
                    }
                    container.Release();
                    ClearCurrentInvoker();
                }

                public void Dispose()
                {
                    _capturedValue = default(TCapture);
                    _onCanceled = null;
                    _valueContainer = null;
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                void ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
            }

            public sealed class CancelationRef : ILinked<CancelationRef>
            {
                private struct RegisteredDelegate : IComparable<RegisteredDelegate>
                {
                    public readonly int order;
                    public readonly ICancelDelegate callback;

                    public RegisteredDelegate(int order)
                    {
                        this.order = order;
                        callback = null;
                    }

                    public RegisteredDelegate(int order, ICancelDelegate callback)
                    {
                        this.order = order;
                        this.callback = callback;
                    }

                    public int CompareTo(RegisteredDelegate other)
                    {
                        return order.CompareTo(other.order);
                    }
                }

                CancelationRef ILinked<CancelationRef>.Next { get; set; }

                private static ValueLinkedStack<CancelationRef> _pool;

                private readonly List<RegisteredDelegate> _registeredCallbacks = new List<RegisteredDelegate>();
                private int _registeredCount;
                private int _retainCount;

                public ICancelValueContainer ValueContainer { get; private set; }
                public int SourceId { get; private set; }
                public int TokenId { get; private set; }

                public bool IsCanceled { get { return ValueContainer != null; } }

                public static CancelationRef GetOrCreate()
                {
                    return _pool.IsNotEmpty ? _pool.Pop() : new CancelationRef();
                }

                public int Register(ICancelDelegate callback)
                {
                    if (ValueContainer != null)
                    {
                        callback.Invoke(ValueContainer);
                        return 0;
                    }
                    checked
                    {
                        int order = ++_registeredCount;
                        _registeredCallbacks.Add(new RegisteredDelegate(order, callback));
                        return order;
                    }
                }

                public void Unregister(int order)
                {
                    int index = _registeredCallbacks.BinarySearch(new RegisteredDelegate(order));
                    _registeredCallbacks.RemoveAt(index);
                }

                public bool IsRegistered(int order)
                {
                    int index = _registeredCallbacks.BinarySearch(new RegisteredDelegate(order));
                    return index >= 0;
                }

                public void SetCanceled()
                {
                    ValueContainer = CancelContainerVoid.GetOrCreate();
                    InvokeCallbacks();
                }

                public void SetCanceled<T>(T cancelValue)
                {
                    var container = CancelContainer<T>.GetOrCreate(cancelValue);
                    container.Retain();
                    ValueContainer = container;
                    InvokeCallbacks();
                }

                private void InvokeCallbacks()
                {
                    foreach (var del in _registeredCallbacks)
                    {
                        del.callback.Invoke(ValueContainer);
                    }
                    _registeredCallbacks.Clear();
                }

                public void Dispose()
                {
                    ++SourceId;
                    _registeredCount = 0;
                    foreach (var del in _registeredCallbacks)
                    {
                        del.callback.Dispose();
                    }
                    _registeredCallbacks.Clear();
                    if (_retainCount == 0)
                    {
                        ResetAndRepool();
                    }
                }

                private void ResetAndRepool()
                {
                    TokenId = SourceId;
                    if (ValueContainer != null)
                    {
                        ValueContainer.Release();
                        ValueContainer = null;
                    }
                    if (Config.ObjectPooling != PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                public void Retain()
                {
                    checked
                    {
                        ++_retainCount;
                    }
                }

                public void Release()
                {
                    checked
                    {
                        // If SourceId is different from TokenId, Dispose was called while this was retained.
                        if (--_retainCount == 0 & SourceId != TokenId)
                        {
                            ResetAndRepool();
                        }
                    }
                }
            }
        }

        private void ResolveDirectIfNotCanceled()
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                if (_retainCounter == 0)
                {
                    AddToHandleQueueFront(this);
                }
            }
            else
#endif
            {
                _state = State.Resolved;
                var resolveValue = Internal.ResolveContainerVoid.GetOrCreate();
                _valueOrPrevious = resolveValue;
                AddBranchesToHandleQueueBack(resolveValue);
                ResolveProgressListeners();
                AddToHandleQueueFront(this);
            }
        }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2, but this symbol is the closest Unity offers.
        private void ResolveDirectIfNotCanceled<T>(in T value)
#else
        private void ResolveDirectIfNotCanceled<T>(T value)
#endif
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                if (_retainCounter == 0)
                {
                    AddToHandleQueueFront(this);
                }
            }
            else
#endif
            {
                _state = State.Resolved;
                var resolveValue = Internal.ResolveContainer<T>.GetOrCreate(value);
                resolveValue.Retain();
                _valueOrPrevious = resolveValue;
                AddBranchesToHandleQueueBack(resolveValue);
                ResolveProgressListeners();
                AddToHandleQueueFront(this);
            }
        }

        protected void RejectDirectIfNotCanceled<TReject>(TReject reason)
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                AddRejectionToUnhandledStack(reason, null);
                if (_retainCounter == 0)
                {
                    AddToHandleQueueFront(this);
                }
            }
            else
#endif
            {
                RejectDirect(reason, 2);
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
                ResolveInternal(Internal.ResolveContainerVoid.GetOrCreate());
            }
        }

        protected void ResolveInternalIfNotCanceled<T>(T value)
        {
#if PROMISE_CANCEL
            if (_state == State.Canceled)
            {
                ReleaseInternal();
            }
            else
#endif
            {
                ResolveInternal(Internal.ResolveContainer<T>.GetOrCreate(value));
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