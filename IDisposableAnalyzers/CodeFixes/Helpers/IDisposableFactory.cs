namespace IDisposableAnalyzers
{
    using System;
    using System.Linq;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;

    internal static class IDisposableFactory
    {
        private static readonly TypeSyntax IDisposable = SyntaxFactory.ParseTypeName("System.IDisposable")
                                                                      .WithSimplifiedNames();

        private static readonly IdentifierNameSyntax Dispose = SyntaxFactory.IdentifierName("Dispose");

        internal static ExpressionStatementSyntax DisposeStatement(ExpressionSyntax disposable)
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        disposable,
                        Dispose)));
        }

        internal static ExpressionStatementSyntax ConditionalDisposeStatement(ExpressionSyntax disposable)
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.ConditionalAccessExpression(
                    disposable,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(SyntaxFactory.Token(SyntaxKind.DotToken), Dispose))));
        }

        internal static AnonymousFunctionExpressionSyntax PrependStatements(this AnonymousFunctionExpressionSyntax lambda, params StatementSyntax[] statements)
        {
            return lambda switch
            {
                { Body: ExpressionSyntax body } => lambda.ReplaceNode(
                                                             body,
                                                             SyntaxFactory.Block(statements.Append(SyntaxFactory.ExpressionStatement(body)))
                                                                          .WithLeadingLineFeed())
                                                         .WithAdditionalAnnotations(Formatter.Annotation),
                { Body: BlockSyntax block } => lambda.ReplaceNode(block, block.AddStatements(statements)),
                _ => throw new NotSupportedException(
                    $"No support for adding statements to lambda with the shape: {lambda?.ToString() ?? "null"}"),
            };
        }

        internal static ParenthesizedExpressionSyntax AsIDisposable(ExpressionSyntax e)
        {
            return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, e, IDisposable));
        }
    }
}
