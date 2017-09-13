namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class CallsWalker : CSharpSyntaxWalker
    {
        private static readonly Pool<CallsWalker> Pool = new Pool<CallsWalker>(
            () => new CallsWalker(),
            x =>
                {
                    x.invocations.Clear();
                    x.initializers.Clear();
                    x.objectCreations.Clear();
                    x.ctors.Clear();
                    x.method = null;
                    x.contextType = null;
                    x.semanticModel = null;
                    x.cancellationToken = CancellationToken.None;
                });

        private readonly List<InvocationExpressionSyntax> invocations = new List<InvocationExpressionSyntax>();
        private readonly List<ConstructorInitializerSyntax> initializers = new List<ConstructorInitializerSyntax>();
        private readonly List<ObjectCreationExpressionSyntax> objectCreations = new List<ObjectCreationExpressionSyntax>();
        private readonly HashSet<IMethodSymbol> ctors = new HashSet<IMethodSymbol>(SymbolComparer.Default);

        private IMethodSymbol method;
        private ITypeSymbol contextType;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private CallsWalker()
        {
        }

        public IReadOnlyList<InvocationExpressionSyntax> Invocations => this.invocations;

        public IReadOnlyList<ConstructorInitializerSyntax> Initializers => this.initializers;

        public IReadOnlyList<ObjectCreationExpressionSyntax> ObjectCreations => this.objectCreations;

        public static Pool<CallsWalker>.Pooled GetCallsInContext(IMethodSymbol method, SyntaxNode context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.method = method;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            Constructor.AddRunBefore(context, pooled.Item.ctors, semanticModel, cancellationToken);
            var contextCtor = semanticModel.GetDeclaredSymbolSafe(
                context.FirstAncestorOrSelf<ConstructorDeclarationSyntax>(),
                cancellationToken);
            if (contextCtor != null)
            {
                pooled.Item.ctors.Add(contextCtor).IgnoreReturnValue();
            }

            pooled.Item.contextType = semanticModel.GetDeclaredSymbolSafe(context.FirstAncestor<TypeDeclarationSyntax>(), cancellationToken);
            var type = pooled.Item.contextType;
            while (type != null && type != KnownSymbol.Object)
            {
                foreach (var reference in type.DeclaringSyntaxReferences)
                {
                    pooled.Item.Visit(reference.GetSyntax(cancellationToken));
                }

                type = type.BaseType;
            }

            return pooled;
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var invokedMethod = this.semanticModel.GetSymbolSafe(node, this.cancellationToken) as IMethodSymbol;
            if (SymbolComparer.Equals(this.method, invokedMethod))
            {
                this.invocations.Add(node);
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            if (this.method.MethodKind == MethodKind.Constructor)
            {
                var calledCtor = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                if (this.IsValidCtor(calledCtor))
                {
                    var callingCtor = this.semanticModel.GetDeclaredSymbolSafe(node.Parent, this.cancellationToken) as IMethodSymbol;
                    if (SymbolComparer.Equals(calledCtor.ContainingType, callingCtor?.ContainingType))
                    {
                        this.initializers.Add(node);
                    }
                    else if (this.ctors.Contains(callingCtor))
                    {
                        this.initializers.Add(node);
                    }
                }
            }
            else
            {
                this.initializers.Add(node);
            }

            base.VisitConstructorInitializer(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (this.method.MethodKind == MethodKind.Constructor)
            {
                var ctor = this.semanticModel.GetSymbolSafe(node, this.cancellationToken) as IMethodSymbol;
                if (this.IsValidCtor(ctor))
                {
                    this.objectCreations.Add(node);
                }
            }
            else
            {
                this.objectCreations.Add(node);
            }

            base.VisitObjectCreationExpression(node);
        }

        private bool IsValidCtor(IMethodSymbol ctor)
        {
            if (!SymbolComparer.Equals(this.method, ctor))
            {
                return false;
            }

            if (SymbolComparer.Equals(this.contextType, ctor?.ContainingType))
            {
                return true;
            }

            return this.ctors.Contains(ctor);
        }
    }
}