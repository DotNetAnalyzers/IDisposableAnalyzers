namespace IDisposableAnalyzers
{
    internal class StringBuilderType : QualifiedType
    {
        internal readonly QualifiedMethod AppendLine;
        internal readonly QualifiedMethod Append;
        internal readonly QualifiedMethod AppendFormat;

        internal StringBuilderType()
            : base("System.Text.StringBuilder")
        {
            this.AppendLine = new QualifiedMethod(this, nameof(this.AppendLine));
            this.Append = new QualifiedMethod(this, nameof(this.Append));
            this.AppendFormat = new QualifiedMethod(this, nameof(this.AppendFormat));
        }
    }
}