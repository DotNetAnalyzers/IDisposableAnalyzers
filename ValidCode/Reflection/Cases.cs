using System;
using System.Reflection;

namespace ValidCode.Reflection
{
    public class Cases
    {
        public static void ActivatorCreateInstanceOfDisposable()
        {
            using var disposable = Activator.CreateInstance<Disposable>();
        }

        public static void ActivatorCreateInstance()
        {
            using var disposable = (IDisposable)Activator.CreateInstance(typeof(Disposable));
        }

        public static void ConstructorInfoInvoke(ConstructorInfo constructorInfo)
        {
            using var disposable = (IDisposable)constructorInfo.Invoke(null);
        }
    }
}
