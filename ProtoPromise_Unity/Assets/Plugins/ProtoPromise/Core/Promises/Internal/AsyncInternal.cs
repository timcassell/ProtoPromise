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
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

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
        /// This type and its members are intended for use by the compiler.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public struct PromiseMethodBuilder
        {
            private Internal.PromiseMethodBuilderInternal<Internal.VoidResult> _builder;

            [MethodImpl(Internal.InlineOption)]
            private PromiseMethodBuilder(Internal.PromiseMethodBuilderInternal<Internal.VoidResult> builder)
            {
                _builder = builder;
            }

            public Promise Task
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _builder.Task; }
            }

            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder Create()
            {
                return new PromiseMethodBuilder(Internal.PromiseMethodBuilderInternal<Internal.VoidResult>.Create());
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetException(Exception exception)
            {
                _builder.SetException(exception);
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetResult()
            {
                _builder.SetResult(new Internal.VoidResult());
            }

            [MethodImpl(Internal.InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
            }

            [SecuritySafeCritical]
            [MethodImpl(Internal.InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
            }

            [MethodImpl(Internal.InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                _builder.Start(ref stateMachine);
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine)
            {
                _builder.SetStateMachine(stateMachine);
            }
        }

        /// <summary>
        /// This type and its members are intended for use by the compiler.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public struct PromiseMethodBuilder<T>
        {
            private Internal.PromiseMethodBuilderInternal<T> _builder;

            [MethodImpl(Internal.InlineOption)]
            private PromiseMethodBuilder(Internal.PromiseMethodBuilderInternal<T> builder)
            {
                _builder = builder;
            }

            public Promise<T> Task
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _builder.Task; }
            }

            [MethodImpl(Internal.InlineOption)]
            public static PromiseMethodBuilder<T> Create()
            {
                return new PromiseMethodBuilder<T>(Internal.PromiseMethodBuilderInternal<T>.Create());
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetException(Exception exception)
            {
                _builder.SetException(exception);
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetResult(T result)
            {
                _builder.SetResult(result);
            }

            [MethodImpl(Internal.InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
            }

            [SecuritySafeCritical]
            [MethodImpl(Internal.InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
            }

            [MethodImpl(Internal.InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                _builder.Start(ref stateMachine);
            }

            [MethodImpl(Internal.InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine)
            {
                _builder.SetStateMachine(stateMachine);
            }
        }
    } // namespace Async.CompilerServices
} // namespace Proto.Promises

namespace Proto.Promises
{
    partial class Internal
    {
#if !OPTIMIZED_ASYNC_MODE
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal partial struct PromiseMethodBuilderInternal<TResult>
        {
            [MethodImpl(InlineOption)]
            private PromiseMethodBuilderInternal(PromiseRefBase.AsyncPromiseRef<TResult> promise)
            {
                _ref = promise;
            }

            public Promise<TResult> Task
            {
                [MethodImpl(InlineOption)]
                get { return new Promise<TResult>(_ref, _ref.Id, 0); }
            }

            [MethodImpl(InlineOption)]
            public static PromiseMethodBuilderInternal<TResult> Create()
            {
                return new PromiseMethodBuilderInternal<TResult>(PromiseRefBase.AsyncPromiseRef<TResult>.GetOrCreate());
            }

            public void SetException(Exception exception)
            {
                _ref.SetException(exception);
            }

            [MethodImpl(InlineOption)]
            public void SetResult(TResult result)
            {
                _ref.SetAsyncResult(result);
            }

            [MethodImpl(InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _ref.AwaitOnCompleted<TResult, TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
            }

            [SecuritySafeCritical]
            [MethodImpl(InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _ref.AwaitUnsafeOnCompleted<TResult, TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
            }

            [MethodImpl(InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                // TODO: to support ExecutionContext for AsyncLocal
                //new AsyncTaskMethodBuilder().Start(ref stateMachine);
                stateMachine.MoveNext();
            }

            [MethodImpl(InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }

#else // !OPTIMIZED_ASYNC_MODE

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal partial struct PromiseMethodBuilderInternal<TResult>
        {
            internal Promise<TResult> Task
            {
                [MethodImpl(InlineOption)]
                get { return new Promise<TResult>(_ref, _smallFields._id, 0, _smallFields._result); }
            }

            [MethodImpl(InlineOption)]
            internal static PromiseMethodBuilderInternal<TResult> Create()
            {
                return new PromiseMethodBuilderInternal<TResult>();
            }

            internal void SetException(Exception exception)
            {
                if (_ref == null)
                {
                    _ref = PromiseRefBase.AsyncPromiseRef<TResult>.GetOrCreate();
                    _smallFields._id = _ref.Id;
                }
                _ref.SetException(exception);
            }

            internal void SetResult(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TResult result)
            {
                if (_ref == null)
                {
                    _ref = PromiseResolvedSentinel;
                    _smallFields._result = result;
                    _smallFields._id = ValidIdFromApi;
                }
                else
                {
                    _ref.SetAsyncResult(result);
                }
            }

            [MethodImpl(InlineOption)]
            internal void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                SetStateMachine(ref stateMachine);
#if NET5_0_OR_GREATER
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
                    awaiter.OnCompleted(_ref.UnsafeAs<PromiseRefBase.AsyncPromiseRef<TResult>>().MoveNext);
                }
            }

            [SecuritySafeCritical]
            [MethodImpl(InlineOption)]
            internal void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                SetStateMachine(ref stateMachine);
#if NET5_0_OR_GREATER
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
                    awaiter.UnsafeOnCompleted(_ref.UnsafeAs<PromiseRefBase.AsyncPromiseRef<TResult>>().MoveNext);
                }
            }

            [MethodImpl(InlineOption)]
            internal void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                stateMachine.MoveNext();
            }

            [MethodImpl(InlineOption)]
            internal void SetStateMachine(IAsyncStateMachine stateMachine) { }

            [MethodImpl(InlineOption)]
            private void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                if (_ref == null)
                {
                    PromiseRefBase.AsyncPromiseRef<TResult>.SetStateMachine(ref stateMachine, ref _ref);
                    _smallFields._id = _ref.Id;
                }
            }
        }
#endif // DEBUG or IL2CPP

        partial class PromiseRefBase
        {
            [ThreadStatic]
            private static HandleablePromiseBase ts_currentRunner;

            [MethodImpl(InlineOption)]
            private HandleablePromiseBase ExchangeCurrentRunner(HandleablePromiseBase currentRunner)
            {
#if PROTO_PROMISE_STACK_UNWIND_DISABLE && PROTO_PROMISE_DEVELOPER_MODE
                return null;
#else
                var previous = ts_currentRunner;
                ts_currentRunner = currentRunner;
                return previous;
#endif
            }

            [MethodImpl(InlineOption)]
            internal void SetAsyncResult<TResult>(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                TResult result)
            {
                ThrowIfInPool(this);
                this.UnsafeAs<PromiseRef<TResult>>().SetResult(result);
                MaybeHandleCompletion();
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
                MaybeHandleCompletion();
            }

            private void MaybeHandleCompletion()
            {
                // If this is completed from another promise, let the stack can unwind so the other promise will schedule the continuation.
                var nextHandler = CompareExchangeWaiter(PromiseCompletionSentinel.s_instance, null);
                if (ExchangeCurrentRunner(nextHandler) != this)
                {
                    ts_currentRunner = null;
                    MaybeHandleNext(nextHandler);
                }
            }

            [MethodImpl(InlineOption)]
            internal void HookupAwaiter(PromiseRefBase awaiter, short promiseId)
            {
                ts_currentRunner = null;
                ValidateAwait(awaiter, promiseId);

                SetPrevious(awaiter);

                // TODO: detect if this is being called from another promise higher in the stack, and call AddWaiter and allow the stack to unwind instead of calling HookupExistingWaiter.
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
            [DebuggerNonUserCode]
#endif
            internal partial class AsyncPromiseRef<TResult> : AsyncPromiseBase<TResult>
            {
                [MethodImpl(InlineOption)]
                public static AsyncPromiseRef<TResult> GetOrCreate()
                {
                    var promise = ObjectPool.TryTake<AsyncPromiseRef<TResult>>()
                        ?? new AsyncPromiseRef<TResult>();
                    promise.Reset();
                    return promise;
                }

#if PROMISE_PROGRESS
                protected override void HookupAwaiterWithProgressVirt(PromiseRefBase awaiter, short promiseId, ushort depth, float minProgress, float maxProgress)
                {
                    ts_currentRunner = null;
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

                        // TODO: detect if this is being called from another promise higher in the stack, allow the stack to unwind instead of calling HandleNext.
                        awaiter.HandleNext(this);
                    }
                }
#endif

                partial void ReportProgressFromHookupWaiterWithProgress(PromiseRefBase other, ushort depth);
                partial void SetPreviousAndProgress(PromiseRefBase awaiter, float minProgress, float maxProgress);
                partial void SetAwaitedComplete(PromiseRefBase handler);
#if !PROMISE_PROGRESS && PROMISE_DEBUG
                [MethodImpl(InlineOption)]
                partial void SetPreviousAndProgress(PromiseRefBase other, float minProgress, float maxProgress)
                {
                    _previous = other;
                }

                [MethodImpl(InlineOption)]
                partial void SetAwaitedComplete(PromiseRefBase handler)
                {
                    _previous = null;
                }
#endif
            }

#if !OPTIMIZED_ASYNC_MODE
            [MethodImpl(InlineOption)]
            internal void AwaitOnCompleted<TResult, TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                var _this = this.UnsafeAs<AsyncPromiseRef<TResult>>();
                _this.SetStateMachine(ref stateMachine);
#if NET5_0_OR_GREATER
                if (null != default(TAwaiter) && awaiter is IPromiseAwaiter)
                {
                    ((IPromiseAwaiter) awaiter).AwaitOnCompletedInternal(_this);
                }
#else
                if (null != default(TAwaiter) && AwaitOverrider<TAwaiter>.IsOverridden())
                {
                    AwaitOverrider<TAwaiter>.AwaitOnCompletedInternal(ref awaiter, _this);
                }
#endif
                else
                {
                    awaiter.OnCompleted(_this.MoveNext);
                }
            }

            [MethodImpl(InlineOption)]
            internal void AwaitUnsafeOnCompleted<TResult, TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                var _this = this.UnsafeAs<AsyncPromiseRef<TResult>>();
                _this.SetStateMachine(ref stateMachine);
#if NET5_0_OR_GREATER
                if (null != default(TAwaiter) && awaiter is IPromiseAwaiter)
                {
                    ((IPromiseAwaiter) awaiter).AwaitOnCompletedInternal(_this);
                }
#else
                if (null != default(TAwaiter) && AwaitOverrider<TAwaiter>.IsOverridden())
                {
                    AwaitOverrider<TAwaiter>.AwaitOnCompletedInternal(ref awaiter, _this);
                }
#endif
                else
                {
                    awaiter.UnsafeOnCompleted(_this.MoveNext);
                }
            }

            sealed partial class AsyncPromiseRef<TResult>
            {
                internal Action MoveNext
                {
                    [MethodImpl(InlineOption)]
                    get { return _continuer.MoveNext; }
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode]
#endif
                private abstract partial class PromiseMethodContinuer : HandleablePromiseBase, IDisposable
                {
                    public Action MoveNext
                    {
                        [MethodImpl(InlineOption)]
                        get
                        {
                            ts_currentRunner = null;
                            return _moveNext;
                        }
                    }

                    private PromiseMethodContinuer() { }

                    public abstract void Dispose();

                    [MethodImpl(InlineOption)]
                    public static PromiseMethodContinuer GetOrCreate<TStateMachine>(ref TStateMachine stateMachine, ITraceable owner) where TStateMachine : IAsyncStateMachine
                    {
                        var continuer = Continuer<TStateMachine>.GetOrCreate(ref stateMachine);
#if PROMISE_DEBUG
                        continuer._owner = owner;
#endif
                        return continuer;
                    }

#if !PROTO_PROMISE_DEVELOPER_MODE
                    [DebuggerNonUserCode]
#endif
                    private sealed partial class Continuer<TStateMachine> : PromiseMethodContinuer where TStateMachine : IAsyncStateMachine
                    {
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
#if PROMISE_DEBUG
                            _owner = null;
#endif
                            _stateMachine = default(TStateMachine);
                            ObjectPool.MaybeRepool(this);
                        }

                        private void ContinueMethod()
                        {
#if PROMISE_DEBUG
                            SetCurrentInvoker(_owner);
                            try
                            {
                                _stateMachine.MoveNext();
                            }
                            finally
                            {
                                ClearCurrentInvoker();
                            }
#else
                            _stateMachine.MoveNext();
#endif
                        }
                    }
                }

                private PromiseMethodContinuer _continuer;

                [MethodImpl(InlineOption)]
                internal void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
                {
                    if (_continuer == null)
                    {
                        _continuer = PromiseMethodContinuer.GetOrCreate(ref stateMachine, this);
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
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler)
                {
                    ThrowIfInPool(this);
                    SetAwaitedComplete(handler);

                    var previousRunner = ExchangeCurrentRunner(this);

                    MoveNext();

                    handler = this;
                    nextHandler = ExchangeCurrentRunner(previousRunner);
                }
            } // class AsyncPromiseRef

#else // !OPTIMIZED_ASYNC_MODE

            partial class AsyncPromiseRef<TResult>
            {
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode]
#endif
                private sealed partial class AsyncPromiseRefMachine<TStateMachine> : AsyncPromiseRef<TResult> where TStateMachine : IAsyncStateMachine
                {
                    private AsyncPromiseRefMachine()
                    {
                        _moveNext = ContinueMethod;
                    }

                    internal static void SetStateMachine(ref TStateMachine stateMachine, ref PromiseRefBase _ref)
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
                        ObjectPool.MaybeRepool(this);
                    }

                    [MethodImpl(InlineOption)]
                    private void ContinueMethod()
                    {
                        _stateMachine.MoveNext();
                    }

                    internal override void Handle(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler)
                    {
                        ThrowIfInPool(this);
                        SetAwaitedComplete(handler);

                        var previousRunner = ExchangeCurrentRunner(this);

                        ContinueMethod();

                        handler = this;
                        nextHandler = ExchangeCurrentRunner(previousRunner);
                    }
                }

                internal Action MoveNext
                {
                    [MethodImpl(InlineOption)]
                    get
                    {
                        ts_currentRunner = null;
                        return _moveNext;
                    }
                }

                protected AsyncPromiseRef() { }

                [MethodImpl(InlineOption)]
                internal static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref PromiseRefBase _ref) where TStateMachine : IAsyncStateMachine
                {
                    AsyncPromiseRefMachine<TStateMachine>.SetStateMachine(ref stateMachine, ref _ref);
                }

                protected override void MaybeDispose()
                {
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }
            }
#endif // OPTIMIZED_ASYNC_MODE
        } // class PromiseRef
    } // class Internal
} // namespace Proto.Promises