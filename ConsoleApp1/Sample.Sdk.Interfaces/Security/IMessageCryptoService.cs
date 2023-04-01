using Sample.Sdk.Data.Msg;
using static Sample.Sdk.Data.Enums.Enums;

namespace Sample.Sdk.Interface.Security
{
    public interface IMessageCryptoService
    {
        Task<(bool wasEncrypted, EncryptedMessage? msg)>
            EncryptExternalMessage(ExternalMessage toEncrypt, CancellationToken token);

        Task<(bool wasDecrypted, List<KeyValuePair<string, string>> message, EncryptionDecryptionFail reason)>
        GetDecryptedExternalMessage(
           EncryptedMessage encryptedMessage,
           CancellationToken token);
    }
}
