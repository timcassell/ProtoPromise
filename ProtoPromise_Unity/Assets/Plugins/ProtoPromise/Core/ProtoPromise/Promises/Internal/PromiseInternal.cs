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

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

using Proto.Utils;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    partial struct Promise { }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    partial struct Promise<T> { }

    partial class Internal
    {
        // Just a random number that's not zero. Using this in Promise(<T>) instead of a bool prevents extra memory padding.
        internal const short ValidIdFromApi = 31265;

#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal abstract partial class PromiseRef : ITreeHandleable, ITraceable
        {
            ITreeHandleable ILinked<ITreeHandleable>.Next
            {
                [MethodImpl(InlineOption)]
                get { return _next; }
                [MethodImpl(InlineOption)]
                set { _next = value; }
            }

            internal short Id
            {
                [MethodImpl(InlineOption)]
                get { return _idsAndRetains._promiseId; }
            }

            internal Promise.State State
            {
                [MethodImpl(InlineOption)]
                get { return _smallFields._stateAndFlags._state; }
                [MethodImpl(InlineOption)]
                private set { _smallFields._stateAndFlags._state = value; }
            }

            private bool SuppressRejection
            {
                [MethodImpl(InlineOption)]
                get { return _smallFields._stateAndFlags._suppressRejection; }
                [MethodImpl(InlineOption)]
                set { _smallFields._stateAndFlags._suppressRejection = value; }
            }

            private bool WasAwaitedOrForgotten
            {
                [MethodImpl(InlineOption)]
                get { return _smallFields._stateAndFlags._wasAwaitedOrForgotten; }
                [MethodImpl(InlineOption)]
                set { _smallFields._stateAndFlags._wasAwaitedOrForgotten = value; }
            }

            private PromiseRef() { }

            ~PromiseRef()
            {
                if (!WasAwaitedOrForgotten)
                {
                    // Promise was not awaited or forgotten.
                    string message = "A Promise's resources were garbage collected without it being awaited. You must await, return, or forget each promise.";
                    AddRejectionToUnhandledStack(new UnobservedPromiseException(message), this);
                }
                if (State != Promise.State.Pending & _valueOrPrevious != null)
                {
                    if (SuppressRejection)
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

            protected virtual void MarkAwaited(short promiseId)
            {
                IncrementId(promiseId);
                WasAwaitedOrForgotten = true;
            }

            internal void Forget(short promiseId)
            {
                IncrementId(promiseId);
                WasAwaitedOrForgotten = true;
                MaybeDispose();
            }

            private void IncrementId(short promiseId)
            {
                if (!_idsAndRetains.InterlockedTryIncrementPromiseId(promiseId))
                {
                    // Public APIs do a simple validation check in DEBUG mode, this is an extra thread-safe validation in case the same object is concurrently used and/or forgotten at the same time.
                    // This is left in RELEASE mode because concurrency issues can be very difficult to track down, and might not show up in DEBUG mode.
                    throw new InvalidOperationException("Attempted to use an invalid Promise. This may be because you are attempting to use a promise simultaneously on multiple threads that you have not preserved.",
                        GetFormattedStacktrace(3));
                }
                ThrowIfInPool(this);
            }

            private void InterlockedRetainInternal(short promiseId)
            {
                if (!_idsAndRetains.InterlockedTryRetain(promiseId))
                {
                    // Public APIs do a simple validation check in DEBUG mode, this is an extra thread-safe validation in case the same object is concurrently used and/or forgotten at the same time.
                    // This is left in RELEASE mode because concurrency issues can be very difficult to track down, and might not show up in DEBUG mode.
                    throw new InvalidOperationException("Attempted to use an invalid Promise. This may be because you are attempting to use a promise after it was forgotten.",
                        GetFormattedStacktrace(1));
                }
                ThrowIfInPool(this);
            }

            internal void MarkAwaitedAndMaybeDispose(short promiseId, bool suppressRejection)
            {
                MarkAwaited(promiseId);
                SuppressRejection |= suppressRejection;
                MaybeDispose();
            }

            void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
            {
                ThrowIfInPool(this);
                owner.SuppressRejection = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                AddToHandleQueueFront(this);
                WaitWhileProgressFlags(ProgressFlags.Subscribing);
            }

            void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
            {
                ThrowIfInPool(this);
                owner.SuppressRejection = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                AddToHandleQueueBack(this);
                WaitWhileProgressFlags(ProgressFlags.Subscribing);
            }

            protected void Reset()
            {
                State = Promise.State.Pending;
                SuppressRejection = false;
                WasAwaitedOrForgotten = false;
                // Set retain counter to 2 without changing the Id.
                // 1 retain for state, 1 retain for await/forget.
                _idsAndRetains.InterlockedRetainDisregardId(2);
                SetCreatedStacktrace(this, 3);
            }

            protected void MaybeDispose()
            {
                ThrowIfInPool(this);
                if (_idsAndRetains.InterlockedTryReleaseComplete())
                {
                    Dispose();
                }
            }

            protected virtual void Dispose()
            {
                if (_valueOrPrevious != null)
                {
                    if (SuppressRejection)
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
#if PROMISE_PROGRESS
                _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.All);
#endif
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

#if PROMISE_PROGRESS
            private void HookupNewPromiseWithProgress<TPromiseRef>(TPromiseRef newPromise) where TPromiseRef : PromiseRef, IProgressListener
            {
                newPromise.SetDepth(this);
                newPromise._valueOrPrevious = this;
                AddWaiterWithProgress(newPromise);
            }

            private void AddWaiterWithProgress<TWaiter>(TWaiter waiter) where TWaiter : ITreeHandleable, IProgressListener
            {
                SubscribeListener(waiter);
                AddWaiter(waiter);
            }
#else
            [MethodImpl(InlineOption)]
            private void HookupNewPromiseWithProgress<TPromiseRef>(TPromiseRef newPromise) where TPromiseRef : PromiseRef, IProgressListener
            {
                HookupNewPromise(newPromise);
            }

            [MethodImpl(InlineOption)]
            private void AddWaiterWithProgress<TWaiter>(TWaiter waiter) where TWaiter : ITreeHandleable, IProgressListener
            {
                AddWaiter(waiter);
            }
#endif

            internal PromiseRef GetPreserved(short promiseId)
            {
                MarkAwaited(promiseId);
                SuppressRejection = true;
                var newPromise = PromiseMultiAwait.GetOrCreate();
                HookupNewPromise(newPromise);
                return newPromise;
            }

            public abstract void Handle(); // ITreeHandleable.Handle()

            internal abstract PromiseRef GetDuplicate(short promiseId);

            internal abstract void AddWaiter(ITreeHandleable waiter);

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseSingleAwait : PromiseRef
            {
                internal sealed override PromiseRef GetDuplicate(short promiseId)
                {
                    IncrementId(promiseId);
                    return this;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseMultiAwait : PromiseRef, IProgressListener
            {
                private PromiseMultiAwait() { }

                ~PromiseMultiAwait()
                {
                    if (!WasAwaitedOrForgotten)
                    {
                        WasAwaitedOrForgotten = true; // Stop base finalizer from adding an extra exception.
                        string message = "A preserved Promise's resources were garbage collected without it being forgotten. You must call Forget() on each preserved promise when you are finished with it.";
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static PromiseMultiAwait GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseMultiAwait>()
                        ?? new PromiseMultiAwait();
                    promise.Reset();
                    promise.ResetProgress();
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void MarkAwaited(short promiseId)
                {
                    InterlockedRetainInternal(promiseId);
                }

                internal override PromiseRef GetDuplicate(short promiseId)
                {
                    MarkAwaited(promiseId);
                    var newPromise = PromiseDuplicate.GetOrCreate();
                    HookupNewPromise(newPromise);
                    return newPromise;
                }

                internal override void AddWaiter(ITreeHandleable waiter)
                {
                    ThrowIfInPool(this);
                    if (State == Promise.State.Pending)
                    {
                        lock (_branchLocker)
                        {
                            if (State == Promise.State.Pending)
                            {
                                _nextBranches.Push(waiter);
                                goto MaybeDisposeAndReturn;
                            }
                        }
                    }
                    waiter.MakeReadyFromSettled(this, (IValueContainer) _valueOrPrevious);
                MaybeDisposeAndReturn:
                    MaybeDispose();
                }

                public override void Handle()
                {
                    ThrowIfInPool(this);
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    Promise.State state = valueContainer.GetState();
                    State = state;

                    HandleBranches(valueContainer);
                    HandleProgressListeners(state);

                    MaybeDispose();
                }

                private void HandleBranches(IValueContainer valueContainer)
                {
                    ValueLinkedStack<ITreeHandleable> branches;
                    lock (_branchLocker)
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
            internal abstract partial class PromiseBranch : PromiseSingleAwait
            {
                internal sealed override void AddWaiter(ITreeHandleable waiter)
                {
                    ThrowIfInPool(this);
                    // When this is completed, _state is set then _next is swapped, so we must reverse that process here.
                    _waiter = waiter;
                    Thread.MemoryBarrier(); // Make sure _state is read after _waiter is written.
                    if (State != Promise.State.Pending)
                    {
                        // Exchange and check for null to handle race condition with HandleWaiter on another thread.
                        waiter = Interlocked.Exchange(ref _waiter, null);
                        if (waiter != null)
                        {
                            waiter.MakeReadyFromSettled(this, (IValueContainer) _valueOrPrevious);
                        }
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
                            RejectOrCancelInternal(CreateRejectContainer(ref e, int.MinValue, this));
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
                    State = Promise.State.Resolved;
                    HandleWaiter(valueContainer);
                    ResolveProgressListener();

                    MaybeDispose();
                }

                internal void RejectOrCancelInternal(IValueContainer valueContainer)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    State = valueContainer.GetState();
                    HandleWaiter(valueContainer);
                    CancelProgressListener();

                    MaybeDispose();
                }

                internal void HandleSelf(IValueContainer valueContainer)
                {
                    Promise.State state = valueContainer.GetState();
                    State = state;
                    HandleWaiter(valueContainer);
                    HandleProgressListener(state);

                    MaybeDispose();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class PromiseDuplicate : PromiseBranch, ITreeHandleable
            {
                private PromiseDuplicate() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                internal static PromiseDuplicate GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseDuplicate>()
                        ?? new PromiseDuplicate();
                    promise.Reset();
                    return promise;
                }

                public override void Handle()
                {
                    HandleSelf((IValueContainer) _valueOrPrevious);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    owner.SuppressRejection = true;
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    State = valueContainer.GetState();
                    _idsAndRetains.InterlockedTryReleaseComplete();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseWaitPromise : PromiseBranch, IProgressListener
            {
                internal void WaitFor(Promise other)
                {
                    ThrowIfInPool(this);
                    ValidateReturn(other);
                    var _ref = other._target._ref;
                    if (_ref == null)
                    {
                        ResolveInternal(ResolveContainerVoid.GetOrCreate());
                    }
                    else
                    {
                        _ref.MarkAwaited(other._target._id);
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
                internal sealed override void AddWaiter(ITreeHandleable waiter)
                {
                    ThrowIfInPool(this);
                    // When this is completed, _state is set then _next is swapped, so we must reverse that process here.
                    _next = waiter;
                    Thread.MemoryBarrier(); // Make sure _state is read after _next is written.
                    if (State != Promise.State.Pending)
                    {
                        // Exchange and check for null to handle race condition with HandleWaiter on another thread.
                        waiter = Interlocked.Exchange(ref _next, null);
                        if (waiter != null)
                        {
                            waiter.MakeReadyFromSettled(this, (IValueContainer) _valueOrPrevious);
                        }
                    }
                    MaybeDispose();
                }

                private void HandleWaiter(IValueContainer valueContainer)
                {
                    ITreeHandleable waiter = Interlocked.Exchange(ref _next, null);
                    if (waiter != null)
                    {
                        waiter.MakeReady(this, valueContainer, ref _handleQueue);
                    }
                }

                private void ResolveInternal(IValueContainer valueContainer)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    State = Promise.State.Resolved;
                    HandleWaiter(valueContainer);
                    ResolveProgressListener();

                    MaybeDispose();
                }

                protected void RejectOrCancelInternal(IValueContainer valueContainer)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    State = valueContainer.GetState();
                    HandleWaiter(valueContainer);
                    CancelProgressListener();

                    MaybeDispose();
                }

                [MethodImpl(InlineOption)]
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

                [MethodImpl(InlineOption)]
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
                internal short DeferredId
                {
                    [MethodImpl(InlineOption)]
                    get { return _idsAndRetains._deferredId; }
                }

                protected DeferredPromiseBase() { }

                ~DeferredPromiseBase()
                {
                    if (State == Promise.State.Pending)
                    {
                        // Deferred wasn't handled.
                        AddRejectionToUnhandledStack(UnhandledDeferredException.instance, this);
                    }
                }

                protected virtual bool TryUnregisterCancelation() { return true; }

                protected bool TryIncrementDeferredIdAndUnregisterCancelation(short deferredId)
                {
                    return _idsAndRetains.InterlockedTryIncrementDeferredId(deferredId)
                        && TryUnregisterCancelation(); // If TryUnregisterCancelation returns false, it means the CancelationSource was canceled.
                }

                internal bool TryReject<TReject>(ref TReject reason, short deferredId, int rejectSkipFrames)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        RejectDirect(ref reason, rejectSkipFrames + 1);
                        return true;
                    }
                    return false;
                }

                internal bool TryCancel<TCancel>(ref TCancel reason, short deferredId)
                {
                    if (TryIncrementDeferredIdAndUnregisterCancelation(deferredId))
                    {
                        CancelDirect(ref reason);
                        return true;
                    }
                    return false;
                }

                internal bool TryCancel(short deferredId)
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
                    _idsAndRetains.InterlockedIncrementDeferredId();
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

                internal bool TryResolve(short deferredId)
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
                    var promise = ObjectPool<ITreeHandleable>.TryTake<DeferredPromiseVoid>()
                        ?? new DeferredPromiseVoid();
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

                internal bool TryResolve(ref T value, short deferredId)
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
                    var promise = ObjectPool<ITreeHandleable>.TryTake<DeferredPromise<T>>()
                        ?? new DeferredPromise<T>();
                    promise.Reset();
                    promise.ResetDepth();
                    return promise;
                }
            }

            // IDelegate to reduce the amount of classes I would have to write (Composition Over Inheritance).
            // Using generics with constraints allows us to use structs to get composition for "free"
            // (no extra object allocation or extra memory overhead, and the compiler will generate the Promise classes for us).
            // The only downside is that more classes are created than if we just used straight interfaces (not a problem with JIT, but makes the code size larger with AOT).

            // Resolve types for more common .Then(onResolved) calls to be more efficient (because the runtime does not allow 0-size structs).

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseResolve<TResolver> : PromiseBranch
                where TResolver : IDelegateResolve
            {
                private PromiseResolve() { }

                [MethodImpl(InlineOption)]
                internal static PromiseResolve<TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseResolve<TResolver>>()
                        ?? new PromiseResolve<TResolver>();
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
            private sealed partial class PromiseResolvePromise<TResolver> : PromiseWaitPromise
                where TResolver : IDelegateResolvePromise
            {
                private PromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                internal static PromiseResolvePromise<TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseResolvePromise<TResolver>>()
                        ?? new PromiseResolvePromise<TResolver>();
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
            private sealed partial class PromiseResolveReject<TResolver, TRejecter> : PromiseBranch
                where TResolver : IDelegateResolve
                where TRejecter : IDelegateReject
            {
                private PromiseResolveReject() { }

                [MethodImpl(InlineOption)]
                internal static PromiseResolveReject<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseResolveReject<TResolver, TRejecter>>()
                        ?? new PromiseResolveReject<TResolver, TRejecter>();
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
            private sealed partial class PromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise
                where TResolver : IDelegateResolvePromise
                where TRejecter : IDelegateRejectPromise
            {
                private PromiseResolveRejectPromise() { }

                [MethodImpl(InlineOption)]
                internal static PromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseResolveRejectPromise<TResolver, TRejecter>>()
                        ?? new PromiseResolveRejectPromise<TResolver, TRejecter>();
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
            private sealed partial class PromiseContinue<TContinuer> : PromiseBranch
                where TContinuer : IDelegateContinue
            {
                private PromiseContinue() { }

                [MethodImpl(InlineOption)]
                internal static PromiseContinue<TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseContinue<TContinuer>>()
                        ?? new PromiseContinue<TContinuer>();
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
            private sealed partial class PromiseContinuePromise<TContinuer> : PromiseWaitPromise
                where TContinuer : IDelegateContinuePromise
            {
                private PromiseContinuePromise() { }

                [MethodImpl(InlineOption)]
                internal static PromiseContinuePromise<TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseContinuePromise<TContinuer>>()
                        ?? new PromiseContinuePromise<TContinuer>();
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
            private sealed partial class PromiseFinally<TFinalizer> : PromiseBranch
                where TFinalizer : IDelegateSimple
            {
                private PromiseFinally() { }

                [MethodImpl(InlineOption)]
                internal static PromiseFinally<TFinalizer> GetOrCreate(TFinalizer finalizer)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseFinally<TFinalizer>>()
                        ?? new PromiseFinally<TFinalizer>();
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
            private sealed partial class PromiseCancel<TCanceler> : PromiseBranch, ITreeHandleable
                where TCanceler : IDelegateSimple
            {
                private PromiseCancel() { }

                [MethodImpl(InlineOption)]
                internal static PromiseCancel<TCanceler> GetOrCreate(TCanceler canceler)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseCancel<TCanceler>>()
                        ?? new PromiseCancel<TCanceler>();
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
            internal sealed partial class PromisePassThrough : ITreeHandleable, ILinked<PromisePassThrough>, IProgressListener
            {
                internal PromiseRef Owner
                {
                    [MethodImpl(InlineOption)]
                    get
                    {
                        ThrowIfInPool(this);
                        return _owner;
                    }
                }

                private PromisePassThrough() { }

                ~PromisePassThrough()
                {
                    if (_smallFields._retainCounter != 0)
                    {
                        // For debugging. This should never happen.
                        string message = "A PromisePassThrough was garbage collected without it being released."
#if CSHARP_7_3_OR_NEWER
                            + $" _retainCounter: {_smallFields._retainCounter}, _index: {_smallFields._index}, _target: {_target}, _owner: {_owner}"
#if PROMISE_PROGRESS
                            + $", _reportingProgress: {_smallFields._reportingProgress}, _settingInitialProgress: {_smallFields._settingInitialProgress}, _currentProgress: {_smallFields._currentProgress.ToDouble()}"
#endif
#endif
                            ;
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), _target as ITraceable);
                    }
                }

                internal static PromisePassThrough GetOrCreate(Promise owner, int index)
                {
                    // owner._ref is checked for nullity before passing into this.
                    owner._target._ref.MarkAwaited(owner._target._id);
                    var passThrough = ObjectPool<PromisePassThrough>.TryTake<PromisePassThrough>()
                        ?? new PromisePassThrough();
                    passThrough._owner = owner._target._ref;
                    passThrough._smallFields._index = index;
                    passThrough._smallFields._retainCounter = 1;
                    passThrough.ResetProgress();
                    return passThrough;
                }

                partial void ResetProgress();
                partial void WaitWhileProgressIsBusy();

                internal void SetTargetAndAddToOwner(IMultiTreeHandleable target)
                {
                    ThrowIfInPool(this);
                    _target = target;
                    // Unfortunately, we have to eagerly subscribe progress. Lazy algorithm would be much more expensive with thread safety, requiring allocations. (see ValidateReturn)
                    // But it's not so bad, because it doesn't allocate any memory (just uses CPU cycles to set it up).
                    _owner.AddWaiterWithProgress(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    ThrowIfInPool(this);
                    _owner = null;
                    WaitWhileProgressIsBusy();
                    if (_target.Handle(owner, valueContainer, this, _smallFields._index))
                    {
                        AddToHandleQueueFront(_target);
                    }
                    Release();
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    _owner = null;
                    WaitWhileProgressIsBusy();
                    if (_target.Handle(owner, valueContainer, this, _smallFields._index))
                    {
                        AddToHandleQueueBack(_target);
                    }
                    Release();
                }

                [MethodImpl(InlineOption)]
                internal void Retain()
                {
                    ThrowIfInPool(this);
                    int _;
                    // Don't let counter wrap around past 0.
                    if (!InterlockedAddIfNotEqual(ref _smallFields._retainCounter, 1, -1, out _))
                    {
                        throw new OverflowException();
                    }
                }

                internal void Release()
                {
                    ThrowIfInPool(this);
                    int newValue;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // Don't let counter go below 0.
                    if (!InterlockedAddIfNotEqual(ref _smallFields._retainCounter, -1, 0, out newValue))
                    {
                        throw new OverflowException(); // This should never happen, but checking just in case.
                    }
#else
                    newValue = Interlocked.Decrement(ref _smallFields._retainCounter);
#endif
                    if (newValue == 0)
                    {
                        _target = null;
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }
                }

                void ITreeHandleable.Handle() { throw new System.InvalidOperationException(); }
            } // PromisePassThrough

            private partial struct IdRetain
            {
                [MethodImpl(InlineOption)]
                internal bool InterlockedTryIncrementPromiseId(short promiseId)
                {
                    Thread.MemoryBarrier();
                    IdRetain initialValue = default(IdRetain), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches.
                        if (initialValue._promiseId != promiseId)
                        {
                            return false;
                        }
                        newValue = initialValue;
                        unchecked // We want the id to wrap around.
                        {
                            ++newValue._promiseId;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryIncrementDeferredId(short deferredId)
                {
                    Thread.MemoryBarrier();
                    IdRetain initialValue = default(IdRetain), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches.
                        if (initialValue._deferredId != deferredId)
                        {
                            return false;
                        }
                        newValue = initialValue;
                        unchecked // We want the id to wrap around.
                        {
                            ++newValue._deferredId;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedIncrementDeferredId()
                {
                    Thread.MemoryBarrier();
                    IdRetain initialValue = default(IdRetain), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
                        unchecked // We want the id to wrap around.
                        {
                            ++newValue._deferredId;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryRetain(short promiseId)
                {
                    Thread.MemoryBarrier();
                    IdRetain initialValue = default(IdRetain), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches and we're not overflowing.
                        if (initialValue._promiseId != promiseId | initialValue._retains == uint.MaxValue) // Use a single branch for fast-path.
                        {
                            // If either check fails, see which failed.
                            if (initialValue._promiseId != promiseId)
                            {
                                return false;
                            }
                            throw new OverflowException("A promise was retained more than " + uint.MaxValue + " times.");
                        }
                        newValue = initialValue;
                        unchecked
                        {
                            ++newValue._retains;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryRetainWithDeferredId(short deferredId)
                {
                    Thread.MemoryBarrier();
                    IdRetain initialValue = default(IdRetain), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches and we're not overflowing.
                        if (initialValue._deferredId != deferredId | initialValue._retains == uint.MaxValue) // Use a single branch for fast-path.
                        {
                            // If either check fails, see which failed.
                            if (initialValue._deferredId != deferredId)
                            {
                                return false;
                            }
                            throw new OverflowException("A promise was retained more than " + uint.MaxValue + " times.");
                        }
                        newValue = initialValue;
                        unchecked
                        {
                            ++newValue._retains;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryReleaseComplete()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    Thread.MemoryBarrier();
                    IdRetain initialValue = default(IdRetain), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
                        checked
                        {
                            --newValue._retains;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return newValue._retains == 0;
#else
                    unchecked
                    {
                        return InterlockedRetainDisregardId((uint) -1) == 0;
                    }
#endif
                }

                internal uint InterlockedRetainDisregardId(uint retains = 1)
                {
                    Thread.MemoryBarrier();
                    IdRetain initialValue = default(IdRetain), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        checked
#else
                        unchecked
#endif
                        {
                            newValue._retains += retains;
                        }
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return newValue._retains;
                }
            } // IdRetain

            internal static void MaybeMarkAwaitedAndDispose(Promise promise, bool suppressRejection)
            {
                if (promise._target._ref != null)
                {
                    promise._target._ref.MarkAwaitedAndMaybeDispose(promise._target._id, suppressRejection);
                }
            }
        } // PromiseRef

        internal static uint PrepareForMulti(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index)
        {
            if (promise._target._ref != null)
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
                return 1;
            }
            return 0;
        }

        internal static uint PrepareForMulti(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index, ref ulong completedProgress)
        {
            if (promise._target._ref != null)
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
    } // Internal
}