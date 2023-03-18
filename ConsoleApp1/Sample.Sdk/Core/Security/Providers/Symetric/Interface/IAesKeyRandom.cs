namespace Sample.Sdk.Core.Security.Providers.Symetric.Interface
{
    public interface IAesKeyRandom 
    {
        byte[] GenerateRandomKey(int keySize);
    }
}
