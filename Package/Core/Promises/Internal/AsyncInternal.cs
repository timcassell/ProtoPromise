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
            // NoInlining because it's only used on slow paths, and it would otherwise inline a static class initialization check into async methods.
            internal object NoAsyncContinuationContext
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
#if PROMISE_DEBUG
                // In DEBUG mode, the trace is kept while causality traces are disabled (see GetTrace), so the sentinel is restored if this has a trace,
                // so that this enters its trace's scope again if causality traces are enabled again before it completes.
                get => ((ITraceable) this).Trace != null ? TracedSentinel.s_instance : null;
#else
                get => CausalityTrace.IsEnabled ? TracedSentinel.s_instance : null;
#endif
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
                    var promise = GetFromPoolOrCreate();
                    promise.ResetWithSentinel();
                    return promise;
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
            sealed partial class AsyncPromiseRef<TResult>
            {
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

                        [MethodImpl(MethodImplOptions.NoInlining)]
                        private void MoveNextInScope()
                        {
                            // An async promise that wasn't traced doesn't enter a scope, the same as the fast path.
                            var scope = CausalityTrace.EnterScopeInternal(_owner.Trace);
                            try
                            {
                                _stateMachine.MoveNext();
                            }
                            finally
                            {
                                scope.Dispose();
                            }
                        }

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
                        _ref.ContinuationContext = ExecutionContext.Capture() ?? _ref.NoAsyncContinuationContext;
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
                        var promise = GetOrCreate();
                        promise.ResetWithSentinel();
                        id = promise.Id;
                        // ORDER VERY IMPORTANT, ref must be set before copying stateMachine.
                        _ref = promise;
                        promise._stateMachine = stateMachine;
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

                    [MethodImpl(MethodImplOptions.NoInlining)]
                    private void MoveNextInScope()
                    {
                        // An async promise that wasn't traced doesn't enter a scope, the same as the fast path.
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
                        _ref.ContinuationContext = ExecutionContext.Capture() ?? _ref.NoAsyncContinuationContext;
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