namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UsingStatementAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            Descriptors.IDISP007DoNotDisposeInjected);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.UsingStatement);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is UsingStatementSyntax usingStatement)
            {
                if (usingStatement.Declaration is VariableDeclarationSyntax variableDeclaration)
                {
                    foreach (var variableDeclarator in variableDeclaration.Variables)
                    {
                        if (variableDeclarator.Initializer == null)
                        {
                            continue;
                        }

                        var value = variableDeclarator.Initializer.Value;
                        if (Disposable.IsCachedOrInjectedOnly(value, value, context.SemanticModel, context.CancellationToken))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP007DoNotDisposeInjected, value.GetLocation()));
                        }
                    }
                }
                else if (usingStatement.Expression is ExpressionSyntax expression)
                {
                    if (Disposable.IsCachedOrInjectedOnly(expression, expression, context.SemanticModel, context.CancellationToken))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptors.IDISP007DoNotDisposeInjected, usingStatement.Expression.GetLocation()));
                    }
                }
            }
        }
    }
}
