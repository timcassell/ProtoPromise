#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE1005 // Delegate invocation can be simplified.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        // If C#9 is available, we use function pointers instead of delegates to clear the pool.
#if NETCOREAPP || UNITY_2021_2_OR_NEWER
        // Raw pointers cannot be used as array types, so we have to cast to IntPtr.
        private static IntPtr[] s_clearPoolPointers = new IntPtr[64];
        private static int s_clearPoolCount;
        // Must not be readonly.
        private static SpinLocker s_clearPoolLock = new SpinLocker();

        internal static unsafe void ClearPool()
        {
            // We don't need to lock here.
            var temp = s_clearPoolPointers;
            for (int i = 0, max = s_clearPoolCount; i < max; ++i)
            {
                ((delegate*<void>) temp[i])();
            }
        }

        internal static unsafe void AddClearPoolListener(delegate*<void> action)
        {
            s_clearPoolLock.Enter();
            int count = s_clearPoolCount;
            if (count >= s_clearPoolPointers.Length)
            {
                Array.Resize(ref s_clearPoolPointers, count * 2);
            }
            s_clearPoolPointers[count] = (IntPtr) action;
            // Volatile write the new count so when ClearPool is called it doesn't need to lock.
            // This prevents the count write from being moved before the pointer write.
            System.Threading.Volatile.Write(ref s_clearPoolCount, count + 1);
            s_clearPoolLock.Exit();
        }
#else
        private static event Action OnClearPool;

        internal static void ClearPool()
        {
            Action temp = OnClearPool;
            if (temp != null)
            {
                temp.Invoke();
            }
        }

        internal static void AddClearPoolListener(Action action)
        {
            OnClearPool += action;
        }
#endif

        // Using static generic classes to hold the pools allows direct pool access at runtime without doing a dictionary lookup.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static partial class ObjectPool
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Type<T> where T : HandleablePromiseBase
            {
                // Using ValueLinkedStackSafe makes object pooling free.
                // No array allocations or linked list node allocations are necessary (the objects have links built-in through the HandleablePromiseBase._next property).
                // Even the pool itself doesn't require a class instance (that would be necessary with a typed dictionary).

                // This must not be readonly.
                private static ValueLinkedStackSafe s_pool = new ValueLinkedStackSafe(PromiseRefBase.InvalidAwaitSentinel.s_instance);

                // The downside to static pools instead of a Type dictionary is adding each type's clear function to the OnClearPool delegate consumes memory and is potentially more expensive than clearing a dictionary.
                // This cost could be removed if Promise.Config.ObjectPoolingEnabled is made constant and set to false, and we add a check before accessing the pool.
                // But as a general-purpose library, it makes more sense to leave that configurable at runtime.
#if NETCOREAPP || UNITY_2021_2_OR_NEWER
                static unsafe Type() { AddClearPoolListener(&Clear); }
#else
                static Type() { AddClearPoolListener(Clear); }
#endif

                private static void Clear()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
#pragma warning disable IDE0018 // Inline variable declaration
                    HandleablePromiseBase head;
#pragma warning restore IDE0018 // Inline variable declaration
                    var instances = s_pool.MoveElementsToStack(out head);
                    // The pooled stack ends with InvalidAwaitSentinel instead of null.
                    while (head != PromiseRefBase.InvalidAwaitSentinel.s_instance)
                    {
                        var instance = head;
                        head = instance._next;
                        instances.Pop();
                        MarkNotInPoolPrivate(instance);
                        Discard(instance);
                    }
#else
                    s_pool.ClearUnsafe();
#endif
                }

                [MethodImpl(InlineOption)]
                internal static HandleablePromiseBase TryTakeOrInvalid()
                {
                    return s_pool.PopOrInvalid();
                }

                [MethodImpl(InlineOption)]
                internal static void Repool(T obj)
                {
                    s_pool.Push(obj);
                }
            }

            [MethodImpl(InlineOption)]
            internal static HandleablePromiseBase TryTakeOrInvalid<T>() where T : HandleablePromiseBase
            {
                var obj = Type<T>.TryTakeOrInvalid();
#if PROTO_PROMISE_DEVELOPER_MODE
                if (s_trackObjectsForRelease & obj == PromiseRefBase.InvalidAwaitSentinel.s_instance)
                {
                    // Create here via reflection so that the object can be tracked.
                    obj = Activator.CreateInstance(typeof(T), true).UnsafeAs<HandleablePromiseBase>();
                }
#endif
                MarkNotInPoolPrivate(obj);
                return obj;
            }

            [MethodImpl(InlineOption)]
            internal static void MaybeRepool<T>(T obj) where T : HandleablePromiseBase
            {
                MarkInPoolPrivate(obj);
                if (Promise.Config.ObjectPoolingEnabled)
                {
                    Type<T>.Repool(obj);
                }
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                else if (obj is IFinalizable)
                {
                    // Finalizers are only used to validate that objects were used and released properly.
                    // If the object is being repooled, it means it was released properly. If pooling is disabled, we don't need the finalizer anymore.
                    // SuppressFinalize reduces pressure on the system when the GC runs.
                    SuppressAndUntrackFinalizable(obj.UnsafeAs<IFinalizable>());
                }
#endif
            }

            static partial void MarkInPoolPrivate(object obj);
            static partial void MarkNotInPoolPrivate(object obj);
#if PROTO_PROMISE_DEVELOPER_MODE
            static partial void MarkInPoolPrivate(object obj)
            {
                MarkInPool(obj);
            }

            static partial void MarkNotInPoolPrivate(object obj)
            {
                MarkNotInPool(obj);
            }
#endif
        } // class ObjectPool
    } // class Internal
} // namespace Proto.Promises