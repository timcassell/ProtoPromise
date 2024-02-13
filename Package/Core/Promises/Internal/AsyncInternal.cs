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

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0038 // Use pattern matching
#pragma warning disable IDE0074 // Use compound assignment

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
#if NETCOREAPP
        private const MethodImplOptions AggressiveOptimizationOption = (MethodImplOptions) 512;
#else
        private const MethodImplOptions AggressiveOptimizationOption = 0;
#endif

        partial class PromiseRefBase
        {
            [MethodImpl(InlineOption)]
            internal void HookupAwaiter(PromiseRefBase awaiter, short promiseId)
            {
                ValidateAwait(awaiter, promiseId);
#if PROMISE_DEBUG
                _previous = awaiter;
#endif
                awaiter.HookupExistingWaiter(promiseId, this);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial class AsyncPromiseRef<TResult> : PromiseSingleAwait<TResult>
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
                    promise.Reset();
                    return promise;
                }

                internal void SetException(Exception exception)
                {
                    if (exception is OperationCanceledException)
                    {
                        HandleNextInternal(null, Promise.State.Canceled);
                    }
                    else
                    {
                        HandleNextInternal(CreateRejectContainer(exception, int.MinValue, null, this), Promise.State.Rejected);
                    }
                }

                [MethodImpl(InlineOption)]
                internal void SetAsyncResultVoid()
                {
                    ThrowIfInPool(this);
                    HandleNextInternal(null, Promise.State.Resolved);
                }

                [MethodImpl(InlineOption)]
                internal void SetAsyncResult(in TResult result)
                {
                    ThrowIfInPool(this);
                    _result = result;
                    HandleNextInternal(null, Promise.State.Resolved);
                }

                [MethodImpl(InlineOption | AggressiveOptimizationOption)]
                internal static void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref)
                    where TAwaiter : INotifyCompletion
                    where TStateMachine : IAsyncStateMachine
                {
                    SetStateMachine(ref stateMachine, ref _ref);
#if NETCOREAPP
                    // These checks and cast are eliminated by the JIT.
                    if (null != default(TAwaiter) && awaiter is IPromiseAwaiter)
                    {
                        ((IPromiseAwaiter) awaiter).AwaitOnCompletedInternal(_ref);
                    }
                    else
                    {
                        awaiter.OnCompleted(_ref.MoveNext);
                    }
#else
                    // Unity does not optimize the pattern, so we have to call through AwaitOverrider to avoid boxing allocations.
                    AwaitOverrider<TAwaiter>.AwaitOnCompleted(ref awaiter, _ref, _ref.MoveNext);
#endif
                }

                [MethodImpl(InlineOption | AggressiveOptimizationOption)]
                internal static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref)
                    where TAwaiter : ICriticalNotifyCompletion
                    where TStateMachine : IAsyncStateMachine
                {
                    SetStateMachine(ref stateMachine, ref _ref);
#if NETCOREAPP
                    // These checks and cast are eliminated by the JIT.
                    if (null != default(TAwaiter) && awaiter is IPromiseAwaiter)
                    {
                        ((IPromiseAwaiter) awaiter).AwaitOnCompletedInternal(_ref);
                    }
                    else
                    {
                        awaiter.UnsafeOnCompleted(_ref.MoveNext);
                    }
#else
                    // Unity does not optimize the pattern, so we have to call through CriticalAwaitOverrider to avoid boxing allocations.
                    CriticalAwaitOverrider<TAwaiter>.AwaitOnCompleted(ref awaiter, _ref, _ref.MoveNext);
#endif
                }

                partial void SetAwaitedComplete(PromiseRefBase handler);
            }

