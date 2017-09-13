namespace IDisposableAnalyzers
{
    // ReSharper disable once InconsistentNaming
    internal class IListType : QualifiedType
    {
        internal readonly QualifiedMethod Add;
        internal readonly QualifiedMethod Remove;

        public IListType()
            : base("System.Collections.IList")
        {
            this.Add = new QualifiedMethod(this, nameof(this.Add));
            this.Remove = new QualifiedMethod(this, nameof(this.Remove));
        }
    }
}