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
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

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
        internal abstract partial class PromiseRef : HandleablePromiseBase, ITraceable
        {
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

            internal ushort Depth
            {
                [MethodImpl(InlineOption)]
                get { return _smallFields._depth; }
            }

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

            private bool WasAwaitedOrForgotten
            {
                [MethodImpl(InlineOption)]
                get { return _smallFields.AreFlagsSet(PromiseFlags.WasAwaitedOrForgotten); }
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
                    ((ValueContainer) _valueOrPrevious).ReleaseAndMaybeAddToUnhandledStack(!SuppressRejection);
                }
            }

            protected abstract void MarkAwaited(short promiseId, PromiseFlags flags);

            internal void Forget(short promiseId)
            {
                IncrementIdAndSetFlags(promiseId, PromiseFlags.WasAwaitedOrForgotten);
                OnForgetOrHookupFailed();
                MaybeReportUnhandledRejections();
            }

            internal void IncrementIdAndSetFlags(short promiseId, PromiseFlags flags)
            {
                if (!_smallFields.InterlockedTryIncrementPromiseIdAndSetFlags(promiseId, flags))
                {
                    // Public APIs do a simple validation check in DEBUG mode, this is an extra thread-safe validation in case the same object is concurrently used and/or forgotten at the same time.
                    // This is left in RELEASE mode because concurrency issues can be very difficult to track down, and might not show up in DEBUG mode.
                    throw new InvalidOperationException("Attempted to use an invalid Promise. This may be because you are attempting to use a promise simultaneously on multiple threads that you have not preserved.",
                        GetFormattedStacktrace(3));
                }
                ThrowIfInPool(this);
            }

            private void InterlockedRetainAndSetFlagsInternal(short promiseId, PromiseFlags flags)
            {
                if (!_smallFields.InterlockedTryRetainAndSetFlags(promiseId, flags))
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

            protected void Reset(ushort depth)
            {
                _smallFields.Reset(depth);
                SetCreatedStacktrace(this, 3);
            }

            internal void MaybeDispose()
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
                    throw new System.InvalidOperationException("Promise disposed while pending: " + this);
                }
#endif
                // Rejection maybe wasn't caught.
                ((ValueContainer) _valueOrPrevious).ReleaseAndMaybeAddToUnhandledStack(!SuppressRejection);
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

            private void HookupNewWaiter(HandleablePromiseBase newWaiter)
            {
                var executionScheduler = new ExecutionScheduler(true);
                HookupNewWaiter(newWaiter, ref executionScheduler);
            }

            private void HookupNewWaiter(HandleablePromiseBase newWaiter, ref ExecutionScheduler executionScheduler)
            {
                AddWaiter(newWaiter, ref executionScheduler);
                executionScheduler.Execute();
            }

#if PROMISE_PROGRESS
            private void HookupNewPromiseWithProgress<TPromiseRef>(TPromiseRef newPromise, ushort depth) where TPromiseRef : PromiseRef, IProgressListener
            {
                newPromise._valueOrPrevious = this;
                HookupNewWaiterWithProgress(newPromise, depth);
            }

            private void HookupNewWaiterWithProgress<TWaiter>(TWaiter newWaiter, ushort depth) where TWaiter : HandleablePromiseBase, IProgressListener
            {
                var executionScheduler = new ExecutionScheduler(true);
                SubscribeListener(newWaiter, Fixed32.FromWhole(depth), ref executionScheduler);
                HookupNewWaiter(newWaiter, ref executionScheduler);
            }
