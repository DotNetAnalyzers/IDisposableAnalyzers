namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddSuppressFinalizeCallFix))]
    [Shared]
    internal class AddSuppressFinalizeCallFix : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP018CallSuppressFinalize.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => null;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out MethodDeclarationSyntax disposeMethod))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Call GC.SuppressFinalize(this)",
                            _ => Update(),
                            equivalenceKey: nameof(AddSuppressFinalizeCallFix)),
                        diagnostic);

                    Task<Document> Update()
                    {
                        if (disposeMethod.Body is BlockSyntax body)
                        {
                            return Task.FromResult(
                                context.Document.WithSyntaxRoot(
                                    syntaxRoot.ReplaceNode(
                                        body,
                                        body.AddStatements(SyntaxFactory.ParseStatement("GC.SuppressFinalize(this);")
                                                                        .WithAdditionalAnnotations(Formatter.Annotation)))));
                        }
                        else if (disposeMethod.ExpressionBody is ArrowExpressionClauseSyntax arrowExpression)
                        {
                            var block = SyntaxFactory.Block(
                                SyntaxFactory.ExpressionStatement(arrowExpression.Expression)
                                             .WithAdditionalAnnotations(Formatter.Annotation),
                                SyntaxFactory.ParseStatement("GC.SuppressFinalize(this);")
                                             .WithAdditionalAnnotations(Formatter.Annotation));
                            return Task.FromResult(
                                context.Document.WithSyntaxRoot(
                                    syntaxRoot.ReplaceNode(
                                        disposeMethod,
                                        disposeMethod.WithBody(block)
                                                     .WithExpressionBody(null)
                                                     .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None)))));
                        }

                        return Task.FromResult(context.Document);
                    }
                }
            }
        }
    }
}
