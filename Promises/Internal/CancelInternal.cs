#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
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
using System.Diagnostics;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        private void MakeCanceledFromToken()
        {
            // This might be called synchronously when it's registered to an already canceled token. In that case, _valueOrPrevious will be null.
            if (_valueOrPrevious == null)
            {
                _valueOrPrevious = Internal.ResolveContainerVoid.GetOrCreate();
                return;
            }

            // Otherwise, the promise is either waiting for its previous, or it's in the handle queue.
#if CSHARP_7_OR_LATER
            if (_valueOrPrevious is Promise previous)
#else
            Promise previous = _valueOrPrevious as Promise;
            if (previous != null)
#endif
            {
                // Remove this from previous' next branches.
                previous._nextBranches.Remove(this);
                _valueOrPrevious = Internal.ResolveContainerVoid.GetOrCreate();
                AddToHandleQueueBack(this);
            }
            else
            {
                // Rejection maybe wasn't caught.
                ((Internal.IValueContainer) _valueOrPrevious).ReleaseAndMaybeAddToUnhandledStack();
                _valueOrPrevious = Internal.ResolveContainerVoid.GetOrCreate();
                // Don't add to handle queue since it's already in it.
            }
        }

        private void CancelDirect()
        {
            _state = State.Canceled;
            var cancelContainer = Internal.CancelContainerVoid.GetOrCreate();
            _valueOrPrevious = cancelContainer;
            AddBranchesToHandleQueueBack(cancelContainer);
            CancelProgressListeners();
            AddToHandleQueueFront(this);
        }

        private void CancelDirect<TCancel>(ref TCancel reason)
        {
            _state = State.Canceled;
            var cancelContainer = CreateCancelContainer(ref reason);
            cancelContainer.Retain();
            _valueOrPrevious = cancelContainer;
            AddBranchesToHandleQueueBack(cancelContainer);
            CancelProgressListeners();
            AddToHandleQueueFront(this);
        }

        private static Internal.ICancelValueContainer CreateCancelContainer<TCancel>(ref TCancel reason)
        {
            Internal.ICancelValueContainer cancelValue;
            if (typeof(TCancel).IsValueType)
            {
                cancelValue = Internal.CancelContainer<TCancel>.GetOrCreate(ref reason);
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
                else if (ReferenceEquals(reason, null) || reason is OperationCanceledException)
                {
                    // Use void container instead of wrapping OperationCanceledException, or if reason is null.
                    cancelValue = Internal.CancelContainerVoid.GetOrCreate();
                }
                else
                {
                    // Only need to create one object pool for reference types.
                    object o = reason;
                    cancelValue = Internal.CancelContainer<object>.GetOrCreate(ref o);
                }
            }
            return cancelValue;
        }

        protected static CancelationRegistration RegisterForCancelation(Promise promise, CancelationToken cancelationToken)
        {
            return cancelationToken.Register(promise, (p, _) => p.MakeCanceledFromToken());
        }

        private static void ReleaseAndMaybeThrow(CancelationToken cancelationToken)
        {
            try
            {
                cancelationToken.ThrowIfCancelationRequested();
            }
            finally
            {
                if (cancelationToken.CanBeCanceled)
                {
                    cancelationToken.Release();
                }
            }
        }

        partial class Internal
        {
            [DebuggerNonUserCode]
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

                public static CancelDelegate GetOrCreate(Action<ReasonContainer> onCanceled)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegate) _pool.Pop() : new CancelDelegate();
                    del._onCanceled = onCanceled;
                    SetCreatedStacktrace(del, 2);
                    return del;
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    if (valueContainer.GetState() == State.Canceled)
                    {
                        valueContainer.Retain();
                        _valueContainer = valueContainer;
                        handleQueue.Push(this);
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
                        AddToHandleQueueBack(this);
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

                void ITreeHandleable.Handle()
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
            }

            [DebuggerNonUserCode]
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

                public static CancelDelegateCapture<TCapture> GetOrCreate(TCapture capturedValue, Action<TCapture, ReasonContainer> onCanceled)
                {
                    var del = _pool.IsNotEmpty ? (CancelDelegateCapture<TCapture>) _pool.Pop() : new CancelDelegateCapture<TCapture>();
                    del._onCanceled = onCanceled;
                    del._capturedValue = capturedValue;
                    SetCreatedStacktrace(del, 2);
                    return del;
                }

                void ITreeHandleable.MakeReady(IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    if (valueContainer.GetState() == State.Canceled)
                    {
                        valueContainer.Retain();
                        _valueContainer = valueContainer;
                        handleQueue.Push(this);
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
                        AddToHandleQueueBack(this);
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

                void ITreeHandleable.Handle()
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
            }

            [DebuggerNonUserCode]
            public sealed class CancelationRef : ILinked<CancelationRef>, ITraceable
            {
                private struct RegisteredDelegate : IComparable<RegisteredDelegate>
                {
                    public readonly ICancelDelegate callback;
                    public readonly int order;

                    public RegisteredDelegate(int order)
                    {
                        callback = null;
                        this.order = order;
                    }

                    public RegisteredDelegate(int order, ICancelDelegate callback)
                    {
                        this.callback = callback;
                        this.order = order;
                    }

                    public int CompareTo(RegisteredDelegate other)
                    {
                        return order.CompareTo(other.order);
                    }
                }

#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif

                ~CancelationRef()
                {
                    if (ValueContainer != null)
                    {
                        ValueContainer.Release();
                    }
                    if (_retainCount > 0)
                    {
                        // CancelationToken wasn't released.
                        string message = "A CancelationToken's resources were garbage collected without being released. You must release all IRetainable objects that you have retained.";
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                    }
                    if (!_isDisposed)
                    {
                        // CancelationSource wasn't disposed.
                        AddRejectionToUnhandledStack(new Exception("CancelationSource's resources were garbage collected without being disposed."), this);
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

                private bool _isDisposed;

                public static CancelationRef GetOrCreate()
                {
                    var cancelRef = _pool.IsNotEmpty ? _pool.Pop() : new CancelationRef();
                    cancelRef._isDisposed = false;
                    SetCreatedStacktrace(cancelRef, 2);
                    return cancelRef;
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

                public void SetCanceled<T>(ref T cancelValue)
                {
                    ValueContainer = CreateCancelContainer(ref cancelValue);
                    ValueContainer.Retain();
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
                    _isDisposed = true;
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
    }
}