#pragma warning disable 660,661 // using a hack with operator overloads
namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    // ReSharper disable once UseNameofExpression
        [System.Diagnostics.DebuggerDisplay("FullName: {FullName}")]
    internal class QualifiedType
    {
        internal readonly string FullName;
        internal readonly NamespaceParts Namespace;
        internal readonly string Type;

        internal QualifiedType(string fullName)
            : this(fullName, NamespaceParts.Create(fullName), fullName.Substring(fullName.LastIndexOf('.') + 1))
        {
        }

        private QualifiedType(string fullName, NamespaceParts @namespace, string type)
        {
            this.FullName = fullName;
            this.Namespace = @namespace;
            this.Type = type;
        }

        public static bool operator ==(ITypeSymbol left, QualifiedType right)
        {
            if (left == null && right == null)
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            return left.MetadataName == right.Type &&
                   left.ContainingNamespace == right.Namespace;
        }

        public static bool operator !=(ITypeSymbol left, QualifiedType right) => !(left == right);
    }
}