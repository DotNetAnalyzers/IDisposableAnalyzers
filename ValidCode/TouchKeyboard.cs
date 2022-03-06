// ReSharper disable All
namespace ValidCode;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

public static class TouchKeyboard
{
    private static TouchProcessInfo? touchProcessInfo = TouchProcessInfo.Default;
    private static Process? process;

    /// <summary>
    /// Gets or sets the path to the exe.
    /// </summary>
    public static string? TouchKeyboardPath
    {
        get => touchProcessInfo?.ProcessStartInfo.FileName;
        set => touchProcessInfo = TouchProcessInfo.Create(value);
    }

    /// <summary>
    /// Show the on screen keyboard.
    /// </summary>
    public static void Show()
    {
        if (touchProcessInfo?.ProcessStartInfo is null)
        {
            return;
        }
        
        process?.Dispose();
        process = Process.Start(touchProcessInfo.ProcessStartInfo);
    }

    /// <summary>
    /// Hide the on screen keyboard.
    /// </summary>
    public static void Hide()
    {
        if (touchProcessInfo?.ProcessStartInfo is null)
        {
            return;
        }

        // http://mheironimus.blogspot.se/2015/05/adding-touch-keyboard-support-to-wpf.html
        var keyboardWnd = NativeMethods.FindWindow("IPTip_Main_Window", null);
        var nullIntPtr = new IntPtr(0);
        const uint wmSysCommand = 0x0112;
        var scClose = new IntPtr(0xF060);

        if (keyboardWnd != nullIntPtr)
        {
            _ = NativeMethods.SendMessage(keyboardWnd, wmSysCommand, scClose, nullIntPtr);
        }

        process?.Dispose();
    }

    ////private static bool HasTouchInput()
    ////{
    ////    return Tablet.TabletDevices.Cast<TabletDevice>().Any(tabletDevice => tabletDevice.Type == TabletDeviceType.Touch);
    ////}

    private static class NativeMethods
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string sClassName, string? sAppName);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    }

    private class TouchProcessInfo
    {
        private TouchProcessInfo(string path)
        {
            this.ProcessStartInfo = new ProcessStartInfo(path);
            this.ProcessName = Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Gets the default TouchProcessInfo pointing to C:\Program Files\Common Files\Microsoft Shared\Ink\TabTip.exe.
        /// </summary>
        internal static TouchProcessInfo? Default { get; } = CreateDefault();

        internal ProcessStartInfo ProcessStartInfo { get; }

        internal string ProcessName { get; }

        internal static TouchProcessInfo? Create(string? path)
        {
            if (path is null || Path.GetExtension(path) != ".exe")
            {
                return null;
            }

            if (File.Exists(path))
            {
                return new TouchProcessInfo(path);
            }

            return null;
        }

        private static TouchProcessInfo? CreateDefault()
        {
            const string microsoftSharedInkTabTipExe = @"Microsoft Shared\ink\TabTip.exe";

            return Create(@"C:\Program Files\Common Files\Microsoft Shared\Ink\TabTip.exe") ??
                   Create(Path.Combine(
                              Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                              microsoftSharedInkTabTipExe)) ??
                   Create(Path.Combine(
                              Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                              microsoftSharedInkTabTipExe));
        }
    }
}
