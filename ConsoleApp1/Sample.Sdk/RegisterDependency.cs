
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
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.Core.Security;
using Sample.Sdk.Core.Security.Providers.Asymetric;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Core.Security.Providers.Protocol.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Signature;
using Sample.Sdk.Core.Security.Providers.Symetric;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.EntityModel;
using Sample.Sdk.InMemory;
using Sample.Sdk.InMemory.InMemoryListMessage;
using Sample.Sdk.InMemory.Interfaces;
using Sample.Sdk.Msg;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Data.Options;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg.Providers;
using Sample.Sdk.Msg.Providers.Interfaces;
using Sample.Sdk.Services;
using Sample.Sdk.Services.Interfaces;
using Sample.Sdk.Services.Realtime;
using Sample.Sdk.Services.Realtime.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk
{
    /// $Env:AZURE_CLIENT_ID="51df4bce-6532-4345-9be7-5be7af315003"
    /// $Env:AZURE_CLIENT_SECRET="tdm8Q~Cw_e7cLFadttN7Zebacx_kC5Y-0xaWZdv2"
    /// $Env:AZURE_TENANT_ID="c8656f45-daf5-42c1-9b29-ac27d3e63bf3"
    public static class SdkRegisterDependencies
    {
        public static IServiceCollection AddSampleSdk(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ServiceDbContext>();
            services.Configure<DatabaseSettingOptions>(configuration.GetSection(DatabaseSettingOptions.DatabaseSetting));
            services.Configure<MessageSettingsConfigurationOptions>(configuration.GetSection(MessageSettingsConfigurationOptions.SECTION_ID));
            services.Configure<AzureKeyVaultOptions>(configuration.GetSection(AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION));
            services.AddTransient<IHttpClientResponseConverter, HttpClientResponseConverter>();
            services.AddTransient<IMessageInTransitService, MessageInTransitService>();
            services.AddTransient<ISignatureCryptoProvider, SignatureCryptoProvider>();

            services.AddSingleton<IMemoryCacheState<string, X509Certificate2>, MemoryCacheState<string, X509Certificate2>>();
            services.AddSingleton<IMemoryCacheState<string, KeyVaultCertificateWithPolicy>, MemoryCacheState<string, KeyVaultCertificateWithPolicy>>();

            services.AddTransient<IDecryptorService, DecryptorService>();
            services.AddTransient<IAcknowledgementService, AcknowledgementService>();
            services.AddTransient<ISecurityEndpointValidator, SecurityEndpointValidator>();
            services.AddTransient<ISecurePointToPoint, SecurePointToPoint>();
            services.AddTransient<IPointToPointSession, PointToPointSession>();
            services.AddTransient<IOutgoingMessageProvider, SqlOutgoingMessageProvider>();
            services.AddTransient<IMessageCryptoService, MessageCryptoService>();

            services.AddTransient<ISymetricCryptoProvider, AesSymetricCryptoProvider>();
            services.AddTransient<IAsymetricCryptoProvider, X509CertificateServiceProviderAsymetricAlgorithm>();
            services.AddTransient<IExternalServiceKeyProvider, ExternalServiceKeyProvider>();

            services.Configure<CustomProtocolOptions>(configuration.GetSection(CustomProtocolOptions.Identifier));
            
            services.AddSampleSdkAzureKeyVaultCertificateAndSecretClient(configuration);
            services.AddSampleSdkServiceBusReceiver(configuration);
            services.AddSampleSdkServiceBusSender(configuration);
            
            return services;
        }

        public static IServiceCollection AddSampleSdkInMemoryServices(this IServiceCollection services, IConfiguration config) 
        {
            services.AddSingleton<IInMemoryMessageBus<PointToPointSession>, InMemoryMessageBus<PointToPointSession>>();
            services.AddSingleton<IMemoryCacheState<string, ShortLivedSessionState>, MemoryCacheState<string, ShortLivedSessionState>>();
            services.AddTransient<IMemoryCacheState<string, string>, MemoryCacheState<string, string>>();
            services.Configure<MemoryCacheOptions>(config);
            services.AddTransient<IMemoryCache, MemoryCache>();

            services.AddHostedService<MessageSenderRealtimeHostedService>();

            services.AddTransient<IMessageRealtimeService, MessageSenderRealtimeService>();
            services.AddTransient<IMessageSender, ServiceBusMessageSender>();

            services.AddSingleton<IInMemoryCollection<MessageSentFailedIdInMemmoryList, MessageFailed>
                , InMemoryCollection<MessageSentFailedIdInMemmoryList, MessageFailed>>();

            services.AddSingleton<IInMemoryCollection<ExternalMessageSentIdInMemoryList, string>
                , InMemoryCollection<ExternalMessageSentIdInMemoryList, string>>();

            services.AddSingleton<IInMemoryDeDuplicateCache<ExternalMessageInMemoryList, ExternalMessage>
                , InMemoryDeDuplicateCache<ExternalMessageInMemoryList, ExternalMessage>>();

            services.AddSingleton<IInMemoryDeDuplicateCache<CompletedMessageInMemoryList, InComingEventEntity>
                , InMemoryDeDuplicateCache<CompletedMessageInMemoryList, InComingEventEntity>>();

            services.AddSingleton<IInMemoryDeDuplicateCache<InComingEventEntityInMemoryList, InComingEventEntity>
                , InMemoryDeDuplicateCache<InComingEventEntityInMemoryList, InComingEventEntity>>();

            services.AddSingleton<IInMemoryDeDuplicateCache<ComputedMessageInMemoryList, InComingEventEntity>
                , InMemoryDeDuplicateCache<ComputedMessageInMemoryList, InComingEventEntity>>();

            services.AddSingleton<IInMemoryDeDuplicateCache<InCompatibleMessageInMemoryList, InCompatibleMessage>
                , InMemoryDeDuplicateCache<InCompatibleMessageInMemoryList, InCompatibleMessage>>();

            services.AddSingleton<IInMemoryDeDuplicateCache<CorruptedMessageInMemoryList, CorruptedMessage>
                , InMemoryDeDuplicateCache<CorruptedMessageInMemoryList, CorruptedMessage>>();

            services.AddSingleton<IInMemoryDeDuplicateCache<AcknowledgementMessageInMemoryList, InComingEventEntity>
                , InMemoryDeDuplicateCache<AcknowledgementMessageInMemoryList, InComingEventEntity>>();

            return services;
        }

        public static IServiceCollection AddSampleSdkAzureKeyVaultCertificateAndSecretClient(this IServiceCollection services, IConfiguration config) 
        {
            services.Configure<AzureKeyVaultOptions>(config.GetSection(AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION));
            services.AddAzureClients((azureClientBuilder) =>
            {
                var keyVaultOptions = AzureKeyVaultOptions.Create();
                config.GetSection(AzureKeyVaultOptions.SERVICE_SECURITY_KEYVAULT_SECTION).Bind(keyVaultOptions);
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
                var senderOptions = new List<AzureMessageSettingsOptions>();
                config.GetSection(AzureMessageSettingsOptions.SENDER_SECTION_ID).Bind(senderOptions);
                azureClientFactory.AddServiceBusClient(senderOptions.First().ConnStr);
            });
            return services;
        }

        public static IServiceCollection AddSampleSdkServiceBusReceiver(this IServiceCollection services, IConfiguration config) 
        {
            services.Configure<List<AzureMessageSettingsOptions>>(config.GetSection(AzureMessageSettingsOptions.RECEIVER_SECTION_ID));
            
            services.AddAzureClients(azureFactory => 
            {
                var receiverOptions = new List<AzureMessageSettingsOptions>();
                config.GetSection(AzureMessageSettingsOptions.RECEIVER_SECTION_ID).Bind(receiverOptions);
                azureFactory.AddServiceBusClient(receiverOptions.First().ConnStr);
            });
            
            return services;
        }
    }
}
