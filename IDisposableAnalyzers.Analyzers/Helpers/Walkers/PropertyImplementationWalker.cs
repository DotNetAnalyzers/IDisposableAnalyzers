namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class PropertyImplementationWalker : CSharpSyntaxWalker
    {
        private static readonly Pool<PropertyImplementationWalker> Cache = new Pool<PropertyImplementationWalker>(
            () => new PropertyImplementationWalker(),
            x =>
            {
                x.implementations.Clear();
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
                x.property = null;
            });

        private readonly List<PropertyDeclarationSyntax> implementations = new List<PropertyDeclarationSyntax>();
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;
        private IPropertySymbol property;

        private PropertyImplementationWalker()
        {
        }

        public IReadOnlyList<PropertyDeclarationSyntax> Implementations => this.implementations;

        public static Pool<PropertyImplementationWalker>.Pooled Create(IPropertySymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Cache.GetOrCreate();
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.property = symbol;
            foreach (var tree in semanticModel.Compilation.SyntaxTrees)
            {
                if (tree.FilePath.EndsWith(".g.cs"))
                {
                    continue;
                }

                if (tree.TryGetRoot(out SyntaxNode root))
                {
                    pooled.Item.Visit(root);
                }
            }

            return pooled;
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Identifier.ValueText == this.property.Name)
            {
                var symbol = this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken);
                if (symbol == null)
                {
                    return;
                }

                var forInterfaceMember = symbol.ContainingType.FindImplementationForInterfaceMember(this.property);
                if (ReferenceEquals(this.property, symbol) ||
                    ReferenceEquals(this.property, symbol.OverriddenProperty) ||
                    ReferenceEquals(symbol, forInterfaceMember))
                {
                    this.implementations.Add(node);
                }
            }
        }
    }
}