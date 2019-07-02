namespace IDisposableAnalyzers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class AssignedValueWalker : PooledWalker<AssignedValueWalker>, IReadOnlyList<ExpressionSyntax>
    {
        private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
        private readonly List<ExpressionSyntax> outValues = new List<ExpressionSyntax>();
        private readonly MemberWalkers memberWalkers = new MemberWalkers();
        private readonly HashSet<IParameterSymbol> refParameters = new HashSet<IParameterSymbol>(SymbolComparer.Default);
        private readonly HashSet<IParameterSymbol> outParameters = new HashSet<IParameterSymbol>(SymbolComparer.Default);
        private readonly PublicMemberWalker publicMemberWalker;
        private readonly CtorArgWalker ctorArgWalker;

        private Context context;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private AssignedValueWalker()
        {
            this.publicMemberWalker = new PublicMemberWalker(this);
            this.ctorArgWalker = new CtorArgWalker(this);
        }

        public int Count => this.values.Count;

        internal ISymbol CurrentSymbol { get; private set; }

        public ExpressionSyntax this[int index] => this.values[index];

        public IEnumerator<ExpressionSyntax> GetEnumerator() => this.values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override void Visit(SyntaxNode node)
        {
            if (this.context.ShouldVisit(node))
            {
                base.Visit(node);
            }
            else
            {
                // ReSharper disable once RedundantJumpStatement for debugging, useful to set bp here.
                return;
            }
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            this.HandleAssignedValue(node, node.Initializer?.Value);
            base.VisitVariableDeclarator(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.Initializer == null &&
                this.semanticModel.TryGetSymbol(node, this.cancellationToken, out var ctor) &&
                ctor.ContainingType is INamedTypeSymbol containingType &&
                containingType.BaseType is INamedTypeSymbol baseType &&
                Constructor.TryFindDefault(baseType, Search.Recursive, out var baseCtor))
            {
                this.HandleInvoke(baseCtor, null);
            }
            else if (node.Initializer is ConstructorInitializerSyntax initializer &&
                     this.semanticModel.TryGetSymbol(initializer, this.cancellationToken, out var chained))
            {
                this.HandleInvoke(chained, node.Initializer.ArgumentList);
            }

            base.VisitConstructorDeclaration(node);
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
            if (this.semanticModel.TryGetSymbol(node, this.cancellationToken, out var method))
            {
                base.VisitInvocationExpression(node);
                if (this.context.Node is ElementAccessExpressionSyntax &&
                    method.Name == "Add" &&
                    MemberPath.TrySingle(node, out var member) &&
                    this.semanticModel.TryGetSymbol(member, this.cancellationToken, out ISymbol memberSymbol) &&
                    memberSymbol.Equals(this.CurrentSymbol))
                {
                    if (method.Parameters.TrySingle(out var parameter) &&
                        node.TryFindArgument(parameter, out var argument))
                    {
                        this.values.Add(argument.Expression);
                    }
                    else if (method.Parameters.Length == 2 &&
                             method.ContainingType.IsAssignableTo(KnownSymbol.IDictionary, this.semanticModel.Compilation) &&
                             method.TryFindParameter("value", out parameter) &&
                             node.TryFindArgument(parameter, out argument))
                    {
                        this.values.Add(argument.Expression);
                    }
                }

                this.HandleInvoke(method, node.ArgumentList);
            }
        }

        public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            if (node.Parent is AssignmentExpressionSyntax assignment &&
                this.context.Node is ElementAccessExpressionSyntax &&
                this.semanticModel.TryGetSymbol(node.Expression, this.cancellationToken, out ISymbol symbol) &&
                symbol.Equals(this.CurrentSymbol))
            {
                this.values.Add(assignment.Right);
                base.VisitElementAccessExpression(node);
            }
        }

        public override void VisitIsPatternExpression(IsPatternExpressionSyntax node)
        {
            this.HandleAssignedValue(node.Pattern, node.Expression);
            base.VisitIsPatternExpression(node);
        }

        public override void VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
        {
            if (node.Parent is SwitchSectionSyntax switchSection &&
                switchSection.Parent is SwitchStatementSyntax switchStatement)
            {
                this.HandleAssignedValue(node.Pattern, switchStatement.Expression);
            }

            base.VisitCasePatternSwitchLabel(node);
        }

        internal static AssignedValueWalker Borrow(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (property.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration.Parent is TypeDeclarationSyntax typeDeclaration)
            {
                return BorrowCore(property, typeDeclaration, semanticModel, cancellationToken);
            }

            return Borrow(() => new AssignedValueWalker());
        }

        internal static AssignedValueWalker Borrow(IFieldSymbol field, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (field.TrySingleDeclaration(cancellationToken, out var declaration) &&
                declaration.Parent is TypeDeclarationSyntax typeDeclaration)
            {
                return BorrowCore(field, typeDeclaration, semanticModel, cancellationToken);
            }

            return Borrow(() => new AssignedValueWalker());
        }

        internal static AssignedValueWalker Borrow(ExpressionSyntax value, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (value is ElementAccessExpressionSyntax elementAccess)
            {
                return BorrowCore(semanticModel.GetSymbolSafe(elementAccess.Expression, cancellationToken), elementAccess, semanticModel, cancellationToken);
            }

            if (semanticModel.TryGetSymbol(value, cancellationToken, out ISymbol symbol))
            {
                return BorrowCore(symbol, value, semanticModel, cancellationToken);
            }

            return Borrow(() => new AssignedValueWalker());
        }

        internal static AssignedValueWalker Borrow(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            switch (symbol)
            {
                case IFieldSymbol field:
                    return Borrow(field, semanticModel, cancellationToken);
                case IPropertySymbol property:
                    return Borrow(property, semanticModel, cancellationToken);
                case ILocalSymbol _:
                case IParameterSymbol _:
                    if (symbol.TrySingleDeclaration(cancellationToken, out SyntaxNode declaration) &&
                        declaration.TryFirstAncestor(out MemberDeclarationSyntax memberDeclaration))
                    {
                        return BorrowCore(symbol, memberDeclaration, semanticModel, cancellationToken);
                    }

                    break;
            }

            return Borrow(() => new AssignedValueWalker());
        }

        internal static AssignedValueWalker Borrow(ISymbol symbol, ExpressionSyntax context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return BorrowCore(symbol, context, semanticModel, cancellationToken);
        }

        internal void HandleInvoke(IMethodSymbol method, ArgumentListSyntax argumentList)
        {
            if (method != null &&
                (method.Parameters.TryFirst(x => x.RefKind != RefKind.None, out _) ||
                 this.CurrentSymbol.ContainingType.IsAssignableTo(method.ContainingType, this.semanticModel.Compilation)))
            {
                if (TryGetWalker(out var walker))
                {
                    foreach (var value in walker.values)
                    {
                        this.values.Add(GetArgumentValue(value));
                    }

                    foreach (var outValue in walker.outValues)
                    {
                        this.values.Add(GetArgumentValue(outValue));
                    }
                }
                else
                {
                    foreach (var outParameter in this.outParameters)
                    {
                        foreach (var argument in argumentList.Arguments)
                        {
                            if (argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) &&
                                argument.Expression is IdentifierNameSyntax identifierName &&
                                identifierName.Identifier.Text == outParameter.Name)
                            {
                                this.outValues.Add(GetArgumentValue(argument.Expression));
                            }
                        }
                    }

                    if (this.CurrentSymbol.IsEitherKind(SymbolKind.Local, SymbolKind.Parameter))
                    {
                        foreach (var argument in argumentList.Arguments)
                        {
                            if (argument.Expression is IdentifierNameSyntax identifierName &&
                                identifierName.Identifier.Text == this.CurrentSymbol.Name &&
                                argument.RefOrOutKeyword.IsEither(SyntaxKind.RefKeyword, SyntaxKind.OutKeyword))
                            {
                                this.values.Add(GetArgumentValue(argument.Expression));
                            }
                        }
                    }
                }
            }

            bool TryGetWalker(out AssignedValueWalker result)
            {
                result = null;
                if (TryGetKey(out var key))
                {
                    if (this.memberWalkers.TryGetValue(key, out result))
                    {
                        return false;
                    }

                    if (method.TrySingleDeclaration(this.cancellationToken, out BaseMethodDeclarationSyntax declaration))
                    {
                        result = Borrow(() => new AssignedValueWalker());
                        this.memberWalkers.Add(key, result);
                        result.CurrentSymbol = this.CurrentSymbol;
                        result.semanticModel = this.semanticModel;
                        result.cancellationToken = this.cancellationToken;
                        result.context = this.context.Node.TryFirstAncestor<ConstructorDeclarationSyntax>(out _)
                            ? this.context
                            : new Context(declaration, null);
                        result.memberWalkers.Parent = this.memberWalkers;
                        if (argumentList != null)
                        {
                            foreach (var argument in argumentList.Arguments)
                            {
                                if (argument.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword) &&
                                    TryGetMatchingParameter(argument, out var parameter))
                                {
                                    result.refParameters.Add(parameter);
                                }
                                else if (argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) &&
                                         TryGetMatchingParameter(argument, out parameter))
                                {
                                    result.outParameters.Add(parameter);
                                }
                            }
                        }

                        result.Visit(declaration);
                    }
                }

                return result != null;

                bool TryGetMatchingParameter(ArgumentSyntax argument, out IParameterSymbol parameter)
                {
                    parameter = null;
                    if (this.semanticModel.TryGetSymbol(argument.Expression, this.cancellationToken, out ISymbol candidate))
                    {
                        if (candidate.Equals(this.CurrentSymbol) ||
                            this.refParameters.Contains(candidate as IParameterSymbol) ||
                            this.outParameters.Contains(candidate as IParameterSymbol))
                        {
                            return method.TryFindParameter(argument, out parameter);
                        }
                    }

                    return false;
                }

                bool TryGetKey(out SyntaxNode node)
                {
                    if (argumentList != null)
                    {
                        node = argumentList;
                        return true;
                    }

                    return method.TrySingleDeclaration(this.cancellationToken, out node);
                }
            }

            ExpressionSyntax GetArgumentValue(ExpressionSyntax value)
            {
                if (value is IdentifierNameSyntax identifierName &&
                    method.TryFindParameter(identifierName.Identifier.ValueText, out var parameter) &&
                    parameter.RefKind != RefKind.Out)
                {
                    if (argumentList.TryFind(parameter, out var argument))
                    {
                        return argument.Expression;
                    }

                    if (parameter.HasExplicitDefaultValue &&
                        parameter.TrySingleDeclaration(this.cancellationToken, out var parameterDeclaration))
                    {
                        return parameterDeclaration.Default?.Value;
                    }
                }

                return value;
            }
        }

        internal void RemoveAll(Predicate<ExpressionSyntax> match)
        {
            this.values.RemoveAll(match);
        }

        protected override void Clear()
        {
            this.values.Clear();
            this.outValues.Clear();
            this.refParameters.Clear();
            this.outParameters.Clear();
            this.CurrentSymbol = null;
            this.context = default(Context);
            this.semanticModel = null;
            this.cancellationToken = CancellationToken.None;
            this.memberWalkers.Clear();
        }

        private static AssignedValueWalker BorrowCore(ISymbol symbol, SyntaxNode context, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (symbol == null || context == null)
            {
                return Borrow(() => new AssignedValueWalker());
            }

            var pooled = Borrow(() => new AssignedValueWalker());
            pooled.CurrentSymbol = symbol;
            pooled.context = Context.Create(context, symbol, cancellationToken);
            pooled.semanticModel = semanticModel;
            pooled.cancellationToken = cancellationToken;
            pooled.Run();
            pooled.values.PurgeDuplicates();
            return pooled;
        }

        private void Run()
        {
            if (this.CurrentSymbol == null)
            {
                return;
            }

            if (this.CurrentSymbol is ILocalSymbol local &&
                local.TrySingleDeclaration(this.cancellationToken, out var declaration) &&
                TryGetScope(declaration, out var scope))
            {
                this.Visit(scope);
            }
            else if (this.CurrentSymbol.Kind == SymbolKind.Discard &&
                     TryGetScope(this.context.Node, out scope))
            {
                this.Visit(scope);
            }
            else if (this.CurrentSymbol.Kind == SymbolKind.Parameter &&
                     TryGetScope(this.context.Node, out scope))
            {
                this.Visit(scope);
            }
            else if (this.CurrentSymbol.IsEitherKind(SymbolKind.Field, SymbolKind.Property) &&
                     this.context.Node.TryFirstAncestorOrSelf(out TypeDeclarationSyntax typeDeclaration) &&
                     this.semanticModel.TryGetSymbol(typeDeclaration, this.cancellationToken, out var type))
            {
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

                foreach (var reference in type.DeclaringSyntaxReferences)
                {
                    using (var ctorWalker = ConstructorsWalker.Borrow((TypeDeclarationSyntax)reference.GetSyntax(this.cancellationToken), this.semanticModel, this.cancellationToken))
                    {
                        if (this.context.Node.TryFirstAncestorOrSelf<ConstructorDeclarationSyntax>(out var contextCtor))
                        {
                            this.Visit(contextCtor);
                            if (contextCtor.ParameterList is ParameterListSyntax parameterList &&
                                parameterList.Parameters.Any())
                            {
                                foreach (var creation in ctorWalker.ObjectCreations)
                                {
                                    this.ctorArgWalker.Visit(creation);
                                }

                                foreach (var ctor in ctorWalker.NonPrivateCtors)
                                {
                                    this.ctorArgWalker.Visit(ctor);
                                }

                                if (contextCtor.Modifiers.Any(SyntaxKind.PrivateKeyword))
                                {
                                    this.values.RemoveAll(
                                        x => x is IdentifierNameSyntax identifierName &&
                                             x.TryFirstAncestorOrSelf<ConstructorDeclarationSyntax>(out var ctor) &&
                                             ctor == contextCtor &&
                                             parameterList.TryFind(identifierName.Identifier.ValueText, out _));
                                }
                            }
                        }
                        else
                        {
                            foreach (var creation in ctorWalker.ObjectCreations)
                            {
                                this.VisitObjectCreationExpression(creation);
                                var method = this.semanticModel.GetSymbolSafe(creation, this.cancellationToken);
                                this.HandleInvoke(method, creation.ArgumentList);
                            }

                            foreach (var ctor in ctorWalker.NonPrivateCtors)
                            {
                                this.Visit(ctor);
                            }
                        }
                    }
                }

                if ((this.CurrentSymbol is IFieldSymbol field && !field.IsReadOnly) ||
                    (this.CurrentSymbol is IPropertySymbol property && !property.IsReadOnly))
                {
                    if (TryGetScope(this.context.Node, out scope) &&
                        !(scope is ConstructorDeclarationSyntax))
                    {
                        while (type.IsAssignableTo(this.CurrentSymbol.ContainingType, this.semanticModel.Compilation))
                        {
                            foreach (var reference in type.DeclaringSyntaxReferences)
                            {
                                this.publicMemberWalker.Visit((TypeDeclarationSyntax)reference.GetSyntax(this.cancellationToken));
                            }

                            type = type.BaseType;
                        }
                    }
                }
            }

            bool TryGetScope(SyntaxNode location, out SyntaxNode result)
            {
                if (location is SingleVariableDesignationSyntax designation &&
                    this.context.Node is ExpressionSyntax expression &&
                    expression.Kind() != SyntaxKind.DeclarationExpression &&
                    expression.Contains(designation))
                {
                    result = null;
                    return false;
                }

                result = (SyntaxNode)location.FirstAncestorOrSelf<AnonymousFunctionExpressionSyntax>() ??
                                     location.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                return result != null;
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

            if (this.CurrentSymbol.IsEitherKind(SymbolKind.Local, SymbolKind.Parameter) &&
                assigned is DeclarationPatternSyntax declarationPattern &&
                declarationPattern.Designation is SingleVariableDesignationSyntax singleVariableDesignation &&
                singleVariableDesignation.Identifier.ValueText == this.CurrentSymbol.Name)
            {
                this.values.Add(value);
                return;
            }

            if (this.CurrentSymbol.IsEitherKind(SymbolKind.Local, SymbolKind.Parameter) &&
                assigned is MemberAccessExpressionSyntax)
            {
                return;
            }

            if (this.context.Node is ElementAccessExpressionSyntax)
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

            if (this.semanticModel.TryGetSymbol(assigned, this.cancellationToken, out ISymbol assignedSymbol))
            {
                if (assignedSymbol.IsEquivalentTo(this.CurrentSymbol) ||
                    this.refParameters.Contains(assignedSymbol as IParameterSymbol))
                {
                    this.values.Add(value);
                }

                if (this.outParameters.Contains(assignedSymbol as IParameterSymbol))
                {
                    this.outValues.Add(value);
                }
            }

            bool TryGetSetterWalker(out AssignedValueWalker walker)
            {
                walker = null;
                if (!this.CurrentSymbol.IsEitherKind(SymbolKind.Field, SymbolKind.Property))
                {
                    return false;
                }

                if (TryGetProperty(out var property))
                {
                    if (this.memberWalkers.TryGetValue(value, out walker))
                    {
                        return false;
                    }

                    if (property.TrySingleDeclaration(this.cancellationToken, out var declaration) &&
                        declaration.TryGetSetter(out var setter))
                    {
                        walker = Borrow(() => new AssignedValueWalker());
                        this.memberWalkers.Add(value, walker);
                        walker.CurrentSymbol = this.CurrentSymbol;
                        walker.semanticModel = this.semanticModel;
                        walker.cancellationToken = this.cancellationToken;
                        walker.context = new Context(setter, null);
                        walker.memberWalkers.Parent = this.memberWalkers;
                        walker.Visit(setter);
                    }
                }

                return walker != null;

                bool TryGetProperty(out IPropertySymbol result)
                {
                    result = null;
                    return assigned is ExpressionSyntax assignedExpression &&
                           MemberPath.TrySingle(assignedExpression, out var assignedMember) &&
                           assignedMember.Identifier.ValueText != this.CurrentSymbol.Name &&
                           this.CurrentSymbol.ContainingType.TryFindPropertyRecursive(assignedMember.Identifier.ValueText, out result);
                }
            }
        }

        private struct Context
        {
            internal readonly SyntaxNode Node;
            private readonly StatementSyntax stopAt;

            internal Context(SyntaxNode node, StatementSyntax stopAt)
            {
                this.Node = node;
                this.stopAt = stopAt;
            }

            internal static Context Create(SyntaxNode node, ISymbol symbol, CancellationToken cancellationToken)
            {
                return new Context(node, GetStopAt(node, symbol, cancellationToken));
            }

            internal bool ShouldVisit(SyntaxNode node)
            {
                if (this.stopAt is StatementSyntax stopAtStatement &&
                    node is StatementSyntax statement)
                {
                    return statement.IsExecutedBefore(stopAtStatement) != ExecutedBefore.No;
                }

                return true;
            }

            private static StatementSyntax GetStopAt(SyntaxNode location, ISymbol symbol, CancellationToken cancellationToken)
            {
                if (location == null)
                {
                    return null;
                }

                if (symbol.IsEitherKind(SymbolKind.Field, SymbolKind.Property) &&
                    !location.TryFirstAncestor<ConstructorDeclarationSyntax>(out _))
                {
                    return null;
                }

                if (location.TryFirstAncestor(out AnonymousFunctionExpressionSyntax anonymous) &&
                    !IsDeclaredIn(anonymous) &&
                    anonymous.TryFirstAncestor(out StatementSyntax statement))
                {
                    return Next(statement);
                }

                if (location.Parent is ArgumentSyntax argument &&
                    !argument.RefOrOutKeyword.IsKind(SyntaxKind.None) &&
                    location.TryFirstAncestor(out statement))
                {
                    return Next(statement);
                }

                return location.FirstAncestorOrSelf<StatementSyntax>();

                bool IsDeclaredIn(AnonymousFunctionExpressionSyntax lambda)
                {
                    if (symbol is ILocalSymbol local &&
                        local.TrySingleDeclaration(cancellationToken, out var declaration))
                    {
                        return lambda.Contains(declaration);
                    }

                    return false;
                }

                StatementSyntax Next(StatementSyntax current)
                {
                    if (current.Parent is BlockSyntax block &&
                        block.Statements.TryElementAt(block.Statements.IndexOf(current) + 1, out var next))
                    {
                        return next;
                    }

                    return null;
                }
            }
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

        private class CtorArgWalker : CSharpSyntaxWalker
        {
            private readonly AssignedValueWalker inner;

            public CtorArgWalker(AssignedValueWalker inner)
            {
                this.inner = inner;
            }

            public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                if (node.Initializer is ConstructorInitializerSyntax initializer &&
                    this.inner.semanticModel.TryGetSymbol(initializer, this.inner.cancellationToken, out var chained) &&
                    Equals(chained.ContainingType, this.inner.CurrentSymbol.ContainingType))
                {
                    this.inner.HandleInvoke(chained, node.Initializer.ArgumentList);
                }
            }

            public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                if (this.inner.semanticModel.TryGetSymbol(node, this.inner.cancellationToken, out var ctor) &&
                    Equals(ctor.ContainingType, this.inner.CurrentSymbol.ContainingType))
                {
                    this.inner.HandleInvoke(ctor, node.ArgumentList);
                }
            }
        }

        private class MemberWalkers
        {
            private readonly Dictionary<SyntaxNode, AssignedValueWalker> map = new Dictionary<SyntaxNode, AssignedValueWalker>();

            public MemberWalkers Parent { get; set; }

            private Dictionary<SyntaxNode, AssignedValueWalker> Map => this.Parent?.Map ??
                                                                       this.map;

            public void Add(SyntaxNode location, AssignedValueWalker walker)
            {
                this.Map.Add(location, walker);
            }

            public bool TryGetValue(SyntaxNode location, out AssignedValueWalker walker)
            {
                return this.Map.TryGetValue(location, out walker);
            }

            public void Clear()
            {
                foreach (var walker in this.map.Values)
                {
                    ((IDisposable)walker)?.Dispose();
                }

                this.map.Clear();
                this.Parent = null;
            }
        }
    }
}
