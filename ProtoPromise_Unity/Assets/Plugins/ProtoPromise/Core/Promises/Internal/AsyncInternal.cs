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

// Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
// Unity fixed in 2020.3.20f1 and 2021.1.24f1, but it's simpler to just check for 2021.2 or newer.
// Don't use optimized mode in DEBUG mode for causality traces.
#if (ENABLE_IL2CPP && !UNITY_2021_2_OR_NEWER) || PROMISE_DEBUG
#undef OPTIMIZED_ASYNC_MODE
#else
#define OPTIMIZED_ASYNC_MODE
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0038 // Use pattern matching
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable 0436 // Type conflicts with imported type

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Proto.Promises.Async.CompilerServices;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false, AllowMultiple = false)]
    internal sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public Type BuilderType { get; private set; }

        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }
    }

#if NET_LEGACY
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface INotifyCompletion
    {
        void OnCompleted(Action continuation);
    }

    public interface ICriticalNotifyCompletion : INotifyCompletion
    {
        void UnsafeOnCompleted(Action continuation);
    }

    public interface IAsyncStateMachine
    {
        void MoveNext();
        void SetStateMachine(IAsyncStateMachine stateMachine);
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#endif // NET_LEGACY
}

namespace Proto.Promises
{
    [AsyncMethodBuilder(typeof(PromiseMethodBuilder))]
    partial struct Promise { }

    [AsyncMethodBuilder(typeof(PromiseMethodBuilder<>))]
    partial struct Promise<T> { }

