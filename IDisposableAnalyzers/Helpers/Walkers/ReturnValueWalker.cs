namespace IDisposableAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ReturnValueWalker : PooledWalker<ReturnValueWalker>
    {
        private readonly SmallSet<ExpressionSyntax> values = new SmallSet<ExpressionSyntax>();
        private readonly RecursiveWalkers recursiveWalkers = new RecursiveWalkers();
        private readonly AssignedValueWalkers assignedValueWalkers = new AssignedValueWalkers();
        private ReturnValueSearch search;
        private SemanticModel semanticModel = null!;
        private CancellationToken cancellationToken;

        private ReturnValueWalker()
        {
        }

        internal IReadOnlyList<ExpressionSyntax> Values => this.values;

        public override void Visit(SyntaxNode node)
        {
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
            if (node.Expression is { } expression)
            {
                this.AddReturnValue(expression);
            }
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            this.AddReturnValue(node.Expression);
        }

        internal static ReturnValueWalker Borrow(SyntaxNode node, ReturnValueSearch search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = Borrow(() => new ReturnValueWalker());
            walker.search = search;
            walker.semanticModel = semanticModel;
            walker.cancellationToken = cancellationToken;
            walker.Run(node);
            return walker;
        }

        protected override void Clear()
        {
            this.values.Clear();
            this.recursiveWalkers.Clear();
            this.assignedValueWalkers.Clear();
            this.semanticModel = null!;
            this.cancellationToken = CancellationToken.None;
        }

        private ReturnValueWalker? Recursive(SyntaxNode location, SyntaxNode scope)
        {
            if (this.recursiveWalkers.TryGetValue(location, out _))
            {
                return null;
            }

            var walker = Borrow(() => new ReturnValueWalker());
            this.recursiveWalkers.Add(location, walker);
            walker.search = this.search == ReturnValueSearch.RecursiveInside ? ReturnValueSearch.Recursive : this.search;
            walker.semanticModel = this.semanticModel;
            walker.cancellationToken = this.cancellationToken;
            walker.recursiveWalkers.Parent = this.recursiveWalkers;
            walker.Run(scope);
            return walker;
        }

        private void Run(SyntaxNode node)
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation:
                    this.HandleInvocation(invocation);
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

        private void HandleInvocation(InvocationExpressionSyntax invocation)
        {
            if (this.semanticModel.TryGetSymbol(invocation, this.cancellationToken, out var method))
            {
                if (method.TrySingleDeclaration(this.cancellationToken, out SyntaxNode? declaration))
                {
                    if (this.Recursive(invocation, declaration) is { } methodWalker)
                    {
                        foreach (var value in methodWalker.values)
                        {
                            this.AddReturnValue(this.ValueOrArgument(value, invocation, method));
                        }

                        this.values.RemoveAll(x => IsParameter(x));

                        bool IsParameter(ExpressionSyntax value)
                        {
                            return value is IdentifierNameSyntax id &&
                                   method.TryFindParameter(id.Identifier.ValueText, out _);
                        }
                    }
                }
                else
                {
                    _ = this.values.Add(invocation);
                }
            }
        }

        private bool TryHandlePropertyGet(ExpressionSyntax propertyGet, [NotNullWhen(true)] out IPropertySymbol? property)
        {
            if (this.semanticModel.TryGetSymbol(propertyGet, this.cancellationToken, out property) &&
                property.GetMethod is { } getMethod &&
                getMethod.TrySingleDeclaration(this.cancellationToken, out SyntaxNode? getter) &&
                this.Recursive(propertyGet, getter) is { } getterWalker)
            {
                foreach (var returnValue in getterWalker.values)
                {
                    this.AddReturnValue(returnValue);
                }

                return true;
            }

            return false;
        }

        private bool TryHandleAwait(AwaitExpressionSyntax awaitExpression)
        {
            if (awaitExpression.Expression is { } awaited)
            {
                foreach (var e in Await(awaited))
                {
                    this.AddReturnValue(e);
                }
            }

            return false;

            IEnumerable<ExpressionSyntax> Await(ExpressionSyntax expression)
            {
                switch (expression)
                {
                    case InvocationExpressionSyntax candidate
                        when IDisposableAnalyzers.Await.ConfigureAwait(candidate) is { } inner:
                        foreach (var e in Await(inner))
                        {
                            yield return e;
                        }

                        break;
                    case InvocationExpressionSyntax candidate
                        when IDisposableAnalyzers.Await.TaskRun(candidate, this.semanticModel, this.cancellationToken) is { } lambda &&
                             this.Recursive(lambda, lambda) is { } recursive:
                        foreach (var inner in recursive.values)
                        {
                            yield return inner;
                        }

                        break;
                    case InvocationExpressionSyntax candidate
                        when IDisposableAnalyzers.Await.TaskFromResult(candidate, this.semanticModel, this.cancellationToken) is { } result:
                        yield return result;
                        break;
                    case InvocationExpressionSyntax candidate
                        when this.semanticModel.GetSymbolSafe(candidate, this.cancellationToken) is { } method &&
                             method.TrySingleDeclaration(this.cancellationToken, out MethodDeclarationSyntax? methodDeclaration) &&
                             this.Recursive(awaitExpression, methodDeclaration) is { } recursive:
                        foreach (var value in recursive.values)
                        {
                            if (methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword))
                            {
                                this.AddReturnValue(this.ValueOrArgument(value, candidate, method));
                            }
                            else
                            {
                                foreach (var e in Await(value))
                                {
                                    this.AddReturnValue(this.ValueOrArgument(e, candidate, method));
                                }
                            }
                        }

                        this.values.RemoveAll(x => IsParameter(x));

                        bool IsParameter(ExpressionSyntax value)
                        {
                            return value is IdentifierNameSyntax id &&
                                   method.Parameters.TryFirst(x => x.Name == id.Identifier.ValueText, out _);
                        }

                        break;
                    default:
                        yield return expression;
                        break;
                }
            }
        }

        private bool TryHandleLambda(LambdaExpressionSyntax lambda)
        {
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
                        this.HandleInvocation(invocation);
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
                        when this.semanticModel.TryGetSymbol(identifierName, this.cancellationToken, out var candidate) &&
                             candidate.IsEither<ILocalSymbol, IParameterSymbol>():
                        if (this.assignedValueWalkers.TryGetValue(identifierName, out _))
                        {
                            _ = this.values.Add(value);
                            return;
                        }

                        var walker = AssignedValueWalker.Borrow(value, this.semanticModel, this.cancellationToken);
                        this.assignedValueWalkers.Add(identifierName, walker);
                        if (walker.Values.Count == 0)
                        {
                            _ = this.values.Add(value);
                        }
                        else
                        {
                            foreach (var assignment in walker.Values)
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
                            _ = this.values.Add(value);
                        }

                        break;
                    default:
                        this.values.Add(value);
                        break;
                }
            }
            else
            {
                _ = this.values.Add(value);
            }
        }

        private ExpressionSyntax ValueOrArgument(ExpressionSyntax value, InvocationExpressionSyntax invocation, IMethodSymbol method)
        {
            if (value is IdentifierNameSyntax identifierName &&
                method.TryFindParameter(identifierName.Identifier.ValueText, out var parameter))
            {
                if (this.search != ReturnValueSearch.RecursiveInside &&
                    invocation.TryFindArgument(parameter, out var argument))
                {
                    return argument.Expression;
                }

                if (parameter.HasExplicitDefaultValue &&
                    parameter.TrySingleDeclaration(this.cancellationToken, out var parameterDeclaration) &&
                    parameterDeclaration is { Default: { Value: { } defaultValue } })
                {
                    return defaultValue;
                }
            }

            return value;
        }

        private class RecursiveWalkers
        {
            private readonly Dictionary<SyntaxNode, ReturnValueWalker> map = new Dictionary<SyntaxNode, ReturnValueWalker>();

            internal RecursiveWalkers? Parent { get; set; }

            private Dictionary<SyntaxNode, ReturnValueWalker> Map => this.Parent?.Map ??
                                                                     this.map;

            internal void Add(SyntaxNode member, ReturnValueWalker walker)
            {
                this.Map.Add(member, walker);
            }

            internal bool TryGetValue(SyntaxNode member, [NotNullWhen(true)] out ReturnValueWalker? walker)
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

            internal bool TryGetValue(IdentifierNameSyntax location, [NotNullWhen(true)] out AssignedValueWalker? walker)
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
