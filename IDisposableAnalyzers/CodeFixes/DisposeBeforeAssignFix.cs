namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
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
                switch (node)
                {
                    case AssignmentExpressionSyntax { Left: { } left } assignment:
                        context.RegisterCodeFix(
                            "Dispose before re-assigning.",
                            (editor, cancellationToken) => DisposeBefore(
                                editor,
                                BackingField(left).Normalize(editor.SemanticModel, cancellationToken),
                                assignment),
                            "Dispose before re-assigning.",
                            diagnostic);
                        break;
                    case ArgumentSyntax { Expression: { } expression } argument:
                        context.RegisterCodeFix(
                            "Dispose before re-assigning.",
                            (editor, cancellationToken) => DisposeBefore(
                                editor,
                                expression.Normalize(editor.SemanticModel, cancellationToken),
                                argument),
                            "Dispose before re-assigning.",
                            diagnostic);
                        break;
                }

                ExpressionSyntax BackingField(ExpressionSyntax e)
                {
                    if (semanticModel.TryGetSymbol(e, context.CancellationToken, out var symbol) &&
                        symbol is IPropertySymbol { GetMethod: { } get } &&
                        get.TrySingleAccessorDeclaration(context.CancellationToken, out var getter))
                    {
                        switch (getter)
                        {
                            case { ExpressionBody: { Expression: { } expression } }:
                                return expression;
                            case { Body: { Statements: { Count: 1 } statements } }
                                when statements[0] is ReturnStatementSyntax { Expression: { } expression }:
                                return expression;
                        }
                    }

                    return e;
                }
            }
        }

        private static void DisposeBefore(DocumentEditor editor, ExpressionSyntax disposable, SyntaxNode location)
        {
            switch (location)
            {
                case AssignmentExpressionSyntax { Parent: { } }:
                case ArgumentSyntax { Parent: { } }:
                case InvocationExpressionSyntax { Parent: { } }:
                case CastExpressionSyntax { Parent: { } }:
                case ParenthesizedExpressionSyntax { Parent: { } }:
                    DisposeBefore(editor, disposable, location.Parent);
                    break;
                case StatementSyntax { Parent: BlockSyntax _ } statement:
                    editor.InsertBefore(statement, IDisposableFactory.ConditionalDisposeStatement(disposable));
                    break;
                case AnonymousFunctionExpressionSyntax lambda:
                    editor.ReplaceNode(
                        lambda,
                        x => x.PrependStatements(IDisposableFactory.ConditionalDisposeStatement(disposable)));
                    break;
                case ArgumentListSyntax { Parent: InvocationExpressionSyntax { Parent: ExpressionStatementSyntax { Parent: BlockSyntax _ } invocationStatement } }:
                    editor.InsertBefore(invocationStatement, IDisposableFactory.ConditionalDisposeStatement(disposable));
                    break;
                case ArgumentListSyntax { Parent: InvocationExpressionSyntax { Parent: ArrowExpressionClauseSyntax _ } invocation }:
                    DisposeBefore(editor, disposable, invocation);
                    break;
                case ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax { ReturnType: PredefinedTypeSyntax { Keyword: { ValueText: "void" } } } method }:
                    editor.ReplaceNode(
                        method,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithBody(
                                  SyntaxFactory.Block(
                                      IDisposableFactory.ConditionalDisposeStatement(disposable),
                                      SyntaxFactory.ExpressionStatement(x.ExpressionBody.Expression))));
                    break;
                case ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax method }:
                    editor.ReplaceNode(
                        method,
                        x => x.WithExpressionBody(null)
                              .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                              .WithBody(
                                  SyntaxFactory.Block(
                                      IDisposableFactory.ConditionalDisposeStatement(disposable),
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
                                      IDisposableFactory.ConditionalDisposeStatement(disposable),
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
                                      IDisposableFactory.ConditionalDisposeStatement(disposable),
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
                                                  IDisposableFactory.ConditionalDisposeStatement(disposable),
                                                  SyntaxFactory.ReturnStatement(
                                                      x.ExpressionBody.Expression)))))));
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Dispose before code gen failed for {location.Kind()}, write an issue so we can add support.");
            }
        }
    }
}
