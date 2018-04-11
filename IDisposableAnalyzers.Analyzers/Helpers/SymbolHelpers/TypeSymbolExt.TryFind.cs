namespace IDisposableAnalyzers
{
    using System;
    using Microsoft.CodeAnalysis;

    internal static partial class TypeSymbolExt
    {
        internal static bool TryFindField(this ITypeSymbol type, string name, out IFieldSymbol field)
        {
            return type.TryFindSingleMember(name, out field);
        }

        internal static bool TryFindEvent(this ITypeSymbol type, string name, out IEventSymbol @event)
        {
            return type.TryFindSingleMember(name, out @event);
        }

        internal static bool TryFindProperty(this ITypeSymbol type, string name, out IPropertySymbol property)
        {
            if (name == "Item[]")
            {
                return type.TryFindSingleMember(x => x.IsIndexer, out property);
            }

            return type.TryFindSingleMember(name, out property);
        }

        internal static bool TryFindFirstMethod(this ITypeSymbol type, string name, out IMethodSymbol result)
        {
            return type.TryFindFirstMember(name, out result);
        }

        internal static bool TryFindFirstMethod(this ITypeSymbol type, Func<IMethodSymbol, bool> predicate, out IMethodSymbol result)
        {
            return type.TryFindFirstMember(predicate, out result);
        }

        internal static bool TryFindSingleMethod(this ITypeSymbol type, string name, out IMethodSymbol result)
        {
            return type.TryFindSingleMember(name, out result);
        }

        internal static bool TryFindSingleMethod(this ITypeSymbol type, Func<IMethodSymbol, bool> predicate, out IMethodSymbol result)
        {
            return type.TryFindSingleMember(predicate, out result);
        }

        internal static bool TryFindSingleMethod(this ITypeSymbol type, string name, Func<IMethodSymbol, bool> predicate, out IMethodSymbol result)
        {
            return type.TryFindSingleMember(name, predicate, out result);
        }

        internal static bool TryFindFirstMethod(this ITypeSymbol type, string name, Func<IMethodSymbol, bool> predicate, out IMethodSymbol property)
        {
            return type.TryFindFirstMember(name, predicate, out property);
        }

        internal static bool TryFindFirstMember(this ITypeSymbol type, string name, out ISymbol result)
        {
            return type.TryFindFirstMember<ISymbol>(name, out result);
        }

        private static bool TryFindSingleMember<TMember>(this ITypeSymbol type, string name, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null ||
                string.IsNullOrEmpty(name))
            {
                return false;
            }

            foreach (var symbol in type.GetMembers(name))
            {
                if (member != null)
                {
                    member = null;
                    return false;
                }

                member = symbol as TMember;
            }

            return member != null;
        }

        private static bool TryFindSingleMember<TMember>(this ITypeSymbol type, Func<TMember, bool> predicate, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null ||
                predicate == null)
            {
                return false;
            }

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

            return member != null;
        }

        private static bool TryFindSingleMember<TMember>(this ITypeSymbol type, string name, Func<TMember, bool> predicate, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null ||
                predicate == null)
            {
                return false;
            }

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

            return member != null;
        }

        private static bool TryFindFirstMember<TMember>(this ITypeSymbol type, Func<TMember, bool> predicate, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null ||
                predicate == null)
            {
                return false;
            }

            foreach (var symbol in type.GetMembers())
            {
                if (symbol is TMember candidate &&
                    predicate(candidate))
                {
                    member = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindFirstMember<TMember>(this ITypeSymbol type, string name, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null)
            {
                return false;
            }

            foreach (var symbol in type.GetMembers(name))
            {
                if (symbol is TMember candidate)
                {
                    member = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindFirstMember<TMember>(this ITypeSymbol type, string name, Func<TMember, bool> predicate, out TMember member)
            where TMember : class, ISymbol
        {
            member = null;
            if (type == null ||
                predicate == null)
            {
                return false;
            }

            foreach (var symbol in type.GetMembers(name))
            {
                if (symbol is TMember candidate &&
                    predicate(candidate))
                {
                    member = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
