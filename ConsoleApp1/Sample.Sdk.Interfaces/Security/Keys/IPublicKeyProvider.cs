namespace Sample.Sdk.Interface.Security.Keys
{
    public interface IPublicKeyProvider
    {
        Task<byte[]> GetPublicKey(string uri, string keyId, CancellationToken token);
    }
}
