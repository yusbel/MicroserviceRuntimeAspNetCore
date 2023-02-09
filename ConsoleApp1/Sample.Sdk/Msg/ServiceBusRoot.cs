using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg
{
    public class ServiceBusRoot : IAsyncDisposable
    {
        protected readonly string MsgContentType = "application/json;charset=utf8";
        protected readonly ConcurrentDictionary<string, ServiceBusSender> serviceBusSender = new ConcurrentDictionary<string, ServiceBusSender>();
        /// <summary>
        /// PeekLock is the default, AddReceiveAndDelete
        /// </summary>
        protected readonly ConcurrentDictionary<string, ServiceBusReceiver> serviceBusReceiver = new ConcurrentDictionary<string, ServiceBusReceiver>();
        public ServiceBusRoot(IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions, IEnumerable<ServiceBusClient> services) 
        {
            if (serviceBusInfoOptions == null || serviceBusInfoOptions.Value == null || serviceBusInfoOptions.Value.Count == 0) 
            {
                throw new ApplicationException("Service bus info options are required");
            }
            if(services == null || services.ToList().Count == 0) 
            {
                throw new ApplicationException("Service bus client must be registered as a services");
            }
            serviceBusInfoOptions.Value.ForEach(option => 
            {
                if(string.IsNullOrEmpty(option.QueueNames)) 
                {
                    throw new ApplicationException("Add queue to azure service bus info");
                }
                if(string.IsNullOrEmpty(option.Identifier)) 
                {
                    throw new ApplicationException("Add identifier to azure service bus info");
                }
                var serviceBusClient = services.ToList().FirstOrDefault(service=> service.Identifier == option.Identifier);
                if (serviceBusClient != null) 
                {
                    option.QueueNames.Split(',').ToList().ForEach(q => 
                    {
                        var serviceSender = serviceBusClient.CreateSender(q);
                        var serviceReceiver = serviceBusClient.CreateReceiver(q);
                        serviceBusSender?.TryAdd(q, serviceSender);
                        serviceBusReceiver?.TryAdd(q, serviceReceiver);
                    });
                }
            });
        }

        public async ValueTask DisposeAsync()
        {
            foreach(var sender in serviceBusSender) 
            {
                if(sender.Value != null) 
                {
                    await sender.Value.CloseAsync();
                }
            }
            foreach(var receiver in serviceBusReceiver) 
            {
                if(receiver.Value != null) 
                {
                    await receiver.Value.CloseAsync();
                }
            }
        }
    }
}
