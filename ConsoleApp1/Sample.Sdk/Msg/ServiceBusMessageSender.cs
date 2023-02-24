using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Sample.Sdk.Msg
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ServiceBusMessageSender : ServiceRoot, IMessageBusSender
    {
        private ILogger<ServiceBusMessageSender> _logger;
        public ServiceBusMessageSender(ILoggerFactory loggerFactory
            , IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions
            , ServiceBusClient serviceBusClient
            , IAsymetricCryptoProvider asymCryptoProvider
            , ISymetricCryptoProvider cryptoProvider
            , IExternalServiceKeyProvider externalServiceKeyProvider
            , HttpClient httpClient
            , IHttpClientResponseConverter httpResponseConverter
            , IOptions<AzureKeyVaultOptions> keyVaultOptions
            , ISecurePointToPoint securePointToPoint
            , ISecurityEndpointValidator validator) : 
            base(serviceBusInfoOptions
                , serviceBusClient
                , asymCryptoProvider
                , cryptoProvider
                , externalServiceKeyProvider
                , httpClient
                , httpResponseConverter
                , keyVaultOptions
                , securePointToPoint
                , validator
                , loggerFactory.CreateLogger<ServiceRoot>())
        {
            _logger = loggerFactory.CreateLogger<ServiceBusMessageSender>();
        }

        
        /// <summary>
        /// Send message given a queue name and messages
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="token"></param>
        /// <param name="messages"></param>
        /// <param name="onSent"></param>
        /// <returns></returns>
        /// <exception cref="SenderQueueNotRegisteredException"></exception>
        public async Task<bool> Send(string queueName, CancellationToken token, IEnumerable<ExternalMessage> messages, Action<ExternalMessage> onSent)
        {
            if (!serviceBusSender.Any(s=>s.Key.ToLower() == queueName.ToLower())) 
            {
                throw new SenderQueueNotRegisteredException();
            }
            var sender = serviceBusSender.First(s=>s.Key.ToLower() == queueName.ToLower()).Value;
            //create service bus then iterate over messages, use cancellation token in case the service is stopped while processing
            foreach(var msg in messages) 
            {
                var serviceBusMsg = new ServiceBusMessage()
                {
                    ContentType = MsgContentType,
                    MessageId = msg.Key,
                    CorrelationId = msg.CorrelationId,
                    Body = new BinaryData(System.Text.Json.JsonSerializer.Serialize(msg))
                };
                try
                {
                    await sender.SendMessageAsync(serviceBusMsg);
                }
                catch (ServiceBusException e)
                {
                    throw;
                }

                if (onSent != null)
                {
                    onSent(new ExternalMessage() 
                    { 
                        Content = msg.Content, 
                        Key = msg.Key, 
                        CorrelationId = msg.CorrelationId 
                    });
                }
            }
            return true;
        }

        
    }
}
