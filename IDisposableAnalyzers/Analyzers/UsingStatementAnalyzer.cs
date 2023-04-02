namespace IDisposableAnalyzers;

using System.Collections.Immutable;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class UsingStatementAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.IDISP007DoNotDisposeInjected);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.UsingStatement);
    }

    private static void Handle(SyntaxNodeAnalysisContext context)
    {
        if (!context.IsExcludedFromAnalysis() &&
            context.Node is UsingStatementSyntax usingStatement)
        {
            switch (usingStatement)
            {
                case { Declaration.Variables: { } variables }:
                    foreach (var declarator in variables)
                    {
                        if (declarator is { Initializer.Value: { } value } &&
                            Disposable.IsCachedOrInjectedOnly(value, value, context.SemanticModel, context.CancellationToken))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP007DoNotDisposeInjected, value.GetLocation()));
                        }
                    }

                    break;
                case { Expression: { } expression }
                    when Disposable.IsCachedOrInjectedOnly(expression, expression, context.SemanticModel, context.CancellationToken):
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP007DoNotDisposeInjected, usingStatement.Expression.GetLocation()));

                    break;
            }
        }
    }
}
