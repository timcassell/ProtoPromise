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
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace Proto.Promises
{
    partial class Extensions
    {
        public static PromiseYieldInstruction ToYieldInstruction(this Promise promise)
        {
            return Internal.YieldInstructionVoid.GetOrCreate(promise);
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
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public abstract class PromiseYieldInstruction : CustomYieldInstruction, IDisposable
    {
        volatile protected object _rejectContainer;
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
                    ((Internal.IRejectContainer) _rejectContainer).GetExceptionDispatchInfo().Throw();
                    throw null; // This point will never be reached, but the C# compiler thinks it might.
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
    [DebuggerNonUserCode, StackTraceHidden]
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
            base.GetResult();
            return _result;
        }
    }

    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class YieldInstructionVoid : PromiseYieldInstruction, ILinked<YieldInstructionVoid>
        {
            // These must not be readonly.
            private static ValueLinkedStack<YieldInstructionVoid> s_pool;
            private static SpinLocker s_spinLocker;

            private int _disposeChecker; // To detect if Dispose is called from multiple threads.

            YieldInstructionVoid ILinked<YieldInstructionVoid>.Next { get; set; }

            static YieldInstructionVoid()
            {
                OnClearPool += () => s_pool = new ValueLinkedStack<YieldInstructionVoid>();
            }

            private YieldInstructionVoid() { }

            public static YieldInstructionVoid GetOrCreate(Promise promise)
            {
                YieldInstructionVoid yieldInstruction;
                s_spinLocker.Enter();
                yieldInstruction = s_pool.IsNotEmpty
                    ? s_pool.Pop()
                    : new YieldInstructionVoid();
                s_spinLocker.Exit();

                yieldInstruction._disposeChecker = 0;
                yieldInstruction._state = Promise.State.Pending;
                yieldInstruction._retainCounter = 2; // 1 retain for complete, 1 for dispose.
                promise
                    .ContinueWith(yieldInstruction, (yi, resultContainer) =>
                    {
                        var state = resultContainer.State;
                        if (state != Promise.State.Resolved)
                        {
                            yi._rejectContainer = resultContainer._target._rejectContainer;
                        }
                        yi._state = state;
                        yi.MaybeDispose();
                    })
                    .Forget();
                return yieldInstruction;
            }

            public override void Dispose()
            {
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. So use CompareExchange instead
                if (Interlocked.CompareExchange(ref _disposeChecker, 1, 0) == 1)
#else
                if (Interlocked.Exchange(ref _disposeChecker, 1) == 1)
#endif
                {
                    throw new InvalidOperationException("Promise yield instruction is not valid after you have disposed. You can get a valid yield instruction by calling promise.ToYieldInstruction().", GetFormattedStacktrace(1));
                }
                MaybeDispose();
            }

            private void MaybeDispose()
            {
                if (Interlocked.Decrement(ref _retainCounter) == 0)
                {
                    _rejectContainer = null;
#if !PROMISE_DEBUG // Don't repool in DEBUG mode.
                    if (Promise.Config.ObjectPoolingEnabled)
                    {
                        s_spinLocker.Enter();
                        s_pool.Push(this);
                        s_spinLocker.Exit();
                    }
#endif
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class YieldInstruction<T> : PromiseYieldInstruction<T>, ILinked<YieldInstruction<T>>
        {
            // These must not be readonly.
            private static ValueLinkedStack<YieldInstruction<T>> s_pool;
            private static SpinLocker s_spinLocker;

            private int _disposeChecker; // To detect if Dispose is called from multiple threads.

            YieldInstruction<T> ILinked<YieldInstruction<T>>.Next { get; set; }

            static YieldInstruction()
            {
                OnClearPool += () => s_pool = new ValueLinkedStack<YieldInstruction<T>>();
            }

            private YieldInstruction() { }

            public static YieldInstruction<T> GetOrCreate(Promise<T> promise)
            {
                YieldInstruction<T> yieldInstruction;
                s_spinLocker.Enter();
                yieldInstruction = s_pool.IsNotEmpty
                    ? s_pool.Pop()
                    : new YieldInstruction<T>();
                s_spinLocker.Exit();

                yieldInstruction._disposeChecker = 0;
                yieldInstruction._state = Promise.State.Pending;
                yieldInstruction._retainCounter = 2; // 1 retain for complete, 1 for dispose.
                promise
                    .ContinueWith(yieldInstruction, (yi, resultContainer) =>
                    {
                        var state = resultContainer.State;
                        if (state == Promise.State.Resolved)
                        {
                            yi._result = resultContainer.Result;
                        }
                        else
                        {
                            yi._rejectContainer = resultContainer._rejectContainer;
                        }
                        yi._state = state;
                        yi.MaybeDispose();
                    })
                    .Forget();
                return yieldInstruction;
            }

            public override void Dispose()
            {
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. So use CompareExchange instead
                if (Interlocked.CompareExchange(ref _disposeChecker, 1, 0) == 1)
#else
                if (Interlocked.Exchange(ref _disposeChecker, 1) == 1)
#endif
                {
                    throw new InvalidOperationException("Promise yield instruction is not valid after you have disposed. You can get a valid yield instruction by calling promise.ToYieldInstruction().", GetFormattedStacktrace(1));
                }
                MaybeDispose();
            }

            private void MaybeDispose()
            {
                if (Interlocked.Decrement(ref _retainCounter) == 0)
                {
                    _rejectContainer = null;
#if !PROMISE_DEBUG // Don't repool in DEBUG mode.
                    if (Promise.Config.ObjectPoolingEnabled)
                    {
                        s_spinLocker.Enter();
                        s_pool.Push(this);
                        s_spinLocker.Exit();
                    }
#endif
                }
            }
        }
    }
}