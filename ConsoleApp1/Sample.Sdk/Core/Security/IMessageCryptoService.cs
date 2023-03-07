using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security
{
    public interface IMessageCryptoService
    {
        Task<(bool wasEncrypted, EncryptedMessage? msg)>
            EncryptExternalMessage(ExternalMessage toEncrypt, CancellationToken token);
    }
}
