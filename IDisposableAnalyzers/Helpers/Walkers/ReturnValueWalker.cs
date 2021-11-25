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
        private readonly SmallSet<ExpressionSyntax> values = new();
        private readonly AssignedValueWalkers assignedValueWalkers = new();
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
                switch (node)
                {
                    case InvocationExpressionSyntax invocation
                        when walker.recursion.Method(invocation) is { } target:
                        walker.HandleInvocation(target);
                        break;
                    case AwaitExpressionSyntax awaitExpression:
                        walker.HandleAwait(awaitExpression);
                        break;
                    case LambdaExpressionSyntax { Body: ExpressionSyntax expression }:
                        walker.AddReturnValue(expression);
                        break;
                    case LambdaExpressionSyntax { Body: { } body }:
                        walker.Visit(body);
                        break;
                    case ExpressionSyntax expression
                        when walker.recursion.PropertyGet(expression) is { } propertyGet:
                        walker.HandlePropertyGet(propertyGet);
                        break;
                    case LocalFunctionStatementSyntax { ExpressionBody: { Expression: { } expression } }:
                        walker.AddReturnValue(expression);
                        break;
                    case LocalFunctionStatementSyntax { Body: { } body }:
                        walker.Visit(body);
                        break;
                    default:
                        walker.Visit(node);
                        break;
                }
            }

            return walker;
        }

        protected override void Clear()
        {
            this.values.Clear();
            this.assignedValueWalkers.Clear();
            //// ReSharper disable once ConstantConditionalAccessQualifier can be null due to hack
            this.recursion?.Dispose();
            this.recursion = null!;
        }

        private void HandleInvocation(Target<InvocationExpressionSyntax, IMethodSymbol, SyntaxNode> target)
        {
            if (target is { Declaration: { } })
            {
                using var recursive = this.Recursive(target);
                foreach (var value in recursive.values)
                {
                    this.AddReturnValue(value, target);
                }

                this.values.RemoveAll(x => IsParameter(x));

                bool IsParameter(ExpressionSyntax x)
                {
                    return x is IdentifierNameSyntax { Identifier: { ValueText: { } name } } &&
                           target.Symbol.TryFindParameter(name, out _);
                }
            }
            else
            {
                _ = this.values.Add(target.Source);
            }
        }

        private void HandlePropertyGet(Target<ExpressionSyntax, IMethodSymbol, SyntaxNode> target)
        {
            if (target is { Declaration: { } })
            {
                using var recursive = this.Recursive(target);
                foreach (var value in recursive.values)
                {
                    this.AddReturnValue(value);
                }

                this.values.RemoveAll(x => ShouldRemove(x));

                bool ShouldRemove(ExpressionSyntax x)
                {
                    return x == target.Source;
                }
            }
            else
            {
                _ = this.values.Add(target.Source);
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
                        when IDisposableAnalyzers.Await.TaskRun(candidate, this.recursion.SemanticModel, this.recursion.CancellationToken) is { } lambda:
                        if (lambda is { ExpressionBody: { } expressionBody })
                        {
                            yield return expressionBody;
                        }
                        else
                        {
                            using var recursive = this.Recursive(new Target<LambdaExpressionSyntax, ISymbol, SyntaxNode>(lambda, null!, lambda.Body));
                            foreach (var inner in recursive.values)
                            {
                                yield return inner;
                            }
                        }

                        break;
                    case InvocationExpressionSyntax candidate
                        when IDisposableAnalyzers.Await.TaskFromResult(candidate, this.recursion.SemanticModel, this.recursion.CancellationToken) is { } result:
                        yield return result;
                        break;
                    case InvocationExpressionSyntax candidate
                        when this.recursion.Method(candidate) is { Declaration: { } } target:
                        {
                            using var recursive = this.Recursive(target);
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

                            bool IsParameter(ExpressionSyntax x)
                            {
                                return x is IdentifierNameSyntax { Identifier: { ValueText: { } name } } &&
                                       target.Symbol.TryFindParameter(name, out _);
                            }
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
                    case BinaryExpressionSyntax { OperatorToken: { ValueText: "??" } } coalesce:
                        this.AddReturnValue(coalesce.Left);
                        this.AddReturnValue(coalesce.Right);
                        break;
                    case BinaryExpressionSyntax { OperatorToken: { ValueText: "as" } } coalesce:
                        this.AddReturnValue(coalesce.Left);
                        break;
                    case CastExpressionSyntax cast:
                        this.AddReturnValue(cast.Expression);
                        break;
                    case SwitchExpressionSyntax { Arms: { } arms }:
                        foreach (var arm in arms)
                        {
                            this.AddReturnValue(arm.Expression);
                        }

                        break;
                    case InvocationExpressionSyntax invocation:
                        switch (this.recursion.Method(invocation))
                        {
                            case { Declaration: { } } target:
                                this.HandleInvocation(target);
                                break;
                            case { }:
                                _ = this.values.Add(invocation);
                                break;
                            case null
                                when this.recursion.SemanticModel.GetSymbolSafe(invocation, this.recursion.CancellationToken) is { DeclaringSyntaxReferences: { Length: 0 } }:
                                _ = this.values.Add(invocation);
                                break;
                        }

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
            if (value == target.Source)
            {
                return;
            }

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

        private ReturnValueWalker Recursive<TSource, TSymbol, TDeclaration>(Target<TSource, TSymbol, TDeclaration> target)
            where TSource : SyntaxNode
            where TSymbol : ISymbol
            where TDeclaration : SyntaxNode
        {
            if (target.Declaration is null)
            {
                throw new InvalidOperationException("Only call this when symbol has declaration.");
            }

            var walker = Borrow(() => new ReturnValueWalker());
            walker.search = this.search == ReturnValueSearch.RecursiveInside ? ReturnValueSearch.Recursive : this.search;
            walker.recursion = this.recursion;
            switch (target.Declaration)
            {
                case LocalFunctionStatementSyntax { ExpressionBody: { } expressionBody }:
                    walker.Visit(expressionBody);
                    break;
                case LocalFunctionStatementSyntax { Body: { } body }:
                    walker.Visit(body);
                    break;
                default:
                    walker.Visit(target.Declaration);
                    break;
            }

            walker.recursion = null!;
            return walker;
        }

        private class AssignedValueWalkers
        {
            private readonly Dictionary<IdentifierNameSyntax, AssignedValueWalker> map = new();

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
