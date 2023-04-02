namespace IDisposableAnalyzers;

using System;
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
                if (Scope() is { } scope &&
                    usage.FirstAncestor<IfStatementSyntax>() is { Statement: { } statement } ifStatement &&
                    scope.Contains(ifStatement))
                {
                    if (statement.Contains(usage))
                    {
                        // check that other branch is handled.
                        foreach (var other in walker.Usages)
                        {
                            if (scope.Contains(other) &&
                                other.SpanStart > statement.Span.End &&
                                ShouldNotDispose(other, recursion))
                            {
                                return false;
                            }
                        }

                        return true;
                    }

                    if (!statement.Contains(usage))
                    {
                        // check that other branch is handled.
                        foreach (var other in walker.Usages)
                        {
                            if (statement.Contains(other) &&
                                ShouldNotDispose(other, recursion))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }

                return false;
            }
        }

        return true;

        SyntaxNode? Scope()
        {
            return localOrParameter.Symbol switch
            {
                IParameterSymbol { ContainingSymbol.DeclaringSyntaxReferences: { Length: 1 } references }
                    => references[0].GetSyntax(cancellationToken),
                ILocalSymbol { DeclaringSyntaxReferences: { Length: 1 } references }
                    => references[0].GetSyntax(cancellationToken).FirstAncestor<BlockSyntax>(),
                _ => throw new InvalidOperationException("Should never get here."),
            };
        }

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
