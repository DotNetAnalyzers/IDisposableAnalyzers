// ReSharper disable All
namespace ValidCode.Reflection;

using System;
using System.Reflection;
using System.Text;

public class Cases
{
    public static void ActivatorCreateInstanceGeneric()
    {
        using var disposable = Activator.CreateInstance<Disposable>();
        var builder = Activator.CreateInstance<StringBuilder>();
    }

    public static void ActivatorCreateInstance()
    {
        using var disposable = (IDisposable?)Activator.CreateInstance(typeof(Disposable));
        var builder1 = Activator.CreateInstance(typeof(StringBuilder));
        var builder2 = (StringBuilder)Activator.CreateInstance(typeof(StringBuilder))!;
    }

    public static void ConstructorInfoInvoke(ConstructorInfo constructorInfo)
    {
        using var disposable = (IDisposable)constructorInfo.Invoke(null);
        var o = constructorInfo.Invoke(null); ;
    }
}
