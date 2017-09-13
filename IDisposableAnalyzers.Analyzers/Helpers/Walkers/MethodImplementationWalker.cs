namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class MethodImplementationWalker : CSharpSyntaxWalker
    {
        private static readonly Pool<MethodImplementationWalker> Cache = new Pool<MethodImplementationWalker>(
            () => new MethodImplementationWalker(),
            x =>
            {
                x.implementations.Clear();
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
                x.method = null;
            });

        private readonly List<MethodDeclarationSyntax> implementations = new List<MethodDeclarationSyntax>();
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;
        private IMethodSymbol method;

        private MethodImplementationWalker()
        {
        }

        public IReadOnlyList<MethodDeclarationSyntax> Implementations => this.implementations;

        public static Pool<MethodImplementationWalker>.Pooled Create(IMethodSymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Cache.GetOrCreate();
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.method = symbol;
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
                        pooled.Item.Visit(root);
                    }
                }
            }

            return pooled;
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