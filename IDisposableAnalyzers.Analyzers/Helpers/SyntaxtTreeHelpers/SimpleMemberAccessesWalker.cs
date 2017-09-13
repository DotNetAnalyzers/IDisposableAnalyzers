namespace IDisposableAnalyzers
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class SimpleMemberAccessesWalker : CSharpSyntaxWalker
    {
        private static readonly Pool<SimpleMemberAccessesWalker> Cache = new Pool<SimpleMemberAccessesWalker>(
            () => new SimpleMemberAccessesWalker(),
            x => x.simpleMemberAccesses.Clear());

        private readonly List<MemberAccessExpressionSyntax> simpleMemberAccesses = new List<MemberAccessExpressionSyntax>();

        private SimpleMemberAccessesWalker()
        {
        }

        public IReadOnlyList<MemberAccessExpressionSyntax> SimpleMemberAccesses => this.simpleMemberAccesses;

        public static Pool<SimpleMemberAccessesWalker>.Pooled Create(SyntaxNode node)
        {
            var pooled = Cache.GetOrCreate();
            pooled.Item.Visit(node);
            return pooled;
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                this.simpleMemberAccesses.Add(node);
            }

            base.VisitMemberAccessExpression(node);
        }
    }
}