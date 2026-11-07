// TODO: Unity hasn't adopted .Net 6+ yet, and they usually use different compilation symbols than .Net SDK, so we'll have to update the compilation symbols here once Unity finally does adopt it.
#if !NET6_0_OR_GREATER

namespace System.Diagnostics
{
    // The runtime ignores this before .Net 6, but causality traces use it (by name) to hide frames (see CausalityTrace.IsStackTraceHidden).
    // It also makes it so we don't need to #if NET6_0_OR_GREATER at every place the attribute is used.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Struct, Inherited = false)]
    internal sealed class StackTraceHiddenAttribute : Attribute
    {

    }
}

#endif