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

    internal sealed class ReturnValueWalker : PooledWalker<ReturnValueWalker>, IReadOnlyList<ExpressionSyntax>
    {
        private readonly SmallSet<ExpressionSyntax> returnValues = new SmallSet<ExpressionSyntax>();
        private readonly RecursiveWalkers recursiveWalkers = new RecursiveWalkers();
        private readonly AssignedValueWalkers assignedValueWalkers = new AssignedValueWalkers();
        private ReturnValueSearch search;
        private SemanticModel semanticModel = null!;
        private CancellationToken cancellationToken;

        private ReturnValueWalker()
        {
        }

        public int Count => this.returnValues.Count;

        public ExpressionSyntax this[int index] => this.returnValues[index];

        public IEnumerator<ExpressionSyntax> GetEnumerator() => this.returnValues.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override void Visit(SyntaxNode node)
        {
            if (node == null)
            {
                return;
            }

            switch (node.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.LocalFunctionStatement:
                    return;
                default:
                    base.Visit(node);
                    break;
            }
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            this.AddReturnValue(node.Expression);
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            this.AddReturnValue(node.Expression);
        }

        internal static ReturnValueWalker Borrow(SyntaxNode node, ReturnValueSearch search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = Borrow(() => new ReturnValueWalker());
            if (node == null)
            {
                return walker;
            }

            walker.search = search;
            walker.semanticModel = semanticModel;
            walker.cancellationToken = cancellationToken;
            walker.Run(node);
            return walker;
        }

        protected override void Clear()
        {
            this.returnValues.Clear();
            this.recursiveWalkers.Clear();
            this.assignedValueWalkers.Clear();
            this.semanticModel = null!;
            this.cancellationToken = CancellationToken.None;
        }

        private bool TryGetRecursive(SyntaxNode location, SyntaxNode scope, out ReturnValueWalker walker)
        {
            if (this.recursiveWalkers.TryGetValue(location, out walker))
            {
                return false;
            }

            walker = Borrow(() => new ReturnValueWalker());
            this.recursiveWalkers.Add(location, walker);
            walker.search = this.search == ReturnValueSearch.RecursiveInside ? ReturnValueSearch.Recursive : this.search;
            walker.semanticModel = this.semanticModel;
            walker.cancellationToken = this.cancellationToken;
            walker.recursiveWalkers.Parent = this.recursiveWalkers;
            walker.Run(scope);
            return true;
        }

        private void Run(SyntaxNode node)
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation:
                    _ = this.TryHandleInvocation(invocation, out _);
                    return;
                case AwaitExpressionSyntax awaitExpression:
                    _ = this.TryHandleAwait(awaitExpression);
                    return;
                case LambdaExpressionSyntax lambda:
                    _ = this.TryHandleLambda(lambda);
                    return;
                case LocalFunctionStatementSyntax { ExpressionBody: { Expression: { } expression } }:
                    this.AddReturnValue(expression);
                    break;
                case LocalFunctionStatementSyntax { Body: { } body }:
                    this.Visit(body);
                    break;
                case ExpressionSyntax expression:
                    _ = this.TryHandlePropertyGet(expression, out _);
                    return;
                default:
                    this.Visit(node);
                    break;
            }
        }

        private bool TryHandleInvocation(InvocationExpressionSyntax invocation, out IMethodSymbol method)
        {
            if (this.semanticModel.TryGetSymbol(invocation, this.cancellationToken, out method))
            {
                if (method.TrySingleDeclaration(this.cancellationToken, out BaseMethodDeclarationSyntax baseMethod) &&
                    this.TryGetRecursive(invocation, baseMethod, out var methodWalker))
                {
                    foreach (var value in methodWalker.returnValues)
                    {
                        if (value is IdentifierNameSyntax identifierName &&
                            method.TryFindParameter(identifierName.Identifier.ValueText, out var parameter))
                        {
                            if (this.search != ReturnValueSearch.RecursiveInside &&
                                invocation.TryFindArgument(parameter, out var argument))
                            {
                                this.AddReturnValue(argument.Expression);
                            }
                            else if (parameter.HasExplicitDefaultValue &&
                                     parameter.TrySingleDeclaration(this.cancellationToken, out var parameterDeclaration))
                            {
                                this.returnValues.Add(parameterDeclaration.Default?.Value);
                            }
                        }

                        this.AddReturnValue(value);
                    }

                    this.returnValues.RemoveAll(x => IsParameter(x));
                    return true;

                    bool IsParameter(ExpressionSyntax value)
                    {
                        return value is IdentifierNameSyntax id &&
                               baseMethod.TryFindParameter(id.Identifier.ValueText, out _);
                    }
                }

                if (method.TrySingleDeclaration(this.cancellationToken, out LocalFunctionStatementSyntax localFunction) &&
                    this.TryGetRecursive(invocation, localFunction, out var localFunctionWalker))
                {
                    foreach (var value in localFunctionWalker.returnValues)
                    {
                        if (value is IdentifierNameSyntax identifierName &&
                            method.TryFindParameter(identifierName.Identifier.ValueText, out var parameter))
                        {
                            if (this.search != ReturnValueSearch.RecursiveInside &&
                                invocation.TryFindArgument(parameter, out var argument))
                            {
                                this.AddReturnValue(argument.Expression);
                            }
                            else if (parameter.HasExplicitDefaultValue &&
                                     parameter.TrySingleDeclaration(this.cancellationToken, out var parameterDeclaration))
                            {
                                this.returnValues.Add(parameterDeclaration.Default?.Value);
                            }
                        }

                        this.AddReturnValue(value);
                    }

                    this.returnValues.RemoveAll(x => IsParameter(x));
                    return true;

                    bool IsParameter(ExpressionSyntax value)
                    {
                        return value is IdentifierNameSyntax id &&
                               localFunction.ParameterList.TryFind(id.Identifier.ValueText, out _);
                    }
                }
            }

            return false;
        }

        private bool TryHandlePropertyGet(ExpressionSyntax propertyGet, out IPropertySymbol property)
        {
            if (this.semanticModel.TryGetSymbol(propertyGet, this.cancellationToken, out property) &&
                property.GetMethod.TrySingleDeclaration(this.cancellationToken, out SyntaxNode getter) &&
                this.TryGetRecursive(propertyGet, getter, out var walker))
            {
                foreach (var returnValue in walker.returnValues)
                {
                    this.AddReturnValue(returnValue);
                }

                return true;
            }

            return false;
        }

        private bool TryHandleAwait(AwaitExpressionSyntax awaitExpression)
        {
            if (AsyncAwait.TryGetAwaitedInvocation(awaitExpression, this.semanticModel, this.cancellationToken, out var invocation) &&
                this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken) is ISymbol symbol)
            {
                if (symbol.TrySingleDeclaration(this.cancellationToken, out MemberDeclarationSyntax declaration) &&
                    declaration is MethodDeclarationSyntax methodDeclaration)
                {
                    if (methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword))
                    {
                        return this.TryHandleInvocation(invocation, out _);
                    }

                    if (this.TryGetRecursive(awaitExpression, declaration, out var walker))
                    {
                        foreach (var value in walker.returnValues)
                        {
                            AwaitValue(value);
                        }
                    }
                }
                else
                {
                    AwaitValue(invocation);
                }

                this.returnValues.RemoveAll(x => IsParameter(x));
                return true;
            }

            return false;

            void AwaitValue(ExpressionSyntax expression)
            {
                if (AsyncAwait.TryAwaitTaskFromResult(expression, this.semanticModel, this.cancellationToken, out var awaited))
                {
                    if (awaited is IdentifierNameSyntax identifierName &&
                        symbol is IMethodSymbol method &&
                        method.Parameters.TryFirst(x => x.Name == identifierName.Identifier.ValueText, out var parameter))
                    {
                        if (this.search != ReturnValueSearch.RecursiveInside &&
                            invocation.TryFindArgument(parameter, out var argument))
                        {
                            this.AddReturnValue(argument.Expression);
                        }
                        else if (parameter.HasExplicitDefaultValue &&
                                 parameter.TrySingleDeclaration(this.cancellationToken, out var parameterDeclaration))
                        {
                            this.returnValues.Add(parameterDeclaration.Default?.Value);
                        }
                    }

                    this.AddReturnValue(awaited);
                }
                else if (AsyncAwait.TryAwaitTaskRun(expression, this.semanticModel, this.cancellationToken, out awaited))
                {
                    if (this.TryGetRecursive(awaited, awaited, out var walker))
                    {
                        foreach (var value in walker.returnValues)
                        {
                            AwaitValue(value);
                        }
                    }
                }
                else
                {
                    this.AddReturnValue(expression);
                }
            }

            bool IsParameter(ExpressionSyntax value)
            {
                return value is IdentifierNameSyntax id &&
                       symbol is IMethodSymbol method &&
                       method.Parameters.TryFirst(x => x.Name == id.Identifier.ValueText, out _);
            }
        }

        private bool TryHandleLambda(LambdaExpressionSyntax lambda)
        {
            if (lambda == null)
            {
                return false;
            }

            if (lambda.Body is ExpressionSyntax expressionBody)
            {
                this.AddReturnValue(expressionBody);
            }
            else
            {
                base.Visit(lambda);
            }

            return true;
        }

        private void AddReturnValue(ExpressionSyntax value)
        {
            if (this.search.IsEither(ReturnValueSearch.Recursive, ReturnValueSearch.RecursiveInside))
            {
                switch (value)
                {
                    case InvocationExpressionSyntax invocation:
                        if (!this.TryHandleInvocation(invocation, out var method) &&
                            method != null &&
                            method.DeclaringSyntaxReferences.Length == 0)
                        {
                            this.returnValues.Add(invocation);
                        }

                        break;
                    case AwaitExpressionSyntax awaitExpression:
                        this.TryHandleAwait(awaitExpression);
                        break;
                    case ConditionalExpressionSyntax ternary:
                        this.AddReturnValue(ternary.WhenTrue);
                        this.AddReturnValue(ternary.WhenFalse);
                        break;
                    case BinaryExpressionSyntax coalesce
                        when coalesce.IsKind(SyntaxKind.CoalesceExpression):
                        this.AddReturnValue(coalesce.Left);
                        this.AddReturnValue(coalesce.Right);
                        break;
                    case IdentifierNameSyntax identifierName
                        when this.semanticModel.GetSymbolSafe(identifierName, this.cancellationToken).IsEither<ILocalSymbol, IParameterSymbol>():
                        if (this.assignedValueWalkers.TryGetValue(identifierName, out _))
                        {
                            this.returnValues.Add(value);
                            return;
                        }

                        var assignedValues = AssignedValueWalker.Borrow(value, this.semanticModel, this.cancellationToken);
                        this.assignedValueWalkers.Add(identifierName, assignedValues);
                        if (assignedValues.Count == 0)
                        {
                            this.returnValues.Add(value);
                        }
                        else
                        {
                            foreach (var assignment in assignedValues)
                            {
                                this.AddReturnValue(assignment);
                            }
                        }

                        break;
                    case { } expression
                        when this.semanticModel.GetSymbolSafe(expression, this.cancellationToken) is IPropertySymbol:
                        if (!this.TryHandlePropertyGet(value, out var property) &&
                            property != null &&
                            property.DeclaringSyntaxReferences.Length == 0)
                        {
                            this.returnValues.Add(value);
                        }

                        break;
                    default:
                        this.returnValues.Add(value);
                        break;
                }
            }
            else
            {
                this.returnValues.Add(value);
            }
        }

        private class RecursiveWalkers
        {
            private readonly Dictionary<SyntaxNode, ReturnValueWalker> map = new Dictionary<SyntaxNode, ReturnValueWalker>();

            internal RecursiveWalkers Parent { get; set; }

            private Dictionary<SyntaxNode, ReturnValueWalker> Map => this.Parent?.Map ??
                                                                     this.map;

            internal void Add(SyntaxNode member, ReturnValueWalker walker)
            {
                this.Map.Add(member, walker);
            }

            internal bool TryGetValue(SyntaxNode member, out ReturnValueWalker walker)
            {
                return this.Map.TryGetValue(member, out walker);
            }

            internal void Clear()
            {
                foreach (var walker in this.map.Values)
                {
                    ((IDisposable)walker)?.Dispose();
                }

                this.map.Clear();
                this.Parent = null;
            }
        }

        private class AssignedValueWalkers
        {
            private readonly Dictionary<IdentifierNameSyntax, AssignedValueWalker> map = new Dictionary<IdentifierNameSyntax, AssignedValueWalker>();

            internal void Add(IdentifierNameSyntax location, AssignedValueWalker walker)
            {
                this.map.Add(location, walker);
            }

            internal bool TryGetValue(IdentifierNameSyntax location, out AssignedValueWalker walker)
            {
                return this.map.TryGetValue(location, out walker);
            }

            internal void Clear()
            {
                foreach (var walker in this.map.Values)
                {
                    ((IDisposable)walker)?.Dispose();
                }

                this.map.Clear();
            }
        }
    }
}
