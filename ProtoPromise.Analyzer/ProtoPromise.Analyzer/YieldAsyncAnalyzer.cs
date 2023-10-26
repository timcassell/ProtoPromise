using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Proto.Promises.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class YieldAsyncAnalyzer : DiagnosticAnalyzer
    {
        public const string YieldAsyncTryCatchId = "PPAE001";
        public const string YieldAsyncCatchId = "PPAE002";
        public const string YieldAsyncFinallyId = "PPAE003";

        private static readonly DiagnosticDescriptor YieldAsyncTryCatchDiagnostic = new DiagnosticDescriptor(
            YieldAsyncTryCatchId,
            "Yield with try/catch",
            "Cannot yield in the body of a try block with a catch clause",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A yield is not allowed in a try block if there is a catch clause associated with the try block. To avoid this error, either move the yield statement out of the try/catch/finally block, or remove the catch block.");

        private static readonly DiagnosticDescriptor YieldAsyncCatchDiagnostic = new DiagnosticDescriptor(
            YieldAsyncCatchId,
            "Yield inside catch",
            "Cannot yield in the body of a catch clause",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A yield is not allowed from within the body of a catch clause. To avoid this error, move the yield out of the catch clause.");

        private static readonly DiagnosticDescriptor YieldAsyncFinallyDiagnostic = new DiagnosticDescriptor(
            YieldAsyncFinallyId,
            "Yield inside finally",
            "Cannot yield in the body of a finally clause",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A yield is not allowed from within the body of a finally clause. To avoid this error, move the yield out of the finally clause.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            YieldAsyncTryCatchDiagnostic,
            YieldAsyncCatchDiagnostic,
            YieldAsyncFinallyDiagnostic);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeAwaitExpressionSyntax, SyntaxKind.AwaitExpression);
        }

        private static void AnalyzeAwaitExpressionSyntax(SyntaxNodeAnalysisContext context)
        {
            var awaitNode = (AwaitExpressionSyntax) context.Node;
            var awaitType = context.SemanticModel.GetTypeInfo(awaitNode.Expression).Type as INamedTypeSymbol;
            bool isAsyncYield = awaitType?.Arity == 1
                && awaitType.ContainingAssembly.Name == "ProtoPromise"
                && awaitType.ConstructUnboundGenericType().ToDisplayString() == "Proto.Promises.Async.CompilerServices.AsyncStreamYielder<>";
            if (!isAsyncYield)
            {
                return;
            }

            for (var parentNode = awaitNode.Parent; parentNode != null; parentNode = parentNode.Parent)
            {
                if (parentNode.IsKind(SyntaxKind.MethodDeclaration)
                    || parentNode.IsKind(SyntaxKind.LocalFunctionStatement)
                    || parentNode.IsKind(SyntaxKind.ParenthesizedLambdaExpression)
                    || parentNode.IsKind(SyntaxKind.SimpleLambdaExpression)
                    || parentNode.IsKind(SyntaxKind.AnonymousMethodExpression))
                {
                    return;
                }
                if (parentNode.IsKind(SyntaxKind.CatchClause))
                {
                    context.ReportDiagnostic(Diagnostic.Create(YieldAsyncCatchDiagnostic, awaitNode.GetLocation()));
                    break;
                }
                else if (parentNode.IsKind(SyntaxKind.FinallyClause))
                {
                    context.ReportDiagnostic(Diagnostic.Create(YieldAsyncFinallyDiagnostic, awaitNode.GetLocation()));
                    break;
                }
                else if (parentNode.IsKind(SyntaxKind.TryStatement))
                {
                    var tryNode = (TryStatementSyntax) parentNode;
                    if (tryNode.Catches.Count > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(YieldAsyncTryCatchDiagnostic, awaitNode.GetLocation()));
                        break;
                    }
                }
            }
        }
    }
}