// File named AssemblyAttributes.cs instead of AssemblyInfo.cs to avoid conflicts with an auto-generated file of the same name.

using System;
using System.Runtime.CompilerServices;

[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("ProtoPromise.UnityHelpers.2018.3")]
[assembly: InternalsVisibleTo("ProtoPromise.UnityHelpers.2023.1")]
// Old Unity IL2CPP fails tests with TestCaseSource if the assembly name has a . in it, so we remove the . from the test assembly name in Unity.
[assembly: InternalsVisibleTo("ProtoPromiseTests")]
[assembly: InternalsVisibleTo("ProtoPromise.Tests")]
[assembly: InternalsVisibleTo("ProtoPromise.Tests.UnityHelpers.2018.3")]
[assembly: InternalsVisibleTo("ProtoPromise.Tests.UnityHelpers.2023.1")]