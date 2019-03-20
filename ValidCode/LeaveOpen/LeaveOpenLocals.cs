namespace ValidCode.LeaveOpen
{
    using System.IO;
    using System.Text;

    public class LeaveOpenLocals
    {
        public LeaveOpenLocals(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                using (var reader = new StreamReader(stream, new UTF8Encoding(), true, 1024, leaveOpen: true))
                {
                    _ = reader.ReadLine();
                }

                _ = stream.ReadByte();
            }
        }
    }
}
