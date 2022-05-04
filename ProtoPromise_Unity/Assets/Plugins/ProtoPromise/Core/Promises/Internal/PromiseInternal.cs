#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

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
            internal abstract void MaybeMarkAwaitedAndDispose(short promiseId);
            protected abstract void MaybeDispose();
            internal abstract bool GetIsCompleted(short promiseId);
            protected abstract void OnForget(short promiseId);
            internal abstract PromiseRef GetDuplicate(short promiseId, ushort depth);
            internal abstract PromiseSingleAwait AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter, ref ExecutionScheduler executionScheduler);
            internal abstract bool GetIsValid(short promiseId);

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
                get { return _smallFields._suppressRejection; }
                [MethodImpl(InlineOption)]
                set { _smallFields._suppressRejection = value; }
            }

            private bool WasAwaitedOrForgotten
            {
                [MethodImpl(InlineOption)]
                get { return _smallFields._wasAwaitedorForgotten; }
                [MethodImpl(InlineOption)]
                set { _smallFields._wasAwaitedorForgotten = value; }
            }

            private PromiseRef() { }

            ~PromiseRef()
            {
                try
                {
                    if (!WasAwaitedOrForgotten)
                    {
                        // Promise was not awaited or forgotten.
                        string message = "A Promise's resources were garbage collected without it being awaited. You must await, return, or forget each promise.";
                        AddRejectionToUnhandledStack(new UnobservedPromiseException(message), this);
                    }
                    if (_valueContainer != null & State == Promise.State.Rejected & !SuppressRejection)
                    {
                        // Rejection maybe wasn't caught. Just add to unhandled stack without dispose.
                        _valueContainer.AddToUnhandledStack();
                    }
                }
                catch (Exception e)
                {
                    // This should never happen.
                    AddRejectionToUnhandledStack(e, this);
                }
            }

            internal void Forget(short promiseId)
            {
                OnForget(promiseId);
                MaybeReportUnhandledRejections();
            }

            [MethodImpl(InlineOption)]
            protected void Reset()
            {
                _waiter = null;
                _smallFields.Reset();
                SetCreatedStacktrace(this, 3);
            }

            [MethodImpl(InlineOption)]
            protected void Reset(ushort depth)
            {
                Reset();
                _smallFields._depth = depth;
            }

            private void Dispose()
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (State == Promise.State.Pending)
                {
                    throw new System.InvalidOperationException("Promise disposed while pending: " + this);
                }
#endif
                _smallFields.IncrementPromiseId();
#if PROMISE_DEBUG
                _previous = null;
