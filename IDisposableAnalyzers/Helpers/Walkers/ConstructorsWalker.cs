namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ConstructorsWalker : PooledWalker<ConstructorsWalker>
    {
        private readonly List<ConstructorDeclarationSyntax> nonPrivateCtors = new List<ConstructorDeclarationSyntax>();
        private readonly List<ObjectCreationExpressionSyntax> objectCreations = new List<ObjectCreationExpressionSyntax>();
        private readonly List<ConstructorInitializerSyntax> initializers = new List<ConstructorInitializerSyntax>();

        private SemanticModel semanticModel = null!;
        private CancellationToken cancellationToken;
        private TypeDeclarationSyntax context = null!;
        private INamedTypeSymbol? type;

        private ConstructorsWalker()
        {
        }

        internal IReadOnlyList<ConstructorDeclarationSyntax> NonPrivateCtors => this.nonPrivateCtors;

        internal IReadOnlyList<ObjectCreationExpressionSyntax> ObjectCreations => this.objectCreations;

        internal IReadOnlyList<ConstructorInitializerSyntax> Initializers => this.initializers;

        internal ConstructorDeclarationSyntax? Default { get; private set; }

        private INamedTypeSymbol Type => this.type ?? (this.type = this.semanticModel.GetDeclaredSymbolSafe(this.context, this.cancellationToken) as INamedTypeSymbol);

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (ReferenceEquals(this.context, node.Parent))
            {
                if (!node.Modifiers.Any(SyntaxKind.PrivateKeyword))
                {
                    this.nonPrivateCtors.Add(node);
                }

                if (node.ParameterList?.Parameters.Count == 0)
                {
                    this.Default = node;
                }
            }

            base.VisitConstructorDeclaration(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (node.Type is SimpleNameSyntax typeName &&
                typeName.Identifier.ValueText == this.context.Identifier.ValueText)
            {
                if (this.semanticModel.TryGetSymbol(node, this.cancellationToken, out var ctor) &&
                    this.Type.Equals(ctor.ContainingType))
                {
                    this.objectCreations.Add(node);
                }
            }
            else if (node.Type is QualifiedNameSyntax qualifiedName &&
                     qualifiedName.Right.Identifier.ValueText == this.context.Identifier.ValueText)
            {
                if (this.semanticModel.TryGetSymbol(node, this.cancellationToken, out var ctor) &&
                    this.Type.Equals(ctor.ContainingType))
                {
                    this.objectCreations.Add(node);
                }
            }
            else if (this.semanticModel.TryGetSymbol(node, this.cancellationToken, out var ctor) &&
                     this.Type.Equals(ctor.ContainingType))
            {
                this.objectCreations.Add(node);
            }

            base.VisitObjectCreationExpression(node);
        }

        public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            if (node.Parent?.Parent == this.context)
            {
                this.initializers.Add(node);
            }

            base.VisitConstructorInitializer(node);
        }

        internal static ConstructorsWalker Borrow(TypeDeclarationSyntax context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = Borrow(() => new ConstructorsWalker());
            walker.semanticModel = semanticModel;
            walker.cancellationToken = cancellationToken;
            walker.context = context;

            if (context.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                foreach (var reference in walker.Type.DeclaringSyntaxReferences)
                {
                    walker.Visit(reference.GetSyntax(cancellationToken));
                }
            }
            else
            {
                walker.Visit(context);
                if (context is StructDeclarationSyntax)
                {
                    return walker;
                }

                if (context is ClassDeclarationSyntax classDeclaration &&
                    classDeclaration.BaseList == null)
                {
                    return walker;
                }
            }

            if (walker.nonPrivateCtors.Count == 0 &&
                walker.Default == null)
            {
                if (Constructor.TryFindDefault(walker.Type, Search.Recursive, out var defaultCtor) &&
                    defaultCtor.TrySingleDeclaration(cancellationToken, out ConstructorDeclarationSyntax defaultCtorDeclaration))
                {
                    walker.Default = defaultCtorDeclaration;
                    if (!defaultCtorDeclaration.Modifiers.Any(SyntaxKind.PrivateKeyword))
                    {
                        walker.nonPrivateCtors.Add(defaultCtorDeclaration);
                    }

                    walker.Visit(walker.Default);
                }
            }

            return walker;
        }

        protected override void Clear()
        {
            this.nonPrivateCtors.Clear();
            this.objectCreations.Clear();
            this.initializers.Clear();
            this.Default = null!;
            this.semanticModel = null!;
            this.cancellationToken = CancellationToken.None;
            this.type = null!;
            this.context = null!;
        }
    }
}
