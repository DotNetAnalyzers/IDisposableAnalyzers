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
        private readonly List<ExpressionSyntax> returnValues = new List<ExpressionSyntax>();
        private readonly RecursiveWalkers recursiveWalkers = new RecursiveWalkers();
        private Search search;
        private SemanticModel semanticModel;
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
                if (walker.returnValues.Count != 1)
                {
                    returnValue = null;
                    return false;
                }

                returnValue = walker.returnValues[0];
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
            this.returnValues.Clear();
            this.recursiveWalkers.Clear();
            this.semanticModel = null;
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
            walker.search = this.search;
            walker.semanticModel = this.semanticModel;
            walker.cancellationToken = this.cancellationToken;
            walker.recursiveWalkers.Parent = this.recursiveWalkers;
            walker.Run(scope);
            return true;
        }

        private void Run(SyntaxNode node)
        {
            if (this.TryHandleInvocation(node as InvocationExpressionSyntax, out _) ||
                this.TryHandleAwait(node as AwaitExpressionSyntax) ||
                this.TryHandlePropertyGet(node as ExpressionSyntax, out _) ||
                this.TryHandleLambda(node as LambdaExpressionSyntax))
            {
                return;
            }

            this.Visit(node);
        }

        private bool TryHandleInvocation(InvocationExpressionSyntax invocation, out IMethodSymbol method)
        {
            if (this.semanticModel.TryGetSymbol(invocation, this.cancellationToken, out method) &&
                method.TrySingleDeclaration(this.cancellationToken, out var declaration) &&
                this.TryGetRecursive(invocation, declaration, out var walker))
            {
                foreach (var value in walker.returnValues)
                {
                    if (value is IdentifierNameSyntax identifierName &&
                        method.Parameters.TryFirst(x => x.Name == identifierName.Identifier.ValueText, out var parameter))
                    {
                        if (invocation.ArgumentList.TryGetMatchingArgument(parameter, out var argument))
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

                this.returnValues.RemoveAll(IsParameter);
                this.returnValues.PurgeDuplicates();
                return true;
            }

            return false;

            bool IsParameter(ExpressionSyntax value)
            {
                return value is IdentifierNameSyntax id &&
                       declaration.ParameterList.Parameters.TryFirst(x => x.Identifier.ValueText == id.Identifier.ValueText, out _);
            }
        }

        private bool TryHandlePropertyGet(ExpressionSyntax propertyGet, out IPropertySymbol property)
        {
            if (this.semanticModel.TryGetSymbol(propertyGet, this.cancellationToken, out property) &&
                property.GetMethod.TrySingleDeclaration(this.cancellationToken, out SyntaxNode getter) &&
                this.TryGetRecursive(propertyGet, getter, out var walker))
            {
                this.returnValues.AddRange(walker.returnValues);
                this.returnValues.PurgeDuplicates();
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

                this.returnValues.RemoveAll(IsParameter);
                this.returnValues.PurgeDuplicates();
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
                        if (invocation.ArgumentList.TryGetMatchingArgument(parameter, out var argument))
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
                    this.TryHandleLambda(awaited as LambdaExpressionSyntax);
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

            this.returnValues.PurgeDuplicates();
            return true;
        }

        private void AddReturnValue(ExpressionSyntax value)
        {
            if (this.search == Search.Recursive)
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
                    case BinaryExpressionSyntax coalesce when coalesce.IsKind(SyntaxKind.CoalesceExpression):
                        this.AddReturnValue(coalesce.Left);
                        this.AddReturnValue(coalesce.Right);
                        break;
                    case IdentifierNameSyntax identifierName when this.semanticModel.GetSymbolSafe(identifierName, this.cancellationToken).IsEither<ILocalSymbol, IParameterSymbol>():
                        using (var assignedValues = AssignedValueWalker.Borrow(value, this.semanticModel, this.cancellationToken))
                        {
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
                        }

                        break;
                    case ExpressionSyntax expression when this.semanticModel.GetSymbolSafe(expression, this.cancellationToken) is IPropertySymbol:
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