#endif

            internal PromiseRef GetPreserved(short promiseId, ushort depth)
            {
                MarkAwaited(promiseId, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                var newPromise = PromiseMultiAwait.GetOrCreate(depth);
                HookupNewPromise(newPromise);
                return newPromise;
            }

            internal virtual PromiseConfigured GetConfigured(short promiseId, SynchronizationContext synchronizationContext, ushort depth)
            {
                MarkAwaited(promiseId, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                var newPromise = PromiseConfigured.GetOrCreate(synchronizationContext, depth);
                HookupNewPromise(newPromise);
                return newPromise;
            }

            internal abstract PromiseRef GetDuplicate(short promiseId, ushort depth);

            internal abstract void AddWaiter(HandleablePromiseBase waiter, ref ExecutionScheduler executionScheduler);
            internal abstract void AddWaiter(HandleablePromiseBase waiter, ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler);

            private void SetResult(ValueContainer valueContainer, Promise.State state)
            {
                _valueOrPrevious = valueContainer;
                Thread.MemoryBarrier(); // Make sure state is written after value.
                State = state;
            }

            [MethodImpl(InlineOption)]
            internal TResult GetResult<TResult>()
            {
                return ((ValueContainer) _valueOrPrevious).GetValue<TResult>();
            }

            [MethodImpl(InlineOption)]
            private bool TryGetRejectValue<TReject>(out TReject rejectValue)
            {
                return ((ValueContainer) _valueOrPrevious).TryGetValue(out rejectValue);
            }

            internal void MaybeHandleNext(HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
            {
                PromiseRef handler = this;
#if PROTO_PROMISE_NO_STACK_UNWIND
                if (nextHandler != null)
                {
                    nextHandler.Handle(ref handler, out nextHandler, ref executionScheduler);
                }
                else
                {
                    handler.MaybeDispose();
                }
#else
                while (nextHandler != null)
                {
                    nextHandler.Handle(ref handler, out nextHandler, ref executionScheduler);
                }
                handler.MaybeDispose();
#endif
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseSingleAwait : PromiseRef
            {
                internal sealed override PromiseRef GetDuplicate(short promiseId, ushort depth)
                {
                    IncrementIdAndSetFlags(promiseId, PromiseFlags.None);
                    return this;
                }

                protected override void MarkAwaited(short promiseId, PromiseFlags flags)
                {
                    IncrementIdAndSetFlags(promiseId, flags | PromiseFlags.HadCallback);
                }

                [MethodImpl(InlineOption)]
                protected void SetWaiter(HandleablePromiseBase waiter)
                {
#if PROMISE_DEBUG
                    if (Interlocked.CompareExchange(ref _waiter, waiter, null) != null)
                    {
                        throw new System.InvalidOperationException("Cannot add more than 1 waiter to a single await promise.");
                    }
#else
                    _waiter = waiter;
#endif
                }

                internal override void AddWaiter(HandleablePromiseBase waiter, ref ExecutionScheduler executionScheduler)
                {
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        ThrowIfInPool(this);
                        // When this is completed, State is set then _waiter is swapped, so we must reverse that process here.
                        Thread.MemoryBarrier();
                        SetWaiter(waiter);
                        Thread.MemoryBarrier(); // Make sure State is read after _waiter is written.
                        if (State != Promise.State.Pending)
                        {
                            // Interlocked.Exchange to handle race condition with Handle on another thread.
                            MaybeHandleNext(Interlocked.Exchange(ref _waiter, null), ref executionScheduler);
                        }
                        else
                        {
                            MaybeDispose();
                        }
                    }
                }

                internal override void AddWaiter(HandleablePromiseBase waiter, ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        ThrowIfInPool(this);
                        // When this is completed, State is set then _waiter is swapped, so we must reverse that process here.
                        Thread.MemoryBarrier();
                        SetWaiter(waiter);
                        Thread.MemoryBarrier(); // Make sure State is read after _waiter is written.
                        if (State != Promise.State.Pending)
                        {
                            // Interlocked.Exchange to handle race condition with Handle on another thread.
                            nextHandler = Interlocked.Exchange(ref _waiter, null);
                        }
                        else
                        {
                            nextHandler = null;
                        }
                        handler = this;
                    }
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    HandleWithCatch(ref handler, out nextHandler, ref executionScheduler);
                }

                internal void HandleWithCatch(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    bool invokingRejected = false;
                    var previousHandler = handler;
                    SetCurrentInvoker(this);
                    try
                    {
                        Execute(ref handler, out nextHandler, ref invokingRejected, ref executionScheduler);
                    }
                    catch (RethrowException e)
                    {
                        var state = previousHandler.State;
                        var valueContainer = (ValueContainer) previousHandler._valueOrPrevious;
                        bool isAcceptableRethrow = invokingRejected || (e is ForcedRethrowException && state != Promise.State.Resolved);
                        if (!isAcceptableRethrow)
                        {
                            // If the rethrow was invalid, send the previous rejection to the uncaught rejection handler. This is a no-op for resolve and cancel containers.
                            ((ValueContainer) handler._valueOrPrevious).AddToUnhandledStack();
                            valueContainer = CreateRejectContainer(e, int.MinValue, this);
                            state = Promise.State.Rejected;
                        }
                        else
                        {
                            valueContainer.Retain();
                        }
                        SetResultAndMaybeHandleFromCatch(valueContainer, state, out nextHandler, ref executionScheduler);
                        handler = this;
                    }
                    catch (OperationCanceledException)
                    {
                        var valueContainer = CancelContainerVoid.GetOrCreate();
                        SetResultAndMaybeHandleFromCatch(valueContainer, Promise.State.Canceled, out nextHandler, ref executionScheduler);
                        handler = this;
                    }
                    catch (Exception e)
                    {
                        var valueContainer = CreateRejectContainer(e, int.MinValue, this);
                        SetResultAndMaybeHandleFromCatch(valueContainer, Promise.State.Rejected, out nextHandler, ref executionScheduler);
                        handler = this;
                    }
                    ClearCurrentInvoker();
                    WaitWhileProgressFlags(PromiseFlags.Subscribing);
                    previousHandler.MaybeDispose();
                }

                protected virtual void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref ExecutionScheduler executionScheduler)
                {
                    throw new System.InvalidOperationException();
                }

                internal void SetResultAndMaybeHandle(ValueContainer valueContainer, Promise.State state, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    SetResult(valueContainer, state);
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                        nextHandler = Interlocked.Exchange(ref _waiter, null);
                    }
#if PROTO_PROMISE_NO_STACK_UNWIND
                    MaybeHandleNext(nextHandler, ref executionScheduler);
                    nextHandler = null;
#endif
                }

                internal void HandleSelf(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    var valueContainer = (ValueContainer) handler._valueOrPrevious;
                    valueContainer.Retain();
                    SetResultAndMaybeHandle(valueContainer, handler.State, out nextHandler, ref executionScheduler);
                    handler = this;
                }

                [MethodImpl(InlineOption)]
                internal void SetResultAndMaybeHandleFromCatch(ValueContainer valueContainer, Promise.State state, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    SetResult(valueContainer, state);
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                        nextHandler = Interlocked.Exchange(ref _waiter, null);
                    }
                    HandleProgressListener(state, Depth, ref executionScheduler);
#if PROTO_PROMISE_NO_STACK_UNWIND
                    MaybeHandleNext(nextHandler, ref executionScheduler);
                    nextHandler = null;
#endif
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseSingleAwaitWithProgress : PromiseSingleAwait
            {

                [MethodImpl(InlineOption)]
                new internal void SetResultAndMaybeHandle(ValueContainer valueContainer, Promise.State state, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    SetResult(valueContainer, state);
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                        nextHandler = Interlocked.Exchange(ref _waiter, null);
                    }
                    HandleProgressListener(state, Depth, ref executionScheduler);
#if PROTO_PROMISE_NO_STACK_UNWIND
                    MaybeHandleNext(nextHandler, ref executionScheduler);
                    nextHandler = null;
#endif
                }

                new internal void HandleSelf(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    var valueContainer = (ValueContainer) handler._valueOrPrevious;
                    valueContainer.Retain();
                    SetResultAndMaybeHandle(valueContainer, handler.State, out nextHandler, ref executionScheduler);
                    handler = this;
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
                        _smallFields.InterlockedSetFlags(PromiseFlags.WasAwaitedOrForgotten); // Stop base finalizer from adding an extra exception.
                        string message = "A preserved Promise's resources were garbage collected without it being forgotten. You must call Forget() on each preserved promise when you are finished with it.";
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static PromiseMultiAwait GetOrCreate(ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseMultiAwait>()
                        ?? new PromiseMultiAwait();
                    promise.Reset(depth);
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void MarkAwaited(short promiseId, PromiseFlags flags)
                {
                    InterlockedRetainAndSetFlagsInternal(promiseId, flags);
                }

                internal override PromiseRef GetDuplicate(short promiseId, ushort depth)
                {
                    MarkAwaited(promiseId, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                    var newPromise = PromiseDuplicate.GetOrCreate(depth);
                    HookupNewPromise(newPromise);
                    return newPromise;
                }

                internal override void AddWaiter(HandleablePromiseBase waiter, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    var state = State;
                    if (state == Promise.State.Pending)
                    {
                        _progressAndLocker._branchLocker.Enter();
                        state = State;
                        if (state == Promise.State.Pending)
                        {
                            _nextBranches.Enqueue(waiter);
                            _progressAndLocker._branchLocker.Exit();

                            MaybeDispose();
                            return;
                        }
                        _progressAndLocker._branchLocker.Exit();
                    }
                    HandleablePromiseBase nextHandler;
                    PromiseRef handler = this;
                    InterlockedRetainDisregardId(); // Retain since Handle will release indiscriminately.
                    waiter.Handle(ref handler, out nextHandler, ref executionScheduler);
                    handler.MaybeHandleNext(nextHandler, ref executionScheduler);
                    
                    MaybeDispose();
                }

                internal override void AddWaiter(HandleablePromiseBase waiter, ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    handler = this;
                    if (State == Promise.State.Pending)
                    {
                        _progressAndLocker._branchLocker.Enter();
                        if (State == Promise.State.Pending)
                        {
                            _nextBranches.Enqueue(waiter);
                            _progressAndLocker._branchLocker.Exit();
                            nextHandler = null;
                            return;
                        }
                        _progressAndLocker._branchLocker.Exit();
                    }
                    nextHandler = waiter;
                }

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);

                    HandleProgressListeners(State, ref executionScheduler);
                    HandleBranches(ref executionScheduler);

                    MaybeDispose();
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    nextHandler = null;
                    ThrowIfInPool(this);
                    var valueContainer = (ValueContainer) handler._valueOrPrevious;
                    valueContainer.Retain();
                    SetResult(valueContainer, handler.State);
                    executionScheduler.ScheduleSynchronous(this);
                    WaitWhileProgressFlags(PromiseFlags.Subscribing);
                }

                private void HandleBranches(ref ExecutionScheduler executionScheduler)
                {
                    _progressAndLocker._branchLocker.Enter();
                    var branches = _nextBranches.MoveElementsToStack();
                    _progressAndLocker._branchLocker.Exit();
                    while (branches.IsNotEmpty)
                    {
                        var waiter = branches.Pop();
                        HandleablePromiseBase nextHandler;
                        PromiseRef handler = this;
                        InterlockedRetainDisregardId(); // Retain since Handle will release indiscriminately.
                        waiter.Handle(ref handler, out nextHandler, ref executionScheduler);
                        handler.MaybeHandleNext(nextHandler, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class PromiseDuplicate : PromiseSingleAwait
            {
                private PromiseDuplicate() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                internal static PromiseDuplicate GetOrCreate(ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseDuplicate>()
                        ?? new PromiseDuplicate();
                    promise.Reset(depth);
                    return promise;
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    var previousHandler = handler;
                    HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    WaitWhileProgressFlags(PromiseFlags.Subscribing);
                    previousHandler.MaybeDispose();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseConfigured : PromiseSingleAwait
            {
                private PromiseConfigured() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal static PromiseConfigured GetOrCreate(SynchronizationContext synchronizationContext, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseConfigured>()
                        ?? new PromiseConfigured();
                    promise.Reset(depth);
                    promise._synchronizationContext = synchronizationContext;
                    promise._mostRecentPotentialScheduleMethod = (int) ScheduleMethod.None;
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal static PromiseConfigured GetOrCreateFromNull<TResult>(SynchronizationContext synchronizationContext,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult result, ushort depth)
                {
                    var promise = GetOrCreate(synchronizationContext, depth);
                    promise._valueOrPrevious = CreateResolveContainer(result);
                    promise._mostRecentPotentialScheduleMethod = (int) ScheduleMethod.Handle;
                    return promise;
                }

                internal override PromiseConfigured GetConfigured(short promiseId, SynchronizationContext synchronizationContext, ushort depth)
                {
                    IncrementIdAndSetFlags(promiseId, PromiseFlags.None);
                    _synchronizationContext = synchronizationContext;
                    return this;
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    var valueContainer = (ValueContainer) handler._valueOrPrevious;
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    nextHandler = null;
                    ScheduleMethod previousScheduleType = (ScheduleMethod) Interlocked.Exchange(ref _mostRecentPotentialScheduleMethod, (int) ScheduleMethod.Handle);
                    // Leave pending until this is awaited or forgotten.
                    _previousState = handler.State;
                    if (previousScheduleType == ScheduleMethod.AddWaiter)
                    {
                        executionScheduler.ScheduleOnContext(_synchronizationContext, this);
                    }
                    else if (previousScheduleType == ScheduleMethod.OnForgetOrHookupFailed)
                    {
                        executionScheduler.ScheduleSynchronous(this);
                    }
                    WaitWhileProgressFlags(PromiseFlags.Subscribing);
                }

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    State = _previousState;
                    // We don't need to synchronize access here because this is only called when the waiter has already been added the previous promise completed, so there are no race conditions.
                    HandleablePromiseBase nextHandler = _waiter;
                    _waiter = null;
                    MaybeHandleNext(nextHandler, ref executionScheduler);
                }

                private void AddWaiterImpl(HandleablePromiseBase waiter, ref ExecutionScheduler executionScheduler)
                {
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        ThrowIfInPool(this);
                        Thread.MemoryBarrier();
                        SetWaiter(waiter);
                        ScheduleMethod previousScheduleType = (ScheduleMethod) Interlocked.Exchange(ref _mostRecentPotentialScheduleMethod, (int) ScheduleMethod.AddWaiter);
                        if (previousScheduleType == ScheduleMethod.Handle)
                        {
                            executionScheduler.ScheduleOnContext(_synchronizationContext, this);
                        }
                    }
                }

                internal override void AddWaiter(HandleablePromiseBase waiter, ref ExecutionScheduler executionScheduler)
                {
                    AddWaiterImpl(waiter, ref executionScheduler);
                    MaybeDispose();
                }

                internal override void AddWaiter(HandleablePromiseBase waiter, ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    nextHandler = null;
                    handler = this;
                    AddWaiterImpl(waiter, ref executionScheduler);
                }

                protected override void OnForgetOrHookupFailed()
                {
                    ThrowIfInPool(this);
                    if ((ScheduleMethod) Interlocked.Exchange(ref _mostRecentPotentialScheduleMethod, (int) ScheduleMethod.OnForgetOrHookupFailed) == ScheduleMethod.Handle)
                    {
                        State = _previousState;
                        _smallFields.InterlockedTryReleaseComplete();
                    }
                    base.OnForgetOrHookupFailed();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseWaitPromise : PromiseSingleAwaitWithProgress, IProgressListener
            {
                [MethodImpl(InlineOption)]
                internal void WaitFor<T>(Promise<T> other, ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValidateReturn(other);
                    var _ref = other._ref;
                    if (_ref == null)
                    {
                        handler = this;
                        var valueContainer = CreateResolveContainer(other.Result);
                        SetResultAndMaybeHandle(valueContainer, Promise.State.Resolved, out nextHandler, ref executionScheduler);
                    }
                    else
                    {
                        _ref.MarkAwaited(other.Id, PromiseFlags.SuppressRejection | PromiseFlags.WasAwaitedOrForgotten);
                        SetPreviousAndSubscribeProgress(_ref, other.Depth, ref executionScheduler);
                        _ref.AddWaiter(this, ref handler, out nextHandler, ref executionScheduler);
                    }
                }

#if !PROMISE_PROGRESS
                [MethodImpl(InlineOption)]
                private void SetPreviousAndSubscribeProgress(PromiseRef other, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    _valueOrPrevious = other;
                }
#endif
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class AsyncPromiseBase : PromiseSingleAwaitWithProgress
            {
                protected void Reset()
                {
#if PROMISE_PROGRESS
                    _progressAndSubscribeFields._currentProgress = default(Fixed32);
#endif
                    _smallFields.Reset();
                    SetCreatedStacktrace(this, 3);
                }

                protected void HandleInternal(ValueContainer valueContainer, Promise.State state)
                {
                    SetResult(valueContainer, state);
                    HandleablePromiseBase nextHandler;
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                        nextHandler = Interlocked.Exchange(ref _waiter, null);
                    }
                    var executionScheduler = new ExecutionScheduler(true);
                    HandleProgressListener(state, 0, ref executionScheduler);
                    MaybeHandleNext(nextHandler, ref executionScheduler);
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
                    HandleInternal(CreateResolveContainer(value), Promise.State.Resolved);
                }

                protected void RejectDirect<TReject>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TReject reason, int rejectSkipFrames)
                {
                    ThrowIfInPool(this);
                    HandleInternal(CreateRejectContainer(reason, rejectSkipFrames + 1, this), Promise.State.Rejected);
                }

                [MethodImpl(InlineOption)]
                protected void CancelDirect()
                {
                    ThrowIfInPool(this);
                    HandleInternal(CancelContainerVoid.GetOrCreate(), Promise.State.Canceled);
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
            private sealed partial class PromiseResolve<TResolver> : PromiseSingleAwait
                where TResolver : IDelegateResolveOrCancel
            {
                private PromiseResolve() { }

                [MethodImpl(InlineOption)]
                internal static PromiseResolve<TResolver> GetOrCreate(TResolver resolver, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseResolve<TResolver>>()
                        ?? new PromiseResolve<TResolver>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref ExecutionScheduler executionScheduler)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (handler.State == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseResolvePromise<TResolver> : PromiseWaitPromise
                where TResolver : IDelegateResolveOrCancelPromise
            {
                private PromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                internal static PromiseResolvePromise<TResolver> GetOrCreate(TResolver resolver, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseResolvePromise<TResolver>>()
                        ?? new PromiseResolvePromise<TResolver>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref ExecutionScheduler executionScheduler)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (handler.State == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseResolveReject<TResolver, TRejecter> : PromiseSingleAwait
                where TResolver : IDelegateResolveOrCancel
                where TRejecter : IDelegateReject
            {
                private PromiseResolveReject() { }

                [MethodImpl(InlineOption)]
                internal static PromiseResolveReject<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseResolveReject<TResolver, TRejecter>>()
                        ?? new PromiseResolveReject<TResolver, TRejecter>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref ExecutionScheduler executionScheduler)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    _rejecter = default(TRejecter);
                    var state = handler.State;
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseResolveRejectPromise<TResolver, TRejecter> : PromiseWaitPromise
                where TResolver : IDelegateResolveOrCancelPromise
                where TRejecter : IDelegateRejectPromise
            {
                private PromiseResolveRejectPromise() { }

                [MethodImpl(InlineOption)]
                internal static PromiseResolveRejectPromise<TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseResolveRejectPromise<TResolver, TRejecter>>()
                        ?? new PromiseResolveRejectPromise<TResolver, TRejecter>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref ExecutionScheduler executionScheduler)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    _rejecter = default(TRejecter);
                    var state = handler.State;
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
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
                internal static PromiseContinue<TContinuer> GetOrCreate(TContinuer continuer, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseContinue<TContinuer>>()
                        ?? new PromiseContinue<TContinuer>();
                    promise.Reset(depth);
                    promise._continuer = continuer;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref ExecutionScheduler executionScheduler)
                {
                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    callback.Invoke(ref handler, out nextHandler, this, ref executionScheduler);
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
                internal static PromiseContinuePromise<TContinuer> GetOrCreate(TContinuer continuer, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseContinuePromise<TContinuer>>()
                        ?? new PromiseContinuePromise<TContinuer>();
                    promise.Reset(depth);
                    promise._continuer = continuer;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref ExecutionScheduler executionScheduler)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    callback.Invoke(ref handler, out nextHandler, this, ref executionScheduler);
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
                internal static PromiseFinally<TFinalizer> GetOrCreate(TFinalizer finalizer, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseFinally<TFinalizer>>()
                        ?? new PromiseFinally<TFinalizer>();
                    promise.Reset(depth);
                    promise._finalizer = finalizer;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref ExecutionScheduler executionScheduler)
                {
                    var callback = _finalizer;
                    _finalizer = default(TFinalizer);
                    try
                    {
                        callback.Invoke();
                    }
                    catch
                    {
                        // Unlike normal finally clauses, we won't swallow the previous rejection. Instead, we send it to the uncaught rejection handler.
                        ((ValueContainer) handler._valueOrPrevious).AddToUnhandledStack();
                        throw;
                    }
                    HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseCancel<TCanceler> : PromiseSingleAwait
                where TCanceler : IDelegateResolveOrCancel
            {
                private PromiseCancel() { }

                [MethodImpl(InlineOption)]
                internal static PromiseCancel<TCanceler> GetOrCreate(TCanceler canceler, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseCancel<TCanceler>>()
                        ?? new PromiseCancel<TCanceler>();
                    promise.Reset(depth);
                    promise._canceler = canceler;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref ExecutionScheduler executionScheduler)
                {
                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    if (handler.State == Promise.State.Canceled)
                    {
                        callback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private sealed partial class PromiseCancelPromise<TCanceler> : PromiseWaitPromise
                where TCanceler : IDelegateResolveOrCancelPromise
            {
                private PromiseCancelPromise() { }

                [MethodImpl(InlineOption)]
                internal static PromiseCancelPromise<TCanceler> GetOrCreate(TCanceler resolver, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseCancelPromise<TCanceler>>()
                        ?? new PromiseCancelPromise<TCanceler>();
                    promise.Reset(depth);
                    promise._canceler = resolver;
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref ExecutionScheduler executionScheduler)
                {
                    if (_canceler.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    if (handler.State == Promise.State.Canceled)
                    {
                        callback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler, ref executionScheduler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromisePassThrough : HandleablePromiseBase, ILinked<PromisePassThrough>, IProgressListener
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
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), _target);
                    }
                }

                internal static PromisePassThrough GetOrCreate(Promise owner, int index, PromiseFlags ownerSetFlags)
                {
                    // owner._ref is checked for nullity before passing into this.
                    owner._target._ref.MarkAwaited(owner._target.Id, ownerSetFlags);
                    var passThrough = ObjectPool<PromisePassThrough>.TryTake<PromisePassThrough>()
                        ?? new PromisePassThrough();
                    passThrough._owner = owner._target._ref;
                    passThrough._smallFields._index = index;
                    passThrough._smallFields._retainCounter = 1;
                    passThrough.ResetProgress(owner._target.Depth);
                    return passThrough;
                }

                partial void ResetProgress(ushort depth);
                partial void WaitWhileProgressIsBusy();

                internal void SetTargetAndAddToOwner(MultiHandleablePromiseBase target)
                {
                    ThrowIfInPool(this);
                    _target = target;
#if PROMISE_PROGRESS
                    // Unfortunately, we have to eagerly subscribe progress. Lazy algorithm would be much more expensive with thread safety, requiring allocations. (see ValidateReturn)
                    // But it's not so bad, because it doesn't allocate any memory (just uses CPU cycles to set it up).
                    _owner.HookupNewWaiterWithProgress(this, _smallFields._depth);
#else
                    _owner.HookupNewWaiter(this);
#endif
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    // TODO: pass refs to target.
                    ThrowIfInPool(this);
                    _owner = null;
                    WaitWhileProgressIsBusy();
                    _target.Handle(handler, (ValueContainer) handler._valueOrPrevious, this, ref executionScheduler);
                    Release();
                    nextHandler = null;
                }

                [MethodImpl(InlineOption)]
                internal void Retain()
                {
                    ThrowIfInPool(this);
                    InterlockedAddWithOverflowCheck(ref _smallFields._retainCounter, 1, -1);
                }

                internal void Release()
                {
                    ThrowIfInPool(this);
                    if (InterlockedAddWithOverflowCheck(ref _smallFields._retainCounter, -1, 0) == 0)
                    {
                        _target = null;
                        ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                    }
                }

#if !PROMISE_PROGRESS
                internal ushort Depth
                {
                    [MethodImpl(InlineOption)]
                    get { return 0; }
                }
#endif
            } // PromisePassThrough

            partial struct SmallFields
            {
                [MethodImpl(InlineOption)]
                internal bool InterlockedTryIncrementPromiseIdAndSetFlags(short promiseId, PromiseFlags flags)
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
                        newValue._flags |= flags;
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
                internal bool InterlockedTryRetainAndSetFlags(short promiseId, PromiseFlags flags)
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
                        newValue._flags |= flags;
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

                [MethodImpl(InlineOption)]
                internal void Reset(ushort depth)
                {
                    Reset();
                    _depth = depth;
                }

                internal PromiseFlags InterlockedSetFlags(PromiseFlags flags)
                {
                    Thread.MemoryBarrier();
                    SmallFields initialValue = default(SmallFields), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
                        newValue._flags |= flags;
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return initialValue._flags;
                }

                internal PromiseFlags InterlockedUnsetFlags(PromiseFlags flags)
                {
                    Thread.MemoryBarrier();
                    SmallFields initialValue = default(SmallFields), newValue;
                    PromiseFlags unsetFlags = ~flags;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
                        newValue._flags &= unsetFlags;
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return initialValue._flags;
                }

                [MethodImpl(InlineOption)]
                internal bool AreFlagsSet(PromiseFlags flags)
                {
                    return (_flags & flags) != 0;
                }
            } // SmallFields

            internal static void MaybeMarkAwaitedAndDispose(PromiseRef promise, short id, PromiseFlags flags)
            {
                if (promise != null)
                {
                    promise.MarkAwaited(id, flags);
                    promise.MaybeDispose();
                }
            }
        } // PromiseRef

        [MethodImpl(InlineOption)]
        internal static void MaybeMarkAwaitedAndDispose(PromiseRef promise, short id, PromiseFlags flags)
        {
            PromiseRef.MaybeMarkAwaitedAndDispose(promise, id, flags);
        }

        // Only using uint for maxDepth because Math.Max does not have an overload for ushort. It gets casted back to ushort when used for the final promise.
        internal static void PrepareForMerge(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs,
            int index, ref uint pendingAwaits, ref ulong completedProgress, ref ulong totalProgress, ref ushort maxDepth)
        {
            VoidResult voidResult = default(VoidResult);
            PrepareForMerge(promise._target, ref voidResult, ref passThroughs, index, ref pendingAwaits, ref completedProgress, ref totalProgress, ref maxDepth);
        }

        internal static void PrepareForMerge<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs,
            int index, ref uint pendingAwaits, ref ulong completedProgress, ref ulong totalProgress, ref ushort maxDepth)
        {
#if PROMISE_DEBUG
            checked
#else
            unchecked
#endif
            {
                uint expectedProgress = promise.Depth + 1u;
                if (promise._ref == null)
                {
                    completedProgress += expectedProgress;
                    value = promise.Result;
                }
                else
                {
                    passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection));
                    ++pendingAwaits;
                }
                totalProgress += expectedProgress;
                maxDepth = Math.Max(maxDepth, promise.Depth);
            }
        }

        // Only using uint for minDepth because Math.Min does not have an overload for ushort. It gets casted back to ushort when used for the final promise.
        internal static bool TryPrepareForRace(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index, ref ushort minDepth, PromiseFlags flags)
        {
            VoidResult voidResult = default(VoidResult);
            return TryPrepareForRace(promise._target, ref voidResult, ref passThroughs, index, ref minDepth, flags);
        }

        internal static bool TryPrepareForRace<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index, ref ushort minDepth, PromiseFlags flags)
        {
            bool isPending = promise._ref != null;
            if (!isPending)
            {
                value = promise.Result;
            }
            else
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index, flags));
            }
            minDepth = Math.Min(minDepth, promise.Depth);
            return isPending;
        }

        [MethodImpl(InlineOption)]
        internal static Promise CreateResolved(ushort depth)
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging in the finalizer.
            var deferred = Promise.NewDeferred();
            deferred.Resolve();
            return deferred.Promise;
#else
            // Make a promise on the stack for efficiency.
            return new Promise(null, ValidIdFromApi, depth);
#endif
        }

        [MethodImpl(InlineOption)]
        internal static Promise<T> CreateResolved<T>(
#if CSHARP_7_3_OR_NEWER
            in
#endif
            T value, ushort depth)
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging in the finalizer.
            var deferred = Promise.NewDeferred<T>();
            deferred.Resolve(value);
            return deferred.Promise;
#else
            // Make a promise on the stack for efficiency.
            return new Promise<T>(null, ValidIdFromApi, depth, value);
#endif
        }
    } // Internal
}