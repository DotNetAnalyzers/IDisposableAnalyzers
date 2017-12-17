namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeBeforeAssignCodeFixProvider))]
    [Shared]
    internal class DisposeBeforeAssignCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP003DisposeBeforeReassigning.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => DocumentEditorFixAllProvider.Default;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            if (syntaxRoot == null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
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
                if (node is AssignmentExpressionSyntax assignment)
                {
                    if (TryCreateDisposeStatement(assignment, semanticModel, context.CancellationToken, out var disposeStatement))
                    {
                        context.RegisterDocumentEditorFix(
                            "Dispose before re-assigning.",
                            (editor, cancellationToken) => ApplyDisposeBeforeAssign(editor, assignment, disposeStatement),
                            diagnostic);
                    }

                    continue;
                }

                var argument = node.FirstAncestorOrSelf<ArgumentSyntax>();
                if (argument != null)
                {
                    if (TryCreateDisposeStatement(argument, semanticModel, context.CancellationToken, out var disposeStatement))
                    {
                        context.RegisterDocumentEditorFix(
                                "Dispose before re-assigning.",
                                (editor, cancellationToken) => ApplyDisposeBeforeAssign(editor, argument, disposeStatement),
                            diagnostic);
                    }
                }
            }
        }

        private static void ApplyDisposeBeforeAssign(DocumentEditor editor, SyntaxNode assignment, StatementSyntax disposeStatement)
        {
            if (assignment.Parent is StatementSyntax statement &&
                statement.Parent is BlockSyntax)
            {
                editor.InsertBefore(statement, new[] { disposeStatement });
            }
            else if (assignment.Parent is ArgumentListSyntax argumentList &&
                     argumentList.Parent is InvocationExpressionSyntax invocation &&
                     invocation.Parent is StatementSyntax invocationStatement &&
                     invocationStatement.Parent is BlockSyntax)
            {
                editor.InsertBefore(invocationStatement, new[] { disposeStatement });
            }
            else if (assignment.Parent is AnonymousFunctionExpressionSyntax anonymousFunction)
            {
                editor.ReplaceNode(
                    anonymousFunction.Body,
                    (x, _) => SyntaxFactory.Block(
                        disposeStatement,
                        SyntaxFactory.ExpressionStatement((ExpressionSyntax)x)));
            }
        }

        private static bool TryCreateDisposeStatement(AssignmentExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken, out StatementSyntax result)
        {
            result = null;
            if (assignment.Parent is StatementSyntax ||
                assignment.Parent is AnonymousFunctionExpressionSyntax)
            {
                if (Disposable.IsAssignedWithCreated(assignment.Left, semanticModel, cancellationToken, out var assignedSymbol)
                              .IsEither(Result.No, Result.Unknown))
                {
                    return false;
                }

                result = Snippet.DisposeStatement(
                    assignedSymbol,
                    semanticModel,
                    cancellationToken);
                return true;
            }

            return false;
        }

        private static bool TryCreateDisposeStatement(ArgumentSyntax argument, SemanticModel semanticModel, CancellationToken cancellationToken, out StatementSyntax result)
        {
            var symbol = semanticModel.GetSymbolSafe(argument.Expression, cancellationToken);
            if (symbol == null)
            {
                result = null;
                return false;
            }

            result = Snippet.DisposeStatement(
                symbol,
                semanticModel,
                cancellationToken);
            return true;
        }
    }
}