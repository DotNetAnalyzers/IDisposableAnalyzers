namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ConstructorsWalker : PooledWalker<ConstructorsWalker>
    {
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

        internal static ConstructorsWalker Borrow(TypeDeclarationSyntax context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = Borrow(() => new ConstructorsWalker());
            walker.semanticModel = semanticModel;
            walker.cancellationToken = cancellationToken;
            walker.Visit(context);
            walker.type = semanticModel.GetDeclaredSymbolSafe(context, cancellationToken) as INamedTypeSymbol;
            if (walker.type == null)
            {
                return walker;
            }

            foreach (var reference in walker.type.DeclaringSyntaxReferences)
            {
                walker.Visit(reference.GetSyntax(cancellationToken));
            }

            if (walker.nonPrivateCtors.Count == 0 &&
                walker.Default == null)
            {
                if (Constructor.TryGetDefault(walker.type, out var @default))
                {
                    foreach (var reference in @default.DeclaringSyntaxReferences)
                    {
                        walker.Default = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                        walker.Visit(walker.Default);
                    }
                }
            }

            return walker;
        }

        protected override void Clear()
        {
            this.nonPrivateCtors.Clear();
            this.objectCreations.Clear();
            this.Default = null;
            this.semanticModel = null;
            this.cancellationToken = CancellationToken.None;
            this.type = null;
        }
    }
}