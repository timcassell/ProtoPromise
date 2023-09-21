// File named AssemblyAttributes.cs instead of AssemblyInfo.cs to avoid conflicts with an auto-generated file of the same name.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ProtoPromiseUnityHelpers")]
[assembly: InternalsVisibleTo("ProtoPromiseTests")]

namespace Proto.Promises
{
    partial class Internal
    {
        internal static readonly HashSet<string> FriendAssemblies = new HashSet<string>()
        {
            "ProtoPromiseUnityHelpers"
        };
    }
}