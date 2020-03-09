//#if CSHARP_7_OR_LATER

//#pragma warning disable RECS0108 // Warns about static fields in generic types

//using System;
//using System.Runtime.CompilerServices;
//using System.Threading.Tasks;
//using Proto.Promises.Await;

//namespace Proto.Promises.Await
//{
//    // Interfaces taken from Microsoft.Bot.Builder
//    public interface IAwaitable<out T>
//    {
//        IAwaiter<T> GetAwaiter();
//    }

//    public interface IAwaiter<out T> : INotifyCompletion
//    {
//        bool IsCompleted { get; }
//        T GetResult();
//    }

//    public interface IAwaitable
//    {
//        IAwaiter GetAwaiter();
//    }

//    public interface IAwaiter : INotifyCompletion
//    {
//        bool IsCompleted { get; }
//        void GetResult();
//    }

//    public static class Extensions
//    {
//        public static IAwaiter GetAwaiter(this Promise promise)
//        {
//            return ((IAwaitable) promise).GetAwaiter();
//        }

//        public static IAwaiter<T> GetAwaiter<T>(this Promise<T> promise)
//        {
//            return ((IAwaitable<T>) promise).GetAwaiter();
//        }

//        public static async Task ToTask<TAwaitable>(this TAwaitable awaitable) where TAwaitable : IAwaitable
//        {
//            await awaitable;
//        }

//        public static async Task<T> ToTask<T, TAwaitable>(this TAwaitable awaitable) where TAwaitable : IAwaitable<T>
//        {
//            return await awaitable;
//        }
//    }
//}

//namespace Proto.Promises
//{
//    partial class Promise : IAwaitable, IAwaiter
//    {
//        bool IAwaiter.IsCompleted
//        {
//            get
//            {
//                ValidateOperation(this, 1);

//                return _state != State.Pending;
//            }
//        }

//        IAwaiter IAwaitable.GetAwaiter()
//        {
//            ValidateOperation(this, 1);

//            RetainInternal();
//            return this;
//        }

//        void IAwaiter.GetResult()
//        {
//            ValidateOperation(this, 1);

//            // Check the most likely state first.
//            if (_state == State.Resolved)
//            {
//                ReleaseInternal();
//                return;
//            }
//            if (_state == State.Rejected)
//            {
//                // TODO: ReleaseInternal and throw exception
//            }
//            // TODO: ReleaseInternal and throw operationcanceledexception
//        }

//        void INotifyCompletion.OnCompleted(Action continuation)
//        {
//            ValidateOperation(this, 1);

//            Finally(continuation);
//        }

//        partial class Internal
//        {
//            partial class PromiseInternal<T> : IAwaiter<T>
//            {
//                bool IAwaiter<T>.IsCompleted
//                {
//                    get
//                    {
//                        ValidateOperation(this, 1);

//                        return _state != State.Pending;
//                    }
//                }

//                T IAwaiter<T>.GetResult()
//                {
//                    ValidateOperation(this, 1);

//                    // Check the most likely state first.
//                    if (_state == State.Resolved)
//                    {
//                        var temp = _value;
//                        ReleaseInternal();
//                        return temp;
//                    }
//                    if (_state == State.Rejected)
//                    {
//                        // TODO: ReleaseInternal and throw exception
//                    }
//                    // TODO: ReleaseInternal and throw operationcanceledexception
//                }
//            }
//        }
//    }

//    partial class Promise<T> : IAwaitable<T>
//    {
//        IAwaiter<T> IAwaitable<T>.GetAwaiter()
//        {
//            ValidateOperation(this, 1);

//            RetainInternal();
//            return (IAwaiter<T>) this;
//        }
//    }
//}
//#endif