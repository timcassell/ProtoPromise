#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable RECS0108 // Warns about static fields in generic types

using Proto.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        internal static event Action OnClearPool;

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
            // TODO: This is the bottleneck across the entire library for multithreading, investigate using ConcurrentBag (not safe in Unity's WebGL).
            // It may be worthwhile to add InterlockedTryPop and InterlockedPush to ValueLinkedStack<T> instead of using a lock or ConcurrentBag.

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            private static class Type<T> where T : TLinked
            {
                // Using ValueLinkedStack<> makes object pooling free.
                // No array allocations or linked list node allocations are necessary (the objects have links built-in through the ILinked<> interface).
                // Even the pool itself doesn't require a class instance (that would be necessary with a typed dictionary).

                internal static ValueLinkedStack<TLinked> pool; // Must not be readonly.
                internal static readonly object locker = new object(); // Simple lock for now.

                // The downside to static pools instead of a Type dictionary is adding each type's clear function to the OnClearPool delegate consumes memory and is potentially more expensive than clearing a dictionary.
                // This cost could be removed if Promise.Config.ObjectPoolingEnabled is made constant and set to false, and we add a check before accessing the pool.
                // But as a general-purpose library, it makes more sense to leave that configurable at runtime.
                static Type()
                {
                    OnClearPool += () => pool.Clear();
                }
            }

            // `new` constraint uses reflection, too expensive. Delegate consumes memory and has indirection costs.
            // Generic constraint for the creator allows using a struct to make a new object, so no extra memory is consumed.
            // If the compiler/JIT is smart, it should be able to resolve the creator statically and inline it, so no indirection or function call costs.
            [MethodImpl(InlineOption)]
            internal static T GetOrCreate<T, TCreator>()
                where T : TLinked
                where TCreator : struct, ICreator<T>
            {
                TLinked obj;
                lock (Type<T>.locker)
                {
                    if (Type<T>.pool.IsEmpty)
                    {
                        goto CreateNew;
                    }
                    obj = Type<T>.pool.Pop();
                }
                // Exit lock before allocating or casting.
                RemoveFromTrackedObjects(obj);
                return (T) obj;
            CreateNew:
                return default(TCreator).Create();
            }

            [MethodImpl(InlineOption)]
            internal static void MaybeRepool<T>(T obj) where T : TLinked
            {
                if (Promise.Config.ObjectPoolingEnabled)
                {
                    AddToTrackedObjects(obj);
                    lock (Type<T>.locker)
                    {
                        Type<T>.pool.Push(obj);
                    }
                }
                // else TODO: GC.SuppressFinalize
            }

            static partial void AddToTrackedObjects(object obj);
            static partial void RemoveFromTrackedObjects(object obj);
#if PROMISE_DEBUG

            static partial void AddToTrackedObjects(object obj)
            {
                lock (_pooledObjects)
                {
                    if (!_pooledObjects.Add(obj))
                    {
                        throw new Exception("Same object was added to the pool twice");
                    }
                }
            }

            static partial void RemoveFromTrackedObjects(object obj)
            {
                lock (_pooledObjects)
                {
                    _pooledObjects.Remove(obj);
                }
            }
#endif
        }

        static partial void ThrowIfInPool(object obj);
#if PROMISE_DEBUG
        private static readonly HashSet<object> _pooledObjects = new HashSet<object>();

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
                    throw new Exception("Object is in pool.");
                }
            }
        }
#endif
    }
}

