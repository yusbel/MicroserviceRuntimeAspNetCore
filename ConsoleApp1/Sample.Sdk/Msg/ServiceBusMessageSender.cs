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
    /// Message sender implementation using azure service bus
    /// </summary>
    public partial class ServiceBusMessageSender : ServiceBusSenderRoot, IMessageSender
    {
        private ILogger<ServiceBusMessageSender> _logger;
        public ServiceBusMessageSender(ILoggerFactory loggerFactory
            , IOptions<List<AzureMessageSettingsOptions>> serviceBusInfoOptions
            , ServiceBusClient serviceBusClient) : 
            base(serviceBusInfoOptions
                , serviceBusClient
                , loggerFactory.CreateLogger<ServiceBusSenderRoot>())
        {
            _logger = loggerFactory.CreateLogger<ServiceBusMessageSender>();
        }

        
        /// <summary>
        /// Send messages
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <param name="messages">List of messages to send</param>
        /// <param name="onSent">Invoked per message successful sent</param>
        /// <returns cref="int">Amount of successful sent messages</returns>
        public async Task<int> Send(CancellationToken token, 
                                        IEnumerable<ExternalMessage> messages, 
                                        Action<ExternalMessage> onSent)
        {
            var exceptions = new List<Exception>();
            var tasks = new List<Task>();
            foreach (var msg in messages)
            {
                var sender = GetSender(msg);
                if (sender == null)
                {
                    exceptions.Add(new SenderQueueNotRegisteredException($"Sender not found for queue name {msg.MsgQueueName} and endpoint {msg.MsgQueueEndpoint}"));
                    continue;
                }
                var task = Task.Run(async () =>
                {
                    var serviceBusMsg = new ServiceBusMessage()
                    {
                        ContentType = MsgContentType,
                        MessageId = msg.Key,
                        CorrelationId = msg.CorrelationId,
                        Body = new BinaryData(System.Text.Json.JsonSerializer.Serialize(msg))
                    };
                    await sender.SendMessageAsync(serviceBusMsg, token).ConfigureAwait(false);
                    onSent?.Invoke(new ExternalMessage()
                    {
                        Content = msg.Content,
                        Key = msg.Key,
                        CorrelationId = msg.CorrelationId
                    });
                }, token);
                _ = task.ConfigureAwait(false);
                tasks.Add(task); 
            }
            try 
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch(Exception e) { exceptions.Add(e); }
            exceptions.ForEach(exception => exception.LogException(_logger.LogCritical));
            return tasks.Count(task=> task.IsCompletedSuccessfully);
        }
    }
}
