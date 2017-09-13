namespace IDisposableAnalyzers
{
    using System.Collections.Generic;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ObjectCreationWalker : CSharpSyntaxWalker
    {
        private static readonly Pool<ObjectCreationWalker> Cache = new Pool<ObjectCreationWalker>(
                                                                       () => new ObjectCreationWalker(),
                                                                       x => x.objectCreations.Clear());

        private readonly List<ObjectCreationExpressionSyntax> objectCreations = new List<ObjectCreationExpressionSyntax>();

        private ObjectCreationWalker()
        {
        }

        public IReadOnlyList<ObjectCreationExpressionSyntax> ObjectCreations => this.objectCreations;

        public static Pool<ObjectCreationWalker>.Pooled Create(SyntaxNode node)
        {
            var pooled = Cache.GetOrCreate();
            pooled.Item.Visit(node);
            return pooled;
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            this.objectCreations.Add(node);
            base.VisitObjectCreationExpression(node);
        }
    }
}