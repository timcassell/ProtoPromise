// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// TODO: Unity hasn't adopted .Net 5+ yet, and they usually use different compilation symbols than .Net SDK, so we'll have to update the compilation symbols here once Unity finally does adopt it.
#if !NET5_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// ArraySortHelpers used for Span.Sort extensions in older runtimes.
// This exists natively in .Net 5+.
// Using Proto.Promises namespace instead of the original System.Runtime.CompilerServices, and wrapped in Internal class.
namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class IntrospectiveSortUtilities
        {
            // This is the threshold where Introspective sort switches to Insertion sort.
            // Empirically, 16 seems to speed up most cases without slowing down others, at least for integers.
            // Large value types may benefit from a smaller number.
            internal const int IntrosortSizeThreshold = 16;

            internal static int FloorLog2PlusOne(int n)
            {
                int result = 0;
                while (n >= 1)
                {
                    result++;
                    n /= 2;
                }
                return result;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class ArraySortHelper<T>
        {
            public static void Sort<TComparer>(Span<T> keys, TComparer comparer) where TComparer : IComparer<T>
            {
                IntrospectiveSort(keys, comparer);
            }

            private static void SwapIfGreater<TComparer>(Span<T> keys, TComparer comparer, int i, int j) where TComparer : IComparer<T>
            {
                if (i != j)
                {
                    if (comparer.Compare(keys[i], keys[j]) > 0)
                    {
                        T key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void Swap(Span<T> a, int i, int j)
            {
                if (i != j)
                {
                    T t = a[i];
                    a[i] = a[j];
                    a[j] = t;
                }
            }

            internal static void IntrospectiveSort<TComparer>(Span<T> keys, TComparer comparer) where TComparer : IComparer<T>
            {
                Debug.Assert(comparer != null);

                if (keys.Length > 1)
                {
                    IntroSort(keys, 2 * IntrospectiveSortUtilities.FloorLog2PlusOne(keys.Length), comparer);
                }
            }

            private static void IntroSort<TComparer>(Span<T> keys, int depthLimit, TComparer comparer) where TComparer : IComparer<T>
            {
                int partitionSize = keys.Length;
                while (partitionSize > 1)
                {
                    if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                    {

                        if (partitionSize == 2)
                        {
                            SwapIfGreater(keys, comparer, 0, 1);
                            return;
                        }

                        if (partitionSize == 3)
                        {
                            SwapIfGreater(keys, comparer, 0, 1);
                            SwapIfGreater(keys, comparer, 0, 2);
                            SwapIfGreater(keys, comparer, 1, 2);
                            return;
                        }

                        InsertionSort(keys.Slice(0, partitionSize), comparer);
                        return;
                    }

                    if (depthLimit == 0)
                    {
                        HeapSort(keys.Slice(0, partitionSize), comparer);
                        return;
                    }
                    depthLimit--;

                    int p = PickPivotAndPartition(keys.Slice(0, partitionSize), comparer);

                    // Note we've already partitioned around the pivot and do not have to move the pivot again.
                    // `keys[(p + 1)..partitionSize]` de-sugared to work in C# 7.
                    int num = p + 1;
                    IntroSort(keys.Slice(num, partitionSize - num), depthLimit, comparer);
                    partitionSize = p;
                }
            }

            private static int PickPivotAndPartition<TComparer>(Span<T> keys, TComparer comparer) where TComparer : IComparer<T>
            {
                Debug.Assert(comparer != null);
                Debug.Assert(!keys.IsEmpty);

                int lo = 0;
                int hi = keys.Length - 1;

                // Compute median-of-three.  But also partition them, since we've done the comparison.
                int middle = lo + ((hi - lo) / 2);

                // Sort lo, mid and hi appropriately, then pick mid as the pivot.
                SwapIfGreater(keys, comparer, lo, middle);  // swap the low with the mid point
                SwapIfGreater(keys, comparer, lo, hi);   // swap the low with the high
                SwapIfGreater(keys, comparer, middle, hi); // swap the middle with the high

                T pivot = keys[middle];
                Swap(keys, middle, hi - 1);
                int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

                while (left < right)
                {
                    while (comparer.Compare(keys[++left], pivot) < 0) ;
                    while (comparer.Compare(pivot, keys[--right]) < 0) ;

                    if (left >= right)
                        break;

                    Swap(keys, left, right);
                }

                // Put pivot in the right location.
                Swap(keys, left, hi - 1);
                return left;
            }

            private static void HeapSort<TComparer>(Span<T> keys, TComparer comparer) where TComparer : IComparer<T>
            {
                Debug.Assert(comparer != null);
                Debug.Assert(!keys.IsEmpty);

                int lo = 0;
                int hi = keys.Length - 1;

                int n = hi - lo + 1;

                for (int i = n / 2; i >= 1; i--)
                {
                    DownHeap(keys, i, n, lo, comparer);
                }

                for (int i = n; i > 1; i--)
                {
                    Swap(keys, lo, lo + i - 1);
                    DownHeap(keys, 1, i - 1, lo, comparer);
                }
            }

            private static void DownHeap<TComparer>(Span<T> keys, int i, int n, int lo, TComparer comparer) where TComparer : IComparer<T>
            {
                Debug.Assert(comparer != null);
                Debug.Assert(lo >= 0);
                Debug.Assert(lo < keys.Length);

                T d = keys[lo + i - 1];
                while (i <= n / 2)
                {
                    int child = 2 * i;
                    if (child < n && comparer.Compare(keys[lo + child - 1], keys[lo + child]) < 0)
                    {
                        child++;
                    }

                    if (!(comparer.Compare(d, keys[lo + child - 1]) < 0))
                        break;

                    keys[lo + i - 1] = keys[lo + child - 1];
                    i = child;
                }

                keys[lo + i - 1] = d;
            }

            private static void InsertionSort<TComparer>(Span<T> keys, TComparer comparer) where TComparer : IComparer<T>
            {
                for (int i = 0; i < keys.Length - 1; i++)
                {
                    T t = keys[i + 1];

                    int j = i;
                    while (j >= 0 && comparer.Compare(t, keys[j]) < 0)
                    {
                        keys[j + 1] = keys[j];
                        j--;
                    }

                    keys[j + 1] = t;
                }
            }
        }
    }
}

#endif // !NET5_0_OR_GREATER