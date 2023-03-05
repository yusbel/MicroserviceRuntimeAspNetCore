
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
using Sample.Sdk.Msg.Providers;
using Sample.Sdk.Services;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
            services.AddTransient<IOutgoingMessageProvider, SqlOutgoingMessageProvider>();

            services.AddTransient<ISymetricCryptoProvider, AesSymetricCryptoProvider>();
            services.AddTransient<IAsymetricCryptoProvider, X509CertificateServiceProviderAsymetricAlgorithm>();
            services.AddTransient<IExternalServiceKeyProvider, ExternalServiceKeyProvider>();

            services.Configure<CustomProtocolOptions>(configuration.GetSection(CustomProtocolOptions.Identifier));
            services.AddSampleSdkInMemoryQueues(configuration);
            services.AddSampleSdkAzureKeyVaultCertificateAndSecretClient(configuration);
            services.AddSampleSdkServiceBusReceiver(configuration);
            services.AddSampleSdkServiceBusSender(configuration);
            
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

        public static IServiceCollection AddSampleSdkAzureKeyVaultCertificateAndSecretClient(this IServiceCollection services, IConfiguration config) 
        {
            services.Configure<AzureKeyVaultOptions>(config.GetSection(AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION));
            services.AddAzureClients((azureClientBuilder) =>
            {
                var keyVaultOptions = config.GetValue<AzureKeyVaultOptions>(AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION);
                azureClientBuilder.AddCertificateClient(new Uri(keyVaultOptions.VaultUri));
                azureClientBuilder.AddSecretClient(new Uri(keyVaultOptions.VaultUri));
            });
            return services;
        }

        public static IServiceCollection AddSampleSdkServiceBusSender(this IServiceCollection services, IConfiguration config) 
        {
            services.Configure<List<AzureMessageSettingsOptions>>(config.GetSection(AzureMessageSettingsOptions.SENDER_SECTION_ID));

            services.AddAzureClients(azureClientFactory =>
            {
                var settingsOptions = config.GetValue<List<AzureMessageSettingsOptions>>(AzureMessageSettingsOptions.SENDER_SECTION_ID);
                var uniqueServiceClients  = settingsOptions.Distinct(new AzureMessageSettingsOptionsComparer());
                uniqueServiceClients.ToList().ForEach(service => 
                {
                    azureClientFactory.AddServiceBusClient(service.ConnStr);
                });
            });
            return services;
        }

        public static IServiceCollection AddSampleSdkServiceBusReceiver(this IServiceCollection services, IConfiguration config) 
        {
            services.Configure<List<AzureMessageSettingsOptions>>(config.GetSection(AzureMessageSettingsOptions.RECEIVER_SECTION_ID));

            services.AddAzureClients(azureFactory => 
            {
                var receiverOptions = config.GetValue<List<AzureMessageSettingsOptions>>(AzureMessageSettingsOptions.RECEIVER_SECTION_ID);
                var uniqueReceivers = receiverOptions.Distinct(new AzureMessageSettingsOptionsComparer());
                uniqueReceivers.ToList().ForEach(service =>
                {
                    azureFactory.AddServiceBusClient(service.ConnStr);
                });
            });
            
            return services;
        }
    }
}
