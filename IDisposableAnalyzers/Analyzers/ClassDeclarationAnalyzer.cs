namespace IDisposableAnalyzers;

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
        Descriptors.IDISP025SealDisposable, Descriptors.IDISP026SealAsyncDisposable);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ClassDeclaration);
    }

    private static void Handle(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return;
        }

        if (context.ContainingSymbol is not INamedTypeSymbol { IsSealed: false } type)
        {
            return;
        }

        if (type.IsAssignableTo(KnownSymbols.IDisposable, context.SemanticModel.Compilation))
        {
            if (HasDisposeDisposing(type))
            {
                return;
            }

            if (DisposeMethod.Find(type, context.Compilation, Search.TopLevel) is not { IsVirtual: false, IsAbstract: false, IsOverride: false } disposeMethod)
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.IDISP025SealDisposable,
                    classDeclaration.Identifier.GetLocation(),
                    additionalLocations: new[] { disposeMethod.Locations[0] }));
        }
        else if (type.IsAssignableTo(KnownSymbols.IAsyncDisposable, context.SemanticModel.Compilation))
        {
            if (DisposeMethod.FindDisposeAsync(type, context.Compilation, Search.TopLevel) is not { IsAbstract: false, IsOverride: false } disposeMethod)
            {
                return;
            }

            if (HasDisposeAsyncCore(type))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.IDISP026SealAsyncDisposable,
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

    private static bool HasDisposeAsyncCore(INamedTypeSymbol type)
    {
        // Types with IAsyncDisposable should have DisposeAsyncCore
        // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync#:~:text=A%20protected%20virtual%20ValueTask%20DisposeAsyncCore()%20method%20whose%20signature%20is%3A
        foreach (var member in type.GetMembers("DisposeAsyncCore"))
        {
            if (member is IMethodSymbol { Name: "DisposeAsyncCore", IsVirtual: true, ReturnType.MetadataName: "ValueTask", Parameters.IsEmpty: true })
            {
                return true;
            }
        }

        return false;
    }
}
