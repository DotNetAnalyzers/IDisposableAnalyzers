namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal class RecursiveValues : IEnumerator<ExpressionSyntax>
    {
        private static readonly ConcurrentQueue<RecursiveValues> Cache = new ConcurrentQueue<RecursiveValues>();
        private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
        private readonly HashSet<SyntaxNode> checkedLocations = new HashSet<SyntaxNode>();

        private int rawIndex = -1;
        private int recursiveIndex = -1;
        private IReadOnlyList<ExpressionSyntax> rawValues;
        private SemanticModel semanticModel;
        private CancellationToken cancellationToken;

        private RecursiveValues()
        {
        }

        object IEnumerator.Current => this.Current;

        public int Count => this.rawValues.Count;

        public ExpressionSyntax Current => this.values[this.recursiveIndex];

        public static RecursiveValues Create(IReadOnlyList<ExpressionSyntax> rawValues, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var item = Cache.GetOrCreate(() => new RecursiveValues());
            item.rawValues = rawValues;
            item.semanticModel = semanticModel;
            item.cancellationToken = cancellationToken;
            return item;
        }

        public bool MoveNext()
        {
            if (this.recursiveIndex < this.values.Count - 1)
            {
                this.recursiveIndex++;
                return true;
            }

            if (this.rawIndex < this.rawValues.Count - 1)
            {
                this.rawIndex++;
                if (!this.AddRecursiveValues(this.rawValues[this.rawIndex]))
                {
                    return this.MoveNext();
                }

                this.recursiveIndex++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            this.recursiveIndex = -1;
        }

        public void Dispose()
        {
            this.values.Clear();
            this.checkedLocations.Clear();
            this.rawValues = null;
            this.rawIndex = -1;
            this.recursiveIndex = -1;
            this.semanticModel = null;
            this.cancellationToken = CancellationToken.None;
            Cache.Enqueue(this);
        }

        private bool AddRecursiveValues(ExpressionSyntax assignedValue)
        {
            if (assignedValue == null ||
                assignedValue.IsMissing ||
                !this.checkedLocations.Add(assignedValue))
            {
                return false;
            }

            if (assignedValue is LiteralExpressionSyntax ||
                assignedValue is DefaultExpressionSyntax ||
                assignedValue is TypeOfExpressionSyntax ||
                assignedValue is ObjectCreationExpressionSyntax ||
                assignedValue is ArrayCreationExpressionSyntax ||
                assignedValue is ImplicitArrayCreationExpressionSyntax ||
                assignedValue is InitializerExpressionSyntax)
            {
                this.values.Add(assignedValue);
                return true;
            }

            var argument = assignedValue.Parent as ArgumentSyntax;
            if (argument?.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) == true)
            {
                var invocation = assignedValue.FirstAncestor<InvocationExpressionSyntax>();
                var invokedMethod = this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
                if (invokedMethod == null ||
                    invokedMethod.DeclaringSyntaxReferences.Length == 0)
                {
                    this.values.Add(invocation);
                    return true;
                }

                var before = this.values.Count;
                foreach (var reference in invokedMethod.DeclaringSyntaxReferences)
                {
                    var methodDeclaration = reference.GetSyntax(this.cancellationToken) as MethodDeclarationSyntax;
                    if (methodDeclaration.TryGetMatchingParameter(argument, out ParameterSyntax parameter))
                    {
                        using (var assignedValues = AssignedValueWalker.Borrow(this.semanticModel.GetDeclaredSymbolSafe(parameter, this.cancellationToken), this.semanticModel, this.cancellationToken))
                        {
                            assignedValues.HandleInvoke(invokedMethod, invocation.ArgumentList);
                            return this.AddManyRecursively(assignedValues);
                        }
                    }
                }

                return before != this.values.Count;
            }

            if (assignedValue is BinaryExpressionSyntax binaryExpression)
            {
                switch (binaryExpression.Kind())
                {
                case SyntaxKind.CoalesceExpression:
                    var left = this.AddRecursiveValues(binaryExpression.Left);
                    var right = this.AddRecursiveValues(binaryExpression.Right);
                    return left || right;
                case SyntaxKind.AsExpression:
                    return this.AddRecursiveValues(binaryExpression.Left);
                default:
                    return false;
                }
            }

            if (assignedValue is CastExpressionSyntax cast)
            {
                return this.AddRecursiveValues(cast.Expression);
            }

            if (assignedValue is ConditionalExpressionSyntax conditional)
            {
                var whenTrue = this.AddRecursiveValues(conditional.WhenTrue);
                var whenFalse = this.AddRecursiveValues(conditional.WhenFalse);
                return whenTrue || whenFalse;
            }

            if (assignedValue is AwaitExpressionSyntax @await)
            {
                using (var walker = ReturnValueWalker.Borrow(@await, Search.Recursive, this.semanticModel, this.cancellationToken))
                {
                    return this.AddManyRecursively(walker);
                }
            }

            if (assignedValue is ElementAccessExpressionSyntax)
            {
                this.values.Add(assignedValue);
                return true;
            }

            var symbol = this.semanticModel.GetSymbolSafe(assignedValue, this.cancellationToken);
            if (symbol == null)
            {
                return false;
            }

            if (symbol is IFieldSymbol)
            {
                this.values.Add(assignedValue);
                return true;
            }

            if (symbol is IParameterSymbol)
            {
                this.values.Add(assignedValue);
                using (var assignedValues = AssignedValueWalker.Borrow(assignedValue, this.semanticModel, this.cancellationToken))
                {
                    return this.AddManyRecursively(assignedValues);
                }
            }

            if (symbol is ILocalSymbol)
            {
                using (var assignedValues = AssignedValueWalker.Borrow(assignedValue, this.semanticModel, this.cancellationToken))
                {
                    return this.AddManyRecursively(assignedValues);
                }
            }

            if (symbol is IPropertySymbol property)
            {
                if (property.DeclaringSyntaxReferences.Length == 0)
                {
                    this.values.Add(assignedValue);
                    return true;
                }

                using (var returnValues = ReturnValueWalker.Borrow(assignedValue, Search.Recursive, this.semanticModel, this.cancellationToken))
                {
                    return this.AddManyRecursively(returnValues);
                }
            }

            if (symbol is IMethodSymbol method)
            {
                if (method.DeclaringSyntaxReferences.Length == 0)
                {
                    this.values.Add(assignedValue);
                    return true;
                }

                using (var walker = ReturnValueWalker.Borrow(assignedValue, Search.Recursive, this.semanticModel, this.cancellationToken))
                {
                    return this.AddManyRecursively(walker);
                }
            }

            return false;
        }

        private bool AddManyRecursively(IReadOnlyList<ExpressionSyntax> newValues)
        {
            var addedAny = false;
            foreach (var value in newValues)
            {
                addedAny |= this.AddRecursiveValues(value);
            }

            return addedAny;
        }
    }
}