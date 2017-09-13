namespace IDisposableAnalyzers
{
    internal class PasswordBoxType : QualifiedType
    {
        internal readonly QualifiedProperty SecurePassword;

        internal PasswordBoxType()
            : base("System.Windows.Controls.PasswordBox")
        {
            this.SecurePassword = new QualifiedProperty(this, nameof(this.SecurePassword));
        }
    }
}