using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Sample.Sdk.Data.Enums;
using Sample.Sdk.Data.Options;
using System.Collections.Concurrent;

namespace Sample.Sdk.Core.Msg
{
    public class ServiceBusFactory : IAsyncDisposable
    {
        protected readonly string MsgContentType = "application/json;charset=utf8";
        private readonly ConcurrentDictionary<string, ServiceBusSender> serviceBusSender = new ConcurrentDictionary<string, ServiceBusSender>();
        /// <summary>
        /// PeekLock is the default, AddReceiveAndDelete
        /// </summary>
        private readonly ConcurrentDictionary<string, ServiceBusReceiver> serviceBusReceiver = new ConcurrentDictionary<string, ServiceBusReceiver>();
        private readonly ConcurrentDictionary<string, ServiceBusProcessor> serviceBusProcessor = new ConcurrentDictionary<string, ServiceBusProcessor>();
        private readonly ServiceBusClient _service;

        public ServiceBusFactory(
            IOptions<List<AzureMessageSettingsOptions>> serviceBusInfoOptions
            , ServiceBusClient service)
        {
            Initialize(
                serviceBusInfoOptions.Value.ToList(),
                service);
            _service = service;
        }
        protected ServiceBusReceiver GetReceiver(string queueName)
        {
            if (!serviceBusReceiver.Any(s => s.Key.ToLower() == queueName.ToLower()))
            {
                return default;
            }
            return serviceBusReceiver.First(s => s.Key.ToLower() == queueName.ToLower()).Value;
        }
        protected ServiceBusSender? GetSender(string queueName, string queueEndpoint)
        {
            if (!serviceBusSender.Any(sender => sender.Key.ToLower() == queueName.ToLower()))
            {
                return default;
            }
            var sender = serviceBusSender.FirstOrDefault(s => s.Key.ToLower() == queueName.ToLower()).Value;
            return sender != null && sender.FullyQualifiedNamespace.Contains(queueEndpoint)
                                    ? sender
                                    : default;
        }
        protected ServiceBusProcessor GetServiceBusProcessor(string queueName, Func<ServiceBusProcessorOptions> options = null)
        {
            if (serviceBusProcessor.TryGetValue(queueName, out var processor))
            {
                return processor;
            }

            processor = options != null ? _service.CreateProcessor(queueName, options.Invoke())
                                        : _service.CreateProcessor(queueName);

            if (serviceBusProcessor.TryAdd(queueName, processor))
            {
                return processor;

            };
            return default;
        }

        private void Initialize(List<AzureMessageSettingsOptions> serviceBusInfoOptions, ServiceBusClient service)
        {
            if (serviceBusInfoOptions == null || serviceBusInfoOptions == null || serviceBusInfoOptions.Count == 0)
            {
                throw new ApplicationException("Service bus info options are required");
            }
            if (service == null)
            {
                throw new ApplicationException("Service bus client must be registered as a services");
            }
            serviceBusInfoOptions.Where(option=> option.ConfigType == Enums.AzureMessageSettingsOptionType.Receiver)
                .ToList().ForEach(option =>
            {
                if (string.IsNullOrEmpty(option.Identifier))
                    throw new ApplicationException("Add identifier to azure service bus info");

                option.MessageInTransitOptions.ForEach(queue =>
                {   
                    if (!string.IsNullOrEmpty(queue.AckQueueName))
                        serviceBusSender?.TryAdd(queue.AckQueueName, service.CreateSender(queue.AckQueueName));
                    if (!string.IsNullOrEmpty(queue.MsgQueueName))
                        serviceBusReceiver?.TryAdd(queue.MsgQueueName, service.CreateReceiver(queue.MsgQueueName));
                });
            });
            serviceBusInfoOptions.Where(option => option.ConfigType == Enums.AzureMessageSettingsOptionType.Sender)
                .ToList().ForEach(option => 
            {
                option.MessageInTransitOptions.ForEach(queue =>
                {
                    if (!string.IsNullOrEmpty(queue.MsgQueueName))
                        serviceBusSender?.TryAdd(queue.MsgQueueName, service.CreateSender(queue.MsgQueueName));
                    //if (!string.IsNullOrEmpty(queue.AckQueueName))
                    //    serviceBusReceiver?.TryAdd(queue.AckQueueName, service.CreateReceiver(queue.AckQueueName));
                });
            });
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var sender in serviceBusSender)
            {
                if (sender.Value != null)
                {
                    await sender.Value.CloseAsync().ConfigureAwait(false);
                }
            }
            foreach (var receiver in serviceBusReceiver)
            {
                if (receiver.Value != null)
                {
                    await receiver.Value.CloseAsync().ConfigureAwait(false);
                }
            }
            foreach (var processor in serviceBusProcessor)
            {
                if (processor.Value != null)
                {
                    await processor.Value.CloseAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
