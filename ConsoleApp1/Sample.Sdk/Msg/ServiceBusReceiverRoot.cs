using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Http.Data;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.Msg.Data.Options;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg
{
    public class ServiceBusReceiverRoot : IAsyncDisposable
    {
        protected readonly string MsgContentType = "application/json;charset=utf8";
        private readonly ConcurrentDictionary<string, ServiceBusSender> serviceBusSender = new ConcurrentDictionary<string, ServiceBusSender>();
        /// <summary>
        /// PeekLock is the default, AddReceiveAndDelete
        /// </summary>
        private readonly ConcurrentDictionary<string, ServiceBusReceiver> serviceBusReceiver = new ConcurrentDictionary<string, ServiceBusReceiver>();
        private readonly ConcurrentDictionary<string, ServiceBusProcessor> serviceBusProcessor = new ConcurrentDictionary<string, ServiceBusProcessor>();
        private readonly ServiceBusClient _service;

        public ServiceBusReceiverRoot(
            IOptions<List<AzureMessageSettingsOptions>> serviceBusInfoOptions
            , ServiceBusClient service)
        {
            Initialize(
                serviceBusInfoOptions.Value.Where(s=> s.ConfigType == Core.Enums.Enums.AzureMessageSettingsOptionType.Receiver).ToList(), 
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

        protected ServiceBusSender GetSender(string queueName) 
        {
            if (!serviceBusSender.Any(s => s.Key.ToLower() == queueName.ToLower())) 
            {
                return default;
            }
            return serviceBusSender.First(s => s.Key.ToLower() == queueName.ToLower()).Value;
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
            serviceBusInfoOptions.ForEach(option =>
            {
                if (string.IsNullOrEmpty(option.Identifier))
                {
                    throw new ApplicationException("Add identifier to azure service bus info");
                }

                option.MessageInTransitOptions.ForEach(queue => 
                {
                    if(!string.IsNullOrEmpty(queue.AckQueueName))
                        serviceBusSender?.TryAdd(queue.AckQueueName, service.CreateSender(queue.AckQueueName));
                    if (!string.IsNullOrEmpty(queue.MsgQueueName))
                        serviceBusReceiver?.TryAdd(queue.MsgQueueName, service.CreateReceiver(queue.MsgQueueName));

                });
            });
        }

        
        public async ValueTask DisposeAsync()
        {
            foreach(var sender in serviceBusSender) 
            {
                if(sender.Value != null) 
                {
                    await sender.Value.CloseAsync().ConfigureAwait(false);
                }
            }
            foreach(var receiver in serviceBusReceiver) 
            {
                if(receiver.Value != null) 
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