    namespace Async.CompilerServices
    {
        /// <summary>
        /// Provides a builder for asynchronous methods that return <see cref="Promise"/>.
        /// This type is intended for compiler use only.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public partial struct PromiseMethodBuilder
        {
            /// <summary>
            /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
            /// </summary>
            /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="awaiter">The awaiter.</param>
            /// <param name="stateMachine">The state machine.</param>
            [MethodImpl(Internal.InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref _ref);
            }

            /// <summary>
            /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
            /// </summary>
            /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="awaiter">The awaiter.</param>
            /// <param name="stateMachine">The state machine.</param>
            [SecuritySafeCritical]
            [MethodImpl(Internal.InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref _ref);
            }

            /// <summary>Initiates the builder's execution with the associated state machine.</summary>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="stateMachine">The state machine instance, passed by reference.</param>
            [MethodImpl(Internal.InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.Start(ref stateMachine, ref _ref);
            }

            /// <summary>Does nothing.</summary>
            /// <param name="stateMachine">The heap-allocated state machine object.</param>
            [MethodImpl(Internal.InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }

        /// <summary>
        /// Provides a builder for asynchronous methods that return <see cref="Promise{T}"/>.
        /// This type is intended for compiler use only.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public partial struct PromiseMethodBuilder<T>
        {
            /// <summary>
            /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
            /// </summary>
            /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="awaiter">The awaiter.</param>
            /// <param name="stateMachine">The state machine.</param>
            [MethodImpl(Internal.InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                Internal.PromiseRefBase.AsyncPromiseRef<T>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref _ref);
            }

            /// <summary>
            /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
            /// </summary>
            /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="awaiter">The awaiter.</param>
            /// <param name="stateMachine">The state machine.</param>
            [SecuritySafeCritical]
            [MethodImpl(Internal.InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                Internal.PromiseRefBase.AsyncPromiseRef<T>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref _ref);
            }

            /// <summary>Initiates the builder's execution with the associated state machine.</summary>
            /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
            /// <param name="stateMachine">The state machine instance, passed by reference.</param>
            [MethodImpl(Internal.InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                Internal.PromiseRefBase.AsyncPromiseRef<T>.Start(ref stateMachine, ref _ref);
            }

            /// <summary>Does nothing.</summary>
            /// <param name="stateMachine">The heap-allocated state machine object.</param>
            [MethodImpl(Internal.InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }

#if !OPTIMIZED_ASYNC_MODE

        partial struct PromiseMethodBuilder
        {
            [MethodImpl(Internal.InlineOption)]
            private PromiseMethodBuilder(Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult> promise)
            {
                _ref = promise;
            }

            /// <summary>Gets the <see cref="Promise"/> for this builder.</summary>
            /// <returns>The <see cref="Promise"/> representing the builder's asynchronous operation.</returns>
            public Promise Task
            {
                [MethodImpl(Internal.InlineOption)]
                get { return new Promise(_ref, _ref.Id, 0); }
            }

            /// <summary>Initializes a new <see cref="PromiseMethodBuilder"/>.</summary>
            /// <returns>The initialized <see cref="PromiseMethodBuilder"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder Create()
            {
                return new PromiseMethodBuilder(Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.GetOrCreate());
            }

            /// <summary>
            /// Completes the <see cref="Promise"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
            {
                _ref.SetException(exception);
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Resolved</see> state.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult()
            {
                _ref.SetAsyncResultVoid();
            }
        }

        partial struct PromiseMethodBuilder<T>
        {
            [MethodImpl(Internal.InlineOption)]
            private PromiseMethodBuilder(Internal.PromiseRefBase.AsyncPromiseRef<T> promise)
            {
                _ref = promise;
            }

            /// <summary>Gets the <see cref="Promise{T}"/> for this builder.</summary>
            /// <returns>The <see cref="Promise{T}"/> representing the builder's asynchronous operation.</returns>
            public Promise<T> Task
            {
                [MethodImpl(Internal.InlineOption)]
                get { return new Promise<T>(_ref, _ref.Id, 0); }
            }

            /// <summary>Initializes a new <see cref="PromiseMethodBuilder{T}"/>.</summary>
            /// <returns>The initialized <see cref="PromiseMethodBuilder{T}"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder<T> Create()
            {
                return new PromiseMethodBuilder<T>(Internal.PromiseRefBase.AsyncPromiseRef<T>.GetOrCreate());
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
            {
                _ref.SetException(exception);
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Resolved</see> state with the specified result.
            /// </summary>
            /// <param name="result">The result to use to complete the task.</param>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult(T result)
            {
                _ref.SetAsyncResult(result);
            }
        }

#else // !OPTIMIZED_ASYNC_MODE

        // This code could be used for DEBUG mode, but IL2CPP requires the non-optimized code even in RELEASE mode, and I don't want to add extra unnecessary null checks there.
        partial struct PromiseMethodBuilder
        {
            /// <summary>Gets the <see cref="Promise"/> for this builder.</summary>
            /// <returns>The <see cref="Promise"/> representing the builder's asynchronous operation.</returns>
            public Promise Task
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return _ref == null ? Promise.Resolved() : new Promise(_ref, _ref.Id, 0);
                }
            }
            
            /// <summary>Initializes a new <see cref="PromiseMethodBuilder"/>.</summary>
            /// <returns>The initialized <see cref="PromiseMethodBuilder"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder Create()
            {
                return default(PromiseMethodBuilder);
            }

            /// <summary>
            /// Completes the <see cref="Promise"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
            {
                if (_ref == null)
                {
                    _ref = Internal.PromiseRefBase.AsyncPromiseRef<Internal.VoidResult>.GetOrCreate();
                }
                _ref.SetException(exception);
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Resolved</see> state.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult()
            {
                if (_ref != null)
                {
                    _ref.SetAsyncResultVoid();
                }
            }
        }

        partial struct PromiseMethodBuilder<T>
        {
            /// <summary>Gets the <see cref="Promise{T}"/> for this builder.</summary>
            /// <returns>The <see cref="Promise{T}"/> representing the builder's asynchronous operation.</returns>
            public Promise<T> Task
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return _ref == null ? new Promise<T>(_result) : new Promise<T>(_ref, _ref.Id, 0);
                }
            }

            /// <summary>Initializes a new <see cref="PromiseMethodBuilder{T}"/>.</summary>
            /// <returns>The initialized <see cref="PromiseMethodBuilder{T}"/>.</returns>
            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder<T> Create()
            {
                return default(PromiseMethodBuilder<T>);
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Rejected</see> state with the specified exception.
            /// </summary>
            /// <param name="exception">The <see cref="Exception"/> to use to reject the promise.</param>
            public void SetException(Exception exception)
            {
                if (_ref == null)
                {
                    _ref = Internal.PromiseRefBase.AsyncPromiseRef<T>.GetOrCreate();
                }
                _ref.SetException(exception);
            }

            /// <summary>
            /// Completes the <see cref="Promise{T}"/> in the <see cref="Promise.State">Resolved</see> state with the specified result.
            /// </summary>
            /// <param name="result">The result to use to complete the task.</param>
            [MethodImpl(Internal.InlineOption)]
            public void SetResult(T result)
            {
                if (_ref == null)
                {
                    _result = result;
                }
                else
                {
                    _ref.SetAsyncResult(result);
                }
            }
        }
#endif // !OPTIMIZED_ASYNC_MODE
    } // namespace Async.CompilerServices
} // namespace Proto.Promises

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
            [MethodImpl(InlineOption)]
            internal void HookupAwaiter(PromiseRefBase awaiter, short promiseId)
            {
                ValidateAwait(awaiter, promiseId);

                SetPrevious(awaiter);

                awaiter.HookupExistingWaiter(promiseId, this);
            }

            partial void SetPrevious(PromiseRefBase awaiter);
#if PROMISE_DEBUG
            partial void SetPrevious(PromiseRefBase awaiter)
            {
                _previous = awaiter;
            }
#endif

            [MethodImpl(InlineOption)]
            internal void HookupAwaiterWithProgress(PromiseRefBase awaiter, short promiseId, ushort depth, float minProgress, float maxProgress)
            {
#if PROMISE_PROGRESS
                HookupAwaiterWithProgressVirt(awaiter, promiseId, depth, minProgress, maxProgress);
#else
                HookupAwaiter(awaiter, promiseId);
#endif
            }

#if PROMISE_PROGRESS
            protected virtual void HookupAwaiterWithProgressVirt(PromiseRefBase awaiter, short promiseId, ushort depth, float minProgress, float maxProgress) { throw new System.InvalidOperationException(); }
#endif

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial class AsyncPromiseRef<TResult> : AsyncPromiseBase<TResult>
            {
                [MethodImpl(InlineOption)]
                internal static AsyncPromiseRef<TResult> GetOrCreate()
                {
                    var promise = ObjectPool.TryTake<AsyncPromiseRef<TResult>>()
                        ?? new AsyncPromiseRef<TResult>();
                    promise.Reset();
                    return promise;
                }

                internal void SetException(Exception exception)
                {
                    if (exception is OperationCanceledException)
                    {
                        SetRejectOrCancel(RejectContainer.s_completionSentinel, Promise.State.Canceled);
                    }
                    else
                    {
                        SetRejectOrCancel(CreateRejectContainer(exception, int.MinValue, this), Promise.State.Rejected);
                    }
                    HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                internal void SetAsyncResultVoid()
                {
                    ThrowIfInPool(this);
                    State = Promise.State.Resolved;
                    HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                internal void SetAsyncResult(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    TResult result)
                {
                    ThrowIfInPool(this);
                    SetResult(result);
                    HandleNextInternal();
                }

                [MethodImpl(InlineOption)]
                internal static void Start<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref)
                    where TStateMachine : IAsyncStateMachine
                {
                    if (Promise.Config.AsyncFlowExecutionContextEnabled)
                    {
                        // To support ExecutionContext for AsyncLocal<T>.
#if !NET_LEGACY
                        // We can use AsyncTaskMethodBuilder to run the state machine on the execution context without creating an object. https://github.com/dotnet/runtime/discussions/56202#discussioncomment-1042195
                        new AsyncTaskMethodBuilder().Start(ref stateMachine);
#else
                        // AsyncTaskMethodBuilder isn't available pre .Net 4.5, so we have to create the object to run the state machine on the execution context.
                        SetStateMachine(ref stateMachine, ref _ref);
                        _ref.MoveNext();
#endif
                    }
                    else
                    {
                        stateMachine.MoveNext();
                    }
                }

                [MethodImpl(InlineOption)]
                internal static void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref)
                    where TAwaiter : INotifyCompletion
                    where TStateMachine : IAsyncStateMachine
                {
                    SetStateMachine(ref stateMachine, ref _ref);
#if NETCOREAPP
                    if (null != default(TAwaiter) && awaiter is IPromiseAwaiter)
                    {
                        ((IPromiseAwaiter) awaiter).AwaitOnCompletedInternal(_ref);
                    }
#else
                    if (null != default(TAwaiter) && AwaitOverrider<TAwaiter>.IsOverridden())
                    {
                        AwaitOverrider<TAwaiter>.AwaitOnCompletedInternal(ref awaiter, _ref);
                    }
#endif
                    else
                    {
                        awaiter.OnCompleted(_ref.MoveNext);
                    }
                }

                [MethodImpl(InlineOption)]
                internal static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref)
                    where TAwaiter : ICriticalNotifyCompletion
                    where TStateMachine : IAsyncStateMachine
                {
                    SetStateMachine(ref stateMachine, ref _ref);
#if NETCOREAPP
                    if (null != default(TAwaiter) && awaiter is IPromiseAwaiter)
                    {
                        ((IPromiseAwaiter) awaiter).AwaitOnCompletedInternal(_ref);
                    }
#else
                    if (null != default(TAwaiter) && AwaitOverrider<TAwaiter>.IsOverridden())
                    {
                        AwaitOverrider<TAwaiter>.AwaitOnCompletedInternal(ref awaiter, _ref);
                    }
#endif
                    else
                    {
                        awaiter.UnsafeOnCompleted(_ref.MoveNext);
                    }
                }

#if PROMISE_PROGRESS
                protected override void HookupAwaiterWithProgressVirt(PromiseRefBase awaiter, short promiseId, ushort depth, float minProgress, float maxProgress)
                {
                    ValidateAwait(awaiter, promiseId);

                    SetPreviousAndProgress(awaiter, minProgress, maxProgress);

                    awaiter.InterlockedIncrementProgressReportingCount();
                    HandleablePromiseBase previousWaiter;
                    PromiseRefBase promiseSingleAwait = awaiter.AddWaiter(promiseId, this, out previousWaiter);
                    if (previousWaiter == null)
                    {
                        ReportProgressFromHookupWaiterWithProgress(awaiter, depth);
                    }
                    else
                    {
                        awaiter.InterlockedDecrementProgressReportingCount();
                        if (!VerifyWaiter(promiseSingleAwait))
                        {
                            throw new InvalidOperationException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", GetFormattedStacktrace(2));
                        }

                        awaiter.HandleNext(this);
                    }
                }
#endif

                partial void SetAwaitedComplete(PromiseRefBase handler);
#if !PROMISE_PROGRESS && PROMISE_DEBUG
                [MethodImpl(InlineOption)]
                partial void SetAwaitedComplete(PromiseRefBase handler)
                {
                    _previous = null;
                }
#endif
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
                        public static Continuer<TStateMachine> GetOrCreate(ref TStateMachine stateMachine)
                        {
                            var continuer = ObjectPool.TryTake<Continuer<TStateMachine>>()
                                ?? new Continuer<TStateMachine>();
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
                            SetCurrentInvoker(_owner);
                            try
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
                            finally
                            {
                                ClearCurrentInvoker();
                            }
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

                protected override void MaybeDispose()
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

                internal override void Handle(PromiseRefBase handler)
                {
                    ThrowIfInPool(this);
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

                    internal static void SetStateMachine(ref TStateMachine stateMachine, ref AsyncPromiseRef<TResult> _ref)
                    {
                        var promise = ObjectPool.TryTake<AsyncPromiseRefMachine<TStateMachine>>()
                            ?? new AsyncPromiseRefMachine<TStateMachine>();
                        promise.Reset();
                        // ORDER VERY IMPORTANT, ref must be set before copying stateMachine.
                        _ref = promise;
                        promise._stateMachine = stateMachine;
                    }

                    protected override void MaybeDispose()
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

                    internal override void Handle(PromiseRefBase handler)
                    {
                        ThrowIfInPool(this);
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

                protected override void MaybeDispose()
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