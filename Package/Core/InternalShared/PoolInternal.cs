#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable RECS0108 // Warns about static fields in generic types
#pragma warning disable IDE0031 // Use null propagation
#pragma warning disable IDE0054 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE1005 // Delegate invocation can be simplified.
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        // If C#9 is available, we use function pointers instead of delegates to clear the pool.
#if NETCOREAPP || UNITY_2021_2_OR_NEWER
        // Pointers cannot be used as array types, so we have to wrap it in a struct.
        private unsafe struct Ptr
        {
            internal delegate*<void> _ptr;
        }

        private static Ptr[] s_clearPoolPointers = new Ptr[64];
        private static int s_clearPoolCount;
        // Must not be readonly.
        private static SpinLocker s_clearPoolLock = new SpinLocker();

        internal static unsafe void ClearPool()
        {
            // We don't need to lock here.
            var temp = s_clearPoolPointers;
            for (int i = 0, max = s_clearPoolCount; i < max; ++i)
            {
                temp[i]._ptr();
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
            s_clearPoolPointers[count]._ptr = action;
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
                    s_pool.ClearUnsafe();
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
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
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
                else
                {
                    // Finalizers are only used to validate that objects were used and released properly.
                    // If the object is being repooled, it means it was released properly. If pooling is disabled, we don't need the finalizer anymore.
                    // SuppressFinalize reduces pressure on the system when the GC runs.
                    GC.SuppressFinalize(obj);
                }
            }

            static partial void MarkInPoolPrivate(object obj);
            static partial void MarkNotInPoolPrivate(object obj);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
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

        internal static void Discard(object waste)
        {
            GC.SuppressFinalize(waste);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            lock (s_pooledObjects)
            {
                s_inUseObjects.Remove(waste);
            }
#endif
        }

        static partial void ThrowIfInPool(object obj);
        static partial void MaybeThrowIfInPool(object obj, bool shouldCheck);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        internal static bool s_trackObjectsForRelease = false;
        private static readonly HashSet<object> s_pooledObjects = new HashSet<object>();
        private static readonly HashSet<object> s_inUseObjects = new HashSet<object>();

#if NETCOREAPP || UNITY_2021_2_OR_NEWER
        static unsafe Internal() { AddClearPoolListener(&ClearObjectTracking); }
#else
        static Internal() { AddClearPoolListener(ClearObjectTracking); }
#endif

        private static void ClearObjectTracking()
        {
            lock (s_pooledObjects)
            {
                s_pooledObjects.Clear();
            }
        }

        internal static void MarkInPool(object obj)
        {
            lock (s_pooledObjects)
            {
                if (Promise.Config.ObjectPoolingEnabled && !s_pooledObjects.Add(obj))
                {
                    throw new Exception("Same object was added to the pool twice: " + obj);
                }
                s_inUseObjects.Remove(obj);
            }
        }

        internal static void MarkNotInPool(object obj)
        {
            if (obj == null || obj == PromiseRefBase.InvalidAwaitSentinel.s_instance)
            {
                return;
            }
            lock (s_pooledObjects)
            {
                s_pooledObjects.Remove(obj);
                if (s_trackObjectsForRelease && !s_inUseObjects.Add(obj))
                {
                    throw new Exception("Same object was taken from the pool twice: " + obj);
                }
            }
        }

        static partial void ThrowIfInPool(object obj)
        {
            lock (s_pooledObjects)
            {
                if (s_pooledObjects.Contains(obj))
                {
                    throw new Exception("Object is in pool: " + obj);
                }
            }
        }

        static partial void MaybeThrowIfInPool(object obj, bool shouldCheck)
        {
            if (shouldCheck)
            {
                ThrowIfInPool(obj);
            }
        }

        // This is used in unit testing, because finalizers are not guaranteed to run, even when calling `GC.WaitForPendingFinalizers()`.
        internal static void TrackObjectsForRelease()
        {
            s_trackObjectsForRelease = true;
        }

        internal static void AssertAllObjectsReleased()
        {
            lock (s_pooledObjects)
            {
                if (s_inUseObjects.Count > 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine(s_inUseObjects.Count + " objects not released:");
                    sb.AppendLine();
                    ITraceable traceable = null;
                    int counter = 0;
                    foreach (var obj in s_inUseObjects)
                    {
                        // Only capture up to 100 objects to prevent overloading the test error output.
                        if (++counter <= 100)
                        {
                            traceable = traceable ?? obj as ITraceable;
                            sb.AppendLine(obj.ToString());
                        }
                        GC.SuppressFinalize(obj); // SuppressFinalize to not spoil the results of subsequent unit tests.
                    }
                    s_inUseObjects.Clear();
                    throw new UnreleasedObjectException(sb.ToString(), GetFormattedStacktrace(traceable));
                }
            }
        }
#endif
    } // class Internal
} // namespace Proto.Promises