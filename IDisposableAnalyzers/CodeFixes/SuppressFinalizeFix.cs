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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SuppressFinalizeFix))]
    [Shared]
    internal class SuppressFinalizeFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            Descriptors.IDISP018CallSuppressFinalizeSealed.Id,
            Descriptors.IDISP019CallSuppressFinalizeVirtual.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) is MethodDeclarationSyntax disposeMethod)
                {
                    switch (disposeMethod)
                    {
                        case { Body: { } body }:
                            context.RegisterCodeFix(
                                "GC.SuppressFinalize(this)",
                                (e, _) => e.ReplaceNode(
                                    body,
                                    x => x.AddStatements(IDisposableFactory.GcSuppressFinalizeThis)),
                                equivalenceKey: nameof(SuppressFinalizeFix),
                                diagnostic);
                            break;
                        case { ExpressionBody: { } }:
                            context.RegisterCodeFix(
                                "GC.SuppressFinalize(this)",
                                (e, _) => e.ReplaceNode(
                                    disposeMethod,
                                    x => x.AsBlockBody(
                                        SyntaxFactory.ExpressionStatement(x.ExpressionBody.Expression),
                                        IDisposableFactory.GcSuppressFinalizeThis)),
                                equivalenceKey: nameof(SuppressFinalizeFix),
                                diagnostic);
                            break;
                    }
                }
            }
        }
    }
}
