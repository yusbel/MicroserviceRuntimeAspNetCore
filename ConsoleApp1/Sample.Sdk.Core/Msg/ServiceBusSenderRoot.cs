using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Data.Enums;
using Sample.Sdk.Data.Options;
using System.Collections.Concurrent;

namespace Sample.Sdk.Core.Msg
{
    public class ServiceBusSenderRoot : IDisposable
    {
        protected readonly string MsgContentType = "application/json;charset=utf8";
        protected readonly ConcurrentDictionary<string, ServiceBusSender> serviceBusSender = new ConcurrentDictionary<string, ServiceBusSender>();

        private readonly ILogger<ServiceBusSenderRoot> _logger;

        public ServiceBusSenderRoot(
            IOptions<List<AzureMessageSettingsOptions>> serviceBusInfoOptions
            , ServiceBusClient service
            , ILogger<ServiceBusSenderRoot> logger)
        {
            Initialize(serviceBusInfoOptions.Value.Where(s => s.ConfigType == Enums.AzureMessageSettingsOptionType.Sender).ToList(), service);
            _logger = logger;
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
            serviceBusInfoOptions.ForEach(option =>
            {
                if (string.IsNullOrEmpty(option.Identifier))
                {
                    throw new ApplicationException("Add identifier to azure service bus info");
                }
                option.MessageInTransitOptions.ForEach(queue =>
                {
                    if (!string.IsNullOrEmpty(queue.MsgQueueName))
                        serviceBusSender?.TryAdd(queue.MsgQueueName, service.CreateSender(queue.MsgQueueName));
                });
            });
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

        public async ValueTask DisposeAsync()
        {
            foreach (var sender in serviceBusSender)
            {
                if (sender.Value != null)
                {
                    await sender.Value.CloseAsync().ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            Task.Run(async () =>
            {
                foreach (var sender in serviceBusSender)
                {
                    if (sender.Value != null)
                    {
                        await sender.Value.CloseAsync().ConfigureAwait(false);
                    }
                }
            }).Wait();
        }
    }
}
