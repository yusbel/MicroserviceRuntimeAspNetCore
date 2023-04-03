using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Data.Entities;
using Sample.Sdk.Data.Msg;
using Sample.Sdk.Data.Options;
using Sample.Sdk.Interface.Msg;
using System.Text;
using System.Text.Json;
using static Sample.Sdk.Core.Extensions.ExternalMessageExtensions;

namespace Sample.Sdk.Core.Msg
{
    public class ServiceBusMessageReceiver : ServiceBusFactory, IMessageReceiver
    {
        private readonly ILogger<ServiceBusMessageReceiver> _logger;
        public ServiceBusMessageReceiver(
            IOptions<List<AzureMessageSettingsOptions>> serviceBusInfoOptions
            , ServiceBusClient service
            , ILoggerFactory loggerFactory) :
            base(serviceBusInfoOptions
                , service)
        {
            _logger = loggerFactory.CreateLogger<ServiceBusMessageReceiver>();
        }

        /// <summary>
        /// Retrieve message from the acknowledgement queue
        /// </summary>
        /// <param name="queueName">Acknowsledgement queue name</param>
        /// <param name="messageProcessor">Process acknowledgement message</param>
        /// <param name="token">Cancel operation</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task ReceiveMessages(string queueName,
            Func<ExternalMessage, Task<bool>> messageProcessor,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var serviceProcessor = GetServiceBusProcessor(queueName, () =>
            {
                return new ServiceBusProcessorOptions()
                {
                    AutoCompleteMessages = true,
                    ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
                    MaxConcurrentCalls = Environment.ProcessorCount
                };
            });
            serviceProcessor.ProcessMessageAsync += async (args) =>
            {
                var externalMsg = JsonSerializer.Deserialize<ExternalMessage>(Encoding.UTF8.GetString(args.Message.Body.ToArray()));
                if (externalMsg != null)
                    await messageProcessor.Invoke(externalMsg);
            };
            serviceProcessor.ProcessErrorAsync +=  (args) => 
            {
                args.Exception.LogException(_logger.LogCritical);
                return Task.CompletedTask;
            }; 
            await serviceProcessor.StartProcessingAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="saveEntity"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ApplicationException"></exception>
        public async Task<ExternalMessage> Receive(CancellationToken token
            , Func<InComingEventEntity, CancellationToken, Task<bool>> saveEntity
            , string queueName = "employeeadded")
        {
            var receiver = GetReceiver(queueName);
            if (receiver == null)
                throw new InvalidOperationException("Receiver not found");

            token.ThrowIfCancellationRequested();

            var message = await receiver.ReceiveMessageAsync(null, token);
            if (message == null)
            {
                return null;
            }
            if (message.ContentType != MsgContentType)
            {
                throw new ApplicationException("Invalid event content type");
            }
            var msgReceivedBytes = message.Body.ToMemory().ToArray();
            var receivedStringMsg = Encoding.UTF8.GetString(msgReceivedBytes);
            var externalMsg = JsonSerializer.Deserialize<ExternalMessage>(receivedStringMsg);
            if (externalMsg == null)
            {
                throw new ApplicationException("Invalid event message");
            }
            var inComingEvent = externalMsg.ConvertToInComingEventEntity();

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
