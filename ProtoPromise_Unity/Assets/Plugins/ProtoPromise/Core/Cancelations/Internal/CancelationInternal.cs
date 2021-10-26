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
using System.Runtime.InteropServices;
using System.Threading;

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
                internal CancelDelegateTokenVoid(Promise.CanceledAction callback)
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
            [DebuggerNonUserCode]
#endif
            private struct CancelDelegateToken<TCapture> : IDelegateSimple
            {
                private readonly TCapture _capturedValue;
                private readonly Promise.CanceledAction<TCapture> _callback;

                [MethodImpl(InlineOption)]
                internal CancelDelegateToken(ref TCapture capturedValue, Promise.CanceledAction<TCapture> callback)
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
                internal static CancelDelegate<TCanceler> GetOrCreate(TCanceler canceler)
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
                internal readonly ICancelDelegate callback;
                internal readonly uint order;

                [MethodImpl(InlineOption)]
                internal RegisteredDelegate(uint order, ICancelDelegate callback)
                {
                    this.callback = callback;
                    this.order = order;
                }

                [MethodImpl(InlineOption)]
                internal RegisteredDelegate(uint order) : this(order, null) { }

                [MethodImpl(InlineOption)]
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

            [StructLayout(LayoutKind.Explicit)]
            private struct IdsAndRetains
            {
                [FieldOffset(0)]
                internal volatile short _tokenId;
                [FieldOffset(2)]
                internal volatile short _sourceId;
                // internal retains and user retains are separated so that user retains can be validated without interfering with internal retains.
                [FieldOffset(4)]
                private ushort _internalRetains;
                [FieldOffset(6)]
                internal ushort _userRetains;
                // internal and user retains are combined to know when the ref can be repooled (FieldOffset is free vs adding them together).
                [FieldOffset(4)]
#pragma warning disable IDE0044 // Add readonly modifier
                private uint _totalRetains;
#pragma warning restore IDE0044 // Add readonly modifier
                [FieldOffset(0)]
                private long _longValue; // For interlocked

                internal IdsAndRetains(short initialId)
                {
                    _longValue = 0;
                    _internalRetains = 0;
                    _userRetains = 0;
                    _totalRetains = 0;
                    _tokenId = initialId;
                    _sourceId = initialId;
                }

                [MethodImpl(InlineOption)]
                internal void SetInternalRetain(ushort internalRetains)
                {
                    _internalRetains = internalRetains;
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedRetainInternal()
                {
                    Thread.MemoryBarrier();
                    IdsAndRetains initialValue = default(IdsAndRetains), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        if (initialValue._internalRetains == ushort.MaxValue)
                        {
                            throw new OverflowException();
                        }
#endif
                        newValue = initialValue;
                        unchecked
                        {
                            ++newValue._internalRetains;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedReleaseInternal(out bool fullyReleased)
                {
                    Thread.MemoryBarrier();
                    IdsAndRetains initialValue = default(IdsAndRetains), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        if (initialValue._internalRetains == 0)
                        {
                            throw new OverflowException();
                        }
#endif
                        newValue = initialValue;
                        unchecked
                        {
                            --newValue._internalRetains;
                            fullyReleased = newValue._totalRetains == 0;
                            if (fullyReleased)
                            {
                                ++newValue._tokenId;
                            }
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                }

                internal bool InterlockedTryRetainInternal(short tokenId)
                {
                    Thread.MemoryBarrier();
                    IdsAndRetains initialValue = default(IdsAndRetains), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        if (initialValue._tokenId != tokenId)
                        {
                            return false;
                        }
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        if (initialValue._internalRetains == ushort.MaxValue)
                        {
                            throw new OverflowException();
                        }
#endif
                        newValue = initialValue;
                        unchecked
                        {
                            ++newValue._internalRetains;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                internal bool InterlockedTryRetainInternalFromSource(short sourceId)
                {
                    Thread.MemoryBarrier();
                    IdsAndRetains initialValue = default(IdsAndRetains), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        if (initialValue._sourceId != sourceId)
                        {
                            return false;
                        }
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        if (initialValue._internalRetains == ushort.MaxValue)
                        {
                            throw new OverflowException();
                        }
#endif
                        newValue = initialValue;
                        unchecked
                        {
                            ++newValue._internalRetains;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryIncrementSource(short sourceId)
                {
                    Thread.MemoryBarrier();
                    IdsAndRetains initialValue = default(IdsAndRetains), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        if (initialValue._sourceId != sourceId)
                        {
                            return false;
                        }
                        newValue = initialValue;
                        unchecked
                        {
                            ++newValue._sourceId;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryRetainUser(short tokenId)
                {
                    Thread.MemoryBarrier();
                    IdsAndRetains initialValue = default(IdsAndRetains), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        if (initialValue._tokenId != tokenId)
                        {
                            return false;
                        }
                        if (initialValue._userRetains == ushort.MaxValue)
                        {
                            throw new OverflowException();
                        }
                        newValue = initialValue;
                        unchecked
                        {
                            ++newValue._userRetains;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryReleaseUser(short tokenId, out bool fullyReleased)
                {
                    Thread.MemoryBarrier();
                    IdsAndRetains initialValue = default(IdsAndRetains), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        if (initialValue._tokenId != tokenId)
                        {
                            return fullyReleased = false;
                        }
                        if (initialValue._userRetains == 0)
                        {
                            throw new OverflowException();
                        }
                        newValue = initialValue;
                        unchecked
                        {
                            --newValue._userRetains;
                            fullyReleased = newValue._totalRetains == 0;
                            if (fullyReleased)
                            {
                                ++newValue._tokenId;
                            }
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
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
                if (_idsAndRetains._userRetains > 0)
                {
                    // CancelationToken wasn't released.
                    string message = "A CancelationToken's resources were garbage collected without being released. You must release all IRetainable objects that you have retained.";
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                }
                if (_valueContainer != DisposedRef.instance)
                {
                    // CancelationSource wasn't disposed.
                    AddRejectionToUnhandledStack(new UnreleasedObjectException("CancelationSource's resources were garbage collected without being disposed."), this);
                }
            }

            CancelationRef ILinked<CancelationRef>.Next { get; set; }

            // TODO: replace lock(_registeredCallbacks) with Monitor.TryEnter() in abortable loop.
            // TODO: create a custom SortedDictionary with pooled nodes instead.
            private readonly List<RegisteredDelegate> _registeredCallbacks = new List<RegisteredDelegate>();
            private ValueLinkedStackZeroGC<CancelationRegistration> _links = ValueLinkedStackZeroGC<CancelationRegistration>.Create();
            volatile private ICancelValueContainer _valueContainer;
            private uint _registeredCount;
            private IdsAndRetains _idsAndRetains = new IdsAndRetains(1); // Start with Id 1 instead of 0 to reduce risk of false positives.

            internal ICancelValueContainer ValueContainer
            {
                [MethodImpl(InlineOption)]
                get { return _valueContainer; }
            }
            internal short SourceId
            {
                [MethodImpl(InlineOption)]
                get { return _idsAndRetains._sourceId; }
            }
            internal short TokenId
            {
                [MethodImpl(InlineOption)]
                get { return _idsAndRetains._tokenId; }
            }

            internal static CancelationRef GetOrCreate()
            {
                var cancelRef = ObjectPool<CancelationRef>.TryTake<CancelationRef>()
                    ?? new CancelationRef();
                cancelRef._idsAndRetains.SetInternalRetain(1); // 1 retain for Dispose.
                cancelRef._valueContainer = null;
                SetCreatedStacktrace(cancelRef, 2);
                return cancelRef;
            }

            [MethodImpl(InlineOption)]
            internal bool IsSourceCanceled(short sourceId)
            {
                var temp = _valueContainer;
                return sourceId == SourceId & temp != null & temp != DisposedRef.instance;
            }

            [MethodImpl(InlineOption)]
            internal bool IsTokenCanceled(short tokenId)
            {
                var temp = _valueContainer;
                return tokenId == TokenId & temp != null & temp != DisposedRef.instance;
            }

            [MethodImpl(InlineOption)]
            internal void ThrowIfCanceled(short tokenId)
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
            internal bool TryGetCanceledType(short tokenId, out Type type)
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
            internal bool TryGetCanceledValue(short tokenId, out object value)
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
            internal bool TryGetCanceledValueAs<T>(short tokenId, out bool didConvert, out T value)
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
                didConvert = TryGetValue(ValueContainer, out value);
                ReleaseAfterRetainInternal();
                return true;
            }

            [MethodImpl(InlineOption)]
            internal void MaybeAddLinkedCancelation(CancelationRef listener, short tokenId)
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
                        listener._idsAndRetains.InterlockedRetainInternal();
                        _registeredCallbacks.Add(new RegisteredDelegate(order, listener));
                    }
                    listener._links.Push(new CancelationRegistration(this, TokenId, order));
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
                registration = new CancelationRegistration(this, TokenId, order);
                return true;

            MaybeInvoke:
                if (temp != DisposedRef.instance)
                {
                    registration = new CancelationRegistration(this, TokenId, 0);
                    callback.Invoke(temp);
                    return true;
                }
                registration = default(CancelationRegistration);
                return false;
            }

            [MethodImpl(InlineOption)]
            internal bool TryRegister(Promise.CanceledAction callback, short tokenId, out CancelationRegistration registration)
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
                            registration = new CancelationRegistration(this, TokenId, 0);
                            callback.Invoke(new ReasonContainer(temp, InvokeId));
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
            internal bool TryRegister<TCapture>(ref TCapture capturedValue, Promise.CanceledAction<TCapture> callback, short tokenId, out CancelationRegistration registration)
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
                            registration = new CancelationRegistration(this, TokenId, 0);
                            callback.Invoke(capturedValue, new ReasonContainer(temp, InvokeId));
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
            internal bool IsRegistered(short tokenId, uint order, out bool isCanceled)
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
            internal bool TryUnregister(short tokenId, uint order, out bool isCanceled)
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
            internal bool TrySetCanceled(short sourceId)
            {
                // Retain for thread safety and recursive calls.
                if (!TryRetainInternal(sourceId))
                {
                    return false;
                }
                try
                {
                    return _valueContainer == null && TryInvokeCallbacks(CancelContainerVoid.GetOrCreate());
                }
                finally
                {
                    ReleaseAfterRetainInternal();
                }
            }

            [MethodImpl(InlineOption)]
            internal bool TrySetCanceled<T>(ref T cancelValue, short sourceId)
            {
                // Retain for thread safety and recursive calls.
                if (!TryRetainInternal(sourceId))
                {
                    return false;
                }
                try
                {
                    return _valueContainer == null && TryInvokeCallbacks(CreateCancelContainer(ref cancelValue));
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
            internal bool TryDispose(short sourceId)
            {
                if (!_idsAndRetains.InterlockedTryIncrementSource(sourceId))
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
            internal bool TryRetainUser(short tokenId)
            {
                if (_idsAndRetains.InterlockedTryRetainUser(tokenId))
                {
                    ThrowIfInPool(this);
                    return true;
                }
                return false;
            }

            [MethodImpl(InlineOption)]
            internal bool TryReleaseUser(short tokenId)
            {
                bool fullyReleased;
                bool didRelease = _idsAndRetains.InterlockedTryReleaseUser(tokenId, out fullyReleased);
                MaybeResetAndRepool(fullyReleased);
                return didRelease;
            }

            [MethodImpl(InlineOption)]
            internal bool TryRetainInternal(short tokenId)
            {
                if (_idsAndRetains.InterlockedTryRetainInternal(tokenId))
                {
                    ThrowIfInPool(this);
                    return true;
                }
                return false;
            }

            [MethodImpl(InlineOption)]
            internal bool TryRetainInternalFromSource(short sourceId)
            {
                if (_idsAndRetains.InterlockedTryRetainInternalFromSource(sourceId))
                {
                    ThrowIfInPool(this);
                    return true;
                }
                return false;
            }

            internal void ReleaseAfterRetainInternal()
            {
                bool fullyReleased;
                _idsAndRetains.InterlockedReleaseInternal(out fullyReleased);
                MaybeResetAndRepool(fullyReleased);
            }

            private void MaybeResetAndRepool(bool fullyReleased)
            {
                if (fullyReleased)
                {
                    ResetAndRepool();
                }
            }

            [MethodImpl(InlineOption)]
            private void ResetAndRepool()
            {
                ThrowIfInPool(this);
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
        }
    }
}