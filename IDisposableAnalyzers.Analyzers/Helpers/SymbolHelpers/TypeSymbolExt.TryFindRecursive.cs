namespace IDisposableAnalyzers
{
    using System;
    using Microsoft.CodeAnalysis;

    internal static partial class TypeSymbolExt
    {
        internal static bool TryFindFieldRecursive(this ITypeSymbol type, string name, out IFieldSymbol field)
        {
            return type.TryFindSingleMemberRecursive(name, out field);
        }

        internal static bool TryFindEventRecursive(this ITypeSymbol type, string name, out IEventSymbol @event)
        {
            return type.TryFindSingleMemberRecursive(name, out @event);
        }

        internal static bool TryFindPropertyRecursive(this ITypeSymbol type, string name, out IPropertySymbol property)
        {
            if (name == "Item[]")
            {
                return type.TryFindSingleMemberRecursive(x => x.IsIndexer, out property);
            }

            return type.TryFindSingleMemberRecursive(name, out property);
        }

        internal static bool TryFindFirstMethodRecursive(this ITypeSymbol type, string name, out IMethodSymbol result)
        {
            return type.TryFirstMemberRecursive(name, out result);
        }

        internal static bool TryFindFirstMethodRecursive(this ITypeSymbol type, Func<IMethodSymbol, bool> predicate, out IMethodSymbol result)
        {
            return type.TryFirstMemberRecursive(predicate, out result);
        }

        internal static bool TryFindSingleMethodRecursive(this ITypeSymbol type, string name, out IMethodSymbol result)
        {
            return type.TryFindSingleMemberRecursive(name, out result);
        }

        internal static bool TryFindSingleMethodRecursive(this ITypeSymbol type, Func<IMethodSymbol, bool> predicate, out IMethodSymbol result)
        {
            return type.TryFindSingleMemberRecursive(predicate, out result);
        }

        internal static bool TryFindSingleMethodRecursive(this ITypeSymbol type, string name, Func<IMethodSymbol, bool> predicate, out IMethodSymbol result)
        {
            return type.TryFindSingleMemberRecursive(name, predicate, out result);
        }

        internal static bool TryFindFirstMethodRecursive(this ITypeSymbol type, string name, Func<IMethodSymbol, bool> predicate, out IMethodSymbol property)
        {
            return type.TryFindSingleMemberRecursive(name, predicate, out property);
        }

        internal static bool TryFindSingleMemberRecursive<TMember>(this ITypeSymbol type, string name, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null ||
                string.IsNullOrEmpty(name))
            {
                return false;
            }

            while (type != null &&
                   type != KnownSymbol.Object)
            {
                foreach (var symbol in type.GetMembers(name))
                {
                    if (member != null)
                    {
                        member = null;
                        return false;
                    }

                    member = symbol as TMember;
                }

                type = type.BaseType;
            }

            return member != null;
        }

        internal static bool TryFindSingleMemberRecursive<TMember>(this ITypeSymbol type, Func<TMember, bool> predicate, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null ||
                predicate == null)
            {
                return false;
            }

            while (type != null &&
                   type != KnownSymbol.Object)
            {
                foreach (var symbol in type.GetMembers())
                {
                    if (symbol is TMember candidate &&
                        predicate(candidate))
                    {
                        if (member != null)
                        {
                            member = null;
                            return false;
                        }

                        member = candidate;
                    }
                }

                type = type.BaseType;
            }

            return member != null;
        }

        internal static bool TryFindSingleMemberRecursive<TMember>(this ITypeSymbol type, string name, Func<TMember, bool> predicate, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null ||
                predicate == null)
            {
                return false;
            }

            while (type != null &&
                   type != KnownSymbol.Object)
            {
                foreach (var symbol in type.GetMembers(name))
                {
                    if (symbol is TMember candidate &&
                        predicate(candidate))
                    {
                        if (member != null)
                        {
                            member = null;
                            return false;
                        }

                        member = candidate;
                    }
                }

                type = type.BaseType;
            }

            return member != null;
        }

        internal static bool TryFirstMemberRecursive<TMember>(this ITypeSymbol type, Func<TMember, bool> predicate, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null ||
                predicate == null)
            {
                return false;
            }

            while (type != null &&
                   type != KnownSymbol.Object)
            {
                foreach (var symbol in type.GetMembers())
                {
                    if (symbol is TMember candidate &&
                        predicate(candidate))
                    {
                        member = candidate;
                        return true;
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        internal static bool TryFirstMemberRecursive<TMember>(this ITypeSymbol type, string name, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null)
            {
                return false;
            }

            while (type != null &&
                   type != KnownSymbol.Object)
            {
                foreach (var symbol in type.GetMembers(name))
                {
                    if (symbol is TMember candidate)
                    {
                        member = candidate;
                        return true;
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        internal static bool TryFirstMemberRecursive<TMember>(this ITypeSymbol type, string name, Func<TMember, bool> predicate, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null ||
                predicate == null)
            {
                return false;
            }

            while (type != null &&
                   type != KnownSymbol.Object)
            {
                foreach (var symbol in type.GetMembers(name))
                {
                    if (symbol is TMember candidate &&
                        predicate(candidate))
                    {
                        member = candidate;
                        return true;
                    }
                }

                type = type.BaseType;
            }

            return false;
        }
    }
}
