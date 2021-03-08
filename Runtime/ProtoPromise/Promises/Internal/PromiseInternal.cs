#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable RECS0001 // Class is declared partial but has only one part
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

using Proto.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    [StructLayout(LayoutKind.Auto)]
    partial struct Promise
    {
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRef _ref;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly ushort _id;

        /// <summary>
        /// Internal use.
        /// </summary>
        internal Promise(Internal.PromiseRef promiseRef, ushort id)
        {
            _ref = promiseRef;
            _id = id;
        }
    }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    [StructLayout(LayoutKind.Auto)]
    partial struct Promise<T>
    {
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly Internal.PromiseRef _ref;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly ushort _id;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly T _result;

        /// <summary>
        /// Internal use.
        /// </summary>
        internal Promise(Internal.PromiseRef promiseRef, ushort id)
        {
            _ref = promiseRef;
            _id = id;
            _result = default(T);
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        internal Promise(Internal.PromiseRef promiseRef, ushort id, ref T value)
        {
            _ref = promiseRef;
            _id = id;
            _result = value;
        }
    }

    partial class Internal
    {
        // Just a random number that's not zero. Using this in Promise(<T>) instead of a bool prevents extra memory padding.
        internal const ushort ValidIdFromApi = 41265;
        internal const int ID_BITSHIFT = 16;

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal abstract partial class PromiseRef : ITreeHandleable, ITraceable
        {
            private ITreeHandleable _next;
            volatile private object _valueOrPrevious;
            // Left bits are for Id, right bits are for retains. (Retains are just for thread safety to prevent an object from being disposed while a callback is being added.)
            volatile private int _idAndRetainCounter = 1 << ID_BITSHIFT;
            volatile private Promise.State _state;
            volatile private bool _wasAwaited;
            private bool _suppressRejection;

            ITreeHandleable ILinked<ITreeHandleable>.Next
            {
                [MethodImpl(InlineOption)]
                get { return _next; }
                [MethodImpl(InlineOption)]
                set { _next = value; }
            }

            internal ushort Id
            {
                [MethodImpl(InlineOption)]
                get
                {
                    unchecked
                    {
                        return (ushort) (_idAndRetainCounter >> ID_BITSHIFT);
                    }
                }
            }
            internal Promise.State State
            {
                [MethodImpl(InlineOption)]
                get { return _state; }
            }

            private PromiseRef() { }

            ~PromiseRef()
            {
                if (!_wasAwaited)
                {
                    // Promise was not awaited or forgotten.
                    string message = "A Promise's resources were garbage collected without it being awaited. You must await, return, or forget each promise.";
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                }
                if (_state != Promise.State.Pending & _valueOrPrevious != null)
                {
                    if (_suppressRejection)
                    {
                        ((IValueContainer) _valueOrPrevious).Release();
                    }
                    else
                    {
                        // Rejection maybe wasn't caught.
                        ((IValueContainer) _valueOrPrevious).ReleaseAndAddToUnhandledStack();
                    }
                }
            }

            protected virtual void MarkAwaited(ushort promiseId)
            {
                IncrementIdAndRetain(promiseId, 1);
                _wasAwaited = true;
            }

            private int IncrementIdAndRetain(ushort promiseId, int retainCount)
            {
                int newValue;
                if (!InterlockedTryAddIdAndRetain(promiseId, (1 << ID_BITSHIFT) + retainCount, out newValue)) // Left bits are for Id, right bits are for retains.
                {
                    // Public APIs do a simple validation check in DEBUG mode, this is an extra thread-safe validation in case the same object is concurrently used and/or forgotten at the same time.
                    // This is left in RELEASE mode because concurrency issues can be very difficult to track down, and might not show up in DEBUG mode.
                    throw new InvalidOperationException("Attempted to use an invalid Promise. This may be because you are attempting to use a promise simultaneously on multiple threads that you have not preserved.",
                        GetFormattedStacktrace(1));
                }
                ThrowIfInPool(this);
                return newValue;
            }

            [MethodImpl(InlineOption)]
            private bool InterlockedTryAddIdAndRetain(ushort promiseId, int addValue, out int newValue)
            {
                int initialValue;
                do
                {
                    initialValue = _idAndRetainCounter;
                    unchecked // We want the id to wrap around.
                    {
                        newValue = initialValue + addValue;
                    }
                    uint oldId = (uint) initialValue >> ID_BITSHIFT; // Left bits are for Id.
                    if (oldId != promiseId)
                    {
                        return false;
                    }
                }
                while (Interlocked.CompareExchange(ref _idAndRetainCounter, newValue, initialValue) != initialValue);
                return true;
            }

            internal void MarkAwaitedAndMaybeDispose(ushort promiseId, bool suppressRejection)
            {
                ThrowIfInPool(this);
                MarkAwaited(promiseId);
                _suppressRejection |= suppressRejection;
                MaybeDispose();
            }

            void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
            {
                ThrowIfInPool(this);
                owner._suppressRejection = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                handleQueue.Push(this);
            }

            void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
            {
                ThrowIfInPool(this);
                owner._suppressRejection = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                AddToHandleQueueBack(this);
            }

            protected void Reset()
            {
                _state = Promise.State.Pending;
                _suppressRejection = false;
                _wasAwaited = false;
                ++_idAndRetainCounter; // Set retain counter to 1 without changing the Id (Interlocked unnecessary).
                SetCreatedStacktrace(this, 3);
            }

            protected void MaybeDispose()
            {
                ThrowIfInPool(this);
                unchecked
                {
                    if ((ushort) Interlocked.Decrement(ref _idAndRetainCounter) == 0 // Right bits are for retain.
                        & _wasAwaited & _state != Promise.State.Pending)
                    {
                        Dispose();
                    }
                }
            }

            protected virtual void Dispose()
            {
                if (_valueOrPrevious != null)
                {
                    if (_suppressRejection)
                    {
                        ((IValueContainer) _valueOrPrevious).Release();
                    }
                    else
                    {
                        // Rejection maybe wasn't caught.
                        ((IValueContainer) _valueOrPrevious).ReleaseAndAddToUnhandledStack();
                    }
                    _valueOrPrevious = null;
                }
            }

            private void HookupNewCancelablePromise(PromiseRef newPromise)
            {
                newPromise.SetDepth(this);
                if (Interlocked.CompareExchange(ref newPromise._valueOrPrevious, this, null) == null)
                {
                    AddWaiter(newPromise);
                }
                else
                {
                    MaybeDispose();
                }
            }

            private void HookupNewPromise(PromiseRef newPromise)
            {
                newPromise.SetDepth(this);
                newPromise._valueOrPrevious = this;
                AddWaiter(newPromise);
            }

            internal PromiseRef GetPreserved(ushort promiseId)
            {
                MarkAwaited(promiseId);
                _suppressRejection = true;
                var newPromise = PromiseMultiAwait.GetOrCreate();
                HookupNewPromise(newPromise);
                return newPromise;
            }

            public abstract void Handle(); // ITreeHandleable.Handle()

            internal abstract void Forget(ushort promiseId);

            internal abstract PromiseRef GetDuplicate(ushort promiseId);

            protected abstract bool TryRemoveWaiter(ITreeHandleable treeHandleable);

            protected abstract void AddWaiter(ITreeHandleable waiter);

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract class PromiseSingleAwait : PromiseRef
            {
                internal sealed override void Forget(ushort promiseId)
                {
                    MarkAwaited(promiseId);
                    MaybeDispose();
                }

                internal sealed override PromiseRef GetDuplicate(ushort promiseId)
                {
                    IncrementIdAndRetain(promiseId, 0);
                    return this;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class PromiseMultiAwait : PromiseRef
            {
                private struct Creator : ICreator<PromiseMultiAwait>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseMultiAwait Create()
                    {
                        return new PromiseMultiAwait();
                    }
                }

                private readonly object _locker = new object();
                private ValueLinkedStack<ITreeHandleable> _nextBranches;

                private PromiseMultiAwait() { }

                ~PromiseMultiAwait()
                {
                    _wasAwaited = true; // Stop base finalizer from adding an extra exception.
                    if ((_idAndRetainCounter & ushort.MaxValue) != 0)
                    {
                        string message = "A preserved Promise's resources were garbage collected without it being forgotten. You must call Forget() on each preserved promise when you are finished with it.";
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                    }
                }

                [MethodImpl(InlineOption)]
                public static PromiseMultiAwait GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseMultiAwait, Creator>(new Creator());
                    promise.Reset();
                    ++promise._idAndRetainCounter; // Add an extra retain to be released when Forget is called.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void MarkAwaited(ushort promiseId)
                {
                    int _;
                    if (!InterlockedTryAddIdAndRetain(promiseId, 1, out _)) // Only retain without incrementing Id.
                    {
                        // Public APIs do a simple validation check in DEBUG mode, this is an extra thread-safe validation in case the same object is concurrently used and/or forgotten at the same time.
                        // This is left in RELEASE mode because concurrency issues can be very difficult to track down, and might not show up in DEBUG mode.
                        throw new InvalidOperationException("Attempted to use an invalid Promise. This may be because you are attempting to use a promise after it was forgotten.",
                            GetFormattedStacktrace(1));
                    }
                }

                internal override void Forget(ushort promiseId)
                {
                    IncrementIdAndRetain(promiseId, 0); // Don't retain.
                    _wasAwaited = true;
                    MaybeDispose();
                }


                internal override PromiseRef GetDuplicate(ushort promiseId)
                {
                    MarkAwaited(promiseId);
                    var newPromise = PromiseDuplicate.GetOrCreate();
                    HookupNewPromise(newPromise);
                    return newPromise;
                }

                protected override bool TryRemoveWaiter(ITreeHandleable treeHandleable)
                {
                    lock (_locker)
                    {
                        return _nextBranches.TryRemove(treeHandleable);
                    }
                }

                protected override void AddWaiter(ITreeHandleable waiter)
                {
                    ThrowIfInPool(this);
                    if (_state == Promise.State.Pending)
                    {
                        lock (_locker)
                        {
                            _nextBranches.Push(waiter);
                        }
                    }
                    else
                    {
                        waiter.MakeReadyFromSettled(this, (IValueContainer) _valueOrPrevious);
                    }
                    MaybeDispose();
                }

                public override void Handle()
                {
                    ThrowIfInPool(this);
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    _state = valueContainer.GetState();

                    HandleBranches(valueContainer);
                    if (_state == Promise.State.Resolved)
                    {
                        ResolveProgressListeners();
                    }
                    else
                    {
                        CancelProgressListeners(null);
                    }

                    MaybeDispose();
                }

                private void HandleBranches(IValueContainer valueContainer)
                {
                    ValueLinkedStack<ITreeHandleable> branches;
                    lock (_locker)
                    {
                        branches = _nextBranches;
                        _nextBranches.Clear();
                    }
                    while (branches.IsNotEmpty)
                    {
                        branches.Pop().MakeReady(this, valueContainer, ref _handleQueue);
                    }

                    //// TODO: keeping this code around for when background threaded tasks are implemented.
                    //ValueLinkedQueue<ITreeHandleable> handleQueue = new ValueLinkedQueue<ITreeHandleable>();
                    //while (_nextBranches.IsNotEmpty)
                    //{
                    //    _nextBranches.Pop().MakeReady(this, valueContainer, ref handleQueue);
                    //}
                    //AddToHandleQueueFront(ref handleQueue);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract class PromiseBranch : PromiseSingleAwait
            {
                volatile private ITreeHandleable _waiter;

                protected sealed override bool TryRemoveWaiter(ITreeHandleable treeHandleable)
                {
                    return Interlocked.CompareExchange(ref _waiter, null, treeHandleable) == treeHandleable;
                }

                protected sealed override void AddWaiter(ITreeHandleable waiter)
                {
                    ThrowIfInPool(this);
                    // When this is completed, _state is set then _waiter is swapped, so setting _waiter before checking _state here ensures thread safety.
                    _waiter = waiter;
                    if (_state != Promise.State.Pending)
                    {
                        _waiter = null;
                        waiter.MakeReadyFromSettled(this, (IValueContainer) _valueOrPrevious);
                    }
                    MaybeDispose();
                }

                public override void Handle()
                {
                    ThrowIfInPool(this);
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    bool invokingRejected = false;
                    SetCurrentInvoker(this);
                    try
                    {
                        // valueContainer is released deeper in the call stack, so we only release it in this method if an exception is thrown.
                        // (This is in case it is canceled, the valueContainer will be released from the cancelation.)
                        Execute(valueContainer, ref invokingRejected);
                    }
                    catch (RethrowException e)
                    {
                        if (invokingRejected || (e is ForcedRethrowException && valueContainer.GetState() != Promise.State.Resolved))
                        {
                            RejectOrCancelInternal(valueContainer);
                            valueContainer.Release(); // Must release since RejectOrCancelInternal adds an extra retain.
                        }
                        else
                        {
                            valueContainer.ReleaseAndMaybeAddToUnhandledStack(true);
#if PROMISE_DEBUG
                            string stacktrace = FormatStackTrace(new System.Diagnostics.StackTrace[1] { new System.Diagnostics.StackTrace(e, true) });
#else
                            string stacktrace = new System.Diagnostics.StackTrace(e, true).ToString();
#endif
                            object exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
                            RejectOrCancelInternal(RejectionContainer<object>.GetOrCreate(ref exception, 0));
                        }
                    }
                    catch (OperationCanceledException e)
                    {
                        valueContainer.ReleaseAndMaybeAddToUnhandledStack(!invokingRejected);
                        RejectOrCancelInternal(CreateCancelContainer(ref e));
                    }
                    catch (Exception e)
                    {
                        valueContainer.ReleaseAndMaybeAddToUnhandledStack(!invokingRejected);
                        RejectOrCancelInternal(CreateRejectContainer(ref e, int.MinValue, this));
                    }
                    ClearCurrentInvoker();
                }

                protected virtual void Execute(IValueContainer valueContainer, ref bool invokingRejected) { }

                internal void ResolveInternal(IValueContainer valueContainer)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    _state = Promise.State.Resolved;
                    HandleWaiter(valueContainer);
                    ResolveProgressListeners();

                    MaybeDispose();
                }

                internal void RejectOrCancelInternal(IValueContainer valueContainer)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    _state = valueContainer.GetState();
                    HandleWaiter(valueContainer);
                    CancelProgressListeners(null);

                    MaybeDispose();
                }

                internal void HandleSelf(IValueContainer valueContainer)
                {
                    _state = valueContainer.GetState();
                    HandleWaiter(valueContainer);
                    if (_state == Promise.State.Resolved)
                    {
                        ResolveProgressListeners();
                    }
                    else
                    {
                        CancelProgressListeners(null);
                    }

                    MaybeDispose();
                }

                internal void HandleWaiter(IValueContainer valueContainer)
                {
                    ITreeHandleable waiter = Interlocked.Exchange(ref _waiter, null);
                    if (waiter != null)
                    {
                        waiter.MakeReady(this, valueContainer, ref _handleQueue);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class PromiseDuplicate : PromiseBranch
            {
                private struct Creator : ICreator<PromiseDuplicate>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseDuplicate Create()
                    {
                        return new PromiseDuplicate();
                    }
                }

                private PromiseDuplicate() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                public static PromiseDuplicate GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseDuplicate, Creator>(new Creator());
                    promise.Reset();
                    return promise;
                }

                public override void Handle()
                {
                    HandleSelf((IValueContainer) _valueOrPrevious);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseWaitPromise : PromiseBranch
            {
                internal void WaitFor(Promise other)
                {
                    ThrowIfInPool(this);
                    ValidateReturn(other);
                    var _ref = other._ref;
                    if (_ref == null)
                    {
                        ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                    else
                    {
                        _ref.MarkAwaited(other._id);
                        _valueOrPrevious = _ref;
                        SubscribeProgressToOther(_ref);
                        _ref.AddWaiter(this);
                    }
                }

                internal void WaitFor<T>(Promise<T> other)
                {
                    ThrowIfInPool(this);
                    ValidateReturn(other);
                    var _ref = other._ref;
                    if (_ref == null)
                    {
                        ResolveInternal(ResolveContainer<T>.GetOrCreate(other._result, 0));
                    }
                    else
                    {
                        _ref.MarkAwaited(other._id);
                        _valueOrPrevious = _ref;
                        SubscribeProgressToOther(_ref);
                        _ref.AddWaiter(this);
                    }
                }

                partial void SubscribeProgressToOther(PromiseRef other);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class AsyncPromiseBase : PromiseSingleAwait
            {
                protected sealed override bool TryRemoveWaiter(ITreeHandleable treeHandleable)
                {
                    return Interlocked.CompareExchange(ref _next, null, treeHandleable) == treeHandleable;
                }

                protected sealed override void AddWaiter(ITreeHandleable waiter)
                {
                    ThrowIfInPool(this);
                    // When this is completed, _state is set then _next is swapped, so setting _waiter before checking _state here ensures thread safety.
                    _next = waiter;
                    if (_state != Promise.State.Pending)
                    {
                        _next = null;
                        waiter.MakeReadyFromSettled(this, (IValueContainer) _valueOrPrevious);
                    }
                    MaybeDispose();
                }

                internal void ResolveInternal(IValueContainer valueContainer)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    _state = Promise.State.Resolved;
                    HandleWaiter(valueContainer);
                    ResolveProgressListeners();

                    MaybeDispose();
                }

                internal void RejectOrCancelInternal(IValueContainer valueContainer)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    _state = valueContainer.GetState();
                    HandleWaiter(valueContainer);
                    CancelProgressListeners(null);

                    MaybeDispose();
                }

                internal void HandleWaiter(IValueContainer valueContainer)
                {
                    ITreeHandleable waiter = Interlocked.Exchange(ref _next, null);
                    if (waiter != null)
                    {
                        waiter.MakeReady(this, valueContainer, ref _handleQueue);
                    }
                }

                protected void ResolveDirect()
                {
                    ThrowIfInPool(this);
                    ResolveInternal(ResolveContainerVoid.GetOrCreate());
                }

                protected void ResolveDirect<T>(ref T value)
                {
                    ThrowIfInPool(this);
                    ResolveInternal(ResolveContainer<T>.GetOrCreate(ref value, 0));
                }

                protected void RejectDirect<TReject>(ref TReject reason, int rejectSkipFrames)
                {
                    ThrowIfInPool(this);
                    RejectOrCancelInternal(CreateRejectContainer(ref reason, rejectSkipFrames + 1, this));
                }

                protected void CancelDirect()
                {
                    ThrowIfInPool(this);
                    RejectOrCancelInternal(CancelContainerVoid.GetOrCreate());
                }

                protected void CancelDirect<TCancel>(ref TCancel reason)
                {
                    ThrowIfInPool(this);
                    RejectOrCancelInternal(CreateCancelContainer(ref reason));
                }

                public sealed override void Handle() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class DeferredPromiseBase : AsyncPromiseBase
            {
                // Only using int because Interlocked does not support ushort.
                private int _deferredId = 1 << ID_BITSHIFT; // Using left bits for Id instead of right bits so that we will get automatic wrapping without an extra operation.
                internal ushort DeferredId
                {
                    [MethodImpl(InlineOption)]
                    get
                    {
                        unchecked
                        {
                            return (ushort) (_deferredId >> ID_BITSHIFT);
                        }
                    }
                }

                protected DeferredPromiseBase() { }

                ~DeferredPromiseBase()
                {
                    if (_state == Promise.State.Pending)
                    {
                        // Deferred wasn't handled.
                        AddRejectionToUnhandledStack(UnhandledDeferredException.instance, this);
                    }
                }

                protected virtual bool TryUnregisterCancelation() { return true; }

                protected bool TryIncrementDeferredIdAndUnregisterCancelation(ushort comparand)
                {
                    int intComp = comparand << ID_BITSHIFT; // Left bits are for Id.
                    unchecked // We want the id to wrap around.
                    {
                        return Interlocked.CompareExchange(ref _deferredId, intComp + (1 << ID_BITSHIFT), intComp) == intComp
                            && TryUnregisterCancelation(); // If TryUnregisterCancelation returns false, it means the CancelationSource was canceled.
                    }
                }

                internal bool TryReject<TReject>(ref TReject reason, ushort deferredId, int rejectSkipFrames)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        RejectDirect(ref reason, rejectSkipFrames + 1);
                        return true;
                    }
                    return false;
                }

                internal bool TryCancel<TCancel>(ref TCancel reason, ushort deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        CancelDirect(ref reason);
                        return true;
                    }
                    return false;
                }

                internal bool TryCancel(ushort deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        CancelDirect();
                        return true;
                    }
                    return false;
                }

                protected void CancelFromToken(ICancelValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    // A simple increment is sufficient.
                    // If the CancelationSource was canceled before the Deferred was completed, even if the Deferred was completed before the cancelation was invoked, the cancelation takes precedence.
                    Interlocked.Add(ref _deferredId, 1 << ID_BITSHIFT); // Left bits are for Id.
                    RejectOrCancelInternal(valueContainer);
                }
            }

            // The only purpose of this is to cast the ref when converting a DeferredBase to a Deferred(<T>) to avoid extra checks.
            // Otherwise, DeferredPromise<T> would be unnecessary and this would be implemented in the base class.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal class DeferredPromiseVoid : DeferredPromiseBase
            {
                private struct Creator : ICreator<DeferredPromiseVoid>
                {
                    [MethodImpl(InlineOption)]
                    public DeferredPromiseVoid Create()
                    {
                        return new DeferredPromiseVoid();
                    }
                }

                protected DeferredPromiseVoid() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                // Used for child to call base dispose without repooling for both types.
                // This is necessary because C# doesn't allow `base.base.Dispose()`.
                [MethodImpl(InlineOption)]
                protected void SuperDispose()
                {
                    base.Dispose();
                }

                internal bool TryResolve(ushort deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        ResolveDirect();
                        return true;
                    }
                    return false;
                }

                internal static DeferredPromiseVoid GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromiseVoid, Creator>(new Creator());
                    promise.Reset();
                    promise.ResetDepth();
                    return promise;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal class DeferredPromise<T> : DeferredPromiseBase
            {
                private struct Creator : ICreator<DeferredPromise<T>>
                {
                    [MethodImpl(InlineOption)]
                    public DeferredPromise<T> Create()
                    {
                        return new DeferredPromise<T>();
                    }
                }

                protected DeferredPromise() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                // Used for child to call base dispose without repooling for both types.
                // This is necessary because C# doesn't allow `base.base.Dispose()`.
                [MethodImpl(InlineOption)]
                protected void SuperDispose()
                {
                    base.Dispose();
                }

                internal bool TryResolve(ref T value, ushort deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        ResolveDirect(ref value);
                        return true;
                    }
                    return false;
                }

                internal static DeferredPromise<T> GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromise<T>, Creator>(new Creator());
                    promise.Reset();
                    promise.ResetDepth();
                    return promise;
                }
            }

            // IDelegate to reduce the amount of classes I would have to write(Composition Over Inheritance).
            // Using generics with constraints allows us to use structs to get composition for "free"
            // (no extra object allocation or extra memory overhead, and the compiler will generate the Promise classes for us).
            // The only downside is that more classes are created than if we just used straight interfaces (not a problem with JIT, but makes the code size larger with AOT).

            // Resolve types for more common .Then(onResolved) calls to be more efficient.

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseResolve<TResolver> : PromiseBranch where TResolver : IDelegateResolve
            {
                private struct Creator : ICreator<PromiseResolve<TResolver>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseResolve<TResolver> Create()
                    {
                        return new PromiseResolve<TResolver>();
                    }
                }

                private TResolver _resolver;

                private PromiseResolve() { }

                [MethodImpl(InlineOption)]
                public static PromiseResolve<TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolve<TResolver>, Creator>(new Creator());
                    promise.Reset();
                    promise._resolver = resolver;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this);
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseResolvePromise<TResolver> : PromiseWaitPromise where TResolver : IDelegateResolvePromise
            {
                private struct Creator : ICreator<PromiseResolvePromise<TResolver>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseResolvePromise<TResolver> Create()
                    {
                        return new PromiseResolvePromise<TResolver>();
                    }
                }

                private TResolver _resolver;

                private PromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                public static PromiseResolvePromise<TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolvePromise<TResolver>, Creator>(new Creator());
                    promise.Reset();
                    promise._resolver = resolver;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this);
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseResolveReject<TResolver, TRejecter> : PromiseBranch
                where TResolver : IDelegateResolve
                where TRejecter : IDelegateReject
            {
                private struct Creator : ICreator<PromiseResolveReject<TResolver, TRejecter>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseResolveReject<TResolver, TRejecter> Create()
                    {
                        return new PromiseResolveReject<TResolver, TRejecter>();
                    }
                }

                private TResolver _resolver;
                private TRejecter _rejecter;

                private PromiseResolveReject() { }

                [MethodImpl(InlineOption)]
                public static PromiseResolveReject<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolveReject<TResolver, TRejecter>, Creator>(new Creator());
                    promise.Reset();
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    _rejecter = default(TRejecter);
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(valueContainer, this);
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise
                where TResolver : IDelegateResolvePromise
                where TRejecter : IDelegateRejectPromise
            {
                private struct Creator : ICreator<PromiseResolveRejectPromise<TResolver, TRejecter>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseResolveRejectPromise<TResolver, TRejecter> Create()
                    {
                        return new PromiseResolveRejectPromise<TResolver, TRejecter>();
                    }
                }

                private TResolver _resolver;
                private TRejecter _rejecter;

                private PromiseResolveRejectPromise() { }

                [MethodImpl(InlineOption)]
                public static PromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolveRejectPromise<TResolver, TRejecter>, Creator>(new Creator());
                    promise.Reset();
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    _rejecter = default(TRejecter);
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(valueContainer, this);
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer);
                        valueContainer.Release();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseContinue<TContinuer> : PromiseBranch
                where TContinuer : IDelegateContinue
            {
                private struct Creator : ICreator<PromiseContinue<TContinuer>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseContinue<TContinuer> Create()
                    {
                        return new PromiseContinue<TContinuer>();
                    }
                }

                private TContinuer _continuer;

                private PromiseContinue() { }

                [MethodImpl(InlineOption)]
                public static PromiseContinue<TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseContinue<TContinuer>, Creator>(new Creator());
                    promise.Reset();
                    promise._continuer = continuer;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    callback.Invoke(valueContainer, this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseContinuePromise<TContinuer> : PromiseWaitPromise
                where TContinuer : IDelegateContinuePromise
            {
                private struct Creator : ICreator<PromiseContinuePromise<TContinuer>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseContinuePromise<TContinuer> Create()
                    {
                        return new PromiseContinuePromise<TContinuer>();
                    }
                }

                private TContinuer _continuer;

                private PromiseContinuePromise() { }

                [MethodImpl(InlineOption)]
                public static PromiseContinuePromise<TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseContinuePromise<TContinuer>, Creator>(new Creator());
                    promise.Reset();
                    promise._continuer = continuer;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    callback.Invoke(valueContainer, this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseFinally<TFinalizer> : PromiseBranch
                where TFinalizer : IDelegateSimple
            {
                private struct Creator : ICreator<PromiseFinally<TFinalizer>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseFinally<TFinalizer> Create()
                    {
                        return new PromiseFinally<TFinalizer>();
                    }
                }

                private TFinalizer _finalizer;

                private PromiseFinally() { }

                [MethodImpl(InlineOption)]
                public static PromiseFinally<TFinalizer> GetOrCreate(TFinalizer finalizer)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseFinally<TFinalizer>, Creator>(new Creator());
                    promise.Reset();
                    promise._finalizer = finalizer;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(IValueContainer valueContainer, ref bool invokingRejected)
                {
                    var callback = _finalizer;
                    _finalizer = default(TFinalizer);
                    callback.Invoke(valueContainer);
                    HandleSelf(valueContainer);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed class PromiseCancel<TCanceler> : PromiseBranch, ITreeHandleable
                where TCanceler : IDelegateSimple
            {
                private struct Creator : ICreator<PromiseCancel<TCanceler>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseCancel<TCanceler> Create()
                    {
                        return new PromiseCancel<TCanceler>();
                    }
                }

                private TCanceler _canceler;

                private PromiseCancel() { }

                [MethodImpl(InlineOption)]
                public static PromiseCancel<TCanceler> GetOrCreate(TCanceler canceler)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseCancel<TCanceler>, Creator>(new Creator());
                    promise.Reset();
                    promise._canceler = canceler;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                public override void Handle()
                {
                    ThrowIfInPool(this);
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;

                    if (valueContainer.GetState() != Promise.State.Canceled)
                    {
                        HandleSelf(valueContainer);
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    SetCurrentInvoker(this);
                    try
                    {
                        callback.Invoke(valueContainer);
                    }
                    catch (Exception e)
                    {
                        AddRejectionToUnhandledStack(e, this);
                    }
                    ClearCurrentInvoker();

                    HandleSelf(valueContainer);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromisePassThrough : ITreeHandleable, ILinked<PromisePassThrough>
            {
                private struct Creator : ICreator<PromisePassThrough>
                {
                    [MethodImpl(InlineOption)]
                    public PromisePassThrough Create()
                    {
                        return new PromisePassThrough();
                    }
                }

                ITreeHandleable ILinked<ITreeHandleable>.Next { get; set; }
                PromisePassThrough ILinked<PromisePassThrough>.Next { get; set; }

                internal PromiseRef Owner
                {
                    [MethodImpl(InlineOption)]
                    get
                    {
                        ThrowIfInPool(this);
                        return _owner;
                    }
                }
                internal IMultiTreeHandleable Target
                {
                    [MethodImpl(InlineOption)]
                    get
                    {
                        ThrowIfInPool(this);
                        return _target;
                    }
                }

                private PromiseRef _owner;
                private IMultiTreeHandleable _target;
                private int _index;
                private int _retainCount;

                private PromisePassThrough() { }

                ~PromisePassThrough()
                {
                    if (_retainCount != 0)
                    {
                        string message = "A PromisePassThrough was garbage collected without it being released.";
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), null);
                    }
                }

                public static PromisePassThrough GetOrCreate(Promise owner, int index)
                {
                    // owner._ref is checked for nullity before passing into this.
                    owner._ref.MarkAwaited(owner._id);
                    var passThrough = ObjectPool<PromisePassThrough>.GetOrCreate<PromisePassThrough, Creator>(new Creator());
                    passThrough._owner = owner._ref;
                    passThrough._index = index;
                    passThrough.ResetProgress();
                    passThrough._retainCount = 1;
                    return passThrough;
                }

                partial void ResetProgress();

                internal void SetTargetAndAddToOwner(IMultiTreeHandleable target)
                {
                    ThrowIfInPool(this);
                    _target = target;
                    Owner.AddWaiter(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    ThrowIfInPool(this);
                    var temp = Target;
                    if (temp.Handle(valueContainer, this, _index))
                    {
                        handleQueue.Push(temp);
                    }
                    Release();
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    var temp = Target;
                    if (temp.Handle(valueContainer, this, _index))
                    {
                        AddToHandleQueueBack(temp);
                    }
                    Release();
                }

                [MethodImpl(InlineOption)]
                internal void Retain()
                {
                    ThrowIfInPool(this);
                    Interlocked.Increment(ref _retainCount);
                }

                internal void Release()
                {
                    ThrowIfInPool(this);
                    if (Interlocked.Decrement(ref _retainCount) == 0)
                    {
                        _owner = null;
                        _target = null;
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }
                }

                void ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
            }

            internal static void MaybeMarkAwaitedAndDispose(Promise promise, bool suppressRejection)
            {
                if (promise._ref != null)
                {
                    promise._ref.MarkAwaitedAndMaybeDispose(promise._id, suppressRejection);
                }
            }
        }

        internal static uint PrepareForMulti(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index)
        {
            if (promise._ref != null)
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
                return 1;
            }
            return 0;
        }

        internal static uint PrepareForMulti(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index, ref ulong completedProgress)
        {
            if (promise._ref != null)
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
                return 1;
            }
            // TODO: store depthAndProgress in Promise structs.
            ++completedProgress;
            return 0;
        }

        internal static uint PrepareForMulti<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index)
        {
            if (promise._ref != null)
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
                return 1;
            }
            value = promise._result;
            return 0;
        }

        internal static uint PrepareForMulti<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index, ref ulong completedProgress)
        {
            if (promise._ref != null)
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
                return 1;
            }
            // TODO: store depthAndProgress in Promise structs.
            ++completedProgress;
            value = promise._result;
            return 0;
        }

        [MethodImpl(InlineOption)]
        internal static Promise CreateResolved()
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging in the finalizer.
            var deferred = Promise.NewDeferred();
            deferred.Resolve();
            return deferred.Promise;
#else
            // Make a promise on the stack for efficiency.
            return new Promise(null, ValidIdFromApi);
#endif
        }

        [MethodImpl(InlineOption)]
        internal static Promise<T> CreateResolved<T>(ref T value)
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging in the finalizer.
            var deferred = Promise.NewDeferred<T>();
            deferred.Resolve(value);
            return deferred.Promise;
#else
            // Make a promise on the stack for efficiency.
            return new Promise<T>(null, ValidIdFromApi, ref value);
#endif
        }
    }
}