#pragma warning disable RECS0108 // Warns about static fields in generic types

using Proto.Utils;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        // Using static generic classes to hold the pools allows direct pool access at runtime without doing a dictionary lookup.
        internal static class ObjectPool<TLinked> where TLinked : class, ILinked<TLinked>
        {
            private static class Type<T> where T : TLinked
            {
                // Using ValueLinkedStack<> makes object pooling free.
                // No array allocations or linked list node allocations are necessary (the objects have links built-in through the ILinked<> interface).
                // Even the pool itself doesn't require a class instance (that would be necessary with a typed dictionary).
                public static ValueLinkedStack<TLinked> pool; // Must not be readonly.
                // TODO
                //public static readonly object lockObject = new object(); // Simple lock for now.

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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T GetOrCreate<T, TCreator>(TCreator creator) where T : TLinked where TCreator : ICreator<T>
            {
                // TODO
                //Monitor.Enter(Type<T>.lockObject);
                if (Type<T>.pool.IsEmpty)
                {
                    //Monitor.Exit(Type<T>.lockObject);
                    return creator.Create();
                }
                var obj = Type<T>.pool.Pop();
                //Monitor.Exit(Type<T>.lockObject);
                return (T) obj;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void MaybeRepool<T>(T obj) where T : TLinked
            {
                if (Promise.Config.ObjectPoolingEnabled)
                {
                    // TODO
                    //lock (Type<T>.lockObject)
                    {
                        Type<T>.pool.Push(obj);
                    }
                }
            }
        }
    }
}
