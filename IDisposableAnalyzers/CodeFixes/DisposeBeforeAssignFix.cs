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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposeBeforeAssignFix))]
    [Shared]
    internal class DisposeBeforeAssignFix : DocumentEditorCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDISP003DisposeBeforeReassigning.DiagnosticId);

        protected override DocumentEditorFixAllProvider FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                                      .ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                if (node is AssignmentExpressionSyntax assignment &&
                    TryCreateDisposeStatement(assignment, semanticModel, context.CancellationToken, out var disposeStatement))
                {
                    context.RegisterCodeFix(
                        "Dispose before re-assigning.",
                        (editor, cancellationToken) => ApplyDisposeBeforeAssign(editor, assignment, disposeStatement),
                        "Dispose before re-assigning.",
                        diagnostic);
                }
                else if (node.TryFirstAncestorOrSelf<ArgumentSyntax>(out var argument) &&
                         TryCreateDisposeStatement(argument, semanticModel, context.CancellationToken, out disposeStatement))
                {
                    context.RegisterCodeFix(
                        "Dispose before re-assigning.",
                        (editor, cancellationToken) => ApplyDisposeBeforeAssign(editor, argument, disposeStatement),
                        "Dispose before re-assigning.",
                        diagnostic);
                }
            }
        }

        private static void ApplyDisposeBeforeAssign(DocumentEditor editor, SyntaxNode assignment, StatementSyntax disposeStatement)
        {
            switch (assignment.Parent)
            {
                case StatementSyntax { Parent: BlockSyntax _ } statement:
                    editor.InsertBefore(statement, new[] { disposeStatement });
                    break;
                case AnonymousFunctionExpressionSyntax lambda:
                    editor.ReplaceNode(
                        lambda,
                        x => x.PrependStatements(disposeStatement));
                    break;
                case ArgumentListSyntax { Parent: InvocationExpressionSyntax { Parent: ExpressionStatementSyntax { Parent: BlockSyntax _ } invocationStatement } }:
                    editor.InsertBefore(invocationStatement, new[] { disposeStatement });
                    break;
                case ArgumentListSyntax { Parent: InvocationExpressionSyntax { Parent: ArrowExpressionClauseSyntax _} invocation }:
                    ApplyDisposeBeforeAssign(editor, invocation, disposeStatement);
                    break;
                case ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax { ReturnType: PredefinedTypeSyntax { Keyword: { ValueText: "void" } } } method }:
                    editor.ReplaceNode(
                        method,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithBody(
                                  SyntaxFactory.Block(
                                      disposeStatement,
                                      SyntaxFactory.ExpressionStatement(x.ExpressionBody.Expression))));
                    break;
                case ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax method }:
                    editor.ReplaceNode(
                        method,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithBody(
                                  SyntaxFactory.Block(
                                      disposeStatement,
                                      SyntaxFactory.ReturnStatement(x.ExpressionBody.Expression))));
                    break;
                case ArrowExpressionClauseSyntax { Parent: AccessorDeclarationSyntax accessor }
                    when accessor.IsKind(SyntaxKind.GetAccessorDeclaration):
                    editor.ReplaceNode(
                        accessor,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithBody(
                                  SyntaxFactory.Block(
                                      disposeStatement,
                                      SyntaxFactory.ReturnStatement(x.ExpressionBody.Expression))));
                    break;
                case ArrowExpressionClauseSyntax { Parent: AccessorDeclarationSyntax accessor }
                    when accessor.IsKind(SyntaxKind.SetAccessorDeclaration):
                    editor.ReplaceNode(
                        accessor,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithBody(
                                  SyntaxFactory.Block(
                                      disposeStatement,
                                      SyntaxFactory.ExpressionStatement(x.ExpressionBody.Expression))));
                    break;
                case ArrowExpressionClauseSyntax { Parent: PropertyDeclarationSyntax property }:
                    editor.ReplaceNode(
                        property,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithAccessorList(
                                  SyntaxFactory.AccessorList(
                                      SyntaxFactory.SingletonList(
                                          SyntaxFactory.AccessorDeclaration(
                                              SyntaxKind.GetAccessorDeclaration,
                                              SyntaxFactory.Block(
                                                  disposeStatement,
                                                  SyntaxFactory.ReturnStatement(
                                                      x.ExpressionBody.Expression)))))));
                    break;

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
