namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignedValueWalker : PooledWalker<AssignedValueWalker>, IReadOnlyList<ExpressionSyntax>
    {
        private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
        private readonly MemberWalkers<IPropertySymbol> setterWalkers = new MemberWalkers<IPropertySymbol>();
        private readonly HashSet<SyntaxNode> visited = new HashSet<SyntaxNode>();
        private readonly HashSet<IParameterSymbol> refParameters = new HashSet<IParameterSymbol>(SymbolComparer.Default);
        private readonly PublicMemberWalker publicMemberWalker;

        private SyntaxNode context;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private AssignedValueWalker()
        {
            this.publicMemberWalker = new PublicMemberWalker(this);
        }

        public int Count => this.values.Count;

        internal ISymbol CurrentSymbol { get; private set; }

        public ExpressionSyntax this[int index] => this.values[index];

        public IEnumerator<ExpressionSyntax> GetEnumerator() => this.values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override void Visit(SyntaxNode node)
        {
            if (this.ShouldVisit(node).IsEither(Result.AssumeNo, Result.No))
            {
                return;
            }

            base.Visit(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            this.HandleAssignedValue(node, node.Initializer?.Value);
            base.VisitVariableDeclarator(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.Initializer != null)
            {
                if (this.visited.Add(node.Initializer))
                {
                    var ctor = this.semanticModel.GetSymbolSafe(node.Initializer, this.cancellationToken);
                    this.HandleInvoke(ctor, node.Initializer.ArgumentList);
                }
            }
            else
            {
                var ctor = this.semanticModel.GetDeclaredSymbolSafe(node, this.cancellationToken);
                if (Constructor.TryGetDefault(ctor?.ContainingType?.BaseType, out var baseCtor))
                {
                    this.HandleInvoke(baseCtor, null);
                }
            }

            var contextCtor = this.context?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (contextCtor != null)
            {
                if (contextCtor == node ||
                    node.IsRunBefore(contextCtor, this.semanticModel, this.cancellationToken))
                {
                    base.VisitConstructorDeclaration(node);
                }
            }
            else
            {
                base.VisitConstructorDeclaration(node);
            }
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            this.HandleAssignedValue(node.Left, node.Right);
            base.VisitAssignmentExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.LogicalNotExpression:
                    break;
                default:
                    this.HandleAssignedValue(node.Operand, node);
                    break;
            }

            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            this.HandleAssignedValue(node.Operand, node);
            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (this.visited.Add(node) &&
                this.semanticModel.GetSymbolSafe(node, this.cancellationToken) is IMethodSymbol method)
            {
                base.VisitInvocationExpression(node);
                if (this.context is ElementAccessExpressionSyntax &&
                    node.Expression is MemberAccessExpressionSyntax memberAccess &&
                    method.Name == "Add" &&
                    SymbolComparer.Equals(this.CurrentSymbol, this.semanticModel.GetSymbolSafe(memberAccess.Expression, this.cancellationToken)))
                {
                    if (method.ContainingType.Is(KnownSymbol.IDictionary) &&
                        node.ArgumentList?.Arguments.Count == 2)
                    {
                        this.values.Add(node.ArgumentList.Arguments[1].Expression);
                    }
                    else if (node.ArgumentList?.Arguments.Count == 1)
                    {
                        this.values.Add(node.ArgumentList.Arguments[0].Expression);
                    }
                }

                this.HandleInvoke(method, node.ArgumentList);
            }
        }

        public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            if (node.Parent is AssignmentExpressionSyntax assignment &&
                this.visited.Add(node) &&
                this.context is ElementAccessExpressionSyntax &&
                SymbolComparer.Equals(this.CurrentSymbol, this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken)))
            {
                this.values.Add(assignment.Right);
                base.VisitElementAccessExpression(node);
            }
        }

        public override void VisitArgument(ArgumentSyntax node)
        {
            if (this.visited.Add(node) &&
                node.Parent is ArgumentListSyntax argumentList)
            {
                if (node.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) &&
                    this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken) is ISymbol refSymbol &&
                    (SymbolComparer.Equals(this.CurrentSymbol, refSymbol) ||
                    this.refParameters.Contains(refSymbol as IParameterSymbol)) &&
                    this.semanticModel.GetSymbolSafe(argumentList.Parent, this.cancellationToken) is IMethodSymbol method &&
                    method.TryGetMatchingParameter(node, out var parameter))
                {
                    this.refParameters.Add(parameter);
                }
                else if (node.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) &&
                         this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken) is ISymbol outSymbol &&
                         (SymbolComparer.Equals(this.CurrentSymbol, outSymbol) ||
                          this.refParameters.Contains(outSymbol as IParameterSymbol)))
                {
                    this.values.Add(node.Expression);
                }
            }

            base.VisitArgument(node);
        }

        internal static AssignedValueWalker Borrow(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return Borrow(property, null, semanticModel, cancellationToken);
        }

        internal static AssignedValueWalker Borrow(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return Borrow(field, null, semanticModel, cancellationToken);
        }

        internal static AssignedValueWalker Borrow(ExpressionSyntax value, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (value is ElementAccessExpressionSyntax elementAccess)
            {
                return Borrow(semanticModel.GetSymbolSafe(elementAccess.Expression, cancellationToken), elementAccess, semanticModel, cancellationToken);
            }

            var symbol = semanticModel.GetSymbolSafe(value, cancellationToken);
            if (symbol is IFieldSymbol ||
                symbol is IPropertySymbol ||
                symbol is ILocalSymbol ||
                symbol is IParameterSymbol)
            {
                return Borrow(symbol, value, semanticModel, cancellationToken);
            }

            return Borrow(() => new AssignedValueWalker());
        }

        internal static AssignedValueWalker Borrow(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (symbol is IFieldSymbol ||
                symbol is IPropertySymbol ||
                symbol is ILocalSymbol ||
                symbol is IParameterSymbol)
            {
                return Borrow(symbol, null, semanticModel, cancellationToken);
            }

            return Borrow(() => new AssignedValueWalker());
        }

        internal static AssignedValueWalker Borrow(ISymbol symbol, SyntaxNode context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (symbol == null)
            {
                return Borrow(() => new AssignedValueWalker());
            }

            var pooled = Borrow(() => new AssignedValueWalker());
            pooled.CurrentSymbol = symbol;
            pooled.context = context;
            pooled.semanticModel = semanticModel;
            pooled.cancellationToken = cancellationToken;
            if (context != null)
            {
                pooled.Run();
            }
            else
            {
                foreach (var reference in symbol.DeclaringSyntaxReferences)
                {
                    pooled.context = symbol is IFieldSymbol || symbol is IPropertySymbol
                                              ? reference.GetSyntax(cancellationToken)
                                                         .FirstAncestor<TypeDeclarationSyntax>()
                                              : reference.GetSyntax(cancellationToken)
                                                         .FirstAncestor<MemberDeclarationSyntax>();
                    pooled.Run();
                }
            }

            pooled.values.PurgeDuplicates();
            return pooled;
        }

        internal void HandleInvoke(ISymbol method, ArgumentListSyntax argumentList)
        {
            if (method != null)
            {
                var before = this.values.Count;
                if (method.ContainingType.Is(this.CurrentSymbol.ContainingType) ||
                    this.CurrentSymbol.ContainingType.Is(method.ContainingType))
                {
                    foreach (var reference in method.DeclaringSyntaxReferences)
                    {
                        base.Visit(reference.GetSyntax(this.cancellationToken));
                    }
                }

                if (before != this.values.Count &&
                    argumentList != null)
                {
                    for (var i = before; i < this.values.Count; i++)
                    {
                        if (this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken) is IParameterSymbol parameter &&
                            parameter.RefKind != RefKind.Out)
                        {
                            if (argumentList.TryGetArgumentValue(parameter, this.cancellationToken, out var arg))
                            {
                                this.values[i] = arg;
                            }
                        }
                    }
                }
            }
        }

        protected override void Clear()
        {
            this.values.Clear();
            this.visited.Clear();
            this.refParameters.Clear();
            this.CurrentSymbol = null;
            this.context = null;
            this.semanticModel = null;
            this.cancellationToken = CancellationToken.None;
            this.setterWalkers.Clear();
        }

        private void Run()
        {
            if (this.CurrentSymbol == null)
            {
                return;
            }

            if (this.CurrentSymbol is ILocalSymbol local)
            {
                var declaration = local.DeclaringSyntaxReferences.Single().GetSyntax(this.cancellationToken);
                var scope = (SyntaxNode)declaration.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() ??
                                        declaration.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                if (scope != null)
                {
                    this.Visit(scope);
                }

                return;
            }

            if (this.CurrentSymbol is IParameterSymbol)
            {
                var scope = (SyntaxNode)this.context?.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() ??
                                        this.context?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                if (scope != null)
                {
                    this.Visit(scope);
                }

                return;
            }

            if (this.CurrentSymbol is IFieldSymbol ||
                this.CurrentSymbol is IPropertySymbol)
            {
                var type = (INamedTypeSymbol)this.semanticModel.GetDeclaredSymbolSafe(this.context?.FirstAncestorOrSelf<TypeDeclarationSyntax>(), this.cancellationToken);
                if (type == null)
                {
                    return;
                }

                if (this.CurrentSymbol is IFieldSymbol &&
                    this.CurrentSymbol.TrySingleDeclaration(this.cancellationToken, out FieldDeclarationSyntax fieldDeclarationSyntax))
                {
                    this.Visit(fieldDeclarationSyntax);
                }
                else if (this.CurrentSymbol is IPropertySymbol &&
                    this.CurrentSymbol.TrySingleDeclaration(this.cancellationToken, out PropertyDeclarationSyntax propertyDeclaration) &&
                    propertyDeclaration.Initializer != null)
                {
                    this.values.Add(propertyDeclaration.Initializer.Value);
                }

                var contextCtor = this.context?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
                foreach (var reference in type.DeclaringSyntaxReferences)
                {
                    using (var ctorWalker = ConstructorsWalker.Borrow((TypeDeclarationSyntax)reference.GetSyntax(this.cancellationToken), this.semanticModel, this.cancellationToken))
                    {
                        foreach (var creation in ctorWalker.ObjectCreations)
                        {
                            if (this.visited.Add(creation))
                            {
                                if (contextCtor == null ||
                                    creation.Creates(contextCtor, Search.Recursive, this.semanticModel, this.cancellationToken))
                                {
                                    this.VisitObjectCreationExpression(creation);
                                    var method = this.semanticModel.GetSymbolSafe(creation, this.cancellationToken);
                                    this.HandleInvoke(method, creation.ArgumentList);
                                }
                            }
                        }

                        if (contextCtor != null)
                        {
                            foreach (var initializer in ctorWalker.Initializers)
                            {
                                var other = (ConstructorDeclarationSyntax)initializer.Parent;
                                if (Constructor.IsRunBefore(contextCtor, other, this.semanticModel, this.cancellationToken))
                                {
                                    this.Visit(other);
                                }
                            }

                            if (!contextCtor.Modifiers.Any(SyntaxKind.PrivateKeyword))
                            {
                                this.Visit(contextCtor);
                            }

                            return;
                        }

                        if (ctorWalker.Default != null)
                        {
                            this.Visit(ctorWalker.Default);
                        }

                        foreach (var ctor in ctorWalker.NonPrivateCtors)
                        {
                            this.Visit(ctor);
                        }
                    }
                }

                if ((this.CurrentSymbol is IFieldSymbol field &&
                     !field.IsReadOnly) ||
                    (this.CurrentSymbol is IPropertySymbol property &&
                     !property.IsReadOnly))
                {
                    var scope = (SyntaxNode)this.context?.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() ??
                                this.context?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                    if (scope != null &&
                        !(scope is ConstructorDeclarationSyntax))
                    {
                        while (type.Is(this.CurrentSymbol.ContainingType))
                        {
                            foreach (var reference in type.DeclaringSyntaxReferences)
                            {
                                var typeDeclaration = (TypeDeclarationSyntax)reference.GetSyntax(this.cancellationToken);
                                this.publicMemberWalker.Visit(typeDeclaration);
                            }

                            type = type.BaseType;
                        }
                    }
                }
            }
        }

        private void HandleAssignedValue(SyntaxNode assigned, ExpressionSyntax value)
        {
            if (value == null)
            {
                return;
            }

            if (assigned is VariableDeclaratorSyntax declarator &&
                declarator.Identifier.ValueText != this.CurrentSymbol.Name)
            {
                return;
            }

            if (this.CurrentSymbol.IsEither<ILocalSymbol, IParameterSymbol>() &&
                assigned is MemberAccessExpressionSyntax)
            {
                return;
            }

            if (this.context is ElementAccessExpressionSyntax)
            {
                switch (value)
                {
                    case ArrayCreationExpressionSyntax arrayCreation:
                        {
                            if (arrayCreation.Initializer == null)
                            {
                                return;
                            }

                            foreach (var item in arrayCreation.Initializer.Expressions)
                            {
                                this.values.Add(item);
                            }
                        }

                        break;

                    case ObjectCreationExpressionSyntax objectCreation:
                        {
                            if (objectCreation.Initializer == null)
                            {
                                return;
                            }

                            foreach (var item in objectCreation.Initializer.Expressions)
                            {
                                if (item is InitializerExpressionSyntax kvp)
                                {
                                    if (kvp.Expressions.Count == 2)
                                    {
                                        this.values.Add(kvp.Expressions[1]);
                                    }
                                }
                                else if (item is AssignmentExpressionSyntax assignment)
                                {
                                    this.values.Add(assignment.Right);
                                }
                                else
                                {
                                    this.values.Add(item);
                                }
                            }
                        }

                        break;

                    case InitializerExpressionSyntax initializer:
                        {
                            foreach (var item in initializer.Expressions)
                            {
                                this.values.Add(item);
                            }
                        }

                        break;
                    default:
                        return;
                }

                return;
            }

            if (TryGetSetterWalker(out var setterWalker))
            {
                foreach (var nested in setterWalker.values)
                {
                    if (nested is IdentifierNameSyntax identifierName &&
                        identifierName.Identifier.ValueText == "value")
                    {
                        this.values.Add(value);
                    }
                    else
                    {
                        this.values.Add(nested);
                    }
                }

                return;
            }

            var assignedSymbol = this.semanticModel.GetSymbolSafe(assigned, this.cancellationToken) ??
                                 this.semanticModel.GetDeclaredSymbolSafe(assigned, this.cancellationToken);
            if (assignedSymbol == null)
            {
                return;
            }

            if (SymbolComparer.Equals(this.CurrentSymbol, assignedSymbol) ||
                     this.refParameters.Contains(assignedSymbol as IParameterSymbol))
            {
                this.values.Add(value);
            }

            bool TryGetSetterWalker(out AssignedValueWalker walker)
            {
                walker = null;
                if (!this.CurrentSymbol.IsEither<IFieldSymbol, IPropertySymbol>())
                {
                    return false;
                }

                if (TryGetProperty(out var property) &&
                    !SymbolComparer.Equals(this.CurrentSymbol, property) &&
                    property.ContainingType.Is(this.CurrentSymbol.ContainingType))
                {
                    if (this.setterWalkers.TryGetValue(property, out walker))
                    {
                        return walker != null;
                    }

                    if (property.TrySingleDeclaration(this.cancellationToken, out var declaration) &&
                        declaration.TryGetSetAccessorDeclaration(out var setter))
                    {
                        walker = Borrow(() => new AssignedValueWalker());
                        this.setterWalkers.Add(property, walker);
                        walker.CurrentSymbol = this.CurrentSymbol;
                        walker.semanticModel = this.semanticModel;
                        walker.cancellationToken = this.cancellationToken;
                        walker.context = setter;
                        walker.setterWalkers.Parent = this.setterWalkers;
                        walker.Visit(setter);
                    }
                }

                return walker != null;

                bool TryGetProperty(out IPropertySymbol result)
                {
                    if (assigned is IdentifierNameSyntax identifierName &&
                        this.CurrentSymbol.ContainingType.TryGetPropertyRecursive(identifierName.Identifier.ValueText, out result))
                    {
                        return true;
                    }

                    if (assigned is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Expression is InstanceExpressionSyntax &&
                        this.CurrentSymbol.ContainingType.TryGetPropertyRecursive(memberAccess.Name.Identifier.ValueText, out result))
                    {
                        return true;
                    }

                    result = null;
                    return false;
                }
            }
        }

        private Result ShouldVisit(SyntaxNode node)
        {
            if (this.CurrentSymbol.IsEither<IFieldSymbol, IPropertySymbol>())
            {
                if (node is ExpressionStatementSyntax &&
                    this.context.SharesAncestor<ConstructorDeclarationSyntax>(node) &&
                    node.FirstAncestor<AnonymousFunctionExpressionSyntax>() == null)
                {
                    return node.IsExecutedBefore(this.context);
                }

                return Result.Yes;
            }

            if (this.CurrentSymbol.IsEither<ILocalSymbol, IParameterSymbol>())
            {
                if (node.FirstAncestor<AnonymousFunctionExpressionSyntax>() is AnonymousFunctionExpressionSyntax lambda)
                {
                    if (this.CurrentSymbol is ILocalSymbol local &&
                        local.TrySingleDeclaration(this.cancellationToken, out var declaration) &&
                        lambda.Contains(declaration))
                    {
                        return node.IsExecutedBefore(this.context);
                    }

                    return Result.Yes;
                }

                return node.IsExecutedBefore(this.context);
            }

            if (this.context is InvocationExpressionSyntax &&
                ReferenceEquals(node, this.context))
            {
                return Result.No;
            }

            return Result.Yes;
        }

        private class PublicMemberWalker : CSharpSyntaxWalker
        {
            private readonly AssignedValueWalker inner;

            public PublicMemberWalker(AssignedValueWalker inner)
            {
                this.inner = inner;
            }

            public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                // Don't walk ctor.
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (!node.Modifiers.Any(SyntaxKind.PrivateKeyword))
                {
                    base.VisitPropertyDeclaration(node);
                }
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (!node.Modifiers.Any(SyntaxKind.PrivateKeyword))
                {
                    this.inner.VisitMethodDeclaration(node);
                }
            }

            public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
            {
                if (!node.Modifiers.Any(SyntaxKind.PrivateKeyword))
                {
                    this.inner.VisitAccessorDeclaration(node);
                }
            }

            public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
            {
                this.inner.VisitArrowExpressionClause(node);
            }
        }

        private class MemberWalkers<T>
            where T : ISymbol
        {
            private readonly Dictionary<T, AssignedValueWalker> map = new Dictionary<T, AssignedValueWalker>();

            public MemberWalkers<T> Parent { get; set; }

            private Dictionary<T, AssignedValueWalker> Current => this.Parent?.Current ??
                                                                  this.map;

            public void Add(T property, AssignedValueWalker walker)
            {
                this.Current.Add(property, walker);
            }

            public bool TryGetValue(T property, out AssignedValueWalker walker)
            {
                return this.Current.TryGetValue(property, out walker);
            }

            public void Clear()
            {
                if (this.map != null)
                {
                    foreach (var propertyWalker in this.map)
                    {
                        propertyWalker.Value?.Dispose();
                    }

                    this.map.Clear();
                }

                this.Parent = null;
            }
        }
    }
}
