namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArgumentFix))]
    [Shared]
    internal class ArgumentFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.IDISP020SuppressFinalizeThis.Id,
            Descriptors.IDISP021DisposeTrue.Id,
            Descriptors.IDISP022DisposeFalse.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ArgumentSyntax argument))
                {
                    if (diagnostic.Id == Descriptors.IDISP020SuppressFinalizeThis.Id)
                    {
                        context.RegisterCodeFix(
                            "GC.SuppressFinalize(this)",
                            (e, _) => e.ReplaceNode(
                                argument.Expression,
                                SyntaxFactory.ThisExpression()),
                            equivalenceKey: nameof(SuppressFinalizeFix),
                            diagnostic);
                    }
                    else if (diagnostic.Id == Descriptors.IDISP021DisposeTrue.Id)
                    {
                        context.RegisterCodeFix(
                            "this.Dispose(true)",
                            (e, _) => e.ReplaceNode(
                                argument.Expression,
                                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                            equivalenceKey: nameof(SuppressFinalizeFix),
                            diagnostic);
                    }
                    else if (diagnostic.Id == Descriptors.IDISP022DisposeFalse.Id)
                    {
                        context.RegisterCodeFix(
                            "this.Dispose(false)",
                            (e, _) => e.ReplaceNode(
                                argument.Expression,
                                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)),
                            equivalenceKey: nameof(SuppressFinalizeFix),
                            diagnostic);
                    }
                }
            }
        }
    }
}
