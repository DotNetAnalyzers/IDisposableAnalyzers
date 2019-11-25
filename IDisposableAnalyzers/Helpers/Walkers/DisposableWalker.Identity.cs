namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed partial class DisposableWalker
    {
        private static ExpressionSyntax? Identity(ExpressionSyntax candidate, Recursion recursion)
        {
            return candidate switch
            {
                { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier: { ValueText: "FromResult" } } } } invocation } } }
                when invocation.IsSymbol(KnownSymbol.Task.FromResult, recursion.SemanticModel, recursion.CancellationToken)
                => Recursive(invocation, recursion),
                { Parent: AwaitExpressionSyntax parent }
                => Recursive(parent, recursion),
                { Parent: BinaryExpressionSyntax { OperatorToken: { ValueText: "as" } } parent }
                => Recursive(parent, recursion),
                { Parent: BinaryExpressionSyntax { OperatorToken: { ValueText: "??" } } parent }
                => Recursive(parent, recursion),
                { Parent: CastExpressionSyntax parent }
                => Recursive(parent, recursion),
                { Parent: ConditionalExpressionSyntax parent }
                => Recursive(parent, recursion),
                { Parent: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier: { ValueText: "ConfigureAwait" } }, Parent: InvocationExpressionSyntax invocation } }
                => Recursive(invocation, recursion),
                { Parent: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier: { ValueText: "GetAwaiter" } }, Parent: InvocationExpressionSyntax invocation } }
                => Recursive(invocation, recursion),
                { Parent: LambdaExpressionSyntax { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } } }
                when invocation.IsSymbol(KnownSymbol.Task.Run, recursion.SemanticModel, recursion.CancellationToken)
                => Recursive(invocation, recursion),
                { Parent:ReturnStatementSyntax returnStatement}
                when returnStatement.TryFirstAncestor(out LambdaExpressionSyntax? lambda) &&
                     lambda is { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } } &&
                     invocation.IsSymbol(KnownSymbol.Task.Run, recursion.SemanticModel, recursion.CancellationToken)
                => Recursive(invocation, recursion),
                { Parent: MemberAccessExpressionSyntax { Expression: { } expression, Name: IdentifierNameSyntax { Identifier: { ValueText: "Result" } } } memberAccess }
                when recursion.SemanticModel.TryGetNamedType(expression, recursion.CancellationToken, out var type) &&
                     type.IsAssignableTo(KnownSymbol.Task, recursion.SemanticModel.Compilation)
                => Recursive(memberAccess, recursion),
                { Parent: MemberAccessExpressionSyntax { Expression: { } expression, Name: IdentifierNameSyntax { Identifier: { ValueText: "GetResult" } }, Parent: InvocationExpressionSyntax invocation } }
                when recursion.SemanticModel.TryGetNamedType(expression, recursion.CancellationToken, out var type) &&
                     type.IsAssignableTo(KnownSymbol.INotifyCompletion, recursion.SemanticModel.Compilation)
                => Recursive(invocation, recursion),
                { Parent: ParenthesizedExpressionSyntax parent }
                => Recursive(parent, recursion),
                _ => null,
            };

            static ExpressionSyntax Recursive(ExpressionSyntax parent, Recursion recursion) => Identity(parent, recursion) ?? parent;
        }
    }
}
