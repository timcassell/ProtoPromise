#if CSHARP_7_OR_LATER

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

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
    public struct PromiseMethodBuilder
    {
        private Promise.Deferred _deferred;

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
            if (exception is OperationCanceledException ex)
            {
                if (_deferred == null)
                {
                    Task = Promise.Canceled(ex);
                }
                else
                {
                    _deferred.Cancel(ex);
                    _deferred = null;
                }
            }
            else
            {
                if (_deferred == null)
                {
                    Task = Promise.Rejected(exception);
                }
                else
                {
                    _deferred.Reject(exception);
                    _deferred = null;
                }
            }
        }

        [DebuggerHidden]
        public void SetResult()
        {
            if (_deferred == null)
            {
                Task = Promise.Resolved();
            }
            else
            {
                _deferred.Resolve();
                _deferred = null;
            }
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (_deferred == null)
            {
                _deferred = Promise.NewDeferred();
                Task = _deferred.Promise;
            }
            if (awaiter is Promise p)
            {
                p.Finally(stateMachine, sm => sm.MoveNext());
            }
            else
            {
                awaiter.OnCompleted(stateMachine.MoveNext);
            }
        }

        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (_deferred == null)
            {
                _deferred = Promise.NewDeferred();
                Task = _deferred.Promise;
            }
            if (awaiter is Promise p)
            {
                p.Finally(stateMachine, sm => sm.MoveNext());
            }
            else
            {
                awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
            }
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
    public struct PromiseMethodBuilder<T>
    {
        private Promise<T>.Deferred _deferred;

        [DebuggerHidden]
        public Promise<T> Task { get; private set; }

        [DebuggerHidden]
        public static PromiseMethodBuilder Create()
        {
            return new PromiseMethodBuilder();
        }

        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            if (exception is OperationCanceledException ex)
            {
                if (_deferred == null)
                {
                    Task = Promise.Canceled<T, OperationCanceledException>(ex);
                }
                else
                {
                    _deferred.Cancel(ex);
                    _deferred = null;
                }
            }
            else
            {
                if (_deferred == null)
                {
                    Task = Promise.Rejected<T, Exception>(exception);
                }
                else
                {
                    _deferred.Reject(exception);
                    _deferred = null;
                }
            }
        }

        [DebuggerHidden]
        public void SetResult(T result)
        {
            if (_deferred == null)
            {
                Task = Promise.Resolved(result);
            }
            else
            {
                _deferred.Resolve(result);
                _deferred = null;
            }
        }

        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (_deferred == null)
            {
                _deferred = Promise.NewDeferred<T>();
                Task = _deferred.Promise;
            }
            if (awaiter is Promise p)
            {
                p.Finally(stateMachine, sm => sm.MoveNext());
            }
            else
            {
                awaiter.OnCompleted(stateMachine.MoveNext);
            }
        }

        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (_deferred == null)
            {
                _deferred = Promise.NewDeferred<T>();
                Task = _deferred.Promise;
            }
            if (awaiter is Promise p)
            {
                p.Finally(stateMachine, sm => sm.MoveNext());
            }
            else
            {
                awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
            }
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
#endif