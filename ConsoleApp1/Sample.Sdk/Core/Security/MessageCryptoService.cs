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
            var aad = plainText.GetInTransitAadData();
            if (_msgDataProtection.TryEncrypt(_serviceContext.GetAesKeys().ToList(), msg, aad, out var firstEncryptionResult)) 
            {   
                token.ThrowIfCancellationRequested();
                firstEncryptionResult = firstEncryptionResult.ConvertAll(item => 
                                                {
                                                    item.Key.PlainData = item.Key.EncryptedData;
                                                    item.Value.PlainData = item.Value.EncryptedData;
                                                    return item;
                                                }).ToList();
                //using random keys
                if (_msgDataProtection.TryEncrypt(firstEncryptionResult, aad, out var doubleSymetricResult)) 
                {
                    List<KeyValuePair<SymetricResult, SymetricResult>> doubleEncryptionResult;
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        doubleEncryptionResult = await _msgDataProtection.EncryptMessageKeys(doubleSymetricResult, token)
                                                                        .ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
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
                        WellknownEndpoint = plainText.WellknownEndpoint,
                        DecryptEndpoint = plainText.DecryptEndpoint,
                        AcknowledgementEndpoint = plainText.AcknowledgementEndpoint,
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

        public async Task<(bool wasDecrypted, List<KeyValuePair<string,string>> message, EncryptionDecryptionFail reason)>
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
           
            token.ThrowIfCancellationRequested();

            (bool wasValid, EncryptionDecryptionFail reason) isValidSignature;
            try
            {
                var plainSig = encryptedMessage.GetPlainSignature();
                isValidSignature = await _asymetricCryptoProvider.VerifySignature(Convert.FromBase64String(encryptedMessage.Signature)
                                            , Encoding.UTF8.GetBytes(plainSig)
                                            , token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return (false, default, EncryptionDecryptionFail.Base64StringConvertionFail);
            }

            var encryptedContent = new List<KeyValuePair<SymetricResult, SymetricResult>>();
            for (var i = 0; i < encryptedMessage.CypherPropertyNameKey.Count; i++) 
            {
                encryptedContent.Add(KeyValuePair.Create(encryptedMessage.CypherPropertyNameKey[i].ToSymetricResult(),
                                                            encryptedMessage.CypherPropertyValueKey[i].ToSymetricResult()));
            }
            var keyDecryptionResult = await _msgDataProtection.DecryptMessageKeys(encryptedContent, token).ConfigureAwait(false);
            var aad = encryptedMessage.GetAadData();
            if (_msgDataProtection.TryDecrypt(keyDecryptionResult, 1, aad, out var firstDecryptionResult)) 
            {
                firstDecryptionResult = firstDecryptionResult.ConvertAll(kvp =>
                {
                    kvp.Key.EncryptedData = kvp.Key.PlainData;
                    kvp.Value.EncryptedData = kvp.Value.PlainData;
                    return kvp;
                }).ToList();
                if (_msgDataProtection.TryDecrypt(firstDecryptionResult, 0, aad, out var secondDecryptionResult)) 
                {
                    var plainDataResult = new List<KeyValuePair<string, string>>();
                    for (var i = 0; i < secondDecryptionResult.Count; i++) 
                    {
                        plainDataResult.Add(KeyValuePair.Create(Encoding.UTF8.GetString(secondDecryptionResult[i].Key.PlainData),
                                                                Encoding.UTF8.GetString(secondDecryptionResult[i].Value.PlainData)));
                    }
                    return (true, plainDataResult, default);
                }
            }
            return (false, default, EncryptionDecryptionFail.DecryptionFail);
        }

        private List<KeyValuePair<SymetricResult, SymetricResult>> ConvertExternalMessage(ExternalMessage message) 
        {
            var result = new List<KeyValuePair<SymetricResult, SymetricResult>>();
            var jsonStr = JsonConvert.SerializeObject(message);
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
