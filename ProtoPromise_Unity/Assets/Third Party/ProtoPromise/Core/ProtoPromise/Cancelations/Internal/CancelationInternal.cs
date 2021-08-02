#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Proto.Utils;

namespace Proto.Promises
{
    internal static partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class CancelationRef : ICancelDelegate, ILinked<CancelationRef>, ITraceable
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            private struct CancelDelegateTokenVoid : IDelegateSimple
            {
                private readonly Promise.CanceledAction _callback;

                [MethodImpl(InlineOption)]
                public CancelDelegateTokenVoid(Promise.CanceledAction callback)
                {
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer)
                {
                    _callback.Invoke(new ReasonContainer(valueContainer));
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            private struct CancelDelegateToken<TCapture> : IDelegateSimple
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.CanceledAction<TCapture> _callback;

                [MethodImpl(InlineOption)]
                public CancelDelegateToken(ref TCapture capturedValue, Promise.CanceledAction<TCapture> callback)
                {
                    _capturedValue = capturedValue;
                    _callback = callback;
                }

                [MethodImpl(InlineOption)]
                public void Invoke(IValueContainer valueContainer)
                {
                    _callback.Invoke(_capturedValue, new ReasonContainer(valueContainer));
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            private sealed class CancelDelegate<TCanceler> : ICancelDelegate, ITraceable, ILinked<CancelDelegate<TCanceler>>
                where TCanceler : IDelegateSimple
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
                CancelDelegate<TCanceler> ILinked<CancelDelegate<TCanceler>>.Next { get; set; }

                private TCanceler _canceler;

                private CancelDelegate() { }

                [MethodImpl(InlineOption)]
                public static CancelDelegate<TCanceler> GetOrCreate(TCanceler canceler)
                {
                    var del = ObjectPool<CancelDelegate<TCanceler>>.TryTake<CancelDelegate<TCanceler>>()
                        ?? new CancelDelegate<TCanceler>();
                    del._canceler = canceler;
                    SetCreatedStacktrace(del, 2);
                    return del;
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    SetCurrentInvoker(this);
                    var canceler = _canceler;
                    Dispose();
                    try
                    {
                        // Canceler may dispose this.
                        canceler.Invoke(valueContainer);
                    }
                    finally
                    {
                        ClearCurrentInvoker();
                    }
                }

                [MethodImpl(InlineOption)]
                private void Dispose()
                {
                    _canceler = default(TCanceler);
                    ObjectPool<CancelDelegate<TCanceler>>.MaybeRepool(this);
                }

                void ICancelDelegate.Dispose()
                {
                    ThrowIfInPool(this);
                    Dispose();
                }
            }

            private struct RegisteredDelegate : IComparable<RegisteredDelegate>
            {
                public readonly ICancelDelegate callback;
                public readonly uint order;

                public RegisteredDelegate(uint order, ICancelDelegate callback)
                {
                    this.callback = callback;
                    this.order = order;
                }

                public RegisteredDelegate(uint order) : this(order, null) { }

                public int CompareTo(RegisteredDelegate other)
                {
                    return order.CompareTo(other.order);
                }
            }

            // Used as a reference holder for _valueContainer for thread safety purposes and to let the finalizer know that the source was disposed.
            private class DisposedRef : ICancelValueContainer
            {
                internal static readonly DisposedRef instance = new DisposedRef();

                private DisposedRef() { }

                void IValueContainer.Retain() { }
                void IValueContainer.Release() { }

                Type IValueContainer.ValueType { get { throw new System.InvalidOperationException(); } }
                object IValueContainer.Value { get { throw new System.InvalidOperationException(); } }
                Exception IThrowable.GetException() { throw new System.InvalidOperationException(); }
                Promise.State IValueContainer.GetState() { throw new System.InvalidOperationException(); }
                void IValueContainer.ReleaseAndAddToUnhandledStack() { throw new System.InvalidOperationException(); }
                void IValueContainer.ReleaseAndMaybeAddToUnhandledStack(bool shouldAdd) { throw new System.InvalidOperationException(); }

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
                if (_retainCounter > 0)
                {
                    // CancelationToken wasn't released.
                    string message = "A CancelationToken's resources were garbage collected without being released. You must release all IRetainable objects that you have retained.";
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                }
                if (_valueContainer != DisposedRef.instance)
                {
                    // CancelationSource wasn't disposed.
                    AddRejectionToUnhandledStack(new Exception("CancelationSource's resources were garbage collected without being disposed."), this);
                }
            }

            CancelationRef ILinked<CancelationRef>.Next { get; set; }

            // TODO: replace lock(_registeredCallbacks) with Monitor.TryEnter() in abortable loop.
            // TODO: create a custom SortedDictionary with pooled nodes instead.
            private readonly List<RegisteredDelegate> _registeredCallbacks = new List<RegisteredDelegate>();
            private ValueLinkedStackZeroGC<CancelationRegistration> _links;
            volatile private ICancelValueContainer _valueContainer;
            private int _retainCounter;
            private uint _registeredCount;
            volatile private int _sourceId;
            volatile private int _tokenId;

            internal ICancelValueContainer ValueContainer
            {
                [MethodImpl(InlineOption)]
                get { return _valueContainer; }
            }
            internal int SourceId
            {
                [MethodImpl(InlineOption)]
                get { return _sourceId; }
            }
            internal int TokenId
            {
                [MethodImpl(InlineOption)]
                get { return _tokenId; }
            }

            internal static CancelationRef GetOrCreate()
            {
                var cancelRef = ObjectPool<CancelationRef>.TryTake<CancelationRef>()
                    ?? new CancelationRef();
                // Left 16 bits are for internal retains.
                cancelRef._retainCounter = 1 << 16;
                cancelRef._valueContainer = null;
                SetCreatedStacktrace(cancelRef, 2);
                return cancelRef;
            }

            [MethodImpl(InlineOption)]
            internal bool IsSourceCanceled(int sourceId)
            {
                var temp = _valueContainer;
                return sourceId == _sourceId & temp != null & temp != DisposedRef.instance;
            }

            [MethodImpl(InlineOption)]
            internal bool IsTokenCanceled(int tokenId)
            {
                var temp = _valueContainer;
                return tokenId == _tokenId & temp != null & temp != DisposedRef.instance;
            }

            [MethodImpl(InlineOption)]
            internal void ThrowIfCanceled(int tokenId)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    return;
                }
                try
                {
                    var temp = _valueContainer;
                    if (temp != null & temp != DisposedRef.instance)
                    {
                        throw temp.GetException();
                    }
                }
                finally
                {
                    ReleaseAfterRetainInternal();
                }
            }

