namespace IDisposableAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal class QualifiedProperty : QualifiedMember<IPropertySymbol>
    {
        public QualifiedProperty(QualifiedType containingType, string name)
            : base(containingType, name)
        {
        }
    }
}