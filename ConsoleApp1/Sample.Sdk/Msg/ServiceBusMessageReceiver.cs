using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Attributes;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
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
            , IEnumerable<ServiceBusClient> services) : base(serviceBusInfoOptions, services)
        {
        }

        public async Task<T> Receive(CancellationToken token, Func<T, Task<T>> processBeforeCompleted, string queueName = "")
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
            if (!serviceBusReceiver.ContainsKey(queueName) || serviceBusReceiver[queueName] == null) 
            {
                throw new ApplicationException("No receiver registered for this queue");
            }
            var receiver = serviceBusReceiver[queueName];
            var message = await receiver.ReceiveMessageAsync(null, token);
            if (message.ContentType != MsgContentType) 
            {
                throw new ApplicationException("Invalid message content type");
            }
            var msgToReturn = System.Text.Json.JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(message.Body.ToMemory().ToArray()));
            if (msgToReturn == null) 
            {
                throw new ApplicationException("Invalid message received");
            }
            await processBeforeCompleted(msgToReturn);
            await receiver.CompleteMessageAsync(message);
            return msgToReturn;
        }
    }
}
