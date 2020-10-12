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

#if CSHARP_7_OR_LATER

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0060 // Remove unused parameter

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using Proto.Promises.Await;
using Proto.Utils;

namespace Proto.Promises.Await
{
    // Interfaces taken from Microsoft.Bot.Builder
    public interface IAwaitable<out T>
    {
        IAwaiter<T> GetAwaiter();
    }

    public interface IAwaiter<out T> : INotifyCompletion
    {
        bool IsCompleted { get; }
        T GetResult();
    }

    public interface IAwaitable
    {
        IAwaiter GetAwaiter();
    }

    public interface IAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }
        void GetResult();
    }
}

namespace Proto.Promises
{
    [AsyncMethodBuilder(typeof(Async.CompilerServices.PromiseMethodBuilder))]
    partial class Promise : IAwaitable, IAwaiter, ICriticalNotifyCompletion
    {
        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
        public IAwaiter GetAwaiter()
        {
            ValidateOperation(this, 1);

            RetainInternal();
            return this;
        }

        bool IAwaiter.IsCompleted
        {
            get
            {
                ValidateOperation(this, 1);

                return _state != State.Pending;
            }
        }

        void IAwaiter.GetResult()
        {
            ValidateOperation(this, 1);

            if (_state == State.Resolved)
            {
                ReleaseInternal();
                return;
            }
            // Throw unhandled exception or canceled exception.
            Exception exception = ((Internal.IThrowable) _valueOrPrevious).GetException();
            // We're throwing here, no need to throw again.
            _wasWaitedOn = true;
            ReleaseInternal();
            throw exception;
        }

        void INotifyCompletion.OnCompleted(Action continuation)
        {
            ValidateOperation(this, 1);

            Finally(continuation);
        }

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
        {
            ValidateOperation(this, 1);

            Finally(continuation);
        }
    }

    [AsyncMethodBuilder(typeof(Async.CompilerServices.PromiseMethodBuilder<>))]
    partial class Promise<T> : IAwaitable<T>, IAwaiter<T>
    {
        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
        public new IAwaiter<T> GetAwaiter()
        {
            ValidateOperation(this, 1);

            RetainInternal();
            return this;
        }

        bool IAwaiter<T>.IsCompleted
        {
            get
            {
                ValidateOperation(this, 1);

                return _state != State.Pending;
            }
        }

        T IAwaiter<T>.GetResult()
        {
            ValidateOperation(this, 1);

            if (_state == State.Resolved)
            {
                T value = ((Internal.ResolveContainer<T>) _valueOrPrevious).value;
                ReleaseInternal();
                return value;
            }
            // Throw unhandled exception or canceled exception.
            Exception exception = ((Internal.IThrowable) _valueOrPrevious).GetException();
            // We're throwing here, no need to throw again.
            _wasWaitedOn = true;
            ReleaseInternal();
            throw exception;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public Type BuilderType { get; }

        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }
    }
}

#if PROMISE_DEBUG || ENABLE_IL2CPP
// Fix for IL2CPP compile bug. https://issuetracker.unity3d.com/issues/il2cpp-incorrect-results-when-calling-a-method-from-outside-class-in-a-struct
// Also use this in DEBUG mode for cleaner causality traces.
namespace Proto.Promises
{
    partial class Internal
    {
        /// <summary>
        /// This type and its members are intended for use by the compiler (async Promise functions).
        /// </summary>
        [DebuggerNonUserCode]
        internal abstract class PromiseMethodContinuer : IDisposable
        {
            public abstract Action Continuation { get; }
#if PROMISE_DEBUG
            protected ITraceable _owner;
#endif

            private PromiseMethodContinuer() { }

            public abstract void Dispose();

            public static PromiseMethodContinuer GetOrCreate<TStateMachine>(ref TStateMachine stateMachine, ITraceable owner) where TStateMachine : IAsyncStateMachine
            {
                var continuer = Continuer<TStateMachine>.ReuseOrNew(ref stateMachine);
#if PROMISE_DEBUG
                continuer._owner = owner;
#endif
                return continuer;
            }

