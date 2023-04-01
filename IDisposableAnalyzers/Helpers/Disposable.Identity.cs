namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool AssumeIsIdentity(IMethodSymbol method)
        {
            return method switch
            {
                { DeclaringSyntaxReferences.Length: 0, IsExtensionMethod: true }
                    when method.TryGetThisParameter(out var parameter) &&
                         parameter.ContainingSymbol is IMethodSymbol extensionMethod
                    => TypeSymbolComparer.Equal(extensionMethod.ReturnType, parameter.Type),
                { DeclaringSyntaxReferences.Length: 0, IsStatic: false }
                    => !method.MetadataName.Contains("Open") &&
                       !method.MetadataName.Contains("Create") &&
                       TypeSymbolComparer.Equal(method.ReturnType, method.ContainingType),
                _ => false,
            };
        }

        private static ExpressionSyntax? Identity(ExpressionSyntax candidate, Recursion recursion)
        {
            return candidate switch
            {
                { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: "FromResult" } } } invocation } } }
                    when invocation.IsSymbol(KnownSymbols.Task.FromResult, recursion.SemanticModel, recursion.CancellationToken)
                    => Recursive(invocation, recursion),
                { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } argument }
                    when recursion.Target(argument) is { } target &&
                         IsIdentity(target, recursion)
                    => Recursive(invocation, recursion),
                { Parent: AwaitExpressionSyntax parent }
                    => Recursive(parent, recursion),
                { Parent: BinaryExpressionSyntax { OperatorToken.ValueText: "as" } parent }
                    => Recursive(parent, recursion),
                { Parent: BinaryExpressionSyntax { OperatorToken.ValueText: "??" } parent }
                    => Recursive(parent, recursion),
                { Parent: CastExpressionSyntax parent }
                    => Recursive(parent, recursion),
                { Parent: ConditionalExpressionSyntax parent }
                    => Recursive(parent, recursion),
                { Parent: ConditionalAccessExpressionSyntax parent }
                    when parent.WhenNotNull == candidate
                    => Recursive(parent, recursion),
                { Parent: LambdaExpressionSyntax { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } } }
                    when invocation.IsSymbol(KnownSymbols.Task.Run, recursion.SemanticModel, recursion.CancellationToken)
                    => Recursive(invocation, recursion),
                { Parent: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: "ConfigureAwait" }, Parent: InvocationExpressionSyntax invocation } }
                    => Recursive(invocation, recursion),
                { Parent: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: "GetAwaiter" }, Parent: InvocationExpressionSyntax invocation } }
                    => Recursive(invocation, recursion),
                { Parent: MemberAccessExpressionSyntax { Expression: { } expression, Name: IdentifierNameSyntax { Identifier.ValueText: "GetResult" }, Parent: InvocationExpressionSyntax invocation } }
                    when recursion.SemanticModel.TryGetNamedType(expression, recursion.CancellationToken, out var type) &&
                         type.IsAssignableTo(KnownSymbols.INotifyCompletion, recursion.SemanticModel.Compilation)
                    => Recursive(invocation, recursion),
                { Parent: MemberAccessExpressionSyntax { Expression: { } expression, Name: IdentifierNameSyntax { Identifier.ValueText: "Result" } } memberAccess }
                    when recursion.SemanticModel.TryGetNamedType(expression, recursion.CancellationToken, out var type) &&
                         type.IsAssignableTo(KnownSymbols.Task, recursion.SemanticModel.Compilation)
                    => Recursive(memberAccess, recursion),
                { Parent: MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation } }
                    when recursion.Method(invocation) is { } target &&
                         IsIdentity(target, recursion)
                    => Recursive(invocation, recursion),
                { Parent: ReturnStatementSyntax returnStatement }
                    when returnStatement.TryFirstAncestor(out LambdaExpressionSyntax? lambda) &&
                         lambda is { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } } &&
                         invocation.IsSymbol(KnownSymbols.Task.Run, recursion.SemanticModel, recursion.CancellationToken)
                    => Recursive(invocation, recursion),
                { Parent: ParenthesizedExpressionSyntax parent }
                    => Recursive(parent, recursion),
                _ => null,
            };

            static ExpressionSyntax Recursive(ExpressionSyntax parent, Recursion recursion) => Identity(parent, recursion) ?? parent;
        }

        private static bool IsIdentity<TSource, TSymbol, TNode>(Target<TSource, TSymbol, TNode> target, Recursion recursion)
            where TSource : SyntaxNode
            where TSymbol : ISymbol
            where TNode : SyntaxNode
        {
            switch (target.Symbol)
            {
                case IMethodSymbol { DeclaringSyntaxReferences.Length: 0 } method
                     when AssumeIsIdentity(method):
                    return true;
                case IMethodSymbol { MethodKind: MethodKind.ReducedExtension, ReducedFrom.Parameters: { } parameters }
                    when target.Declaration is MethodDeclarationSyntax methodDeclaration &&
                         parameters.TryFirst(out var parameter):
                    using (var walker = Gu.Roslyn.AnalyzerExtensions.ReturnValueWalker.Borrow(methodDeclaration))
                    {
                        foreach (var returnValue in walker.ReturnValues)
                        {
                            if (returnValue is IdentifierNameSyntax identifierName &&
                                identifierName.IsSymbol(parameter, recursion.SemanticModel, recursion.CancellationToken))
                            {
                                return true;
                            }
                        }
                    }

                    break;

                case IMethodSymbol { IsStatic: false }
                    when target.Declaration is MethodDeclarationSyntax methodDeclaration:
                    using (var walker = Gu.Roslyn.AnalyzerExtensions.ReturnValueWalker.Borrow(methodDeclaration))
                    {
                        foreach (var returnValue in walker.ReturnValues)
                        {
                            switch (returnValue)
                            {
                                case ThisExpressionSyntax:
                                    return true;
                                case InvocationExpressionSyntax invocation
                                    when recursion.Target(invocation) is { Symbol: { IsStatic: false } } next &&
                                         next.Symbol.ContainingType.IsAssignableTo(target.Symbol.ContainingType, recursion.SemanticModel.Compilation):
                                    return IsIdentity(next, recursion);
                            }
                        }
                    }

                    break;
                case IFieldSymbol _:
                case IPropertySymbol _:
                    return false;
            }

            if (target.Declaration is { })
            {
                using var walker = UsagesWalker.Borrow(target.Symbol, target.Declaration, recursion.SemanticModel, recursion.CancellationToken);
                foreach (var usage in walker.Usages)
                {
                    switch (usage.Parent?.Kind())
                    {
                        case SyntaxKind.ReturnStatement:
                        case SyntaxKind.ArrowExpressionClause:
                            return true;
                    }

                    if (Identity(usage, recursion) is { } id)
                    {
                        switch (id.Parent?.Kind())
                        {
                            case SyntaxKind.ReturnStatement:
                            case SyntaxKind.ArrowExpressionClause:
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
