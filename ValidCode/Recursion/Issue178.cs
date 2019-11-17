namespace ValidCode.Recursion
{
    class Issue178
    {
        void M() => M(new System.IO.BinaryWriter(null));

        void M(System.IDisposable p) => M(p);
    }
}
