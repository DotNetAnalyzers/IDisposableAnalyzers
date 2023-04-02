namespace ValidCode.LeaveOpen;

using System.IO;
using System.Text;

public sealed class LeaveOpenFalse
{
    public void M(string fileName)
    {
        using var reader1 = new StreamReader(File.OpenRead(fileName), new UTF8Encoding(), true, 1024, leaveOpen: false);
        using var reader2 = new StreamReader(File.OpenRead(fileName));
        using var attachment = new System.Net.Mail.Attachment(File.OpenRead(fileName), string.Empty);
    }
}
