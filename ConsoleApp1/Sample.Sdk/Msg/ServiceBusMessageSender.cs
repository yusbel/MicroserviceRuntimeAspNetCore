using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
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
    public partial class ServiceBusMessageSender : ServiceBusRoot, IMessageBusSender
    {
        private ILogger<ServiceBusMessageSender> _logger;
        public ServiceBusMessageSender(ILoggerFactory loggerFactory
            , IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions
            , IEnumerable<ServiceBusClient> serviceBusClients) : base(serviceBusInfoOptions, serviceBusClients)
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
        public async Task<bool> Send(string queueName, CancellationToken token, IEnumerable<ExternalMessage> messages, Action<IExternalMessage> onSent)
        {
            if (!serviceBusSender.ContainsKey(queueName)) 
            {
                throw new SenderQueueNotRegisteredException();
            }
            var sender = serviceBusSender[queueName];
            //create service bus then iterate over messages, use cancellation token in case the service is stopped while processing
            while (!token.IsCancellationRequested && messages.Any(item=> item != null)) 
            {
                var msg = messages.Take(1).FirstOrDefault(item=> item != null);
                var serviceBusMsg = new ServiceBusMessage() 
                {
                    ContentType= MsgContentType, 
                    MessageId = msg.Key, 
                    CorrelationId = msg.CorrelationId,
                    Body = new BinaryData(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(msg)))
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
                    onSent(new ExternalMessage() { Content = msg.Content, Key = msg.Key, CorrelationId = msg.CorrelationId });
                }
                messages.ToList().Remove(msg);
            }
            return true;
        }

        
    }
}
