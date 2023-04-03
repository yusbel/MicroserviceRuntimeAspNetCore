using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Data.Msg;
using System.Text.Json;
using static Sample.Sdk.Core.Extensions.AggregateExceptionExtensions;
using Sample.Sdk.Data.Options;
using Sample.Sdk.Interface.Msg;
using static Sample.Sdk.Data.Enums.Enums;

namespace Sample.Sdk.Core.Msg
{
    /// <summary>
    /// Message sender implementation using azure service bus
    /// </summary>
    public partial class ServiceBusMessageSender : ServiceBusFactory, IMessageSender
    {
        private class Message
        {
            public string EndpointAndQueue { get; init; } = string.Empty;
            public List<ExternalMessage> ExternalMessages { get; init; } = new List<ExternalMessage>();
        }
        private ILogger<ServiceBusMessageSender> _logger;
        public ServiceBusMessageSender(ILoggerFactory loggerFactory
            , IOptions<List<AzureMessageSettingsOptions>> serviceBusInfoOptions
            , ServiceBusClient serviceBusClient) :
            base(serviceBusInfoOptions
                , serviceBusClient)
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
            SendMessage(CancellationToken token,
                    ExternalMessage msg,
                    Action<ExternalMessage> onSent,
                    Action<ExternalMessage, SendFailedReason?, Exception?> onError)
        {
            var sender = GetSender(msg.MsgQueueName, msg.MsgQueueEndpoint);
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
                    Body = new BinaryData(JsonSerializer.Serialize(msg))
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

        /// <summary>
        /// Send messages. All messages would be send to the same queue.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="onSent"></param>
        /// <param name="onError"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> SendMessages(Func<ExternalMessage, string> getQueue,
            IEnumerable<ExternalMessage> messages,
            Action<IEnumerable<ExternalMessage>> onSent,
            Action<IEnumerable<ExternalMessage>, Exception> onError,
            CancellationToken token)
        {
            var queueGrouped = from message in messages
                               let queueName = $"{message.MsgQueueEndpoint}{message.MsgQueueName}"
                               group message by queueName into groupedMessages
                               select new Message()
                               {
                                   EndpointAndQueue = groupedMessages.Key,
                                   ExternalMessages = groupedMessages.ToList()
                               };

            foreach (var message in queueGrouped)
            {
                if (!message.ExternalMessages.Any())
                    continue;
                do
                {
                    var counter = 0;
                    var toSend = message.ExternalMessages.TakeWhile(msg =>
                    {
                        if (counter >= 100)
                            return false;
                        counter++;
                        return true;
                    }).ToList();
                    var sender = GetSender(getQueue(toSend.First()), toSend.First().MsgQueueEndpoint);
                    if (sender == null)
                        throw new InvalidOperationException($"Sender not found for queue {getQueue(toSend.First())}");
                    try
                    {
                        var msgBatch = await sender!.CreateMessageBatchAsync(token).ConfigureAwait(false);
                        toSend.ToList().ForEach(msg =>
                        {
                            var serviceBusMsg = new ServiceBusMessage()
                            {
                                CorrelationId = msg.CorrelationId,
                                MessageId = msg.Id,
                                Body = new BinaryData(JsonSerializer.Serialize(msg))
                            };
                            msgBatch.TryAddMessage(serviceBusMsg);
                        });
                        await sender.SendMessagesAsync(msgBatch, token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        onError(toSend, exception);
                    }
                    try
                    {
                        onSent(toSend);
                    }
                    catch (Exception exception)
                    {
                        exception.LogException(_logger.LogCritical);
                    }
                    message.ExternalMessages.RemoveRange(0, toSend.Count());
                }
                while (message.ExternalMessages.Any());
            }
            return true;
        }
    }
}
