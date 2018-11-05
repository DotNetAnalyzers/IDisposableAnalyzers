namespace IDisposableAnnotations
{
    using System;

    /// <summary>
    /// The return value should not be disposed by caller.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class DoNotDisposeAttribute : Attribute
    {
    }
}
