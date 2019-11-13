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
            IDISP020SuppressFinalizeThis.Descriptor.Id,
            IDISP021DisposeTrue.Descriptor.Id,
            IDISP022DisposeFalse.Descriptor.Id);

        protected override DocumentEditorFixAllProvider FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNode(diagnostic, out ArgumentSyntax argument))
                {
                    if (diagnostic.Id == IDISP020SuppressFinalizeThis.Descriptor.Id)
                    {
                        context.RegisterCodeFix(
                            "GC.SuppressFinalize(this)",
                            (e, _) => e.ReplaceNode(
                                argument.Expression,
                                SyntaxFactory.ThisExpression()),
                            equivalenceKey: nameof(SuppressFinalizeFix),
                            diagnostic);
                    }
                    else if (diagnostic.Id == IDISP021DisposeTrue.Descriptor.Id)
                    {
                        context.RegisterCodeFix(
                            "this.Dispose(true)",
                            (e, _) => e.ReplaceNode(
                                argument.Expression,
                                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                            equivalenceKey: nameof(SuppressFinalizeFix),
                            diagnostic);
                    }
                    else if (diagnostic.Id == IDISP022DisposeFalse.Descriptor.Id)
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