#if !OPTIMIZED_ASYNC_MODE
            sealed partial class AsyncPromiseRef<TResult>
            {
                private Action MoveNext
                {
                    [MethodImpl(InlineOption)]
                    get { return _continuer.MoveNext; }
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private abstract partial class PromiseMethodContinuer : HandleablePromiseBase, IDisposable
                {
                    internal Action MoveNext
                    {
                        [MethodImpl(InlineOption)]
                        get { return _moveNext; }
                    }

                    private PromiseMethodContinuer() { }

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
                        private static readonly ContextCallback s_executionContextCallback = ExecutionContextCallback;

                        private Continuer()
                        {
                            _moveNext = ContinueMethod;
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
                            _stateMachine = default(TStateMachine);
                            ObjectPool.MaybeRepool(this);
                        }

                        private static void ExecutionContextCallback(object state)
                        {
                            state.UnsafeAs<Continuer<TStateMachine>>()._stateMachine.MoveNext();
                        }

                        private void ContinueMethod()
                        {
#if PROMISE_DEBUG
                            SetCurrentInvoker(_owner);
                            try
#endif
                            {
                                if (_owner._executionContext != null)
                                {
                                    ExecutionContext.Run(_owner._executionContext, s_executionContextCallback, this);
                                }
                                else
                                {
                                    _stateMachine.MoveNext();
                                }
                            }
#if PROMISE_DEBUG
                            finally
                            {
                                ClearCurrentInvoker();
                            }
#endif
                        }
                    }
                }

                private PromiseMethodContinuer _continuer;

                [MethodImpl(InlineOption)]
                private static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref) where TStateMachine : IAsyncStateMachine
                {
                    if (_ref._continuer == null)
                    {
                        _ref._continuer = PromiseMethodContinuer.GetOrCreate(ref stateMachine, _ref);
                    }
                    if (Promise.Config.AsyncFlowExecutionContextEnabled)
                    {
                        _ref._executionContext = ExecutionContext.Capture();
                    }
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    if (_continuer != null)
                    {
                        _continuer.Dispose();
                        _continuer = null;
                    }
                    _executionContext = null;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(rejectContainer, state);
                    SetAwaitedComplete(handler);
                    _continuer.MoveNext.Invoke();
                }
            } // class AsyncPromiseRef

#else // !OPTIMIZED_ASYNC_MODE

            partial class AsyncPromiseRef<TResult>
            {
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private sealed partial class AsyncPromiseRefMachine<TStateMachine> : AsyncPromiseRef<TResult> where TStateMachine : IAsyncStateMachine
                {
                    private static readonly ContextCallback s_executionContextCallback = ExecutionContextCallback;

                    private AsyncPromiseRefMachine()
                    {
                        _moveNext = ContinueMethod;
                    }

                    [MethodImpl(InlineOption)]
                    new private static AsyncPromiseRefMachine<TStateMachine> GetOrCreate()
                    {
                        var obj = ObjectPool.TryTakeOrInvalid<AsyncPromiseRefMachine<TStateMachine>>();
                        return obj == InvalidAwaitSentinel.s_instance
                            ? new AsyncPromiseRefMachine<TStateMachine>()
                            : obj.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>();
                    }

                    internal static void SetStateMachine(ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref)
                    {
                        var promise = GetOrCreate();
                        promise.Reset();
                        // ORDER VERY IMPORTANT, ref must be set before copying stateMachine.
                        _ref = promise;
                        promise._stateMachine = stateMachine;
                    }

                    internal override void MaybeDispose()
                    {
                        Dispose();
                        _stateMachine = default(TStateMachine);
                        _executionContext = null;
                        ObjectPool.MaybeRepool(this);
                    }

                    private static void ExecutionContextCallback(object state)
                    {
                        state.UnsafeAs<AsyncPromiseRefMachine<TStateMachine>>()._stateMachine.MoveNext();
                    }

                    [MethodImpl(InlineOption)]
                    private void ContinueMethod()
                    {
                        if (_executionContext != null)
                        {
                            ExecutionContext.Run(_executionContext, s_executionContextCallback, this);
                        }
                        else
                        {
                            _stateMachine.MoveNext();
                        }
                    }

                    internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state)
                    {
                        ThrowIfInPool(this);
                        handler.SetCompletionState(rejectContainer, state);
                        SetAwaitedComplete(handler);
                        ContinueMethod();
                    }
                }

                private Action MoveNext
                {
                    [MethodImpl(InlineOption)]
                    get { return _moveNext; }
                }

                protected AsyncPromiseRef() { }

                [MethodImpl(InlineOption)]
                private static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref) where TStateMachine : IAsyncStateMachine
                {
                    if (_ref == null)
                    {
                        AsyncPromiseRefMachine<TStateMachine>.SetStateMachine(ref stateMachine, ref _ref);
                    }
                    if (Promise.Config.AsyncFlowExecutionContextEnabled)
                    {
                        _ref._executionContext = ExecutionContext.Capture();
                    }
                }

                internal override void MaybeDispose()
                {
                    Dispose();
                    _executionContext = null;
                    ObjectPool.MaybeRepool(this);
                }
            }
#endif // OPTIMIZED_ASYNC_MODE
        } // class PromiseRef
    } // class Internal
} // namespace Proto.Promises