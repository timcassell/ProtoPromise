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

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CS0436 // Type conflicts with imported type

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Proto.Promises.Async.CompilerServices;

namespace System.Runtime.CompilerServices
{
    // I would #ifdef this entire file, but Unity complains about namespaces changed with define symbols.
#if CSHARP_7_3_OR_NEWER // Custom async builders only available after C# 7.2.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false, AllowMultiple = false)]
    internal sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public Type BuilderType { get; }

        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }
    }
#endif // CSHARP_7_3_OR_NEWER
}

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER // Custom async builders only available after C# 7.2.
    [AsyncMethodBuilder(typeof(PromiseMethodBuilder))]
    partial struct Promise { }

    [AsyncMethodBuilder(typeof(PromiseMethodBuilder<>))]
    partial struct Promise<T> { }
#endif // CSHARP_7_3_OR_NEWER

    namespace Async.CompilerServices
    {
#if CSHARP_7_3_OR_NEWER // Custom async builders only available after C# 7.2.
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
#endif // CSHARP_7_3_OR_NEWER
    } // namespace Async.CompilerServices
} // namespace Proto.Promises

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER // Custom async builders only available after C# 7.2.
    partial class Internal
    {
#if !OPTIMIZED_ASYNC_MODE
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        internal partial struct PromiseMethodBuilderInternal<T>
        {
            [MethodImpl(InlineOption)]
            private PromiseMethodBuilderInternal(PromiseRef.AsyncPromiseRef promise)
            {
                _ref = promise;
            }

            public Promise<T> Task
            {
                [MethodImpl(InlineOption)]
                get { return new Promise<T>(_ref, _ref.Id, 0); }
            }

            [MethodImpl(InlineOption)]
            public static PromiseMethodBuilderInternal<T> Create()
            {
                return new PromiseMethodBuilderInternal<T>(PromiseRef.AsyncPromiseRef.GetOrCreate());
            }

            public void SetException(Exception exception)
            {
                _ref.SetException(exception);
            }

            [MethodImpl(InlineOption)]
            public void SetResult(T result)
            {
                _ref.SetResult(result);
            }

            [MethodImpl(InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _ref.SetStateMachine(ref stateMachine);
                if (null != default(TAwaiter) && AwaitOverrider<TAwaiter>.IsOverridden())
                {
                    AwaitOverrider<TAwaiter>.AwaitOnCompletedInternal(ref awaiter, _ref);
                }
                else
                {
                    awaiter.OnCompleted(_ref.MoveNext);
                }
            }

            [SecuritySafeCritical]
            [MethodImpl(InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                _ref.SetStateMachine(ref stateMachine);
                if (null != default(TAwaiter) && AwaitOverrider<TAwaiter>.IsOverridden())
                {
                    AwaitOverrider<TAwaiter>.AwaitOnCompletedInternal(ref awaiter, _ref);
                }
                else
                {
                    awaiter.UnsafeOnCompleted(_ref.MoveNext);
                }
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
        internal partial struct PromiseMethodBuilderInternal<T>
        {
            public Promise<T> Task
            {
                [MethodImpl(InlineOption)]
                get { return new Promise<T>(_ref, _smallFields._id, 0, _smallFields._result); }
            }

            [MethodImpl(InlineOption)]
            public static PromiseMethodBuilderInternal<T> Create()
            {
                return new PromiseMethodBuilderInternal<T>();
            }

            public void SetException(Exception exception)
            {
                if (_ref is null)
                {
                    _ref = PromiseRef.AsyncPromiseRef.GetOrCreate();
                    _smallFields._id = _ref.Id;
                }
                _ref.SetException(exception);
            }

            public void SetResult(in T result)
            {
                if (_ref is null)
                {
                    _smallFields._result = result;
                    _smallFields._id = ValidIdFromApi;
                }
                else
                {
                    _ref.SetResult(result);
                }
            }

            [MethodImpl(InlineOption)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : INotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                SetStateMachine(ref stateMachine);
                // TODO: optimize this check to call direct in .Net 5 or later. (see https://github.com/dotnet/runtime/discussions/61574)
                if (null != default(TAwaiter) && AwaitOverrider<TAwaiter>.IsOverridden())
                {
                    AwaitOverrider<TAwaiter>.AwaitOnCompletedInternal(ref awaiter, _ref);
                }
                else
                {
                    awaiter.OnCompleted(_ref.MoveNext);
                }
            }

            [SecuritySafeCritical]
            [MethodImpl(InlineOption)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                SetStateMachine(ref stateMachine);
                if (null != default(TAwaiter) && AwaitOverrider<TAwaiter>.IsOverridden())
                {
                    AwaitOverrider<TAwaiter>.AwaitOnCompletedInternal(ref awaiter, _ref);
                }
                else
                {
                    awaiter.UnsafeOnCompleted(_ref.MoveNext);
                }
            }

            [MethodImpl(InlineOption)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                stateMachine.MoveNext();
            }

            [MethodImpl(InlineOption)]
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }

            [MethodImpl(InlineOption)]
            private void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : IAsyncStateMachine
            {
                if (_ref is null)
                {
                    PromiseRef.AsyncPromiseRef.SetStateMachine(ref stateMachine, ref _ref);
                    _smallFields._id = _ref.Id;
                }
            }
        }
#endif // DEBUG or IL2CPP

        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            internal partial class AsyncPromiseRef : AsyncPromiseBase
            {
                [ThreadStatic]
                private static AsyncPromiseRef _currentRunner;

                [MethodImpl(InlineOption)]
                private AsyncPromiseRef ExchangeCurrentRunner(AsyncPromiseRef currentRunner)
                {
                    var previous = _currentRunner;
                    _currentRunner = currentRunner;
                    return previous;
                }

#if !PROMISE_PROGRESS
                [MethodImpl(InlineOption)]
                internal void SetPreviousAndMaybeSubscribeProgress(PromiseRef other, ushort depth, float minProgress, float maxProgress, ref ExecutionScheduler executionScheduler)
                {
                    _valueOrPrevious = other;
                }

                [MethodImpl(InlineOption)]
                private void SetAwaitedComplete(PromiseRef handler, ref ExecutionScheduler executionScheduler)
                {
                    _valueOrPrevious = null;
                }
#endif

                [MethodImpl(InlineOption)]
                public static AsyncPromiseRef GetOrCreate()
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<AsyncPromiseRef>()
                        ?? new AsyncPromiseRef();
                    promise.Reset();
                    return promise;
                }

                [MethodImpl(InlineOption)]
                internal void SetResult<T>(
#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    T result)
                {
                    ThrowIfInPool(this);
                    ValueContainer valueContainer = CreateResolveContainer(result);
                    MaybeHandleCompletion(valueContainer, Promise.State.Resolved);
                }

                internal void SetException(Exception exception)
                {
                    ValueContainer valueContainer;
                    Promise.State state;
                    if (exception is OperationCanceledException)
                    {
                        valueContainer = CancelContainerVoid.GetOrCreate();
                        state = Promise.State.Canceled;
                    }
                    else
                    {
                        valueContainer = CreateRejectContainer(exception, int.MinValue, this);
                        state = Promise.State.Rejected;
                    }
                    MaybeHandleCompletion(valueContainer, state);
                }

                private void MaybeHandleCompletion(ValueContainer valueContainer, Promise.State state)
                {
                    // If this is completed from another promise, just set the result so that the stack can unwind and the other promise will schedule the continuation.
                    if (ExchangeCurrentRunner(null) == this)
                    {
                        SetResult(valueContainer, state);
                    }
                    else
                    {
                        HandleInternal(valueContainer, state);
                    }
                }
            }

#if !OPTIMIZED_ASYNC_MODE
            sealed partial class AsyncPromiseRef
            {
                internal Action MoveNext
                {
                    [MethodImpl(InlineOption)]
                    get { return _continuer.MoveNext; }
                }

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode]
#endif
                private abstract partial class PromiseMethodContinuer : IDisposable
                {
                    internal Action MoveNext
                    {
                        [MethodImpl(InlineOption)]
                        get { return _moveNext; }
                        [MethodImpl(InlineOption)]
                        private protected set { _moveNext = value; }
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
                    private sealed partial class Continuer<TStateMachine> : PromiseMethodContinuer, ILinked<Continuer<TStateMachine>> where TStateMachine : IAsyncStateMachine
                    {
                        private Continuer()
                        {
                            _moveNext = ContinueMethod;
                        }

                        [MethodImpl(InlineOption)]
                        public static Continuer<TStateMachine> GetOrCreate(ref TStateMachine stateMachine)
                        {
                            var continuer = ObjectPool<Continuer<TStateMachine>>.TryTake<Continuer<TStateMachine>>()
                                ?? new Continuer<TStateMachine>();
                            continuer._stateMachine = stateMachine;
                            return continuer;
                        }

                        public override void Dispose()
                        {
#if PROMISE_DEBUG
                            _owner = null;
#endif
                            _stateMachine = default;
                            ObjectPool<Continuer<TStateMachine>>.MaybeRepool(this);
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
                public void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
                {
                    if (_continuer is null)
                    {
                        _continuer = PromiseMethodContinuer.GetOrCreate(ref stateMachine, this);
                    }
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    if (_continuer != null)
                    {
                        _continuer.Dispose();
                        _continuer = null;
                    }
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    SetAwaitedComplete(handler, ref executionScheduler);

                    AsyncPromiseRef previousRunner = ExchangeCurrentRunner(this);

                    MoveNext();

                    bool isComplete = ExchangeCurrentRunner(previousRunner) == null;
                    if (isComplete)
                    {
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                        lock (this)
#endif
                        {
                            Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                            nextHandler = Interlocked.Exchange(ref _waiter, null);
                        }
                        HandleProgressListener(handler.State, 0, ref executionScheduler);
                        WaitWhileProgressFlags(PromiseFlags.Subscribing);
                        handler.MaybeDispose();
                        handler = this;
                    }
                    else
                    {
                        nextHandler = null;
                        WaitWhileProgressFlags(PromiseFlags.Subscribing);
                    }
                }
            } // class AsyncPromiseRef

#else // !OPTIMIZED_ASYNC_MODE

            partial class AsyncPromiseRef
            {
#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode]
#endif
                private sealed partial class AsyncPromiseRefMachine<TStateMachine> : AsyncPromiseRef where TStateMachine : IAsyncStateMachine
                {
                    private AsyncPromiseRefMachine()
                    {
                        _moveNext = ContinueMethod;
                    }

                    public static void SetStateMachine(ref TStateMachine stateMachine, ref AsyncPromiseRef _ref)
                    {
                        var promise = ObjectPool<HandleablePromiseBase>.TryTake<AsyncPromiseRefMachine<TStateMachine>>()
                            ?? new AsyncPromiseRefMachine<TStateMachine>();
                        promise.Reset();
                        // ORDER VERY IMPORTANT, ref must be set before copying stateMachine.
                        _ref = promise;
                        promise._stateMachine = stateMachine;
                    }

                    protected override void Dispose()
                    {
                        SuperDispose();
                        _stateMachine = default;
                        ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                    }

                    [MethodImpl(InlineOption)]
                    private void ContinueMethod()
                    {
                        _stateMachine.MoveNext();
                    }

                    internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                    {
                        ThrowIfInPool(this);
                        SetAwaitedComplete(handler, ref executionScheduler);

                        AsyncPromiseRef previousRunner = ExchangeCurrentRunner(this);

                        ContinueMethod();

                        bool isComplete = ExchangeCurrentRunner(previousRunner) == null;
                        if (isComplete)
                        {
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                            lock (this)
#endif
                            {
                                Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                                nextHandler = Interlocked.Exchange(ref _waiter, null);
                            }
                            HandleProgressListener(handler.State, 0, ref executionScheduler);
                            WaitWhileProgressFlags(PromiseFlags.Subscribing);
                            handler.MaybeDispose();
                            handler = this;
                        }
                        else
                        {
                            nextHandler = null;
                            WaitWhileProgressFlags(PromiseFlags.Subscribing);
                        }
                    }
                }

                internal Action MoveNext
                {
                    [MethodImpl(InlineOption)]
                    get { return _moveNext; }
                }

                protected AsyncPromiseRef() { }

                [MethodImpl(InlineOption)]
                internal static void SetStateMachine<TStateMachine>(ref TStateMachine stateMachine, ref AsyncPromiseRef _ref) where TStateMachine : IAsyncStateMachine
                {
                    AsyncPromiseRefMachine<TStateMachine>.SetStateMachine(ref stateMachine, ref _ref);
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                // Used for child to call base dispose without repooling for both types.
                // This is necessary because C# doesn't allow `base.base.Dispose()`.
                [MethodImpl(InlineOption)]
                protected void SuperDispose()
                {
                    base.Dispose();
                }
            }
#endif // OPTIMIZED_ASYNC_MODE
        } // class PromiseRef
    } // class Internal
#endif // CSHARP_7_3_OR_NEWER
} // namespace Proto.Promises