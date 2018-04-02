namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class SyntaxNodeExt
    {
        internal static bool IsEitherKind(this SyntaxToken node, SyntaxKind first, SyntaxKind other)
        {
            var kind = node.Kind();
            return kind == first || kind == other;
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

        internal static Result IsExecutedBefore(this SyntaxNode node, SyntaxNode other)
        {
            if (node is null ||
                other is null)
            {
                return Result.No;
            }

            if (node.Contains(other) &&
                node.SpanStart < other.SpanStart)
            {
                return Result.Yes;
            }

            if (!node.SharesAncestor<MemberDeclarationSyntax>(other))
            {
                return Result.Unknown;
            }

            var statement = node.FirstAncestorOrSelf<StatementSyntax>();
            var otherStatement = other.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null ||
                otherStatement == null)
            {
                return Result.Unknown;
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
