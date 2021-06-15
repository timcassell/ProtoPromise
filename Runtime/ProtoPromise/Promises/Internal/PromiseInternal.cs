﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
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
        internal readonly short _id;

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRef promiseRef, short id)
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
        internal readonly short _id;
        /// <summary>
        /// Internal use.
        /// </summary>
        internal readonly T _result;

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRef promiseRef, short id)
        {
            _ref = promiseRef;
            _id = id;
            _result = default(T);
        }

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal Promise(Internal.PromiseRef promiseRef, short id, ref T value)
        {
            _ref = promiseRef;
            _id = id;
            _result = value;
        }
    }

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
                    string message = "A Promise's resources were garbage collected without it being awaited. You must await, return, or forget each promise. Type: " + GetType();
                    AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
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
                WaitForProgressRetain();
            }

            void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
            {
                ThrowIfInPool(this);
                owner.SuppressRejection = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                AddToHandleQueueBack(this);
                WaitForProgressRetain();
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
                _smallFields.InterlockedUnsetProgressFlags(ProgressFlags.All);
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
            private void HookupNewCancelablePromiseWithProgress(PromiseRef newPromise, IProgressListener progressListener)
            {
                newPromise.SetDepth(this);
                if (Interlocked.CompareExchange(ref newPromise._valueOrPrevious, this, null) == null)
                {
                    if (State == Promise.State.Pending)
                    {
                        SubscribeListener(progressListener);
                    }
                    AddWaiter(newPromise);
                }
                else
                {
                    MaybeDispose();
                }
            }

            private void HookupNewPromiseWithProgress(PromiseRef newPromise, IProgressListener progressListener)
            {
                newPromise.SetDepth(this);
                newPromise._valueOrPrevious = this;
                if (State == Promise.State.Pending)
                {
                    SubscribeListener(progressListener);
                }
                AddWaiter(newPromise);
            }
#else
            [MethodImpl(InlineOption)]
            private void HookupNewCancelablePromiseWithProgress(PromiseRef newPromise, IProgressListener progressListener)
            {
                HookupNewCancelablePromise(newPromise);
            }

            [MethodImpl(InlineOption)]
            private void HookupNewPromiseWithProgress(PromiseRef newPromise, IProgressListener progressListener)
            {
                HookupNewPromise(newPromise);
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

            protected abstract bool TryRemoveWaiter(ITreeHandleable treeHandleable);

            protected abstract void AddWaiter(ITreeHandleable waiter);

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
                private struct Creator : ICreator<PromiseMultiAwait>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseMultiAwait Create()
                    {
                        return new PromiseMultiAwait();
                    }
                }

                private readonly object _branchLocker = new object();
                private ValueLinkedStack<ITreeHandleable> _nextBranches;

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
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseMultiAwait, Creator>();
                    promise.Reset();
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void MarkAwaited(short promiseId)
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

                internal override PromiseRef GetDuplicate(short promiseId)
                {
                    MarkAwaited(promiseId);
                    var newPromise = PromiseDuplicate.GetOrCreate();
                    HookupNewPromise(newPromise);
                    return newPromise;
                }

                protected override bool TryRemoveWaiter(ITreeHandleable treeHandleable)
                {
                    lock (_branchLocker)
                    {
                        return _nextBranches.TryRemove(treeHandleable);
                    }
                }

                protected override void AddWaiter(ITreeHandleable waiter)
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
                private ITreeHandleable _waiter;

                protected sealed override bool TryRemoveWaiter(ITreeHandleable treeHandleable)
                {
                    return Interlocked.CompareExchange(ref _waiter, null, treeHandleable) == treeHandleable;
                }

                protected sealed override void AddWaiter(ITreeHandleable waiter)
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
                    State = valueContainer.GetState();
                    HandleWaiter(valueContainer);
                    HandleProgressListener(State);

                    MaybeDispose();
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
                internal static PromiseDuplicate GetOrCreate()
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseDuplicate, Creator>();
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
            internal abstract partial class PromiseWaitPromise : PromiseBranch, IProgressListener
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
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromiseVoid, Creator>();
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
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<DeferredPromise<T>, Creator>();
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
                internal static PromiseResolve<TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolve<TResolver>, Creator>();
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
                internal static PromiseResolvePromise<TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolvePromise<TResolver>, Creator>();
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
                internal static PromiseResolveReject<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolveReject<TResolver, TRejecter>, Creator>();
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
                internal static PromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseResolveRejectPromise<TResolver, TRejecter>, Creator>();
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
                internal static PromiseContinue<TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseContinue<TContinuer>, Creator>();
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
                internal static PromiseContinuePromise<TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseContinuePromise<TContinuer>, Creator>();
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
                internal static PromiseFinally<TFinalizer> GetOrCreate(TFinalizer finalizer)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseFinally<TFinalizer>, Creator>();
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
                internal static PromiseCancel<TCanceler> GetOrCreate(TCanceler canceler)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseCancel<TCanceler>, Creator>();
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

                private PromiseRef _owner;
                private IMultiTreeHandleable _target;
                private int _index;
                private int _retainCounter;

                private PromisePassThrough() { }

                ~PromisePassThrough()
                {
                    if (_retainCounter != 0)
                    {
                        string message = "A PromisePassThrough was garbage collected without it being released.";
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), _target as ITraceable);
                    }
                }

                internal static PromisePassThrough GetOrCreate(Promise owner, int index)
                {
                    // owner._ref is checked for nullity before passing into this.
                    owner._ref.MarkAwaited(owner._id);
                    var passThrough = ObjectPool<PromisePassThrough>.GetOrCreate<PromisePassThrough, Creator>();
                    passThrough._owner = owner._ref;
                    passThrough._index = index;
                    passThrough._retainCounter = 2;
                    passThrough.ResetProgress();
                    return passThrough;
                }

                partial void ResetProgress();

                internal void SetTargetAndAddToOwner(IMultiTreeHandleable target)
                {
                    ThrowIfInPool(this);
                    _target = target;
                    _owner.AddWaiter(this);
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedQueue<ITreeHandleable> handleQueue)
                {
                    ThrowIfInPool(this);
                    if (_target.Handle(valueContainer, this, _index))
                    {
                        AddToHandleQueueFront(_target);
                    }
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    if (_target.Handle(valueContainer, this, _index))
                    {
                        AddToHandleQueueBack(_target);
                    }
                }

                [MethodImpl(InlineOption)]
                internal void Retain()
                {
                    ThrowIfInPool(this);
                    int _;
                    // Don't let counter wrap around past 0.
                    if (!InterlockedAddIfNotEqual(ref _retainCounter, 1, -1, out _))
                    {
                        throw new OverflowException();
                    }
                }

                [MethodImpl(InlineOption)]
                internal void Release()
                {
                    Release2(-1);
                }

                internal void Release2(int addRetains)
                {
                    ThrowIfInPool(this);
                    if (Interlocked.Add(ref _retainCounter, addRetains) == 0)
                    {
                        _owner = null;
                        _target = null;
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }
                }

                internal bool TryRemoveFromOwner()
                {
                    ThrowIfInPool(this);
                    TryUnsubscribeProgressAndRelease();
                    return _owner.TryRemoveWaiter(this);
                }

                partial void TryUnsubscribeProgressAndRelease();

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