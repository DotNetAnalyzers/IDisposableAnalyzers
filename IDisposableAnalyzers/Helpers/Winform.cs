namespace IDisposableAnalyzers;

using System.Threading;
using Gu.Roslyn.AnalyzerExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class Winform
{
    internal static bool IsComponentsAdd(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        return invocation switch
        {
            { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "components" }, Name.Identifier.ValueText: "Add" }, ArgumentList.Arguments.Count: 1 }
            => IsInWinForm(),
            { Expression: MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: InstanceExpressionSyntax _, Name.Identifier.ValueText: "components" }, Name.Identifier.ValueText: "Add" }, ArgumentList.Arguments.Count: 1 }
            => IsInWinForm(),
            _ => false,
        };

        bool IsInWinForm()
        {
            return invocation.TryFirstAncestor(out ClassDeclarationSyntax? containingClass) &&
                   semanticModel.TryGetNamedType(containingClass, cancellationToken, out var containingType) &&
                   containingType.IsAssignableTo(KnownSymbols.SystemWindowsFormsForm, semanticModel.Compilation);
        }
    }

    internal static bool IsAddedToComponents(FieldOrProperty member, INamedTypeSymbol context, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (context.TrySingleDeclaration(cancellationToken, out ClassDeclarationSyntax? containingType))
        {
            using var walker = UsagesWalker.Borrow(member.Symbol, containingType, semanticModel, cancellationToken);
            foreach (var usage in walker.Usages)
            {
                switch (usage)
                {
                    case { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } }
                        when IsComponentsAdd(invocation, semanticModel, cancellationToken):
                        return true;
                    case { Parent: MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax _, Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } } } }
                        when IsComponentsAdd(invocation, semanticModel, cancellationToken):
                        return true;
                }
            }
        }

        return false;
    }
}
