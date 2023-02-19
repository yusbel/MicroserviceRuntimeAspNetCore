using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Attributes;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Symetric;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg
{
    public class ServiceBusMessageReceiver<T> : ServiceBusRoot, IMessageBusReceiver<T> where T : ExternalMessage
    {
        public ServiceBusMessageReceiver(
            IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions
            , ServiceBusClient service
            , IAsymetricCryptoProvider asymCryptoProvider
            , ISymetricCryptoProvider cryptoProvider
            , IExternalServiceKeyProvider externalServiceKeyProvider
            , HttpClient httpClient
            , ISecurePointToPoint securePointToPoint
            , IOptions<AzureKeyVaultOptions> options
            , ISecurityEndpointValidator securityEndpointValidator) : 
            base(serviceBusInfoOptions
                , service
                , asymCryptoProvider
                , cryptoProvider
                , externalServiceKeyProvider
                , httpClient
                , options
                , securePointToPoint
                , securityEndpointValidator)
        {
        }
        /// <summary>
        /// Retrieve incommign event from table
        /// Validate and decrypt enciming event using the cutom protocol
        /// Sent acknowdlegement of message processed
        /// Update incoming event entity as processed
        /// </summary>
        /// <param name="getInComingEvents"></param>
        /// <param name="processInComingMessage"></param>
        /// <param name="updateEntity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> Process(
            Func<Task<IEnumerable<InComingEventEntity>>> getInComingEvents
            , Func<ExternalMessage, Task<bool>> processDeryptedInComingMessage
            , Func<InComingEventEntity, Task<bool>> updateEntity
            , CancellationToken token)
        {
            var events = await getInComingEvents();
            foreach(var inComingEvent in events) 
            {
                var encryptMsgMetadata = System.Text.Json.JsonSerializer.Deserialize<EncryptedMessageMetadata>(inComingEvent.Body);
                var externalMessage = await GetDecryptedExternalMessage(encryptMsgMetadata, _asymCryptoProvider, token);
                await processDeryptedInComingMessage(externalMessage);
                //Send acknowledgement to sender service
                await SendAcknowledgement(inComingEvent.Body, encryptMsgMetadata);
                await updateEntity(inComingEvent);
            }
            return true;
        }

        public async Task<ExternalMessage> Receive(CancellationToken token
            , Func<InComingEventEntity, Task<bool>> saveEntity
            , string queueName = "")
        {
            if(!string.IsNullOrEmpty(queueName)) 
            {
                var msgMetadataAttr = typeof(T).GetCustomAttributes(false).OfType<MessageMetadaAttribute>().FirstOrDefault();
                if(msgMetadataAttr == null) 
                {
                    throw new ApplicationException("Queue name is empty");
                }
                queueName = msgMetadataAttr.QueueName;
            }
            if (!serviceBusReceiver.Any(s=> s.Key.ToLower() == queueName.ToLower())) 
            {
                throw new ApplicationException("No receiver registered for this queue");
            }
            var receiver = serviceBusReceiver.First(s=> s.Key.ToLower() == queueName.ToLower()).Value;
            var message = await receiver.ReceiveMessageAsync(null, token);
            if (message == null || message.ContentType != MsgContentType)
            {
                throw new ApplicationException("Invalid event content type");
            }
            var msgReceivedBytes = message.Body.ToMemory().ToArray();
            var receivedStringMsg = Encoding.UTF8.GetString(msgReceivedBytes);
            var externalMsg = System.Text.Json.JsonSerializer.Deserialize<ExternalMessage>(receivedStringMsg);
            if (externalMsg == null) 
            {
                throw new ApplicationException("Invalid event message");
            }
            var inComingEvent = new InComingEventEntity()
            {
                Id = Guid.NewGuid().ToString(),
                Body = externalMsg.Content,
                MessageKey = externalMsg.Key, 
                CreationTime = DateTime.Now.ToLong(), 
                IsDeleted = false, 
                Scheme = String.Empty, 
                Type = string.Empty, 
                Version = "1.0.0",
                WasAcknowledge = false
            };
            var result = await saveEntity(inComingEvent);
            if (!result)
            {
                await receiver.AbandonMessageAsync(message);
            }
            else 
            {
                await receiver.CompleteMessageAsync(message);
            }
            return externalMsg;
        }
    } 
}
