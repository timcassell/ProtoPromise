#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Proto.Promises
{
    /// <summary>
    /// Extensions to convert Promises to Yield Instructions for Coroutines.
    /// </summary>
    public static class UnityHelperExtensions
    {
        /// <summary>
        /// Convert the <paramref name="promise"/> to a <see cref="PromiseYieldInstruction"/>.
        /// </summary>
        public static PromiseYieldInstruction ToYieldInstruction(this Promise promise)
        {
            return InternalHelper.YieldInstructionVoid.GetOrCreate(promise);
        }

        /// <summary>
        /// Convert the <paramref name="promise"/> to a <see cref="PromiseYieldInstruction{T}"/>.
        /// </summary>
        public static PromiseYieldInstruction<T> ToYieldInstruction<T>(this Promise<T> promise)
        {
            return InternalHelper.YieldInstruction<T>.GetOrCreate(promise);
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
        /// If the Promise was rejected or canceled, this will throw the appropriate exception.
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
                    throw new InvalidOperationException("Promise is still pending. You must wait for the promise to settle before calling GetResult.", Internal.GetFormattedStacktrace(1));
                }
            }
        }

        /// <summary>
        /// Adds this object back to the pool if object pooling is enabled.
        /// Don't try to access it after disposing! Results are undefined.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the
        /// <see cref="YieldInstruction"/>. The <see cref="Dispose"/> method leaves the
        /// <see cref="YieldInstruction"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the
        /// <see cref="YieldInstruction"/> so the garbage collector can reclaim the memory
        /// that the <see cref="YieldInstruction"/> was occupying.</remarks>
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
        /// If the Promise was rejected or canceled, this will throw the appropriate exception.
        /// </summary>
        public new T GetResult()
        {
            base.GetResult();
            return _result;
        }
    }

    partial class InternalHelper
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class YieldInstructionVoid : PromiseYieldInstruction, Internal.ILinked<YieldInstructionVoid>
        {
            private int _disposeChecker; // To detect if Dispose is called from multiple threads.

            YieldInstructionVoid Internal.ILinked<YieldInstructionVoid>.Next { get; set; }

            private YieldInstructionVoid() { }

            public static YieldInstructionVoid GetOrCreate(Promise promise)
            {
                var yieldInstruction = LinkedPool.TryTakeOrNull<YieldInstructionVoid>()
                    ?? new YieldInstructionVoid();

                yieldInstruction._disposeChecker = 0;
                yieldInstruction._state = Promise.State.Pending;
                yieldInstruction._retainCounter = 2; // 1 retain for complete, 1 for dispose.
                promise
                    .ContinueWith(yieldInstruction, (yi, resultContainer) =>
                    {
                        yi._rejectContainer = resultContainer._target._rejectContainer;
                        yi._state = resultContainer.State;
                        yi.MaybeDispose();
                    })
                    .Forget();
                return yieldInstruction;
            }

            public override void Dispose()
            {
                if (Interlocked.Exchange(ref _disposeChecker, 1) == 1)
                {
                    throw new InvalidOperationException("Promise yield instruction is not valid after you have disposed. You can get a valid yield instruction by calling promise.ToYieldInstruction().", Internal.GetFormattedStacktrace(1));
                }
                MaybeDispose();
            }

            private void MaybeDispose()
            {
                if (Interlocked.Decrement(ref _retainCounter) == 0)
                {
                    _rejectContainer = null;
                    LinkedPool.MaybeRepool(this);
                }
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class YieldInstruction<T> : PromiseYieldInstruction<T>, Internal.ILinked<YieldInstruction<T>>
        {
            private int _disposeChecker; // To detect if Dispose is called from multiple threads.

            YieldInstruction<T> Internal.ILinked<YieldInstruction<T>>.Next { get; set; }

            private YieldInstruction() { }

            public static YieldInstruction<T> GetOrCreate(Promise<T> promise)
            {
                var yieldInstruction = LinkedPool.TryTakeOrNull<YieldInstruction<T>>()
                    ?? new YieldInstruction<T>();

                yieldInstruction._disposeChecker = 0;
                yieldInstruction._state = Promise.State.Pending;
                yieldInstruction._retainCounter = 2; // 1 retain for complete, 1 for dispose.
                promise
                    .ContinueWith(yieldInstruction, (yi, resultContainer) =>
                    {
                        yi._result = resultContainer.Value;
                        yi._rejectContainer = resultContainer._rejectContainer;
                        yi._state = resultContainer.State;
                        yi.MaybeDispose();
                    })
                    .Forget();
                return yieldInstruction;
            }

            public override void Dispose()
            {
                if (Interlocked.Exchange(ref _disposeChecker, 1) == 1)
                {
                    throw new InvalidOperationException("Promise yield instruction is not valid after you have disposed. You can get a valid yield instruction by calling promise.ToYieldInstruction().", Internal.GetFormattedStacktrace(1));
                }
                MaybeDispose();
            }

            private void MaybeDispose()
            {
                if (Interlocked.Decrement(ref _retainCounter) == 0)
                {
                    _rejectContainer = null;
                    LinkedPool.MaybeRepool(this);
                }
            }
        } // class YieldInstruction<T>

        // Implemented a separate pool from Internal.ObjectPool, because we use ILinked<T> instead of HandleablePromiseBase.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private static partial class LinkedPool
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Type<T> where T : class, Internal.ILinked<T>
            {
                private static volatile T s_head;

#if UNITY_2021_2_OR_NEWER
                static unsafe Type() { Internal.AddClearPoolListener(&Clear); }
#else
                static Type() { Internal.AddClearPoolListener(Clear); }
#endif

                private static void Clear()
                {
                    s_head = null;
                }

                internal static T TryTakeOrNull()
                {
                    while (true)
                    {
                        T item = s_head;
                        if (item == null)
                        {
                            return null;
                        }
                        T next = item.Next;
                        if (Interlocked.CompareExchange(ref s_head, next, item) == item)
                        {
                            item.Next = null;
                            return item;
                        }
                    }
                }

                internal static void Repool(T item)
                {
                    T next;
                    do
                    {
                        next = s_head;
                        item.Next = next;
                    } while (Interlocked.CompareExchange(ref s_head, item, next) != next);
                }
            }

            // We return null instead of using the `new` constraint, because it uses reflection.
            [MethodImpl(Internal.InlineOption)]
            internal static T TryTakeOrNull<T>() where T : class, Internal.ILinked<T>
            {
                var obj = Type<T>.TryTakeOrNull();
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                if (Internal.s_trackObjectsForRelease & obj == null)
                {
                    // Create here via reflection so that the object can be tracked.
                    obj = Activator.CreateInstance(typeof(T), true).UnsafeAs<T>();
                }
#endif
                MarkNotInPool(obj);
                return obj;
            }

            [MethodImpl(Internal.InlineOption)]
            internal static void MaybeRepool<T>(T obj) where T : class, Internal.ILinked<T>
            {
                MarkInPool(obj);
                if (Promise.Config.ObjectPoolingEnabled)
                {
                    Type<T>.Repool(obj);
                }
                else
                {
                    // Finalizers are only used to validate that objects were used and released properly.
                    // If the object is being repooled, it means it was released properly. If pooling is disabled, we don't need the finalizer anymore.
                    // SuppressFinalize reduces pressure on the system when the GC runs.
                    GC.SuppressFinalize(obj);
                }
            }

            static partial void MarkInPool(object obj);
            static partial void MarkNotInPool(object obj);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            static partial void MarkInPool(object obj)
            {
                Internal.MarkInPool(obj);
            }

            static partial void MarkNotInPool(object obj)
            {
                Internal.MarkNotInPool(obj);
            }
#endif
        }
    } // class InternalHelper
} // namespace Proto.Promises