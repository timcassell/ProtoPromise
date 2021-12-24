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
                get { return _smallFields._promiseId; }
            }

            internal Promise.State State
            {
                [MethodImpl(InlineOption)]
                get { return _smallFields._state; }
                [MethodImpl(InlineOption)]
                private set { _smallFields._state = value; }
            }

            // TODO: SuppressRejection can be set simultaneously with WasAwaitedOrForgotten when the promise is awaited (not forgotten).
            private bool SuppressRejection
            {
                [MethodImpl(InlineOption)]
                get { return _smallFields.AreFlagsSet(PromiseFlags.SuppressRejection); }
                [MethodImpl(InlineOption)]
                set
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (!value)
                    {
                        throw new System.InvalidOperationException("Cannot unset SuppressRejection via the property.");
                    }
#endif
                    _smallFields.InterlockedSetFlags(PromiseFlags.SuppressRejection);
                }
            }

            // TODO: WasAwaitedOrForgotten can be set simultaneously with IncrementDeferredId or Retain when the promise is awaited or forgotten.
            private bool WasAwaitedOrForgotten
            {
                [MethodImpl(InlineOption)]
                get { return _smallFields.AreFlagsSet(PromiseFlags.WasAwaitedOrForgotten); }
                [MethodImpl(InlineOption)]
                set
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (!value)
                    {
                        throw new System.InvalidOperationException("Cannot unset WasAwaitedOrForgotten via the property.");
                    }
#endif
                    _smallFields.InterlockedSetFlags(PromiseFlags.WasAwaitedOrForgotten);
                }
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
                    // Rejection maybe wasn't caught.
                    ((IValueContainer) _valueOrPrevious).ReleaseAndMaybeAddToUnhandledStack(!SuppressRejection);
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
                OnForgetOrHookupFailed();
            }

            private void IncrementId(short promiseId)
            {
                if (!_smallFields.InterlockedTryIncrementPromiseId(promiseId))
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
                if (!_smallFields.InterlockedTryRetain(promiseId))
                {
                    // Public APIs do a simple validation check in DEBUG mode, this is an extra thread-safe validation in case the same object is concurrently used and/or forgotten at the same time.
                    // This is left in RELEASE mode because concurrency issues can be very difficult to track down, and might not show up in DEBUG mode.
                    throw new InvalidOperationException("Attempted to use an invalid Promise. This may be because you are attempting to use a promise after it was forgotten.",
                        GetFormattedStacktrace(1));
                }
                ThrowIfInPool(this);
            }

            [MethodImpl(InlineOption)]
            protected void InterlockedRetainDisregardId()
            {
                ThrowIfInPool(this);
                _smallFields.InterlockedRetainDisregardId();
            }

            void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
            {
                ThrowIfInPool(this);
                owner.SuppressRejection = true;
                valueContainer.Retain();
                _valueOrPrevious = valueContainer;
                executionScheduler.ScheduleSynchronous(this);
                WaitWhileProgressFlags(PromiseFlags.Subscribing);
            }

            protected void Reset()
            {
                _smallFields.Reset();
                SetCreatedStacktrace(this, 3);
            }

            protected void MaybeDispose()
            {
                ThrowIfInPool(this);
                if (_smallFields.InterlockedTryReleaseComplete())
                {
                    Dispose();
                }
            }

            protected virtual void Dispose()
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (State == Promise.State.Pending)
                {
                    throw new System.InvalidOperationException("Promise disposed while pending.");
                }
#endif
                // Rejection maybe wasn't caught.
                ((IValueContainer) _valueOrPrevious).ReleaseAndMaybeAddToUnhandledStack(!SuppressRejection);
                _valueOrPrevious = null;
            }

            private void HookupNewCancelablePromise(PromiseRef newPromise)
            {
                // If _valueOrPrevious is not null, it means newPromise was already canceled from the token.
                if (Interlocked.CompareExchange(ref newPromise._valueOrPrevious, this, null) == null)
                {
                    HookupNewWaiter(newPromise);
                }
                else
                {
                    newPromise.OnHookupFailed();
                    SuppressRejection = true; // Don't report rejection if newPromise is already canceled.
                    OnForgetOrHookupFailed();
                }
            }

            // This is only overloaded by cancelable promises.
            protected virtual void OnHookupFailed() { throw new System.InvalidOperationException(); }

            protected virtual void OnForgetOrHookupFailed()
            {
                MaybeDispose();
            }

            private void HookupNewPromise(PromiseRef newPromise)
            {
                newPromise._valueOrPrevious = this;
                HookupNewWaiter(newPromise);
            }

            private void HookupNewWaiter(ITreeHandleable newWaiter)
            {
                ExecutionScheduler executionScheduler = new ExecutionScheduler(true);
                AddWaiter(newWaiter, ref executionScheduler);
                executionScheduler.Execute();
            }

