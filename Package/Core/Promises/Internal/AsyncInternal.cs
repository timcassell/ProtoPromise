#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

// Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
// Unity fixed in 2020.3.20f1 and 2021.1.24f1, but it's simpler to just check for 2021.2 or newer.
// Don't use optimized mode in DEBUG mode for causality traces.
#if (ENABLE_IL2CPP && !UNITY_2021_2_OR_NEWER) || PROMISE_DEBUG
#undef OPTIMIZED_ASYNC_MODE
#else
#define OPTIMIZED_ASYNC_MODE
#endif

#pragma warning disable IDE0074 // Use compound assignment

using Proto.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
#if NETCOREAPP
        private const MethodImplOptions AggressiveOptimizationOption = MethodImplOptions.AggressiveOptimization;
#else
        private const MethodImplOptions AggressiveOptimizationOption = 0;
#endif

        partial class PromiseRefBase
        {
            // The trace of where a traced async function last awaited, which is entered outside of its own trace when it continues (see MoveNextInScope in the traced async types),
            // like the trace of where a callback was scheduled (see ScheduleContextCallback), or null. Its scope is exited with the async promise's scope (see CausalityTrace.ExitScopeIfCurrent).
            // It's null for other promises.
            internal virtual CausalityTrace EnteredAwaitTrace => null;

            // NoInlining wrapper to prevent inlining and expanding GDV (guarded devirtualization) into the caller.
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void CaptureForAwait(bool isFirstAwait)
            {
                // AsyncFlowExecutionContextEnabled is checked too, because causality traces may have been disabled since s_isAwaitCaptureEnabled was read.
                // Otherwise (rare), the virtual call checks each setting.
                if (!CausalityTrace.IsEnabled & Promise.Config.AsyncFlowExecutionContextEnabled)
                {
                    // Common case: only the ExecutionContext flows (see Promise.Config.s_isAwaitCaptureEnabled).
                    // Capture returns null if the flow is suppressed.
                    ContinuationContext = ExecutionContext.Capture();
                    return;
                }
                CaptureAwaitContextAndOrTrace(isFirstAwait);
            }

            // Captures the ExecutionContext to continue the async function on, if it flows. Traced async promises also capture where it awaits, if await traces are enabled (see EnteredAwaitTrace).
            internal virtual void CaptureAwaitContextAndOrTrace(bool isFirstAwait)
            {
                if (Promise.Config.AsyncFlowExecutionContextEnabled)
                {
                    // Capture returns null if the flow is suppressed.
                    ContinuationContext = ExecutionContext.Capture();
                }
            }

            // Traced async promises add this in place of their waiter (see AsyncPromiseRef.TracedAsyncPromiseRef and AsyncPromiseRef.TracedAsyncPromiseRefMachine),
            // so that when an async function completes inside its own scope (its body completes it), the waiter, and everything that it continues synchronously,
            // runs outside of that scope, as if it was continued from where the async function was called.
            // The async function's trace is where it was called, which is usually already in the waiter's frames (e.g. the code that awaited it called it),
            // and nesting would make the scopes of a chain of async functions that complete synchronously grow with its depth.
            // The scope of where it last awaited (see EnteredAwaitTrace) is exited with it, for the same reason.
            // It's a separate type, so that untraced async promises don't pay for it when they complete.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class ScopeExitingWaiter : HandleablePromiseBase
            {
                private readonly HandleablePromiseBase _waiter;

                internal ScopeExitingWaiter(HandleablePromiseBase waiter)
                {
                    _waiter = waiter;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    var exitedScope = CausalityTrace.ExitScopeIfCurrent(handler);
                    if (exitedScope._trace == null)
                    {
                        _waiter.Handle(handler, state);
                        return;
                    }
                    try
                    {
                        _waiter.Handle(handler, state);
                    }
                    finally
                    {
                        exitedScope.Dispose();
                    }
                }
            }

            [MethodImpl(InlineOption)]
            internal void HookupAwaiter(PromiseRefBase awaiter, short promiseId)
            {
                ValidateAwait(awaiter, promiseId);
                this.SetPrevious(awaiter);
                awaiter.HookupExistingWaiter(promiseId, this);
            }

            [MethodImpl(InlineOption)]
            internal void HookupAwaiterWithContext(PromiseRefBase awaiter, short promiseId, SynchronizationContext synchronizationContext)
            {
                ValidateAwait(awaiter, promiseId);

                if (awaiter == null)
                {
                    // The awaited promise was already complete, and the await was configured to continue asynchronously.
                    ContinueOnContext(synchronizationContext);
                    return;
                }

                // The field is null or an ExecutionContext (see ContinueOnContextAfterCompleted).
                var context = ContinuationContext;
                ContinuationContext = context != null
                    ? ConfiguredAwaitDualContext.GetOrCreate(synchronizationContext, context.UnsafeAs<ExecutionContext>())
                    : (object) synchronizationContext;

                this.SetPrevious(awaiter);

                // The same as HookupExistingWaiter, except that if the awaited promise already completed, this doesn't capture where it was scheduled (see ContinueOnContextAfterCompleted).
                PromiseRefBase promiseSingleAwait = awaiter.AddWaiter(promiseId, this, out var previousWaiter);
                if (previousWaiter != PendingAwaitSentinel.s_instance)
                {
                    ContinueOnContextAfterCompleted(awaiter, promiseSingleAwait, synchronizationContext, context);
                }
            }

            // The awaited promise completed before this was added as its waiter (or before it was awaited), so this continues the same as if it was complete without a backing reference.
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void ContinueOnContextAfterCompleted(PromiseRefBase awaiter, PromiseRefBase promiseSingleAwait, SynchronizationContext synchronizationContext, object previousContext)
            {
                // Restore the previous context before the await is verified in case it's invalid and throws.
                if (previousContext != null)
                {
                    ContinuationContext.UnsafeAs<ConfiguredAwaitDualContext>().Dispose();
                }
                ContinuationContext = previousContext;
                awaiter.VerifyCompletedAwait(promiseSingleAwait);
                this.SetPrevious(null);
                ContinueOnContext(synchronizationContext);
            }

#if NETCOREAPP
            // For an async function that awaits a switch to the context (see PromiseSwitchToContextAwaiter).
            [MethodImpl(InlineOption)]
            internal void SwitchToContext(SynchronizationContext synchronizationContext)
                => ContinueOnContext(synchronizationContext);
#endif

            protected virtual void ContinueOnContext(SynchronizationContext synchronizationContext) => throw new System.InvalidOperationException();

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial class AsyncPromiseRef<TResult> : SingleAwaitPromise<TResult>, IAwaitWaiter
            {
                [MethodImpl(InlineOption)]
                private static AsyncPromiseRef<TResult> GetFromPoolOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AsyncPromiseRef<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AsyncPromiseRef<TResult>()
                        : obj.UnsafeAs<AsyncPromiseRef<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static AsyncPromiseRef<TResult> GetOrCreate()
                {
#if OPTIMIZED_ASYNC_MODE
                    // Only used for async functions that throw before they await anything, so nothing waits for them while they complete,
                    // and they never continue in their scope, so they don't need the traced type.
                    var promise = GetFromPoolOrCreate();
                    promise.Reset();
                    return promise;
#else
                    if (!CausalityTrace.IsEnabled)
                    {
                        var promise = GetFromPoolOrCreate();
                        promise.ResetWithoutStacktrace();
                        return promise;
                    }
                    return TracedAsyncPromiseRef.Create();
#endif
                }

                internal void SetException(Exception exception)
                    => SetException(exception, true);

#if OPTIMIZED_ASYNC_MODE
                // For an async function that threw before it awaited anything. Its trace was just captured where it threw (see GetOrCreate),
                // so the rejection uses that trace instead of capturing the same frames again.
                internal void SetExceptionCreated(Exception exception)
                    => SetException(exception, false);
#endif

                private void SetException(Exception exception, bool captureTrace)
                {
                    if (exception is OperationCanceledException)
                    {
                        // The field is shared with RejectContainer, so clear the continuation context before completing.
                        ContinuationContext = null;
                        HandleNextInternal(Promise.State.Canceled);
                    }
                    else
                    {
                        RejectContainer = CreateRejectContainer(exception, captureTrace, this);
                        HandleNextInternal(Promise.State.Rejected);
                    }
                }

                [MethodImpl(InlineOption)]
                internal void SetAsyncResultVoid()
                {
                    ThrowIfInPool(this);
                    // The field is shared with RejectContainer, so clear the continuation context before completing.
                    ContinuationContext = null;
                    HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                internal void SetAsyncResult(in TResult result)
                {
                    ThrowIfInPool(this);
                    _result = result;
                    // The field is shared with RejectContainer, so clear the continuation context before completing.
                    ContinuationContext = null;
                    HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption | AggressiveOptimizationOption)]
                internal static void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref, ref short id)
                    where TAwaiter : INotifyCompletion
                    where TStateMachine : IAsyncStateMachine
                {
                    SetStateMachine(ref stateMachine, ref _ref, ref id);
#if NETCOREAPP
                    // These checks and cast are eliminated by the JIT.
#pragma warning disable IDE0038 // Use pattern matching
                    if (null != default(TAwaiter) && awaiter is IPromiseAwareAwaiter)
                    {
                        ((IPromiseAwareAwaiter) awaiter).AwaitOnCompletedInternal(_ref);
                    }
                    else
                    {
                        awaiter.OnCompleted(_ref.MoveNext);
                    }
#pragma warning restore IDE0038 // Use pattern matching
#else
                    // Unity does not optimize the pattern, so we have to call through AwaitOverrider to avoid boxing allocations.
                    AwaitOverrider<TAwaiter>.AwaitOnCompleted(ref awaiter, _ref, _ref.MoveNext);
#endif
                }

                [MethodImpl(InlineOption | AggressiveOptimizationOption)]
                internal static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref, ref short id)
                    where TAwaiter : ICriticalNotifyCompletion
                    where TStateMachine : IAsyncStateMachine
                {
                    SetStateMachine(ref stateMachine, ref _ref, ref id);
#if NETCOREAPP
                    // These checks and cast are eliminated by the JIT.
#pragma warning disable IDE0038 // Use pattern matching
                    if (null != default(TAwaiter) && awaiter is IPromiseAwareAwaiter)
                    {
                        ((IPromiseAwareAwaiter) awaiter).AwaitOnCompletedInternal(_ref);
                    }
                    else
                    {
                        awaiter.UnsafeOnCompleted(_ref.MoveNext);
                    }
#pragma warning restore IDE0038 // Use pattern matching
#else
                    // Unity does not optimize the pattern, so we have to call through CriticalAwaitOverrider to avoid boxing allocations.
                    CriticalAwaitOverrider<TAwaiter>.AwaitOnCompleted(ref awaiter, _ref, _ref.MoveNext);
#endif
                }
            } // class AsyncPromiseRef<TResult>

