namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ConstructorsWalker : CSharpSyntaxWalker
    {
        private static readonly Pool<ConstructorsWalker> Pool = new Pool<ConstructorsWalker>(
            () => new ConstructorsWalker(),
            x =>
            {
                x.nonPrivateCtors.Clear();
                x.objectCreations.Clear();
                x.Default = null;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
                x.type = null;
            });

        private readonly List<ConstructorDeclarationSyntax> nonPrivateCtors = new List<ConstructorDeclarationSyntax>();
        private readonly List<ObjectCreationExpressionSyntax> objectCreations = new List<ObjectCreationExpressionSyntax>();

        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;
        private INamedTypeSymbol type;

        private ConstructorsWalker()
        {
        }

        public IReadOnlyList<ConstructorDeclarationSyntax> NonPrivateCtors => this.nonPrivateCtors;

        public IReadOnlyList<ObjectCreationExpressionSyntax> ObjectCreations => this.objectCreations;

        public ConstructorDeclarationSyntax Default { get; private set; }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var ctor = this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken);
            if (SymbolComparer.Equals(this.type, ctor.ContainingType))
            {
                if (ctor.DeclaredAccessibility != Accessibility.Private)
                {
                    this.nonPrivateCtors.Add(node);
                }

                if (ctor.Parameters.Length == 0)
                {
                    this.Default = node;
                }
            }

            base.VisitConstructorDeclaration(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var ctor = this.semanticModel.GetSymbolSafe(node, this.cancellationToken) as IMethodSymbol;
            if (SymbolComparer.Equals(this.type, ctor?.ContainingType))
            {
                this.objectCreations.Add(node);
            }

            base.VisitObjectCreationExpression(node);
        }

        internal static Pool<ConstructorsWalker>.Pooled Create(TypeDeclarationSyntax context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.Visit(context);
            pooled.Item.type = semanticModel.GetDeclaredSymbolSafe(context, cancellationToken) as INamedTypeSymbol;
            if (pooled.Item.type == null)
            {
                return pooled;
            }

            foreach (var reference in pooled.Item.type.DeclaringSyntaxReferences)
            {
                pooled.Item.Visit(reference.GetSyntax(cancellationToken));
            }

            if (pooled.Item.nonPrivateCtors.Count == 0 &&
                pooled.Item.Default == null)
            {
                if (Constructor.TryGetDefault(pooled.Item.type, out IMethodSymbol @default))
                {
                    foreach (var reference in @default.DeclaringSyntaxReferences)
                    {
                        pooled.Item.Default = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                        pooled.Item.Visit(pooled.Item.Default);
                    }
                }
            }

            return pooled;
        }
    }
}