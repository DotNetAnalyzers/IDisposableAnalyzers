namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class SuppressFinalizeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.IDISP024DoNotCallSuppressFinalizeIfSealedAndNoFinalizer);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.InvocationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is InvocationExpressionSyntax { ArgumentList: { Arguments: { Count: 1 } arguments } } invocation &&
                invocation.IsSymbol(KnownSymbol.GC.SuppressFinalize, context.SemanticModel, context.CancellationToken) &&
                context.SemanticModel.TryGetNamedType(arguments[0].Expression, context.CancellationToken, out var type) &&
                type.IsSealed &&
                !type.TryFindFirstMethod(x => x.MethodKind == MethodKind.Destructor, out _))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.IDISP024DoNotCallSuppressFinalizeIfSealedAndNoFinalizer,
                        invocation.GetLocation()));
            }
        }
    }
}
