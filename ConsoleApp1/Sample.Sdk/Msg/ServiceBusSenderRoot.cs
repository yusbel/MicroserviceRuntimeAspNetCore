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
using Sample.Sdk.Msg.Data;
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
    public class ServiceBusSenderRoot : IAsyncDisposable
    {
        protected readonly string MsgContentType = "application/json;charset=utf8";
        protected readonly ConcurrentDictionary<string, ServiceBusSender> serviceBusSender = new ConcurrentDictionary<string, ServiceBusSender>();
       
        //protected readonly ConcurrentDictionary<string, ServiceBusReceiver> serviceBusReceiver = new ConcurrentDictionary<string, ServiceBusReceiver>();
        private readonly ILogger<ServiceBusSenderRoot> _logger;

        //protected readonly IAsymetricCryptoProvider _asymCryptoProvider;

        public ServiceBusSenderRoot(
            IOptions<List<AzureMessageSettingsOptions>> serviceBusInfoOptions
            , ServiceBusClient service
            , ILogger<ServiceBusSenderRoot> logger)
        {
            Initialize(serviceBusInfoOptions, service);
            _logger = logger;
        }

        private void Initialize(IOptions<List<AzureMessageSettingsOptions>> serviceBusInfoOptions, ServiceBusClient service)
        {
            if (serviceBusInfoOptions == null || serviceBusInfoOptions.Value == null || serviceBusInfoOptions.Value.Count == 0)
            {
                throw new ApplicationException("Service bus info options are required");
            }
            if (service == null)
            {
                throw new ApplicationException("Service bus client must be registered as a services");
            }
            serviceBusInfoOptions.Value.ForEach(option =>
            {
                if (string.IsNullOrEmpty(option.QueueNames))
                {
                    throw new ApplicationException("Add queue to azure service bus info");
                }
                if (string.IsNullOrEmpty(option.Identifier))
                {
                    throw new ApplicationException("Add identifier to azure service bus info");
                }

                option.QueueNames.Split(',').ToList().ForEach(q =>
                {
                    var serviceSender = service.CreateSender(q);
                    serviceBusSender?.TryAdd(q, serviceSender);
                });
            });
        }

        protected ServiceBusSender? GetSender(ExternalMessage externalMsg)
        {
            if(!serviceBusSender.Any(sender=> sender.Key.ToLower() == externalMsg.MsgQueueName.ToLower())) 
            { 
                return default; 
            }
            var sender = serviceBusSender.FirstOrDefault(s => s.Key.ToLower() == externalMsg.MsgQueueName.ToLower()).Value;
            return sender != null && sender.FullyQualifiedNamespace.Contains(externalMsg.MsgQueueEndpoint) 
                                    ? sender 
                                    : default;
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
        }

       
    }
}