#if PROMISE_PROGRESS
            private void HookupNewPromiseWithProgress<TPromiseRef>(TPromiseRef newPromise, int depth) where TPromiseRef : PromiseRef, IProgressListener
            {
                newPromise._valueOrPrevious = this;
                HookupNewWaiterWithProgress(newPromise, depth);
            }

            private void HookupNewWaiterWithProgress<TWaiter>(TWaiter newWaiter, int depth) where TWaiter : ITreeHandleable, IProgressListener
            {
                ExecutionScheduler executionScheduler = new ExecutionScheduler(true);
                SubscribeListener(newWaiter, new Fixed32(depth), ref executionScheduler);
                AddWaiter(newWaiter, ref executionScheduler);
                executionScheduler.Execute();
            }
#endif

            internal PromiseRef GetPreserved(short promiseId, int depth)
            {
                MarkAwaited(promiseId);
                var newPromise = PromiseMultiAwait.GetOrCreate(depth);
                HookupNewPromise(newPromise);
                return newPromise;
            }

            internal virtual ConfiguredPromise GetConfigured(short promiseId, SynchronizationContext synchronizationContext)
            {
                MarkAwaited(promiseId);
                var newPromise = ConfiguredPromise.GetOrCreate(false, synchronizationContext);
                HookupNewPromise(newPromise);
                return newPromise;
            }

            public abstract void Handle(ref ExecutionScheduler executionScheduler); // ITreeHandleable.Handle()

            internal abstract PromiseRef GetDuplicate(short promiseId);

            internal abstract void AddWaiter(ITreeHandleable waiter, ref ExecutionScheduler executionScheduler);

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

                internal override void AddWaiter(ITreeHandleable waiter, ref ExecutionScheduler executionScheduler)
                {
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        ThrowIfInPool(this);
                        // When this is completed, State is set then _waiter is swapped, so we must reverse that process here.
                        Thread.MemoryBarrier();
                        _waiter = waiter;
                        Thread.MemoryBarrier(); // Make sure State is read after _waiter is written.
                        if (State != Promise.State.Pending)
                        {
                            // Exchange and check for null to handle race condition with HandleWaiter on another thread.
                            waiter = Interlocked.Exchange(ref _waiter, null);
                            if (waiter != null)
                            {
                                waiter.MakeReady(this, (IValueContainer) _valueOrPrevious, ref executionScheduler);
                            }
                        }
                        MaybeDispose();
                    }
                }

                internal void HandleWaiter(IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        ITreeHandleable waiter = Interlocked.Exchange(ref _waiter, null);
                        if (waiter != null)
                        {
                            waiter.MakeReady(this, valueContainer, ref executionScheduler);
                        }
                    }
                }

                public override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    // TODO: refactor to reduce this from 2 virtual calls to 1.
                    ThrowIfInPool(this);
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    bool invokingRejected = false;
                    bool suppressRejection = false;
                    SetCurrentInvoker(this);
                    try
                    {
                        // valueContainer is released deeper in the call stack, so we only release it in this method if an exception is thrown.
                        // (This is in case it is canceled, the valueContainer will be released from the cancelation.)
                        Execute(ref executionScheduler, valueContainer, ref invokingRejected, ref suppressRejection);
                    }
                    catch (RethrowException e)
                    {
                        if (invokingRejected || (e is ForcedRethrowException && valueContainer.GetState() != Promise.State.Resolved))
                        {
                            RejectOrCancelInternal(valueContainer, ref executionScheduler);
                            valueContainer.Release(); // Must release since RejectOrCancelInternal adds an extra retain.
                        }
                        else
                        {
                            valueContainer.ReleaseAndMaybeAddToUnhandledStack(true);
                            RejectOrCancelInternal(CreateRejectContainer(e, int.MinValue, this), ref executionScheduler);
                        }
                    }
                    catch (OperationCanceledException e)
                    {
                        valueContainer.ReleaseAndMaybeAddToUnhandledStack(!suppressRejection);
                        RejectOrCancelInternal(CreateCancelContainer(e), ref executionScheduler);
                    }
                    catch (Exception e)
                    {
                        valueContainer.ReleaseAndMaybeAddToUnhandledStack(!suppressRejection);
                        RejectOrCancelInternal(CreateRejectContainer(e, int.MinValue, this), ref executionScheduler);
                    }
                    ClearCurrentInvoker();
                }

                protected virtual void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection) { }

                internal virtual void ResolveInternal(IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    State = Promise.State.Resolved;
                    HandleWaiter(valueContainer, ref executionScheduler);

                    MaybeDispose();
                }

                internal virtual void RejectOrCancelInternal(IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    State = valueContainer.GetState();
                    HandleWaiter(valueContainer, ref executionScheduler);

                    MaybeDispose();
                }

                internal virtual void HandleSelf(IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    State = valueContainer.GetState();
                    HandleWaiter(valueContainer, ref executionScheduler);

                    MaybeDispose();
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
                internal static PromiseMultiAwait GetOrCreate(int depth)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseMultiAwait>()
                        ?? new PromiseMultiAwait();
                    promise.Reset(depth);
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
                    var newPromise = ConfiguredPromise.GetOrCreate(true, null);
                    HookupNewPromise(newPromise);
                    return newPromise;
                }

                internal override void AddWaiter(ITreeHandleable waiter, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    if (State == Promise.State.Pending)
                    {
                        _progressAndLocker._branchLocker.Enter();
                        if (State == Promise.State.Pending)
                        {
#if PROTO_PROMISE_DEVELOPER_MODE
                            _nextBranches.Enqueue(waiter);
#else
                            _nextBranches.Push(waiter);
#endif
                            _progressAndLocker._branchLocker.Exit();
                            MaybeDispose();
                            return;
                        }
                        _progressAndLocker._branchLocker.Exit();
                    }
                    waiter.MakeReady(this, (IValueContainer) _valueOrPrevious, ref executionScheduler);
                    MaybeDispose();
                }

                public override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    Promise.State state = valueContainer.GetState();
                    State = state;

                    HandleBranches(valueContainer, ref executionScheduler);
                    HandleProgressListeners(state, ref executionScheduler);

                    MaybeDispose();
                }

                private void HandleBranches(IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    _progressAndLocker._branchLocker.Enter();
#if PROTO_PROMISE_DEVELOPER_MODE
                    var branches = _nextBranches.MoveElementsToStack();
#else
                    var branches = _nextBranches;
                    _nextBranches = new ValueLinkedStack<ITreeHandleable>();
#endif
                    _progressAndLocker._branchLocker.Exit();
                    while (branches.IsNotEmpty)
                    {
                        branches.Pop().MakeReady(this, valueContainer, ref executionScheduler);
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
            internal abstract partial class PromiseSingleAwaitWithProgress : PromiseSingleAwait
            {
                internal override sealed void ResolveInternal(IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    State = Promise.State.Resolved;
                    HandleWaiter(valueContainer, ref executionScheduler);
                    HandleProgressListener(Promise.State.Resolved, ref executionScheduler);

                    MaybeDispose();
                }

                internal override sealed void RejectOrCancelInternal(IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    var state = valueContainer.GetState();
                    State = state;
                    HandleWaiter(valueContainer, ref executionScheduler);
                    HandleProgressListener(state, ref executionScheduler);

                    MaybeDispose();
                }

                internal override sealed void HandleSelf(IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    Promise.State state = valueContainer.GetState();
                    State = state;
                    HandleWaiter(valueContainer, ref executionScheduler);
                    HandleProgressListener(state, ref executionScheduler);

                    MaybeDispose();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class ConfiguredPromise : PromiseSingleAwait, ITreeHandleable
            {
                private ConfiguredPromise() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                internal static ConfiguredPromise GetOrCreate(bool isSynchronous, SynchronizationContext synchronizationContext)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<ConfiguredPromise>()
                        ?? new ConfiguredPromise();
                    promise.Reset();
                    promise._synchronizationContext = synchronizationContext;
                    promise._isSynchronous = isSynchronous;
                    promise._mostRecentPotentialScheduleMethod = (int) ScheduleMethod.None;
                    promise._wasForgottenOrHookupFailed = false;
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal static ConfiguredPromise GetOrCreateFromNull<TResult>(bool isSynchronous, SynchronizationContext synchronizationContext,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult result)
                {
                    var promise = GetOrCreate(isSynchronous, synchronizationContext);
                    promise._valueOrPrevious = CreateResolveContainer(result, 1);
                    promise._mostRecentPotentialScheduleMethod = (int) ScheduleMethod.MakeReady;
                    return promise;
                }

                internal override ConfiguredPromise GetConfigured(short promiseId, SynchronizationContext synchronizationContext)
                {
                    IncrementId(promiseId);
                    _isSynchronous = false;
                    _synchronizationContext = synchronizationContext;
                    return this;
                }

                [MethodImpl(InlineOption)]
                public override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    HandleSelf((IValueContainer) _valueOrPrevious, ref executionScheduler);
                }

                internal override void AddWaiter(ITreeHandleable waiter, ref ExecutionScheduler executionScheduler)
                {
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        ThrowIfInPool(this);
                        ScheduleMethod previousScheduleType = (ScheduleMethod) Interlocked.Exchange(ref _mostRecentPotentialScheduleMethod, (int) ScheduleMethod.AddWaiter);
                        // When this is completed, State is set then _waiter is swapped, so we must reverse that process here.
                        _waiter = waiter;
                        Thread.MemoryBarrier(); // Make sure State is read after _waiter is written.
                        if (State != Promise.State.Pending)
                        {
                            // Exchange and check for null to handle race condition with HandleWaiter on another thread.
                            waiter = Interlocked.Exchange(ref _waiter, null);
                            if (waiter != null)
                            {
                                waiter.MakeReady(this, (IValueContainer) _valueOrPrevious, ref executionScheduler);
                            }
                        }
                        else if (previousScheduleType == ScheduleMethod.MakeReady)
                        {
                            if (_isSynchronous)
                            {
                                executionScheduler.ScheduleSynchronous(this);
                                //AddToHandleQueueFront(this);
                            }
                            else
                            {
                                executionScheduler.ScheduleOnContext(_synchronizationContext, this);
                            }
                        }
                        MaybeDispose();
                    }
                }

                protected override void OnForgetOrHookupFailed()
                {
                    _wasForgottenOrHookupFailed = true;
                    Thread.MemoryBarrier(); // Make sure _mostRecentPotentialScheduleMethod is read after _wasForgottenOrHookupFailed is written.
                    if ((ScheduleMethod) _mostRecentPotentialScheduleMethod == ScheduleMethod.MakeReady)
                    {
                        _smallFields.InterlockedTryReleaseComplete();
                    }
                    base.OnForgetOrHookupFailed();
                }

                // TODO: Transition state to complete when this is created from Promise.GetAwaiter() and _isSynchronous is true and previous was already complete.
                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    owner.SuppressRejection = true;
                    ScheduleMethod previousScheduleType = (ScheduleMethod) Interlocked.Exchange(ref _mostRecentPotentialScheduleMethod, (int) ScheduleMethod.MakeReady);
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    Thread.MemoryBarrier();
                    // Leave pending until this is awaited or forgotten.
                    if (previousScheduleType == ScheduleMethod.AddWaiter)
                    {
                        if (_isSynchronous)
                        {
                            executionScheduler.ScheduleSynchronous(this);
                            //AddToHandleQueueFront(this);
                        }
                        else
                        {
                            executionScheduler.ScheduleOnContext(_synchronizationContext, this);
                        }
                    }
                    else if (_wasForgottenOrHookupFailed)
                    {
                        executionScheduler.ScheduleSynchronous(this);
                    }
                    WaitWhileProgressFlags(PromiseFlags.Subscribing);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseWaitPromise : PromiseSingleAwaitWithProgress, IProgressListener
            {
                [MethodImpl(InlineOption)]
                internal void WaitFor<T>(Promise<T> other, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValidateReturn(other);
                    var _ref = other._ref;
                    if (_ref == null)
                    {
                        ResolveInternal(CreateResolveContainer(other.Result, 0), ref executionScheduler);
                    }
                    else
                    {
                        _ref.MarkAwaited(other.Id);
                        _valueOrPrevious = _ref;
                        SubscribeProgressToOther(_ref, other.Depth, ref executionScheduler);
                        _ref.AddWaiter(this, ref executionScheduler);
                    }
                }

                partial void SubscribeProgressToOther(PromiseRef other, int depth, ref ExecutionScheduler executionScheduler);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class AsyncPromiseBase : PromiseSingleAwaitWithProgress
            {
                private void ResolveInternal(IValueContainer valueContainer)
                {
                    ExecutionScheduler executionScheduler = new ExecutionScheduler(true);
                    ResolveInternal(valueContainer, ref executionScheduler);
                    executionScheduler.Execute();
                }

                protected void RejectOrCancelInternal(IValueContainer valueContainer)
                {
                    ExecutionScheduler executionScheduler = new ExecutionScheduler(true);
                    RejectOrCancelInternal(valueContainer, ref executionScheduler);
                    executionScheduler.Execute();
                }

                [MethodImpl(InlineOption)]
                protected void ResolveDirect<T>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    T value)
                {
                    ThrowIfInPool(this);
                    ResolveInternal(CreateResolveContainer(value, 0));
                }

                protected void RejectDirect<TReject>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TReject reason, int rejectSkipFrames)
                {
                    ThrowIfInPool(this);
                    RejectOrCancelInternal(CreateRejectContainer(reason, rejectSkipFrames + 1, this));
                }

                [MethodImpl(InlineOption)]
                protected void CancelDirect()
                {
                    ThrowIfInPool(this);
                    RejectOrCancelInternal(CancelContainerVoid.GetOrCreate(0));
                }

                protected void CancelDirect<TCancel>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TCancel reason)
                {
                    ThrowIfInPool(this);
                    RejectOrCancelInternal(CreateCancelContainer(reason));
                }

                public sealed override void Handle(ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
            }

            // IDelegate to reduce the amount of classes I would have to write (Composition Over Inheritance).
            // Using generics with constraints allows us to use structs to get composition for "free"
            // (no extra object allocation or extra memory overhead, and the compiler will generate the Promise classes for us).
            // The only downside is that more classes are created than if we just used straight interfaces (not a problem with JIT, but makes the code size larger with AOT).

            // Resolve types for more common .Then(onResolved) calls to be more efficient (because the runtime does not allow 0-size structs).

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseResolve<TResolver> : PromiseSingleAwait
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

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref executionScheduler);
                        //TArg arg = valueContainer.GetValue<TArg>();
                        //TResult result = resolveCallback.Invoke(arg);
                        //valueContainer.Release();
                        //ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                        //return;
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
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
                internal static PromiseResolvePromise<TResolver> GetOrCreate(TResolver resolver, int depth)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseResolvePromise<TResolver>>()
                        ?? new PromiseResolvePromise<TResolver>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (valueContainer.GetState() == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref executionScheduler);
                        //TArg arg = valueContainer.GetValue<TArg>();
                        //Promise<TResult> result = resolveCallback.Invoke(arg);
                        //WaitFor(result, ref executionScheduler);
                        //valueContainer.Release();
                        //return;
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseResolveReject<TResolver, TRejecter> : PromiseSingleAwait
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

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    _rejecter = default(TRejecter);
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref executionScheduler);
                        //TArgResolve arg = valueContainer.GetValue<TArgResolve>();
                        //TResult result = resolveCallback.Invoke(arg);
                        //valueContainer.Release();
                        //ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                        //return;
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        suppressRejection = true;
                        rejectCallback.InvokeRejecter(valueContainer, this, ref executionScheduler);
                        //TArgReject arg;
                        //if (valueContainer.TryGetValue(out arg))
                        //{
                        //    TResult result = rejectCallback.Invoke(arg);
                        //    valueContainer.Release();
                        //    ResolveInternal(CreateResolveContainer(result, 0), ref executionScheduler);
                        //    return;
                        //}
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
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
                internal static PromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, int depth)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseResolveRejectPromise<TResolver, TRejecter>>()
                        ?? new PromiseResolveRejectPromise<TResolver, TRejecter>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    _rejecter = default(TRejecter);
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(valueContainer, this, ref executionScheduler);
                        //TArgResolve arg = valueContainer.GetValue<TArgResolve>();
                        //Promise<TResult> result = resolveCallback.Invoke(arg);
                        //WaitFor(result, ref executionScheduler);
                        //valueContainer.Release();
                        //return;
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        suppressRejection = true;
                        rejectCallback.InvokeRejecter(valueContainer, this, ref executionScheduler);
                        //TArgReject arg;
                        //if (valueContainer.TryGetValue(out arg))
                        //{
                        //    Promise<TResult> result = rejectCallback.Invoke(arg);
                        //    WaitFor(result, ref executionScheduler);
                        //    valueContainer.Release();
                        //    return;
                        //}
                    }
                    else
                    {
                        RejectOrCancelInternal(valueContainer, ref executionScheduler);
                        valueContainer.Release();
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseContinue<TContinuer> : PromiseSingleAwait
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

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    suppressRejection = true;
                    callback.Invoke(valueContainer, this, ref executionScheduler);
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
                internal static PromiseContinuePromise<TContinuer> GetOrCreate(TContinuer continuer, int depth)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseContinuePromise<TContinuer>>()
                        ?? new PromiseContinuePromise<TContinuer>();
                    promise.Reset(depth);
                    promise._continuer = continuer;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(valueContainer, ref executionScheduler);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    suppressRejection = true;
                    callback.Invoke(valueContainer, this, ref executionScheduler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseFinally<TFinalizer> : PromiseSingleAwait
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

                protected override void Execute(ref ExecutionScheduler executionScheduler, IValueContainer valueContainer, ref bool invokingRejected, ref bool suppressRejection)
                {
                    var callback = _finalizer;
                    _finalizer = default(TFinalizer);
                    callback.Invoke(valueContainer);
                    HandleSelf(valueContainer, ref executionScheduler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseCancel<TCanceler> : PromiseSingleAwait, ITreeHandleable
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

                public override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;

                    if (valueContainer.GetState() != Promise.State.Canceled)
                    {
                        HandleSelf(valueContainer, ref executionScheduler);
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

                    HandleSelf(valueContainer, ref executionScheduler);
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

                internal int Index
                {
                    [MethodImpl(InlineOption)]
                    get
                    {
                        ThrowIfInPool(this);
                        return _smallFields._index;
                    }
                }

                private PromisePassThrough() { }

                ~PromisePassThrough()
                {
                    if (_smallFields._retainCounter != 0)
                    {
                        // For debugging. This should never happen.
                        string message = "A PromisePassThrough was garbage collected without it being released."
                            + " _retainCounter: " + _smallFields._retainCounter + ", _index: " + _smallFields._index + ", _target: " + _target + ", _owner: " + _owner
#if PROMISE_PROGRESS
                            + ", _reportingProgress: " + _smallFields._reportingProgress + ", _settingInitialProgress: " + _smallFields._settingInitialProgress + ", _currentProgress: " + _smallFields._currentProgress.ToDouble()
#endif
                            ;
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), _target as ITraceable);
                    }
                }

                internal static PromisePassThrough GetOrCreate(Promise owner, int index)
                {
                    // owner._ref is checked for nullity before passing into this.
                    owner._target._ref.MarkAwaited(owner._target.Id);
                    var passThrough = ObjectPool<PromisePassThrough>.TryTake<PromisePassThrough>()
                        ?? new PromisePassThrough();
                    passThrough._owner = owner._target._ref;
                    passThrough._smallFields._index = index;
                    passThrough._smallFields._retainCounter = 1;
                    passThrough.ResetProgress(owner._target.Depth);
                    return passThrough;
                }

                partial void ResetProgress(int depth);
                partial void WaitWhileProgressIsBusy();

                internal void SetTargetAndAddToOwner(IMultiTreeHandleable target)
                {
                    ThrowIfInPool(this);
                    _target = target;
#if PROMISE_PROGRESS
                    // Unfortunately, we have to eagerly subscribe progress. Lazy algorithm would be much more expensive with thread safety, requiring allocations. (see ValidateReturn)
                    // But it's not so bad, because it doesn't allocate any memory (just uses CPU cycles to set it up).
                    _owner.HookupNewWaiterWithProgress(this, _smallFields._depth.WholePart);
#else
                    _owner.HookupNewWaiter(this);
#endif
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _owner = null;
                    WaitWhileProgressIsBusy();
                    _target.Handle(owner, valueContainer, this, ref executionScheduler);
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

                void ITreeHandleable.Handle(ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
            } // PromisePassThrough

            partial struct SmallFields
            {
                [MethodImpl(InlineOption)]
                internal bool InterlockedTryIncrementPromiseId(short promiseId)
                {
                    Thread.MemoryBarrier();
                    SmallFields initialValue = default(SmallFields), newValue;
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
                    SmallFields initialValue = default(SmallFields), newValue;
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
                    SmallFields initialValue = default(SmallFields), newValue;
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
                    SmallFields initialValue = default(SmallFields), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches and we're not overflowing.
                        if (initialValue._promiseId != promiseId | initialValue._retains == ushort.MaxValue) // Use a single branch for fast-path.
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
                    SmallFields initialValue = default(SmallFields), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        // Make sure id matches and we're not overflowing.
                        if (initialValue._deferredId != deferredId | initialValue._retains == ushort.MaxValue) // Use a single branch for fast-path.
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
                    unchecked
                    {
                        // Adding -1 casted to ushort works the same as subtracting 1.
                        return InterlockedRetainDisregardId((ushort) -1) == 0;
                    }
                }

                internal ushort InterlockedRetainDisregardId(ushort retains = 1)
                {
                    unchecked
                    {
                        Thread.MemoryBarrier();
                        SmallFields initialValue = default(SmallFields), newValue;
                        do
                        {
                            initialValue._longValue = Interlocked.Read(ref _longValue);
                            newValue = initialValue;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                            bool adding = (short) retains > 0;
                            if (newValue._retains == 0 && adding)
                            {
                                throw new System.InvalidOperationException("Cannot retain after full release");
                            }
                            // We add ushort to subtract, so we do manual overflow checking.
                            if ((adding && newValue._retains > (ushort) (ushort.MaxValue - retains))
                                || (!adding && newValue._retains < (ushort) (ushort.MinValue - retains)))
                            {
                                throw new OverflowException();
                            }
#endif
                            newValue._retains += retains;
                        } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                        return newValue._retains;
                    }
                }

                [MethodImpl(InlineOption)]
                internal void Reset()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (_retains != 0)
                    {
                        throw new System.InvalidOperationException("Expected 0 retains, actual retains: " + _retains);
                    }
#endif
                    _state = Promise.State.Pending;
                    _flags = PromiseFlags.None;
                    // Set retain counter to 2.
                    // 1 retain for state, 1 retain for await/forget.
                    _retains = 2;
                }

                internal PromiseFlags InterlockedSetFlags(PromiseFlags progressFlags)
                {
                    Thread.MemoryBarrier();
                    SmallFields initialValue = default(SmallFields), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
                        newValue._flags |= progressFlags;
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return initialValue._flags;
                }

                internal PromiseFlags InterlockedUnsetFlags(PromiseFlags progressFlags)
                {
                    Thread.MemoryBarrier();
                    SmallFields initialValue = default(SmallFields), newValue;
                    PromiseFlags unsetFlags = ~progressFlags;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
                        newValue._flags &= unsetFlags;
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return initialValue._flags;
                }

                [MethodImpl(InlineOption)]
                internal bool AreFlagsSet(PromiseFlags progressFlags)
                {
                    return (_flags & progressFlags) != 0;
                }
            } // SmallFields

            internal static void MaybeMarkAwaitedAndSuppressRejectionAndDispose(PromiseRef promise, short id)
            {
                if (promise != null)
                {
                    promise.MarkAwaited(id);
                    promise.SuppressRejection = true;
                    promise.MaybeDispose();
                }
            }

            internal static void MaybeMarkAwaitedAndDispose(PromiseRef promise, short id)
            {
                if (promise != null)
                {
                    promise.MarkAwaited(id);
                    promise.MaybeDispose();
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
            value = promise.Result;
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
            value = promise.Result;
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
            return new Promise(null, ValidIdFromApi, 0);
#endif
        }

        [MethodImpl(InlineOption)]
        internal static Promise<T> CreateResolved<T>(
#if CSHARP_7_3_OR_NEWER
            in
#endif
            T value)
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging in the finalizer.
            var deferred = Promise.NewDeferred<T>();
            deferred.Resolve(value);
            return deferred.Promise;
#else
            // Make a promise on the stack for efficiency.
            return new Promise<T>(null, ValidIdFromApi, 0, value);
#endif
        }
    } // Internal
}