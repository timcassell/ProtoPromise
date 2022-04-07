#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Threading;
using UnityEngine;

namespace Proto.Promises
{
    partial class Extensions
    {
        public static PromiseYieldInstruction ToYieldInstruction(this Promise promise)
        {
            return ToYieldInstruction(promise._target);
        }

        public static PromiseYieldInstruction<T> ToYieldInstruction<T>(this Promise<T> promise)
        {
            return Internal.YieldInstruction<T>.GetOrCreate(promise);
        }
    }


    /// <summary>
    /// Yield instruction that can be yielded in a coroutine to wait until the <see cref="Promise"/> it came from has settled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public abstract class PromiseYieldInstruction : CustomYieldInstruction, IDisposable
    {
        volatile protected object _value;
        volatile protected Promise.State _state;
        volatile protected int _retainCounter;


        internal PromiseYieldInstruction() { }

        /// <summary>
        /// The state of the <see cref="Promise"/> this came from.
        /// </summary>
        /// <value>The state.</value>
        public Promise.State State
        {
            get
            {
                ValidateOperation();
                return _state;
            }
        }

        /// <summary>
        /// Is the Promise still pending?
        /// </summary>
        public override bool keepWaiting
        {
            get
            {
                ValidateOperation();
                return State == Promise.State.Pending;
            }
        }

        /// <summary>
        /// Get the result. If the Promise resolved successfully, this will return without error.
        /// If the Promise was rejected, this will throw an <see cref="UnhandledException"/>.
        /// If the Promise was canceled, this will throw a <see cref="CanceledException"/>.
        /// </summary>
        public void GetResult()
        {
            ValidateOperation();

            switch (_state)
            {
                case Promise.State.Resolved:
                {
                    return;
                }
                case Promise.State.Canceled:
                {
                    throw Internal.CanceledExceptionInternal.GetOrCreate();
                }
                case Promise.State.Rejected:
                {
#if !NET_LEGACY
                    ((Internal.IRejectValueContainer) _value).GetExceptionDispatchInfo().Throw();
                    throw new Exception(); // This point will never be reached, but the C# compiler thinks it might.
#else
                    throw ((Internal.IRejectValueContainer) _value).GetException();
#endif
                }
                default:
                {
                    throw new InvalidOperationException("Promise is still pending. You must wait for the promse to settle before calling GetResult.", Internal.GetFormattedStacktrace(1));
                }
            }
        }

        /// <summary>
        /// Adds this object back to the pool if object pooling is enabled.
        /// Don't try to access it after disposing! Results are undefined.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the
        /// <see cref="T:ProtoPromise.Promise.YieldInstruction"/>. The <see cref="Dispose"/> method leaves the
        /// <see cref="T:ProtoPromise.Promise.YieldInstruction"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the
        /// <see cref="T:ProtoPromise.Promise.YieldInstruction"/> so the garbage collector can reclaim the memory
        /// that the <see cref="T:ProtoPromise.Promise.YieldInstruction"/> was occupying.</remarks>
        public virtual void Dispose()
        {
            ValidateOperation();
        }

        protected void ValidateOperation()
        {
            if (_retainCounter == 0)
            {
                throw new InvalidOperationException("Promise yield instruction is not valid after you have disposed. You can get a valid yield instruction by calling promise.ToYieldInstruction().", Internal.GetFormattedStacktrace(1));
            }
        }
    }

    /// <summary>
    /// Yield instruction that can be yielded in a coroutine to wait until the <see cref="Promise{T}"/> it came from has settled.
    /// An instance of this should be disposed when you are finished with it.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public abstract class PromiseYieldInstruction<T> : PromiseYieldInstruction
    {
        protected T _result;

        internal PromiseYieldInstruction() { }

        /// <summary>
        /// Get the result. If the Promise resolved successfully, this will return the result of the operation.
        /// If the Promise was rejected, this will throw an <see cref="UnhandledException"/>.
        /// If the Promise was canceled, this will throw a <see cref="CanceledException"/>.
        /// </summary>
        public new T GetResult()
        {
            ValidateOperation();

            switch (_state)
            {
                case Promise.State.Resolved:
                {
                    return _result;
                }
                case Promise.State.Canceled:
                {
                    throw Internal.CanceledExceptionInternal.GetOrCreate();
                }
                case Promise.State.Rejected:
                {
#if !NET_LEGACY
                    ((Internal.IRejectValueContainer) _value).GetExceptionDispatchInfo().Throw();
                    throw new Exception(); // This point will never be reached, but the C# compiler thinks it might.
#else
                    throw ((Internal.IRejectValueContainer) _value).GetException();
#endif
                }
                default:
                {
                    throw new InvalidOperationException("Promise is still pending. You must wait for the promse to settle before calling GetResult.", Internal.GetFormattedStacktrace(1));
                }
            }
        }
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal sealed class YieldInstruction<T> : PromiseYieldInstruction<T>, ILinked<YieldInstruction<T>>
        {
            private int _disposeChecker; // To detect if Dispose is called from multiple threads.

            YieldInstruction<T> ILinked<YieldInstruction<T>>.Next { get; set; }

            private YieldInstruction() { }

            public static YieldInstruction<T> GetOrCreate(Promise<T> promise)
            {
                var yieldInstruction = ObjectPool<YieldInstruction<T>>.TryTake<YieldInstruction<T>>()
                    ?? new YieldInstruction<T>();
                yieldInstruction._disposeChecker = 0;
                yieldInstruction._state = Promise.State.Pending;
                yieldInstruction._retainCounter = 2; // 1 retain for complete, 1 for dispose.
                promise.ContinueWith(yieldInstruction, (yi, resultContainer) =>
                {
                    yi._state = resultContainer.State;
                    if (yi._state == Promise.State.Resolved)
                    {
                        yi._result = resultContainer.Result;
                    }
                    else
                    {
                        yi._value = ((ValueContainer) resultContainer._target._valueOrPrevious).Clone();
                    }
                    
                    yi.MaybeDispose();
                })
                    .Forget();
                return yieldInstruction;
            }

            public override void Dispose()
            {
                if (Interlocked.Exchange(ref _disposeChecker, 1) == 1)
                {
                    throw new InvalidOperationException("Promise yield instruction is not valid after you have disposed. You can get a valid yield instruction by calling promise.ToYieldInstruction().", GetFormattedStacktrace(1));
                }
                MaybeDispose();
            }

            private void MaybeDispose()
            {
                if (Interlocked.Decrement(ref _retainCounter) == 0)
                {
                    var container = _value;
                    _value = null;
                    if (container != null)
                    {
                        ((IRetainable) container).Release();
                    }
#if !PROMISE_DEBUG // Don't repool in DEBUG mode.
                    ObjectPool<YieldInstruction<T>>.MaybeRepool(this);
#endif
                }
            }
        }
    }
}