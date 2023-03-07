using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Protocol.State;

namespace Sample.Sdk.Core.Security.Providers.Protocol.Interfaces
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