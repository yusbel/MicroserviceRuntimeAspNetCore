using Sample.Sdk.Data.Enums;
using Sample.Sdk.Data.Msg;

namespace Sample.Sdk.Interface.Security.Signature
{
    public interface ISignatureCryptoProvider
    {
        Task CreateSignature(EncryptedMessage msg, Enums.HostTypeOptions keyVaultType, CancellationToken token);
    }
}