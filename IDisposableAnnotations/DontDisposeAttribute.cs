namespace IDisposableAnnotations
{
    using System;

    /// <summary>
    /// The return value should not be disposed by caller.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
    public class DontDisposeAttribute : Attribute
    {
    }
}
