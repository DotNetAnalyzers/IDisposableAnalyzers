namespace IDisposableAnnotations
{
    using System;

    /// <summary>
    /// The ownership of instance is transferred and the receiver is responsible for disposing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class TransferOwnershipAttribute : Attribute
    {
    }
}
