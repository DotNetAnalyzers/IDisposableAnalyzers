namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
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
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(Descriptors.IDISP003DisposeBeforeReassigning.Id);

        protected override DocumentEditorFixAllProvider? FixAllProvider() => null;

        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                                   .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                switch (syntaxRoot?.FindNode(diagnostic.Location.SourceSpan))
                {
                    case AssignmentExpressionSyntax { Left: { } left } assignment:
                        context.RegisterCodeFix(
                            "Dispose before re-assigning.",
                            (editor, cancellationToken) => DisposeBefore(editor, left, assignment, cancellationToken),
                            "Dispose before re-assigning.",
                            diagnostic);
                        break;
                    case ArgumentSyntax { Expression: { } expression } argument:
                        context.RegisterCodeFix(
                            "Dispose before re-assigning.",
                            (editor, cancellationToken) => DisposeBefore(editor, expression, argument, cancellationToken),
                            "Dispose before re-assigning.",
                            diagnostic);
                        break;
                }
            }
        }

        private static void DisposeBefore(DocumentEditor editor, ExpressionSyntax disposable, SyntaxNode location, CancellationToken cancellationToken)
        {
            switch (location)
            {
                case AssignmentExpressionSyntax { Parent: { } }:
                case ArgumentSyntax { Parent: { } }:
                case InvocationExpressionSyntax { Parent: { } }:
                case CastExpressionSyntax { Parent: { } }:
                case ParenthesizedExpressionSyntax { Parent: { } }:
                    DisposeBefore(editor, disposable, location.Parent, cancellationToken);
                    break;
                case StatementSyntax { Parent: BlockSyntax _ } statement:
                    editor.InsertBefore(statement, IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken));
                    break;
                case AnonymousFunctionExpressionSyntax lambda:
                    editor.ReplaceNode(
                        lambda,
                        x => x.PrependStatements(IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken)));
                    break;
                case ArgumentListSyntax { Parent: InvocationExpressionSyntax { Parent: ExpressionStatementSyntax { Parent: BlockSyntax _ } invocationStatement } }:
                    editor.InsertBefore(invocationStatement, IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken));
                    break;
                case ArgumentListSyntax { Parent: InvocationExpressionSyntax { Parent: ArrowExpressionClauseSyntax _ } invocation }:
                    DisposeBefore(editor, disposable, invocation, cancellationToken);
                    break;
                case ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax { ReturnType: PredefinedTypeSyntax { Keyword: { ValueText: "void" } } } method }:
                    editor.ReplaceNode(
                        method,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithBody(
                                  SyntaxFactory.Block(
                                      IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken),
                                      SyntaxFactory.ExpressionStatement(x.ExpressionBody!.Expression))));
                    break;
                case ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax method }:
                    editor.ReplaceNode(
                        method,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithBody(
                                  SyntaxFactory.Block(
                                      IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken),
                                      SyntaxFactory.ReturnStatement(x.ExpressionBody!.Expression))));
                    break;
                case ArrowExpressionClauseSyntax { Parent: AccessorDeclarationSyntax accessor }
                    when accessor.IsKind(SyntaxKind.GetAccessorDeclaration):
                    editor.ReplaceNode(
                        accessor,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithBody(
                                  SyntaxFactory.Block(
                                      IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken),
                                      SyntaxFactory.ReturnStatement(x.ExpressionBody!.Expression))));
                    break;
                case ArrowExpressionClauseSyntax { Parent: AccessorDeclarationSyntax accessor }
                    when accessor.IsKind(SyntaxKind.SetAccessorDeclaration):
                    editor.ReplaceNode(
                        accessor,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithBody(
                                  SyntaxFactory.Block(
                                      IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken),
                                      SyntaxFactory.ExpressionStatement(x.ExpressionBody!.Expression))));
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
                                                  IDisposableFactory.DisposeStatement(disposable, editor.SemanticModel, cancellationToken),
                                                  SyntaxFactory.ReturnStatement(
                                                      x.ExpressionBody!.Expression)))))));
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Dispose before code gen failed for {location.Kind()}, write an issue so we can add support.");
            }
        }
    }
}
