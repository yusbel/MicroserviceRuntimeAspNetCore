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
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg
{
    public class ServiceBusMessageReceiver<T> : ServiceBusReceiverRoot, IMessageBusReceiver<T> where T : ExternalMessage
    {
        private readonly ILogger<ServiceBusMessageReceiver<T>> _logger;
        public ServiceBusMessageReceiver(
            IOptions<List<AzureMessageSettingsOptions>> serviceBusInfoOptions
            , ServiceBusClient service
            , ILoggerFactory loggerFactory) : 
            base(serviceBusInfoOptions
                , service)
        {
            _logger = loggerFactory.CreateLogger<ServiceBusMessageReceiver<T>>();
        }

        public async Task<ExternalMessage> Receive(CancellationToken token
            , Func<InComingEventEntity,CancellationToken, Task<bool>> saveEntity
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
            
            if(token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

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

            if(token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();

            var result = await saveEntity(inComingEvent, token);
            if (!result)
            {
                await receiver.AbandonMessageAsync(message, null, token);
            }
            else 
            {
                await receiver.CompleteMessageAsync(message, token);
            }
            return externalMsg;
        }
    } 
}
