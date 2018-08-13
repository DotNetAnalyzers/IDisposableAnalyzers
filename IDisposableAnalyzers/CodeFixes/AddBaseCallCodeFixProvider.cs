namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBaseCallCodeFixProvider))]
    [Shared]
    internal class AddBaseCallCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP010CallBaseDispose.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => null;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan) is MethodDeclarationSyntax disposeMethod &&
                    disposeMethod.Body is BlockSyntax body &&
                    disposeMethod.ParameterList is ParameterListSyntax parameterList &&
                    parameterList.Parameters.TrySingle(out var parameter))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            $"Call base.Dispose({parameter.Identifier.ValueText})",
                            _ => Task.FromResult(
                                context.Document.WithSyntaxRoot(
                                    syntaxRoot.ReplaceNode(
                                        body,
                                        body.AddStatements(SyntaxFactory.ParseStatement($"base.{disposeMethod.Identifier.ValueText}({parameter.Identifier.ValueText});")
                                                                        .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                                                        .WithTrailingTrivia(SyntaxFactory.ElasticMarker))))),
                            "Call base.Dispose()"),
                        diagnostic);
                }
            }
        }
    }
}
