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
        private Recursion recursion = null!;

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
            if (Recursion.Borrow(node, semanticModel, cancellationToken) is { } recursion)
            {
                walker.recursion = recursion;
                walker.Run(node);
            }

            return walker;
        }

        protected override void Clear()
        {
            this.values.Clear();
            this.recursiveWalkers.Clear();
            this.assignedValueWalkers.Clear();
            this.recursion.Dispose();
            this.recursion = null!;
        }

        private void Run(SyntaxNode node)
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation
                    when this.recursion.Method(invocation) is { } target:
                    this.HandleInvocation(target);
                    return;
                case AwaitExpressionSyntax awaitExpression:
                    this.HandleAwait(awaitExpression);
                    return;
                case LambdaExpressionSyntax { Body: ExpressionSyntax expression }:
                    this.AddReturnValue(expression);
                    return;
                case LambdaExpressionSyntax lambda:
                    base.Visit(lambda);
                    return;
                case LocalFunctionStatementSyntax { ExpressionBody: { Expression: { } expression } }:
                    this.AddReturnValue(expression);
                    return;
                case LocalFunctionStatementSyntax { Body: { } body }:
                    this.Visit(body);
                    return;
                case ExpressionSyntax expression
                    when this.recursion.PropertyGet(expression) is { } propertyGet:
                    this.HandlePropertyGet(propertyGet);
                    return;
                default:
                    this.Visit(node);
                    break;
            }
        }

        private void HandleInvocation(Target<InvocationExpressionSyntax, IMethodSymbol, SyntaxNode> invocation)
        {
            if (invocation is { Declaration: { } declaration })
            {
                if (this.Recursive(invocation.Source, declaration) is { } recursive)
                {
                    foreach (var value in recursive.values)
                    {
                        this.AddReturnValue(value, invocation);
                    }

                    this.values.RemoveAll(x => IsParameter(x));

                    bool IsParameter(ExpressionSyntax value)
                    {
                        return value is IdentifierNameSyntax id &&
                               invocation.Symbol.TryFindParameter(id.Identifier.ValueText, out _);
                    }
                }
            }
            else
            {
                _ = this.values.Add(invocation.Source);
            }
        }

        private void HandlePropertyGet(Target<ExpressionSyntax, IMethodSymbol, SyntaxNode> propertyGet)
        {
            if (propertyGet is { Declaration: { } declaration })
            {
                if (this.Recursive(propertyGet.Source, declaration) is { } recursive)
                {
                    foreach (var value in recursive.values)
                    {
                        this.AddReturnValue(value);
                    }
                }
            }
            else
            {
                _ = this.values.Add(propertyGet.Source);
            }
        }

        private void HandleAwait(AwaitExpressionSyntax awaitExpression)
        {
            if (awaitExpression.Expression is { } awaited)
            {
                foreach (var e in Await(awaited))
                {
                    this.AddReturnValue(e);
                }
            }

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
                        when IDisposableAnalyzers.Await.TaskRun(candidate, this.recursion.SemanticModel, this.recursion.CancellationToken) is { } lambda &&
                             this.Recursive(lambda, lambda) is { } recursive:
                        foreach (var inner in recursive.values)
                        {
                            yield return inner;
                        }

                        break;
                    case InvocationExpressionSyntax candidate
                        when IDisposableAnalyzers.Await.TaskFromResult(candidate, this.recursion.SemanticModel, this.recursion.CancellationToken) is { } result:
                        yield return result;
                        break;
                    case InvocationExpressionSyntax candidate
                        when this.recursion.Method(candidate) is { Declaration: { } declaration } target &&
                             this.Recursive(awaitExpression, declaration) is { } recursive:
                        foreach (var value in recursive.values)
                        {
                            if (target.Symbol.IsAsync)
                            {
                                this.AddReturnValue(value, target);
                            }
                            else
                            {
                                foreach (var e in Await(value))
                                {
                                    this.AddReturnValue(e, target);
                                }
                            }
                        }

                        this.values.RemoveAll(x => IsParameter(x));

                        bool IsParameter(ExpressionSyntax value)
                        {
                            return value is IdentifierNameSyntax id &&
                                   target.Symbol.Parameters.TryFirst(x => x.Name == id.Identifier.ValueText, out _);
                        }

                        break;
                    default:
                        yield return expression;
                        break;
                }
            }
        }

        private void AddReturnValue(ExpressionSyntax value)
        {
            if (this.search.IsEither(ReturnValueSearch.Recursive, ReturnValueSearch.RecursiveInside))
            {
                switch (value)
                {
                    case AwaitExpressionSyntax awaitExpression:
                        this.HandleAwait(awaitExpression);
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
                    case SwitchExpressionSyntax { Arms: { } arms }:
                        foreach (var arm in arms)
                        {
                            this.AddReturnValue(arm.Expression);
                        }

                        break;
                    case InvocationExpressionSyntax invocation
                        when this.recursion.Method(invocation) is { } target:
                        this.HandleInvocation(target);
                        break;
                    case { } expression
                        when this.recursion.PropertyGet(expression) is { } propertyGet:
                        this.HandlePropertyGet(propertyGet);
                        break;
                    case IdentifierNameSyntax identifierName
                        when this.recursion.SemanticModel.TryGetSymbol(identifierName, this.recursion.CancellationToken, out var candidate) &&
                             candidate.IsEither<ILocalSymbol, IParameterSymbol>():
                        if (this.assignedValueWalkers.TryGetValue(identifierName, out _))
                        {
                            _ = this.values.Add(value);
                            return;
                        }

                        var walker = AssignedValueWalker.Borrow(value, this.recursion.SemanticModel, this.recursion.CancellationToken);
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

        private void AddReturnValue(ExpressionSyntax value, Target<InvocationExpressionSyntax, IMethodSymbol, SyntaxNode> target)
        {
            if (value is IdentifierNameSyntax identifierName &&
                target.Symbol.TryFindParameter(identifierName.Identifier.ValueText, out var parameter))
            {
                if (this.search != ReturnValueSearch.RecursiveInside &&
                    target.Source.TryFindArgument(parameter, out var argument))
                {
                    this.AddReturnValue(argument.Expression);
                    return;
                }

                if (parameter.HasExplicitDefaultValue &&
                    parameter.TrySingleDeclaration(this.recursion.CancellationToken, out var parameterDeclaration) &&
                    parameterDeclaration is { Default: { Value: { } defaultValue } })
                {
                    this.AddReturnValue(defaultValue);
                    return;
                }
            }

            this.AddReturnValue(value);
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
            walker.recursion = Recursion.Borrow(this.recursion.ContainingType, this.recursion.SemanticModel, this.recursion.CancellationToken);
            walker.recursiveWalkers.Parent = this.recursiveWalkers;
            walker.Run(scope);
            return walker;
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
                    walker.Dispose();
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
