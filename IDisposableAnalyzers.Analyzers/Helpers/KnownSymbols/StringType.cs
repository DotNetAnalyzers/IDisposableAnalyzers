namespace IDisposableAnalyzers
{
    internal class StringType : QualifiedType
    {
        internal readonly QualifiedMethod Format;

        internal StringType()
            : base("System.String", "string")
        {
            this.Format = new QualifiedMethod(this, nameof(this.Format));
        }
    }
}
