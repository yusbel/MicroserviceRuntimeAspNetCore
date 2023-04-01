using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Data.Msg;
using Sample.Sdk.Interface.Security.Asymetric;
using Sample.Sdk.Interface.Security.Signature;
using System.Text;
using static Sample.Sdk.Data.Enums.Enums;

namespace Sample.Sdk.Core.Security.Signature
{
    public class SignatureCryptoProvider : ISignatureCryptoProvider
    {
        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;

        public SignatureCryptoProvider(IAsymetricCryptoProvider asymetricCryptoProvider)
        {
            _asymetricCryptoProvider = asymetricCryptoProvider;
        }
        public async Task CreateSignature(EncryptedMessage msg,
            HostTypeOptions keyVaultType,
            CancellationToken token)
        {
            if (msg == null)
            {
                throw new ArgumentNullException(nameof(msg));
            }
            token.ThrowIfCancellationRequested();
            var plainSign = msg.GetPlainSignature();
            (bool wasCreated, byte[]? data, EncryptionDecryptionFail reason) =
                   await _asymetricCryptoProvider
                           .CreateSignature(Encoding.UTF8.GetBytes(plainSign), keyVaultType, token)
                           .ConfigureAwait(false);
            if (!wasCreated || data == null)
            {
                throw new InvalidOperationException("Unable to create signature for encrypted message");
            }
            msg.Signature = Convert.ToBase64String(data);
        }

    }
}
