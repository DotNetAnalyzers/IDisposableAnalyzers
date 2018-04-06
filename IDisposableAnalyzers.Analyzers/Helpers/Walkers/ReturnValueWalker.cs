namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class ReturnValueWalker : PooledWalker<ReturnValueWalker>, IReadOnlyList<ExpressionSyntax>
    {
        private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
        private readonly RecursiveWalkers recursiveWalkers = new RecursiveWalkers();
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

        internal static bool TrySingle(BlockSyntax body, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax returnValue)
        {
            if (body == null ||
                body.Statements.Count == 0)
            {
                returnValue = null;
                return false;
            }

            if (body.Statements.Count == 1)
            {
                returnValue = (body.Statements[0] as ReturnStatementSyntax)?.Expression;
                return returnValue != null;
            }

            using (var walker = Borrow(body, Search.TopLevel, semanticModel, cancellationToken))
            {
                if (walker.values.Count != 1)
                {
                    returnValue = null;
                    return false;
                }

                returnValue = walker.values[0];
                return returnValue != null;
            }
        }

        internal static ReturnValueWalker Borrow(SyntaxNode node, Search search, SemanticModel semanticModel, CancellationToken cancellationToken)
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
            this.values.Clear();
            this.recursiveWalkers.Clear();
            this.awaits = false;
            this.semanticModel = null;
            this.cancellationToken = CancellationToken.None;
        }

        private bool TryGetRecursive(SyntaxNode node, out ReturnValueWalker walker)
        {
            if (this.recursiveWalkers.TryGetValue(node, out walker))
            {
                return walker != this;
            }

            walker = Borrow(() => new ReturnValueWalker());
            this.recursiveWalkers.Add(node, walker);
            walker.search = this.search;
            walker.awaits = this.awaits;
            walker.semanticModel = this.semanticModel;
            walker.cancellationToken = this.cancellationToken;
            walker.recursiveWalkers.Parent = this.recursiveWalkers;
            walker.Run(node);
            return true;
        }

        private void AddReturnValue(ExpressionSyntax value)
        {
            if (this.awaits)
            {
                if (AsyncAwait.TryAwaitTaskRun(value, this.semanticModel, this.cancellationToken, out var awaited) &&
                    this.TryGetRecursive(awaited, out var walker))
                {
                    if (walker.values.Count == 0)
                    {
                        this.values.Add(awaited);
                    }
                    else
                    {
                        foreach (var returnValue in walker.values)
                        {
                            this.AddReturnValue(returnValue);
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
                    else if (this.TryGetRecursive(invocation, out var walker))
                    {
                        foreach (var returnValue in walker.values)
                        {
                            this.AddReturnValue(returnValue);
                        }
                    }
                }
                else if (this.semanticModel.IsEither<IParameterSymbol, ILocalSymbol>(value, this.cancellationToken))
                {
                    using (var assignedValues = AssignedValueWalker.Borrow(value, this.semanticModel, this.cancellationToken))
                    {
                        if (assignedValues.Count == 0)
                        {
                            this.values.Add(value);
                        }
                        else
                        {
                            foreach (var assignment in assignedValues)
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
            if (this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken) is IMethodSymbol method)
            {
                if (method.TrySingleDeclaration(this.cancellationToken, out var declaration))
                {
                    base.Visit(declaration);
                    for (var i = this.values.Count - 1; i >= 0; i--)
                    {
                        var symbol = this.semanticModel.GetSymbolSafe(this.values[i], this.cancellationToken);
                        if (this.search == Search.Recursive &&
                            SymbolComparer.Equals(symbol, method))
                        {
                            this.values.RemoveAt(i);
                            continue;
                        }

                        if (invocation.TryGetArgumentValue(symbol as IParameterSymbol, this.cancellationToken, out var arg))
                        {
                            this.values[i] = arg;
                        }
                    }

                    this.values.PurgeDuplicates();
                }

                return true;
            }

            return false;
        }

        private bool TryHandlePropertyGet(ExpressionSyntax propertyGet)
        {
            if (this.semanticModel.GetSymbolSafe(propertyGet, this.cancellationToken) is IPropertySymbol property)
            {
                if (property.GetMethod.TrySingleDeclaration(this.cancellationToken, out SyntaxNode getter))
                {
                    base.Visit(getter);
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
                }

                return true;
            }

            return false;
        }

        private bool TryHandleAwait(AwaitExpressionSyntax @await)
        {
            if (@await == null)
            {
                return false;
            }

            if (AsyncAwait.TryGetAwaitedInvocation(@await, this.semanticModel, this.cancellationToken, out var invocation))
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

        private class RecursiveWalkers
        {
            private readonly Dictionary<SyntaxNode, ReturnValueWalker> map = new Dictionary<SyntaxNode, ReturnValueWalker>();

            public RecursiveWalkers Parent { get; set; }

            private Dictionary<SyntaxNode, ReturnValueWalker> Current => this.Parent?.Current ??
                                                                        this.map;

            public void Add(SyntaxNode member, ReturnValueWalker walker)
            {
                this.Current.Add(member, walker);
            }

            public bool TryGetValue(SyntaxNode member, out ReturnValueWalker walker)
            {
                return this.Current.TryGetValue(member, out walker);
            }

            public void Clear()
            {
                if (this.map != null)
                {
                    foreach (var walker in this.map)
                    {
                        walker.Value?.Dispose();
                    }

                    this.map.Clear();
                }

                this.Parent = null;
            }
        }
    }
}
