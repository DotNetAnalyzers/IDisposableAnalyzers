#pragma warning disable 660,661 // using a hack with operator overloads
namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [System.Diagnostics.DebuggerDisplay("{System.String.Join(\".\", Parts)}")]
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
            System.Diagnostics.Debug.Assert(parts.Count != 0, "Parts.Length != 0");
            return new NamespaceParts(parts.RemoveAt(parts.Count - 1));
        }

        internal bool Matches(NameSyntax nameSyntax)
        {
            return this.Matches(nameSyntax, this.parts.Count - 1);
        }

        private bool Matches(NameSyntax nameSyntax, int index)
        {
            if (nameSyntax is IdentifierNameSyntax identifier)
            {
                return index == 0 &&
                       identifier.Identifier.ValueText == this.parts[0];
            }

            if (nameSyntax is QualifiedNameSyntax qns)
            {
                if (index < 1)
                {
                    return false;
                }

                if (qns.Right.Identifier.ValueText != this.parts[index])
                {
                    return false;
                }

                return this.Matches(qns.Left, index - 1);
            }

            return false;
        }
    }
}
