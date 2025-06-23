#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        // We use abstract classes instead of interfaces, because virtual calls on interfaces are twice as slow as virtual calls on classes.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract partial class HandleablePromiseBase : ILinked<HandleablePromiseBase>
        {
            HandleablePromiseBase ILinked<HandleablePromiseBase>.Next
            {
                [MethodImpl(InlineOption)]
                get => _next;
                [MethodImpl(InlineOption)]
                set => _next = value;
            }

            internal virtual void Handle(PromiseRefBase handler, Promise.State state) => throw new System.InvalidOperationException();
            // For PromisePassThrough
            internal virtual void Handle(PromiseRefBase handler, Promise.State state, int index) => throw new System.InvalidOperationException();
        }

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
                    spinner.SpinOnce(sleep1Threshold: -1);
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

            internal virtual void MaybeReportUnhandledAndDispose(Promise.State state)
            {
                MaybeReportUnhandledRejection(state);
                MaybeDispose();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class SingleAwaitPromise<TResult> : PromiseRef<TResult>
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
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PreservedPromise<TResult> : PromiseRef<TResult>
            {
                private PreservedPromise() { }

                ~PreservedPromise()
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
                private static PreservedPromise<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PreservedPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PreservedPromise<TResult>()
                        : obj.UnsafeAs<PreservedPromise<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> New(Promise previous)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise.SetPrevious(previous._ref);
                    previous._ref.HookupNewWaiter(previous._id, promise);
                    // We create the temp collection after we hook up in case the operation is invalid.
                    promise._nextBranches = new TempCollectionBuilder<HandleablePromiseBase>(0);
                    return new Promise<TResult>(promise);
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

                internal override void MaybeReportUnhandledAndDispose(Promise.State state)
                    // We don't report unhandled rejection here unless none of the waiters suppressed.
                    // This way we only report it once in case multiple waiters were canceled.
                    => MaybeDispose();

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
                    => GetDuplicateT(promiseId);

                internal override PromiseRef<TResult> GetDuplicateT(short promiseId)
                    => DuplicatePromise<TResult>.New(new Promise(this, promiseId))._ref;

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
            internal sealed partial class DuplicatePromise<TResult> : SingleAwaitPromise<TResult>
            {
                private DuplicatePromise() { }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private static DuplicatePromise<TResult> GetOrCreateInstance()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<DuplicatePromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new DuplicatePromise<TResult>()
                        : obj.UnsafeAs<DuplicatePromise<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> New(Promise previous)
                {
                    var promise = GetOrCreateInstance();
                    promise.Reset();
                    promise.SetPrevious(previous._ref);
                    previous._ref.HookupNewWaiter(previous._id, promise);
                    return new Promise<TResult>(promise);
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
            private sealed class AppendResultPromise<TResult> : SingleAwaitPromise<TResult>
            {
                private AppendResultPromise() { }

                [MethodImpl(InlineOption)]
                private static AppendResultPromise<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AppendResultPromise<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AppendResultPromise<TResult>()
                        : obj.UnsafeAs<AppendResultPromise<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static Promise<TResult> New(Promise previous, in TResult result)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result = result;
                    promise.SetPrevious(previous._ref);
                    previous._ref.HookupNewWaiter(previous._id, promise);
                    return new Promise<TResult>(promise);
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);
                    HandleSelfWithoutResult(handler, state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class AppendResultPromise<TResult, TAppend> : SingleAwaitPromise<(TResult, TAppend)>
            {
                private AppendResultPromise() { }

                [MethodImpl(InlineOption)]
                private static AppendResultPromise<TResult, TAppend> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AppendResultPromise<TResult, TAppend>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AppendResultPromise<TResult, TAppend>()
                        : obj.UnsafeAs<AppendResultPromise<TResult, TAppend>>();
                }

                [MethodImpl(InlineOption)]
                internal static Promise<(TResult, TAppend)> New(Promise previous, in TAppend value)
                {
                    var promise = GetOrCreate();
                    promise.Reset();
                    promise._result.Item2 = value;
                    promise.SetPrevious(previous._ref);
                    previous._ref.HookupNewWaiter(previous._id, promise);
                    return new Promise<(TResult, TAppend)>(promise);
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);

                    handler.SetCompletionState(state);
                    _result.Item1 = handler.GetResult<TResult>();
                    HandleSelfWithoutResult(handler, state);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class CallbackWaitPromiseBase<TResult> : SingleAwaitPromise<TResult>
            {
                protected new void Reset()
                {
                    base.Reset();
                    _firstContinue = true;
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