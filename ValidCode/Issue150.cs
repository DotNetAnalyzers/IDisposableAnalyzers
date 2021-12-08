namespace ValidCode
{
    using System.Collections.Generic;
    using System.IO;

    public class Issue150
    {
        public Issue150(string name)
        {
            this.Name = name;
            if (File.Exists(name))
            {
                this.AllText = File.ReadAllText(name);
                this.AllLines = File.ReadAllLines(name);
            }
        }

        public string Name { get; }

        public bool Exists => File.Exists(this.Name);

        public string? AllText { get; }

        public IReadOnlyList<string>? AllLines { get; }
    }
}
