using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Interfaces
{
    public interface IDecryptorService
    {
        Task<(bool wasDecrypted, ExternalMessage? message, EncryptionDecryptionFail reason)>
        GetDecryptedExternalMessage(
           EncryptedMessageMetadata encryptedMessage
           , IAsymetricCryptoProvider cryptoProvider
           , CancellationToken token);
    }
}