            /// <summary>
            /// Generic class to reference the state machine without boxing it.
            /// </summary>
            [DebuggerNonUserCode]
            private sealed class Continuer<TStateMachine> : PromiseMethodContinuer, ILinked<Continuer<TStateMachine>> where TStateMachine : IAsyncStateMachine
            {
                private static ValueLinkedStack<Continuer<TStateMachine>> _pool;

                static Continuer()
                {
                    OnClearPool += () => _pool.Clear();
                }

                Continuer<TStateMachine> ILinked<Continuer<TStateMachine>>.Next { get; set; }
                public override Action Continuation { get { return _continuation; } }
                private readonly Action _continuation;
                private TStateMachine _stateMachine;

                private Continuer()
                {
                    _continuation = Continue;
                }

                public static Continuer<TStateMachine> ReuseOrNew(ref TStateMachine stateMachine)
                {
                    var continuer = _pool.IsNotEmpty ? _pool.Pop() : new Continuer<TStateMachine>();
                    continuer._stateMachine = stateMachine;
                    return continuer;
                }

                public override void Dispose()
                {
#if PROMISE_DEBUG
                    _owner = null;
#endif
                    _stateMachine = default;
                    if (Promise.Config.ObjectPooling != Promise.PoolType.None)
                    {
                        _pool.Push(this);
                    }
                }

                private void Continue()
                {
#if PROMISE_DEBUG
                    // Set current trace to the function's promise for execution, then set it back.
                    var current = CurrentTrace;
                    CurrentTrace = _owner.Trace;
                    try
                    {
                        _stateMachine.MoveNext();
                    }
                    finally
                    {
                        CurrentTrace = current;
                    }
#else
                    _stateMachine.MoveNext();
#endif
                }
            }
        }
    }
}

namespace Proto.Promises.Async.CompilerServices
{
    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
    [DebuggerNonUserCode]
    public struct PromiseMethodBuilder
    {
        [DebuggerNonUserCode]
        private sealed class AsyncPromise : Promise, Internal.ITreeHandleable
        {
            private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

            static AsyncPromise()
            {
                Internal.OnClearPool += () => _pool.Clear();
            }

            private Internal.PromiseMethodContinuer _continuer;

            public void SetResult()
            {
                ResolveDirect();
            }

            public void SetException(Exception exception)
            {
                if (exception is OperationCanceledException)
                {
                    CancelDirect(ref exception);
                }
                else
                {
                    if (exception is RethrowException)
                    {
#if PROMISE_DEBUG
                        string stacktrace = Internal.FormatStackTrace(new StackTrace[1] { new StackTrace(exception, true) });
#else
                        string stacktrace = new StackTrace(exception, true).ToString();
#endif
                        exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
                    }
                    RejectDirect(ref exception, int.MinValue);
                }
            }

            public static AsyncPromise GetOrCreate()
            {
                var promise = _pool.IsNotEmpty ? (AsyncPromise) _pool.Pop() : new AsyncPromise();
                promise.Reset();
                return promise;
            }

            public Action GetContinuation<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            {
                if (_continuer is null)
                {
                    _continuer = Internal.PromiseMethodContinuer.GetOrCreate(ref stateMachine, this);
                }
                return _continuer.Continuation;
            }

            void Internal.ITreeHandleable.Handle()
            {
                ReleaseInternal();
            }

            protected override void Dispose()
            {
                base.Dispose();
                if (_continuer != null)
                {
                    _continuer.Dispose();
                    _continuer = null;
                }
                if (Config.ObjectPooling != PoolType.None)
                {
                    _pool.Push(this);
                }
            }
        }

        private AsyncPromise _promise;

        [DebuggerHidden]
        public Promise Task { get { return _promise; } }

        [DebuggerHidden]
        public static PromiseMethodBuilder Create()
        {
            return new PromiseMethodBuilder()
            {
                _promise = AsyncPromise.GetOrCreate()
            };
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            _promise.SetException(exception);
        }

        [DebuggerHidden]
        public void SetResult()
        {
            _promise.SetResult();
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }

    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
    [DebuggerNonUserCode]
    public struct PromiseMethodBuilder<T>
    {
        [DebuggerNonUserCode]
        private sealed class AsyncPromise : Promise<T>, Internal.ITreeHandleable
        {
            private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

            static AsyncPromise()
            {
                Internal.OnClearPool += () => _pool.Clear();
            }

