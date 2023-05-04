// TODO: Unity hasn't adopted .Net 6+ yet, and they usually use different compilation symbols than .Net SDK, so we'll have to update the compilation symbols here once Unity finally does adopt it.
#if !NET6_0_OR_GREATER

using System;

namespace Proto.Promises
{
    // This doesn't actually do anything before .Net 6, this is just so make it so we don't need to #if NET6_0_OR_GREATER at every place the attribute is used.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Struct, Inherited = false)]
    internal sealed class StackTraceHiddenAttribute : Attribute
    {

    }
}

#endif