// ReSharper disable All
namespace ValidCode
{
    using System.Diagnostics;
    using System.Reflection;

    public class Issue246
    {
        public static Process Spawn(bool isNetCoreApp)
        {
            var process = new Process();
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = true;

            if (isNetCoreApp)
            {
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = Assembly.GetEntryAssembly().Location;
            }
            else
            {
                process.StartInfo.FileName = Assembly.GetEntryAssembly().Location;
            }

            process.Start();
            return process;  // IDISP011	Don't return disposed instance.
        }
    }
}
