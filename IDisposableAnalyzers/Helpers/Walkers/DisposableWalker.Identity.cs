namespace IDisposableAnalyzers
{
    using System;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        private static ExpressionSyntax? Identity(ExpressionSyntax expression)
        {
            return expression switch
            {
                { Parent: BinaryExpressionSyntax { OperatorToken: { ValueText: "as" } } parent }
                => Recursive(parent),
                { Parent: BinaryExpressionSyntax { OperatorToken: { ValueText: "??" } } parent }
                => Recursive(parent),
                { Parent: CastExpressionSyntax parent }
                => Recursive(parent),
                { Parent: ConditionalExpressionSyntax parent }
                => Recursive(parent),
                { Parent: ParenthesizedExpressionSyntax parent }
                => Recursive(parent),
                _ => null,
            };

            static ExpressionSyntax Recursive(ExpressionSyntax parent) => Identity(parent) ?? parent;
        }

        [Obsolete("Use Identity")]
        private static bool IsIdentity(ExpressionSyntax expression)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.AsExpression:
                case SyntaxKind.AwaitExpression:
                case SyntaxKind.CastExpression:
                case SyntaxKind.CoalesceExpression:
                case SyntaxKind.ConditionalExpression:
                case SyntaxKind.ParenthesizedExpression:
                    return true;
                default:
                    return false;
            }
        }
    }
}
