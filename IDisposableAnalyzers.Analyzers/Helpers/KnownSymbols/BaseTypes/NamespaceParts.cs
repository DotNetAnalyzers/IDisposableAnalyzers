#pragma warning disable 660,661 // using a hack with operator overloads
namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;

    [System.Diagnostics.DebuggerDisplay("{System.String.Join(\".\", parts)}")]
    internal class NamespaceParts
    {
        private readonly ImmutableList<string> parts;

        public NamespaceParts(ImmutableList<string> parts)
        {
            this.parts = parts;
        }

        public static bool operator ==(INamespaceSymbol left, NamespaceParts right)
        {
            if (left == null && right == null)
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            var ns = left;
            for (var i = right.parts.Count - 1; i >= 0; i--)
            {
                if (ns == null || ns.IsGlobalNamespace)
                {
                    return false;
                }

                if (ns.Name != right.parts[i])
                {
                    return false;
                }

                ns = ns.ContainingNamespace;
            }

            return ns?.IsGlobalNamespace == true;
        }

        public static bool operator !=(INamespaceSymbol left, NamespaceParts right) => !(left == right);

        internal static NamespaceParts Create(string qualifiedName)
        {
            var parts = qualifiedName.Split('.').ToImmutableList();
            System.Diagnostics.Debug.Assert(parts.Count != 0, "parts.Length != 0");
            return new NamespaceParts(parts.RemoveAt(parts.Count - 1));
        }
    }
}