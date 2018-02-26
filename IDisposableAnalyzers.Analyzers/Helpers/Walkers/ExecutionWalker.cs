namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Walks code as it is executed.
    /// </summary>
    internal abstract class ExecutionWalker<T> : PooledWalker<T>
        where T : ExecutionWalker<T>
    {
        private readonly HashSet<SyntaxNode> visited = new HashSet<SyntaxNode>();

        protected Search Search { get; set; }

        protected SemanticModel SemanticModel { get; set; }

        protected CancellationToken CancellationToken { get; set; }

        public override void Visit(SyntaxNode node)
        {
            if (node is AnonymousFunctionExpressionSyntax)
            {
                switch (node.Parent.Kind())
                {
                    case SyntaxKind.AddAssignmentExpression:
                    case SyntaxKind.Argument:
                        break;
                    default:
                        return;
                }
            }

            base.Visit(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);
            this.VisitChained(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            base.VisitIdentifierName(node);
            if (this.Search == Search.Recursive)
            {
                if (this.TryGetPropertyGet(node, out IMethodSymbol getter))
                {
                    foreach (var reference in getter.DeclaringSyntaxReferences)
                    {
                        this.Visit(reference.GetSyntax(this.CancellationToken));
                    }
                }
            }
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            base.VisitAssignmentExpression(node);
            if (this.Search == Search.Recursive)
            {
                if (this.TryGetPropertySet(node.Left, out IMethodSymbol setter) &&
                    this.visited.Add(node))
                {
                    foreach (var reference in setter.DeclaringSyntaxReferences)
                    {
                        this.Visit(reference.GetSyntax(this.CancellationToken));
                    }
                }
            }
        }

        public override void VisitConstructorInitializer(ConstructorInitializerSyntax node)
        {
            base.VisitConstructorInitializer(node);
            this.VisitChained(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            base.VisitObjectCreationExpression(node);
            this.VisitChained(node);
        }

        protected override void Clear()
        {
            this.visited.Clear();
            this.SemanticModel = null;
            this.CancellationToken = CancellationToken.None;
        }

        protected void VisitChained(SyntaxNode node)
        {
            if (node == null)
            {
                return;
            }

            if (this.Search == Search.Recursive &&
                this.visited.Add(node))
            {
                var method = this.SemanticModel.GetSymbolSafe(node, this.CancellationToken);
                if (method == null)
                {
                    return;
                }

                foreach (var reference in method.DeclaringSyntaxReferences)
                {
                    this.Visit(reference.GetSyntax(this.CancellationToken));
                }
            }
        }

        private bool TryGetPropertySet(SyntaxNode node, out IMethodSymbol setter)
        {
            var property = this.SemanticModel.GetSymbolSafe(node, this.CancellationToken) as IPropertySymbol;
            setter = property?.SetMethod;
            return setter != null;
        }

        private bool TryGetPropertyGet(SyntaxNode node, out IMethodSymbol getter)
        {
            getter = null;
            var property = this.SemanticModel.GetSymbolSafe(node, this.CancellationToken) as IPropertySymbol;
            if (property?.GetMethod == null)
            {
                return false;
            }

            if (node.Parent is MemberAccessExpressionSyntax)
            {
                return this.TryGetPropertyGet(node.Parent, out getter);
            }

            if (node.Parent is ArgumentSyntax ||
                node.Parent is EqualsValueClauseSyntax)
            {
                getter = property.GetMethod;
            }

            return getter != null;
        }
    }
}
