namespace ValidCode.NetCore
{
    using System.Security.Cryptography;

    public static class Issue286
    {
        public static ECDsaCng M(CngAlgorithm algorithm, string keyId, CngKeyCreationParameters creationParameters)
        {
            var key = CngKey.Create(algorithm, keyId, creationParameters);
            return new ECDsaCng(key);
        }
    }
}
