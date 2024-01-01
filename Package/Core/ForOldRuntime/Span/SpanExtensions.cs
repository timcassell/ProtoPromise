using System;
using System.Collections.Generic;

// TODO: Unity hasn't adopted .Net 5+ yet, and they usually use different compilation symbols than .Net SDK, so we'll have to update the compilation symbols here once Unity finally does adopt it.
namespace Proto.Promises
{
    // Span Sort extensions only exist in .Net 5+, so we pulled the ArraySortHelper code directly from the dotnet/runtime repository.
    // We could use Array.Sort, but depending on the runtime, it allocates on one overload or the other.
    // Since we're targeting netstandard which can be used in any runtime, we want to avoid any allocations,
    // so we use Span.Sort with generic comparers which are guaranteed to not allocate.
#if CSHARP_7_3_OR_NEWER && !NET5_0_OR_GREATER
    partial class Internal
    {
        public static void Sort<T, TComparer>(this Span<T> span, TComparer comparer) where TComparer : IComparer<T>
            => ArraySortHelper<T>.Sort(span, comparer);

        public static void Sort<T> (this Span<T> span)
            => ArraySortHelper<T>.Sort(span, Comparer<T>.Default);
    }
#endif // CSHARP_7_3_OR_NEWER && !NET5_0_OR_GREATER
}
