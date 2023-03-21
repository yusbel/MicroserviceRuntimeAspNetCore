using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Signature
{
    public class SignatureCryptoProvider : ISignatureCryptoProvider
    {
        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;

        public SignatureCryptoProvider(IAsymetricCryptoProvider asymetricCryptoProvider)
        {
            _asymetricCryptoProvider = asymetricCryptoProvider;
        }
        public async Task CreateSignature(EncryptedMessage msg, CancellationToken token)
        {
            if (msg == null)
            {
                throw new ArgumentNullException(nameof(msg));
            }
            token.ThrowIfCancellationRequested();
            var plainSign = msg.GetPlainSignature();
            (bool wasCreated, byte[]? data, EncryptionDecryptionFail reason) =
                   await _asymetricCryptoProvider
                           .CreateSignature(Encoding.UTF8.GetBytes(plainSign), token)
                           .ConfigureAwait(false);
            if (!wasCreated || data == null)
            {
                throw new InvalidOperationException("Unable to create signature for encrypted message");
            }
            msg.Signature = Convert.ToBase64String(data);
        }

    }
}
