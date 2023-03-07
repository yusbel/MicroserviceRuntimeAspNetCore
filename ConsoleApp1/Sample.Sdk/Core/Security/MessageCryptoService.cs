using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Signature;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security
{
    public class MessageCryptoService : IMessageCryptoService
    {
        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;
        private readonly ISymetricCryptoProvider _symetricCryptoProvider;
        private readonly IOptions<CustomProtocolOptions> _protocolOptions;
        private readonly ISignatureCryptoProvider _signatureCryptoProvider;
        private readonly ILogger<MessageCryptoService> _logger;

        public MessageCryptoService(IAsymetricCryptoProvider asymetricCryptoProvider,
            ISymetricCryptoProvider symetricCryptoProvider,
            IOptions<CustomProtocolOptions> protocolOptions,
            ISignatureCryptoProvider signatureCryptoProvider,
            ILogger<MessageCryptoService> logger) 
        {
            _asymetricCryptoProvider = asymetricCryptoProvider;
            _symetricCryptoProvider = symetricCryptoProvider;
            _protocolOptions = protocolOptions;
            _signatureCryptoProvider = signatureCryptoProvider;
            _logger = logger;
        }

        /// <summary>
        /// Encrypt external message. Do not raise exception.
        /// </summary>
        /// <param name="toEncrypt">Message to be encrypted</param>
        /// <returns>True if encrypted, null encrypted message if not encrypted</returns>
        /// <exception cref="OperationCanceledException">Throw when cancelling operation</exception>
        public async Task<(bool wasEncrypted, EncryptedMessage? msg)>
            EncryptExternalMessage(ExternalMessage toEncrypt, CancellationToken token)
        {
            var plainData = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(toEncrypt));
            if (_symetricCryptoProvider.TryEncrypt(plainData, out var result))
            {
                token.ThrowIfCancellationRequested();
                (bool wasEncrypted, byte[]? data, EncryptionDecryptionFail reason) keyEncrypted =
                    await _asymetricCryptoProvider.Encrypt(result!.Key, token).ConfigureAwait(false);
                if (keyEncrypted.data == null || !keyEncrypted.wasEncrypted)
                {
                    return (false, default);
                }
                (bool wasEncrypted, byte[]? data, EncryptionDecryptionFail reason) ivEncrypted =
                    await _asymetricCryptoProvider.Encrypt(result.Iv, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (!ivEncrypted.wasEncrypted || ivEncrypted.data == null)
                {
                    return (false, default);
                }
                var encryptedMsg = new EncryptedMessage()
                {
                    CorrelationId = toEncrypt.CorrelationId,
                    Key = toEncrypt.EntityId,
                    CreatedOn = DateTime.Now.Ticks,
                    WellKnownEndpoint = _protocolOptions.Value.WellknownSecurityEndpoint,
                    DecryptEndpoint = _protocolOptions.Value.DecryptEndpoint,
                    AcknowledgementEndpoint = _protocolOptions.Value.AcknowledgementEndpoint,
                    EncryptedEncryptionIv = Convert.ToBase64String(ivEncrypted.data),
                    EncryptedEncryptionKey = Convert.ToBase64String(keyEncrypted.data),
                    EncryptedContent = Convert.ToBase64String(result.EncryptedData), 
                    CertificateLocation = toEncrypt.CertificateLocation,
                    CertificateKey = toEncrypt.CertificateKey, 
                    MsgDecryptScope = toEncrypt.MsgDecryptScope, 
                    MsgQueueEndpoint = toEncrypt.MsgQueueEndpoint, 
                    MsgQueueName = toEncrypt.MsgQueueName
                };
                try
                {
                    await _signatureCryptoProvider.CreateSignature(encryptedMsg, token);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception e)
                {
                    e.LogException(_logger.LogCritical);
                    return (false, default);
                }
                return (true, encryptedMsg);
            }
            return (false, default);
        }

    }
}