            [MethodImpl(InlineOption)]
            internal bool TryGetCanceledType(int tokenId, out Type type)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    type = null;
                    return false;
                }
                var temp = _valueContainer;
                bool isCanceled = temp != null & temp != DisposedRef.instance;
                type = isCanceled ? temp.ValueType : null;
                ReleaseAfterRetainInternal();
                return isCanceled;
            }

            [MethodImpl(InlineOption)]
            internal bool TryGetCanceledValue(int tokenId, out object value)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    value = null;
                    return false;
                }
                var temp = _valueContainer;
                bool isCanceled = temp != null & temp != DisposedRef.instance;
                value = isCanceled ? temp.Value : null;
                ReleaseAfterRetainInternal();
                return isCanceled;
            }

            [MethodImpl(InlineOption)]
            internal bool TryGetCanceledValueAs<T>(int tokenId, out bool didConvert, out T value)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    value = default(T);
                    return didConvert = false;
                }
                var temp = _valueContainer;
                if (temp == null | temp == DisposedRef.instance)
                {
                    value = default(T);
                    ReleaseAfterRetainInternal();
                    return didConvert = false;
                }
                didConvert = TryConvert(ValueContainer, out value);
                ReleaseAfterRetainInternal();
                return true;
            }

            [MethodImpl(InlineOption)]
            internal void MaybeAddLinkedCancelation(CancelationRef listener, int tokenId)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    return;
                }
                var temp = _valueContainer;
                if (temp != null)
                {
                    goto MaybeInvokeAndReturn;
                }
                lock (listener._registeredCallbacks)
                {
                    if (listener._valueContainer != null) // Make sure listener wasn't canceled from another token on another thread.
                    {
                        goto Return;
                    }
                    uint order;
                    lock (_registeredCallbacks)
                    {
                        temp = _valueContainer;
                        if (temp != null) // Double-checked locking! In this case it works because we're not writing back to the field.
                        {
                            goto MaybeInvokeAndReturn;
                        }
                        checked
                        {
                            order = ++_registeredCount;
                        }
                        listener.InterlockedRetainInternal();
                        _registeredCallbacks.Add(new RegisteredDelegate(order, listener));
                    }
                    listener._links.Push(new CancelationRegistration(this, _tokenId, order));
                }
                goto Return;

            MaybeInvokeAndReturn:
                if (temp != DisposedRef.instance)
                {
                    listener.TryInvokeCallbacks(temp);
                }
            Return:
                ReleaseAfterRetainInternal();
            }

            internal bool TryRegister(ICancelDelegate callback, out CancelationRegistration registration)
            {
                uint order;
                ICancelValueContainer temp;
                lock (_registeredCallbacks)
                {
                    temp = _valueContainer;
                    if (temp != null)
                    {
                        goto MaybeInvoke;
                    }
                    checked
                    {
                        order = ++_registeredCount;
                    }
                    _registeredCallbacks.Add(new RegisteredDelegate(order, callback));
                }
                registration = new CancelationRegistration(this, _tokenId, order);
                return true;

            MaybeInvoke:
                if (temp != DisposedRef.instance)
                {
                    registration = new CancelationRegistration(this, _tokenId, 0);
                    callback.Invoke(temp);
                    return true;
                }
                registration = default(CancelationRegistration);
                return false;
            }

            [MethodImpl(InlineOption)]
            internal bool TryRegister(Promise.CanceledAction callback, int tokenId, out CancelationRegistration registration)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    registration = default(CancelationRegistration);
                    return false;
                }
                try
                {
                    var temp = _valueContainer;
                    if (temp != null)
                    {
                        if (temp != DisposedRef.instance)
                        {
                            registration = new CancelationRegistration(this, _tokenId, 0);
                            callback.Invoke(new ReasonContainer(temp));
                            return true;
                        }
                        registration = default(CancelationRegistration);
                        return false;
                    }
                    var cancelDelegate = CancelDelegate<CancelDelegateTokenVoid>.GetOrCreate(new CancelDelegateTokenVoid(callback));
                    return TryRegister(cancelDelegate, out registration);
                }
                finally
                {
                    ReleaseAfterRetainInternal();
                }
            }

            [MethodImpl(InlineOption)]
            internal bool TryRegister<TCapture>(ref TCapture capturedValue, Promise.CanceledAction<TCapture> callback, int tokenId, out CancelationRegistration registration)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    registration = default(CancelationRegistration);
                    return false;
                }
                try
                {
                    var temp = _valueContainer;
                    if (temp != null)
                    {
                        if (temp != DisposedRef.instance)
                        {
                            registration = new CancelationRegistration(this, _tokenId, 0);
                            callback.Invoke(capturedValue, new ReasonContainer(temp));
                            return true;
                        }
                        registration = default(CancelationRegistration);
                        return false;
                    }
                    var cancelDelegate = CancelDelegate<CancelDelegateToken<TCapture>>.GetOrCreate(new CancelDelegateToken<TCapture>(ref capturedValue, callback));
                    return TryRegister(cancelDelegate, out registration);
                }
                finally
                {
                    ReleaseAfterRetainInternal();
                }
            }

            [MethodImpl(InlineOption)]
            internal bool IsRegistered(int tokenId, uint order, out bool isCanceled)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    return isCanceled = false;
                }
                bool validOrder;
                var temp = _valueContainer;
                if (temp != null)
                {
                    isCanceled = temp != DisposedRef.instance;
                    validOrder = false;
                }
                else
                {
                    lock (_registeredCallbacks)
                    {
                        temp = _valueContainer;
                        isCanceled = temp != null;
                        validOrder = !isCanceled && IndexOf(order) >= 0;
                    }
                    isCanceled &= temp != DisposedRef.instance;
                }
                ReleaseAfterRetainInternal();
                return validOrder;
            }

            [MethodImpl(InlineOption)]
            internal bool TryUnregister(int tokenId, uint order, out bool isCanceled)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    return isCanceled = false;
                }
                bool unregistered = false;
                ICancelDelegate del;
                lock (_registeredCallbacks)
                {
                    var temp = _valueContainer;
                    if (temp != null)
                    {
                        isCanceled = temp != DisposedRef.instance;
                        goto ReleaseAndReturn;
                    }
                    isCanceled = false;
                    int index = IndexOf(order);
                    if (index < 0)
                    {
                        goto ReleaseAndReturn;
                    }
                    del = _registeredCallbacks[index].callback;
                    _registeredCallbacks.RemoveAt(index);
                }
                del.Dispose();
                unregistered = true;
            ReleaseAndReturn:
                ReleaseAfterRetainInternal();
                return unregistered;
            }

            [MethodImpl(InlineOption)]
            private int IndexOf(uint order)
            {
                return _registeredCallbacks.BinarySearch(new RegisteredDelegate(order));
            }

            [MethodImpl(InlineOption)]
            internal bool TrySetCanceled(int sourceId, int tokenId)
            {
                // Retain for thread safety and recursive calls.
                if (!TryRetainInternal(tokenId))
                {
                    return false;
                }
                try
                {
                    return (sourceId == _sourceId & _valueContainer == null) && TryInvokeCallbacks(CancelContainerVoid.GetOrCreate());
                }
                finally
                {
                    ReleaseAfterRetainInternal();
                }
            }

            [MethodImpl(InlineOption)]
            internal bool TrySetCanceled<T>(ref T cancelValue, int sourceId, int tokenId)
            {
                // Retain for thread safety and recursive calls.
                if (!TryRetainInternal(tokenId))
                {
                    return false;
                }
                try
                {
                    return (sourceId == _sourceId & _valueContainer == null) && TryInvokeCallbacks(CreateCancelContainer(ref cancelValue));
                }
                finally
                {
                    ReleaseAfterRetainInternal();
                }
            }

            private bool TryInvokeCallbacks(ICancelValueContainer valueContainer)
            {
                valueContainer.Retain();
                if (Interlocked.CompareExchange(ref _valueContainer, valueContainer, null) != null)
                {
                    valueContainer.Release();
                    return false;
                }
                // Wait for a callback currently being added/removed in another thread.
                // When other threads enter the lock, they will see the _valueContainer was already set, so we don't need any further callback synchronization.
                lock (_registeredCallbacks) { }
                Unlink();
                List<Exception> exceptions = null;
                for (int i = 0, max = _registeredCallbacks.Count; i < max; ++i)
                {
                    try
                    {
                        _registeredCallbacks[i].callback.Invoke(valueContainer);
                    }
                    catch (Exception e)
                    {
                        if (exceptions == null)
                        {
                            exceptions = new List<Exception>();
                        }
                        exceptions.Add(e);
                    }
                }
                _registeredCallbacks.Clear();
                if (exceptions != null)
                {
                    // Propagate exceptions to caller as aggregate.
                    throw new AggregateException(exceptions);
                }
                return true;
            }

            [MethodImpl(InlineOption)]
            internal bool TryDispose(int sourceId)
            {
                if (Interlocked.CompareExchange(ref _sourceId, sourceId + 1, sourceId) != sourceId)
                {
                    return false;
                }
                ThrowIfInPool(this);
                // In case Dispose is called concurrently with Cancel.
                if (Interlocked.CompareExchange(ref _valueContainer, DisposedRef.instance, null) == null)
                {
                    // Wait for a callback currently being added/removed in another thread.
                    // When other threads enter the lock, they will see the _valueContainer was already set, so we don't need any further callback synchronization.
                    lock (_registeredCallbacks) { }
                    Unlink();
                    // No need to lock on _registeredCallbacks since it won't be modified after WaitForCallbacks().
                    for (int i = 0, max = _registeredCallbacks.Count; i < max; ++i)
                    {
                        _registeredCallbacks[i].callback.Dispose();
                    }
                    _registeredCallbacks.Clear();
                }
                _registeredCount = 0;
                ReleaseAfterRetainInternal();
                return true;
            }

            private void Unlink()
            {
                while (_links.IsNotEmpty)
                {
                    _links.Pop().TryUnregister();
                }
            }

            [MethodImpl(InlineOption)]
            internal bool TryRetain(int tokenId)
            {
                int oldRetain;
                if (InterlockedTryRetain(tokenId, out oldRetain))
                {
                    ThrowIfInPool(this);
                    return true;
                }
                else if (oldRetain > 0)
                {
                    throw new OverflowException();
                }
                return false;
            }

            [MethodImpl(InlineOption)]
            internal bool TryRelease(int tokenId)
            {
                int newRetain;
                bool didRelease = InterlockedTryRelease(tokenId, out newRetain);
                if (didRelease)
                {
                    ThrowIfInPool(this);
                }
                if (didRelease & newRetain == 0)
                {
                    ResetAndRepool();
                }
                return didRelease;
            }

            [MethodImpl(InlineOption)]
            internal bool TryRetainInternal(int tokenId)
            {
                if (InterlockedTryRetainInternal(tokenId))
                {
                    ThrowIfInPool(this);
                    return true;
                }
                return false;
            }

            internal void ReleaseAfterRetainInternal()
            {
                if (InterlockedReleaseInternal() == 0)
                {
                    ResetAndRepool();
                }
            }

            private void ResetAndRepool()
            {
                _tokenId = _sourceId;
                var oldContainer = Interlocked.Exchange(ref _valueContainer, DisposedRef.instance);
                if (oldContainer != null)
                {
                    oldContainer.Release();
                }
                ObjectPool<CancelationRef>.MaybeRepool(this);
            }

            void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
            {
                ThrowIfInPool(this);
                try
                {
                    TryInvokeCallbacks(valueContainer);
                }
                finally
                {
                    ReleaseAfterRetainInternal();
                }
            }

            void ICancelDelegate.Dispose()
            {
                ThrowIfInPool(this);
                ReleaseAfterRetainInternal();
            }

            // These helpers provide a thread-safe mechanism for checking if the user called Release too many times.
            // Left 16 bits are for internal retains, right 16 bits are for user retains.
            [MethodImpl(InlineOption)]
            private bool InterlockedTryRetain(int tokenId, out int initialValue)
            {
                // Right 16 bits are for user retains.
                int newValue;
                do
                {
                    initialValue = _retainCounter;
                    newValue = initialValue + 1;
                    int oldCount = initialValue & ushort.MaxValue;
                    // Make sure user retain doesn't encroach on dispose retain.
                    // Checking for zero also handles the case if this is called concurrently with TryDispose and/or InvokeCallbacks.
                    if (tokenId != _tokenId | oldCount >= ushort.MaxValue | initialValue == 0) return false;
                }
                while (Interlocked.CompareExchange(ref _retainCounter, newValue, initialValue) != initialValue);
                return true;
            }

            [MethodImpl(InlineOption)]
            private bool InterlockedTryRelease(int tokenId, out int newValue)
            {
                // Right 16 bits are for user retains.
                int initialValue;
                do
                {
                    initialValue = _retainCounter;
                    newValue = initialValue - 1;
                    int oldCount = initialValue & ushort.MaxValue;
                    if (tokenId != _tokenId | oldCount <= 0) return false;
                }
                while (Interlocked.CompareExchange(ref _retainCounter, newValue, initialValue) != initialValue);
                return true;
            }

            [MethodImpl(InlineOption)]
            private bool InterlockedTryRetainInternal(int tokenId)
            {
                // Left 16 bits are for internal retains.
                int initialValue, newValue;
                do
                {
                    initialValue = _retainCounter;
                    newValue = initialValue + (1 << 16);
                    // Checking for zero handles the case if this is called concurrently with TryDispose and/or Release.
                    if (tokenId != _tokenId | initialValue == 0) return false;
                }
                while (Interlocked.CompareExchange(ref _retainCounter, newValue, initialValue) != initialValue);
                return true;
            }

            [MethodImpl(InlineOption)]
            private void InterlockedRetainInternal()
            {
                // Left 16 bits are for internal retains.
                int addValue = 1 << 16;
                Interlocked.Add(ref _retainCounter, addValue);
            }

            [MethodImpl(InlineOption)]
            private int InterlockedReleaseInternal()
            {
                // Left 16 bits are for internal retains.
                int addValue = 1 << 16;
                return Interlocked.Add(ref _retainCounter, -addValue);
            }
        }
    }
}