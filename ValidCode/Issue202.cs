namespace ValidCode
{
    using System.IO;

    public class Issue202
    {
        void M()
        {
            Stream stream;
            while (true)
            {
                using (stream = new MemoryStream())
                {
                    // Do work.
                }
            }
        }
    }
}
