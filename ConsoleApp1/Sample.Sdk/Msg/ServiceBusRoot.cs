using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Http.Data;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg
{
    public class ServiceBusRoot : IAsyncDisposable
    {
        protected readonly string MsgContentType = "application/json;charset=utf8";
        protected readonly ConcurrentDictionary<string, ServiceBusSender> serviceBusSender = new ConcurrentDictionary<string, ServiceBusSender>();
        /// <summary>
        /// PeekLock is the default, AddReceiveAndDelete
        /// </summary>
        protected readonly ConcurrentDictionary<string, ServiceBusReceiver> serviceBusReceiver = new ConcurrentDictionary<string, ServiceBusReceiver>();
        protected readonly IAsymetricCryptoProvider _asymCryptoProvider;
        private readonly ISymetricCryptoProvider _cryptoProvider;
        private readonly IExternalServiceKeyProvider _serviceKeyProvider;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientResponseConverter _httpResponseConverter;
        private readonly IOptions<AzureKeyVaultOptions> _keyVaultOptions;
        private readonly ISecurePointToPoint _securePointToPoint;
        private readonly ISecurityEndpointValidator _securityEndpointValidator;
        private readonly ILogger<ServiceBusRoot> _logger;

        public ServiceBusRoot(
            IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions
            , ServiceBusClient service
            , IAsymetricCryptoProvider asymCryptoProvider
            , ISymetricCryptoProvider cryptoProvider
            , IExternalServiceKeyProvider serviceKeyProvider
            , HttpClient httpClient
            , IHttpClientResponseConverter httpResponseConverter
            , IOptions<AzureKeyVaultOptions> keyVaultOptions
            , ISecurePointToPoint securePointToPoint
            , ISecurityEndpointValidator securityEndpointValidator
            , ILogger<ServiceBusRoot> logger)
        {
            _asymCryptoProvider = asymCryptoProvider;
            _cryptoProvider = cryptoProvider;
            _serviceKeyProvider = serviceKeyProvider;
            _httpClient = httpClient;
            _httpResponseConverter = httpResponseConverter;
            _keyVaultOptions = keyVaultOptions;
            _securePointToPoint = securePointToPoint;
            _securityEndpointValidator = securityEndpointValidator;
            _logger = logger;
            Initialize(serviceBusInfoOptions, service);
        }

        private void Initialize(IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions, ServiceBusClient service)
        {
            if (serviceBusInfoOptions == null || serviceBusInfoOptions.Value == null || serviceBusInfoOptions.Value.Count == 0)
            {
                throw new ApplicationException("Service bus info options are required");
            }
            if (service == null)
            {
                throw new ApplicationException("Service bus client must be registered as a services");
            }
            serviceBusInfoOptions.Value.ForEach(option =>
            {
                if (string.IsNullOrEmpty(option.QueueNames))
                {
                    throw new ApplicationException("Add queue to azure service bus info");
                }
                if (string.IsNullOrEmpty(option.Identifier))
                {
                    throw new ApplicationException("Add identifier to azure service bus info");
                }

                option.QueueNames.Split(',').ToList().ForEach(q =>
                {
                    var serviceSender = service.CreateSender(q);
                    var serviceReceiver = service.CreateReceiver(q);
                    serviceBusSender?.TryAdd(q, serviceSender);
                    serviceBusReceiver?.TryAdd(q, serviceReceiver);
                });
            });
        }

        public async ValueTask DisposeAsync()
        {
            foreach(var sender in serviceBusSender) 
            {
                if(sender.Value != null) 
                {
                    await sender.Value.CloseAsync();
                }
            }
            foreach(var receiver in serviceBusReceiver) 
            {
                if(receiver.Value != null) 
                {
                    await receiver.Value.CloseAsync();
                }
            }
        }
        protected async Task<(bool wasDecrypted, ExternalMessage? message, EncryptionDecryptionFail reason)> GetDecryptedExternalMessage(
            EncryptedMessageMetadata encryptedMessage
            , IAsymetricCryptoProvider cryptoProvider
            , CancellationToken token)
        {
            if (encryptedMessage == null 
                || !_securityEndpointValidator.IsWellKnownEndpointValid(encryptedMessage.WellKnownEndpoint)
                || !_securityEndpointValidator.IsDecryptEndpointValid(encryptedMessage.DecryptEndpoint)
                || !_securityEndpointValidator.IsAcknowledgementValid(encryptedMessage.AcknowledgementEndpoint)) 
            {
                throw new ApplicationException("Invalid wellknown endpoint was provided");
            }
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
                                                        , Encoding.UTF8.GetBytes(baseSignature));
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error occurred when converting from base 64 string");
                return (false, default(ExternalMessage), EncryptionDecryptionFail.Base64StringConvertionFail);
            }
            if(!isValidSignature.wasValid) 
            {
                return (false, default(ExternalMessage), isValidSignature.reason);
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
            //use symetric algorithm to decrypt message content
            if(_cryptoProvider.TryDecrypt(Convert.FromBase64String(encryptedMessage.EncryptedContent)
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
        protected async Task<(bool wasSent, EncryptionDecryptionFail reason)> SendAcknowledgementToSender(
                                                        string encryptedMessage
                                                        , EncryptedMessageMetadata encryptedMessageMetadata
                                                        , CancellationToken token) 
        {
            (bool wasCreated, PointToPointChannel? channel) = 
                                    await _securePointToPoint.GetOrCreateSessionChannel(
                                            encryptedMessageMetadata.WellKnownEndpoint
                                            , token);
            if (!wasCreated || channel == null || channel.ChannelState == null) 
            {
                return (false, default);
            }
            var createdOn = DateTime.Now.Ticks;
            string base64SessionId;
            try
            {
                base64SessionId = Convert.ToBase64String(channel.ChannelState.SessionIdentifierEncrypted);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error occurred when converting to base 64");
                return (false, EncryptionDecryptionFail.Base64StringConvertionFail);
            }
            var baseSign = $"{base64SessionId}:{createdOn}:{encryptedMessage}";
            var signature = await _asymCryptoProvider.CreateSignature(Encoding.UTF8.GetBytes(baseSign));
            if(!signature.wasCreated || signature.data == null) 
            {
                return (false, signature.reason);
            }
            string base64Signature;
            try
            {
                base64Signature = Convert.ToBase64String(signature.data);
            }
            catch (Exception e) 
            {
                _logger.LogCritical(e, "An error has occurred");
                return (false, EncryptionDecryptionFail.Base64StringConvertionFail);
            }
            var acknowledgeMsg = new MessageProcessedAcknowledgement()
            {
                CreatedOn = createdOn,
                EncryptedExternalMessage = encryptedMessage,
                PointToPointSessionIdentifier = base64SessionId,
                Signature = base64Signature
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(acknowledgeMsg));
            Uri ackUri;
            try 
            {
                ackUri = new Uri(encryptedMessageMetadata.AcknowledgementEndpoint);
            }
            catch(Exception e) 
            {
                _logger.LogCritical(e, "An error ocurred when buildign the uri using the metatada endpoint passed in the message");
                return (false, EncryptionDecryptionFail.InValidAcknowledgementUri);
            }
            (bool isValid, AcknowledgeResponse? validResp, AcknowledgeResponse? inValidResp) = 
                await _httpResponseConverter.InvokePost<AcknowledgeResponse, AcknowledgeResponse>(ackUri, content);
           
            return (isValid, default);
        }
    }
}