#endif
                // Rejection maybe wasn't caught.
                _valueContainer.DisposeAndMaybeAddToUnhandledStack(!SuppressRejection);
                _valueContainer = null;
            }

            private void HookupNewPromise(short promiseId, PromiseRef newPromise)
            {
#if PROMISE_DEBUG
                newPromise._previous = this;
#endif
#if PROMISE_PROGRESS
                newPromise._smallFields._currentProgress = _smallFields._currentProgress;
#endif
                HookupNewWaiter(promiseId, newPromise);
            }

            private void HookupNewWaiter(short promiseId, HandleablePromiseBase newWaiter)
            {
                try
                {
                    HookupExistingWaiter(promiseId, newWaiter);
                }
                catch (InvalidOperationException)
                {
                    // We're already throwing InvalidOperationException here, so we don't want the waiter object to also add exceptions from its finalizer.
                    Discard(newWaiter);
                    throw;
                }
            }

            private void HookupExistingWaiter(short promiseId, HandleablePromiseBase newWaiter)
            {
                var executionScheduler = new ExecutionScheduler(true);
                HandleablePromiseBase previousWaiter;
                PromiseSingleAwait promiseSingleAwait = AddWaiter(promiseId, newWaiter, out previousWaiter, ref executionScheduler);
                if (previousWaiter != null)
                {
                    if (!PromiseSingleAwait.VerifyWaiter(promiseSingleAwait))
                    {
                        throw new InvalidOperationException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", GetFormattedStacktrace(2));
                    }
                    PromiseRef handler = this;
                    HandleablePromiseBase _;
                    newWaiter.Handle(ref handler, out _, ref executionScheduler);
                }
                executionScheduler.Execute();
            }

            internal PromiseRef GetPreserved(short promiseId, ushort depth)
            {
                var newPromise = PromiseMultiAwait.GetOrCreate(depth);
                HookupNewPromise(promiseId, newPromise);
                return newPromise;
            }

            internal virtual PromiseConfigured GetConfigured(short promiseId, SynchronizationContext synchronizationContext, ushort depth)
            {
                var newPromise = PromiseConfigured.GetOrCreate(synchronizationContext, depth);
                HookupNewPromise(promiseId, newPromise);
                return newPromise;
            }

            [MethodImpl(InlineOption)]
            private void SetResult(ValueContainer valueContainer, Promise.State state)
            {
                _valueContainer = valueContainer;
                State = state;
            }

            [MethodImpl(InlineOption)]
            internal TResult GetResult<TResult>()
            {
                return _valueContainer.GetValue<TResult>();
            }

            [MethodImpl(InlineOption)]
            private bool TryGetRejectValue<TReject>(out TReject rejectValue)
            {
                return _valueContainer.TryGetValue(out rejectValue);
            }

            // This can be called instead of MaybeHandleNext when we know the nextHandler is not null.
            internal void HandleNext(HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
            {
                PromiseRef handler = this;
                WaitWhileProgressReporting();
                nextHandler.Handle(ref handler, out nextHandler, ref executionScheduler);
                handler.MaybeHandleNext(nextHandler, ref executionScheduler);
            }

            internal void MaybeHandleNext(HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
            {
                PromiseRef handler = this;
                while (nextHandler != null)
                {
                    // Set the waiter to InvalidAwaitSentinel to break the chain to stop progress reports.
                    handler._waiter = InvalidAwaitSentinel._instance;
                    handler.WaitWhileProgressReporting();
                    nextHandler.Handle(ref handler, out nextHandler, ref executionScheduler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseCompletionSentinel : HandleablePromiseBase
            {
                // A singleton instance used to mark the promise as completed.
                internal static readonly PromiseCompletionSentinel _instance = new PromiseCompletionSentinel();

                private PromiseCompletionSentinel() { }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseForgetSentinel : HandleablePromiseBase
            {
                // A singleton instance used to cap off the promise and prevent further awaits.
                internal static readonly PromiseForgetSentinel _instance = new PromiseForgetSentinel();

                private PromiseForgetSentinel() { }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    nextHandler = null;
                    handler.MaybeDispose();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class InvalidAwaitSentinel : PromiseSingleAwait
            {
                // A singleton instance used to indicate that an await was invalid (after the PromiseMultiAwait was forgotten or PromiseSingleAwait awaited).
                internal static readonly InvalidAwaitSentinel _instance = new InvalidAwaitSentinel();

                private InvalidAwaitSentinel()
                {
                    _waiter = this; // Set _waiter to this so that CompareExchangeWaiter will always fail.
                    _smallFields = new SmallFields(-5); // Set an id that is unlikely to match (though this should never be used in a Promise struct).
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
                protected override void MaybeDispose() { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseSingleAwait : PromiseRef
            {
                protected static class ScheduleMethod
                {
                    // Used to synchronize scheduling on context.
                    internal const int None = 0;
                    internal const int Handle = 1;
                    internal const int AddWaiter = 2;

                    [MethodImpl(InlineOption)]
                    internal static int Exchange(PromiseSingleAwait owner, ref int location, int value)
                    {
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                        lock (owner)
#endif
                        {
                            Thread.MemoryBarrier(); // Make sure previous writes are done before swapping schedule method.
                            return Interlocked.Exchange(ref location, value);
                        }
                    }
                }

                protected sealed override void OnForget(short promiseId)
                {
                    HookupExistingWaiter(promiseId, PromiseForgetSentinel._instance);
                }

                internal override void MaybeMarkAwaitedAndDispose(short promiseId)
                {
                    OnForget(promiseId);
                }

                internal override bool GetIsCompleted(short promiseId)
                {
                    var waiter = _waiter;
                    bool isValid = promiseId == Id & (waiter == null | waiter == PromiseCompletionSentinel._instance);
                    if (!isValid)
                    {
                        throw new InvalidOperationException("Cannot await a non-preserved promise more than once.", GetFormattedStacktrace(3));
                    }
                    return waiter != null;
                }

                [MethodImpl(InlineOption)]
                protected void ValidateIdAndNotAwaited(short promiseId)
                {
                    if (!GetIsValid(promiseId))
                    {
                        throw new InvalidOperationException("Cannot await or forget a non-preserved promise more than once.", GetFormattedStacktrace(3));
                    }
                }

                internal sealed override PromiseRef GetDuplicate(short promiseId, ushort depth)
                {
                    // This isn't strictly thread-safe, but when the next promise is awaited, the CompareExchange should catch it.
                    ValidateIdAndNotAwaited(promiseId);
                    _smallFields.IncrementPromiseId();
                    return this;
                }

                [MethodImpl(InlineOption)]
                internal override sealed bool GetIsValid(short promiseId)
                {
                    var waiter = _waiter;
                    return promiseId == Id & (waiter == null | waiter == PromiseCompletionSentinel._instance);
                }


                [MethodImpl(InlineOption)]
                internal HandleablePromiseBase TakeNextWaiter()
                {
                    return CompareExchangeWaiter(PromiseCompletionSentinel._instance, null);
                }

                [MethodImpl(InlineOption)]
                internal HandleablePromiseBase CompareExchangeWaiter(HandleablePromiseBase waiter, HandleablePromiseBase comparand)
                {
                    // TODO: run multi-threaded tests in unity to see if the lock is needed for CompareExchange.

//#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
//                    lock (this)
//#endif
                    {
                        Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                        return Interlocked.CompareExchange(ref _waiter, waiter, comparand);
                    }
                }

                internal static bool VerifyWaiter(PromiseSingleAwait promise)
                {
                    // If the existing waiter is anything except completion sentinel, it's an invalid await.
                    // We place another instance in its place to make sure future checks are caught.
                    // Promise may be null if it was verified internally, or InvalidAwaitSentinel if it's an invalid await.
                    return promise == null || promise.CompareExchangeWaiter(InvalidAwaitSentinel._instance, PromiseCompletionSentinel._instance) == PromiseCompletionSentinel._instance;
                }

                protected PromiseSingleAwait AddWaiterImpl(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    if (promiseId != Id)
                    {
                        previousWaiter = InvalidAwaitSentinel._instance;
                        return InvalidAwaitSentinel._instance;
                    }
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;
                    previousWaiter = CompareExchangeWaiter(waiter, null);
                    return this;
                }

                internal override PromiseSingleAwait AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter, ref ExecutionScheduler executionScheduler)
                {
                    return AddWaiterImpl(promiseId, waiter, out previousWaiter, Depth, ref executionScheduler);
                }

                internal void MaybeDisposePreviousFromCatch(PromiseRef previous, bool dispose)
                {
                    if (dispose)
                    {
                        MaybeDisposePrevious(previous);
                    }
                }

                [MethodImpl(InlineOption)]
                internal void MaybeDisposePrevious(PromiseRef previous)
                {
                    previous.MaybeDispose();
                }

                // TODO: I don't think this is necessary anymore. Can probably remove.
#if PROMISE_DEBUG
                protected const bool _resolveWillDisposeAfterSecondAwait = true;
#else
                protected const bool _resolveWillDisposeAfterSecondAwait = false;
#endif

                [MethodImpl(InlineOption)]
                internal void MaybeDisposePreviousBeforeSecondWait(PromiseRef previous)
                {
#if !PROMISE_DEBUG // Don't dispose before the callback if we're in debug mode so that if a circular promise chain is detected, it will be disposed properly.
                    MaybeDisposePrevious(previous);
#endif
                }

                [MethodImpl(InlineOption)]
                internal void MaybeDisposePreviousAfterSecondWait(PromiseRef previous)
                {
#if PROMISE_DEBUG // Dispose after the callback if we're in debug mode so that if a circular promise chain is detected, it will be disposed properly.
                    MaybeDisposePrevious(previous);
#endif
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    
                    bool invokingRejected = false;
                    bool handlerDisposedAfterCallback = false;
                    var previousHandler = handler;
                    var previousState = previousHandler.State;
                    SetCurrentInvoker(this);
                    try
                    {
                        // Handler is disposed deeper in the call stack, so we only dispose it here if an exception is thrown and it was not disposed before the callback.
                        Execute(ref handler, out nextHandler, ref invokingRejected, ref handlerDisposedAfterCallback, ref executionScheduler);
                    }
                    catch (RethrowException e)
                    {
                        ValueContainer valueContainer;
                        bool isAcceptableRethrow = invokingRejected || (e is ForcedRethrowException && previousState != Promise.State.Resolved);
                        if (!isAcceptableRethrow)
                        {
                            valueContainer = CreateRejectContainer(e, int.MinValue, this);
                            previousState = Promise.State.Rejected;
                        }
                        else
                        {
                            valueContainer = previousHandler._valueContainer.Clone();
                        }
                        MaybeDisposePreviousFromCatch(previousHandler, handlerDisposedAfterCallback);
                        SetResultAndTakeNextWaiter(valueContainer, previousState, out nextHandler);
                        handler = this;
                    }
                    catch (OperationCanceledException)
                    {
                        MaybeDisposePreviousFromCatch(previousHandler, handlerDisposedAfterCallback);
                        var valueContainer = CancelContainerVoid.GetOrCreate();
                        SetResultAndTakeNextWaiter(valueContainer, Promise.State.Canceled, out nextHandler);
                        handler = this;
                    }
                    catch (Exception e)
                    {
                        MaybeDisposePreviousFromCatch(previousHandler, handlerDisposedAfterCallback);
                        var valueContainer = CreateRejectContainer(e, int.MinValue, this);
                        SetResultAndTakeNextWaiter(valueContainer, Promise.State.Rejected, out nextHandler);
                        handler = this;
                    }
                    ClearCurrentInvoker();
                }

                protected virtual void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    throw new System.InvalidOperationException();
                }

                [MethodImpl(InlineOption)]
                internal void SetResultAndTakeNextWaiter(ValueContainer valueContainer, Promise.State state, out HandleablePromiseBase nextHandler)
                {
                    SetResult(valueContainer, state);
                    nextHandler = TakeNextWaiter();
                }

                internal void HandleSelf(ref PromiseRef handler, out HandleablePromiseBase nextHandler)
                {
                    var state = handler.State;
                    var valueContainer = handler._valueContainer.Clone();
                    handler.SuppressRejection = true;
                    MaybeDisposePrevious(handler);
                    handler = this;
                    SetResultAndTakeNextWaiter(valueContainer, state, out nextHandler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseMultiAwait : PromiseRef
            {
                private PromiseMultiAwait() { }

                ~PromiseMultiAwait()
                {
                    try
                    {
                        if (!WasAwaitedOrForgotten)
                        {
                            WasAwaitedOrForgotten = true; // Stop base finalizer from adding an extra exception.
                            string message = "A preserved Promise's resources were garbage collected without it being forgotten. You must call Forget() on each preserved promise when you are finished with it.";
                            AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                        }
                        else if (_retainCounter != 0 & State != Promise.State.Pending)
                        {
                            string message = "A preserved Promise had an awaiter created without awaiter.GetResult() called.";
                            AddRejectionToUnhandledStack(new UnreleasedObjectException(message), this);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        AddRejectionToUnhandledStack(e, this);
                    }
                }

                [MethodImpl(InlineOption)]
                new private void Reset(ushort depth)
                {
                    _retainCounter = 2; // 1 for forget, 1 for completion.
                    base.Reset(depth);
                }

                [MethodImpl(InlineOption)]
                internal static PromiseMultiAwait GetOrCreate(ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseMultiAwait>()
                        ?? new PromiseMultiAwait();
                    promise.Reset(depth);
                    return promise;
                }

                private void Retain()
                {
                    checked
                    {
                        ++_retainCounter;
                    }
                }

                protected override void MaybeDispose()
                {
                    lock (this)
                    {
                        checked
                        {
                            if (--_retainCounter != 0)
                            {
                                return;
                            }
                        }
                    }
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal override bool GetIsCompleted(short promiseId)
                {
                    lock (this)
                    {
                        if (!GetIsValid(promiseId))
                        {
                            throw new InvalidOperationException("Cannot await a forgotten promise.", GetFormattedStacktrace(3));
                        }
                        bool isCompleted = State != Promise.State.Pending;
                        if (isCompleted)
                        {
                            Retain(); // Retain since Awaiter.GetResult() will be called higher in the stack which will call MaybeDispose indiscriminately.
                        }
                        return isCompleted;
                    }
                }

                internal override PromiseRef GetDuplicate(short promiseId, ushort depth)
                {
                    var newPromise = PromiseDuplicate.GetOrCreate(depth);
                    HookupNewPromise(promiseId, newPromise);
                    return newPromise;
                }

                [MethodImpl(InlineOption)]
                internal override bool GetIsValid(short promiseId)
                {
                    return promiseId == Id & !WasAwaitedOrForgotten;
                }

                protected override void OnForget(short promiseId)
                {
                    lock (this)
                    {
                        if (!GetIsValid(promiseId))
                        {
                            throw new InvalidOperationException("Cannot forget a promise more than once.", GetFormattedStacktrace(3));
                        }
                        WasAwaitedOrForgotten = true;
                        MaybeDispose();
                    }
                }

                internal override void MaybeMarkAwaitedAndDispose(short promiseId)
                {
                    lock (this)
                    {
                        if (!GetIsValid(promiseId))
                        {
                            throw new InvalidOperationException("Cannot await a forgotten promise.", GetFormattedStacktrace(3));
                        }
                    }
                    // We don't need to do any actual work here besides verifying the promise.
                }

                internal override PromiseSingleAwait AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter, ref ExecutionScheduler executionScheduler)
                {
                    lock (this)
                    {
                        if (promiseId != Id | WasAwaitedOrForgotten)
                        {
                            previousWaiter = InvalidAwaitSentinel._instance;
                            return InvalidAwaitSentinel._instance;
                        }
                        ThrowIfInPool(this);

                        if (State == Promise.State.Pending)
                        {
                            _nextBranches.Enqueue(waiter);
                            previousWaiter = null;
                            return null;
                        }
                        Retain(); // Retain since Handle will be called higher in the stack which will call MaybeDispose indiscriminately.
                    }
                    previousWaiter = waiter;
                    return null;
                }

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValueLinkedStack<HandleablePromiseBase> branches;
                    lock (this)
                    {
                        branches = _nextBranches.MoveElementsToStack();
                    }
                    WaitWhileProgressReporting();
                    while (branches.IsNotEmpty)
                    {
                        var waiter = branches.Pop();
                        HandleablePromiseBase nextHandler;
                        PromiseRef handler = this;
                        Retain(); // Retain since Handle will call MaybeDispose indiscriminately.
                        waiter.Handle(ref handler, out nextHandler, ref executionScheduler);
                        handler.MaybeHandleNext(nextHandler, ref executionScheduler);
                    }
                    MaybeDispose();
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    nextHandler = null;
                    handler.SuppressRejection = true;
                    SetResult(handler._valueContainer.Clone(), handler.State);
                    handler.MaybeDispose();
                    executionScheduler.ScheduleSynchronous(this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class PromiseDuplicate : PromiseSingleAwait
            {
                private PromiseDuplicate() { }

                protected override void MaybeDispose()
                {
                    Dispose();
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
                    HandleSelf(ref handler, out nextHandler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseConfigured : PromiseSingleAwait
            {
                private PromiseConfigured() { }

                protected override void MaybeDispose()
                {
                    // TODO: handle properly when Promise.WaitAsync(CancelationToken) is added.
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal static PromiseConfigured GetOrCreate(SynchronizationContext synchronizationContext, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseConfigured>()
                        ?? new PromiseConfigured();
                    promise.Reset(depth);
                    promise._synchronizationContext = synchronizationContext;
                    promise._mostRecentPotentialScheduleMethod = ScheduleMethod.None;
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
                    promise._valueContainer = CreateResolveContainer(result);
                    promise._previousState = Promise.State.Resolved;
                    promise._mostRecentPotentialScheduleMethod = ScheduleMethod.Handle;
                    return promise;
                }

                internal override PromiseConfigured GetConfigured(short promiseId, SynchronizationContext synchronizationContext, ushort depth)
                {
                    // This isn't strictly thread-safe, but when the next promise is awaited, the CompareExchange should catch it.
                    ValidateIdAndNotAwaited(promiseId);
                    _smallFields.IncrementPromiseId();
                    _synchronizationContext = synchronizationContext;
                    return this;
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    // TODO: when cancelations are added, don't suppress rejection if this is canceled.
                    handler.SuppressRejection = true;
                    _valueContainer = handler._valueContainer.Clone();
                    nextHandler = null;
                    _previousState = handler.State;
                    handler.MaybeDispose();
                    int previousScheduleType = ScheduleMethod.Exchange(this, ref _mostRecentPotentialScheduleMethod, ScheduleMethod.Handle);
                    // Leave pending until this is awaited (OnForget awaits with PromiseForgetSentinel).
                    if (previousScheduleType == ScheduleMethod.AddWaiter)
                    {
                        executionScheduler.ScheduleOnContext(_synchronizationContext, this);
                    }
                }

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    State = _previousState;
                    // We don't need to synchronize access here because this is only called when the previous promise completed and the waiter has already been added, so there are no race conditions.
                    HandleablePromiseBase nextHandler = _waiter;
                    _waiter = InvalidAwaitSentinel._instance;

                    // nextHandler is guaranteed to be non-null here, so we can call HandleNext instead of MaybeHandleNext.
                    HandleNext(nextHandler, ref executionScheduler);
                }

                internal override PromiseSingleAwait AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter, ref ExecutionScheduler executionScheduler)
                {
                    if (promiseId != Id)
                    {
                        previousWaiter = InvalidAwaitSentinel._instance;
                        return InvalidAwaitSentinel._instance;
                    }
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;
                    var previous = CompareExchangeWaiter(waiter, null);

                    // We do the verification process here instead of in the caller, because we need to handle continuations on the synchronization context.
                    if (previous != null && CompareExchangeWaiter(waiter, PromiseCompletionSentinel._instance) != PromiseCompletionSentinel._instance)
                    {
                        previousWaiter = InvalidAwaitSentinel._instance;
                        return InvalidAwaitSentinel._instance;
                    }

                    int previousScheduleType = ScheduleMethod.Exchange(this, ref _mostRecentPotentialScheduleMethod, ScheduleMethod.AddWaiter);
                    if (previousScheduleType == ScheduleMethod.Handle)
                    {
                        executionScheduler.ScheduleOnContext(_synchronizationContext, this);
                    }

                    previousWaiter = null;
                    return this; // It doesn't matter what we return since previousWaiter is set to null.
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class PromiseWaitPromise : PromiseSingleAwait
            {
                [MethodImpl(InlineOption)]
                internal void WaitFor<T>(Promise<T> other, ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    ValidateReturn(other);
                    MaybeDisposePreviousAfterSecondWait(handler);

                    var _ref = other._ref;
                    if (_ref == null)
                    {
                        handler = this;
                        var valueContainer = CreateResolveContainer(other.Result);
                        SetResultAndTakeNextWaiter(valueContainer, Promise.State.Resolved, out nextHandler);
                        return;
                    }

                    SetSecondPrevious(_ref);
                    _ref.InterlockedIncrementProgressReportingCount();

                    HandleablePromiseBase previousWaiter;
                    PromiseSingleAwait promiseSingleAwait = _ref.AddWaiter(other.Id, this, out previousWaiter, ref executionScheduler);
                    if (previousWaiter == null)
                    {
                        _ref.ReportProgressFromAddWaiter(this, Depth, ref executionScheduler);
                        nextHandler = null;
                        return;
                    }
                    _ref.InterlockedDecrementProgressReportingCount();

                    if (!VerifyWaiter(promiseSingleAwait))
                    {
                        throw new InvalidReturnException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", string.Empty);
                    }

                    handler = this;
                    HandleSelf(ref _ref, out nextHandler);
                }

                partial void SetSecondPrevious(PromiseRef other);

#if !PROMISE_PROGRESS && PROMISE_DEBUG
                [MethodImpl(InlineOption)]
                partial void SetSecondPrevious(PromiseRef other)
                {
                    _previous = other;
                }
#endif
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal abstract partial class AsyncPromiseBase : PromiseSingleAwait
            {
                [MethodImpl(InlineOption)]
                new protected void Reset()
                {
                    base.Reset();
#if PROMISE_PROGRESS
                    _smallFields._currentProgress = default(Fixed32);
#endif
                }

                internal override PromiseSingleAwait AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter, ref ExecutionScheduler executionScheduler)
                {
                    return AddWaiterImpl(promiseId, waiter, out previousWaiter, 0, ref executionScheduler);
                }

                protected void HandleInternal(ValueContainer valueContainer, Promise.State state)
                {
                    HandleablePromiseBase nextHandler;
                    SetResultAndTakeNextWaiter(valueContainer, state, out nextHandler);
                    var executionScheduler = new ExecutionScheduler(true);
                    MaybeHandleNext(nextHandler, ref executionScheduler);
                    executionScheduler.Execute();
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

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (handler.State == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler);
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

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (handler.State == Promise.State.Resolved)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler);
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

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
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
                        handler.SuppressRejection = true;
                        invokingRejected = true;
                        handlerDisposedAfterCallback = true;
                        rejectCallback.InvokeRejecter(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler);
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

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    _rejecter = default(TRejecter);
                    var state = handler.State;
                    if (state == Promise.State.Resolved)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        resolveCallback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        handler.SuppressRejection = true;
                        invokingRejected = true;
                        handlerDisposedAfterCallback = true;
                        rejectCallback.InvokeRejecter(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler);
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

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    handler.SuppressRejection = true;
                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    handlerDisposedAfterCallback = true;
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

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    handler.SuppressRejection = true;
                    handlerDisposedAfterCallback = true;
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

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    handler.SuppressRejection = true;
                    var callback = _finalizer;
                    _finalizer = default(TFinalizer);
                    try
                    {
                        callback.Invoke();
                    }
                    catch
                    {
                        // Unlike normal finally clauses, we won't swallow the previous rejection. Instead, we send it to the uncaught rejection handler.
                        handler._valueContainer.AddToUnhandledStack();
                        MaybeDisposePrevious(handler);
                        throw;
                    }
                    HandleSelf(ref handler, out nextHandler);
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

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    if (handler.State == Promise.State.Canceled)
                    {
                        callback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler);
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

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                protected override void Execute(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback, ref ExecutionScheduler executionScheduler)
                {
                    if (_canceler.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(ref handler, out nextHandler);
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    if (handler.State == Promise.State.Canceled)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        callback.InvokeResolver(ref handler, out nextHandler, this, ref executionScheduler);
                    }
                    else
                    {
                        HandleSelf(ref handler, out nextHandler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromisePassThrough : HandleablePromiseBase, ILinked<PromisePassThrough>
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

                internal short Id
                {
                    [MethodImpl(InlineOption)]
                    get
                    {
                        ThrowIfInPool(this);
                        return _smallFields._id;
                    }
                }

                private PromisePassThrough() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ~PromisePassThrough()
                {
                    try
                    {
                        if (!_smallFields._disposed)
                        {
                            // For debugging. This should never happen.
                            string message = "A PromisePassThrough was garbage collected without it being released."
                                + " _id: " + _smallFields._id + ", _index: " + _smallFields._index + ", _target: " + _target + ", _owner: " + _owner
#if PROMISE_PROGRESS
                                + ", _depth: " + _smallFields._depth + ", _currentProgress: " + _smallFields._currentProgress.ToDouble()
#endif
                                ;
                            AddRejectionToUnhandledStack(new UnreleasedObjectException(message), _target);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        AddRejectionToUnhandledStack(e, _target);
                    }
                }
#endif

                internal static PromisePassThrough GetOrCreate(Promise owner, int index)
                {
                    // owner._ref is checked for nullity before passing into this.
                    var passThrough = ObjectPool<HandleablePromiseBase>.TryTake<PromisePassThrough>()
                        ?? new PromisePassThrough();
                    passThrough._owner = owner._target._ref;
                    passThrough._smallFields._id = owner._target.Id;
                    passThrough._smallFields._index = index;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    passThrough._smallFields._disposed = false;
#endif
                    passThrough.SetDepth(owner._target.Depth);
                    return passThrough;
                }

                partial void SetDepth(ushort depth);
                partial void SetInitialProgress();

                internal void SetTargetAndAddToOwner(MultiHandleablePromiseBase target)
                {
                    ThrowIfInPool(this);
                    _target = target;
                    SetInitialProgress();
                    _owner.HookupNewWaiter(_smallFields._id, this);
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (handler != _owner)
                    {
                        throw new InvalidOperationException("Passthrough was handled with a handler other than its owner.");
                    }
#endif
                    handler = _target;
                    _target.Handle(this, out nextHandler, ref executionScheduler);
                    _owner.MaybeDispose();
                    Dispose();
                }

                internal void Dispose()
                {
                    ThrowIfInPool(this);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _smallFields._disposed = true;
#endif
                    _owner = null;
                    _target = null;
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }
            } // PromisePassThrough

            partial struct SmallFields
            {
                [MethodImpl(InlineOption)]
                internal void IncrementPromiseId()
                {
                    unchecked
                    {
                        ++_promiseId;
                    }
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTryIncrementDeferredId(short deferredId)
                {
                    // Hopefully in the future we can just use Interlocked.CompareExchange directly on _deferredId. https://github.com/dotnet/runtime/issues/64658
                    Thread.MemoryBarrier();
                    short incrementedId = (short) (deferredId + 1);
                    SmallFields initialValue = default(SmallFields);
                    SmallFields newValue = default(SmallFields);
                    while (true)
                    {
                        initialValue._idInterlocker = _idInterlocker;
                        newValue._idInterlocker = initialValue._idInterlocker;
                        short oldId = initialValue._deferredId;
                        initialValue._deferredId = deferredId;
                        unchecked // We want the id to wrap around.
                        {
                            newValue._deferredId = incrementedId;
                        }
                        // In the majority of cases, this will succeed on the first try.
                        if (Interlocked.CompareExchange(ref _idInterlocker, newValue._idInterlocker, initialValue._idInterlocker) == initialValue._idInterlocker)
                        {
                            return true;
                        }
                        // Make sure id matches before trying again.
                        if (deferredId != oldId)
                        {
                            return false;
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedIncrementDeferredId()
                {
                    Thread.MemoryBarrier();
                    SmallFields initialValue = default(SmallFields);
                    SmallFields newValue = default(SmallFields);
                    do
                    {
                        initialValue._idInterlocker = _idInterlocker;
                        newValue._idInterlocker = initialValue._idInterlocker;
                        unchecked // We want the id to wrap around.
                        {
                            ++newValue._deferredId;
                        }
                    } while (Interlocked.CompareExchange(ref _idInterlocker, newValue._idInterlocker, initialValue._idInterlocker) != initialValue._idInterlocker);
                }

                [MethodImpl(InlineOption)]
                internal void Reset()
                {
                    _state = Promise.State.Pending;
                    _wasAwaitedorForgotten = false;
                    _suppressRejection = false;
                }
            } // SmallFields

            internal static void MaybeMarkAwaitedAndDispose(PromiseRef promise, short id, bool suppressRejection)
            {
                if (promise != null)
                {
                    promise.MaybeMarkAwaitedAndDispose(id);
                    promise.SuppressRejection = suppressRejection;
                }
            }
        } // PromiseRef

        [MethodImpl(InlineOption)]
        internal static void MaybeMarkAwaitedAndDispose(PromiseRef promise, short id, bool suppressRejection)
        {
            PromiseRef.MaybeMarkAwaitedAndDispose(promise, id, suppressRejection);
        }

        internal static void PrepareForMerge(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs,
            int index, ref int pendingAwaits, ref ulong completedProgress, ref ulong totalProgress, ref ushort maxDepth)
        {
            VoidResult voidResult = default(VoidResult);
            PrepareForMerge(promise._target, ref voidResult, ref passThroughs, index, ref pendingAwaits, ref completedProgress, ref totalProgress, ref maxDepth);
        }

        internal static void PrepareForMerge<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs,
            int index, ref int pendingAwaits, ref ulong completedProgress, ref ulong totalProgress, ref ushort maxDepth)
        {
            unchecked
            {
                uint expectedProgress = promise.Depth + 1u;
                if (promise._ref == null)
                {
                    completedProgress += expectedProgress;
                    value = promise.Result;
                }
                else
                {
                    passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
                    checked
                    {
                        ++pendingAwaits;
                    }
                }
                totalProgress += expectedProgress;
                maxDepth = Math.Max(maxDepth, promise.Depth);
            }
        }

        internal static bool TryPrepareForRace(Promise promise, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index, ref ushort minDepth)
        {
            VoidResult voidResult = default(VoidResult);
            return TryPrepareForRace(promise._target, ref voidResult, ref passThroughs, index, ref minDepth);
        }

        internal static bool TryPrepareForRace<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRef.PromisePassThrough> passThroughs, int index, ref ushort minDepth)
        {
            bool isPending = promise._ref != null;
            if (!isPending)
            {
                value = promise.Result;
            }
            else
            {
                passThroughs.Push(PromiseRef.PromisePassThrough.GetOrCreate(promise, index));
            }
            minDepth = Math.Min(minDepth, promise.Depth);
            return isPending;
        }

        [MethodImpl(InlineOption)]
        internal static Promise CreateResolved(ushort depth)
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging.
            return CreateResolved(new VoidResult(), depth);
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
            // Make a promise on the heap to capture causality trace and help with debugging.
            var promise = PromiseRef.DeferredPromise<T>.GetOrCreate();
            promise.ResolveDirect(value);
            return new Promise<T>(promise, promise.Id, depth);
#else
            // Make a promise on the stack for efficiency.
            return new Promise<T>(null, ValidIdFromApi, depth, value);
#endif
        }
    } // Internal
}