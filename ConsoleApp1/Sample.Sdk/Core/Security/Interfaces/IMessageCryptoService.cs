using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Interfaces
{
    public interface IMessageCryptoService
    {
        Task<(bool wasEncrypted, EncryptedMessage? msg)>
            EncryptExternalMessage(ExternalMessage toEncrypt, CancellationToken token);

        Task<(bool wasDecrypted, List<KeyValuePair<string,string>> message, EncryptionDecryptionFail reason)>
        GetDecryptedExternalMessage(
           EncryptedMessage encryptedMessage,
           CancellationToken token);
    }
}
