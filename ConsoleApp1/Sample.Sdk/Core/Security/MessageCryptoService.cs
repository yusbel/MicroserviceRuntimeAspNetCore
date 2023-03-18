using JsonFlatten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        private readonly IMessageDataProtectionProvider _msgDataProtection;
        private readonly ISecurityEndpointValidator _endpointValidator;
        private readonly IExternalServiceKeyProvider _serviceKeyProvider;
        private readonly HttpClient _httpClient;
        private readonly IOptions<AzureKeyVaultOptions> _keyVaultOptions;
        private readonly ISecurePointToPoint _securePointToPoint;
        private readonly IServiceContext _serviceContext;

        public MessageCryptoService(IAsymetricCryptoProvider asymetricCryptoProvider,
            ISymetricCryptoProvider symetricCryptoProvider,
            IOptions<CustomProtocolOptions> protocolOptions,
            ISignatureCryptoProvider signatureCryptoProvider,
            ILogger<MessageCryptoService> logger,
            IMessageDataProtectionProvider msgDataProtection,
            ISecurityEndpointValidator endpointValidator,
            IExternalServiceKeyProvider serviceKeyProvider,
            HttpClient httpClient,
            IOptions<AzureKeyVaultOptions> keyVaultOptions,
            ISecurePointToPoint securePointToPoint,
            IServiceContext serviceContext)
        {
            _asymetricCryptoProvider = asymetricCryptoProvider;
            _symetricCryptoProvider = symetricCryptoProvider;
            _protocolOptions = protocolOptions;
            _signatureCryptoProvider = signatureCryptoProvider;
            _logger = logger;
            _msgDataProtection = msgDataProtection;
            _endpointValidator = endpointValidator;
            _serviceKeyProvider = serviceKeyProvider;
            _httpClient = httpClient;
            _keyVaultOptions = keyVaultOptions;
            _securePointToPoint = securePointToPoint;
            _serviceContext = serviceContext;
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
            if (_msgDataProtection.TryEncrypt(_serviceContext.GetAesKeys().ToList(), msg, plainText.GetInTransitAadData(), out var msgProtectionResult)) 
            {
                var protectedData = new Dictionary<byte[], byte[]>();
                var encryptionKeyDict = new List<KeyValuePair<byte[], byte[]>>();
                var nonces = new List<KeyValuePair<byte[], byte[]>>();
                var tags = new List<KeyValuePair<byte[], byte[]>>();
                try
                {
                    msgProtectionResult.ForEach(item =>
                    {
                        encryptionKeyDict.Add(KeyValuePair.Create(item.Key.Key, item.Value.Key));
                        nonces.Add(KeyValuePair.Create(item.Key.Nonce, item.Value.Nonce));
                        protectedData.Add(item.Key.EncryptedData, item.Value.EncryptedData);
                        tags.Add(KeyValuePair.Create(item.Key.Tag, item.Value.Tag));
                    });
                }
                catch (Exception e)
                {
                    throw;
                }
                if(msg.Count != encryptionKeyDict.Count) 
                {
                    throw new InvalidOperationException();
                }
                token.ThrowIfCancellationRequested();

                //using random keys
                if (_msgDataProtection.TryEncrypt(protectedData, plainText.GetInTransitAadData(), out var doubleSymetricResult)) 
                {
                    token.ThrowIfCancellationRequested();
                    var doubleEncryptionKeyDict = new List<KeyValuePair<byte[], byte[]>>();
                    var doubleNonces = new List<KeyValuePair<byte[], byte[]>>();
                    var doubleProtectedData = new Dictionary<byte[], byte[]>();
                    var doubleTag = new List<KeyValuePair<byte[], byte[]>>();
                    try
                    {
                        doubleSymetricResult.ForEach(item =>
                                    {
                                        doubleEncryptionKeyDict.Add(KeyValuePair.Create(item.Key.Key, item.Value.Key));
                                        doubleNonces.Add(KeyValuePair.Create(item.Key.Nonce, item.Value.Nonce));
                                        doubleProtectedData.Add(item.Key.EncryptedData, item.Value.EncryptedData);
                                        doubleTag.Add(KeyValuePair.Create(item.Key.Tag, item.Value.Tag));
                                    });
                    }
                    catch (Exception e)
                    {
                        throw;
                    }

                    var noncesStr = nonces.ConvertToString();
                    var tagsStr = tags.ConvertToString();

                    var noncesDoubleStr = doubleNonces.ConvertToString();
                    var dataDoubleStr = doubleProtectedData.ConvertDictionaryKeysAndValuesIntoBase64String();
                    var tagDoubleStr = doubleTag.ConvertToString();

                    //Asymetric encryption to protect Aes keys
                    var doubleEncryptedKeys = await _msgDataProtection.EncryptMessageKeys(doubleEncryptionKeyDict, token).ConfigureAwait(false);
                    var doubleStr = doubleEncryptedKeys.ConvertDictionaryKeysAndValuesIntoBase64String();

                    var encryptedKeys = await _msgDataProtection.EncryptMessageKeys(encryptionKeyDict, token).ConfigureAwait(false);
                    var encryptedStr = encryptedKeys.ConvertDictionaryKeysAndValuesIntoBase64String();

                    token.ThrowIfCancellationRequested();

                    var encryptedMsg = new EncryptedMessage()
                    {
                        DoubleCypherPropertyKeyKey = doubleStr.key,
                        DoubleCypherPropertyNameKey = doubleStr.value,
                        CypherPropertyNameKey = encryptedStr.key,
                        CypherPropertyValueKey = encryptedStr.value,
                        CorrelationId = plainText.CorrelationId,
                        Key = plainText.EntityId,
                        CreatedOn = DateTime.Now.Ticks,
                        WellknownEndpoint = _protocolOptions.Value.WellknownSecurityEndpoint,
                        DecryptEndpoint = _protocolOptions.Value.DecryptEndpoint,
                        AcknowledgementEndpoint = _protocolOptions.Value.AcknowledgementEndpoint,
                        NonceKey = noncesStr.key,
                        NonceValue = noncesStr.value,
                        DoubleNonceKey = noncesDoubleStr.key,
                        DoubleNonceValue = noncesDoubleStr.value,
                        TagKey = tagsStr.key,
                        TagValue = tagsStr.value,
                        DoubleTagKey = tagDoubleStr.key,
                        DoubleTagValue= tagDoubleStr.value,
                        CypherContentKey = dataDoubleStr.key,
                        CypherContentValue = dataDoubleStr.value,
                        CertificateVaultUri = plainText.CertificateVaultUri,
                        CertificateKey = plainText.CertificateKey,
                        MsgDecryptScope = plainText.MsgDecryptScope,
                        MsgQueueEndpoint = plainText.MsgQueueEndpoint,
                        MsgQueueName = plainText.MsgQueueName
                    };
                    try
                    {
                        await _signatureCryptoProvider.CreateSignature(encryptedMsg, token).ConfigureAwait(false);
                        return (true, encryptedMsg);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception e)
                    {
                        e.LogException(_logger.LogCritical);
                        return (false, default);
                    }
                }
            }
            
            return (false, default);
        }

        public async Task<(bool wasDecrypted, Dictionary<string,string> message, EncryptionDecryptionFail reason)>
        GetDecryptedExternalMessage(
           EncryptedMessage encryptedMessage
           , CancellationToken token)
        {
            if (encryptedMessage == null)
                throw new ArgumentNullException(nameof(encryptedMessage));
            if (!_endpointValidator.IsWellKnownEndpointValid(encryptedMessage.WellknownEndpoint))
                throw new ArgumentException("Invalid Wellknown endpoint");
            if (!_endpointValidator.IsDecryptEndpointValid(encryptedMessage.DecryptEndpoint))
                throw new ArgumentException("Invalid decrypt endpoint");
            if (!_endpointValidator.IsAcknowledgementValid(encryptedMessage.AcknowledgementEndpoint))
                throw new ArgumentException("Invalid acknowledgement endpoint");

            token.ThrowIfCancellationRequested();

            (bool wasValid, EncryptionDecryptionFail reason) isValidSignature;
            try
            {
                isValidSignature = await _asymetricCryptoProvider.VerifySignature(Convert.FromBase64String(encryptedMessage.Signature)
                                            , Encoding.UTF8.GetBytes(encryptedMessage.GetPlainSignature())
                                            , token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default, EncryptionDecryptionFail.Base64StringConvertionFail);
            }

            //double decryption keys
            var doubleCypherKeys = new Dictionary<byte[], byte[]>();
            doubleCypherKeys.ConvertEncryptedStringsToDictionaryByteArray(encryptedMessage.DoubleCypherPropertyKeyKey, encryptedMessage.DoubleCypherPropertyNameKey);
            var doubleKeyDecryptionResult = await _msgDataProtection.DecryptMessageKeys(doubleCypherKeys, token).ConfigureAwait(false);
            
            var cypherKeys = new Dictionary<byte[], byte[]>();
            cypherKeys.ConvertEncryptedStringsToDictionaryByteArray(encryptedMessage.CypherPropertyNameKey, encryptedMessage.CypherPropertyValueKey);
            var keyDecryptionResult = await _msgDataProtection.DecryptMessageKeys(cypherKeys, token).ConfigureAwait(false);

            //double decrypting content
            var encryptedData = new Dictionary<byte[], byte[]>();
            encryptedData.ConvertEncryptedStringsToDictionaryByteArray(encryptedMessage.CypherContentKey, encryptedMessage.CypherContentValue);  
            var nonces = new Dictionary<byte[], byte[]>();
            nonces.ConvertEncryptedStringsToDictionaryByteArray(encryptedMessage.DoubleNonceKey, encryptedMessage.DoubleNonceValue);
            var tags = new Dictionary<byte[], byte[]>();
            tags.ConvertEncryptedStringsToDictionaryByteArray(encryptedMessage.DoubleTagKey, encryptedMessage.DoubleTagValue);

            if (_msgDataProtection.TryDecrypt(doubleKeyDecryptionResult,
                                                encryptedData,
                                                nonces,
                                                tags,
                                                encryptedMessage.GetAadData(), out var symetricDecryptResult)) 
            {
                encryptedData.Clear();
                foreach (var item in symetricDecryptResult) 
                {
                    encryptedData.Add(item.Key.EncryptedData, item.Value.EncryptedData);
                }
                nonces.Clear();
                nonces.ConvertEncryptedStringsToDictionaryByteArray(encryptedMessage.NonceKey, encryptedMessage.NonceValue);
                tags.Clear();
                tags.ConvertEncryptedStringsToDictionaryByteArray(encryptedMessage.TagKey, encryptedMessage.TagValue);
                if (_msgDataProtection.TryDecrypt(keyDecryptionResult,
                                                encryptedData,
                                                nonces, tags, encryptedMessage.GetAadData(), out var plainText)) 
                {
                    var msg = new Dictionary<string, string>();
                    foreach (var item in plainText) 
                    {
                        msg.Add(Encoding.UTF8.GetString(item.Key.PlainData), Encoding.UTF8.GetString(item.Value.PlainData));
                    }
                    return (true, msg, EncryptionDecryptionFail.None);
                }
            }
            return (false, default, EncryptionDecryptionFail.DecryptionFail);
        }

        private Dictionary<byte[], byte[]> ConvertExternalMessage(ExternalMessage message) 
        {
            var result = new Dictionary<byte[], byte[]>();
            var jsonStr = JsonSerializer.Serialize(message);
            var jObj = JObject.Parse(jsonStr);
            var flatted = jObj.Flatten(false);
            flatted.Keys.ToList().ForEach(key => 
            {
                result.Add(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(flatted[key].ToString()));
            });
            return result;
        }
    }
}