            private Internal.PromiseMethodContinuer _continuer;

            public void SetResult(ref T result)
            {
                ResolveDirect(ref result);
            }

            public void SetException(Exception exception)
            {
                if (exception is OperationCanceledException)
                {
                    CancelDirect(ref exception);
                }
                else
                {
                    if (exception is RethrowException)
                    {
#if PROMISE_DEBUG
                        string stacktrace = Internal.FormatStackTrace(new StackTrace[1] { new StackTrace(exception, true) });
#else
                        string stacktrace = new StackTrace(exception, true).ToString();
#endif
                        exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
                    }
                    RejectDirect(ref exception, int.MinValue);
                }
            }

            public static AsyncPromise GetOrCreate()
            {
                var promise = _pool.IsNotEmpty ? (AsyncPromise) _pool.Pop() : new AsyncPromise();
                promise.Reset();
                return promise;
            }

            public Action GetContinuation<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            {
                if (_continuer is null)
                {
                    _continuer = Internal.PromiseMethodContinuer.GetOrCreate(ref stateMachine, this);
                }
                return _continuer.Continuation;
            }

            void Internal.ITreeHandleable.Handle()
            {
                ReleaseInternal();
            }

            protected override void Dispose()
            {
                base.Dispose();
                if (_continuer != null)
                {
                    _continuer.Dispose();
                    _continuer = null;
                }
                if (Config.ObjectPooling != PoolType.None)
                {
                    _pool.Push(this);
                }
            }
        }

        private AsyncPromise _promise;

        [DebuggerHidden]
        public Promise<T> Task { get { return _promise; } }

        [DebuggerHidden]
        public static PromiseMethodBuilder<T> Create()
        {
            return new PromiseMethodBuilder<T>()
            {
                _promise = AsyncPromise.GetOrCreate()
            };
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            _promise.SetException(exception);
        }

        [DebuggerHidden]
        public void SetResult(T result)
        {
            _promise.SetResult(ref result);
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(_promise.GetContinuation(ref stateMachine));
        }

        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    }
}
#else
namespace Proto.Promises.Async.CompilerServices
{
    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
    [DebuggerNonUserCode]
    public struct PromiseMethodBuilder
    {
        // Using a promise object as its own continuer saves 16 bytes of object overhead (x64).
        [DebuggerNonUserCode]
        private abstract class AsyncPromise : Promise
        {
            // Cache the delegate to prevent new allocations.
            public readonly Action continuation;

            public AsyncPromise()
            {
                continuation = ContinueMethod;
            }

            protected abstract void ContinueMethod();

            public void SetResult()
            {
                ResolveDirect();
            }

            public void SetException(Exception exception)
            {
                if (exception is OperationCanceledException)
                {
                    CancelDirect(ref exception);
                }
                else
                {
                    if (exception is RethrowException)
                    {
                        string stacktrace = new StackTrace(exception, true).ToString();
                        exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
                    }
                    RejectDirect(ref exception, int.MinValue);
                }
            }
        }

        [DebuggerNonUserCode]
        private sealed class AsyncPromise<TStateMachine> : AsyncPromise, Internal.ITreeHandleable where TStateMachine : IAsyncStateMachine
        {
            private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

            static AsyncPromise()
            {
                Internal.OnClearPool += () => _pool.Clear();
            }

            public TStateMachine stateMachine;

            public static AsyncPromise<TStateMachine> GetOrCreate()
            {
                var promise = _pool.IsNotEmpty ? (AsyncPromise<TStateMachine>) _pool.Pop() : new AsyncPromise<TStateMachine>();
                promise.Reset();
                return promise;
            }

            void Internal.ITreeHandleable.Handle()
            {
                ReleaseInternal();
            }

            protected override void Dispose()
            {
                base.Dispose();
                stateMachine = default;
                if (Config.ObjectPooling != PoolType.None)
                {
                    _pool.Push(this);
                }
            }

            protected override void ContinueMethod()
            {
                stateMachine.MoveNext();
            }
        }

        public Promise Task { get; private set; }

        public static PromiseMethodBuilder Create()
        {
            return new PromiseMethodBuilder();
        }

