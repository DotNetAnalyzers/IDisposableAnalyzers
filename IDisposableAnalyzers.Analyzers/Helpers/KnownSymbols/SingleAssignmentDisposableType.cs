namespace IDisposableAnalyzers
{
    internal class SingleAssignmentDisposableType : QualifiedType
    {
        internal readonly QualifiedProperty Disposable;

        internal SingleAssignmentDisposableType()
            : base("System.Reactive.Disposables.SingleAssignmentDisposable")
        {
            this.Disposable = new QualifiedProperty(this, nameof(this.Disposable));
        }
    }
}
