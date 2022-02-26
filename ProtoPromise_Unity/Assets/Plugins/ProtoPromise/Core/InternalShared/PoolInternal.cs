#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable RECS0108 // Warns about static fields in generic types

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ProtoPromiseTests", AllInternalsVisible = true)]

namespace Proto.Promises
{
    partial class Internal
    {
        private static event Action OnClearPool;

        internal static void ClearPool()
        {
            Action temp = OnClearPool;
            if (temp != null)
            {
                temp.Invoke();
            }
        }

        // Using static generic classes to hold the pools allows direct pool access at runtime without doing a dictionary lookup.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        internal static partial class ObjectPool<TLinked> where TLinked : class, ILinked<TLinked>
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private static class Type<T> where T : class, TLinked
            {
                // Using ValueLinkedStackSafe<> makes object pooling free.
                // No array allocations or linked list node allocations are necessary (the objects have links built-in through the ILinked<> interface).
                // Even the pool itself doesn't require a class instance (that would be necessary with a typed dictionary).

                // These must not be readonly.
                private static ValueLinkedStackSafe<TLinked> _pool = new ValueLinkedStackSafe<TLinked>();
                private static SpinLocker _locker;

                // The downside to static pools instead of a Type dictionary is adding each type's clear function to the OnClearPool delegate consumes memory and is potentially more expensive than clearing a dictionary.
                // This cost could be removed if Promise.Config.ObjectPoolingEnabled is made constant and set to false, and we add a check before accessing the pool.
                // But as a general-purpose library, it makes more sense to leave that configurable at runtime.
                static Type()
                {
                    OnClearPool += Clear;
                }

                private static void Clear()
                {
                    _pool.ClearUnsafe();
                }

                [MethodImpl(InlineOption)]
                internal static TLinked TryTake()
                {
                    return _pool.TryPop(ref _locker);
                }

                [MethodImpl(InlineOption)]
                internal static void Repool(TLinked obj)
                {
                    _pool.Push(obj, ref _locker);
                }
            }

            [MethodImpl(InlineOption)]
            internal static T TryTake<T>() where T : class, TLinked
            {
                TLinked obj = Type<T>.TryTake();
#if PROMISE_DEBUG
                if (_trackObjectsForRelease & obj == null)
                {
                    // Create here via reflection so that the object can be tracked.
                    obj = (TLinked) Activator.CreateInstance(typeof(T), true);
                }
#endif
                MarkNotInPool(obj);
                return (T) obj;
            }

            [MethodImpl(InlineOption)]
            internal static void MaybeRepool<T>(T obj) where T : class, TLinked
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
#if PROMISE_DEBUG
            static partial void MarkInPool(object obj)
            {
                lock (_pooledObjects)
                {
                    if (Promise.Config.ObjectPoolingEnabled && !_pooledObjects.Add(obj))
                    {
                        throw new Exception("Same object was added to the pool twice: " + obj);
                    }
                    _inUseObjects.Remove(obj);
                }
            }

            static partial void MarkNotInPool(object obj)
            {
                lock (_pooledObjects)
                {
                    _pooledObjects.Remove(obj);
                    if (_trackObjectsForRelease && !_inUseObjects.Add(obj))
                    {
                        throw new Exception("Same object was taken from the pool twice: " + obj);
                    }
                }
            }
#endif
        }

        static partial void ThrowIfInPool(object obj);
#if PROMISE_DEBUG
        private static bool _trackObjectsForRelease = false;
        private static readonly HashSet<object> _pooledObjects = new HashSet<object>();
        private static readonly HashSet<object> _inUseObjects = new HashSet<object>();

        static Internal()
        {
            OnClearPool += () =>
            {
                lock (_pooledObjects)
                {
                    _pooledObjects.Clear();
                }
            };
        }

        static partial void ThrowIfInPool(object obj)
        {
            lock (_pooledObjects)
            {
                if (_pooledObjects.Contains(obj))
                {
                    throw new Exception("Object is in pool: " + obj);
                }
            }
        }

        // This is used in unit testing, because finalizers are not guaranteed to run, even when calling `GC.WaitForPendingFinalizers()`.
        internal static void TrackObjectsForRelease()
        {
            _trackObjectsForRelease = true;
        }

        internal static void AssertAllObjectsReleased()
        {
            lock (_pooledObjects)
            {
                if (_inUseObjects.Count > 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("Objects not released:");
                    sb.AppendLine();
                    ITraceable traceable = null;
                    foreach (var obj in _inUseObjects)
                    {
                        traceable = traceable ?? obj as ITraceable;
                        sb.AppendLine(obj.ToString());
                        GC.SuppressFinalize(obj); // SuppressFinalize to not spoil the results of subsequent unit tests.
                    }
                    _inUseObjects.Clear();
                    throw new UnreleasedObjectException(sb.ToString(), GetFormattedStacktrace(traceable));
                }
            }
        }
#endif
    }
}