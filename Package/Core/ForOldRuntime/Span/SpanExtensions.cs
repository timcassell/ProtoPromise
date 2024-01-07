using System;
using System.Collections.Generic;

namespace Proto.Promises
{
#if CSHARP_7_3_OR_NEWER
    partial class Internal
    {
        // TODO: Unity hasn't adopted .Net 5+ yet, and they usually use different compilation symbols than .Net SDK, so we'll have to update the compilation symbols here once Unity finally does adopt it.
#if !NET5_0_OR_GREATER
        // Span Sort extensions only exist in .Net 5+, so we pulled the ArraySortHelper code directly from the dotnet/runtime repository.
        // We could use Array.Sort, but depending on the runtime, it allocates on one overload or the other.
        // Since we're targeting netstandard which can be used in any runtime, we want to avoid any allocations,
        // so we use Span.Sort with generic comparers which are guaranteed to not allocate.
        public static void Sort<T, TComparer>(this Span<T> span, TComparer comparer) where TComparer : IComparer<T>
            => ArraySortHelper<T>.Sort(span, comparer);

        public static void Sort<T>(this Span<T> span)
            => ArraySortHelper<T>.Sort(span, Comparer<T>.Default);
#endif // !NET5_0_OR_GREATER

#if UNITY_5_5_OR_NEWER && !UNITY_2021_2_OR_NEWER
        // AsSpan extension exists in netstandard2.1 and in the Span nuget package. We only add this in Unity where we're not using nuget packages.
        public static Span<T> AsSpan<T>(this T[] array, int start)
            => new Span<T>(array, start, array.Length);
#endif
    }
#endif // CSHARP_7_3_OR_NEWER
}