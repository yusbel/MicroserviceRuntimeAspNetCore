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
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg
{
    public class ServiceRoot : IAsyncDisposable
    {
        protected readonly string MsgContentType = "application/json;charset=utf8";
        protected readonly ConcurrentDictionary<string, ServiceBusSender> serviceBusSender = new ConcurrentDictionary<string, ServiceBusSender>();
        /// <summary>
        /// PeekLock is the default, AddReceiveAndDelete
        /// </summary>
        protected readonly ConcurrentDictionary<string, ServiceBusReceiver> serviceBusReceiver = new ConcurrentDictionary<string, ServiceBusReceiver>();
        protected readonly IAsymetricCryptoProvider _asymCryptoProvider;
        private readonly ISymetricCryptoProvider _cryptoProvider;
        private readonly IExternalServiceKeyProvider _serviceKeyProvider;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientResponseConverter _httpResponseConverter;
        private readonly IOptions<AzureKeyVaultOptions> _keyVaultOptions;
        private readonly ISecurePointToPoint _securePointToPoint;
        private readonly ISecurityEndpointValidator _securityEndpointValidator;
        private readonly ILogger<ServiceRoot> _logger;

        public ServiceRoot(
            IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions
            , ServiceBusClient service
            , IAsymetricCryptoProvider asymCryptoProvider
            , ISymetricCryptoProvider cryptoProvider
            , IExternalServiceKeyProvider serviceKeyProvider
            , HttpClient httpClient
            , IHttpClientResponseConverter httpResponseConverter
            , IOptions<AzureKeyVaultOptions> keyVaultOptions
            , ISecurePointToPoint securePointToPoint
            , ISecurityEndpointValidator securityEndpointValidator
            , ILogger<ServiceRoot> logger)
        {
            _asymCryptoProvider = asymCryptoProvider;
            _cryptoProvider = cryptoProvider;
            _serviceKeyProvider = serviceKeyProvider;
            _httpClient = httpClient;
            _httpResponseConverter = httpResponseConverter;
            _keyVaultOptions = keyVaultOptions;
            _securePointToPoint = securePointToPoint;
            _securityEndpointValidator = securityEndpointValidator;
            _logger = logger;
            Initialize(serviceBusInfoOptions, service);
        }

        private void Initialize(IOptions<List<ServiceBusInfoOptions>> serviceBusInfoOptions, ServiceBusClient service)
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
                    var serviceReceiver = service.CreateReceiver(q);
                    serviceBusSender?.TryAdd(q, serviceSender);
                    serviceBusReceiver?.TryAdd(q, serviceReceiver);
                });
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
