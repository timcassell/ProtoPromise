#if CSHARP_7_OR_LATER

#pragma warning disable RECS0108 // Warns about static fields in generic types

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

    public static class Extensions
    {
        public static IAwaiter GetAwaiter(this Promise promise)
        {
            return ((IAwaitable) promise).GetAwaiter();
        }

        public static IAwaiter<T> GetAwaiter<T>(this Promise<T> promise)
        {
            return ((IAwaitable<T>) promise).GetAwaiter();
        }
    }
}

namespace Proto.Promises
{
    [AsyncMethodBuilder(typeof(Async.CompilerServices.PromiseMethodBuilder))]
    partial class Promise : IAwaitable, IAwaiter, ICriticalNotifyCompletion
    {
        IAwaiter IAwaitable.GetAwaiter()
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
        IAwaiter<T> IAwaitable<T>.GetAwaiter()
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
#endif