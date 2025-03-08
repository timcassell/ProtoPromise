#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0016 // Use 'throw' expression

using Proto.Promises.Collections;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    partial struct Promise { }

#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    partial struct Promise<T> { }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract partial class PromiseRefBase : PromiseRefBaseWithStructField, ITraceable
        {
            internal void HandleSelfWithoutResult(PromiseRefBase handler, Promise.State state)
            {
                ThrowIfInPool(this);
                RejectContainer = handler.RejectContainer;
                handler.SuppressRejection = true;
                handler.MaybeDispose();
                HandleNextInternal(state);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class PromiseRef<TResult> : PromiseRefBase
            {
                internal void HandleSelf(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    _result = handler.GetResult<TResult>();
                    HandleSelfWithoutResult(handler, state);
                }

                internal abstract PromiseRef<TResult> GetDuplicateT(short promiseId);

                [MethodImpl(InlineOption)]
                new protected void Dispose()
                {
                    base.Dispose();
                    ClearReferences(ref _result);
                }
            }

            // TODO: If/when we remove `Promise.Preserve()`, we can also remove this MaybeMarkAwaitedAndDispose method, and just use Forget instead.
            internal abstract void MaybeMarkAwaitedAndDispose(short promiseId);
            internal abstract void MaybeDispose();
            // TODO: We can remove this virtual GetIsCompleted call and make it a simple State check instead.
            // Doing so will require removing `Promise.Preserve()` API, so it will need a major version update.
            // We will also need to move the `_state = Promise.State.Pending` from ResetWithoutStacktrace() to Dispose().
            internal abstract bool GetIsCompleted(short promiseId);
            internal abstract void Forget(short promiseId);
            internal abstract PromiseRefBase GetDuplicate(short promiseId);
            internal abstract PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter);
            internal abstract bool GetIsValid(short promiseId);

            internal short Id
            {
                [MethodImpl(InlineOption)]
                get => _promiseId;
            }

            internal Promise.State State
            {
                [MethodImpl(InlineOption)]
                get => _state;
                [MethodImpl(InlineOption)]
                private set => _state = value;
            }
            internal bool SuppressRejection
            {
                [MethodImpl(InlineOption)]
                get => _suppressRejection;
                [MethodImpl(InlineOption)]
                set => _suppressRejection = value;
            }

            protected bool WasAwaitedOrForgotten
            {
                [MethodImpl(InlineOption)]
                get => _wasAwaitedOrForgotten;
                [MethodImpl(InlineOption)]
                set => _wasAwaitedOrForgotten = value;
            }

            protected PromiseRefBase() { }

            ~PromiseRefBase()
            {
                if (!WasAwaitedOrForgotten)
                {
                    // Promise was not awaited or forgotten.
                    string message = $"A Promise's resources were garbage collected without it being awaited. You must await, return, or forget each promise. {this}";
                    ReportRejection(new UnobservedPromiseException(message), this);
                }
                MaybeReportUnhandledRejection(State);
            }

            [MethodImpl(InlineOption)]
            protected void ResetWithoutStacktrace()
            {
                _next = PendingAwaitSentinel.s_instance;
                _state = Promise.State.Pending;
                _wasAwaitedOrForgotten = false;
                _suppressRejection = false;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _disposed = false;
#endif
            }

            [MethodImpl(InlineOption)]
            protected void Reset()
            {
                ResetWithoutStacktrace();
                SetCreatedStacktrace(this, 3);
            }

            [MethodImpl(InlineOption)]
            protected void PrepareEarlyDispose()
            {
                // Dispose validates the state is not pending in Debug or Developer mode,
                // so we only set the state for early dispose in those modes.
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                SetCompletionState(Promise.State.Resolved);
#endif
                // Suppress the UnobservedPromiseException from the finalizer.
                WasAwaitedOrForgotten = true;
            }

            [MethodImpl(InlineOption)]
            private void Dispose()
            {
                ThrowIfInPool(this);

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (State == Promise.State.Pending)
                {
                    throw new System.InvalidOperationException("Promise disposed while pending: " + this);
                }
                if (!WasAwaitedOrForgotten)
                {
                    throw new System.InvalidOperationException("Promise disposed without being awaited or forgotten: " + this);
                }
                _disposed = true;
#endif
                IncrementPromiseIdAndClearPrevious();
            }

            [MethodImpl(InlineOption)]
            protected void IncrementPromiseIdAndClearPrevious()
            {
                IncrementPromiseId();
                RejectContainer = null;
                // RejectContainer shares a field with ContinuationContext.
                //ContinuationContext = null;
                this.SetPrevious(null);
            }

            [MethodImpl(InlineOption)]
            private void IncrementPromiseId()
            {
                unchecked // We allow the id to wrap around for infinite re-use.
                {
                    ++_promiseId;
                }
            }

            internal TPromise HookupCancelablePromise<TPromise>(TPromise promise, short promiseId, CancelationToken cancelationToken, ref CancelationHelper cancelationHelper)
                where TPromise : PromiseRefBase, ICancelable
            {
                promise.SetPrevious(this);
                cancelationHelper.Register(cancelationToken, promise); // IMPORTANT - must register after promise is fully setup.
                HookupNewWaiter(promiseId, promise);
                return promise;
            }

            internal void HookupNewPromise(short promiseId, PromiseRefBase newPromise)
            {
                newPromise.SetPrevious(this);
                HookupNewWaiter(promiseId, newPromise);
            }

            internal void HookupNewWaiter(short promiseId, HandleablePromiseBase waiter)
            {
                try
                {
                    HookupExistingWaiter(promiseId, waiter);
                }
                catch (InvalidOperationException)
                {
                    // We're already throwing InvalidOperationException here, so we don't want the waiter object to also add exceptions from its finalizer.
                    Discard(waiter);
                    throw;
                }
            }

            internal void HookupExistingWaiter(short promiseId, HandleablePromiseBase waiter)
            {
                PromiseRefBase promiseSingleAwait = AddWaiter(promiseId, waiter, out var previousWaiter);
                if (previousWaiter != PendingAwaitSentinel.s_instance)
                {
                    VerifyAndHandleWaiter(waiter, promiseSingleAwait);
                }
            }

            // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void VerifyAndHandleWaiter(HandleablePromiseBase waiter, PromiseRefBase promiseSingleAwait)
            {
                if (!VerifyWaiter(promiseSingleAwait))
                {
                    throw new InvalidOperationException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", GetFormattedStacktrace(3));
                }
                ThrowIfInPool(this);

                WaitUntilStateIsNotPending();
                waiter.Handle(this, State);
            }

            [MethodImpl(InlineOption)]
            internal void WaitUntilStateIsNotPending()
            {
                // This is completed, but it may have been completed on another thread, so we have to wait until the state is set.
                if (State == Promise.State.Pending)
                {
                    WaitUntilStateIsNotPendingCore();
                }
            }

            private void WaitUntilStateIsNotPendingCore()
            {
                var spinner = new SpinWait();
                while (State == Promise.State.Pending)
                {
                    spinner.SpinOnce();
                }
            }

            [MethodImpl(InlineOption)]
            internal void SetCompletionState(Promise.State state)
            {
                ThrowIfInPool(this);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (state == Promise.State.Pending)
                {
                    throw new System.InvalidOperationException("Cannot SetCompletionState with a pending state.");
                }
#endif
                State = state;
            }

            [MethodImpl(InlineOption)]
            internal TResult GetResult<TResult>()
                // null check is same as typeof(TResult).IsValueType, but is actually optimized away by the JIT. This prevents the type check when TResult is a reference type.
                => null != default(TResult) && typeof(TResult) == typeof(VoidResult)
                    ? default
                    : this.UnsafeAs<PromiseRef<TResult>>()._result;

            [MethodImpl(InlineOption)]
            protected void HandleNextInternal(Promise.State state)
            {
                // We pass the state to the waiter instead of setting it here, because we check the state for completion, and we must have already swapped the _next field before setting the state.
                // If this was not already awaited, the PendingAwaitSentinel will swap the field to PromiseCompletionSentinel and set the state. All other awaiters will just set the state.
                ThrowIfInPool(this);
                _next.Handle(this, state);
            }

            private static bool VerifyWaiter(PromiseRefBase promise)
                // If the existing waiter is anything except completion sentinel, it's an invalid await.
                // We place another instance in its place to make sure future checks are caught.
                // Promise may be null if it was verified internally, or InvalidAwaitSentinel if it's an invalid await.
                => promise == null || promise.CompareExchangeWaiter(InvalidAwaitSentinel.s_instance, PromiseCompletionSentinel.s_instance) == PromiseCompletionSentinel.s_instance;

            [MethodImpl(InlineOption)]
            private HandleablePromiseBase CompareExchangeWaiter(HandleablePromiseBase waiter, HandleablePromiseBase comparand)
                => Interlocked.CompareExchange(ref _next, waiter, comparand);

            [MethodImpl(InlineOption)]
            private HandleablePromiseBase ReadNextWaiterAndMaybeSetCompleted()
            {
                var nextWaiter = CompareExchangeWaiter(PromiseCompletionSentinel.s_instance, PendingAwaitSentinel.s_instance);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (nextWaiter == PromiseCompletionSentinel.s_instance)
                {
                    throw new System.InvalidOperationException("Cannot complete a promise more than once!");
                }
#endif
                return nextWaiter;
            }

            private void MaybeReportUnhandledRejection(Promise.State state)
            {
                if (state == Promise.State.Rejected & !SuppressRejection)
                {
                    SuppressRejection = true;
                    RejectContainer.ReportUnhandled();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class PromiseSingleAwait<TResult> : PromiseRef<TResult>
            {
                internal sealed override void Forget(short promiseId)
                    => HookupExistingWaiter(promiseId, PromiseForgetSentinel.s_instance);

                internal override void MaybeMarkAwaitedAndDispose(short promiseId)
                    => Forget(promiseId);

                internal override bool GetIsCompleted(short promiseId)
                {
                    ValidateId(promiseId, this, 2);
                    ThrowIfInPool(this);
                    if (CompareExchangeWaiter(InvalidAwaitSentinel.s_instance, PromiseCompletionSentinel.s_instance) == PromiseCompletionSentinel.s_instance)
                    {
                        WasAwaitedOrForgotten = true;
                        WaitUntilStateIsNotPending();
                        return true;
                    }
                    return false;
                }

                [MethodImpl(InlineOption)]
                protected void ValidateIdAndNotAwaited(short promiseId)
                {
                    if (!GetIsValid(promiseId))
                    {
                        throw new InvalidOperationException("Cannot await or forget a non-preserved promise more than once.", GetFormattedStacktrace(3));
                    }
                }

                internal sealed override PromiseRefBase GetDuplicate(short promiseId)
                    => GetDuplicateT(promiseId);

                internal sealed override PromiseRef<TResult> GetDuplicateT(short promiseId)
                {
                    // This isn't strictly thread-safe, but when the next promise is awaited, the CompareExchange should catch it.
                    ValidateIdAndNotAwaited(promiseId);
                    IncrementPromiseId();
                    return this;
                }

                [MethodImpl(InlineOption)]
                internal override sealed bool GetIsValid(short promiseId)
                {
                    var waiter = _next;
                    return promiseId == Id & (waiter == PendingAwaitSentinel.s_instance | waiter == PromiseCompletionSentinel.s_instance);
                }

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    if (promiseId != Id)
                    {
                        previousWaiter = InvalidAwaitSentinel.s_instance;
                        return InvalidAwaitSentinel.s_instance;
                    }
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;
                    previousWaiter = CompareExchangeWaiter(waiter, PendingAwaitSentinel.s_instance);
                    return this;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);

                    // Handler is disposed in Execute, so we need to cache the reject container in case of a RethrowException.
                    var rejectContainer = handler.RejectContainer;
                    bool invokingRejected = false;
                    SetCurrentInvoker(this);
                    try
                    {
                        Execute(handler, state, ref invokingRejected);
                    }
                    catch (RethrowException e)
                    {
                        if (invokingRejected)
                        {
                            RejectContainer = rejectContainer;
                        }
                        else
                        {
                            RejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                            state = Promise.State.Rejected;
                        }
                        HandleNextInternal(state);
                    }
                    catch (OperationCanceledException)
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                    catch (Exception e)
                    {
                        RejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        HandleNextInternal(Promise.State.Rejected);
                    }
                    ClearCurrentInvoker();
                }

                protected virtual void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected) => throw new System.InvalidOperationException();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseMultiAwait<TResult> : PromiseRef<TResult>
            {
                private PromiseMultiAwait() { }

                ~PromiseMultiAwait()
                {
                    if (!WasAwaitedOrForgotten)
                    {
                        WasAwaitedOrForgotten = true; // Stop base finalizer from adding an extra exception.
                        string message = "A preserved Promise's resources were garbage collected without it being forgotten. You must call Forget() on each preserved promise when you are finished with it.";
                        ReportRejection(new UnreleasedObjectException(message), this);
                    }
                    else if (_retainCounter != 0 & State != Promise.State.Pending)
                    {
                        string message = "A preserved Promise had an awaiter created without awaiter.GetResult() called.";
                        ReportRejection(new UnreleasedObjectException(message), this);
                    }
                }

                [MethodImpl(InlineOption)]
                new private void Reset()
                {
                    _retainCounter = 2; // 1 for forget, 1 for completion.
                    base.Reset();
                }

                [MethodImpl(InlineOption)]
                private static PromiseMultiAwait<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseMultiAwait<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseMultiAwait<TResult>()
                        : obj.UnsafeAs<PromiseMultiAwait<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseMultiAwait<TResult> GetOrCreateAndHookup(PromiseRefBase previous, short id)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    previous.HookupNewPromise(id, promise);
                    // We create the temp collection after we hook up in case the operation is invalid.
                    promise._nextBranches = new TempCollectionBuilder<HandleablePromiseBase>(0);
                    return promise;
                }

                private void Retain()
                    => InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);

                internal override void MaybeDispose()
                {
                    lock (this)
                    {
                        if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) != 0)
                        {
                            return;
                        }
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        if (!WasAwaitedOrForgotten)
                        {
                            throw new System.InvalidOperationException("PromiseMultiAwait was disposed completely without being forgotten.");
                        }
