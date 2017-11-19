namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignedValueWalker : PooledWalker<AssignedValueWalker>, IReadOnlyList<ExpressionSyntax>
    {
        private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
        private readonly HashSet<SyntaxNode> visitedLocations = new HashSet<SyntaxNode>();
        private readonly HashSet<IParameterSymbol> refParameters = new HashSet<IParameterSymbol>(SymbolComparer.Default);
        private readonly MemberWalker memberWalker;

        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private AssignedValueWalker()
        {
            this.memberWalker = new MemberWalker(this);
        }

        public int Count => this.values.Count;

        internal ISymbol CurrentSymbol { get; private set; }

        internal SyntaxNode Context { get; private set; }

        public ExpressionSyntax this[int index] => this.values[index];

        public IEnumerator<ExpressionSyntax> GetEnumerator() => this.values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override void Visit(SyntaxNode node)
        {
            if (this.ShouldVisit(node) != Result.Yes)
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
                if (this.visitedLocations.Add(node.Initializer))
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

            var contextCtor = this.Context?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
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
            if (this.visitedLocations.Add(node))
            {
                base.VisitInvocationExpression(node);
                var method = this.semanticModel.GetSymbolSafe(node, this.cancellationToken);
                if (this.Context is ElementAccessExpressionSyntax &&
                    SymbolComparer.Equals(this.CurrentSymbol, this.semanticModel.GetSymbolSafe((node.Expression as MemberAccessExpressionSyntax)?.Expression, this.cancellationToken)))
                {
                    if (method.Name == "Add")
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
                }

                this.HandleInvoke(method, node.ArgumentList);
            }
        }

        public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            if (node.Parent is AssignmentExpressionSyntax assignment &&
                this.visitedLocations.Add(node) &&
                this.Context is ElementAccessExpressionSyntax &&
                SymbolComparer.Equals(this.CurrentSymbol, this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken)))
            {
                this.values.Add(assignment.Right);
                base.VisitElementAccessExpression(node);
            }
        }

        public override void VisitArgument(ArgumentSyntax node)
        {
            if (this.visitedLocations.Add(node) &&
                (node.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) ||
                 node.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword)))
            {
                var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                var argSymbol = this.semanticModel.GetSymbolSafe(node.Expression, this.cancellationToken);
                if (invocation != null &&
                    (SymbolComparer.Equals(this.CurrentSymbol, argSymbol) ||
                     this.refParameters.Contains(argSymbol as IParameterSymbol)))
                {
                    var method = this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
                    if (method != null &&
                        method.DeclaringSyntaxReferences.Length > 0)
                    {
                        foreach (var reference in method.DeclaringSyntaxReferences)
                        {
                            var methodDeclaration = reference.GetSyntax(this.cancellationToken) as MethodDeclarationSyntax;
                            if (methodDeclaration.TryGetMatchingParameter(node, out var parameterSyntax))
                            {
                                var parameterSymbol = this.semanticModel.GetDeclaredSymbolSafe(parameterSyntax, this.cancellationToken);
                                if (parameterSymbol != null)
                                {
                                    if (node.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword))
                                    {
                                        this.refParameters.Add(parameterSymbol).IgnoreReturnValue();
                                    }

                                    if (node.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
                                    {
                                        this.values.Add(node.Expression);
                                    }
                                }
                            }
                        }
                    }
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
            pooled.Context = context;
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
                    pooled.Context = symbol is IFieldSymbol || symbol is IPropertySymbol
                                              ? reference.GetSyntax(cancellationToken)
                                                         .FirstAncestor<TypeDeclarationSyntax>()
                                              : reference.GetSyntax(cancellationToken)
                                                         .FirstAncestor<MemberDeclarationSyntax>();
                    pooled.Run();
                }
            }

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
            this.visitedLocations.Clear();
            this.refParameters.Clear();
            this.CurrentSymbol = null;
            this.Context = null;
            this.semanticModel = null;
            this.cancellationToken = CancellationToken.None;
        }

        private void Run()
        {
            if (this.CurrentSymbol == null)
            {
                return;
            }

            var type = (INamedTypeSymbol)this.semanticModel.GetDeclaredSymbolSafe(this.Context?.FirstAncestorOrSelf<TypeDeclarationSyntax>(), this.cancellationToken);
            if (type == null)
            {
                return;
            }

            if (this.CurrentSymbol is IFieldSymbol ||
                this.CurrentSymbol is IPropertySymbol)
            {
                if (this.CurrentSymbol is IFieldSymbol)
                {
                    foreach (var reference in this.CurrentSymbol.DeclaringSyntaxReferences)
                    {
                        var fieldDeclarationSyntax = reference.GetSyntax(this.cancellationToken)
                                                              ?.FirstAncestorOrSelf<FieldDeclarationSyntax>();
                        if (fieldDeclarationSyntax != null)
                        {
                            this.Visit(fieldDeclarationSyntax);
                        }
                    }
                }

                if (this.CurrentSymbol is IPropertySymbol)
                {
                    foreach (var reference in this.CurrentSymbol.DeclaringSyntaxReferences)
                    {
                        var propertyDeclarationSyntax = reference.GetSyntax(this.cancellationToken)
                                                                 ?.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
                        if (propertyDeclarationSyntax?.Initializer?.Value != null)
                        {
                            this.values.Add(propertyDeclarationSyntax.Initializer.Value);
                        }
                    }
                }

                var contextCtor = this.Context?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
                foreach (var reference in type.DeclaringSyntaxReferences)
                {
                    using (var ctorWalker = ConstructorsWalker.Borrow((TypeDeclarationSyntax)reference.GetSyntax(this.cancellationToken), this.semanticModel, this.cancellationToken))
                    {
                        if (this.Context?.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() == null &&
                            ctorWalker.Default != null)
                        {
                            this.Visit(ctorWalker.Default);
                        }

                        foreach (var creation in ctorWalker.ObjectCreations)
                        {
                            if (contextCtor == null ||
                                creation.Creates(contextCtor, Search.Recursive, this.semanticModel, this.cancellationToken))
                            {
                                if (this.visitedLocations.Add(creation))
                                {
                                    this.VisitObjectCreationExpression(creation);
                                    var method = this.semanticModel.GetSymbolSafe(creation, this.cancellationToken);
                                    this.HandleInvoke(method, creation.ArgumentList);
                                }
                            }
                        }

                        foreach (var ctor in ctorWalker.NonPrivateCtors)
                        {
                            if (contextCtor == null ||
                                ctor == contextCtor ||
                                contextCtor.IsRunBefore(ctor, this.semanticModel, this.cancellationToken))
                            {
                                this.Visit(ctor);
                            }
                        }
                    }
                }
            }

            var contextMember = this.Context?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (contextMember != null &&
                (this.CurrentSymbol is ILocalSymbol ||
                 this.CurrentSymbol is IParameterSymbol))
            {
                this.Visit(contextMember);
            }
            else if (!(contextMember is ConstructorDeclarationSyntax))
            {
                while (type.Is(this.CurrentSymbol.ContainingType))
                {
                    foreach (var reference in type.DeclaringSyntaxReferences)
                    {
                        var typeDeclaration = (TypeDeclarationSyntax)reference.GetSyntax(this.cancellationToken);
                        this.memberWalker.Visit(typeDeclaration);
                    }

                    type = type.BaseType;
                }
            }

            this.values.PurgeDuplicates();
        }

        private void HandleAssignedValue(SyntaxNode assignee, ExpressionSyntax value)
        {
            bool TryGetName(SyntaxNode n, out string name)
            {
                name = null;
                if (n is VariableDeclaratorSyntax declarator)
                {
                    name = declarator.Identifier.ValueText;
                    return true;
                }

                //if (n is IdentifierNameSyntax identifierName)
                //{
                //    name = identifierName.Identifier.ValueText;
                //    return true;
                //}

                //if (n is MemberAccessExpressionSyntax memberAccess &&
                //    memberAccess.Expression is InstanceExpressionSyntax &&
                //    memberAccess.Name is SimpleNameSyntax simpleName)
                //{
                //    name = simpleName.Identifier.ValueText;
                //    return true;
                //}

                return false;
            }

            if (value == null)
            {
                return;
            }

            if (this.Context is ElementAccessExpressionSyntax)
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

            if (TryGetName(assignee, out var assigneName) &&
                assigneName != this.CurrentSymbol.Name)
            {
                return;
            }

            var assignedSymbol = this.semanticModel.GetSymbolSafe(assignee, this.cancellationToken) ??
                                 this.semanticModel.GetDeclaredSymbolSafe(assignee, this.cancellationToken);
            if (assignedSymbol == null)
            {
                return;
            }

            if (assignedSymbol is IPropertySymbol property &&
                !SymbolComparer.Equals(this.CurrentSymbol, property) &&
                (this.CurrentSymbol is IFieldSymbol || this.CurrentSymbol is IPropertySymbol) &&
                Property.AssignsSymbolInSetter(property, this.CurrentSymbol, this.semanticModel, this.cancellationToken))
            {
                var before = this.values.Count;
                foreach (var reference in property.DeclaringSyntaxReferences)
                {
                    var declaration = (PropertyDeclarationSyntax)reference.GetSyntax(this.cancellationToken);
                    if (declaration.TryGetSetAccessorDeclaration(out var setter))
                    {
                        this.Visit(setter);
                    }
                }

                for (var i = before; i < this.values.Count; i++)
                {
                    var parameter = this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken) as IParameterSymbol;
                    if (Equals(parameter?.ContainingSymbol, property.SetMethod))
                    {
                        this.values[i] = value;
                    }
                }
            }
            else
            {
                if (SymbolComparer.Equals(this.CurrentSymbol, assignedSymbol) ||
                    this.refParameters.Contains(assignedSymbol as IParameterSymbol))
                {
                    this.values.Add(value);
                }
            }
        }

        private Result ShouldVisit(SyntaxNode node)
        {
            if (this.CurrentSymbol is IPropertySymbol ||
                this.CurrentSymbol is IFieldSymbol)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ExpressionStatement:
                        return this.Context.SharesAncestor<ConstructorDeclarationSyntax>(node)
                                   ? node.IsBeforeInScope(this.Context)
                                   : Result.Yes;
                    default:
                        return Result.Yes;
                }
            }

            if (this.Context is InvocationExpressionSyntax &&
                ReferenceEquals(node, this.Context))
            {
                return Result.No;
            }

            switch (node.Kind())
            {
                case SyntaxKind.ExpressionStatement:
                    return this.Context.SharesAncestor<MemberDeclarationSyntax>(node)
                               ? node.IsBeforeInScope(this.Context)
                               : Result.Yes;
                default:
                    return Result.Yes;
            }
        }

        private class MemberWalker : CSharpSyntaxWalker
        {
            private readonly AssignedValueWalker inner;

            public MemberWalker(AssignedValueWalker inner)
            {
                this.inner = inner;
            }

            public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (this.inner.semanticModel.GetDeclaredSymbolSafe(node, this.inner.cancellationToken)
                        ?.DeclaredAccessibility != Accessibility.Private)
                {
                    base.VisitPropertyDeclaration(node);
                }
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (this.inner.semanticModel.GetDeclaredSymbolSafe(node, this.inner.cancellationToken)
                        ?.DeclaredAccessibility != Accessibility.Private)
                {
                    this.inner.VisitMethodDeclaration(node);
                }
            }

            public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
            {
                if (this.inner.semanticModel.GetDeclaredSymbolSafe(node, this.inner.cancellationToken)
                        ?.DeclaredAccessibility != Accessibility.Private)
                {
                    this.inner.VisitAccessorDeclaration(node);
                }
            }

            public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
            {
                if (this.inner.semanticModel.GetDeclaredSymbolSafe(node, this.inner.cancellationToken)
                        ?.DeclaredAccessibility != Accessibility.Private)
                {
                    this.inner.VisitArrowExpressionClause(node);
                }
            }
        }
    }
}