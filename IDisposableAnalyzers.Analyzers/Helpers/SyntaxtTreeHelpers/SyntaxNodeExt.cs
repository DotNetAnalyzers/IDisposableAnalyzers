namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class SyntaxNodeExt
    {
        internal static int StartingLineNumber(this SyntaxNode node, CancellationToken cancellationToken)
        {
            return node.SyntaxTree.GetLineSpan(node.Span, cancellationToken).Span.Start.Line;
        }

        internal static int StartingLineNumber(this SyntaxToken token, CancellationToken cancellationToken)
        {
            return token.SyntaxTree.GetLineSpan(token.Span, cancellationToken).Span.Start.Line;
        }

        internal static bool IsEitherKind(this SyntaxNode node, SyntaxKind first, SyntaxKind other)
        {
            var kind = node?.Kind();
            return kind == first || kind == other;
        }

        internal static T FirstAncestor<T>(this SyntaxNode node)
            where T : SyntaxNode
        {
            if (node == null)
            {
                return null;
            }

            if (node is T)
            {
                return node.Parent?.FirstAncestorOrSelf<T>();
            }

            return node.FirstAncestorOrSelf<T>();
        }

        internal static bool IsInExpressionTree(this SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var lambda = node.FirstAncestor<LambdaExpressionSyntax>();
            while (lambda != null)
            {
                var lambdaType = semanticModel.GetTypeInfoSafe(lambda, cancellationToken).ConvertedType;
                if (lambdaType != null &&
                    lambdaType.Is(KnownSymbol.Expression))
                {
                    return true;
                }

                lambda = lambda.FirstAncestor<LambdaExpressionSyntax>();
            }

            return false;
        }

        internal static Result IsBeforeInScope(this SyntaxNode node, SyntaxNode other)
        {
            var statement = node?.FirstAncestorOrSelf<StatementSyntax>();
            var otherStatement = other?.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null ||
                otherStatement == null)
            {
                return Result.AssumeNo;
            }

            var block = statement.Parent as BlockSyntax;
            var otherBlock = otherStatement.Parent as BlockSyntax;
            if (block == null && otherBlock == null)
            {
                return Result.No;
            }

            if (ReferenceEquals(block, otherBlock) ||
                otherBlock?.Contains(node) == true ||
                block?.Contains(other) == true)
            {
                var firstAnon = node.FirstAncestor<AnonymousFunctionExpressionSyntax>();
                var otherAnon = other.FirstAncestor<AnonymousFunctionExpressionSyntax>();
                if (!ReferenceEquals(firstAnon, otherAnon))
                {
                    return Result.Yes;
                }

                return statement.SpanStart < otherStatement.SpanStart
                    ? Result.Yes
                    : Result.No;
            }

            return Result.No;
        }

        internal static bool SharesAncestor<T>(this SyntaxNode first, SyntaxNode other)
            where T : SyntaxNode
        {
            var firstAncestor = first.FirstAncestor<T>();
            var otherAncestor = other.FirstAncestor<T>();
            if (firstAncestor == null ||
                otherAncestor == null)
            {
                return false;
            }

            return firstAncestor == otherAncestor;
        }
    }
}