#endif
                        _nextBranches.Dispose();
                        // Rejection maybe wasn't caught.
                        // We handle this directly here because we don't add the PromiseForgetSentinel to this type when it is forgotten.
                        MaybeReportUnhandledRejection(State);
                        Dispose();
                    }
                    ObjectPool.MaybeRepool(this);
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

                internal override PromiseRefBase GetDuplicate(short promiseId)
                {
                    var newPromise = PromiseDuplicate<VoidResult>.GetOrCreate();
                    HookupNewPromise(promiseId, newPromise);
                    return newPromise;
                }

                internal override PromiseRef<TResult> GetDuplicateT(short promiseId)
                {
                    var newPromise = PromiseDuplicate<TResult>.GetOrCreate();
                    HookupNewPromise(promiseId, newPromise);
                    return newPromise;
                }

                [MethodImpl(InlineOption)]
                internal override bool GetIsValid(short promiseId)
                    => promiseId == Id & !WasAwaitedOrForgotten;

                internal override void Forget(short promiseId)
                {
                    lock (this)
                    {
                        if (!GetIsValid(promiseId))
                        {
                            throw new InvalidOperationException("Cannot forget a promise more than once.", GetFormattedStacktrace(2));
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

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    lock (this)
                    {
                        if (promiseId != Id | WasAwaitedOrForgotten)
                        {
                            previousWaiter = InvalidAwaitSentinel.s_instance;
                            return InvalidAwaitSentinel.s_instance;
                        }
                        ThrowIfInPool(this);

                        if (State == Promise.State.Pending)
                        {
                            _nextBranches.Add(waiter);
                            previousWaiter = PendingAwaitSentinel.s_instance;
                            return null;
                        }
                        Retain(); // Retain since Handle will be called higher in the stack which will call MaybeDispose indiscriminately.
                    }
                    previousWaiter = waiter;
                    return null;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
                    RejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    _result = handler.GetResult<TResult>();
                    SetCompletionState(state);
                    handler.MaybeDispose();

                    TempCollectionBuilder<HandleablePromiseBase> branches;
                    lock (this)
                    {
                        branches = _nextBranches;
                    }
                    for (int i = 0, max = branches._count; i < max; ++i)
                    {
                        Retain(); // Retain since Handle will call MaybeDispose indiscriminately.
                        branches[i].Handle(this, state);
                    }
                    MaybeDispose();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseDuplicate<TResult> : PromiseSingleAwait<TResult>
            {
                private PromiseDuplicate() { }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static PromiseDuplicate<TResult> GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseDuplicate<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseDuplicate<TResult>()
                        : obj.UnsafeAs<PromiseDuplicate<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseDuplicate<TResult> GetOrCreate()
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
                    HandleSelf(handler, state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RunPromise<TResult, TDelegate> : PromiseSingleAwait<TResult>
                where TDelegate : IDelegateRun
            {
                private RunPromise() { }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static RunPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RunPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RunPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<RunPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static RunPromise<TResult, TDelegate> GetOrCreate(TDelegate runner)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._runner = runner;
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal void ScheduleOnContext(SynchronizationContext context)
                    => ScheduleContextCallback(context, this,
                        obj => obj.UnsafeAs<RunPromise<TResult, TDelegate>>().Run(),
                        obj => obj.UnsafeAs<RunPromise<TResult, TDelegate>>().Run()
                    );

                private void Run()
                {
                    ThrowIfInPool(this);

                    var runner = _runner;
                    _runner = default;

                    SetCurrentInvoker(this);
                    try
                    {
                        runner.Invoke(this);
                    }
                    catch (OperationCanceledException)
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                    catch (Exception e)
                    {
                        RejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        HandleNextInternal(Promise.State.Rejected);
                    }
                    ClearCurrentInvoker();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class RunWaitPromise<TResult, TDelegate> : PromiseWaitPromise<TResult>
                where TDelegate : IDelegateRunPromise
            {
                private RunWaitPromise() { }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static RunWaitPromise<TResult, TDelegate> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RunWaitPromise<TResult, TDelegate>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RunWaitPromise<TResult, TDelegate>()
                        : obj.UnsafeAs<RunWaitPromise<TResult, TDelegate>>();
                }

                [MethodImpl(InlineOption)]
                internal static RunWaitPromise<TResult, TDelegate> GetOrCreate(TDelegate runner)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._runner = runner;
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal void ScheduleOnContext(SynchronizationContext context)
                    => ScheduleContextCallback(context, this,
                        obj => obj.UnsafeAs<RunWaitPromise<TResult, TDelegate>>().Run(),
                        obj => obj.UnsafeAs<RunWaitPromise<TResult, TDelegate>>().Run()
                    );

                private void Run()
                {
                    ThrowIfInPool(this);

                    var runner = _runner;
                    _runner = default;

                    SetCurrentInvoker(this);
                    try
                    {
                        runner.Invoke(this);
                    }
                    catch (OperationCanceledException)
                    {
                        HandleNextInternal(Promise.State.Canceled);
                    }
                    catch (Exception e)
                    {
                        RejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        HandleNextInternal(Promise.State.Rejected);
                    }
                    ClearCurrentInvoker();
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    // The returned promise is handling this.
                    handler.SetCompletionState(state);
                    HandleSelf(handler, state);
                }
            }

            [MethodImpl(InlineOption)]
            internal void WaitFor(Promise other)
            {
                ThrowIfInPool(this);
                ValidateReturn(other);
                this.UnsafeAs<PromiseWaitPromise<VoidResult>>().WaitFor(other._ref, other._id);
            }

            [MethodImpl(InlineOption)]
            internal void WaitFor<TResult>(in Promise<TResult> other)
            {
                ThrowIfInPool(this);
                ValidateReturn(other);
                this.UnsafeAs<PromiseWaitPromise<TResult>>().WaitFor(other._ref, other._result, other._id);
            }

            // This is only used in PromiseWaitPromise<TResult>, but we pulled it out to prevent excess generated generic interface types.
            protected interface IWaitForCompleteHandler
            {
                void HandleHookup(PromiseRefBase handler);
                void HandleNull();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class PromiseWaitPromise<TResult> : PromiseSingleAwait<TResult>
            {
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private readonly struct DefaultCompleteHandler : IWaitForCompleteHandler
                {
                    private readonly PromiseWaitPromise<TResult> _owner;

                    [MethodImpl(InlineOption)]
                    internal DefaultCompleteHandler(PromiseWaitPromise<TResult> owner)
                    {
                        _owner = owner;
                    }

                    [MethodImpl(InlineOption)]
                    void IWaitForCompleteHandler.HandleHookup(PromiseRefBase handler)
                        => _owner.HandleSelf(handler, handler.State);

                    [MethodImpl(InlineOption)]
                    void IWaitForCompleteHandler.HandleNull()
                        => _owner.HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                internal void WaitFor(PromiseRefBase other, short id)
                    => WaitFor(other, id, new DefaultCompleteHandler(this));

                [MethodImpl(InlineOption)]
                internal void WaitFor(PromiseRefBase other, in TResult maybeResult, short id)
                    => WaitFor(other, maybeResult, id, new DefaultCompleteHandler(this));

                [MethodImpl(InlineOption)]
                protected void WaitFor<TCompleteHandler>(PromiseRefBase other, short id, TCompleteHandler completeHandler)
                    where TCompleteHandler : IWaitForCompleteHandler
                {
                    if (other == null)
                    {
                        this.SetPrevious(null);
                        completeHandler.HandleNull();
                        return;
                    }
                    SetSecondPreviousAndWaitFor(other, id, completeHandler);
                }

                [MethodImpl(InlineOption)]
                protected void WaitFor<TCompleteHandler>(PromiseRefBase other, in TResult maybeResult, short id, TCompleteHandler completeHandler)
                    where TCompleteHandler : IWaitForCompleteHandler
                {
                    if (other == null)
                    {
                        _result = maybeResult;
                        this.SetPrevious(null);
                        completeHandler.HandleNull();
                        return;
                    }
                    SetSecondPreviousAndWaitFor(other, id, completeHandler);
                }

                private void SetSecondPreviousAndWaitFor<TCompleteHandler>(PromiseRefBase secondPrevious, short id, TCompleteHandler completeHandler)
                    where TCompleteHandler : IWaitForCompleteHandler
                {
                    PromiseRefBase promiseSingleAwait = secondPrevious.AddWaiter(id, this, out var previousWaiter);
                    this.SetPrevious(secondPrevious);
                    if (previousWaiter != PendingAwaitSentinel.s_instance)
                    {
                        VerifyAndHandleSelf(secondPrevious, promiseSingleAwait, completeHandler);
                    }
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private static void VerifyAndHandleSelf<TCompleteHandler>(PromiseRefBase other, PromiseRefBase promiseSingleAwait, TCompleteHandler completeHandler)
                    where TCompleteHandler : IWaitForCompleteHandler
                {
                    if (!VerifyWaiter(promiseSingleAwait))
                    {
                        throw new InvalidReturnException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", string.Empty);
                    }

                    other.WaitUntilStateIsNotPending();
                    completeHandler.HandleHookup(other);
                }
            }

            // IDelegate to reduce the amount of classes I would have to write (Composition Over Inheritance).
            // Using generics with constraints allows us to use structs to get composition for "free"
            // (no extra object allocation or extra memory overhead, and the compiler will generate the Promise classes for us).
            // The only downside is that more classes are created than if we just used straight interfaces (not a problem with JIT, but makes the code size larger with AOT).

            // Resolve types for more common .Then(onResolved) calls to be more efficient (because the runtime does not allow 0-size structs).

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class PromiseResolve<TResult, TResolver> : PromiseSingleAwait<TResult>
                where TResolver : IDelegateResolveOrCancel
            {
                private PromiseResolve() { }

                [MethodImpl(InlineOption)]
                private static PromiseResolve<TResult, TResolver> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseResolve<TResult, TResolver>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseResolve<TResult, TResolver>()
                        : obj.UnsafeAs<PromiseResolve<TResult, TResolver>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseResolve<TResult, TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolver = resolver;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    _resolver = default;
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(handler, state, this);
                    }
                    else
                    {
                        HandleSelfWithoutResult(handler, state);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class PromiseResolvePromise<TResult, TResolver> : PromiseWaitPromise<TResult>
                where TResolver : IDelegateResolveOrCancelPromise
            {
                private PromiseResolvePromise() { }

                [MethodImpl(InlineOption)]
                private static PromiseResolvePromise<TResult, TResolver> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseResolvePromise<TResult, TResolver>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseResolvePromise<TResult, TResolver>()
                        : obj.UnsafeAs<PromiseResolvePromise<TResult, TResolver>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseResolvePromise<TResult, TResolver> GetOrCreate(TResolver resolver)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolver = resolver;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, state);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default;
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(handler, state, this);
                    }
                    else
                    {
                        HandleSelfWithoutResult(handler, state);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class PromiseResolveReject<TResult, TResolver, TRejecter> : PromiseSingleAwait<TResult>
                where TResolver : IDelegateResolveOrCancel
                where TRejecter : IDelegateReject
            {
                private PromiseResolveReject() { }

                [MethodImpl(InlineOption)]
                private static PromiseResolveReject<TResult, TResolver, TRejecter> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseResolveReject<TResult, TResolver, TRejecter>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseResolveReject<TResult, TResolver, TRejecter>()
                        : obj.UnsafeAs<PromiseResolveReject<TResult, TResolver, TRejecter>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseResolveReject<TResult, TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    var resolveCallback = _resolver;
                    _resolver = default;
                    var rejectCallback = _rejecter;
                    _rejecter = default;
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(handler, state, this);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        var rejectContainer = handler.RejectContainer;
                        handler.SuppressRejection = true;
                        handler.MaybeDispose();
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(rejectContainer, this);
                    }
                    else
                    {
                        HandleSelfWithoutResult(handler, state);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class PromiseResolveRejectPromise<TResult, TResolver, TRejecter> : PromiseWaitPromise<TResult>
                where TResolver : IDelegateResolveOrCancelPromise
                where TRejecter : IDelegateRejectPromise
            {
                private PromiseResolveRejectPromise() { }

                [MethodImpl(InlineOption)]
                private static PromiseResolveRejectPromise<TResult, TResolver, TRejecter> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseResolveRejectPromise<TResult, TResolver, TRejecter>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseResolveRejectPromise<TResult, TResolver, TRejecter>()
                        : obj.UnsafeAs<PromiseResolveRejectPromise<TResult, TResolver, TRejecter>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseResolveRejectPromise<TResult, TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, state);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default;
                    var rejectCallback = _rejecter;
                    _rejecter = default;
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(handler, state, this);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        handler.SuppressRejection = true;
                        invokingRejected = true;
                        rejectCallback.InvokeRejecter(handler, handler.RejectContainer, this);
                    }
                    else
                    {
                        HandleSelfWithoutResult(handler, state);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class PromiseContinue<TResult, TContinuer> : PromiseSingleAwait<TResult>
                where TContinuer : IDelegateContinue
            {
                private PromiseContinue() { }

                [MethodImpl(InlineOption)]
                private static PromiseContinue<TResult, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseContinue<TResult, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseContinue<TResult, TContinuer>()
                        : obj.UnsafeAs<PromiseContinue<TResult, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseContinue<TResult, TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._continuer = continuer;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    handler.SuppressRejection = true;
                    var callback = _continuer;
                    _continuer = default;
                    callback.Invoke(handler, handler.RejectContainer, state, this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class PromiseContinuePromise<TResult, TContinuer> : PromiseWaitPromise<TResult>
                where TContinuer : IDelegateContinuePromise
            {
                private PromiseContinuePromise() { }

                [MethodImpl(InlineOption)]
                private static PromiseContinuePromise<TResult, TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseContinuePromise<TResult, TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseContinuePromise<TResult, TContinuer>()
                        : obj.UnsafeAs<PromiseContinuePromise<TResult, TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseContinuePromise<TResult, TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._continuer = continuer;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, state);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default;
                    handler.SuppressRejection = true;
                    callback.Invoke(handler, handler.RejectContainer, state, this);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class PromiseFinally<TResult, TFinalizer> : PromiseSingleAwait<TResult>
                where TFinalizer : IAction
            {
                private PromiseFinally() { }

                [MethodImpl(InlineOption)]
                private static PromiseFinally<TResult, TFinalizer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseFinally<TResult, TFinalizer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseFinally<TResult, TFinalizer>()
                        : obj.UnsafeAs<PromiseFinally<TResult, TFinalizer>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseFinally<TResult, TFinalizer> GetOrCreate(TFinalizer finalizer)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._finalizer = finalizer;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    var callback = _finalizer;
                    _finalizer = default;
                    _result = handler.GetResult<TResult>();
                    RejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    try
                    {
                        callback.Invoke();
                    }
                    catch
                    {
                        // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                        if (state == Promise.State.Rejected)
                        {
                            RejectContainer.ReportUnhandled();
                            RejectContainer = null; // Null it out in case it's a canceled exception.
                        }
                        throw;
                    }
                    HandleNextInternal(state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class PromiseFinallyWait<TResult, TFinalizer> : PromiseWaitPromise<TResult>
                where TFinalizer : IFunc<Promise>, INullable
            {
                private PromiseFinallyWait() { }

                [MethodImpl(InlineOption)]
                private static PromiseFinallyWait<TResult, TFinalizer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseFinallyWait<TResult, TFinalizer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseFinallyWait<TResult, TFinalizer>()
                        : obj.UnsafeAs<PromiseFinallyWait<TResult, TFinalizer>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseFinallyWait<TResult, TFinalizer> GetOrCreate(TFinalizer finalizer)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._finalizer = finalizer;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_finalizer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleFromReturnedPromise(handler, state);
                        return;
                    }

                    _result = handler.GetResult<TResult>();
                    RejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    var callback = _finalizer;
                    _finalizer = default;
                    Promise result;
                    try
                    {
                        result = callback.Invoke();
                    }
                    catch
                    {
                        if (state == Promise.State.Rejected)
                        {
                            // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                            RejectContainer.ReportUnhandled();
                            RejectContainer = null; // Null it out in case it's a canceled exception.
                        }
                        throw;
                    }
                    // Store the state until the returned promise is complete.
                    _previousState = state;
                    WaitFor(result._ref, result._id, new CompleteHandler(this));
                }

                private void HandleFromReturnedPromise(PromiseRefBase handler, Promise.State state)
                {
                    if (state == Promise.State.Resolved)
                    {
                        state = _previousState;
                    }
                    else
                    {
                        if (_previousState == Promise.State.Rejected)
                        {
                            // Unlike normal finally clauses, we don't swallow the previous rejection. Instead, we report it.
                            RejectContainer.ReportUnhandled();
                        }
                        RejectContainer = handler.RejectContainer;
                    }
                    handler.SuppressRejection = true;
                    handler.MaybeDispose();
                    HandleNextInternal(state);
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private readonly struct CompleteHandler : IWaitForCompleteHandler
                {
                    private readonly PromiseFinallyWait<TResult, TFinalizer> _owner;

                    [MethodImpl(InlineOption)]
                    internal CompleteHandler(PromiseFinallyWait<TResult, TFinalizer> owner)
                    {
                        _owner = owner;
                    }

                    [MethodImpl(InlineOption)]
                    void IWaitForCompleteHandler.HandleHookup(PromiseRefBase handler)
                        => _owner.HandleFromReturnedPromise(handler, handler.State);

                    [MethodImpl(InlineOption)]
                    void IWaitForCompleteHandler.HandleNull()
                        => _owner.HandleNextInternal(_owner._previousState);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class PromiseCancel<TResult, TCanceler> : PromiseSingleAwait<TResult>
                where TCanceler : IDelegateResolveOrCancel
            {
                private PromiseCancel() { }

                [MethodImpl(InlineOption)]
                private static PromiseCancel<TResult, TCanceler> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseCancel<TResult, TCanceler>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseCancel<TResult, TCanceler>()
                        : obj.UnsafeAs<PromiseCancel<TResult, TCanceler>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseCancel<TResult, TCanceler> GetOrCreate(TCanceler canceler)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._canceler = canceler;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    var callback = _canceler;
                    _canceler = default;
                    if (state == Promise.State.Canceled)
                    {
                        callback.InvokeResolver(handler, state, this);
                    }
                    else
                    {
                        HandleSelf(handler, state);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class PromiseCancelPromise<TResult, TCanceler> : PromiseWaitPromise<TResult>
                where TCanceler : IDelegateResolveOrCancelPromise
            {
                private PromiseCancelPromise() { }

                [MethodImpl(InlineOption)]
                private static PromiseCancelPromise<TResult, TCanceler> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseCancelPromise<TResult, TCanceler>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseCancelPromise<TResult, TCanceler>()
                        : obj.UnsafeAs<PromiseCancelPromise<TResult, TCanceler>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseCancelPromise<TResult, TCanceler> GetOrCreate(TCanceler resolver)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._canceler = resolver;
                    return promise;
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, Promise.State state, ref bool invokingRejected)
                {
                    if (_canceler.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler, state);
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default;
                    if (state == Promise.State.Canceled)
                    {
                        callback.InvokeResolver(handler, state, this);
                    }
                    else
                    {
                        HandleSelf(handler, state);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial class PromisePassThrough : HandleablePromiseBase
            {
                protected PromisePassThrough() { }

                [MethodImpl(InlineOption)]
                private static PromisePassThrough GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromisePassThrough>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromisePassThrough()
                        : obj.UnsafeAs<PromisePassThrough>();
                }

                [MethodImpl(InlineOption)]
                internal static PromisePassThrough GetOrCreate(PromiseRefBase owner, PromiseRefBase target, int index)
                {
                    var passThrough = GetOrCreate();
                    passThrough._next = target;
                    passThrough._index = index;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    passThrough._owner = owner;
#endif
                    return passThrough;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    var target = _next;
                    var index = _index;
                    Dispose();
                    target.Handle(handler, state, index);
                }

                private void Dispose()
                {
                    ThrowIfInPool(this);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _owner = null;
#endif
                    ObjectPool.MaybeRepool(this);
                }
            } // class PromisePassThrough
        } // class PromiseRefBase
    } // class Internal
} // namespace Proto.Promises