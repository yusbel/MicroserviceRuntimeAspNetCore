using JsonFlatten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Core.Security.DataProtection;
using Sample.Sdk.Core.Security.Interfaces;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Signature;
using Sample.Sdk.Core.Security.Providers.Symetric;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Sample.Sdk.Core.Security
{
    public class MessageCryptoService : IMessageCryptoService
    {
        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;
        private readonly ISignatureCryptoProvider _signatureCryptoProvider;
        private readonly ILogger<MessageCryptoService> _logger;
        private readonly IMessageDataProtectionProvider _msgDataProtection;
        private readonly ISecurityEndpointValidator _endpointValidator;
        private readonly IOptions<List<AzureKeyVaultOptions>> _keyVaultOptions;

        public MessageCryptoService(IAsymetricCryptoProvider asymetricCryptoProvider,
            ISignatureCryptoProvider signatureCryptoProvider,
            ILogger<MessageCryptoService> logger,
            IMessageDataProtectionProvider msgDataProtection,
            ISecurityEndpointValidator endpointValidator,
            IOptions<List<AzureKeyVaultOptions>> keyVaultOptions)
        {
            _asymetricCryptoProvider = asymetricCryptoProvider;
            _signatureCryptoProvider = signatureCryptoProvider;
            _logger = logger;
            _msgDataProtection = msgDataProtection;
            _endpointValidator = endpointValidator;
            _keyVaultOptions = keyVaultOptions;
        }

        /// <summary>
        /// Encrypt external message. Do not raise exception.
        /// </summary>
        /// <param name="plainText">Message to be encrypted</param>
        /// <returns>True if encrypted, null encrypted message if not encrypted</returns>
        /// <exception cref="OperationCanceledException">Throw when cancelling operation</exception>
        public async Task<(bool wasEncrypted, EncryptedMessage? msg)>
            EncryptExternalMessage(ExternalMessage plainText, CancellationToken token)
        {
            var msg = ConvertExternalMessage(plainText);
            //using context keys
            var aad = plainText.GetInTransitAadData();
            var doubleEncryptionResult = await _msgDataProtection.Protect(msg, aad, token).ConfigureAwait(false);

            var listKeyEncryptedContent = new List<string>();
            var listValueEncryptedContent = new List<string>();
            for (var i = 0; i < doubleEncryptionResult.Count; i++)
            {
                listKeyEncryptedContent.Add(doubleEncryptionResult[i].Key.ConvertToBase64String());
                listValueEncryptedContent.Add(doubleEncryptionResult[i].Value.ConvertToBase64String());
            }

            token.ThrowIfCancellationRequested();

            var encryptedMsg = new EncryptedMessage()
            {
                CypherPropertyNameKey = listKeyEncryptedContent,
                CypherPropertyValueKey = listValueEncryptedContent,
                CorrelationId = plainText.CorrelationId,
                Key = plainText.EntityId,
                CreatedOn = DateTime.Now.Ticks,
                CertificateVaultUri = plainText.CertificateVaultUri,
                CertificateKey = plainText.CertificateKey,
                MsgDecryptScope = plainText.MsgDecryptScope,
                MsgQueueEndpoint = plainText.MsgQueueEndpoint,
                MsgQueueName = plainText.MsgQueueName, 
                SignDataKeyId = plainText.SignDataKeyId, 
                CryptoEndpoint = plainText.CryptoEndpoint,
            };
            try
            {
                await _signatureCryptoProvider.CreateSignature(encryptedMsg, 
                    Enums.Enums.AzureKeyVaultOptionsType.ServiceInstance, 
                    token)
                    .ConfigureAwait(false);
                return (true, encryptedMsg);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default);
            }
        }

        public async Task<(bool wasDecrypted, List<KeyValuePair<string,string>> message, EncryptionDecryptionFail reason)>
        GetDecryptedExternalMessage(
           EncryptedMessage encryptedMessage
           , CancellationToken token)
        {
            if (encryptedMessage == null)
                throw new ArgumentNullException(nameof(encryptedMessage));
            
            token.ThrowIfCancellationRequested();

            (bool wasValid, EncryptionDecryptionFail reason) isValidSignature;
            try
            {
                var plainSig = encryptedMessage.GetPlainSignature();
                isValidSignature = await _asymetricCryptoProvider.VerifySignature(
                                            encryptedMessage.CryptoEndpoint,
                                            encryptedMessage.SignDataKeyId,
                                            Convert.FromBase64String(encryptedMessage.Signature), 
                                            Encoding.UTF8.GetBytes(plainSig), 
                                            token).ConfigureAwait(false);
                if (!isValidSignature.wasValid) 
                {
                    return (false, default!, EncryptionDecryptionFail.VerifySignatureFail);
                }
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default!, EncryptionDecryptionFail.Base64StringConvertionFail);
            }

            var encryptedContent = new List<KeyValuePair<SymetricResult, SymetricResult>>();
            for (var i = 0; i < encryptedMessage.CypherPropertyNameKey.Count; i++) 
            {
                encryptedContent.Add(KeyValuePair.Create(encryptedMessage.CypherPropertyNameKey[i].ToSymetricResult(),
                                                            encryptedMessage.CypherPropertyValueKey[i].ToSymetricResult()));
            }

            var aad = encryptedMessage.GetAadData();
            var plainResult = await _msgDataProtection.UnProtect(encryptedContent, aad, token).ConfigureAwait(false);
            return (true, plainResult, EncryptionDecryptionFail.None);
        }

        private List<KeyValuePair<SymetricResult, SymetricResult>> ConvertExternalMessage(ExternalMessage message) 
        {
            var result = new List<KeyValuePair<SymetricResult, SymetricResult>>();
            var jsonStr = System.Text.Json.JsonSerializer.Serialize(message, message.GetType());
            var jObj = JObject.Parse(jsonStr);
            var flatted = jObj.Flatten(false);
            flatted.Keys.ToList().ForEach(key => 
            {
                var symetricKeyKey = new SymetricResult 
                { 
                    PlainData = Encoding.UTF8.GetBytes(key) 
                };
                var symetricValueKey = new SymetricResult
                {
                    PlainData = Encoding.UTF8.GetBytes(flatted[key].ToString())
                };
                result.Add(KeyValuePair.Create(symetricKeyKey, symetricValueKey));
            });
            return result;
        }
    }
}
