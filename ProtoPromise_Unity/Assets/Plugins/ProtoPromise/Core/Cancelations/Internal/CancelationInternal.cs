#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

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
        internal abstract class CancelableBase
        {
            internal abstract void Invoke();
            internal abstract void Dispose();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal struct CancelDelegateTokenVoid : ICancelable
        {
            private readonly Action _callback;

            [MethodImpl(InlineOption)]
            internal CancelDelegateTokenVoid(Action callback)
            {
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Cancel()
            {
                _callback.Invoke();
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal struct CancelDelegateToken<TCapture> : ICancelable
        {
            private readonly TCapture _capturedValue;
            private readonly Action<TCapture> _callback;

            [MethodImpl(InlineOption)]
            internal CancelDelegateToken(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCapture capturedValue, Action<TCapture> callback)
            {
                _capturedValue = capturedValue;
                _callback = callback;
            }

            [MethodImpl(InlineOption)]
            public void Cancel()
            {
                _callback.Invoke(_capturedValue);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal sealed class CancelationRef : CancelableBase, ILinked<CancelationRef>, ITraceable
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            private sealed class CancelableWrappe<TCancelable> : CancelableBase, ITraceable, ILinked<CancelableWrappe<TCancelable>>
                where TCancelable : ICancelable
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
                CancelableWrappe<TCancelable> ILinked<CancelableWrappe<TCancelable>>.Next { get; set; }

                private TCancelable _cancelable;

                private CancelableWrappe() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                volatile private bool _disposed;

                ~CancelableWrappe()
                {
                    try
                    {
                        if (!_disposed)
                        {
                            // For debugging. This should never happen.
                            string message = "A " + GetType() + " was garbage collected without it being disposed.";
                            AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        AddRejectionToUnhandledStack(e, this);
                    }
                }
#endif

                [MethodImpl(InlineOption)]
                internal static CancelableWrappe<TCancelable> GetOrCreate(TCancelable cancelable)
                {
                    var del = ObjectPool<CancelableWrappe<TCancelable>>.TryTake<CancelableWrappe<TCancelable>>()
                        ?? new CancelableWrappe<TCancelable>();
                    del._cancelable = cancelable;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    del._disposed = false;
#endif
                    SetCreatedStacktrace(del, 2);
                    return del;
                }

                internal override void Invoke()
                {
                    ThrowIfInPool(this);
                    var canceler = _cancelable;
#if PROMISE_DEBUG
                    SetCurrentInvoker(this);
                    try
                    {
                        canceler.Cancel();
                    }
                    finally
                    {
                        ClearCurrentInvoker();
                        Dispose();
                    }
#else
                    Dispose();
                    canceler.Cancel();
#endif
                }

                [MethodImpl(InlineOption)]
                internal override void Dispose()
                {
                    ThrowIfInPool(this);
                    _cancelable = default(TCancelable);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
                    ObjectPool<CancelableWrappe<TCancelable>>.MaybeRepool(this);
                }
            }

            private struct RegisteredDelegate : IComparable<RegisteredDelegate>
            {
                internal readonly CancelableBase cancelable;
                internal readonly uint order;

                [MethodImpl(InlineOption)]
                internal RegisteredDelegate(uint order, CancelableBase cancelable)
                {
                    this.cancelable = cancelable;
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
                    this = default(IdsAndRetains);
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
            } // IdsAndRetains

            private enum State : int
            {
                Pending,
                Canceled,
                Disposed
            }

#if PROMISE_DEBUG
            CausalityTrace ITraceable.Trace { get; set; }
#endif

            ~CancelationRef()
            {
                try
                {
                    if (_idsAndRetains._userRetains > 0)
                    {
                        // CancelationToken wasn't released.
                        string message = "A CancelationToken's resources were garbage collected without being released. You must release all IRetainable objects that you have retained.";
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                    }
                    if (_state != (int) State.Disposed)
                    {
                        // CancelationSource wasn't disposed.
                        AddRejectionToUnhandledStack(new UnreleasedObjectException("CancelationSource's resources were garbage collected without being disposed."), this);
                    }
                }
                catch (Exception e)
                {
                    // This should never happen.
                    AddRejectionToUnhandledStack(e, this);
                }
            }

            CancelationRef ILinked<CancelationRef>.Next { get; set; }

            // TODO: replace List with a double-linked list with the CancelationRegistration storing the node directly for O(1) find and removal.
            private readonly List<RegisteredDelegate> _registeredCallbacks = new List<RegisteredDelegate>();
            private ValueLinkedStackZeroGC<CancelationRegistration> _links = ValueLinkedStackZeroGC<CancelationRegistration>.Create();
            volatile private int _state; // State as int for Interlocked.
            private uint _registeredCount;
            private IdsAndRetains _idsAndRetains = new IdsAndRetains(1); // Start with Id 1 instead of 0 to reduce risk of false positives.

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
                cancelRef._state = (int) State.Pending;
                SetCreatedStacktrace(cancelRef, 2);
                return cancelRef;
            }

            [MethodImpl(InlineOption)]
            internal static bool IsValidSource(CancelationRef _this, short sourceId)
            {
                return _this != null && _this.SourceId == sourceId;
            }

            [MethodImpl(InlineOption)]
            internal static bool IsSourceCanceled(CancelationRef _this, short sourceId)
            {
                return _this != null && _this.IsSourceCanceled(sourceId);
            }

            [MethodImpl(InlineOption)]
            private bool IsSourceCanceled(short sourceId)
            {
                return sourceId == SourceId & _state == (int) State.Canceled;
            }

            [MethodImpl(InlineOption)]
            internal static bool CanTokenBeCanceled(CancelationRef _this, short tokenId)
            {
                return _this != null && _this.TokenId == tokenId;
            }

            [MethodImpl(InlineOption)]
            internal static bool IsTokenCanceled(CancelationRef _this, short tokenId)
            {
                return _this != null && _this.IsTokenCanceled(tokenId);
            }

            [MethodImpl(InlineOption)]
            private bool IsTokenCanceled(short tokenId)
            {
                return tokenId == TokenId & _state == (int) State.Canceled;
            }

            [MethodImpl(InlineOption)]
            internal static void ThrowIfCanceled(CancelationRef _this, short tokenId, bool isCanceled)
            {
                if (isCanceled | (_this != null && _this.IsTokenCanceled(tokenId)))
                {
                    throw CanceledExceptionInternal.GetOrCreate();
                }
            }

            [MethodImpl(InlineOption)]
            internal static void MaybeAddLinkedCancelation(CancelationRef listener, CancelationRef _this, short tokenId, bool isCanceled)
            {
                if (isCanceled)
                {
                    listener.TryInvokeCallbacks();
                }
                else if (_this != null)
                {
                    _this.MaybeAddLinkedCancelation(listener, tokenId);
                }
            }

            [MethodImpl(InlineOption)]
            internal void MaybeAddLinkedCancelation(CancelationRef listener, short tokenId)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    return;
                }
                State state = (State) _state;
                if (state != State.Pending)
                {
                    goto MaybeInvokeAndReturn;
                }
                lock (listener._registeredCallbacks)
                {
                    if (listener._state != (int) State.Pending) // Make sure listener wasn't canceled from another token on another thread.
                    {
                        goto Return;
                    }
                    uint order;
                    lock (_registeredCallbacks)
                    {
                        state = (State) _state;
                        if (state != State.Pending) // Double-checked locking! In this case it works because we're not writing back to the field.
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
                    listener._links.Push(new CancelationRegistration(this, TokenId, order, false));
                }
                goto Return;

            MaybeInvokeAndReturn:
                if (state == State.Canceled)
                {
                    listener.TryInvokeCallbacks();
                }
            Return:
                ReleaseAfterRetainInternal();
            }

            [MethodImpl(InlineOption)]
            internal static bool TryRegister<TCancelable>(CancelationRef _this, short tokenId, bool isCanceled,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCancelable cancelable, out CancelationRegistration registration) where TCancelable : ICancelable
            {
                if (isCanceled)
                {
                    registration = new CancelationRegistration(null, ValidIdFromApi, 0, isCanceled);
                    cancelable.Cancel();
                    return true;
                }
                if (_this == null)
                {
                    registration = default(CancelationRegistration);
                    return false;
                }
                return _this.TryRegister(cancelable, tokenId, out registration);
            }

            [MethodImpl(InlineOption)]
            private bool TryRegister<TCancelable>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCancelable cancelable, short tokenId, out CancelationRegistration registration) where TCancelable : ICancelable
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    registration = default(CancelationRegistration);
                    return false;
                }
                try
                {
                    State state = (State) _state;
                    if (state != State.Pending)
                    {
                        if (state == State.Canceled)
                        {
                            registration = new CancelationRegistration(this, TokenId, 0, false);
                            cancelable.Cancel();
                            return true;
                        }
                        registration = default(CancelationRegistration);
                        return false;
                    }
                    return TryRegister(CancelableWrappe<TCancelable>.GetOrCreate(cancelable), out registration);
                }
                finally
                {
                    ReleaseAfterRetainInternal();
                }
            }

            [MethodImpl(InlineOption)]
            private bool TryRegister(CancelableBase callback, out CancelationRegistration registration)
            {
                uint order;
                State state;
                lock (_registeredCallbacks)
                {
                    state = (State) _state;
                    if (state != State.Pending)
                    {
                        goto MaybeInvoke;
                    }
                    checked
                    {
                        order = ++_registeredCount;
                    }
                    _registeredCallbacks.Add(new RegisteredDelegate(order, callback));
                }
                registration = new CancelationRegistration(this, TokenId, order, false);
                return true;

            MaybeInvoke:
                if (state == State.Canceled)
                {
                    registration = new CancelationRegistration(this, TokenId, 0, false);
                    callback.Invoke();
                    return true;
                }
                callback.Dispose();
                registration = default(CancelationRegistration);
                return false;
            }

            [MethodImpl(InlineOption)]
            internal static bool GetIsRegisteredAndIsCanceled(CancelationRef _this, short tokenId, uint order, out bool isCanceled)
            {
                isCanceled = false;
                return _this != null && _this.IsRegistered(tokenId, order, out isCanceled);
            }

            [MethodImpl(InlineOption)]
            private bool IsRegistered(short tokenId, uint order, out bool isCanceled)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    return isCanceled = false;
                }
                bool validOrder;
                State state = (State) _state;
                if (state != State.Pending)
                {
                    isCanceled = state == State.Canceled;
                    validOrder = false;
                }
                else
                {
                    lock (_registeredCallbacks)
                    {
                        state = (State) _state;
                        isCanceled = state == State.Canceled;
                        validOrder = state == State.Pending && IndexOf(order) >= 0;
                    }
                }
                ReleaseAfterRetainInternal();
                return validOrder;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryUnregister(CancelationRef _this, short tokenId, uint order, out bool isCanceled)
            {
                if (_this == null)
                {
                    return isCanceled = false;
                }
                return _this.TryUnregister(tokenId, order, out isCanceled);
            }

            [MethodImpl(InlineOption)]
            private bool TryUnregister(short tokenId, uint order, out bool isCanceled)
            {
                // Retain for thread safety.
                if (!TryRetainInternal(tokenId))
                {
                    return isCanceled = false;
                }
                bool unregistered = false;
                CancelableBase cancelable;
                lock (_registeredCallbacks)
                {
                    State state = (State) _state;
                    isCanceled = state == State.Canceled;
                    if (state != State.Pending)
                    {
                        goto ReleaseAndReturn;
                    }
                    int index = IndexOf(order);
                    if (index < 0)
                    {
                        goto ReleaseAndReturn;
                    }
                    cancelable = _registeredCallbacks[index].cancelable;
                    _registeredCallbacks.RemoveAt(index);
                }
                cancelable.Dispose();
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
            internal static bool TrySetCanceled(CancelationRef _this, short sourceId)
            {
                return _this != null && _this.TrySetCanceled(sourceId);
            }

            [MethodImpl(InlineOption)]
            private bool TrySetCanceled(short sourceId)
            {
                // Retain for thread safety and recursive calls.
                if (!TryRetainInternal(sourceId))
                {
                    return false;
                }
                try
                {
                    return TryInvokeCallbacks();
                }
                finally
                {
                    ReleaseAfterRetainInternal();
                }
            }

            private bool TryInvokeCallbacks()
            {
                if (Interlocked.CompareExchange(ref _state, (int) State.Canceled, (int) State.Pending) != (int) State.Pending)
                {
                    return false;
                }
                // Wait for a callback currently being added/removed in another thread.
                // When other threads enter the lock, they will see the _state was already set, so we don't need any further callback synchronization.
                lock (_registeredCallbacks) { }
                Unlink();
                List<Exception> exceptions = null;
                for (int i = 0, max = _registeredCallbacks.Count; i < max; ++i)
                {
                    try
                    {
                        _registeredCallbacks[i].cancelable.Invoke();
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
            internal static bool TryDispose(CancelationRef _this, short sourceId)
            {
                return _this != null && _this.TryDispose(sourceId);
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
                if (Interlocked.CompareExchange(ref _state, (int) State.Disposed, (int) State.Pending) == (int) State.Pending)
                {
                    // Wait for a callback currently being added/removed in another thread.
                    // When other threads enter the lock, they will see the _state was already set, so we don't need any further callback synchronization.
                    lock (_registeredCallbacks) { }
                    Unlink();
                    for (int i = 0, max = _registeredCallbacks.Count; i < max; ++i)
                    {
                        _registeredCallbacks[i].cancelable.Dispose();
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
            internal static bool TryRetainUser(CancelationRef _this, short tokenId, bool isCanceled)
            {
                return isCanceled | (_this != null && _this.TryRetainUser(tokenId));
            }

            [MethodImpl(InlineOption)]
            private bool TryRetainUser(short tokenId)
            {
                if (_idsAndRetains.InterlockedTryRetainUser(tokenId))
                {
                    ThrowIfInPool(this);
                    return true;
                }
                return false;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryReleaseUser(CancelationRef _this, short tokenId, bool isCanceled)
            {
                return isCanceled | (_this != null && _this.TryReleaseUser(tokenId));
            }

            [MethodImpl(InlineOption)]
            private bool TryReleaseUser(short tokenId)
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
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (_registeredCallbacks.Count != 0)
                {
                    throw new System.InvalidOperationException("CancelationToken callbacks have not been unregistered.");
                }
#endif
                _state = (int) State.Disposed;
                ObjectPool<CancelationRef>.MaybeRepool(this);
            }

            internal override void Invoke()
            {
                ThrowIfInPool(this);
                try
                {
                    TryInvokeCallbacks();
                }
                finally
                {
                    ReleaseAfterRetainInternal();
                }
            }

            internal override void Dispose()
            {
                ThrowIfInPool(this);
                ReleaseAfterRetainInternal();
            }
        }
    }
}