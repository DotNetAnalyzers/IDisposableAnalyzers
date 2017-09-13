namespace IDisposableAnalyzers
{
    internal class StringType : QualifiedType
    {
        internal readonly QualifiedMethod Format;

        internal StringType()
            : base("System.String")
        {
            this.Format = new QualifiedMethod(this, nameof(this.Format));
        }
    }
}