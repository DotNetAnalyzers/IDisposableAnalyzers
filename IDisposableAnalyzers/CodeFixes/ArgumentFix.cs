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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArgumentFix))]
    [Shared]
    internal class ArgumentFix : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP020SuppressFinalizeThis.DiagnosticId,
            IDISP021DisposeTrue.DiagnosticId,
            IDISP022DisposeFalse.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => null;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ArgumentSyntax argument))
                {
                    if (diagnostic.Id == IDISP020SuppressFinalizeThis.DiagnosticId)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Call GC.SuppressFinalize(this)",
                                _ => Task.FromResult(
                                    context.Document.WithSyntaxRoot(
                                        syntaxRoot.ReplaceNode(
                                            argument.Expression,
                                            SyntaxFactory.ThisExpression()))),
                                equivalenceKey: nameof(SuppressFinalizeFix)),
                            diagnostic);
                    }
                    else if (diagnostic.Id == IDISP021DisposeTrue.DiagnosticId)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Call this.Dispose(true)",
                                _ => Task.FromResult(
                                    context.Document.WithSyntaxRoot(
                                        syntaxRoot.ReplaceNode(
                                            argument.Expression,
                                            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)))),
                                equivalenceKey: nameof(SuppressFinalizeFix)),
                            diagnostic);
                    }
                    else if (diagnostic.Id == IDISP022DisposeFalse.DiagnosticId)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Call this.Dispose(false)",
                                _ => Task.FromResult(
                                    context.Document.WithSyntaxRoot(
                                        syntaxRoot.ReplaceNode(
                                            argument.Expression,
                                            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))),
                                equivalenceKey: nameof(SuppressFinalizeFix)),
                            diagnostic);
                    }
                }
            }
        }
    }
}
