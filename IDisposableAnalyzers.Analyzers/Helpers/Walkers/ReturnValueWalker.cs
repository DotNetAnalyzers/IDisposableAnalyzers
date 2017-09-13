namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ReturnValueWalker : CSharpSyntaxWalker, IReadOnlyList<ExpressionSyntax>
    {
        private static readonly Pool<ReturnValueWalker> Pool = new Pool<ReturnValueWalker>(
            () => new ReturnValueWalker(),
            x =>
            {
                x.values.Clear();
                x.recursionLoop.Clear();
                x.awaits = false;
                x.semanticModel = null;
                x.cancellationToken = CancellationToken.None;
            });

        private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
        private readonly RecursionLoop recursionLoop = new RecursionLoop();

        private Search search;
        private bool awaits;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private ReturnValueWalker()
        {
        }

        public int Count => this.values.Count;

        public ExpressionSyntax this[int index] => this.values[index];

        public IEnumerator<ExpressionSyntax> GetEnumerator() => this.values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override void Visit(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
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

        internal static bool TrygetSingle(BlockSyntax body, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax returnValue)
        {
            using (var pooled = Create(body, Search.TopLevel, semanticModel, cancellationToken))
            {
                if (pooled.Item.values.Count != 1)
                {
                    returnValue = null;
                    return false;
                }

                returnValue = pooled.Item.values[0];
                return returnValue != null;
            }
        }

        internal static Pool<ReturnValueWalker>.Pooled Create(SyntaxNode node, Search search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var pooled = Pool.GetOrCreate();
            if (node == null)
            {
                return pooled;
            }

            pooled.Item.search = search;
            pooled.Item.semanticModel = semanticModel;
            pooled.Item.cancellationToken = cancellationToken;
            pooled.Item.Run(node);
            return pooled;
        }

        private Pool<ReturnValueWalker>.Pooled GetRecursive(SyntaxNode node)
        {
            var pooled = Pool.GetOrCreate();
            pooled.Item.search = this.search;
            pooled.Item.awaits = this.awaits;
            pooled.Item.semanticModel = this.semanticModel;
            pooled.Item.cancellationToken = this.cancellationToken;
            pooled.Item.recursionLoop.Add(this.recursionLoop);
            pooled.Item.Run(node);
            return pooled;
        }

        private void AddReturnValue(ExpressionSyntax value)
        {
            if (this.awaits)
            {
                if (AsyncAwait.TryAwaitTaskRun(value, this.semanticModel, this.cancellationToken, out ExpressionSyntax awaited))
                {
                    using (var pooled = this.GetRecursive(awaited))
                    {
                        if (pooled.Item.values.Count == 0)
                        {
                            this.values.Add(awaited);
                        }
                        else
                        {
                            foreach (var returnValue in pooled.Item.values)
                            {
                                this.AddReturnValue(returnValue);
                            }
                        }
                    }

                    return;
                }

                if (AsyncAwait.TryAwaitTaskFromResult(value, this.semanticModel, this.cancellationToken, out awaited))
                {
                    this.AddReturnValue(awaited);
                    return;
                }

                if (this.search == Search.Recursive &&
                    value is AwaitExpressionSyntax @await)
                {
                    value = @await.Expression;
                }
            }

            if (this.search == Search.Recursive)
            {
                if (value is InvocationExpressionSyntax invocation)
                {
                    var method = this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
                    if (method == null ||
                        method.DeclaringSyntaxReferences.Length == 0)
                    {
                        this.values.Add(value);
                    }
                    else
                    {
                        using (var pooled = this.GetRecursive(value))
                        {
                            foreach (var returnValue in pooled.Item.values)
                            {
                                this.AddReturnValue(returnValue);
                            }
                        }
                    }
                }
                else if (this.recursionLoop.Add(value) &&
                         this.semanticModel.IsEither<IParameterSymbol, ILocalSymbol>(value, this.cancellationToken))
                {
                    using (var pooled = AssignedValueWalker.Create(value, this.semanticModel, this.cancellationToken))
                    {
                        if (pooled.Item.Count == 0)
                        {
                            this.values.Add(value);
                        }
                        else
                        {
                            foreach (var assignment in pooled.Item)
                            {
                                this.AddReturnValue(assignment);
                            }
                        }
                    }
                }
                else
                {
                    this.values.Add(value);
                }
            }
            else
            {
                this.values.Add(value);
            }
        }

        private void Run(SyntaxNode node)
        {
            if (!this.recursionLoop.Add(node))
            {
                return;
            }

            if (this.TryHandleInvocation(node as InvocationExpressionSyntax) ||
                this.TryHandleAwait(node as AwaitExpressionSyntax) ||
                this.TryHandlePropertyGet(node as ExpressionSyntax) ||
                this.TryHandleLambda(node as LambdaExpressionSyntax))
            {
                return;
            }

            this.Visit(node);
        }

        private bool TryHandleInvocation(InvocationExpressionSyntax invocation)
        {
            if (invocation == null)
            {
                return false;
            }

            var method = this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
            if (method == null ||
                method.DeclaringSyntaxReferences.Length == 0)
            {
                return true;
            }

            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                base.Visit(reference.GetSyntax(this.cancellationToken));
            }

            for (var i = this.values.Count - 1; i >= 0; i--)
            {
                var symbol = this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken);
                if (this.search == Search.Recursive &&
                    SymbolComparer.Equals(symbol, method))
                {
                    this.values.RemoveAt(i);
                    continue;
                }

                if (invocation.TryGetArgumentValue(symbol as IParameterSymbol, this.cancellationToken, out ExpressionSyntax arg))
                {
                    this.values[i] = arg;
                }
            }

            this.values.PurgeDuplicates();
            return true;
        }

        private bool TryHandlePropertyGet(ExpressionSyntax propertyGet)
        {
            if (propertyGet == null)
            {
                return false;
            }

            var property = this.semanticModel.GetSymbolSafe(propertyGet, this.cancellationToken) as IPropertySymbol;
            var getter = property?.GetMethod;
            if (getter == null)
            {
                return false;
            }

            if (getter.DeclaringSyntaxReferences.Length == 0)
            {
                return true;
            }

            foreach (var reference in getter.DeclaringSyntaxReferences)
            {
                base.Visit(reference.GetSyntax(this.cancellationToken));
            }

            for (var i = this.values.Count - 1; i >= 0; i--)
            {
                var symbol = this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken);
                if (this.search == Search.Recursive &&
                    SymbolComparer.Equals(symbol, property))
                {
                    this.values.RemoveAt(i);
                }
            }

            this.values.PurgeDuplicates();

            return true;
        }

        private bool TryHandleAwait(AwaitExpressionSyntax @await)
        {
            if (@await == null)
            {
                return false;
            }

            if (AsyncAwait.TryGetAwaitedInvocation(@await, this.semanticModel, this.cancellationToken, out InvocationExpressionSyntax invocation))
            {
                this.awaits = true;
                var symbol = this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
                if (symbol != null)
                {
                    if (symbol.DeclaringSyntaxReferences.Length == 0)
                    {
                        this.AddReturnValue(invocation);
                    }
                    else
                    {
                        return this.TryHandleInvocation(invocation);
                    }
                }

                return true;
            }

            return false;
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

            this.values.PurgeDuplicates();
            return true;
        }
    }
}
