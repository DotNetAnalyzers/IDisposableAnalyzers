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

    }
}
