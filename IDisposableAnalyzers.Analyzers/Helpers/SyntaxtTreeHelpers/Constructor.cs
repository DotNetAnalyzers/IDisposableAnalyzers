namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Constructor
    {
        internal static bool IsRunBefore(this ConstructorDeclarationSyntax ctor, ConstructorDeclarationSyntax otherDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (ctor == otherDeclaration)
            {
                return false;
            }

            var first = semanticModel.GetDeclaredSymbolSafe(ctor, cancellationToken);
            var other = semanticModel.GetDeclaredSymbolSafe(otherDeclaration, cancellationToken);
            return IsRunBefore(first, other, semanticModel, cancellationToken);
        }

        internal static bool IsRunBefore(IMethodSymbol first, IMethodSymbol other, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (first == null ||
                other == null)
            {
                return false;
            }

            if (TryGetInitializer(other, cancellationToken, out ConstructorInitializerSyntax initializer))
            {
                if (SymbolComparer.Equals(first.ContainingType, other.ContainingType) &&
                    !initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.ThisKeyword))
                {
                    return false;
                }

                if (!other.ContainingType.Is(first.ContainingType) &&
                    initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword))
                {
                    return false;
                }
            }
            else
            {
                if (SymbolComparer.Equals(first.ContainingType, other.ContainingType) ||
                    !other.ContainingType.Is(first.ContainingType))
                {
                    return false;
                }
            }

            var next = semanticModel.GetSymbolSafe(initializer, cancellationToken);
            if (SymbolComparer.Equals(first, next))
            {
                return true;
            }

            if (next == null)
            {
                if (TryGetDefault(other.ContainingType?.BaseType, out next))
                {
                    return SymbolComparer.Equals(first, next);
                }

                return false;
            }

            return IsRunBefore(first, next, semanticModel, cancellationToken);
        }

        internal static bool TryGetDefault(INamedTypeSymbol type, out IMethodSymbol result)
        {
            result = null;
            while (type != null && type != KnownSymbol.Object)
            {
                bool found = false;
                foreach (var ctorSymbol in type.Constructors)
                {
                    if (ctorSymbol.Parameters.Length == 0)
                    {
                        found = true;
                        if (ctorSymbol.DeclaringSyntaxReferences.Length != 0)
                        {
                            result = ctorSymbol;
                            return true;
                        }
                    }
                }

                if (!found)
                {
                    return false;
                }

                type = type.BaseType;
            }

            return false;
        }

        internal static void AddRunBefore(SyntaxNode context, HashSet<IMethodSymbol> ctorsRunBefore, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                return;
            }

            var contextCtor = context.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (contextCtor == null)
            {
                var type = (INamedTypeSymbol)semanticModel.GetDeclaredSymbolSafe(context.FirstAncestorOrSelf<TypeDeclarationSyntax>(), cancellationToken);
                if (type == null)
                {
                    return;
                }

                if (type.Constructors.Length != 0)
                {
                    foreach (var ctor in type.Constructors)
                    {
                        foreach (var reference in ctor.DeclaringSyntaxReferences)
                        {
                            var ctorDeclaration = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                            ctorsRunBefore.Add(ctor).IgnoreReturnValue();
                            AddCtorsRecursively(ctorDeclaration, ctorsRunBefore, semanticModel, cancellationToken);
                        }
                    }
                }
                else
                {
                    if (TryGetDefault(type, out IMethodSymbol ctor))
                    {
                        foreach (var reference in ctor.DeclaringSyntaxReferences)
                        {
                            var ctorDeclaration = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                            ctorsRunBefore.Add(ctor).IgnoreReturnValue();
                            AddCtorsRecursively(ctorDeclaration, ctorsRunBefore, semanticModel, cancellationToken);
                        }
                    }
                }
            }
            else
            {
                AddCtorsRecursively(contextCtor, ctorsRunBefore, semanticModel, cancellationToken);
            }
        }

        private static void AddCtorsRecursively(ConstructorDeclarationSyntax ctor, HashSet<IMethodSymbol> ctorsRunBefore, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (ctor.Initializer != null)
            {
                var nestedCtor = semanticModel.GetSymbolSafe(ctor.Initializer, cancellationToken);
                if (nestedCtor == null)
                {
                    return;
                }

                foreach (var reference in nestedCtor.DeclaringSyntaxReferences)
                {
                    var runBefore = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                    ctorsRunBefore.Add(nestedCtor).IgnoreReturnValue();
                    AddCtorsRecursively(runBefore, ctorsRunBefore, semanticModel, cancellationToken);
                }
            }
            else
            {
                var baseType = semanticModel.GetDeclaredSymbolSafe(ctor, cancellationToken)
                                            .ContainingType.BaseType;
                if (TryGetDefault(baseType, out IMethodSymbol defaultCtor))
                {
                    foreach (var reference in defaultCtor.DeclaringSyntaxReferences)
                    {
                        ctorsRunBefore.Add(defaultCtor).IgnoreReturnValue();
                        var runBefore = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                        AddCtorsRecursively(runBefore, ctorsRunBefore, semanticModel, cancellationToken);
                    }
                }
            }
        }

        private static bool TryGetInitializer(IMethodSymbol ctor, CancellationToken cancellationToken, out ConstructorInitializerSyntax initializer)
        {
            initializer = null;
            if (ctor == null ||
                ctor.MethodKind != MethodKind.Constructor)
            {
                return false;
            }

            foreach (var reference in ctor.DeclaringSyntaxReferences)
            {
                var declaration = (ConstructorDeclarationSyntax)reference.GetSyntax(cancellationToken);
                initializer = declaration.Initializer;
                if (initializer != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
