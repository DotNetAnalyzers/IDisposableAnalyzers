namespace IDisposableAnalyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

using Gu.Roslyn.CodeFixExtensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveCallFix))]
[Shared]
internal class RemoveCallFix : DocumentEditorCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Descriptors.IDISP024DoNotCallSuppressFinalize.Id);

    protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

    protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
    {
        var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                      .ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (syntaxRoot?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ExpressionStatementSyntax>() is { } statement)
            {
                context.RegisterCodeFix(
                    "Remove",
                    (e, _) => e.RemoveNode(statement),
                    equivalenceKey: nameof(RemoveCallFix),
                    diagnostic);
            }
        }
    }
}
