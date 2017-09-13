namespace IDisposableAnalyzers
{
    internal class DependencyPropertyType : QualifiedType
    {
        internal readonly QualifiedMethod Register;
        internal readonly QualifiedMethod RegisterReadOnly;
        internal readonly QualifiedMethod RegisterAttached;
        internal readonly QualifiedMethod RegisterAttachedReadOnly;

        internal readonly QualifiedMethod AddOwner;

        internal DependencyPropertyType()
            : base("System.Windows.DependencyProperty")
        {
            this.Register = new QualifiedMethod(this, nameof(this.Register));
            this.RegisterReadOnly = new QualifiedMethod(this, nameof(this.RegisterReadOnly));
            this.RegisterAttached = new QualifiedMethod(this, nameof(this.RegisterAttached));
            this.RegisterAttachedReadOnly = new QualifiedMethod(this, nameof(this.RegisterAttachedReadOnly));

            this.AddOwner = new QualifiedMethod(this, nameof(this.AddOwner));
        }
    }
}