#if !OPTIMIZED_ASYNC_MODE
            partial class AsyncPromiseRef<TResult>
            {
                // A traced async promise, which continues in its scope (see TracedContinuer), and exits it while its waiter runs (see ScopeExitingWaiter).
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private sealed class TracedAsyncPromiseRef : AsyncPromiseRef<TResult>
                {
                    private CausalityTrace _awaitTrace;
                    private CausalityTrace _enteredAwaitTrace;

                    internal override CausalityTrace EnteredAwaitTrace => _enteredAwaitTrace;

                    internal override void CaptureAwaitContextAndOrTrace(bool isFirstAwait)
                    {
                        // This is also called when only the ExecutionContext flows, so await traces are checked here.
                        // The first await captures where it awaits too, because its async promise's own trace was captured where it was called, before its body ran.
                        if (Promise.Config.AwaitTracesEnabled)
                        {
                            _awaitTrace = CausalityTrace.Capture();
                        }
                        base.CaptureAwaitContextAndOrTrace(isFirstAwait);
                    }

                    // Called when it continues (see TracedContinuer.MoveNextInScope).
                    internal CausalityTrace TakeAwaitTraceToEnter()
                    {
                        // Where it last awaited is taken when it continues, so that an await that doesn't capture where it awaits
                        // (e.g. while await traces or causality traces are disabled) doesn't continue in the scope of an older await.
                        // It's kept while its scope is entered, so that it's exited with the async promise's scope (see EnteredAwaitTrace).
                        var awaitTrace = _awaitTrace;
                        _awaitTrace = null;
                        _enteredAwaitTrace = awaitTrace;
                        return awaitTrace;
                    }

                    [MethodImpl(MethodImplOptions.NoInlining)]
                    internal static AsyncPromiseRef<TResult> Create()
                    {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        // Take from pool for object tracking purposes.
                        var obj = ObjectPool.TryTakeOrInvalid<TracedAsyncPromiseRef>();
                        var promise = obj == InvalidAwaitSentinel.s_instance
                            ? new TracedAsyncPromiseRef()
                            : obj.UnsafeAs<TracedAsyncPromiseRef>();
#else
                        var promise = new TracedAsyncPromiseRef();
#endif
                        promise.Reset();
                        return promise;
                    }

                    // The waiter is wrapped before it's added, so that it's never handled without the wrapper if this completes on another thread.
                    internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                        => base.AddWaiter(promiseId, new ScopeExitingWaiter(waiter), out previousWaiter);

                    internal override void MaybeDispose()
                    {
                        Dispose();
                        _continuer?.Dispose();
                        _continuer = null;
                        _awaitTrace = null;
                        _enteredAwaitTrace = null;
                        // Traced promises are never returned to the object pool, even if they complete after causality traces are disabled
                        // (object pooling is disabled while they're enabled), so we discard it.
                        Discard(this);
                    }
                }

                private Action MoveNext
                {
                    [MethodImpl(InlineOption)]
                    get => _continuer.MoveNext;
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private abstract partial class PromiseMethodContinuer : HandleablePromiseBase, IDisposable
                {
                    internal Action MoveNext
                    {
                        [MethodImpl(InlineOption)]
                        get => _moveNext;
                    }

                    private PromiseMethodContinuer() { }

                    internal abstract void Continue();

                    public abstract void Dispose();

                    [MethodImpl(InlineOption)]
                    public static PromiseMethodContinuer GetOrCreate<TStateMachine>(ref TStateMachine stateMachine, AsyncPromiseRef<TResult> owner) where TStateMachine : IAsyncStateMachine
                    {
                        if (!CausalityTrace.IsEnabled)
                        {
                            return Continuer<TStateMachine>.GetOrCreate(ref stateMachine, owner);
                        }
                        return TracedContinuer.GetOrCreate(ref stateMachine, owner);
                    }

#if !PROTO_PROMISE_DEVELOPER_MODE
                    [DebuggerNonUserCode, StackTraceHidden]
#endif
                    private sealed partial class Continuer<TStateMachine> : PromiseMethodContinuer where TStateMachine : IAsyncStateMachine
                    {
                        private Continuer()
                        {
                            _moveNext = Continue;
                        }

                        [MethodImpl(InlineOption)]
                        private static Continuer<TStateMachine> GetOrCreate()
                        {
                            var obj = ObjectPool.TryTakeOrInvalid<Continuer<TStateMachine>>();
                            return obj == InvalidAwaitSentinel.s_instance
                                ? new Continuer<TStateMachine>()
                                : obj.UnsafeAs<Continuer<TStateMachine>>();
                        }

                        [MethodImpl(InlineOption)]
                        public static Continuer<TStateMachine> GetOrCreate(ref TStateMachine stateMachine, AsyncPromiseRef<TResult> owner)
                        {
                            var continuer = GetOrCreate();
                            continuer._next = null;
                            continuer._owner = owner;
                            continuer._stateMachine = stateMachine;
                            return continuer;
                        }

                        public override void Dispose()
                        {
                            _owner = null;
                            _stateMachine = default;
                            ObjectPool.MaybeRepool(this);
                        }

                        [MethodImpl(InlineOption)]
                        internal override void Continue()
                        {
                            var continuationContext = _owner.ContinuationContext;
                            if (continuationContext == null)
                            {
                                // Common case: no continuation context.
                                _stateMachine.MoveNext();
                            }
                            else
                            {
                                ContinueOnContext(continuationContext);
                            }
                        }

                        [MethodImpl(InlineOption)]
                        new private void MoveNext()
                            => _stateMachine.MoveNext();

                        // NoInlining to keep the continuation fast path small.
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        private void ContinueOnContext(object continuationContext)
                        {
                            if (continuationContext is SynchronizationContext synchronizationContext)
                            {
                                _owner.ContinuationContext = null;
                                ScheduleContextCallback(synchronizationContext, this,
                                    obj => obj.UnsafeAs<Continuer<TStateMachine>>().MoveNext(),
                                    obj => obj.UnsafeAs<Continuer<TStateMachine>>().MoveNext());
                            }
                            // TODO: Make ConfiguredAwaitDualContext inherit SynchronizationContext to eliminate an extra type check.
                            else if (continuationContext is ConfiguredAwaitDualContext dualContext)
                            {
                                _owner.ContinuationContext = dualContext._executionContext;
                                synchronizationContext = dualContext._synchronizationContext;
                                dualContext.Dispose();
                                ScheduleContextCallback(synchronizationContext, this,
                                    obj => obj.UnsafeAs<Continuer<TStateMachine>>().Continue(),
                                    obj => obj.UnsafeAs<Continuer<TStateMachine>>().Continue());
                            }
                            else
                            {
                                _owner.ContinuationContext = null;
#if NET6_0_OR_GREATER
                                var previousExecutionContext = SwapExecutionContext(continuationContext.UnsafeAs<ExecutionContext>());
                                if (previousExecutionContext != null)
                                {
                                    MoveNext();
                                    ExecutionContext.Restore(previousExecutionContext);
                                    return;
                                }
#endif
                                ExecutionContext.Run(
                                    continuationContext.UnsafeAs<ExecutionContext>(),
                                    obj => obj.UnsafeAs<Continuer<TStateMachine>>().MoveNext(),
                                    this
                                );
                            }
                        }
                    }

                    // The continuer of a traced async promise, which continues it in its scope (see MoveNextInScope).
                    // It's separate from Continuer, so that untraced async promises don't check whether they're traced when they continue.
                    // It holds the state machine boxed, so that its code is only generated once for each TResult instead of for each async method,
                    // which matters for AOT runtimes like IL2CPP.
#if !PROTO_PROMISE_DEVELOPER_MODE
                    [DebuggerNonUserCode, StackTraceHidden]
#endif
                    private sealed class TracedContinuer : PromiseMethodContinuer
                    {
                        private IAsyncStateMachine _stateMachine;

                        private TracedContinuer()
                        {
                            _moveNext = Continue;
                        }

                        // Separate from PromiseMethodContinuer.GetOrCreate, so that the traced path doesn't make the untraced path slower.
                        // The state machine is passed by ref, so that it's only boxed if the async promise is traced. Only this small method is generated for each async method.
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        new internal static PromiseMethodContinuer GetOrCreate<TStateMachine>(ref TStateMachine stateMachine, AsyncPromiseRef<TResult> owner)
                            where TStateMachine : IAsyncStateMachine
                        {
                            // The async promise isn't traced if causality traces were enabled after it was created.
                            if (!(owner is TracedAsyncPromiseRef))
                            {
                                return Continuer<TStateMachine>.GetOrCreate(ref stateMachine, owner);
                            }
                            var continuer = Create();
                            continuer._owner = owner;
                            continuer._stateMachine = stateMachine;
                            return continuer;
                        }

                        // NoInlining so that it's only generated once for each TResult, not in each GetOrCreate.
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        private static TracedContinuer Create()
                        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                            // Take from pool for object tracking purposes.
                            var obj = ObjectPool.TryTakeOrInvalid<TracedContinuer>();
                            return obj == InvalidAwaitSentinel.s_instance
                                ? new TracedContinuer()
                                : obj.UnsafeAs<TracedContinuer>();
#else
                            return new TracedContinuer();
#endif
                        }

                        public override void Dispose()
                        {
                            _owner = null;
                            _stateMachine = null;
                            // Traced continuers are never returned to the object pool, like their async promises (see TracedAsyncPromiseRef.MaybeDispose).
                            Discard(this);
                        }

                        internal override void Continue()
                        {
                            var continuationContext = _owner.ContinuationContext;
                            if (continuationContext == null)
                            {
                                MoveNextInScope();
                            }
                            else
                            {
                                ContinueOnContext(continuationContext);
                            }
                        }

                        // MoveNext, in the scope of the async promise's trace, inside the scope of where it last awaited (see EnteredAwaitTrace).
                        private void MoveNextInScope()
                        {
                            var owner = _owner.UnsafeAs<TracedAsyncPromiseRef>();
                            var awaitScope = CausalityTrace.EnterScopeInternal(owner.TakeAwaitTraceToEnter());
                            try
                            {
                                // The async promise's trace is null if causality traces were disabled after it was created, so it doesn't enter that scope.
                                var scope = CausalityTrace.EnterScopeInternal(owner.Trace);
                                try
                                {
                                    _stateMachine.MoveNext();
                                }
                                finally
                                {
                                    scope.Dispose();
                                }
                            }
                            finally
                            {
                                awaitScope.Dispose();
                            }
                        }

                        private void ContinueOnContext(object continuationContext)
                        {
                            if (continuationContext is SynchronizationContext synchronizationContext)
                            {
                                _owner.ContinuationContext = null;
                                ScheduleContextCallback(synchronizationContext, this,
                                    obj => obj.UnsafeAs<TracedContinuer>().MoveNextInScope(),
                                    obj => obj.UnsafeAs<TracedContinuer>().MoveNextInScope());
                            }
                            else if (continuationContext is ConfiguredAwaitDualContext dualContext)
                            {
                                _owner.ContinuationContext = dualContext._executionContext;
                                synchronizationContext = dualContext._synchronizationContext;
                                dualContext.Dispose();
                                ScheduleContextCallback(synchronizationContext, this,
                                    obj => obj.UnsafeAs<TracedContinuer>().Continue(),
                                    obj => obj.UnsafeAs<TracedContinuer>().Continue());
                            }
                            else
                            {
                                _owner.ContinuationContext = null;
#if NET6_0_OR_GREATER
                                var previousExecutionContext = SwapExecutionContext(continuationContext.UnsafeAs<ExecutionContext>());
                                if (previousExecutionContext != null)
                                {
                                    MoveNextInScope();
                                    ExecutionContext.Restore(previousExecutionContext);
                                    return;
                                }
#endif
                                ExecutionContext.Run(
                                    continuationContext.UnsafeAs<ExecutionContext>(),
                                    obj => obj.UnsafeAs<TracedContinuer>().MoveNextInScope(),
                                    this
                                );
                            }
                        }
                    }
                }

                private PromiseMethodContinuer _continuer;

                [MethodImpl(InlineOption)]
#pragma warning disable IDE0060 // Remove unused parameter
                private static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref, ref short id) where TStateMachine : IAsyncStateMachine
