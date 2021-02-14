namespace IDisposableAnalyzers
{
    using System.Threading;

    using Gu.Roslyn.AnalyzerExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        internal static bool ShouldDispose(LocalOrParameter localOrParameter, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (localOrParameter.Symbol is IParameterSymbol { RefKind: not RefKind.None })
            {
                return false;
            }

            if (localOrParameter.Type.IsAssignableTo(KnownSymbols.Task, semanticModel.Compilation))
            {
                return false;
            }

            using var recursion = Recursion.Borrow(localOrParameter.Symbol.ContainingType, semanticModel, cancellationToken);
            using var walker = UsagesWalker.Borrow(localOrParameter, semanticModel, cancellationToken);
            foreach (var usage in walker.Usages)
            {
                if (ShouldNotDispose(usage, recursion))
                {
                    if (usage.FirstAncestor<IfStatementSyntax>() is { Statement: { } statement } &&
                        usage.FirstAncestor<MemberDeclarationSyntax>() is { } member &&
                        statement.Contains(usage))
                    {
                        // check that other branch is handled.
                        foreach (var other in walker.Usages)
                        {
                            if (member.Contains(other) &&
                                other.SpanStart > statement.Span.End &&
                                ShouldNotDispose(other, recursion))
                            {
                                return false;
                            }
                        }

                        return true;
                    }

                    return false;
                }
            }

            return true;

            static bool ShouldNotDispose(IdentifierNameSyntax usage, Recursion recursion)
            {
                if (Returns(usage, recursion))
                {
                    return true;
                }

                if (Assigns(usage, recursion, out _))
                {
                    return true;
                }

                if (Stores(usage, recursion, out _))
                {
                    return true;
                }

                if (Disposes(usage, recursion))
                {
                    return true;
                }

                if (DisposedByReturnValue(usage, recursion, out _))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
