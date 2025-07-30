// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Modified to work in Unity/netstandard2.0 without using the nuget package.
// Hooks up to Promise.Manager.ClearObjectPool() event instead of using Gen2GC callbacks.
#if UNITY_2018_3_OR_NEWER && !UNITY_2021_2_OR_NEWER

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Buffers
{
    /// <summary>
    /// Provides an ArrayPool implementation meant to be used as the singleton returned from ArrayPool.Shared.
    /// </summary>
    /// <remarks>
    /// The implementation uses a tiered caching scheme, with a small per-thread cache for each array size, followed
    /// by a cache per array size shared by all threads, split into per-core stacks meant to be used by threads
    /// running on that core.  Locks are used to protect each per-core stack, because a thread can migrate after
    /// checking its processor number, because multiple threads could interleave on the same core, and because
    /// a thread is allowed to check other core's buckets if its core's bucket is empty/full.
    /// </remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal sealed partial class SharedArrayPool<T> : ArrayPool<T>
    {
        /// <summary>The number of buckets (array sizes) in the pool, one for each array length, starting from length 16.</summary>
        private const int NumBuckets = 17; // Utilities.SelectBucketIndex(2*1024*1024)
        /// <summary>Maximum number of per-core stacks to use per array size.</summary>
        private const int MaxPerCorePerArraySizeStacks = 64; // selected to avoid needing to worry about processor groups
        /// <summary>The maximum number of buffers to store in a bucket's global queue.</summary>
        private const int MaxBuffersPerArraySizePerCore = 8;

        /// <summary>The length of arrays stored in the corresponding indices in <see cref="_buckets"/>.</summary>
        private readonly int[] _bucketArraySizes;
        /// <summary>
        /// An array of per-core array stacks. The slots are lazily initialized to avoid creating
        /// lots of overhead for unused array sizes.
        /// </summary>
        private readonly PerCoreLockedStacks[] _buckets = new PerCoreLockedStacks[NumBuckets];

        /// <summary>Initialize the pool.</summary>
        public SharedArrayPool()
        {
            var sizes = new int[NumBuckets];
            for (int i = 0; i < sizes.Length; i++)
            {
                sizes[i] = Utilities.GetMaxSizeForBucket(i);
            }
            _bucketArraySizes = sizes;

            // Hook up to Promise.Manager.ClearObjectPool() event.
            Proto.Promises.Internal.AddClearPoolListener(Trim);
        }

        /// <summary>Allocate a new PerCoreLockedStacks and try to store it into the <see cref="_buckets"/> array.</summary>
        private PerCoreLockedStacks CreatePerCoreLockedStacks(int bucketIndex)
        {
            var inst = new PerCoreLockedStacks();
            return Interlocked.CompareExchange(ref _buckets[bucketIndex], inst, null) ?? inst;
        }

        /// <summary>Gets an ID for the pool to use with events.</summary>
        private int Id => GetHashCode();

        public override T[] Rent(int minimumLength)
        {
            // Arrays can't be smaller than zero.  We allow requesting zero-length arrays (even though
            // pooling such an array isn't valuable) as it's a valid length array, and we want the pool
            // to be usable in general instead of using `new`, even for computed lengths.
            if (minimumLength < 0)
            {
                throw new ArgumentOutOfRangeException("minimumLength is less than 0", nameof(minimumLength));
            }
            else if (minimumLength == 0)
            {
                // No need to log the empty array.  Our pool is effectively infinite
                // and we'll never allocate for rents and never store for returns.
                return Array.Empty<T>();
            }

            T[] buffer;

            // Get the bucket number for the array length
            int bucketIndex = Utilities.SelectBucketIndex(minimumLength);

            // If the array could come from a bucket...
            if (bucketIndex < _buckets.Length)
            {
                PerCoreLockedStacks b = _buckets[bucketIndex];
                if (b != null)
                {
                    buffer = b.TryPop();
                    if (buffer != null)
                    {
                        return buffer;
                    }
                }

                // No buffer available.  Allocate a new buffer with a size corresponding to the appropriate bucket.
                buffer = new T[_bucketArraySizes[bucketIndex]];
            }
            else
            {
                // The request was for a size too large for the pool.  Allocate an array of exactly the requested length.
                // When it's returned to the pool, we'll simply throw it away.
                buffer = new T[minimumLength];
            }

            return buffer;
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array is null", nameof(array));
            }

            // Determine with what bucket this array length is associated
            int bucketIndex = Utilities.SelectBucketIndex(array.Length);

            // If we can tell that the buffer was allocated (or empty), drop it. Otherwise, check if we have space in the pool.
            if (bucketIndex < _buckets.Length)
            {
                // Clear the array if the user requests.
                if (clearArray)
                {
                    Array.Clear(array, 0, array.Length);
                }

                // Check to see if the buffer is the correct size for this bucket
                if (array.Length != _bucketArraySizes[bucketIndex])
                {
                    throw new ArgumentException("BufferNotFromPool", nameof(array));
                }

                PerCoreLockedStacks stackBucket = _buckets[bucketIndex] ?? CreatePerCoreLockedStacks(bucketIndex);
                stackBucket.TryPush(array);
            }
        }

        public void Trim()
        {
            int milliseconds = Environment.TickCount;

            PerCoreLockedStacks[] perCoreBuckets = _buckets;
            for (int i = 0; i < perCoreBuckets.Length; i++)
            {
                perCoreBuckets[i]?.Trim((uint) milliseconds, Id, _bucketArraySizes[i]);
            }
        }

        /// <summary>
        /// Stores a set of stacks of arrays, with one stack per core.
        /// </summary>
        private sealed class PerCoreLockedStacks
        {
            /// <summary>The stacks.</summary>
            private readonly LockedStack[] _perCoreStacks;

            /// <summary>Initializes the stacks.</summary>
            public PerCoreLockedStacks()
            {
                // Create the stacks.  We create as many as there are processors, limited by our max.
                var stacks = new LockedStack[Math.Min(Environment.ProcessorCount, MaxPerCorePerArraySizeStacks)];
                for (int i = 0; i < stacks.Length; i++)
                {
                    stacks[i] = new LockedStack();
                }
                _perCoreStacks = stacks;
            }

            /// <summary>Try to push the array into the stacks. If each is full when it's tested, the array will be dropped.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void TryPush(T[] array)
            {
                // Try to push on to the associated stack first.  If that fails,
                // round-robin through the other stacks.
                LockedStack[] stacks = _perCoreStacks;
                // Thread.GetCurrentProcessorId is not available in netstandard2.0, so use ManagedThreadId instead.
                int index = Thread.CurrentThread.ManagedThreadId % stacks.Length;
                for (int i = 0; i < stacks.Length; i++)
                {
                    if (stacks[index].TryPush(array)) return;
                    if (++index == stacks.Length) index = 0;
                }
            }

            /// <summary>Try to get an array from the stacks.  If each is empty when it's tested, null will be returned.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T[] TryPop()
            {
                // Try to pop from the associated stack first.  If that fails,
                // round-robin through the other stacks.
                T[] arr;
                LockedStack[] stacks = _perCoreStacks;
                // Thread.GetCurrentProcessorId is not available in netstandard2.0, so use ManagedThreadId instead.
                int index = Thread.CurrentThread.ManagedThreadId % stacks.Length;
                for (int i = 0; i < stacks.Length; i++)
                {
                    if ((arr = stacks[index].TryPop()) != null) return arr;
                    if (++index == stacks.Length) index = 0;
                }
                return null;
            }

            public void Trim(uint tickCount, int id, int bucketSize)
            {
                LockedStack[] stacks = _perCoreStacks;
                for (int i = 0; i < stacks.Length; i++)
                {
                    stacks[i].Trim(tickCount, id, bucketSize);
                }
            }
        }

        /// <summary>Provides a simple stack of arrays, protected by a lock.</summary>
        private sealed class LockedStack
        {
            private readonly T[][] _arrays = new T[MaxBuffersPerArraySizePerCore][];
            private int _count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryPush(T[] array)
            {
                bool enqueued = false;
                lock (this)
                {
                    if (_count < MaxBuffersPerArraySizePerCore)
                    {
                        _arrays[_count++] = array;
                        enqueued = true;
                    }
                }
                return enqueued;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T[] TryPop()
            {
                T[] arr = null;
                lock (this)
                {
                    if (_count > 0)
                    {
                        arr = _arrays[--_count];
                        _arrays[_count] = null;
                    }
                }
                return arr;
            }

            public void Trim(uint tickCount, int id, int bucketSize)
            {
                if (_count == 0)
                    return;

                lock (this)
                {
                    for (int i = 0; i < _count; ++i)
                    {
                        _arrays[i] = null;
                    }
                    _count = 0;
                }
            }
        }
    }
}

#endif