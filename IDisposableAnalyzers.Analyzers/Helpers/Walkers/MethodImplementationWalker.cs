namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class MethodImplementationWalker : PooledWalker<MethodImplementationWalker>
    {
        private readonly List<MethodDeclarationSyntax> implementations = new List<MethodDeclarationSyntax>();
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;
        private IMethodSymbol method;

        private MethodImplementationWalker()
        {
        }

        public IReadOnlyList<MethodDeclarationSyntax> Implementations => this.implementations;

        public static MethodImplementationWalker Create(IMethodSymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = Borrow(() => new MethodImplementationWalker());
            walker.semanticModel = semanticModel;
            walker.cancellationToken = cancellationToken;
            walker.method = symbol;
            if (symbol != null)
            {
                foreach (var tree in semanticModel.Compilation.SyntaxTrees)
                {
                    if (tree.FilePath.EndsWith(".g.cs"))
                    {
                        continue;
                    }

                    if (tree.TryGetRoot(out SyntaxNode root))
                    {
                        walker.Visit(root);
                    }
                }
            }

            return walker;
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Identifier.ValueText == this.method.Name)
            {
                var symbol = this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken);
                if (symbol == null || symbol.IsStatic)
                {
                    return;
                }

                if (this.IsMatch(symbol))
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
            this.method = null;
        }

        private bool IsMatch(IMethodSymbol other)
        {
            if (this.method.Equals(other) ||
                this.method.Equals(other.OverriddenMethod))
            {
                return true;
            }

            var forInterfaceMember = other.ContainingType.FindImplementationForInterfaceMember(this.method);
            if (forInterfaceMember != null &&
                other.Equals(forInterfaceMember))
            {
                return true;
            }

            if (this.method.OriginalDefinition != null &&
                this.method.OriginalDefinition.Equals(other))
            {
                return true;
            }

            return false;
        }
    }
}