using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proto.Promises.Analyzer;
using System.IO;
using System.Threading.Tasks;
using VerifyCS = ProtoPromise.Analyzer.Test.CSharpAnalyzerVerifier<Proto.Promises.Analyzer.YieldAsyncAnalyzer>;

namespace ProtoPromise.Analyzer.Tests
{
    [TestClass]
    public class ProtoPromiseAnalyzerTests
    {
        [TestMethod]
        public async Task NoDiagnostics()
        {
            const string test = @"
using Proto.Promises.Linq;

namespace ConsoleApplication1
{
    internal class MyClass
    {
        public AsyncEnumerable<int> IterateAsync()
        {
            return AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                try
                {
                    await writer.YieldAsync(42);
                }
                finally { }
            });
        }
    }
}";

            await new CSharpAnalyzerTest<YieldAsyncAnalyzer, MSTestVerifier>()
            {
                ReferenceAssemblies = new ReferenceAssemblies("net6.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "6.0.0"), Path.Combine("ref", "net6.0")),
                TestState =
                {
                    Sources = { test },
                    AdditionalReferences = { typeof(Proto.Promises.Promise).Assembly.Location }
                }
            }.RunAsync();
        }

        [TestMethod]
        public async Task CannotAwaitYieldInTryWithCatch()
        {
            var test = @"
using Proto.Promises.Linq;

namespace ConsoleApplication1
{
    internal class MyClass
    {
        public AsyncEnumerable<int> IterateAsync()
        {
            return AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                try
                {
                    await writer.YieldAsync(42);
                }
                catch { }
            });
        }
    }
}";

            var expected = VerifyCS.Diagnostic(YieldAsyncAnalyzer.YieldAsyncTryCatchId).WithLocation(14, 21);

            var testRunner = new CSharpAnalyzerTest<YieldAsyncAnalyzer, MSTestVerifier>()
            {
                ReferenceAssemblies = new ReferenceAssemblies("net6.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "6.0.0"), Path.Combine("ref", "net6.0")),
                TestState =
                {
                    Sources = { test },
                    AdditionalReferences = { typeof(Proto.Promises.Promise).Assembly.Location },
                    ExpectedDiagnostics = { expected }
                }
            };
            await testRunner.RunAsync();
        }

        [TestMethod]
        public async Task CannotAwaitYieldInCatch()
        {
            var test = @"
using Proto.Promises.Linq;

namespace ConsoleApplication1
{
    internal class MyClass
    {
        public AsyncEnumerable<int> IterateAsync()
        {
            return AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                try
                {
                }
                catch
                {
                    await writer.YieldAsync(42);
                }
            });
        }
    }
}";

            var expected = VerifyCS.Diagnostic(YieldAsyncAnalyzer.YieldAsyncCatchId).WithLocation(17, 21);

            var testRunner = new CSharpAnalyzerTest<YieldAsyncAnalyzer, MSTestVerifier>()
            {
                ReferenceAssemblies = new ReferenceAssemblies("net6.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "6.0.0"), Path.Combine("ref", "net6.0")),
                TestState =
                {
                    Sources = { test },
                    AdditionalReferences = { typeof(Proto.Promises.Promise).Assembly.Location },
                    ExpectedDiagnostics = { expected }
                }
            };
            await testRunner.RunAsync();
        }

        [TestMethod]
        public async Task CannotAwaitYieldInFinally()
        {
            var test = @"
using Proto.Promises.Linq;

namespace ConsoleApplication1
{
    internal class MyClass
    {
        public AsyncEnumerable<int> IterateAsync()
        {
            return AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                try
                {
                }
                finally
                {
                    await writer.YieldAsync(42);
                }
            });
        }
    }
}";

            var expected = VerifyCS.Diagnostic(YieldAsyncAnalyzer.YieldAsyncFinallyId).WithLocation(17, 21);

            var testRunner = new CSharpAnalyzerTest<YieldAsyncAnalyzer, MSTestVerifier>()
            {
                ReferenceAssemblies = new ReferenceAssemblies("net6.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "6.0.0"), Path.Combine("ref", "net6.0")),
                TestState =
                {
                    Sources = { test },
                    AdditionalReferences = { typeof(Proto.Promises.Promise).Assembly.Location },
                    ExpectedDiagnostics = { expected }
                }
            };
            await testRunner.RunAsync();
        }
    }
}
