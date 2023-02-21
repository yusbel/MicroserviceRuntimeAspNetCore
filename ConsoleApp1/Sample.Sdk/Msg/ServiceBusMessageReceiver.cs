using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Attributes;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
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
        private readonly ILogger<ServiceBusMessageReceiver<T>> _logger;
        public ServiceBusMessageReceiver(
            IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions
            , ServiceBusClient service
            , IAsymetricCryptoProvider asymCryptoProvider
            , ISymetricCryptoProvider cryptoProvider
            , IExternalServiceKeyProvider externalServiceKeyProvider
            , HttpClient httpClient
            , IHttpClientResponseConverter httpResponseConverter
            , ISecurePointToPoint securePointToPoint
            , IOptions<AzureKeyVaultOptions> options
            , ISecurityEndpointValidator securityEndpointValidator
            , ILoggerFactory loggerFactory) : 
            base(serviceBusInfoOptions
                , service
                , asymCryptoProvider
                , cryptoProvider
                , externalServiceKeyProvider
                , httpClient
                , httpResponseConverter
                , options
                , securePointToPoint
                , securityEndpointValidator
                , loggerFactory.CreateLogger<ServiceBusRoot>())
        {
            _logger = loggerFactory.CreateLogger<ServiceBusMessageReceiver<T>>();
        }
        /// <summary>
        /// Process message out of order, message that can't be decrypted are skipped. May a dead letter table
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
            IEnumerable<InComingEventEntity> events;
            try
            {
                events = await getInComingEvents();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when retrieving incoming events from database");
                return false;
            }
            EncryptedMessageMetadata? encryptMsgMetadata;
            foreach (var inComingEvent in events) 
            {
                try
                {
                    encryptMsgMetadata = System.Text.Json.JsonSerializer.Deserialize<EncryptedMessageMetadata>(inComingEvent.Body);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "An an error ocurred deserialized the saved message into the encrypted message metadata");
                    continue;
                }
                if (encryptMsgMetadata == null) 
                {
                    continue;
                }
                (bool wasDecrypted, ExternalMessage? message, EncryptionDecryptionFail reason) externalMessage = 
                    await GetDecryptedExternalMessage(encryptMsgMetadata, _asymCryptoProvider, token);
                if (!externalMessage.wasDecrypted || externalMessage.message == null) 
                {
                    continue;
                }
                bool wasProcessed;
                try
                {
                    wasProcessed = await processDeryptedInComingMessage(externalMessage.message);
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "An error ocurred when processing message");
                    continue;
                }
                if (wasProcessed) 
                {
                    try
                    {
                        await updateEntity(inComingEvent);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error ocurred when saving a message that was processed");
                    }
                }
            }
            return true;
        }

        public async Task<bool> SendAcknowledgement(
            Func<Task<IEnumerable<InComingEventEntity>>> getIncomingEventProcessed
            , Func<InComingEventEntity, Task<bool>> updateToProcessed)
        {
            try
            {
                IEnumerable<InComingEventEntity> events;
                try
                {
                    events = await getIncomingEventProcessed();
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "An error ocurred when retrieving incoming events from database");
                    return false;
                }
                EncryptedMessageMetadata? encryptMsgMetadata;
                foreach (var inComingEvent in events)
                {
                    try
                    {
                        encryptMsgMetadata = System.Text.Json.JsonSerializer.Deserialize<EncryptedMessageMetadata>(inComingEvent.Body);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error ocurred when deserializing message from database");
                        continue;
                    }
                    if (encryptMsgMetadata == null) 
                    {
                        _logger.LogCritical($"A message in the database incomming events can not be deserialized to encrypted message metadata");
                        continue;
                    }
                    (bool wasSent, EncryptionDecryptionFail reason) sentResult;
                    try
                    {
                        sentResult = await SendAcknowledgementToSender(inComingEvent.Body, encryptMsgMetadata, CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error ocurred when sending the acknowledge message to sender");
                        await Task.Delay(1000); //adding delay in case is a glitch
                        continue;
                    }
                    try
                    {
                        if(sentResult.wasSent) 
                        {
                            await updateToProcessed(inComingEvent);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error ocurred when updating the acknoedlegement message sent");
                    }
                    
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogCritical("An error occurred {}", e);
                return false;
            }
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
            if(message == null) 
            {
                return null;
            }
            if (message.ContentType != MsgContentType)
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
