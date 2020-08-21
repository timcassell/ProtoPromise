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

namespace Proto.Promises.Async.CompilerServices
{
    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
    [DebuggerNonUserCode]
    public struct PromiseMethodBuilder
    {
        private Promise.Deferred _deferred;
        private Action _continuation;

        [DebuggerHidden]
        public Promise Task { get; private set; }

        [DebuggerHidden]
        public static PromiseMethodBuilder Create()
        {
            return new PromiseMethodBuilder();
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                if (!_deferred.IsValid)
                {
                    Task = Promise.Canceled(exception);
                }
                else
                {
                    ((Internal.ICancelDelegate) _deferred.Promise).Invoke(Internal.CreateCancelContainer(ref exception));
                }
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
                if (!_deferred.IsValid)
                {
                    Task = Promise.Rejected(exception);
                }
                else
                {
                    _deferred.Reject(exception);
                }
            }
            _deferred = default;
            _continuation = null;
        }

        [DebuggerHidden]
        public void SetResult()
        {
            if (!_deferred.IsValid)
            {
                Task = Promise.Resolved();
            }
            else
            {
                _deferred.Resolve();
                _deferred = default;
                _continuation = null;
            }
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            SetContinuation(ref stateMachine);
            awaiter.OnCompleted(_continuation);
        }

        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            SetContinuation(ref stateMachine);
            awaiter.UnsafeOnCompleted(_continuation);
        }

        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        [DebuggerHidden]
        private void SetContinuation<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (_continuation is null)
            {
                _deferred = Promise.Deferred.New();
                Task = _deferred.Promise;
                _continuation = stateMachine.MoveNext;
            }
        }
    }

    /// <summary>
    /// This type and its members are intended for use by the compiler.
    /// </summary>
    [DebuggerNonUserCode]
    public struct PromiseMethodBuilder<T>
    {
        private Promise<T>.Deferred _deferred;
        private Action _continuation;

        [DebuggerHidden]
        public Promise<T> Task { get; private set; }

        [DebuggerHidden]
        public static PromiseMethodBuilder<T> Create()
        {
            return new PromiseMethodBuilder<T>();
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                if (!_deferred.IsValid)
                {
                    Task = Promise.Canceled<T, Exception>(exception);
                }
                else
                {
                    ((Internal.ICancelDelegate) _deferred.Promise).Invoke(Internal.CreateCancelContainer(ref exception));
                }
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
                if (!_deferred.IsValid)
                {
                    Task = Promise.Rejected<T, Exception>(exception);
                }
                else
                {
                    _deferred.Reject(exception);
                }
            }
            _deferred = default;
            _continuation = null;
        }

        [DebuggerHidden]
        public void SetResult(T result)
        {
            if (!_deferred.IsValid)
            {
                Task = Promise.Resolved(result);
            }
            else
            {
                _deferred.Resolve(result);
                _deferred = default;
                _continuation = null;
            }
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            SetContinuation(ref stateMachine);
            awaiter.OnCompleted(_continuation);
        }

        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            SetContinuation(ref stateMachine);
            awaiter.UnsafeOnCompleted(_continuation);
        }

        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }

        [DebuggerHidden]
        private void SetContinuation<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (_continuation is null)
            {
                _deferred = Promise<T>.Deferred.New();
                Task = _deferred.Promise;
                _continuation = stateMachine.MoveNext;
            }
        }
    }
}
#endif