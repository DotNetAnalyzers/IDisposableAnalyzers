namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class PropertyImplementationWalker : PooledWalker<PropertyImplementationWalker>
    {
        private readonly List<PropertyDeclarationSyntax> implementations = new List<PropertyDeclarationSyntax>();
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;
        private IPropertySymbol property;

        private PropertyImplementationWalker()
        {
        }

        public IReadOnlyList<PropertyDeclarationSyntax> Implementations => this.implementations;

        public static PropertyImplementationWalker Create(IPropertySymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Borrow(() => new PropertyImplementationWalker());
            pooled.semanticModel = semanticModel;
            pooled.cancellationToken = cancellationToken;
            pooled.property = symbol;
            foreach (var tree in semanticModel.Compilation.SyntaxTrees)
            {
                if (tree.FilePath.EndsWith(".g.cs"))
                {
                    continue;
                }

                if (tree.TryGetRoot(out var root))
                {
                    pooled.Visit(root);
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

        protected override void Clear()
        {
            this.implementations.Clear();
            this.semanticModel = null;
            this.cancellationToken = CancellationToken.None;
            this.property = null;
        }
    }
}