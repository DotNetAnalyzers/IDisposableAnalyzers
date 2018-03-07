namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class TypeSyntaxExt
    {
        internal static bool IsVoid(this TypeSyntax type)
        {
            return type is PredefinedTypeSyntax predefinedType &&
                   predefinedType.Keyword.ValueText == "void";
        }
    }
}
