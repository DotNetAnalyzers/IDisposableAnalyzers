#nullable disable
namespace ValidCode;

using System;
using System.Collections.Generic;

public class Issue272 : IDisposable
{
    private bool disposedValue;

    // unrelated internal ctor.

    // unrelated event handler.

    // unrelated properties.

    /// <summary>
    /// Gets the plugin name this instance is pointing to.
    /// </summary>
    public string PluginName { get; private protected set; }

    /// <summary>
    /// Gets the current version of the plugin that is pointed to by this instance.
    /// </summary>
    public string CurrentVersion { get; private protected set; }

    /// <summary>
    /// Gets the installed version of the plugin that is pointed to by this instance.
    /// </summary>
    public string InstalledVersion { get; private protected set; }

    /// <summary>
    /// Gets the url to download the files to the plugin from.
    /// </summary>
    public Uri DownloadUrl { get; private protected set; }

    /// <summary>
    /// Gets the files to the plugin to download.
    /// </summary>
    public List<string> DownloadFiles { get; private protected set; }

    // unrelated public static method that calls the internal ctor.

    /// <summary>
    /// Cleans up the resources used by <see cref="Issue272"/>.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    // unrelated methods snipped.

    private protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue && disposing)
        {
            // prevent any leaks from this.
            this.PluginName = null;
            this.CurrentVersion = null;
            this.InstalledVersion = null;
            this.DownloadUrl = null;
            this.DownloadFiles.Clear();
            this.DownloadFiles = null;
            this.disposedValue = true;
        }
    }
}
