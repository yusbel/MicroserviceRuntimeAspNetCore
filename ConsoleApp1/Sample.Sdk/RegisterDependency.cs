
using Azure.Core.Extensions;
using Azure.Identity;
using Azure.ResourceManager.ServiceBus.Models;
using Azure.Security.KeyVault.Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Security.Providers.Asymetric;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Symetric;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.EntityModel;
using Sample.Sdk.InMemory;
using Sample.Sdk.Msg;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.InMemoryListMessage;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk
{
    /// $Env:AZURE_CLIENT_ID="51df4bce-6532-4345-9be7-5be7af315003"
    /// $Env:AZURE_CLIENT_SECRET="tdm8Q~Cw_e7cLFadttN7Zebacx_kC5Y-0xaWZdv2"
    /// $Env:AZURE_TENANT_ID="c8656f45-daf5-42c1-9b29-ac27d3e63bf3"
    public static class SdkRegisterDependencies
    {
        public static IServiceCollection AddSampleSdk(this IServiceCollection services, IConfiguration configuration, string serviceBusInfoSection = "")
        {
            services.AddTransient<IHttpClientResponseConverter, HttpClientResponseConverter>();

            services.AddTransient<IDecryptorService, DecryptorService>();
            services.AddTransient<IAcknowledgementService, AcknowledgementService>();
            services.AddTransient<ISecurityEndpointValidator, SecurityEndpointValidator>();
            services.AddTransient<ISecurePointToPoint, SecurePointToPoint>();
            services.AddTransient<IPointToPointChannel, PointToPointChannel>();

            services.AddTransient<ISymetricCryptoProvider, AesSymetricCryptoProvider>();
            services.AddTransient<IAsymetricCryptoProvider, X509CertificateServiceProviderAsymetricAlgorithm>();
            services.AddTransient<IExternalServiceKeyProvider, ExternalServiceKeyProvider>();

            services.Configure<CustomProtocolOptions>(configuration.GetSection(CustomProtocolOptions.Identifier));
            services.AddSampleSdkInMemoryQueues(configuration);
            services.AddSampleSdkPrincipleAccounts(configuration);
            services.AddSampleSdkAzureKeyVaultCertificateClient(configuration);
            services.AddSampleSdkServiceBusClient(configuration, serviceBusInfoSection);
            
            return services;
        }

        public static IServiceCollection AddSampleSdkInMemoryQueues(this IServiceCollection services, IConfiguration config) 
        {
            services.AddSingleton<IInMemoryMessageBus<PointToPointChannel>, InMemoryMessageBus<PointToPointChannel>>();
            services.AddSingleton<IMemoryCacheState<string, ShortLivedSessionState>, MemoryCacheState<string, ShortLivedSessionState>>();
            services.AddTransient<IMemoryCacheState<string, string>, MemoryCacheState<string, string>>();
            services.Configure<MemoryCacheOptions>(config);
            services.AddTransient<IMemoryCache, MemoryCache>();

            services.AddSingleton<IInMemoryProducerConsumerCollection<CompletedMessageInMemoryList, InComingEventEntity>
                , InMemoryProducerConsumerCollection<CompletedMessageInMemoryList, InComingEventEntity>>();

            services.AddSingleton<IInMemoryProducerConsumerCollection<InComingEventEntityInMemoryList, InComingEventEntity>
                , InMemoryProducerConsumerCollection<InComingEventEntityInMemoryList, InComingEventEntity>>();

            services.AddSingleton<IInMemoryProducerConsumerCollection<ComputedMessageInMemoryList, InComingEventEntity>
                , InMemoryProducerConsumerCollection<ComputedMessageInMemoryList, InComingEventEntity>>();

            services.AddSingleton<IInMemoryProducerConsumerCollection<InCompatibleMessageInMemoryList, InCompatibleMessage>
                , InMemoryProducerConsumerCollection<InCompatibleMessageInMemoryList, InCompatibleMessage>>();

            services.AddSingleton<IInMemoryProducerConsumerCollection<CorruptedMessageInMemoryList, CorruptedMessage>
                , InMemoryProducerConsumerCollection<CorruptedMessageInMemoryList, CorruptedMessage>>();

            services.AddSingleton<IInMemoryProducerConsumerCollection<AcknowledgementMessageInMemoryList, InComingEventEntity>
                , InMemoryProducerConsumerCollection<AcknowledgementMessageInMemoryList, InComingEventEntity>>();

            return services;
        }
        public static IServiceCollection AddSampleSdkPrincipleAccounts(this IServiceCollection services, IConfiguration config) 
        {
            services.Configure<List<AzurePrincipleAccount>>(config.GetSection("ServiceSdk:AzurePrincipleAccount"));
            services.AddAzureClients(azureClientBuilder =>
            {
                var azurePrincipleOption = new AzurePrincipleAccount()
                {
                    AZURE_CLIENT_ID = config.GetValue<string>($"{AzurePrincipleAccount.SectionIdentifier}:AZURE_CLIENT_ID"),
                    AZURE_CLIENT_SECRET = config.GetValue<string>($"{AzurePrincipleAccount.SectionIdentifier}:AZURE_CLIENT_SECRET"),
                    AZURE_TENANT_ID = config.GetValue<string>($"{AzurePrincipleAccount.SectionIdentifier}:AZURE_TENANT_ID")
                };
                var keyVaultUri = new Uri(config.GetValue<string>("ServiceSdk:Security:AzureKeyVaultOptions:VaultUri"));
                config.AsEnumerable().ToList().Add(new KeyValuePair<string, string>("AZURE_CLIENT_ID", azurePrincipleOption.AZURE_CLIENT_ID));
                config.AsEnumerable().ToList().Add(new KeyValuePair<string, string>("AZURE_CLIENT_SECRET", azurePrincipleOption.AZURE_CLIENT_SECRET));
                config.AsEnumerable().ToList().Add(new KeyValuePair<string, string>("AZURE_TENANT_ID", azurePrincipleOption.AZURE_TENANT_ID));
            });
            return services;
        }

        public static IServiceCollection AddSampleSdkAzureKeyVaultCertificateClient(this IServiceCollection services, IConfiguration config) 
        {
            services.Configure<AzureKeyVaultOptions>(config.GetSection(AzureKeyVaultOptions.Identifier));
            services.AddAzureClients(azureClientBuilder =>
            {
                var keyVaultUri = new Uri(config.GetValue<string>("ServiceSdk:Security:AzureKeyVaultOptions:VaultUri"));
                
                //Use principle accoutn from environment variables following convention
                azureClientBuilder.AddCertificateClient(keyVaultUri).WithCredential(new DefaultAzureCredential());

            });
            return services;
        }

        public static IServiceCollection AddSampleSdkServiceBusClient(this IServiceCollection services, IConfiguration config, string serviceBusInfoSectionId = "") 
        {
            services.Configure<List<ServiceBusInfoOptions>>(options =>
            {
                if (string.IsNullOrEmpty(serviceBusInfoSectionId))
                {
                    return;
                }
                var sectionElements = config.AsEnumerable()
                                                    .Where(item => item.Key.StartsWith(serviceBusInfoSectionId) && item.Key.Length > serviceBusInfoSectionId.Length + 1)
                                                    .Select(item => KeyValuePair.Create(item.Key.Substring(serviceBusInfoSectionId.Length + 1), item.Value))
                                                    .Where(item => item.Value != null)
                                                    .ToList();
                var sectionGroup = sectionElements
                                                .GroupBy(item => item.Key[0])
                                                .Select(g => KeyValuePair.Create(g.Key, g.Select(groupElem => KeyValuePair.Create(groupElem.Key.Substring(groupElem.Key.IndexOf(':') + 1), groupElem.Value))))
                                                .Where(itemg => itemg.Value != null)
                                                .ToList();
                sectionGroup.ForEach(item =>
                {
                    var serviceBusInfoOption = new ServiceBusInfoOptions();
                    item.Value.ToList().ForEach(kv =>
                    {
                        var propName = kv.Key;
                        var propValue = kv.Value;
                        var property = serviceBusInfoOption.GetType()
                                                    .GetProperties()
                                                    .ToList()
                                                    .Where(item => item.Name.ToLower() == propName.ToLower())
                                                    .Select(item => item).FirstOrDefault();
                        property?.SetValue(serviceBusInfoOption, propValue);
                    });
                    if (!string.IsNullOrEmpty(serviceBusInfoOption.Identifier))
                    {
                        options.Add(serviceBusInfoOption);
                    }
                });
            });
            return services;
        }
    }
}
