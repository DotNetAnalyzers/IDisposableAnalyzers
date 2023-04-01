namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class LocalDeclarationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.IDISP001DisposeCreated,
            Descriptors.IDISP007DoNotDisposeInjected);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.LocalDeclarationStatement);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is LocalDeclarationStatementSyntax { Declaration: { Variables: { } variables } localDeclaration } statement)
            {
                if (statement.UsingKeyword.IsKind(SyntaxKind.None))
                {
                    foreach (var declarator in variables)
                    {
                        if (declarator.Initializer is { Value: { } value } &&
                            Disposable.IsCreation(value, context.SemanticModel, context.CancellationToken) &&
                            context.SemanticModel.TryGetSymbol(declarator, context.CancellationToken, out ILocalSymbol? local) &&
                            Disposable.ShouldDispose(new LocalOrParameter(local), context.SemanticModel, context.CancellationToken))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP001DisposeCreated, localDeclaration.GetLocation()));
                        }
                    }
                }
                else
                {
                    foreach (var declarator in variables)
                    {
                        if (declarator is { Initializer.Value: { } value } &&
                            Disposable.IsCachedOrInjectedOnly(value, value, context.SemanticModel, context.CancellationToken))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP007DoNotDisposeInjected, value.GetLocation()));
                        }
                    }
                }
            }
        }
    }
}
