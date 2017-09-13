namespace IDisposableAnalyzers
{
    internal class XunitAssertType : QualifiedType
    {
        internal readonly QualifiedMethod Equal;

        internal XunitAssertType()
            : base("Xunit.Assert")
        {
            this.Equal = new QualifiedMethod(this, nameof(this.Equal));
        }
    }
}