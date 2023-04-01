namespace Sample.Sdk.Interface.Security.Symetric
{
    public interface IAesKeyRandom
    {
        byte[] GenerateRandomKey(int keySize);
    }
}
