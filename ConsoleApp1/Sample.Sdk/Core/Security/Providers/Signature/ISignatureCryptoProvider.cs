using Sample.Sdk.Msg.Data;

namespace Sample.Sdk.Core.Security.Providers.Signature
{
    public interface ISignatureCryptoProvider
    {
        Task CreateSignature(EncryptedMessage msg, Enums.Enums.HostTypeOptions keyVaultType, CancellationToken token);
    }
}