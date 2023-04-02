// ReSharper disable All
namespace ValidCode.LeaveOpen;

using System;
using System.IO;
using System.Text;

public sealed class LeaveOpenFields : IDisposable
{
    private readonly FileStream stream;
    private readonly StreamReader reader;

    public LeaveOpenFields(string fileName)
    {
        this.stream = File.OpenRead(fileName);
        this.reader = new StreamReader(this.stream, new UTF8Encoding(), true, 1024, leaveOpen: true);
    }

    public string? ReadLine() => this.reader.ReadLine();

    public int ReadByte() => this.stream.ReadByte();

    public void Dispose()
    {
        this.stream.Dispose();
        this.reader.Dispose();
    }
}
