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

#pragma warning disable IDE0016 // Use 'throw' expression
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

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
#if UNITY_2021_2_OR_NEWER || (!NET_LEGACY && !UNITY_5_5_OR_NEWER)
        partial class PromiseRefBase : System.Threading.Tasks.Sources.IValueTaskSource
        {
            public void GetResult(short token)
            {
                var state = State;
                if (state == Promise.State.Resolved)
                {
                    GetResultForAwaiterVoid(token);
                    return;
                }
                GetExceptionDispatchInfo(state, token).Throw();
            }

            public abstract System.Threading.Tasks.Sources.ValueTaskSourceStatus GetStatus(short token);

            public void OnCompleted(Action<object> continuation, object state, short token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)
            {
                // Ignore flags.
                HookupNewWaiter(token, AwaiterRef<DelegateCaptureVoidVoid<object>>.GetOrCreate(new DelegateCaptureVoidVoid<object>(state, continuation)));
            }

            partial class PromiseRef<TResult> : System.Threading.Tasks.Sources.IValueTaskSource<TResult>
            {
                TResult System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(short token)
                {
                    var state = State;
                    if (state == Promise.State.Resolved)
                    {
                        return GetResultForAwaiter(token);
                    }
                    GetExceptionDispatchInfo(state, token).Throw();
                    throw null; // This will never be reached, but the compiler needs help understanding that.
                }
            }

            partial class PromiseSingleAwait<TResult>
            {
                public override System.Threading.Tasks.Sources.ValueTaskSourceStatus GetStatus(short token)
                {
                    ValidateId(token, this, 2);
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;
                    return CompareExchangeWaiter(InvalidAwaitSentinel.s_instance, PromiseCompletionSentinel.s_instance) == PromiseCompletionSentinel.s_instance
                        ? (System.Threading.Tasks.Sources.ValueTaskSourceStatus) State
                        : System.Threading.Tasks.Sources.ValueTaskSourceStatus.Pending;
                }
            }

            partial class PromiseMultiAwait<TResult>
            {
                public override System.Threading.Tasks.Sources.ValueTaskSourceStatus GetStatus(short token)
                {
                    lock (this)
                    {
                        if (!GetIsValid(token))
                        {
                            throw new InvalidOperationException("Cannot await a forgotten promise.", GetFormattedStacktrace(3));
                        }
                        var state = State;
                        if (state != Promise.State.Pending)
                        {
                            Retain(); // Retain since GetResult() will be called higher in the stack which will call MaybeDispose indiscriminately.
                        }
                        return (System.Threading.Tasks.Sources.ValueTaskSourceStatus) state;
                    }
                }
            }

            partial class InvalidAwaitSentinel
            {
                public override System.Threading.Tasks.Sources.ValueTaskSourceStatus GetStatus(short token) { throw new System.InvalidOperationException("GetStatus called on InvalidAwaitSentinel"); }
            }
        }
