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
            // The value to restore to an async promise's ContinuationContext field when its continuation context is cleared.
            internal object NoAsyncContinuationContext
            {
                [MethodImpl(InlineOption)]
#if PROMISE_DEBUG
                // In DEBUG mode, the trace is kept while causality traces are disabled (see GetTrace), so the sentinel is restored if this has a trace,
                // so that this enters its trace's scope again if causality traces are enabled again before it completes.
                get => ((ITraceable) this).Trace != null ? GetTracedSentinel() : null;
#else
                get => CausalityTrace.IsEnabled ? GetTracedSentinel() : null;
#endif
            }

            // NoInlining for methods that are inlined into async methods, where it's only needed when the ExecutionContext flow is suppressed,
            // so that they don't grow by NoAsyncContinuationContext's check.
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal object GetNoAsyncContinuationContext()
                => NoAsyncContinuationContext;

            // NoInlining because it would otherwise inline a static class initialization check into async methods.
            [MethodImpl(MethodImplOptions.NoInlining)]
            private static object GetTracedSentinel()
                => TracedSentinel.s_instance;

            // Traced async promises add this in place of their waiter (see AsyncPromiseRef.TracedAsyncPromiseRef and AsyncPromiseRef.TracedAsyncPromiseRefMachine),
            // so that when an async function completes inside its own scope (its body completes it), the waiter, and everything that it continues synchronously,
            // runs outside of that scope, as if it was continued from where the async function was called.
            // The async function's trace is where it was called, which is usually already in the waiter's frames (e.g. the code that awaited it called it),
            // and nesting would make the scopes of a chain of async functions that complete synchronously grow with its depth.
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

                // The field is null, an ExecutionContext, or the TracedSentinel.
                ContinuationContext = ContinuationContext is ExecutionContext executionContext
                    ? ConfiguredAwaitDualContext.GetOrCreate(synchronizationContext, executionContext)
                    : (object) synchronizationContext;

                this.SetPrevious(awaiter);
                awaiter.HookupExistingWaiter(promiseId, this);
            }

            protected virtual void ContinueOnContext(SynchronizationContext synchronizationContext) => throw new System.InvalidOperationException();

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial class AsyncPromiseRef<TResult> : SingleAwaitPromise<TResult>
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
                    // Only used for async functions that complete before they await anything, so nothing waits for them while they complete.
                    var promise = GetFromPoolOrCreate();
                    promise.ResetWithSentinel();
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
                {
                    if (exception is OperationCanceledException)
                    {
                        // The field is shared with RejectContainer, so clear the continuation context or sentinel before completing.
                        ContinuationContext = null;
                        HandleNextInternal(Promise.State.Canceled);
                    }
                    else
                    {
                        RejectContainer = CreateRejectContainer(exception, true, this);
                        HandleNextInternal(Promise.State.Rejected);
                    }
                }

                [MethodImpl(InlineOption)]
                internal void SetAsyncResultVoid()
                {
                    ThrowIfInPool(this);
                    // The field is shared with RejectContainer, so clear the continuation context or sentinel before completing.
                    ContinuationContext = null;
                    HandleNextInternal(Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                internal void SetAsyncResult(in TResult result)
                {
                    ThrowIfInPool(this);
                    _result = result;
                    // The field is shared with RejectContainer, so clear the continuation context or sentinel before completing.
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
                // A traced async promise, which exits its scope while its waiter runs (see ScopeExitingWaiter).
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private sealed class TracedAsyncPromiseRef : AsyncPromiseRef<TResult>
                {
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
                        promise.ResetWithSentinel();
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

                    // MoveNext, in the scope of the async promise's trace.
                    // It's not generic over the state machine, so that AOT compilers (e.g. IL2CPP) don't generate it for every async function.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    protected void MoveNextInScope()
                    {
                        // An async promise that wasn't traced doesn't enter a scope, the same as the fast path.
                        var scope = CausalityTrace.EnterScopeInternal(_owner.Trace);
                        try
                        {
                            MoveNextStateMachine();
                        }
                        finally
                        {
                            scope.Dispose();
                        }
                    }

                    protected abstract void MoveNextStateMachine();

                    public abstract void Dispose();

                    [MethodImpl(InlineOption)]
                    public static PromiseMethodContinuer GetOrCreate<TStateMachine>(ref TStateMachine stateMachine, AsyncPromiseRef<TResult> owner) where TStateMachine : IAsyncStateMachine
                    {
                        var continuer = Continuer<TStateMachine>.GetOrCreate(ref stateMachine);
                        continuer._owner = owner;
                        return continuer;
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
                        public static Continuer<TStateMachine> GetOrCreate(ref TStateMachine stateMachine)
                        {
                            var continuer = GetOrCreate();
                            continuer._next = null;
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
                                // Common case: no continuation context and no causality trace.
                                _stateMachine.MoveNext();
                            }
                            else
                            {
                                ContinueOnContext(continuationContext);
                            }
                        }

                        [MethodImpl(InlineOption)]
                        new private void MoveNext()
                        {
                            if (!CausalityTrace.IsEnabled)
                            {
                                _stateMachine.MoveNext();
                                return;
                            }
                            MoveNextInScope();
                        }

                        protected override void MoveNextStateMachine()
                            => _stateMachine.MoveNext();

                        // NoInlining to keep the continuation fast path small.
                        [MethodImpl(MethodImplOptions.NoInlining)]
                        private void ContinueOnContext(object continuationContext)
                        {
                            if (continuationContext is SynchronizationContext synchronizationContext)
                            {
                                _owner.ContinuationContext = _owner.NoAsyncContinuationContext;
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
                            else if (continuationContext == TracedSentinel.s_instance)
                            {
                                MoveNextInScope();
                            }
                            else
                            {
                                _owner.ContinuationContext = _owner.NoAsyncContinuationContext;
                                ExecutionContext.Run(
                                    continuationContext.UnsafeAs<ExecutionContext>(),
                                    obj => obj.UnsafeAs<Continuer<TStateMachine>>().MoveNext(),
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
                    if (_ref._continuer == null)
                    {
                        _ref._continuer = PromiseMethodContinuer.GetOrCreate(ref stateMachine, _ref);
                    }
                    if (Promise.Config.AsyncFlowExecutionContextEnabled)
                    {
                        // Capture returns null if the flow is suppressed.
                        _ref.ContinuationContext = ExecutionContext.Capture() ?? _ref.GetNoAsyncContinuationContext();
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

                protected override void ContinueOnContext(SynchronizationContext synchronizationContext)
                    => ScheduleContextCallback(synchronizationContext, _continuer.MoveNext,
                        obj => obj.UnsafeAs<Action>().Invoke(),
                        obj => obj.UnsafeAs<Action>().Invoke());
            } // class AsyncPromiseRef<TResult>

#else // !OPTIMIZED_ASYNC_MODE

            partial class AsyncPromiseRef<TResult>
            {
                // MoveNext, in the scope of this promise's trace.
                // It's not generic over the state machine, so that AOT compilers (e.g. IL2CPP) don't generate it for every async function.
                [MethodImpl(MethodImplOptions.NoInlining)]
                protected void MoveNextInScope()
                {
                    // An async promise that wasn't traced doesn't enter a scope, the same as the fast path.
                    var scope = CausalityTrace.EnterScopeInternal(Trace);
                    try
                    {
                        MoveNextStateMachine();
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }

                // Only async promises with a state machine are continued (see AsyncPromiseRefMachine).
                protected virtual void MoveNextStateMachine() => throw new System.InvalidOperationException();

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private partial class AsyncPromiseRefMachine<TStateMachine> : AsyncPromiseRef<TResult> where TStateMachine : IAsyncStateMachine
                {
                    protected AsyncPromiseRefMachine()
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
                            // Common case: no continuation context and no causality trace.
                            _stateMachine.MoveNext();
                        }
                        else
                        {
                            ContinueOnContext(continuationContext);
                        }
                    }

                    [MethodImpl(InlineOption)]
                    new private void MoveNext()
                    {
                        if (!CausalityTrace.IsEnabled)
                        {
                            _stateMachine.MoveNext();
                            return;
                        }
                        MoveNextInScope();
                    }

                    protected override void MoveNextStateMachine()
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
                            ContinuationContext = NoAsyncContinuationContext;
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
                        else if (continuationContext == TracedSentinel.s_instance)
                        {
                            MoveNextInScope();
                        }
                        else
                        {
                            ContinuationContext = NoAsyncContinuationContext;
                            ExecutionContext.Run(
                                continuationContext.UnsafeAs<ExecutionContext>(),
                                obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().MoveNext(),
                                this
                            );
                        }
                    }

                    private void ContinueWithExecutionContext()
                    {
                        var continuationContext = ContinuationContext;
                        ContinuationContext = NoAsyncContinuationContext;
                        ExecutionContext.Run(
                            continuationContext.UnsafeAs<ExecutionContext>(),
                            obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().MoveNext(),
                            this
                        );
                    }

                    protected override void ContinueOnContext(SynchronizationContext synchronizationContext)
                        // Continue instead of MoveNext so the ExecutionContext is restored if it was captured.
                        => ScheduleContextCallback(synchronizationContext, this,
                            obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().Continue(),
                            obj => obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>().Continue());
                }

                // A traced async promise, which exits its scope while its waiter runs (see ScopeExitingWaiter).
                // It holds the state machine boxed, so that its code is only generated once for each TResult instead of for each async method,
                // which matters for AOT runtimes like IL2CPP and NativeAOT.
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private sealed class TracedAsyncPromiseRefMachine : AsyncPromiseRefMachine<IAsyncStateMachine>
                {
                    // Separate from AsyncPromiseRefMachine.SetStateMachine, so that the traced path doesn't make the untraced path slower.
                    // The state machine is passed by ref, so that it's not boxed until after _ref is set. Only this small method is generated for each async method.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    new internal static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref, ref short id)
                        where TStateMachine : IAsyncStateMachine
                    {
                        var promise = Create();
                        id = promise.Id;
                        // ORDER VERY IMPORTANT, ref must be set before boxing stateMachine.
                        _ref = promise;
                        promise._stateMachine = stateMachine;
                    }

                    // NoInlining so that it's only generated once for each TResult, not in each SetStateMachine.
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    private static TracedAsyncPromiseRefMachine Create()
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
                        promise.ResetWithSentinel();
                        return promise;
                    }

                    // The waiter is wrapped before it's added, so that it's never handled without the wrapper if this completes on another thread.
                    internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                        => base.AddWaiter(promiseId, new ScopeExitingWaiter(waiter), out previousWaiter);

                    internal override void MaybeDispose()
                    {
                        Dispose();
                        _stateMachine = null;
                        // Traced promises are never returned to the object pool, even if they complete after causality traces are disabled
                        // (object pooling is disabled while they're enabled), so we discard it.
                        Discard(this);
                    }
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
                    if (_ref == null)
                    {
                        AsyncPromiseRefMachine<TStateMachine>.SetStateMachine(ref stateMachine, ref _ref, ref id);
                    }
                    if (Promise.Config.AsyncFlowExecutionContextEnabled)
                    {
                        // Capture returns null if the flow is suppressed.
                        _ref.ContinuationContext = ExecutionContext.Capture() ?? _ref.GetNoAsyncContinuationContext();
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