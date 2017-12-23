namespace IDisposableAnalyzers
{
    internal class ResourceManagerType : QualifiedType
    {
        internal readonly QualifiedMethod GetStream;
        internal readonly QualifiedMethod GetResourceSet;

        internal ResourceManagerType()
            : base("System.Resources.ResourceManager")
        {
            this.GetStream = new QualifiedMethod(this, nameof(this.GetStream));
            this.GetResourceSet = new QualifiedMethod(this, nameof(this.GetResourceSet));
        }
    }
}
