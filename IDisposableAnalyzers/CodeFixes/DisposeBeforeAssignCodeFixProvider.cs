namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeBeforeAssignCodeFixProvider))]
    [Shared]
    internal class DisposeBeforeAssignCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP003DisposeBeforeReassigning.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => DocumentEditorFixAllProvider.Document;

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
                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (node is AssignmentExpressionSyntax assignment &&
                    TryCreateDisposeStatement(assignment, semanticModel, context.CancellationToken, out var disposeStatement))
                {
                    context.RegisterDocumentEditorFix(
                        "Dispose before re-assigning.",
                        (editor, cancellationToken) => ApplyDisposeBeforeAssign(editor, assignment, disposeStatement),
                        diagnostic);
                }
                else if (node.TryFirstAncestorOrSelf<ArgumentSyntax>(out var argument) &&
                         TryCreateDisposeStatement(argument, semanticModel, context.CancellationToken, out disposeStatement))
                {
                    context.RegisterDocumentEditorFix(
                        "Dispose before re-assigning.",
                        (editor, cancellationToken) => ApplyDisposeBeforeAssign(editor, argument, disposeStatement),
                        diagnostic);
                }
            }
        }

        private static void ApplyDisposeBeforeAssign(DocumentEditor editor, SyntaxNode assignment, StatementSyntax disposeStatement)
        {
            switch (assignment.Parent)
            {
                case StatementSyntax statement when statement.Parent is BlockSyntax:
                    editor.InsertBefore(statement, new[] { disposeStatement });
                    break;
                case AnonymousFunctionExpressionSyntax anonymousFunction:
                    editor.ReplaceNode(
                        anonymousFunction,
                        (x, _) =>
                        {
                            var old = (AnonymousFunctionExpressionSyntax)x;
                            return old.ReplaceNode(
                                           old.Body,
                                           SyntaxFactory.Block(
                                               disposeStatement.WithTrailingTrivia(SyntaxFactory.ElasticLineFeed),
                                               SyntaxFactory.ExpressionStatement((ExpressionSyntax)old.Body)))
                                       .WithAdditionalAnnotations(Formatter.Annotation);
                        });
                    break;
                case ArgumentListSyntax argumentList:
                    {
                        if (argumentList.Parent is InvocationExpressionSyntax invocation &&
                            invocation.Parent is StatementSyntax invocationStatement &&
                            invocationStatement.Parent is BlockSyntax)
                        {
                            editor.InsertBefore(invocationStatement, new[] { disposeStatement });
                        }

                        break;
                    }

                case ArrowExpressionClauseSyntax arrow:
                    {
                        if (arrow.Parent is MethodDeclarationSyntax method)
                        {
                            editor.ReplaceNode(
                                method,
                                (x, _) =>
                                {
                                    var old = (MethodDeclarationSyntax)x;
                                    return old.WithExpressionBody(null)
                                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                                              .WithBody(
                                                  SyntaxFactory.Block(
                                                      disposeStatement,
                                                      SyntaxFactory.ReturnStatement(old.ExpressionBody.Expression)));
                                });
                        }

                        if (arrow.Parent is PropertyDeclarationSyntax property)
                        {
                            editor.ReplaceNode(
                                property,
                                (x, _) =>
                                {
                                    var old = (PropertyDeclarationSyntax)x;
                                    return old.WithExpressionBody(null)
                                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                                              .WithAccessorList(
                                                  SyntaxFactory.AccessorList(
                                                      SyntaxFactory.SingletonList(
                                                          SyntaxFactory.AccessorDeclaration(
                                                              SyntaxKind.GetAccessorDeclaration,
                                                              SyntaxFactory.Block(
                                                                  disposeStatement,
                                                                  SyntaxFactory.ReturnStatement(
                                                                      old.ExpressionBody.Expression))))));
                                });
                        }

                        break;
                    }
            }
        }

        private static bool TryCreateDisposeStatement(AssignmentExpressionSyntax assignment, SemanticModel semanticModel, CancellationToken cancellationToken, out StatementSyntax result)
        {
            result = null;
            if (Disposable.IsAlreadyAssignedWithCreated(assignment.Left, semanticModel, cancellationToken, out var assignedSymbol)
                          .IsEither(Result.No, Result.Unknown))
            {
                return false;
            }

            result = Snippet.DisposeStatement(assignedSymbol, semanticModel, cancellationToken);
            return true;
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
