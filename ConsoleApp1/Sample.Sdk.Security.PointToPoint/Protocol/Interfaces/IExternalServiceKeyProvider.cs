using Sample.Sdk.Azure;
using Sample.Sdk.Security.Providers.Protocol.State;

namespace Sample.Sdk.Security.Providers.Protocol.Interfaces
{
    public interface IExternalServiceKeyProvider
    {
        Task<(bool wasRetrieved, byte[]? publicKey, EncryptionDecryptionFail reason)> GetExternalPublicKey(
            string externalWellknownEndpoint,
            HttpClient httpClient,
            AzureKeyVaultOptions options,
            CancellationToken token);
    }
}