#endif

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract partial class PromiseRefBase : HandleablePromiseBase, ITraceable
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class PromiseRef<TResult> : PromiseRefBase
            {
                [MethodImpl(InlineOption)]
                internal void SetResult(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult result)
                {
                    ThrowIfInPool(this);
                    _result = result;
                    // Very important, write State must come after write _result. This is a volatile write, so we don't need a full memory barrier.
                    // State is checked for completion, and if it is read resolved on another thread, _result must have already been written so the other thread can read it.
                    State = Promise.State.Resolved;
                }

                internal void HandleSelf(PromiseRefBase handler)
                {
                    ThrowIfInPool(this);
                    handler.SuppressRejection = true;
                    _result = handler.GetResult<TResult>();
                    _rejectContainer = handler._rejectContainer;
                    // Very important, write State must come after write _result and _rejectContainer. This is a volatile write, so we don't need a full memory barrier.
                    // State is checked for completion, and if it is read not pending on another thread, _result and _rejectContainer must have already been written so the other thread can read them.
                    State = handler.State;
                    handler.MaybeDispose();
                    HandleNextInternal();
                }

                internal void WaitFor(Promise other, PromiseRefBase handler)
                {
                    ThrowIfInPool(this);
                    ValidateReturn(other);
                    MaybeDisposePreviousAfterSecondWait(handler);

                    if (other._ref == null)
                    {
                        State = Promise.State.Resolved;
                        HandleNextInternal();
                        return;
                    }
                    SetSecondPreviousAndWaitFor(other);
                }

                internal void WaitFor(Promise<TResult> other, PromiseRefBase handler)
                {
                    ThrowIfInPool(this);
                    ValidateReturn(other);
                    MaybeDisposePreviousAfterSecondWait(handler);

                    if (other._ref == null)
                    {
                        SetResult(other._result);
                        HandleNextInternal();
                        return;
                    }
                    SetSecondPreviousAndWaitFor(other);
                }

                private void SetSecondPreviousAndWaitFor(Promise other)
                {
                    var _ref = other._ref;
                    SetSecondPrevious(_ref);
                    _ref.InterlockedIncrementProgressReportingCount();

                    HandleablePromiseBase previousWaiter;
                    PromiseRefBase promiseSingleAwait = _ref.AddWaiter(other._id, this, out previousWaiter);
                    if (previousWaiter == null)
                    {
                        ReportProgressFromWaitFor(_ref, other.Depth);
                        return;
                    }
                    _ref.InterlockedDecrementProgressReportingCount();

                    if (!VerifyWaiter(promiseSingleAwait))
                    {
                        throw new InvalidReturnException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", string.Empty);
                    }

                    HandleSelf(_ref);
                }

                internal abstract PromiseRef<TResult> GetDuplicateT(short promiseId, ushort depth);

                internal sealed override PromiseRefBase GetPreserved(short promiseId, ushort depth)
                {
                    return GetPreservedT(promiseId, depth);
                }

                internal PromiseRef<TResult> GetPreservedT(short promiseId, ushort depth)
                {
                    var newPromise = PromiseMultiAwait<TResult>.GetOrCreate(depth);
                    HookupNewPromise(promiseId, newPromise);
                    return newPromise;
                }
            }

            internal abstract void MaybeMarkAwaitedAndDispose(short promiseId);
            protected abstract void MaybeDispose();
            internal abstract bool GetIsCompleted(short promiseId);
            protected abstract void OnForget(short promiseId);
            internal abstract PromiseRefBase GetDuplicate(short promiseId, ushort depth);
            internal abstract PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter);
            internal abstract bool GetIsValid(short promiseId);
            internal abstract PromiseRefBase GetPreserved(short promiseId, ushort depth);

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

            protected PromiseRefBase() { }

            ~PromiseRefBase()
            {
                try
                {
                    if (!WasAwaitedOrForgotten)
                    {
                        // Promise was not awaited or forgotten.
                        string message = "A Promise's resources were garbage collected without it being awaited. You must await, return, or forget each promise.";
                        ReportRejection(new UnobservedPromiseException(message), this);
                    }
                    if (_rejectContainer != null & State == Promise.State.Rejected & !SuppressRejection)
                    {
                        // Rejection maybe wasn't caught. Just add to unhandled stack without dispose.
                        _rejectContainer.AddToUnhandledStack();
                    }
                }
                catch (Exception e)
                {
                    // This should never happen.
                    ReportRejection(e, this);
                }
            }

            internal void Forget(short promiseId)
            {
                OnForget(promiseId);
            }

            [MethodImpl(InlineOption)]
            protected void Reset()
            {
                _next = null;
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
                if (_rejectContainer != null & !SuppressRejection)
                {
                    _rejectContainer.AddToUnhandledStack();
                }
                _rejectContainer = null;
            }

            internal void HookupNewPromise(short promiseId, PromiseRefBase newPromise)
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
                HandleablePromiseBase previousWaiter;
                PromiseRefBase promiseSingleAwait = AddWaiter(promiseId, newWaiter, out previousWaiter);
                if (previousWaiter != null)
                {
                    if (!VerifyWaiter(promiseSingleAwait))
                    {
                        throw new InvalidOperationException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", GetFormattedStacktrace(2));
                    }
                    HandleNext(newWaiter);
                }
            }

            [MethodImpl(InlineOption)]
            private void SetRejectOrCancel(RejectContainer rejectOrCancelContainer, Promise.State state)
            {
                _rejectContainer = rejectOrCancelContainer;
                // Very important, write State must come after write _rejectContainer. This is a volatile write, so we don't need a full memory barrier.
                // State is checked for completion, and if it is read not pending on another thread, _rejectContainer must have already been written so the other thread can read it.
                State = state;
            }

            [MethodImpl(InlineOption)]
            internal TResult GetResult<TResult>()
            {
                // null check is same as typeof(TValue).IsValueType, but is actually optimized away by the JIT. This prevents the type check when TValue is a reference type.
                if (null != default(TResult) && typeof(TResult) == typeof(VoidResult))
                {
                    return default(TResult);
                }
                return this.UnsafeAs<PromiseRef<TResult>>()._result;
            }

            [MethodImpl(InlineOption)]
            private bool TryGetRejectValue<TReject>(out TReject rejectValue)
            {
                return _rejectContainer.TryGetValue(out rejectValue);
            }

            // This can be called instead of MaybeHandleNext when we know the nextHandler is not null.
            internal void HandleNext(HandleablePromiseBase nextHandler)
            {
                WaitWhileProgressReporting();
                // Set the waiter to InvalidAwaitSentinel to break the chain to stop progress reports.
                _next = InvalidAwaitSentinel.s_instance;
                nextHandler.Handle(this);
            }

            internal void MaybeHandleNext(HandleablePromiseBase nextHandler)
            {
                if (nextHandler != null)
                {
                    HandleNext(nextHandler);
                }
            }

            private static bool VerifyWaiter(PromiseRefBase promise)
            {
                // If the existing waiter is anything except completion sentinel, it's an invalid await.
                // We place another instance in its place to make sure future checks are caught.
                // Promise may be null if it was verified internally, or InvalidAwaitSentinel if it's an invalid await.
                return promise == null || promise.CompareExchangeWaiter(InvalidAwaitSentinel.s_instance, PromiseCompletionSentinel.s_instance) == PromiseCompletionSentinel.s_instance;
            }

            [MethodImpl(InlineOption)]
            private HandleablePromiseBase CompareExchangeWaiter(HandleablePromiseBase waiter, HandleablePromiseBase comparand)
            {
                Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _next.
                return Interlocked.CompareExchange(ref _next, waiter, comparand);
            }

            [MethodImpl(InlineOption)]
            internal HandleablePromiseBase TakeNextWaiter()
            {
                var nextWaiter = CompareExchangeWaiter(PromiseCompletionSentinel.s_instance, null);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (nextWaiter == PromiseCompletionSentinel.s_instance)
                {
                    throw new System.InvalidOperationException("Cannot complete a promise more than once!");
                }
#endif
                return nextWaiter;
            }

            [MethodImpl(InlineOption)]
            private void HandleNextInternal()
            {
                ThrowIfInPool(this);
                MaybeHandleNext(TakeNextWaiter());
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void HandleIncompatibleRejection(PromiseRefBase handler)
            {
                handler.SuppressRejection = true;
                SetRejectOrCancel(handler._rejectContainer, handler.State);
                handler.MaybeDispose();
                HandleNextInternal();
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class PromiseSingleAwait<TResult> : PromiseRef<TResult>
            {
                protected sealed override void OnForget(short promiseId)
                {
                    HookupExistingWaiter(promiseId, PromiseForgetSentinel.s_instance);
                }

                internal override void MaybeMarkAwaitedAndDispose(short promiseId)
                {
                    OnForget(promiseId);
                }

                internal override bool GetIsCompleted(short promiseId)
                {
                    ValidateId(promiseId, this, 2);
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;
                    return CompareExchangeWaiter(InvalidAwaitSentinel.s_instance, PromiseCompletionSentinel.s_instance) == PromiseCompletionSentinel.s_instance;
                }

                [MethodImpl(InlineOption)]
                protected void ValidateIdAndNotAwaited(short promiseId)
                {
                    if (!GetIsValid(promiseId))
                    {
                        throw new InvalidOperationException("Cannot await or forget a non-preserved promise more than once.", GetFormattedStacktrace(3));
                    }
                }

                internal sealed override PromiseRefBase GetDuplicate(short promiseId, ushort depth)
                {
                    return GetDuplicateT(promiseId, depth);
                }

                internal sealed override PromiseRef<TResult> GetDuplicateT(short promiseId, ushort depth)
                {
                    // This isn't strictly thread-safe, but when the next promise is awaited, the CompareExchange should catch it.
                    ValidateIdAndNotAwaited(promiseId);
                    _smallFields.IncrementPromiseId();
                    return this;
                }

                [MethodImpl(InlineOption)]
                internal override sealed bool GetIsValid(short promiseId)
                {
                    var waiter = _next;
                    return promiseId == Id & (waiter == null | waiter == PromiseCompletionSentinel.s_instance);
                }

                protected PromiseRefBase AddWaiterImpl(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter, ushort depth)
                {
                    if (promiseId != Id)
                    {
                        previousWaiter = InvalidAwaitSentinel.s_instance;
                        return InvalidAwaitSentinel.s_instance;
                    }
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;
                    previousWaiter = CompareExchangeWaiter(waiter, null);
                    return this;
                }

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    return AddWaiterImpl(promiseId, waiter, out previousWaiter, Depth);
                }

                internal static void MaybeDisposePreviousFromCatch(PromiseRefBase previous, bool dispose)
                {
                    if (dispose)
                    {
                        previous.MaybeDispose();
                    }
                }

                internal override void Handle(PromiseRefBase handler)
                {
                    ThrowIfInPool(this);
                    
                    bool invokingRejected = false;
                    bool handlerDisposedAfterCallback = false;
                    var previousState = handler.State;
                    SetCurrentInvoker(this);
                    try
                    {
                        // Handler is disposed deeper in the call stack, so we only dispose it here if an exception is thrown and it was not disposed before the callback.
                        Execute(handler, ref invokingRejected, ref handlerDisposedAfterCallback);
                    }
                    catch (RethrowException e)
                    {
                        RejectContainer valueContainer;
                        bool isAcceptableRethrow = invokingRejected || (e is ForcedRethrowException && previousState != Promise.State.Resolved);
                        if (!isAcceptableRethrow)
                        {
                            valueContainer = CreateRejectContainer(e, int.MinValue, this);
                            previousState = Promise.State.Rejected;
                        }
                        else
                        {
                            valueContainer = handler._rejectContainer;
                        }
                        MaybeDisposePreviousFromCatch(handler, handlerDisposedAfterCallback);
                        SetRejectAndHandleNext(valueContainer, previousState);
                    }
                    catch (OperationCanceledException)
                    {
                        MaybeDisposePreviousFromCatch(handler, handlerDisposedAfterCallback);
                        SetRejectAndHandleNext(RejectContainer.s_completionSentinel, Promise.State.Canceled);
                    }
                    catch (Exception e)
                    {
                        MaybeDisposePreviousFromCatch(handler, handlerDisposedAfterCallback);
                        var valueContainer = CreateRejectContainer(e, int.MinValue, this);
                        SetRejectAndHandleNext(valueContainer, Promise.State.Rejected);
                    }
                    ClearCurrentInvoker();
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private void SetRejectAndHandleNext(RejectContainer valueContainer, Promise.State state)
                {
                    ThrowIfInPool(this);
                    _rejectContainer = valueContainer;
                    // Very important, write State must come after write _rejectContainer. This is a volatile write, so we don't need a full memory barrier.
                    // State is checked for completion, and if it is read rejected on another thread, _rejectContainer must have already been written so the other thread can read it.
                    State = state;
                    HandleNextInternal();
                }

                protected virtual void Execute(PromiseRefBase handler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback)
                {
                    throw new System.InvalidOperationException();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseMultiAwait<TResult> : PromiseRef<TResult>
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
                            ReportRejection(new UnreleasedObjectException(message), this);
                        }
                        else if (_retainCounter != 0 & State != Promise.State.Pending)
                        {
                            string message = "A preserved Promise had an awaiter created without awaiter.GetResult() called.";
                            ReportRejection(new UnreleasedObjectException(message), this);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ReportRejection(e, this);
                    }
                }

                [MethodImpl(InlineOption)]
                new private void Reset(ushort depth)
                {
                    _retainCounter = 2; // 1 for forget, 1 for completion.
                    base.Reset(depth);
                }

                [MethodImpl(InlineOption)]
                internal static PromiseMultiAwait<TResult> GetOrCreate(ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseMultiAwait<TResult>>()
                        ?? new PromiseMultiAwait<TResult>();
                    promise.Reset(depth);
                    return promise;
                }

                private void Retain()
                {
                    InterlockedAddWithOverflowCheck(ref _retainCounter, 1, -1);
                }

                protected override void MaybeDispose()
                {
                    lock (this)
                    {
                        if (InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0) != 0)
                        {
                            return;
                        }
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        if (!WasAwaitedOrForgotten)
                        {
                            throw new System.InvalidOperationException("PromiseMultiAwait was disposed completely without being forgotten.");
                        }
#endif
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

                internal override PromiseRefBase GetDuplicate(short promiseId, ushort depth)
                {
                    var newPromise = PromiseDuplicate<VoidResult>.GetOrCreate(depth);
                    HookupNewPromise(promiseId, newPromise);
                    return newPromise;
                }

                internal override PromiseRef<TResult> GetDuplicateT(short promiseId, ushort depth)
                {
                    var newPromise = PromiseDuplicate<TResult>.GetOrCreate(depth);
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
                            previousWaiter = null;
                            return null;
                        }
                        Retain(); // Retain since Handle will be called higher in the stack which will call MaybeDispose indiscriminately.
                    }
                    previousWaiter = waiter;
                    return null;
                }

                internal override void HandleFromContext()
                {
                    // If we're not executing on the foreground context, remove the current context so that branches that are scheduled to run in the background can be scheduled in parallel.
                    var currentContext = ts_currentContext;
                    if (currentContext != Promise.Config.ForegroundContext)
                    {
                        ts_currentContext = null;
                    }

                    ValueList<HandleablePromiseBase> branches;
                    lock (this)
                    {
                        ThrowIfInPool(this);
                        branches = _nextBranches;
                        // Remove the branches so progress won't report anymore.
                        _nextBranches = default(ValueList<HandleablePromiseBase>);
                    }
                    WaitWhileProgressReporting();
                    for (int i = 0, max = branches.Count; i < max; ++i)
                    {
                        Retain(); // Retain since Handle will call MaybeDispose indiscriminately.
                        branches[i].Handle(this);
                    }
                    branches.Clear();
                    _nextBranches = branches;
                    MaybeDispose();

                    ts_currentContext = currentContext;
                }

                internal override void Handle(PromiseRefBase handler)
                {
                    ThrowIfInPool(this);
                    handler.SuppressRejection = true;
                    _result = handler.GetResult<TResult>();
                    _rejectContainer = handler._rejectContainer;
                    // Very important, write State must come after write _result and _rejectContainer. This is a volatile write, so we don't need a full memory barrier.
                    // State is checked for completion, and if it is read not pending on another thread, _result and _rejectContainer must have already been written so the other thread can read them.
                    State = handler.State;
                    handler.MaybeDispose();

                    bool canUnwind = StackUnwindHelper.SwapUnwinding(true);
                    if (canUnwind)
                    {
                        StackUnwindHelper.AddHandler(this);
                    }
                    else
                    {
                        HandleFromContext();
                        StackUnwindHelper.InvokeHandlers();
                    }
                    StackUnwindHelper.SwapUnwinding(canUnwind);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseDuplicate<TResult> : PromiseSingleAwait<TResult>
            {
                private PromiseDuplicate() { }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                internal static PromiseDuplicate<TResult> GetOrCreate(ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseDuplicate<TResult>>()
                        ?? new PromiseDuplicate<TResult>();
                    promise.Reset(depth);
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler)
                {
                    ThrowIfInPool(this);
                    HandleSelf(handler);
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseDuplicateCancel<TResult> : PromiseSingleAwait<TResult>, ICancelable
            {
                private PromiseDuplicateCancel() { }

                protected override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                        ObjectPool.MaybeRepool(this);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static PromiseDuplicateCancel<TResult> GetOrCreate(ushort depth, CancelationToken cancelationToken)
                {
                    var promise = ObjectPool.TryTake<PromiseDuplicateCancel<TResult>>()
                        ?? new PromiseDuplicateCancel<TResult>();
                    promise.Reset(depth);
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                internal override void Handle(PromiseRefBase handler)
                {
                    ThrowIfInPool(this);
                    if (_cancelationHelper.TryUnregister(this))
                    {
                        _cancelationHelper.TryRelease();
                        HandleSelf(handler);
                    }
                    else
                    {
                        MaybeDispose();
                        handler.MaybeDispose();
                    }
                }

                void ICancelable.Cancel()
                {
                    HandleFromCancelation();
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseConfigured<TResult> : PromiseSingleAwait<TResult>, ICancelable
            {
                private PromiseConfigured() { }

                protected override void MaybeDispose()
                {
                    if (_cancelationHelper.TryRelease())
                    {
                        Dispose();
                        ObjectPool.MaybeRepool(this);
                    }
                }

                private static PromiseConfigured<TResult> GetOrCreateBase(SynchronizationContext synchronizationContext, ushort depth, bool forceAsync, CancelationToken cancelationToken)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (synchronizationContext == null)
                    {
                        throw new InvalidOperationException("synchronizationContext cannot be null");
                    }
#endif
                    var promise = ObjectPool.TryTake<PromiseConfigured<TResult>>()
                        ?? new PromiseConfigured<TResult>();
                    promise.Reset(depth);
                    promise._synchronizationContext = synchronizationContext;
                    promise._wasCanceled = false;
                    promise._forceAsync = forceAsync;
                    return promise;
                }

                internal static PromiseConfigured<TResult> GetOrCreate(SynchronizationContext synchronizationContext, ushort depth, bool forceAsync, CancelationToken cancelationToken)
                {
                    var promise = GetOrCreateBase(synchronizationContext, depth, forceAsync, cancelationToken);
                    promise._isScheduling = 0;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                internal static PromiseConfigured<TResult> GetOrCreateFromResolved(SynchronizationContext synchronizationContext,
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult result, ushort depth, bool forceAsync, CancelationToken cancelationToken)
                {
                    var promise = GetOrCreateBase(synchronizationContext, depth, forceAsync, cancelationToken);
                    promise._isScheduling = 1;
                    promise._result = result;
                    promise._previousState = Promise.State.Resolved;
                    promise._next = PromiseCompletionSentinel.s_instance;
                    promise._cancelationHelper.Register(cancelationToken, promise); // Very important, must register after promise is fully setup.
                    return promise;
                }

                [MethodImpl(InlineOption)]
                private bool ShouldContinueSynchronous()
                {
                    return !_forceAsync & _synchronizationContext == ts_currentContext;
                }

                internal override void Handle(PromiseRefBase handler)
                {
                    ThrowIfInPool(this);

#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. So use CompareExchange instead
                    if (Interlocked.CompareExchange(ref _isScheduling, 1, 0) != 0)
#else
                    if (Interlocked.Exchange(ref _isScheduling, 1) != 0)
#endif
                    {
                        handler.MaybeDispose();
                        MaybeDispose();
                        return;
                    }

                    handler.SuppressRejection = true;
                    _result = handler.GetResult<TResult>();
                    _rejectContainer = handler._rejectContainer;
                    _previousState = handler.State;
                    handler.MaybeDispose();

                    var next = TakeNextWaiter();
                    // Leave pending until this is awaited.
                    if (next != null)
                    {
                        if (!ShouldContinueSynchronous())
                        {
                            ScheduleForHandle(this, _synchronizationContext);
                            return;
                        }

                        SetCompletionState();
                        HandleNext(next);
                    }
                }

                void ICancelable.Cancel()
                {
                    _wasCanceled = true;
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. So use CompareExchange instead
                    if (Interlocked.CompareExchange(ref _isScheduling, 1, 0) != 0)
#else
                    if (Interlocked.Exchange(ref _isScheduling, 1) != 0)
#endif
                    {
                        MaybeDispose();
                        return;
                    }

                    _rejectContainer = RejectContainer.s_completionSentinel;
                    _previousState = Promise.State.Canceled;

                    // Leave pending until this is awaited.
                    if (CompareExchangeWaiter(PromiseCompletionSentinel.s_instance, null) != null)
                    {
                        if (!ShouldContinueSynchronous())
                        {
                            ScheduleForHandle(this, _synchronizationContext);
                            return;
                        }

                        State = Promise.State.Canceled;
                        HandleNextInternal();
                    }
                }

                private void SetCompletionState()
                {
                    if (_cancelationHelper.TryUnregister(this) & !_wasCanceled)
                    {
                        _cancelationHelper.TryRelease();
                        State = _previousState;
                    }
                    else
                    {
                        var rejectContainer = _rejectContainer;
                        if (rejectContainer != null)
                        {
                            rejectContainer.AddToUnhandledStack();
                        }
                        _rejectContainer = RejectContainer.s_completionSentinel;
                        State = Promise.State.Canceled;
                    }
                }

                internal override void HandleFromContext()
                {
                    var currentContext = ts_currentContext;
                    ts_currentContext = _synchronizationContext;

                    SetCompletionState();
                    // We don't need to synchronize access here because this is only called when the previous promise completed or the token canceled, and the waiter has already been added, so there are no race conditions.
                    // _next is guaranteed to be non-null here, so we can call HandleNext instead of MaybeHandleNext.
                    HandleNext(_next);

                    ts_currentContext = currentContext;
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

                    var previous = CompareExchangeWaiter(waiter, null);
                    if (previous != null)
                    {
                        // We do the verification process here instead of in the caller, because we need to handle continuations on the synchronization context.
                        if (CompareExchangeWaiter(waiter, PromiseCompletionSentinel.s_instance) != PromiseCompletionSentinel.s_instance)
                        {
                            previousWaiter = InvalidAwaitSentinel.s_instance;
                            return InvalidAwaitSentinel.s_instance;
                        }

                        if (ShouldContinueSynchronous())
                        {
                            SetCompletionState();
                            previousWaiter = waiter;
                            return null;
                        }
                        ScheduleForHandle(this, _synchronizationContext);
                    }
                    previousWaiter = null;
                    return null; // It doesn't matter what we return since previousWaiter is set to null.
                }

                internal override bool GetIsCompleted(short promiseId)
                {
                    ValidateId(promiseId, this, 2);
                    ThrowIfInPool(this);
                    // Make sure the continuation happens on the synchronization context.
                    if (ShouldContinueSynchronous()
                        && CompareExchangeWaiter(InvalidAwaitSentinel.s_instance, PromiseCompletionSentinel.s_instance) == PromiseCompletionSentinel.s_instance)
                    {
                        WasAwaitedOrForgotten = true;
                        SetCompletionState();
                        return true;
                    }
                    return false;
                }
            }

#if PROMISE_DEBUG
            private const bool _resolveWillDisposeAfterSecondAwait = true;
#else
            private const bool _resolveWillDisposeAfterSecondAwait = false;
#endif

            [MethodImpl(InlineOption)]
            internal static void MaybeDisposePreviousBeforeSecondWait(PromiseRefBase previous)
            {
#if !PROMISE_DEBUG // Don't dispose before the callback if we're in debug mode so that if a circular promise chain is detected, it will be disposed properly.
                previous.MaybeDispose();
#endif
            }

            [MethodImpl(InlineOption)]
            internal static void MaybeDisposePreviousAfterSecondWait(PromiseRefBase previous)
            {
#if PROMISE_DEBUG // Dispose after the callback if we're in debug mode so that if a circular promise chain is detected, it will be disposed properly.
                previous.MaybeDispose();
#endif
            }

            partial void ReportProgressFromWaitFor(PromiseRefBase other, ushort depth);
            partial void SetSecondPrevious(PromiseRefBase secondPrevious);

#if !PROMISE_PROGRESS && PROMISE_DEBUG
            [MethodImpl(InlineOption)]
            partial void SetSecondPrevious(PromiseRefBase secondPrevious)
            {
                _previous = secondPrevious;
            }
#endif

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class PromiseWaitPromise<TResult> : PromiseSingleAwait<TResult>
            {
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class AsyncPromiseBase<TResult> : PromiseSingleAwait<TResult>
            {
                [MethodImpl(InlineOption)]
                new protected void Reset()
                {
                    base.Reset();
#if PROMISE_PROGRESS
                    _smallFields._currentProgress = default(Fixed32);
#endif
                }

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    return AddWaiterImpl(promiseId, waiter, out previousWaiter, 0);
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
                internal static PromiseResolve<TResult, TResolver> GetOrCreate(TResolver resolver, ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseResolve<TResult, TResolver>>()
                        ?? new PromiseResolve<TResult, TResolver>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    return promise;
                }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (handler.State == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(handler, this);
                    }
                    else
                    {
                        HandleIncompatibleRejection(handler);
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
                internal static PromiseResolvePromise<TResult, TResolver> GetOrCreate(TResolver resolver, ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseResolvePromise<TResult, TResolver>>()
                        ?? new PromiseResolvePromise<TResult, TResolver>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    return promise;
                }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler);
                        return;
                    }

                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    if (handler.State == Promise.State.Resolved)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        resolveCallback.InvokeResolver(handler, this);
                    }
                    else
                    {
                        HandleIncompatibleRejection(handler);
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
                internal static PromiseResolveReject<TResult, TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseResolveReject<TResult, TResolver, TRejecter>>()
                        ?? new PromiseResolveReject<TResult, TResolver, TRejecter>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback)
                {
                    var resolveCallback = _resolver;
                    _resolver = default(TResolver);
                    var rejectCallback = _rejecter;
                    _rejecter = default(TRejecter);
                    var state = handler.State;
                    if (state == Promise.State.Resolved)
                    {
                        resolveCallback.InvokeResolver(handler, this);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        handler.SuppressRejection = true;
                        invokingRejected = true;
                        handlerDisposedAfterCallback = true;
                        rejectCallback.InvokeRejecter(handler, this);
                    }
                    else
                    {
                        HandleIncompatibleRejection(handler);
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
                internal static PromiseResolveRejectPromise<TResult, TResolver, TRejecter> GetOrCreate(TResolver resolver, TRejecter rejecter, ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseResolveRejectPromise<TResult, TResolver, TRejecter>>()
                        ?? new PromiseResolveRejectPromise<TResult, TResolver, TRejecter>();
                    promise.Reset(depth);
                    promise._resolver = resolver;
                    promise._rejecter = rejecter;
                    return promise;
                }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback)
                {
                    if (_resolver.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler);
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
                        resolveCallback.InvokeResolver(handler, this);
                    }
                    else if (state == Promise.State.Rejected)
                    {
                        handler.SuppressRejection = true;
                        invokingRejected = true;
                        handlerDisposedAfterCallback = true;
                        rejectCallback.InvokeRejecter(handler, this);
                    }
                    else
                    {
                        HandleIncompatibleRejection(handler);
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
                internal static PromiseContinue<TResult, TContinuer> GetOrCreate(TContinuer continuer, ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseContinue<TResult, TContinuer>>()
                        ?? new PromiseContinue<TResult, TContinuer>();
                    promise.Reset(depth);
                    promise._continuer = continuer;
                    return promise;
                }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback)
                {
                    handler.SuppressRejection = true;
                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    handlerDisposedAfterCallback = true;
                    callback.Invoke(handler, this);
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
                internal static PromiseContinuePromise<TResult, TContinuer> GetOrCreate(TContinuer continuer, ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseContinuePromise<TResult, TContinuer>>()
                        ?? new PromiseContinuePromise<TResult, TContinuer>();
                    promise.Reset(depth);
                    promise._continuer = continuer;
                    return promise;
                }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback)
                {
                    if (_continuer.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler);
                        return;
                    }

                    var callback = _continuer;
                    _continuer = default(TContinuer);
                    handler.SuppressRejection = true;
                    handlerDisposedAfterCallback = true;
                    callback.Invoke(handler, this);
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
                internal static PromiseFinally<TResult, TFinalizer> GetOrCreate(TFinalizer finalizer, ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseFinally<TResult, TFinalizer>>()
                        ?? new PromiseFinally<TResult, TFinalizer>();
                    promise.Reset(depth);
                    promise._finalizer = finalizer;
                    return promise;
                }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback)
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
                        if (handler._rejectContainer != null)
                        {
                            handler._rejectContainer.AddToUnhandledStack();
                        }
                        handler.MaybeDispose();
                        throw;
                    }
                    HandleSelf(handler);
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
                internal static PromiseCancel<TResult, TCanceler> GetOrCreate(TCanceler canceler, ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseCancel<TResult, TCanceler>>()
                        ?? new PromiseCancel<TResult, TCanceler>();
                    promise.Reset(depth);
                    promise._canceler = canceler;
                    return promise;
                }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback)
                {
                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    if (handler.State == Promise.State.Canceled)
                    {
                        callback.InvokeResolver(handler, this);
                    }
                    else
                    {
                        HandleSelf(handler);
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
                internal static PromiseCancelPromise<TResult, TCanceler> GetOrCreate(TCanceler resolver, ushort depth)
                {
                    var promise = ObjectPool.TryTake<PromiseCancelPromise<TResult, TCanceler>>()
                        ?? new PromiseCancelPromise<TResult, TCanceler>();
                    promise.Reset(depth);
                    promise._canceler = resolver;
                    return promise;
                }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                protected override void Execute(PromiseRefBase handler, ref bool invokingRejected, ref bool handlerDisposedAfterCallback)
                {
                    if (_canceler.IsNull)
                    {
                        // The returned promise is handling this.
                        HandleSelf(handler);
                        return;
                    }

                    var callback = _canceler;
                    _canceler = default(TCanceler);
                    if (handler.State == Promise.State.Canceled)
                    {
                        handlerDisposedAfterCallback = _resolveWillDisposeAfterSecondAwait;
                        callback.InvokeResolver(handler, this);
                    }
                    else
                    {
                        HandleSelf(handler);
                    }
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromisePassThrough : HandleablePromiseBase, ILinked<PromisePassThrough>
            {
                internal PromiseRefBase Owner
                {
                    [MethodImpl(InlineOption)]
                    get
                    {
                        ThrowIfInPool(this);
                        return _ownerOrTarget;
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
                                + " _id: " + _smallFields._id + ", _index: " + _smallFields._index + ", Owner: " + Owner
#if PROMISE_PROGRESS
                                + ", _depth: " + _smallFields._depth + ", _currentProgress: " + _smallFields._currentProgress.ToDouble()
#endif
                                ;
                            ReportRejection(new UnreleasedObjectException(message), Owner);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ReportRejection(e, Owner);
                    }
                }
#endif

                internal static PromisePassThrough GetOrCreate(Promise owner, int index)
                {
                    var passThrough = ObjectPool.TryTake<PromisePassThrough>()
                        ?? new PromisePassThrough();
                    passThrough._ownerOrTarget = owner._ref;
                    passThrough._smallFields._id = owner._id;
                    passThrough._smallFields._index = index;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    passThrough._smallFields._disposed = false;
#endif
                    passThrough.SetDepth(owner.Depth);
                    return passThrough;
                }

                partial void SetDepth(ushort depth);
                partial void SetInitialProgress(PromiseRefBase owner, PromiseRefBase target);

                internal void SetTargetAndAddToOwner(PromiseRefBase target)
                {
                    ThrowIfInPool(this);
                    var owner = _ownerOrTarget;
                    _ownerOrTarget = target;
                    SetInitialProgress(owner, target);
                    owner.HookupNewWaiter(_smallFields._id, this);
                }

                internal override void Handle(PromiseRefBase handler)
                {
                    ThrowIfInPool(this);
                    var target = _ownerOrTarget;
                    _ownerOrTarget = handler;
                    target.Handle(this);
                    _ownerOrTarget.MaybeDispose();
                    Dispose();
                }

                internal void Dispose()
                {
                    ThrowIfInPool(this);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _smallFields._disposed = true;
#endif
                    _ownerOrTarget = null;
                    ObjectPool.MaybeRepool(this);
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

            internal static void MaybeMarkAwaitedAndDispose(PromiseRefBase promise, short id, bool suppressRejection)
            {
                if (promise != null)
                {
                    // Suppress rejection before dispose, since the dispose checks the flag. This may still set the flag if the id doesn't match, but that's not a big deal if it happens.
                    // We only enable SuppressRejection, never disable.
                    promise.SuppressRejection |= suppressRejection;
                    promise.MaybeMarkAwaitedAndDispose(id);
                }
            }
        } // PromiseRefBase

        [MethodImpl(InlineOption)]
        internal static void MaybeMarkAwaitedAndDispose(PromiseRefBase promise, short id, bool suppressRejection)
        {
            PromiseRefBase.MaybeMarkAwaitedAndDispose(promise, id, suppressRejection);
        }

        internal static void PrepareForMerge(Promise promise, ref ValueLinkedStack<PromiseRefBase.PromisePassThrough> passThroughs,
            int index, ref int pendingAwaits, ref ulong completedProgress, ref ulong totalProgress, ref ushort maxDepth)
        {
            unchecked
            {
                uint expectedProgress = promise.Depth + 1u;
                if (promise._ref == null)
                {
                    completedProgress += expectedProgress;
                }
                else
                {
                    passThroughs.Push(PromiseRefBase.PromisePassThrough.GetOrCreate(promise, index));
                    checked
                    {
                        ++pendingAwaits;
                    }
                }
                totalProgress += expectedProgress;
                maxDepth = Math.Max(maxDepth, promise.Depth);
            }
        }

        internal static void PrepareForMerge<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRefBase.PromisePassThrough> passThroughs,
            int index, ref int pendingAwaits, ref ulong completedProgress, ref ulong totalProgress, ref ushort maxDepth)
        {
            unchecked
            {
                uint expectedProgress = promise.Depth + 1u;
                if (promise._ref == null)
                {
                    completedProgress += expectedProgress;
                    value = promise._result;
                }
                else
                {
                    passThroughs.Push(PromiseRefBase.PromisePassThrough.GetOrCreate(promise, index));
                    checked
                    {
                        ++pendingAwaits;
                    }
                }
                totalProgress += expectedProgress;
                maxDepth = Math.Max(maxDepth, promise.Depth);
            }
        }

        internal static bool TryPrepareForRace(Promise promise, ref ValueLinkedStack<PromiseRefBase.PromisePassThrough> passThroughs, int index, ref ushort minDepth)
        {
            bool isPending = promise._ref != null;
            if (isPending)
            {
                passThroughs.Push(PromiseRefBase.PromisePassThrough.GetOrCreate(promise, index));
            }
            minDepth = Math.Min(minDepth, promise.Depth);
            return isPending;
        }

        internal static bool TryPrepareForRace<T>(Promise<T> promise, ref T value, ref ValueLinkedStack<PromiseRefBase.PromisePassThrough> passThroughs, int index, ref ushort minDepth)
        {
            bool isPending = promise._ref != null;
            if (!isPending)
            {
                value = promise._result;
            }
            else
            {
                passThroughs.Push(PromiseRefBase.PromisePassThrough.GetOrCreate(promise, index));
            }
            minDepth = Math.Min(minDepth, promise.Depth);
            return isPending;
        }

        [MethodImpl(InlineOption)]
        internal static Promise CreateResolved(ushort depth)
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging.
            var promise = PromiseRefBase.DeferredPromise<VoidResult>.GetOrCreate();
            promise.ResolveDirect(new VoidResult());
            return new Promise(promise, promise.Id, depth);
#else
            // Make a promise on the stack for efficiency.
            return new Promise(null, 0, depth);
#endif
        }

        [MethodImpl(InlineOption)]
        internal static Promise<T> CreateResolved<T>(T value, ushort depth)
        {
#if PROMISE_DEBUG
            // Make a promise on the heap to capture causality trace and help with debugging.
            var promise = PromiseRefBase.DeferredPromise<T>.GetOrCreate();
            promise.ResolveDirect(value);
            return new Promise<T>(promise, promise.Id, depth);
#else
            // Make a promise on the stack for efficiency.
            return new Promise<T>(null, 0, depth, value);
#endif
        }
    } // Internal
}