#pragma warning restore IDE0060 // Remove unused parameter
                {
                    bool isFirstAwait = _ref._continuer == null;
                    if (isFirstAwait)
                    {
                        _ref._continuer = PromiseMethodContinuer.GetOrCreate(ref stateMachine, _ref);
                    }
                    if (Promise.Config.s_isAwaitCaptureEnabled)
                    {
                        _ref.CaptureForAwait(isFirstAwait);
                    }
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _continuer?.Dispose();
                    _continuer = null;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
                    this.SetPrevious(null);
                    _continuer.Continue();
                }

                // It's scheduled where the async function awaits, so it doesn't capture where it was scheduled, which is where it awaited (see CaptureAwaitContextAndOrTrace).
                protected override void ContinueOnContext(SynchronizationContext synchronizationContext)
                    => ScheduleContextCallbackDirect(synchronizationContext, _continuer.MoveNext,
                        obj => obj.UnsafeAs<Action>().Invoke(),
                        obj => obj.UnsafeAs<Action>().Invoke());
            } // class AsyncPromiseRef<TResult>

#else // !OPTIMIZED_ASYNC_MODE

            partial class AsyncPromiseRef<TResult>
            {
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private sealed partial class AsyncPromiseRefMachine<TStateMachine> : AsyncPromiseRef<TResult> where TStateMachine : IAsyncStateMachine
                {
                    private AsyncPromiseRefMachine()
                    {
                        _moveNext = Continue;
                    }

                    [MethodImpl(InlineOption)]
                    new private static AsyncPromiseRefMachine<TStateMachine> GetOrCreate()
                    {
                        var obj = ObjectPool.TryTakeOrInvalid<AsyncPromiseRefMachine<TStateMachine>>();
                        return obj == InvalidAwaitSentinel.s_instance
                            ? new AsyncPromiseRefMachine<TStateMachine>()
                            : obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>();
                    }

                    internal static void SetStateMachine(ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref, ref short id)
                    {
                        if (!CausalityTrace.IsEnabled)
                        {
                            var promise = GetOrCreate();
                            promise.ResetWithoutStacktrace();
                            id = promise.Id;
                            // ORDER VERY IMPORTANT, ref must be set before copying stateMachine.
                            _ref = promise;
                            promise._stateMachine = stateMachine;
                            return;
                        }
                        TracedAsyncPromiseRefMachine.SetStateMachine(ref stateMachine, ref _ref, ref id);
                    }

                    internal override void MaybeDispose()
                    {
                        Dispose();
                        _stateMachine = default;
                        ObjectPool.MaybeRepool(this);
                    }

                    [MethodImpl(InlineOption)]
                    private void Continue()
                    {
                        var continuationContext = ContinuationContext;
                        if (continuationContext == null)
                        {
                            // Common case: no continuation context.
                            _stateMachine.MoveNext();
                        }
                        else
                        {
                            ContinueOnContext(continuationContext);
                        }
                    }

                    [MethodImpl(InlineOption)]
                    new private void MoveNext()
                        => _stateMachine.MoveNext();

                    internal override void Handle(PromiseRefBase handler, Promise.State state)
                    {
                        ThrowIfInPool(this);
                        handler.SetCompletionState(state);
                        Continue();
                    }

                    // NoInlining to keep the continuation fast path small.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    private void ContinueOnContext(object continuationContext)
                    {
                        if (continuationContext is SynchronizationContext synchronizationContext)
                        {
                            ContinuationContext = null;
                            ScheduleContextCallback(synchronizationContext, this,
                                obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().MoveNext(),
                                obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().MoveNext());
                        }
                        // TODO: Make ConfiguredAwaitDualContext inherit SynchronizationContext to eliminate an extra type check.
                        else if (continuationContext is ConfiguredAwaitDualContext dualContext)
                        {
                            ContinuationContext = dualContext._executionContext;
                            synchronizationContext = dualContext._synchronizationContext;
                            dualContext.Dispose();
                            ScheduleContextCallback(synchronizationContext, this,
                                obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().ContinueWithExecutionContext(),
                                obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().ContinueWithExecutionContext());
                        }
                        else
                        {
                            ContinueWithExecutionContext();
                        }
                    }

                    private void ContinueWithExecutionContext()
                    {
                        var continuationContext = ContinuationContext;
                        ContinuationContext = null;
#if NET6_0_OR_GREATER
                        var previousExecutionContext = SwapExecutionContext(continuationContext.UnsafeAs<ExecutionContext>());
                        if (previousExecutionContext != null)
                        {
                            MoveNext();
                            ExecutionContext.Restore(previousExecutionContext);
                            return;
                        }
#endif
                        ExecutionContext.Run(
                            continuationContext.UnsafeAs<ExecutionContext>(),
                            obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().MoveNext(),
                            this
                        );
                    }

                    // It's scheduled where the async function awaits, so it doesn't capture where it was scheduled, which is where it awaited (see CaptureAwaitContextAndOrTrace).
                    protected override void ContinueOnContext(SynchronizationContext synchronizationContext)
                        // Continue instead of MoveNext so the ExecutionContext is restored if it was captured.
                        => ScheduleContextCallbackDirect(synchronizationContext, this,
                            obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().Continue(),
                            obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().Continue());
                }

                // A traced async promise, which continues in its scope (see MoveNextInScope), and exits it while its waiter runs (see ScopeExitingWaiter).
                // It's separate from AsyncPromiseRefMachine, so that untraced async promises don't check whether they're traced when they continue.
                // It holds the state machine boxed, so that its code is only generated once for each TResult instead of for each async method,
                // which matters for AOT runtimes like IL2CPP and NativeAOT.
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private sealed class TracedAsyncPromiseRefMachine : AsyncPromiseRef<TResult>
                {
                    private IAsyncStateMachine _stateMachine;
                    private CausalityTrace _awaitTrace;
                    private CausalityTrace _enteredAwaitTrace;

                    private TracedAsyncPromiseRefMachine()
                    {
                        _moveNext = Continue;
                    }

                    internal override CausalityTrace EnteredAwaitTrace => _enteredAwaitTrace;

                    internal override void CaptureAwaitContextAndOrTrace(bool isFirstAwait)
                    {
                        // This is also called when only the ExecutionContext flows, so await traces are checked here.
                        if (Promise.Config.AwaitTracesEnabled)
                        {
                            // The async promise's own trace was just captured where it first awaits, without the async function's frame (see Create),
                            // so the first await shares that capture with all of its frames, instead of capturing the same stack again.
                            // Trace is null if causality traces were disabled since then.
                            _awaitTrace = isFirstAwait ? Trace?.GetWithAllFrames() : CausalityTrace.Capture();
                        }
                        base.CaptureAwaitContextAndOrTrace(isFirstAwait);
                    }

                    // Separate from AsyncPromiseRefMachine.SetStateMachine, so that the traced path doesn't make the untraced path slower.
                    // The state machine is passed by ref, so that it's not boxed until after _ref is set. Only this small method is generated for each async method.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    new internal static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref, ref short id)
                        where TStateMachine : IAsyncStateMachine
                    {
                        var promise = Create(typeof(TStateMachine));
                        id = promise.Id;
                        // ORDER VERY IMPORTANT, ref must be set before boxing stateMachine.
                        _ref = promise;
                        promise._stateMachine = stateMachine;
                    }

                    // NoInlining so that it's only generated once for each TResult, not in each SetStateMachine.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    private static TracedAsyncPromiseRefMachine Create(Type stateMachineType)
                    {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        // Take from pool for object tracking purposes.
                        var obj = ObjectPool.TryTakeOrInvalid<TracedAsyncPromiseRefMachine>();
                        var promise = obj == InvalidAwaitSentinel.s_instance
                            ? new TracedAsyncPromiseRefMachine()
                            : obj.UnsafeAs<TracedAsyncPromiseRefMachine>();
#else
                        var promise = new TracedAsyncPromiseRefMachine();
#endif
                        promise.ResetWithoutStacktrace();
                        // This is created where the async function first awaits, so the async function's frame is dropped from its trace,
                        // so that it starts where the async function was called, the same as in the non-optimized mode.
                        var trace = CausalityTrace.CaptureWithoutInnermostVisibleFrame(stateMachineType);
                        if (trace != null)
                        {
                            SetTrace(promise, trace);
                        }
                        return promise;
                    }

                    // The waiter is wrapped before it's added, so that it's never handled without the wrapper if this completes on another thread.
                    internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                        => base.AddWaiter(promiseId, new ScopeExitingWaiter(waiter), out previousWaiter);

                    internal override void MaybeDispose()
                    {
                        Dispose();
                        _stateMachine = null;
                        _awaitTrace = null;
                        _enteredAwaitTrace = null;
                        // Traced promises are never returned to the object pool, even if they complete after causality traces are disabled
                        // (object pooling is disabled while they're enabled), so we discard it.
                        Discard(this);
                    }

                    internal override void Handle(PromiseRefBase handler, Promise.State state)
                    {
                        ThrowIfInPool(this);
                        handler.SetCompletionState(state);
                        Continue();
                    }

                    private void Continue()
                    {
                        var continuationContext = ContinuationContext;
                        if (continuationContext == null)
                        {
                            MoveNextInScope();
                        }
                        else
                        {
                            ContinueOnContext(continuationContext);
                        }
                    }

                    // MoveNext, in the scope of this promise's trace, inside the scope of where it last awaited (see EnteredAwaitTrace).
                    private void MoveNextInScope()
                    {
                        // Where it last awaited is taken when it continues, so that an await that doesn't capture where it awaits
                        // (e.g. while await traces or causality traces are disabled) doesn't continue in the scope of an older await.
                        // It's kept while its scope is entered, so that it's exited with the async promise's scope (see EnteredAwaitTrace).
                        var awaitTrace = _awaitTrace;
                        _awaitTrace = null;
                        _enteredAwaitTrace = awaitTrace;
                        var awaitScope = CausalityTrace.EnterScopeInternal(awaitTrace);
                        try
                        {
                            // Trace is null if causality traces were disabled after this was created, so it doesn't enter that scope.
                            var scope = CausalityTrace.EnterScopeInternal(Trace);
                            try
                            {
                                _stateMachine.MoveNext();
                            }
                            finally
                            {
                                scope.Dispose();
                            }
                        }
                        finally
                        {
                            awaitScope.Dispose();
                        }
                    }

                    private void ContinueOnContext(object continuationContext)
                    {
                        if (continuationContext is SynchronizationContext synchronizationContext)
                        {
                            ContinuationContext = null;
                            ScheduleContextCallback(synchronizationContext, this,
                                obj => obj.UnsafeAs<TracedAsyncPromiseRefMachine>().MoveNextInScope(),
                                obj => obj.UnsafeAs<TracedAsyncPromiseRefMachine>().MoveNextInScope());
                        }
                        else if (continuationContext is ConfiguredAwaitDualContext dualContext)
                        {
                            ContinuationContext = dualContext._executionContext;
                            synchronizationContext = dualContext._synchronizationContext;
                            dualContext.Dispose();
                            ScheduleContextCallback(synchronizationContext, this,
                                obj => obj.UnsafeAs<TracedAsyncPromiseRefMachine>().ContinueWithExecutionContext(),
                                obj => obj.UnsafeAs<TracedAsyncPromiseRefMachine>().ContinueWithExecutionContext());
                        }
                        else
                        {
                            ContinueWithExecutionContext();
                        }
                    }

                    private void ContinueWithExecutionContext()
                    {
                        var continuationContext = ContinuationContext;
                        ContinuationContext = null;
#if NET6_0_OR_GREATER
                        var previousExecutionContext = SwapExecutionContext(continuationContext.UnsafeAs<ExecutionContext>());
                        if (previousExecutionContext != null)
                        {
                            MoveNextInScope();
                            ExecutionContext.Restore(previousExecutionContext);
                            return;
                        }
#endif
                        ExecutionContext.Run(
                            continuationContext.UnsafeAs<ExecutionContext>(),
                            obj => obj.UnsafeAs<TracedAsyncPromiseRefMachine>().MoveNextInScope(),
                            this
                        );
                    }

                    // It's scheduled where the async function awaits, so it doesn't capture where it was scheduled, which is where it awaited (see CaptureAwaitContextAndOrTrace).
                    protected override void ContinueOnContext(SynchronizationContext synchronizationContext)
                        // Continue instead of MoveNextInScope so the ExecutionContext is restored if it was captured.
                        => ScheduleContextCallbackDirect(synchronizationContext, this,
                            obj => obj.UnsafeAs<TracedAsyncPromiseRefMachine>().Continue(),
                            obj => obj.UnsafeAs<TracedAsyncPromiseRefMachine>().Continue());
                }

                private Action MoveNext
                {
                    [MethodImpl(InlineOption)]
                    get => _moveNext;
                }

                protected AsyncPromiseRef() { }

                [MethodImpl(InlineOption)]
                private static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref, ref short id) where TStateMachine : IAsyncStateMachine
                {
                    bool isFirstAwait = _ref == null;
                    if (isFirstAwait)
                    {
                        AsyncPromiseRefMachine<TStateMachine>.SetStateMachine(ref stateMachine, ref _ref, ref id);
                    }
                    if (Promise.Config.s_isAwaitCaptureEnabled)
                    {
                        _ref.CaptureForAwait(isFirstAwait);
                    }
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }
            } // class AsyncPromiseRef<TResult>
#endif // OPTIMIZED_ASYNC_MODE
        } // class PromiseRef
    } // class Internal
} // namespace Proto.Promises