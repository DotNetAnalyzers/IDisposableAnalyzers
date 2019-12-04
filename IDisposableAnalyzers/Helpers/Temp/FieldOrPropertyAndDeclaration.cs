namespace IDisposableAnalyzers
{
    using System;
    using System.Threading;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal struct FieldOrPropertyAndDeclaration : IEquatable<FieldOrPropertyAndDeclaration>
    {
        internal readonly FieldOrProperty FieldOrProperty;
        internal readonly MemberDeclarationSyntax Declaration;

        internal FieldOrPropertyAndDeclaration(IFieldSymbol field, FieldDeclarationSyntax declaration)
        {
            this.FieldOrProperty = new FieldOrProperty(field);
            this.Declaration = declaration;
        }

        internal FieldOrPropertyAndDeclaration(IPropertySymbol property, PropertyDeclarationSyntax declaration)
        {
            this.FieldOrProperty = new FieldOrProperty(property);
            this.Declaration = declaration;
        }

        public static bool operator ==(FieldOrPropertyAndDeclaration left, FieldOrPropertyAndDeclaration right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FieldOrPropertyAndDeclaration left, FieldOrPropertyAndDeclaration right)
        {
            return !left.Equals(right);
        }

        public bool Equals(FieldOrPropertyAndDeclaration other)
        {
            return this.FieldOrProperty.Equals(other.FieldOrProperty);
        }

        public override bool Equals(object? obj)
        {
            return obj is FieldOrPropertyAndDeclaration other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.FieldOrProperty.GetHashCode();
        }

        internal static bool TryCreate(ISymbol memberSymbol, CancellationToken cancellationToken, out FieldOrPropertyAndDeclaration fieldOrProperty)
        {
            switch (memberSymbol)
            {
                case IFieldSymbol field
                    when field.TrySingleDeclaration(cancellationToken, out var declaration):
                    fieldOrProperty = new FieldOrPropertyAndDeclaration(field, declaration);
                    return true;
                case IPropertySymbol property
                    when property.TrySingleDeclaration(cancellationToken, out PropertyDeclarationSyntax? declaration):
                    fieldOrProperty = new FieldOrPropertyAndDeclaration(property, declaration);
                    return true;
                default:
                    fieldOrProperty = default;
                    return false;
            }
        }
    }
}
