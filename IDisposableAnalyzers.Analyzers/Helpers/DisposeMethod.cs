namespace IDisposableAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;

    internal static class DisposeMethod
    {
        internal static bool TryFindFirst(ITypeSymbol type, Compilation compilation, Search search, out IMethodSymbol disposeMethod)
        {
            if (search == Search.TopLevel)
            {
                return TryFindIDisposableDispose(type, compilation, search, out disposeMethod) ||
                       TryFindVirtualDispose(type, compilation, search, out disposeMethod);
            }

            while (type != null &&
                   type != KnownSymbol.Object)
            {
                if (TryFindFirst(type, compilation, Search.TopLevel, out disposeMethod))
                {
                    return true;
                }

                type = type.BaseType;
            }

            disposeMethod = null;
            return false;
        }

        internal static bool TryFindIDisposableDispose(ITypeSymbol type, Compilation compilation, Search search, out IMethodSymbol disposeMethod)
        {
            disposeMethod = null;
            if (!type.IsAssignableTo(KnownSymbol.IDisposable, compilation))
            {
                return false;
            }

            if (search == Search.TopLevel)
            {
                return type.TryFindFirstMethod("Dispose", IsIDisposableDispose, out disposeMethod);
            }

            return type.TryFindFirstMethodRecursive("Dispose", IsIDisposableDispose, out disposeMethod);

            bool IsIDisposableDispose(IMethodSymbol candidate)
            {
                return candidate.Name == "Dispose" &&
                       candidate.ReturnsVoid &&
                       candidate.Parameters.Length == 0 &&
                       candidate.DeclaredAccessibility == Accessibility.Public;
            }
        }

        internal static bool TryFindVirtualDispose(ITypeSymbol type, Compilation compilation, Search search, out IMethodSymbol disposeMethod)
        {
            disposeMethod = null;
            if (!type.IsAssignableTo(KnownSymbol.IDisposable, compilation))
            {
                return false;
            }

            if (search == Search.TopLevel)
            {
                return type.TryFindFirstMethod("Dispose", IsIDisposableDispose, out disposeMethod);
            }

            return type.TryFindFirstMethodRecursive("Dispose", IsIDisposableDispose, out disposeMethod);

            bool IsIDisposableDispose(IMethodSymbol candidate)
            {
                return IsOverrideDispose(candidate) ||
                       IsVirtualDispose(candidate);
            }
        }

        internal static bool TryFindBaseVirtual(ITypeSymbol type, out IMethodSymbol result)
        {
            return type.TryFindFirstMethodRecursive("Dispose", IsVirtualDispose, out result);
        }

        internal static bool IsOverrideDispose(IMethodSymbol candidate)
        {
            return candidate.IsOverride &&
                   candidate.ReturnsVoid &&
                   candidate.Parameters.TrySingle(out var parameter) &&
                   parameter.Type == KnownSymbol.Boolean;
        }

        internal static bool IsVirtualDispose(IMethodSymbol candidate)
        {
            return candidate.IsVirtual &&
                   candidate.ReturnsVoid &&
                   candidate.Parameters.TrySingle(out var parameter) &&
                   parameter.Type == KnownSymbol.Boolean;
        }

        internal static bool IsIDisposableDispose(IMethodSymbol candidate, Compilation compilation)
        {
            return candidate != null &&
                   candidate.Name == "Dispose" &&
                   candidate.ReturnsVoid &&
                   candidate.Parameters.Length == 0 &&
                   candidate.DeclaredAccessibility == Accessibility.Public &&
                   candidate.ContainingType.IsAssignableTo(KnownSymbol.IDisposable, compilation);
        }
    }
}
