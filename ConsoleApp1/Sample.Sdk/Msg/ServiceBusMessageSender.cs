using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Data.Options;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Sample.Sdk.EntityModel.MessageHandlingReason;

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
        public async Task<(bool WasSent, SendFailedReason Reason)> 
            Send(CancellationToken token, 
                    ExternalMessage msg, 
                    Action<ExternalMessage> onSent,
                    Action<ExternalMessage, SendFailedReason?, Exception?> onError)
        {
            var sender = GetSender(msg);
            if (sender == null)
            {
                onError?.Invoke(msg, SendFailedReason.InValidSenderEndpoint | SendFailedReason.InValidQueueName, null);
                return (false, SendFailedReason.InValidQueueName | SendFailedReason.InValidSenderEndpoint);
            }
            try
            {
                token.ThrowIfCancellationRequested();
                var serviceBusMsg = new ServiceBusMessage()
                {
                    ContentType = MsgContentType,
                    MessageId = msg.EntityId,
                    CorrelationId = msg.CorrelationId,
                    Body = new BinaryData(System.Text.Json.JsonSerializer.Serialize(msg))
                };
                await sender.SendMessageAsync(serviceBusMsg, token).ConfigureAwait(false);
                onSent?.Invoke(msg);
            }
            catch (Exception e)
            {
                onError?.Invoke(msg, default, e);
                e.LogException(_logger.LogCritical);
            }
            return (true, default);
        }
    }
}
