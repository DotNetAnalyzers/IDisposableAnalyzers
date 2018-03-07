namespace IDisposableAnalyzers
{
    using System;
    using Microsoft.CodeAnalysis;

    internal static partial class TypeSymbolExt
    {
        internal static bool TryGetFieldRecursive(this ITypeSymbol type, string name, out IFieldSymbol field)
        {
            return type.TrySingleMemberRecursive(name, out field);
        }

        internal static bool TryGetEventRecursive(this ITypeSymbol type, string name, out IEventSymbol @event)
        {
            return type.TrySingleMemberRecursive(name, out @event);
        }

        internal static bool TryGetPropertyRecursive(this ITypeSymbol type, string name, out IPropertySymbol property)
        {
            if (name == "Item[]")
            {
                return type.TrySingleMemberRecursive(x => x.IsIndexer, out property);
            }

            return type.TrySingleMemberRecursive(name, out property);
        }

        internal static bool TryFirstMethodRecursive(this ITypeSymbol type, string name, out IMethodSymbol result)
        {
            return type.TryFirstMemberRecursive(name, out result);
        }

        internal static bool TryFirstMethodRecursive(this ITypeSymbol type, Func<IMethodSymbol, bool> predicate, out IMethodSymbol result)
        {
            return type.TryFirstMemberRecursive(predicate, out result);
        }

        internal static bool TrySingleMethodRecursive(this ITypeSymbol type, string name, out IMethodSymbol result)
        {
            return type.TrySingleMemberRecursive(name, out result);
        }

        internal static bool TrySingleMethodRecursive(this ITypeSymbol type, Func<IMethodSymbol, bool> predicate, out IMethodSymbol result)
        {
            return type.TrySingleMemberRecursive(predicate, out result);
        }

        internal static bool TrySingleMethodRecursive(this ITypeSymbol type, string name, Func<IMethodSymbol, bool> predicate, out IMethodSymbol result)
        {
            return type.TrySingleMemberRecursive(name, predicate, out result);
        }

        internal static bool TryFirstMethodRecursive(this ITypeSymbol type, string name, Func<IMethodSymbol, bool> predicate, out IMethodSymbol property)
        {
            return type.TrySingleMemberRecursive(name, predicate, out property);
        }

        internal static bool TrySingleMemberRecursive<TMember>(this ITypeSymbol type, string name, out TMember member)
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

        internal static bool TrySingleMemberRecursive<TMember>(this ITypeSymbol type, Func<TMember, bool> predicate, out TMember member)
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

        internal static bool TrySingleMemberRecursive<TMember>(this ITypeSymbol type, string name, Func<TMember, bool> predicate, out TMember member)
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
