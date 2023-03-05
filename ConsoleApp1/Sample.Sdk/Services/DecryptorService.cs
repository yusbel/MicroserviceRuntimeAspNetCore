using Microsoft.IdentityModel.Tokens;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Http.Data;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Microsoft.Extensions.Logging;
using System.Diagnostics.SymbolStore;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;

namespace Sample.Sdk.Services
{
    public class DecryptorService : IDecryptorService
    {
        private readonly ISecurityEndpointValidator _securityEndpointValidator;
        private readonly IExternalServiceKeyProvider _serviceKeyProvider;
        private readonly HttpClient _httpClient;
        private readonly IOptions<AzureKeyVaultOptions> _keyVaultOptions;
        private readonly IAsymetricCryptoProvider _asymCryptoProvider;
        private readonly ILogger<DecryptorService> _logger;
        private readonly ISecurePointToPoint _securePointToPoint;
        private readonly ISymetricCryptoProvider _symetricCryptoProvider;

        public DecryptorService(
            ISecurityEndpointValidator securityEndpointValidator,
            IExternalServiceKeyProvider serviceKeyProvider,
            HttpClient httpClient,
            IOptions<AzureKeyVaultOptions> keyVaultOptions,
            IAsymetricCryptoProvider asymCryptoProvider,
            ILogger<DecryptorService> logger,
            ISecurePointToPoint securePointToPoint,
            ISymetricCryptoProvider symetricCryptoProvider)
        {
            _securityEndpointValidator = securityEndpointValidator;
            _serviceKeyProvider = serviceKeyProvider;
            _httpClient = httpClient;
            _keyVaultOptions = keyVaultOptions;
            _asymCryptoProvider = asymCryptoProvider;
            _logger = logger;
            _securePointToPoint = securePointToPoint;
            _symetricCryptoProvider = symetricCryptoProvider;
        }

        public async Task<(bool wasDecrypted, ExternalMessage? message, EncryptionDecryptionFail reason)>
        GetDecryptedExternalMessage(
           EncryptedMessage encryptedMessage
           , IAsymetricCryptoProvider cryptoProvider
           , CancellationToken token)
        {
            if (encryptedMessage == null)
                throw new ArgumentNullException(nameof(encryptedMessage));
            if (!_securityEndpointValidator.IsWellKnownEndpointValid(encryptedMessage.WellKnownEndpoint))
                throw new ArgumentException("Invalid Wellknown endpoint");
            if (!_securityEndpointValidator.IsDecryptEndpointValid(encryptedMessage.DecryptEndpoint))
                throw new ArgumentException("Invalid decrypt endpoint");
            if (!_securityEndpointValidator.IsAcknowledgementValid(encryptedMessage.AcknowledgementEndpoint))
                throw new ArgumentException("Invalid acknowledgement endpoint");

            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            var externalPublicKey = await _serviceKeyProvider.GetExternalPublicKey(
                encryptedMessage.WellKnownEndpoint
                , _httpClient
                , _keyVaultOptions.Value
                , token);
            if (!externalPublicKey.wasRetrieved || externalPublicKey.publicKey == null)
            {
                return (false, default(ExternalMessage), externalPublicKey.reason);
            }
            var baseSignature = $"{encryptedMessage.EncryptedEncryptionKey}:{encryptedMessage.EncryptedEncryptionIv}:{encryptedMessage.CreatedOn}:{encryptedMessage.EncryptedContent}";

            //Verify signature using external public key of the private key used to sign the message
            (bool wasValid, EncryptionDecryptionFail reason) isValidSignature;
            try
            {
                isValidSignature = _asymCryptoProvider.VerifySignature(
                                                        externalPublicKey.publicKey
                                                        , Convert.FromBase64String(encryptedMessage.Signature)
                                                        , Encoding.UTF8.GetBytes(baseSignature)
                                                        , token);
            }
            catch (Exception e)
            {
                AggregateExceptionExtensions.LogCriticalException(e, _logger, "An error occurred when converting from base 64 string");
                return (false, default(ExternalMessage), EncryptionDecryptionFail.Base64StringConvertionFail);
            }
            if (!isValidSignature.wasValid)
            {
                return (false, default, isValidSignature.reason);
            }
            //Decrypt encrypted content
            var resultSymetricEncriptionKey = await _securePointToPoint.Decrypt(
                encryptedMessage.WellKnownEndpoint
                , encryptedMessage.DecryptEndpoint
                , Convert.FromBase64String(encryptedMessage.EncryptedEncryptionKey)
                , cryptoProvider
                , token);
            if (!resultSymetricEncriptionKey.wasDecrypted)
            {
                return (false, default(ExternalMessage), resultSymetricEncriptionKey.reason);
            }
            var symetricIv = await _securePointToPoint.Decrypt(
                encryptedMessage.WellKnownEndpoint
                , encryptedMessage.DecryptEndpoint
                , Convert.FromBase64String(encryptedMessage.EncryptedEncryptionIv)
                , cryptoProvider
                , token);
            if (!symetricIv.wasDecrypted)
            {
                return (false, default(ExternalMessage), symetricIv.reason);
            }

            if (resultSymetricEncriptionKey.data == null
                || resultSymetricEncriptionKey.data.Length == 0
                || symetricIv.data == null
                || symetricIv.data.Length == 0)
            {
                return (false, default(ExternalMessage), EncryptionDecryptionFail.InValidKeys);
            }
            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            //use symetric algorithm to decrypt message content
            if (_symetricCryptoProvider.TryDecrypt(Convert.FromBase64String(encryptedMessage.EncryptedContent)
                , resultSymetricEncriptionKey.data
                , symetricIv.data
                , out var result) && result != null && result.PlainData.Length > 0)
            {
                try
                {
                    var msg = System.Text.Json.JsonSerializer.Deserialize<ExternalMessage>(result.PlainData);
                    return (true, msg, EncryptionDecryptionFail.None);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "An error ocurred when deserializing to external message");
                    return (false, default(ExternalMessage), EncryptionDecryptionFail.DeserializationFail);
                }
            }
            return (false, default, EncryptionDecryptionFail.DecryptionFail);
        }
    }
}
