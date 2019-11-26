namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ClassDeclarationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.IDISP025SealDisposable);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ClassDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is ClassDeclarationSyntax { } classDeclaration &&
                context.ContainingSymbol is INamedTypeSymbol { IsSealed: false } type &&
                type.IsAssignableTo(KnownSymbol.IDisposable, context.SemanticModel.Compilation) &&
                DisposeMethod.TryFindIDisposableDispose(type, context.Compilation, Search.TopLevel, out var disposeMethod) &&
                disposeMethod is { IsVirtual: false, IsOverride: false } &&
                !DisposeMethod.TryFindVirtualDispose(type, context.Compilation, Search.TopLevel, out _))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.IDISP025SealDisposable,
                        classDeclaration.Identifier.GetLocation(),
                        additionalLocations: new[] { disposeMethod.Locations[0] }));
            }
        }
    }
}
