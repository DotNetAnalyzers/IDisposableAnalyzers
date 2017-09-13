namespace IDisposableAnalyzers
{
    internal class FileType : QualifiedType
    {
        internal readonly QualifiedMethod OpenText;
        internal readonly QualifiedMethod CreateText;
        internal readonly QualifiedMethod AppendText;
        internal readonly QualifiedMethod Create;
        internal readonly QualifiedMethod Open;
        internal readonly QualifiedMethod OpenRead;
        internal readonly QualifiedMethod OpenWrite;

        internal FileType()
            : base("System.IO.File")
        {
            this.OpenText = new QualifiedMethod(this, nameof(this.OpenText));
            this.CreateText = new QualifiedMethod(this, nameof(this.CreateText));
            this.AppendText = new QualifiedMethod(this, nameof(this.AppendText));
            this.Create = new QualifiedMethod(this, nameof(this.Create));
            this.Open = new QualifiedMethod(this, nameof(this.Open));
            this.OpenRead = new QualifiedMethod(this, nameof(this.OpenRead));
            this.OpenWrite = new QualifiedMethod(this, nameof(this.OpenWrite));
        }
    }
}