namespace ValidCode
{
    using System;
    using System.IO;

    public class Throws
    {
        public MemoryStream DisposeInThrow()
        {
            var ms = new MemoryStream();
            try
            {
            }
            catch (Exception)
            {
                ms.Dispose();
                throw;
            }

            return ms;
        }
    }
}
