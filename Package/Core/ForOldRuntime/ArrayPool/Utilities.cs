// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if CSHARP_7_3_OR_NEWER && !(NET47 || NETCOREAPP || NETSTANDARD2_0_OR_GREATER || UNITY_2021_2_OR_NEWER)
using System.Diagnostics;
using System.Runtime.CompilerServices;
#endif

using System;

namespace Proto.Promises.Collections
{
#if CSHARP_7_3_OR_NEWER && !(NET47 || NETCOREAPP || NETSTANDARD2_0_OR_GREATER || UNITY_2021_2_OR_NEWER)

    internal static class Utilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int SelectBucketIndex(int bufferSize)
        {
            Debug.Assert(bufferSize >= 0);
            uint bits = ((uint) bufferSize - 1) >> 4;
            return 32 - LeadingZeroCount(bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetMaxSizeForBucket(int binIndex)
        {
            int maxSize = 16 << binIndex;
            Debug.Assert(maxSize >= 0);
            return maxSize;
        }

        private static int LeadingZeroCount(uint value)
        {
            // Unguarded fallback contract is 0->31
            if (value == 0)
            {
                return 32;
            }

            return 31 - Log2SoftwareFallback(value);
        }

        private static int Log2SoftwareFallback(uint value)
        {
            // Just implemented with Math.Log since netstandard2.0 doesn't have Unsafe.AddByteOffset.
            return (int) Math.Log(value, 2);
        }
    }

#endif // CSHARP_7_3_OR_NEWER && !(NET47 || NETCOREAPP || NETSTANDARD2_0_OR_GREATER || UNITY_2021_2_OR_NEWER)
}