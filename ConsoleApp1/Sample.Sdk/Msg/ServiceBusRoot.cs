using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
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
        private readonly IOptions<AzureKeyVaultOptions> _keyVaultOptions;
        private readonly ISecurePointToPoint _securePointToPoint;
        private readonly ISecurityEndpointValidator _securityEndpointValidator;

        public ServiceBusRoot(
            IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions
            , ServiceBusClient service
            , IAsymetricCryptoProvider asymCryptoProvider
            , ISymetricCryptoProvider cryptoProvider
            , IExternalServiceKeyProvider serviceKeyProvider
            , HttpClient httpClient
            , IOptions<AzureKeyVaultOptions> keyVaultOptions
            , ISecurePointToPoint securePointToPoint
            , ISecurityEndpointValidator securityEndpointValidator)
        {
            _asymCryptoProvider = asymCryptoProvider;
            _cryptoProvider = cryptoProvider;
            _serviceKeyProvider = serviceKeyProvider;
            _httpClient = httpClient;
            _keyVaultOptions = keyVaultOptions;
            _securePointToPoint = securePointToPoint;
            _securityEndpointValidator = securityEndpointValidator;
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
        protected async Task<ExternalMessage> GetDecryptedExternalMessage(
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
            
            var baseSignature = $"{encryptedMessage.EncryptedEncryptionKey}:{encryptedMessage.EncryptedEncryptionIv}:{encryptedMessage.CreatedOn}:{encryptedMessage.EncryptedContent}";

            //Verify signature using external public key of the private key used to sign the message
            var isValidSignature = _asymCryptoProvider.VerifySignature(
                externalPublicKey
                , Convert.FromBase64String(encryptedMessage.Signature)
                , Encoding.UTF8.GetBytes(baseSignature));
            if(!isValidSignature) 
            {
                throw new ApplicationException("Invalid signature provided");
            }
            //Decrypt encrypted content
            var symetricEncriptionKey = await _securePointToPoint.Decrypt(
                encryptedMessage.WellKnownEndpoint
                , encryptedMessage.DecryptEndpoint
                , Convert.FromBase64String(encryptedMessage.EncryptedEncryptionKey)
                , cryptoProvider
                , token);
            var symetricIv = await _securePointToPoint.Decrypt(
                encryptedMessage.WellKnownEndpoint
                , encryptedMessage.DecryptEndpoint
                , Convert.FromBase64String(encryptedMessage.EncryptedEncryptionIv)
                , cryptoProvider
                , token);
            if (symetricEncriptionKey.Length == 0 || symetricIv.Length == 0)
            {
                throw new ApplicationException("Unable to decrypt key iv");
            }
            //use symetric algorithm to decrypt message content
            if(_cryptoProvider.TryDecrypt(Convert.FromBase64String(encryptedMessage.EncryptedContent)
                , symetricEncriptionKey
                , symetricIv
                , out var result))
            {
                return System.Text.Json.JsonSerializer.Deserialize<ExternalMessage>(result.PlainData);
            }
            return null;
        }

        protected async Task<bool> SendAcknowledgement(string encryptedMessage
                                                        , EncryptedMessageMetadata encryptedMessageMetadata) 
        {
            var channel = await _securePointToPoint.GetOrCreate(encryptedMessageMetadata.WellKnownEndpoint);
            var createdOn = DateTime.Now.Ticks;
            var baseSign = $"{Convert.ToBase64String(channel.SessionIdentifierEncrypted)}:{createdOn}:{encryptedMessage}";
            var signature = await _asymCryptoProvider.CreateSignature(Encoding.UTF8.GetBytes(baseSign));
            var acknowledgeMsg = new MessageProcessedAcknowledgement() 
            {
                CreatedOn= createdOn, 
                EncryptedExternalMessage = encryptedMessage, 
                PointToPointSessionIdentifier = Convert.ToBase64String(channel.SessionIdentifierEncrypted), 
                Signature = Convert.ToBase64String(signature)
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(acknowledgeMsg)); 
            await _httpClient.PostAsync(encryptedMessageMetadata.AcknowledgementEndpoint, content);
            return true;
        }
    }
}
