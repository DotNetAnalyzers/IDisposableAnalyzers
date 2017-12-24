namespace IDisposableAnalyzers
{
    internal class CompositeDisposableType : QualifiedType
    {
        internal readonly QualifiedMethod Add;

        internal CompositeDisposableType()
            : base("System.Reactive.Disposables.CompositeDisposable")
        {
            this.Add = new QualifiedMethod(this, nameof(this.Add));
        }
    }
}