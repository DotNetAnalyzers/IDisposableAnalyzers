namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddUsingCodeFixProvider))]
    [Shared]
    internal class AddUsingCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDISP001DisposeCreated.DiagnosticId,
            IDISP004DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId);

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (diagnostic.Id == IDISP001DisposeCreated.DiagnosticId)
                {
                    var statement = node.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
                    if (statement.Parent is BlockSyntax block)
                    {
                        context.RegisterDocumentEditorFix(
                            "Add using to end of block.",
                            (editor, cancellationToken) => AddUsing(editor, block, statement, cancellationToken),
                            diagnostic);
                    }
                }

                if (diagnostic.Id == IDISP004DontIgnoreReturnValueOfTypeIDisposable.DiagnosticId)
                {
                    var statement = node.FirstAncestorOrSelf<ExpressionStatementSyntax>();
                    if (statement.Parent is BlockSyntax block)
                    {
                        context.RegisterDocumentEditorFix(
                            "Add using to end of block.",
                            (editor, cancellationToken) => AddUsing(editor, block, statement, cancellationToken),
                            diagnostic);
                    }
                }
            }
        }

        private static void AddUsing(DocumentEditor editor, BlockSyntax block, LocalDeclarationStatementSyntax statement, CancellationToken cancellationToken)
        {
            var statements = block.Statements
                                  .Where(s => s.SpanStart > statement.SpanStart)
                                  .ToArray();
            foreach (var statementSyntax in statements)
            {
                editor.RemoveNode(statementSyntax);
            }

            editor.ReplaceNode(
                statement,
                SyntaxFactory.UsingStatement(
                    declaration: statement.Declaration,
                    expression: null,
                    statement: SyntaxFactory.Block(SyntaxFactory.List(statements))
                                            .WithAdditionalAnnotations(Formatter.Annotation)));
        }

        private static void AddUsing(DocumentEditor editor, BlockSyntax block, ExpressionStatementSyntax statement, CancellationToken cancellationToken)
        {
            var statements = block.Statements
                                  .Where(s => s.SpanStart > statement.SpanStart)
                                  .ToArray();
            foreach (var statementSyntax in statements)
            {
                editor.RemoveNode(statementSyntax);
            }

            editor.ReplaceNode(
                statement,
                SyntaxFactory.UsingStatement(
                    declaration: null,
                    expression: statement.Expression,
                    statement: SyntaxFactory.Block(SyntaxFactory.List(statements))
                                            .WithAdditionalAnnotations(Formatter.Annotation)));
        }
    }
}