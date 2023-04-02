// ReSharper disable All
namespace ValidCode;

using System;
using System.Windows.Forms;

public class UsingWinforms
{
    public UsingWinforms(Form form)
    {
        var a = Control.FromHandle(IntPtr.Zero);
        var b = Control.FromChildHandle(IntPtr.Zero);
        var e = form.FindForm();
    }
}
