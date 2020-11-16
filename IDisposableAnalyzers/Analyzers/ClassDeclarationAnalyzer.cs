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
            if (context.Node is ClassDeclarationSyntax classDeclaration &&
                context.ContainingSymbol is INamedTypeSymbol { IsSealed: false } type &&
                type.IsAssignableTo(KnownSymbols.IDisposable, context.SemanticModel.Compilation) &&
                DisposeMethod.Find(type, context.Compilation, Search.TopLevel) is { IsVirtual: false, IsAbstract: false, IsOverride: false } disposeMethod &&
                !HasDisposeDisposing(type))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.IDISP025SealDisposable,
                        classDeclaration.Identifier.GetLocation(),
                        additionalLocations: new[] { disposeMethod.Locations[0] }));
            }
        }

        private static bool HasDisposeDisposing(INamedTypeSymbol type)
        {
            foreach (var member in type.GetMembers("Dispose"))
            {
                if (member is IMethodSymbol { ReturnsVoid: true, Name: "Dispose", Parameters: { Length: 1 } parameters } &&
                    parameters[0].Type.SpecialType == SpecialType.System_Boolean)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
