namespace ValidCode.NetCore
{
    using System.Security.Cryptography;

    public static class Issue286
    {
        public static ECDsaCng M(CngAlgorithm algorithm, string keyId, CngKeyCreationParameters creationParameters)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            var key = CngKey.Create(algorithm, keyId, creationParameters);
            return new ECDsaCng(key);
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }
}