        public void SetException(Exception exception)
        {
            if (Task is null)
            {
                if (exception is OperationCanceledException e)
                {
                    Task = Promise.Canceled(e);
                }
                else
                {
                    Task = Promise.Rejected(exception);
                }
            }
            else
            {
                ((AsyncPromise) Task).SetException(exception);
            }
        }

        public void SetResult()
        {
            if (Task is null)
            {
                Task = Promise.Resolved();
            }
            else
            {
                ((AsyncPromise) Task).SetResult();
            }
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(GetContinuation(ref stateMachine));
        }

        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(GetContinuation(ref stateMachine));
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        private Action GetContinuation<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (Task is null)
            {
                var promise = AsyncPromise<TStateMachine>.GetOrCreate();
                // ORDER VERY IMPORTANT, Task must be set before copying stateMachine.
                Task = promise;
                promise.stateMachine = stateMachine;
                return promise.continuation;
            }
            return ((AsyncPromise) Task).continuation;
        }
    }

    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
    [DebuggerNonUserCode]
    public struct PromiseMethodBuilder<T>
    {
        // Using a promise object as its own continuer saves 16 bytes of object overhead (x64).
        [DebuggerNonUserCode]
        private abstract class AsyncPromise : Promise<T>
        {
            // Cache the delegate to prevent new allocations.
            public readonly Action continuation;

            public AsyncPromise()
            {
                continuation = ContinueMethod;
            }

            protected abstract void ContinueMethod();

            public void SetResult(ref T result)
            {
                ResolveDirect(ref result);
            }

            public void SetException(Exception exception)
            {
                if (exception is OperationCanceledException)
                {
                    CancelDirect(ref exception);
                }
                else
                {
                    if (exception is RethrowException)
                    {
                        string stacktrace = new StackTrace(exception, true).ToString();
                        exception = new InvalidOperationException("RethrowException is only valid in promise onRejected callbacks.", stacktrace);
                    }
                    RejectDirect(ref exception, int.MinValue);
                }
            }
        }

        [DebuggerNonUserCode]
        private sealed class AsyncPromise<TStateMachine> : AsyncPromise, Internal.ITreeHandleable where TStateMachine : IAsyncStateMachine
        {
            private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

            static AsyncPromise()
            {
                Internal.OnClearPool += () => _pool.Clear();
            }

            public TStateMachine stateMachine;

            public static AsyncPromise<TStateMachine> GetOrCreate()
            {
                var promise = _pool.IsNotEmpty ? (AsyncPromise<TStateMachine>) _pool.Pop() : new AsyncPromise<TStateMachine>();
                promise.Reset();
                return promise;
            }

            void Internal.ITreeHandleable.Handle()
            {
                ReleaseInternal();
            }

            protected override void Dispose()
            {
                base.Dispose();
                stateMachine = default;
                if (Config.ObjectPooling != PoolType.None)
                {
                    _pool.Push(this);
                }
            }

            protected override void ContinueMethod()
            {
                stateMachine.MoveNext();
            }
        }

        public Promise<T> Task { get; set; }

        public static PromiseMethodBuilder<T> Create()
        {
            return new PromiseMethodBuilder<T>();
        }

        public void SetException(Exception exception)
        {
            if (Task is null)
            {
                if (exception is OperationCanceledException e)
                {
                    Task = Promise.Canceled<T, OperationCanceledException>(e);
                }
                else
                {
                    Task = Promise.Rejected<T, Exception>(exception);
                }
            }
            else
            {
                ((AsyncPromise) Task).SetException(exception);
            }
        }

        public void SetResult(T result)
        {
            if (Task is null)
            {
                Task = Promise.Resolved(result);
            }
            else
            {
                ((AsyncPromise) Task).SetResult(ref result);
            }
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(GetContinuation(ref stateMachine));
        }

        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(GetContinuation(ref stateMachine));
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        private Action GetContinuation<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (Task is null)
            {
                var promise = AsyncPromise<TStateMachine>.GetOrCreate();
                // ORDER VERY IMPORTANT, Task must be set before copying stateMachine.
                Task = promise;
                promise.stateMachine = stateMachine;
                return promise.continuation;
            }
            return ((AsyncPromise) Task).continuation;
        }
    }
}
#endif
#endif