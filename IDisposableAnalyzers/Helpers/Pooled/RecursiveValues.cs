namespace IDisposableAnalyzers
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class RecursiveValues : IEnumerator<ExpressionSyntax>
    {
        private static readonly ConcurrentQueue<RecursiveValues> Cache = new();
        private readonly List<ExpressionSyntax> values = new();
        private readonly HashSet<SyntaxNode> checkedLocations = new();

        private int rawIndex = -1;
        private int recursiveIndex = -1;
        private IReadOnlyList<ExpressionSyntax> rawValues = null!;
        private SemanticModel semanticModel = null!;
        private CancellationToken cancellationToken;

        private RecursiveValues()
        {
        }

        object IEnumerator.Current => this.Current;

        public ExpressionSyntax Current => this.values[this.recursiveIndex];

        internal bool IsEmpty => this.rawValues.Count == 0;

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
            this.rawValues = null!;
            this.rawIndex = -1;
            this.recursiveIndex = -1;
            this.semanticModel = null!;
            this.cancellationToken = CancellationToken.None;
            Cache.Enqueue(this);
        }

        internal static RecursiveValues Borrow(IReadOnlyList<ExpressionSyntax> rawValues, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var item = Cache.GetOrCreate(() => new RecursiveValues());
            item.rawValues = rawValues;
            item.semanticModel = semanticModel;
            item.cancellationToken = cancellationToken;
            return item;
        }

        private bool AddRecursiveValues(ExpressionSyntax assignedValue)
        {
            if (assignedValue.IsMissing ||
                !this.checkedLocations.Add(assignedValue))
            {
                return false;
            }

            if (assignedValue.Parent is ArgumentSyntax argument &&
                argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
            {
                if (assignedValue.TryFirstAncestor(out InvocationExpressionSyntax? invocation) &&
                    this.semanticModel.TryGetSymbol(invocation, this.cancellationToken, out var target) &&
                    target.TrySingleMethodDeclaration(this.cancellationToken, out var targetDeclaration))
                {
                    if (targetDeclaration.TryFindParameter(argument, out var parameter) &&
                        this.semanticModel.TryGetSymbol(parameter, this.cancellationToken, out var parameterSymbol))
                    {
                        using var walker = AssignedValueWalker.Borrow(parameterSymbol, this.semanticModel, this.cancellationToken);
                        walker.HandleInvoke(target, invocation.ArgumentList);
                        return this.AddManyRecursively(walker.Values);
                    }

                    return false;
                }

                this.values.Add(assignedValue);
                return true;
            }

            switch (assignedValue)
            {
                case ArrayCreationExpressionSyntax _:
                case DefaultExpressionSyntax _:
                case ElementAccessExpressionSyntax _:
                case ImplicitArrayCreationExpressionSyntax _:
                case InitializerExpressionSyntax _:
                case LiteralExpressionSyntax _:
                case ObjectCreationExpressionSyntax _:
                case TypeOfExpressionSyntax _:
                    this.values.Add(assignedValue);
                    return true;
                case BinaryExpressionSyntax { Left: { }, OperatorToken.ValueText: "as" } binary:
                    return this.AddRecursiveValues(binary.Left);
                case BinaryExpressionSyntax { Left: { }, OperatorToken.ValueText: "??", Right: { } } binary:
                    var left = this.AddRecursiveValues(binary.Left);
                    var right = this.AddRecursiveValues(binary.Right);
                    return left || right;
                case CastExpressionSyntax cast:
                    return this.AddRecursiveValues(cast.Expression);
                case ConditionalExpressionSyntax { WhenTrue: { }, WhenFalse: { } } conditional:
                    var whenTrue = this.AddRecursiveValues(conditional.WhenTrue);
                    var whenFalse = this.AddRecursiveValues(conditional.WhenFalse);
                    return whenTrue || whenFalse;
                case SwitchExpressionSyntax { Arms: { } arms }:
                    var added = false;
                    foreach (var arm in arms)
                    {
                        added |= this.AddRecursiveValues(arm.Expression);
                    }

                    return added;
                case AwaitExpressionSyntax awaitExpression:
                    using (var walker = ReturnValueWalker.Borrow(awaitExpression, ReturnValueSearch.RecursiveInside, this.semanticModel, this.cancellationToken))
                    {
                        return this.AddManyRecursively(walker.Values);
                    }

                case ConditionalAccessExpressionSyntax { WhenNotNull: { } whenNotNull }:
                    return this.AddRecursiveValues(whenNotNull);
            }

            if (this.semanticModel.TryGetSymbol(assignedValue, this.cancellationToken, out var symbol))
            {
                switch (symbol)
                {
                    case ILocalSymbol _:
                        using (var walker = AssignedValueWalker.Borrow(assignedValue, this.semanticModel, this.cancellationToken))
                        {
                            return this.AddManyRecursively(walker.Values);
                        }

                    case IParameterSymbol _:
                        this.values.Add(assignedValue);
                        using (var walker = AssignedValueWalker.Borrow(assignedValue, this.semanticModel, this.cancellationToken))
                        {
                            return this.AddManyRecursively(walker.Values);
                        }

                    case IFieldSymbol _:
                        this.values.Add(assignedValue);
                        return true;
                    case IPropertySymbol property
                        when property is { ContainingType.MetadataName: "Task`1", MetadataName: "Result" } &&
                         assignedValue is MemberAccessExpressionSyntax { Expression: { } expression }:
                        return this.AddRecursiveValues(expression);
                    case IMethodSymbol method
                        when method is { ContainingType.MetadataName: "TaskAwaiter`1", MetadataName: "GetResult" } &&
                             assignedValue is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: { } expression, Name: IdentifierNameSyntax { Identifier.ValueText: "GetAwaiter" } } } } }:
                        return this.AddRecursiveValues(expression);
                    case IMethodSymbol method
                        when method is { ContainingType.MetadataName: "Interlocked", MetadataName: "Exchange" } &&
                             assignedValue is InvocationExpressionSyntax { ArgumentList.Arguments: { Count: 2 } arguments }:
                        using (var walker = AssignedValueWalker.Borrow(arguments[0].Expression, this.semanticModel, this.cancellationToken))
                        {
                            return this.AddManyRecursively(walker.Values) &&
                                   this.AddRecursiveValues(arguments[1].Expression);
                        }

                    case IMethodSymbol { DeclaringSyntaxReferences.Length: 0 } method:
                        if (assignedValue is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: { } parent } } &&
                            !parent.IsKind(SyntaxKind.IdentifierName) &&
                            !parent.IsKind(SyntaxKind.SimpleMemberAccessExpression) &&
                            Disposable.AssumeIsIdentity(method))
                        {
                            return this.AddRecursiveValues(parent);
                        }
                        else
                        {
                            this.values.Add(assignedValue);
                            return true;
                        }

                    case IPropertySymbol { DeclaringSyntaxReferences.Length: 0 }:
                        this.values.Add(assignedValue);
                        return true;
                    case IPropertySymbol _:
                    case IMethodSymbol _:
                        using (var walker = ReturnValueWalker.Borrow(assignedValue, ReturnValueSearch.RecursiveInside, this.semanticModel, this.cancellationToken))
                        {
                            return this.AddManyRecursively(walker.Values);
                        }
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
