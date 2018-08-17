namespace Stubs
{
    using System;

    public static class Extensions
    {
        /// <summary>
        /// Test method that returns a different type than the in parameter. We assume that the method creates a new disposable then.
        /// </summary>
        public static ICustomDisposable AsCustom(this IDisposable disposable) => default(ICustomDisposable);

        /// <summary>
        /// Test method that returns the same type as the in parameter. We assume that the method does not create a new disposable then.
        /// </summary>
        public static IDisposable Fluent(this IDisposable disposable) => disposable;
    }
}
