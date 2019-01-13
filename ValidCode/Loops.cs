namespace ValidCode
{
    using System.IO;

    public class Loops
    {
        public static bool TryGetStreamForEach(string[] fileNames, out Stream result)
        {
            foreach (var name in fileNames)
            {
                if (name.Length > 5)
                {
                    result = File.OpenRead(name);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static bool TryGetStreamFor(string[] fileNames, out Stream result)
        {
            for (int i = 0; i < fileNames.Length; i++)
            {
                string name = fileNames[i];
                if (name.Length > 5)
                {
                    result = File.OpenRead(name);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static bool TryGetStreamWhile(string[] fileNames, out Stream result)
        {
            var i = 0;
            while (i < fileNames.Length)
            {
                string name = fileNames[i];
                if (name.Length > 5)
                {
                    result = File.OpenRead(name);
                    return true;
                }

                i++;
            }

            result = null;
            return false;
        }
    }
}
